#ifndef BATCH_FTP_H
#define BATCH_FTP_H
#include <stdint.h>
#include <pthread.h>
extern pthread_mutex_t batch_mount_mutex;
const char *batch_current_mount(void);
int batch_ftp_start(void);
int batch_ftp_port(void);
/* Caller holds batch_mount_mutex. */
void batch_ftp_authorize(uint32_t peer, int enabled);
#endif
