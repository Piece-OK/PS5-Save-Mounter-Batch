# PS5 Save Mounter Batch

## What it does

- Backs up saves from a jailbroken PS5 to your Windows or Linux PC.
- Windows: native Windows Forms app. Linux: self-contained x86_64 AppImage, without Wine or a separate .NET installation.
- Lets you choose individual saves, all saves from a game, or all saves belonging to a console user.
- Looks up game titles online to make your save library and backup folders easier to identify.

## How it works

The app sends a small program called `savemounter.elf` to your PS5. This payload finds your saves and makes them accessible for copying.

PS5 saves are normally encrypted. “Mounting” a save lets the console open it so the app can download its decrypted files. The app handles each selected save automatically: mount it, download it, unmount it, then move on to the next one.

## Getting started

1. **[Download the release](https://github.com/Piece-OK/PS5-Save-Mounter-Batch/releases).** Choose the Windows x64 ZIP or Linux x86_64 AppImage. Beta releases are marked **Pre-release**.
2. **Open the app.** On Windows, extract the ZIP and open `PS5 Save Mounter Batch.exe`; keep `savemounter.elf` beside it. On Linux, make the AppImage executable and launch it; the payload is bundled. See [LINUX.md](LINUX.md) for requirements and FUSE-free launching.
3. **Connect to your jailbroken PS5.** Start elfldr on the PS5, enter your PS5’s IP address in the app, and click **Connect**. Leave payload deployment enabled to send `savemounter.elf` automatically. No separate FTP payload is needed; leave the Linux FTP override at **0 (embedded)** unless you intentionally use a compatible external FTP server.
4. **Choose your saves and preferences.** Select a console user, load the game library, choose your backup folder, and check the games or individual saves you want to back up.
5. **Click Dump selected saves.** Windows defaults to `Backups` beside the EXE; Linux defaults to `PS5-Backups` beside the AppImage. On Linux, **Parent Folder** restores this default.

To back up another console user’s saves, select that user and repeat steps 4–5.

Releases contain no preconfigured console IP, custom FTP port, personal settings, cached library or save backups. Existing settings on your own PC are retained when updating; clear them yourself if you want a fresh setup. Restoring saves is not supported.

## TODO

- [ ] Restore backed-up saves to the console. Restoring is not supported yet.
- [x] Add Linux support
- [ ] Maybe add a feature to dump the medialibrary content of the PS5
- [ ] Add Support PS4
