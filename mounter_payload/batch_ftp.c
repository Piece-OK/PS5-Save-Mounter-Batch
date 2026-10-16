/* PS5 Savefile Batcher embedded read-only FTP, GPL-3.0-or-later.
 * PASV socket/address construction adapted from ps5-payload-dev/ftpsrv cmd.c
 * Copyright (C) 2023 John Törnblom. See SOURCES.md for pinned origin and changes.
 * Only the mounted save is exposed. No filesystem mutation or SELF conversion.
 */
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdarg.h>
#include <string.h>
#include <strings.h>
#include <unistd.h>
#include <errno.h>
#include <fcntl.h>
#include <dirent.h>
#include <pthread.h>
#include <signal.h>
#include <sys/stat.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <poll.h>
#include <time.h>
#include "batch_ftp.h"

pthread_mutex_t batch_mount_mutex = PTHREAD_MUTEX_INITIALIZER;
static pthread_t server_thread;
static int listener = -1;
static int ftp_port;
static uint32_t allowed_peer;
static int peer_enabled;

static int write_all(int fd, const void *data, size_t size) {
    const char *p = data;
    while (size) {
        ssize_t n = send(fd, p, size, 0);
        if (n < 0 && errno == EINTR) continue;
        if (n <= 0) return -1;
        p += n; size -= n;
    }
    return 0;
}
static int reply(int fd, const char *fmt, ...) {
    char line[2048]; va_list ap; va_start(ap, fmt);
    int n = vsnprintf(line, sizeof(line), fmt, ap); va_end(ap);
    if (n < 0 || (size_t)n >= sizeof(line)) return -1;
    return write_all(fd, line, (size_t)n);
}
static int line_read(int fd, char *line, size_t capacity) {
    size_t n = 0;
    while (n < capacity - 1) {
        char c; ssize_t count = recv(fd, &c, 1, 0);
        if (count < 0 && errno == EINTR) continue;
        if (count <= 0) return -1;
        if (c == '\n') { line[n] = 0; return 0; }
        if (c != '\r') line[n++] = c;
    }
    return -1;
}
static void timeouts(int fd) {
    struct timeval tv = {30, 0};
    setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof(tv));
    setsockopt(fd, SOL_SOCKET, SO_SNDTIMEO, &tv, sizeof(tv));
}
static int path_resolve(const char *cwd, const char *arg, char *out, size_t capacity) {
    char combined[2048];
    int n = arg[0] == '/' ? snprintf(combined,sizeof(combined),"%s",arg)
        : snprintf(combined,sizeof(combined),"%s/%s",cwd,arg);
    if (n < 0 || (size_t)n >= sizeof(combined)) return -1;
    // Resolve components, but refuse traversal entirely rather than interpreting '..'.
    out[0] = 0; char *save = NULL;
    for (char *part = strtok_r(combined,"/",&save); part; part = strtok_r(NULL,"/",&save)) {
        if (!strcmp(part,"..")) return -1;
        if (!strcmp(part,".")) continue;
        if (strchr(part,'\r') || strchr(part,'\n') || strlen(out)+strlen(part)+2 >= capacity) return -1;
        strcat(out,"/"); strcat(out,part);
    }
    if (!out[0]) strcpy(out,"/");
    const char *root = batch_current_mount();
    if (!strcmp(out,"/") || !strcmp(out,"/mnt") || !strcmp(out,"/mnt/pfs")) return 0;
    size_t len = strlen(root);
    if (!len || strncmp(out,root,len) || (out[len] && out[len]!='/')) return -1;
    // Reject symlinks anywhere under the current mount.
    char check[2048]; snprintf(check,sizeof(check),"%s",out);
    for (char *p=check+len; ; p++) {
        if (*p=='/' || *p==0) {
            char saved=*p; *p=0; struct stat st;
            int ok=lstat(check,&st)==0 && !S_ISLNK(st.st_mode); *p=saved;
            if(!ok)return -1;
            if(!saved)break;
        }
    }
    return 0;
}
static int data_open(int passive, uint32_t peer) {
    if (passive < 0) return -1;
    struct pollfd pfd={passive,POLLIN,0};
    if(poll(&pfd,1,10000)<=0)return -1;
    struct sockaddr_in addr; socklen_t len=sizeof(addr);
    int fd=accept(passive,(struct sockaddr*)&addr,&len);
    if(fd>=0 && addr.sin_addr.s_addr!=peer){close(fd);return -1;}
    if(fd>=0)timeouts(fd);
    return fd;
}
static int listing_line(int fd, const char *name, const struct stat *st) {
    char date[40]; struct tm tm; gmtime_r(&st->st_mtime,&tm);
    strftime(date,sizeof(date),"%b %d %H:%M",&tm);
    return reply(fd,"%crwxr-xr-x 1 0 0 %lld %s %s\r\n",S_ISDIR(st->st_mode)?'d':'-',(long long)st->st_size,date,name);
}
static int send_listing(int fd, const char *path) {
    if (!strcmp(path,"/") || !strcmp(path,"/mnt") || !strcmp(path,"/mnt/pfs")) {
        struct stat st={0};st.st_mode=S_IFDIR;st.st_mtime=time(NULL);
        if(!strcmp(path,"/"))return listing_line(fd,"mnt",&st);
        if(!strcmp(path,"/mnt"))return listing_line(fd,"pfs",&st);
        const char *root=batch_current_mount();
        return root[0]?listing_line(fd,strrchr(root,'/')+1,&st):0;
    }
    DIR *d=opendir(path);if(!d)return -1;
    struct dirent *e;int result=0;errno=0;
    while((e=readdir(d))) {
        if(!strcmp(e->d_name,".")||!strcmp(e->d_name,".."))continue;
        char full[2048];struct stat st;
        int n=snprintf(full,sizeof(full),"%s/%s",path,e->d_name);
        if(n<0||(size_t)n>=sizeof(full)||lstat(full,&st)||(!S_ISREG(st.st_mode)&&!S_ISDIR(st.st_mode))||strchr(e->d_name,'\r')||strchr(e->d_name,'\n')){result=-1;break;}
        if(listing_line(fd,e->d_name,&st)){result=-1;break;}
        errno=0;
    }
    if(!e && errno)result=-1;
    closedir(d);return result;
}
static void session(int c, uint32_t peer) {
    char cwd[2048]="/",line[4096],path[2048];int passive=-1,binary=0;
    timeouts(c);if(reply(c,"220 PS5 Savefile Batcher read-only FTP 0.1.0-dev.5\r\n"))return;
    while(!line_read(c,line,sizeof(line))) {
        char *arg=strchr(line,' ');if(arg)*arg++=0;else arg=line+strlen(line);
        pthread_mutex_lock(&batch_mount_mutex);
        if(!peer_enabled || allowed_peer!=peer){pthread_mutex_unlock(&batch_mount_mutex);break;}
        if(!strcasecmp(line,"USER")||!strcasecmp(line,"PASS"))reply(c,"230 Anonymous read-only session\r\n");
        else if(!strcasecmp(line,"QUIT")){reply(c,"221 Goodbye\r\n");pthread_mutex_unlock(&batch_mount_mutex);break;}
        else if(!strcasecmp(line,"SYST"))reply(c,"215 UNIX Type: L8\r\n");
        else if(!strcasecmp(line,"FEAT"))reply(c,"211-Features\r\n UTF8\r\n SIZE\r\n EPSV\r\n211 End\r\n");
        else if(!strcasecmp(line,"OPTS")||!strcasecmp(line,"NOOP"))reply(c,"200 Ready\r\n");
        else if(!strcasecmp(line,"TYPE")){binary=!strcasecmp(arg,"I");reply(c,binary||!strcasecmp(arg,"A")?"200 Type accepted\r\n":"504 Unsupported type\r\n");}
        else if(!strcasecmp(line,"PWD"))reply(c,"257 \"%s\"\r\n",cwd);
        else if(!strcasecmp(line,"CWD")) {
            struct stat st;
            if(path_resolve(cwd,arg,path,sizeof(path)) || (strcmp(path,"/")&&strcmp(path,"/mnt")&&strcmp(path,"/mnt/pfs")&&(stat(path,&st)||!S_ISDIR(st.st_mode))))reply(c,"550 Path unavailable\r\n");
            else {strcpy(cwd,path);reply(c,"250 Directory changed\r\n");}
        }
        else if(!strcasecmp(line,"PASV")||!strcasecmp(line,"EPSV")) {
            if(passive>=0)close(passive);passive=-1;
            struct sockaddr_in local; socklen_t len=sizeof(local);
            if(getsockname(c,(struct sockaddr*)&local,&len))reply(c,"425 Socket error\r\n");
            else {
                uint32_t address=ntohl(local.sin_addr.s_addr);local.sin_port=0;
                passive=socket(AF_INET,SOCK_STREAM,0);
                if(passive<0 || bind(passive,(struct sockaddr*)&local,sizeof(local)) || listen(passive,1) || getsockname(passive,(struct sockaddr*)&local,&len)) {
                    if(passive>=0)close(passive);passive=-1;reply(c,"425 Passive bind failed\r\n");
                } else {
                    unsigned port=ntohs(local.sin_port);
                    if(!strcasecmp(line,"EPSV"))reply(c,"229 Entering Extended Passive Mode (|||%u|)\r\n",port);
                    else reply(c,"227 Entering Passive Mode (%u,%u,%u,%u,%u,%u)\r\n",(address>>24)&255,(address>>16)&255,(address>>8)&255,address&255,port>>8,port&255);
                }
            }
        }
        else if(!strcasecmp(line,"SIZE")) {
            struct stat st;
            if(path_resolve(cwd,arg,path,sizeof(path))||stat(path,&st)||!S_ISREG(st.st_mode))reply(c,"550 File unavailable\r\n");
            else reply(c,"213 %lld\r\n",(long long)st.st_size);
        }
        else if(!strcasecmp(line,"LIST")||!strcasecmp(line,"RETR")) {
            int retr=!strcasecmp(line,"RETR");
            if(path_resolve(cwd,arg,path,sizeof(path)))reply(c,"550 Path unavailable\r\n");
            else if(retr&&!binary)reply(c,"504 Use TYPE I for file downloads\r\n");
            else if(passive<0)reply(c,"425 Use PASV or EPSV first\r\n");
            else {
                int file=-1;struct stat st;int result=0;
                if(retr && ((file=open(path,O_RDONLY))<0 || fstat(file,&st) || !S_ISREG(st.st_mode)))result=-1;
                if(result)reply(c,"550 File unavailable\r\n");
                else {
                    reply(c,"150 Opening data connection\r\n");int data=data_open(passive,peer);
                    if(data<0)result=-1;
                    else {
                        if(retr) {
                            char *buf=malloc(128*1024);ssize_t n=0;off_t total=0;
                            if(!buf)result=-1;
                            else {while((n=read(file,buf,128*1024))>0){if(write_all(data,buf,(size_t)n)){result=-1;break;}total+=n;}if(n<0||total!=st.st_size)result=-1;free(buf);}
                        } else result=send_listing(data,path);
                        close(data);
                    }
                    reply(c,result?"426 Transfer failed\r\n":"226 Transfer complete\r\n");
                }
                if(file>=0)close(file);close(passive);passive=-1;
            }
        }
        else reply(c,"502 Command not supported (read-only server)\r\n");
        pthread_mutex_unlock(&batch_mount_mutex);
    }
    if(passive>=0)close(passive);
}
static void *server_main(void *unused) {
    (void)unused;
    for(;;) {
        struct sockaddr_in peer;socklen_t len=sizeof(peer);
        int c=accept(listener,(struct sockaddr*)&peer,&len);if(c<0){if(errno==EINTR)continue;break;}
        pthread_mutex_lock(&batch_mount_mutex);int allowed=peer_enabled&&peer.sin_addr.s_addr==allowed_peer;pthread_mutex_unlock(&batch_mount_mutex);
        if(allowed)session(c,peer.sin_addr.s_addr);else reply(c,"530 Connect the mounter first\r\n");
        close(c);
    }
    return NULL;
}
int batch_ftp_start(void) {
    signal(SIGPIPE,SIG_IGN);
    listener=socket(AF_INET,SOCK_STREAM,0);if(listener<0)return -1;
    struct sockaddr_in addr={0};addr.sin_family=AF_INET;addr.sin_port=0;addr.sin_addr.s_addr=htonl(INADDR_ANY);
    socklen_t len=sizeof(addr);
    if(bind(listener,(struct sockaddr*)&addr,sizeof(addr))||listen(listener,4)||getsockname(listener,(struct sockaddr*)&addr,&len)){close(listener);listener=-1;return -1;}
    ftp_port=ntohs(addr.sin_port);
    if(pthread_create(&server_thread,NULL,server_main,NULL)){close(listener);listener=-1;return -1;}
    pthread_detach(server_thread);return ftp_port;
}
void batch_ftp_authorize(uint32_t peer,int enabled) {allowed_peer=peer;peer_enabled=enabled;}
int batch_ftp_port(void){return ftp_port;}
