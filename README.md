# PS5 Savefile Batcher

## What it does

- Backs up saves from a jailbroken PS5 to your Windows or Linux PC.
- Windows: simply run the .exe | Linux: simply run the AppImage (See [LINUX.md](LINUX.md) for requirements and FUSE-free launching).
- You can choose individual saves, all saves from a game, or all saves belonging to a console user.
- Conveniently looks up game titles online to make your save library and backup folders easier to identify.

## How it works

The app sends a payload called `savemounter.elf` to your jailbroken PS5. This payload finds your save folders and makes them accessible for copying by mounting them.

PS5 saves are normally encrypted. “Mounting” a save lets the console open it so the app can download its decrypted files. The app handles each selected save automatically: it mounts, it downloads it, it unmounts it, then it moves on to the next one.

## Getting started

1. **[Download the release](https://github.com/Piece-OK/PS5-Savefile-Batcher/releases).** Choose the Windows x64 ZIP or Linux x86_64 AppImage. Beta releases are marked **Pre-release**.
2. **Open the app.** On Windows, extract the ZIP and open `PS5 Savefile Batcher.exe`. On Linux, launch the AppImage; the payload is bundled.
3. **Connect to your jailbroken PS5.** Start elfldr on the PS5, enter your PS5’s IP address in the app, and click **Connect**. Leave payload deployment enabled to send `savemounter.elf` automatically. No separate FTP payload is needed; leave the Linux FTP override at **0 (embedded)** unless you intentionally use a compatible external FTP server.
4. **Choose your saves and preferences.** Select a console user, load the game library, choose your backup folder, and check the games or individual saves you want to back up.
5. **Click Dump selected saves.** Windows defaults to `Backups` beside the EXE; Linux defaults to `PS5-Backups` beside the AppImage. On Linux, **Parent Folder** restores this default.

To back up another console user’s saves, select that user and repeat steps 4–5.

Tested on firmware 13.60 and 4.03.

## TODO

- [ ] Restore backed-up saves to the console. Restoring is not supported yet.
- [x] Add Linux support
- [ ] Add support for PS4 systems

<img width="800" alt="Screenshot 2026-10-16 103112" src="https://github.com/user-attachments/assets/00acea1a-6ed1-427f-acaf-edbc7e8e6063" />


<img width="800" alt="Screenshot 2026-10-16 103657" src="https://github.com/user-attachments/assets/38b1763e-4884-4bb2-b208-6779d83ff2b8" />
