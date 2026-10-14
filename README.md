# PS5 Save Mounter Batch

Back up PS4 and PS5 game saves from your PS5 to your Windows PC.

Choose individual saves, every save from a game, or all game saves for a console user. The app opens each save, downloads it, closes it, and moves on automatically.

**[Download the Windows app](https://github.com/Piece-OK/PS5-Save-Mounter-Batch/releases)**

## What you need

- A Windows 64-bit PC. No separate .NET runtime installation is needed.
- A jailbroken PS5 with [elfldr](https://github.com/ps5-payload-dev/elfldr) running.
- Your PC and PS5 connected to the same local network.

## How to use it

1. **Download and extract** `PS5-Save-Mounter-Batch-win-x64.zip`. Keep `PS5 Save Mounter Batch.exe` and `savemounter.elf` in the same folder.
2. **Prepare your PS5.** Close any running game and other Save Mounter apps. Start **elfldr on the PS5** using your usual payload loader.
3. **Open the Windows app** and enter your PS5's IP address.
4. Leave **Send SAVEMOUNTER.ELF payload** checked, then click **Connect**. The app sends `savemounter.elf` automatically and checks the download connection. You do not need to copy it to the PS5 yourself or start a separate FTP server. Leave **ELF loader port** at `9021` unless your loader uses another port.
5. **Choose a console user**, then click **Load game library**.
6. **Choose where to save your backups.** The default is the `Backups` folder beside the app. Click **Browse…** to choose somewhere else.
7. **Choose your saves.** Check a game to select all its saves. Click the game row to expand it and choose individual saves. **Select all** selects every game save for the chosen user.
8. Click **Dump selected saves** and wait for it to finish. Use **Open backup folder** to see the result, then **Disconnect** when you are done. Repeat with another console user if needed.

Each backup run gets a new folder named with the date, time, and console user. Previous backups are kept.

## Useful options

- **Include game title in backup folder names:** use names like `PPSA21567-ASTRO_BOT` instead of just `PPSA21567`.
- **Look up game titles online:** find more names for PS4 games, PS5 games, and homebrew. Uncheck it to use only names from your console. Unknown titles stay blank.
- **Sort the list:** click the game name, Title ID, Platform, or save count header. Click again to reverse the order.
- **Cancel safely:** stop a backup and wait for cleanup to finish before closing the app.

## If something goes wrong

If the app cannot connect, check the PS5 IP address and make sure elfldr is running.

Click **Open logs** for error details. Incomplete backups stay in folders ending in `.partial`; completed saves are kept. Start a new run to try again.

This app creates backups. Restoring saves is a separate process.

---

[Advanced settings and building from source](ADVANCED.md) · [Credits and title sources](SOURCES.md) · [License](LICENSE)
