# PS5 Save Mounter Batch

## What it does

- Backs up saves from a jailbroken PS5 to your Windows PC.
- Lets you choose individual saves, all saves from a game, or all saves belonging to a console user.
- Looks up game titles online to make your save library and backup folders easier to identify.

## How it works

The app sends a small program called `savemounter.elf` to your PS5. This payload finds your saves and makes them accessible for copying.

PS5 saves are normally encrypted. “Mounting” a save lets the console open it so the app can download its decrypted files. The app handles each selected save automatically: mount it, download it, unmount it, then move on to the next one.

## Getting started

1. **[Download the release](https://github.com/Piece-OK/PS5-Save-Mounter-Batch/releases).** Choose the Windows ZIP. Windows 64-bit is required; no separate .NET installation is needed.
2. **Extract the ZIP and open `PS5 Save Mounter Batch.exe`.** Keep `savemounter.elf` beside the EXE.
3. **Connect to your jailbroken PS5.** Start elfldr on the PS5, enter your PS5’s IP address in the app, and click **Connect**. Leave **Send SAVEMOUNTER.ELF payload** checked to send the payload automatically.
4. **Choose your saves and preferences.** Select a console user, click **Load game library**, choose your download folder, and check the games or individual saves you want to back up.
5. **Click Dump selected saves.** Backups go into the app’s `Backups` folder unless you choose another location.

To back up another console user’s saves, select that user and repeat steps 4–5.

## Planned features

- Restore backed-up saves to the console. Restoring is not supported yet.
