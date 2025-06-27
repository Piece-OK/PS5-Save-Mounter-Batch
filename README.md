# Playstation 5 Save Mounter 1.4.1
This version is using patching so save data can be mounted without launching the game.
It solves the issue of failing to mount specific game saves, as they were already mounted on game boot.
Read the original readme below.

Supports PS5 FWs:
- 3.20, 4.03, 6.02, 7.40, 8.20, 10.01
	- If you have another FW version which is the same major version, it'll try to use the available offsets. This might now work, there's also a warning in the program.
- Currently can only mounts PS4 game saves, no PS5 game support at the moment.

Use [ps5debug](https://github.com/GoldHEN/ps5debug) at least version v1.0b4 for 3.xx-6.xx
Use this elf loader: https://github.com/ps5-payload-dev/elfldr

## Instructions (mouting existing saves)
1) Load ps5debug.
3) Load FTP.
4) Open the tool.
5) Enter the IP address of your PS5 and click 'Connect'.
7) Click 'Setup' and select your username.
6) Click 'Get Games' and select your game in the combobox.
8) Click 'Search'.
9) Select the save you want to mount in the combobox.
11) Click 'Mount'.
12) Your save is now mounted and accessible from FTP in `/mnt/pfs/` or `/mnt/sandbox/{title}/savedataX` (it's the same just a different dir)
13) After you're done copying/replacing files click 'Unmount'

**don't replace files in sce_sys directory, it is unnecessary and will probably corrupt your save**

---

[ChendoChap's PS4 Save Mounter README](https://github.com/ChendoChap/Playstation-4-Save-Mounter/blob/master/README.md)