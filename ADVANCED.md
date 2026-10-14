# Advanced guide

For the quick setup steps, see [README.md](README.md).

## Connections and progress

The mounter protocol uses TCP 9090. The embedded FTP control listener requests an available port from the PS5 and reports it to the app. Passive data connections use separate available ports. This avoids another fixed port that could conflict with common payloads; all traffic does not share port 9090. FTP credentials and port are handled automatically. The server accepts the active mounter client's IP and exposes only the current mounted save, using ordinary unencrypted FTP on your local network.

The ELF loader port sits beside the deployment checkbox and is disabled when deployment is unchecked. Mounter port 9090 is fixed in the bundled payload and has no GUI selector. `MounterPort` in settings.json changes the client's target port for a custom compatible payload. It does not reconfigure the bundled payload.

`AlternativeFTPPort` is optional: null or 0 uses embedded FTP; a value such as 2121 uses an already-running external FTP server on the same console instead. The external server must expose the same mounted /mnt/pfs path, support passive FTP and accept anonymous access. The app still mounts and unmounts through Save Mounter. Failed transfer tests can be retried with the connection button. Close the app before editing settings.json.

```json
{
  "Host": "192.168.0.100",
  "SendPayload": true,
  "MounterPort": 9090,
  "LoaderPort": 9021,
  "AlternativeFTPPort": null,
  "IncludeGameTitle": false,
  "OnlineGameTitles": true,
  "Destination": "Backups"
}
```

Elapsed time updates during a dump. Remaining time is estimated from completed saves and may change substantially when save sizes differ. During cancellation, the bar animates while transfer/unmount cleanup finishes and the elapsed clock continues. Afterwards the display says Cancelled or Failed, retains the final elapsed time and last ETA, and preserves the completed-save count. A stopped batch has no active remaining-time estimate.

## Game title lookup

**Look up game titles online** fetches the maintained [KytyPS5 title database](https://github.com/KytyPS5/kytyps5.github.io/blob/main/src/data/games.json), including regional aliases, and the [PS5 Homebrew Catalog index](https://github.com/blackbearreloaded/ps5-homebrew-catalog/blob/main/docs/api.md). The latter publishes a bulk JSON index at https://homebrew.page/api/v1/index.json. Only title-ID/name facts are used, with homebrew mappings taking precedence for catalogued homebrew IDs. Attribution is shown in the GUI and log. No apps, binaries, icons, emulator or website code are downloaded or incorporated.

For PS4 entries still missing a usable console/save title, online lookup also queries the JSON endpoint used by the official [shadPS4 compatibility page](https://shadps4.net/compatibility/). Its compatibility release JSON contains status/issue references rather than game names; the website search response includes the names. Only exact CUSA matches are accepted. Coverage is limited to reported games; unknown titles remain blank.

KytyPS5 and Homebrew-catalog requests fetch public lists. shadPS4 requests send the missing CUSA title ID as a search query. Your console address, user names, save names and save contents are not sent. Each source has a seven-day cache in Cache. shadPS4 caches both names and unknown results, limits each response to 1 MiB, each request to twelve seconds, and a refresh to thirty seconds. Service errors retain available cached/internal names. Generic labels such as Gespeicherte Daten and Saved Data are ignored. Unresolved titles are blank; their Title ID remains visible and the backup folder retains its ID-only name.

Unchecking online lookup immediately restores the loaded library's console/save titles or blanks without losing checked saves or expansion state. Checking it fetches/uses the cached lists and reapplies online titles. Future backup names use the refreshed title; existing backups are not renamed.

## Files and errors

Each run creates a unique folder:

```
Backups/2026_10_13-224218-[Console_User]/PPSA21567-ASTRO_BOT/<save-name>/
```

Each run contains one console user's selected saves; there is no extra hexadecimal user-ID folder. Same-second runs get a numeric suffix to avoid overwriting. User/title labels are sanitized for Windows. Earlier backups are not renamed or overwritten. `report.json` retains the user name, original user ID, title IDs, completion, mount paths, unmount confirmation, sizes and local SHA-256 hashes. Lengths are checked against directory listings; hashes describe downloaded bytes, not a server-supplied checksum or a restore test.

Error logging is always enabled. **Open logs** opens `Logs/session_*.log` beside the EXE. Each run also has `activity.log` and `report.json`. Incomplete downloads retain `.partial` folders. Final names are assigned only after download and successful unmount. Files get up to three transfer attempts; transient Windows folder-promotion failures are retried without re-downloading. Persistent errors stop the queue. Automatic partial-download resume is not implemented.

**Cancel safely** stops transfers and attempts unmount cleanup. Closing during a batch requests cancellation; wait for cleanup, then close again. A payload command can take up to two minutes to time out. Check `UnmountConfirmed` and the logs after an interruption. Force-killing the program or console power loss cannot guarantee cleanup.

These are mounted/decrypted save-folder backups. Restore and resigning are outside this app's functions. Keep existing backups and follow upstream restore guidance, especially its sce_sys warning.

## Portable build and troubleshooting

The Windows x64 EXE includes .NET 10 and its native dependencies: no separate .NET 8.0.31 or .NET 10 runtime installation is required. First launch extracts native components to .NET's per-user temporary cache. Keep the matching payload beside it. `settings.json` stores host, mounter/loader ports and destination; the default destination is stored as relative `Backups`.

- **Connection refused:** verify the console address and that elfldr is running on the selected port.
- **BATCH/2 missing:** this app requires the combined Save Mounter payload. Exit the original mounter and deploy savemounter.elf before connecting.
- **Payload already running:** uncheck deployment to connect to an existing compatible payload. Disconnecting through this app stops its payload.
- **Missing title:** unresolved game titles stay blank; the Title ID remains visible. Enable online lookup to try additional title sources.
- **Transfer error:** inspect the session log and run report. Unsafe Windows names, case collisions, symlinks and unsupported file types cause explicit failures.
- **Rollback:** the personal package preserves the original separate-FTP build in `PreviousVersions/0.1` and the user-tested embedded-FTP build in `PreviousVersions/0.2.0`.

## Build from source

Install the .NET 10 SDK, LLVM 18.1.8 and PS5 payload SDK v0.44. SDK and ftpsrv source snapshots and licenses are included in `third_party`; see SOURCES.md for exact pins and upstream links.

```powershell
./build-payload.ps1 -SdkPath 'C:\path\ps5-payload-sdk' -LlvmBin 'C:\path\llvm\bin'
dotnet run --project tests/Dumper.Tests.csproj -c Release
./build.ps1
```

The script publishes to `publish/win-x64`. POSIX payload build: set PS5_PAYLOAD_SDK and run `make -C mounter_payload`. Retained Main.cs/Main.Designer.cs/Main.resx are upstream references excluded from the desktop build. DumperForm.cs implements the UI; DumpEngine.cs implements transfers and queue handling.

## License and provenance

LICENSE retains upstream GPL version 3. GPLv3-or-later FTP code and SDK components are used under GPLv3, with file-specific licenses preserved. See SOURCES.md, THIRD-PARTY-NOTICES.md and README.upstream.md. Binary redistribution must include applicable notices and provide matching corresponding source and necessary build scripts. Credits alone are insufficient.

Repository: [Piece-OK/PS5-Save-Mounter-Batch](https://github.com/Piece-OK/PS5-Save-Mounter-Batch), a GitHub fork of the original project. Download portable development builds from [Releases](https://github.com/Piece-OK/PS5-Save-Mounter-Batch/releases). Matching corresponding-source.zip is a separate download on the same release page; it includes the source, build scripts and upstream source snapshots. It is not needed to run the app. Keep Backups, Cache, Logs and personal settings out of public uploads.

Portable builds compress the bundled .NET assemblies to reduce the extracted application size. The full runtime remains included, so no separate .NET installation is needed. Startup decompresses assemblies into memory; native runtime files still use the normal .NET temporary extraction cache. No unsupported Windows Forms trimming is enabled.
