# Playstation 5 Save Mounter 1.4
This version is using patching so save data can be mounted without launching the game.
It solves the issue of failing to mount specific game saves, as they were already mounted on game boot.
Read the original readme below.

Supports PS5 FWs:
- 3.20, 7.40, 8.20, 10.01
	- If you have another FW version which is the same major version, it'll try to use the available offsets. This might now work, there's also a warning in the program.
- Currently can only mounts PS4 game saves, no PS5 game support at the moment.

You can use [ps5debug](https://github.com/GoldHEN/ps5debug) at least version v1.0b4 for 3.xx-6.xx,  
or you can use a ps5debug build of [Dizz's version](https://github.com/DizzRL/ps5debug), which supports 1.xx-5.xx,  
You can use this elf loader: https://github.com/ps5-payload-dev/elfldr


## Instructions (mouting existing saves)
1) Load ps5debug.
3) Load FTP.
4) Open the tool.
5) Enter the IP address of your ps5 and click 'Connect'.
7) Click 'Setup' and select your username.
6) Click 'Get Games' and select your game in the combobox.
8) Click 'Search'.
9) Select the save you want to mount in the combobox.
11) Click 'Mount'.
12) Your save is now mounted and accessible from FTP in `/mnt/pfs/` or `/mnt/sandbox/{title}/savedataX` (it's the same just a different dir)
13) After you're done copying/replacing files click 'Unmount'

**don't replace files in sce_sys directory, it is unnecessary and will probably corrupt your save**

---

## Summary
This program allows you to mount save data as READ/WRITE
### You can
* Make decrypted copies of your saves
* Replace saves with modified ones
* Replace save files with someonelse's save files (share saves)
* Create new saves


### You can't
* Replace save files with an encrypted save
* Use this on unexploited consoles

### You need
* To make sure you're using a recent ps4debug version, bin of the latest ps4debug (as of 11/14) is included in the download
* To be able to run .net framework 2.0 executables (even windows 98 is able to do this)
## Prerequisites
* PS4 5.05
* FTP Client (eg filezilla, ...)
## Instructions (mouting existing saves)
1) Load [ps4debug](https://github.com/xemio/ps4debug)
2) Start a game
3) Load [FTP](https://github.com/xvortex/ps4-ftp-vtx)
4) Open the tool
5) Enter the ip of your ps4 and click 'Connect'
6) Click 'Get Processes' and select your game in the combobox
7) Click 'Setup'
8) Click 'Search'
9) Select the save you want to mount in the combobox
10) Select the mount permission in the combobox (default is READ ONLY)
11) Click 'Mount'
12) Your save is now mounted and accessible from ftp in /mnt/pfs/ & in /mnt/sandbox/{title}/savedataX (it's the same just a different dir)
13) After you're done copying/replacing files click 'Unmount'

**don't replace files in sce_sys directory, it is unnecessary and will probably corrupt your save**



**Some games use another save format, they have an sce_ prefix in their name (saves can be found in /user/home/{userid}/savedata/{titleid} check the name there). they won't show up as search results**  
**This can probably be patched but I was too lazy** 



Here's a workaround
1) go to /user/home/{userid}/savedata/{titleid}
2) make a copy of the sce save: 2 files, the bin file(96KB), the sdimg file
3) rename them  
	"sce_sdmemory.bin" -> "temp.bin"  
    "sdimg_sce_sdmemory" -> "sdimg_temp"
4) go to /system_data/savedata/{userid}/db/user and download the database.db file
5) open it with an [sqlite editor](https://sqlitebrowser.org/)  
6) add a new record in the savedata table
7) fill in the data and you're done
8) replace the original database with the newer one
9) Click 'Search' again, it should now add a temp entry to the combobox
10) proceed as usual
11) go to /user/home/{userid}/savedata/{titleid}
	* delete the original sce_sdmemory.bin and sdimg_sce_sdmemory
	* rename temp.bin to sce_sdmemory.bin and temp to sdimg_sce_sdmemory
12) replace the modified database with the original one
13) you're done

## Authors
- Aida
- [ChendoChap](https://github.com/ChendoChap)
## Acknowledgments
* [golden](https://github.com/xemio)
