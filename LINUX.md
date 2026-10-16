# Linux AppImage

The Linux application uses Avalonia for its desktop UI and the same C# protocol,
metadata, FTP and backup engine as the Windows application. The Windows Forms
application remains a separate build; Wine is not required.

## Run

Download `PS5-Savefile-Batcher-linux-x86_64-v<version>.AppImage`, make it
executable in your file manager (Properties → Permissions) or run:

```bash
chmod +x PS5-Savefile-Batcher-linux-x86_64-v*.AppImage
./PS5-Savefile-Batcher-linux-x86_64-v<version>.AppImage
```

Use an x86_64 Linux desktop with X11 or XWayland, fontconfig, FreeType, ICU,
OpenSSL and the standard X11 libraries. The .NET runtime, Avalonia, SQLite and
`savemounter.elf` are bundled. No separate .NET installation is required. ARM64
and native Wayland-only sessions are not targets of this first build.

If the system cannot mount AppImages using FUSE, run without FUSE:

```bash
./PS5-Savefile-Batcher-linux-x86_64-v<version>.AppImage --appimage-extract-and-run
```

The console requirements are unchanged: a compatible jailbroken PS5, elfldr,
and a reachable console IP address. Connecting with **Deploy savemounter.elf**
checked sends the bundled payload. Do not run two mounter clients
against the console simultaneously. Restore is still not implemented.

## Writable data

The AppImage is read-only. Settings, title caches and session logs must live in
XDG user directories, not alongside the bundled executable. The Linux GUI
shows the default backup destination and allows changing it with **Browse**.
Moving or updating the AppImage does not delete your settings or backups.
Do not package your user-data folders in a release.

Default locations (absolute XDG overrides are honored):

- Settings: `~/.config/ps5-savefile-batcher/settings.json`
- Title caches: `~/.cache/ps5-savefile-batcher/`
- Logs: `~/.local/state/ps5-savefile-batcher/logs/`
- Backups: `PS5-Backups/` beside the actual AppImage. For example, an AppImage
  in `/home/example/Downloads/` defaults to `/home/example/Downloads/PS5-Backups/`.
  This uses the AppImage runtime's absolute `APPIMAGE` path, not the current
  working directory or temporary extraction mount. Unpackaged builds default
  beside their executable.

The default follows the AppImage when it is moved. Explicit custom destinations
are retained. The earlier `~/PS5 Backups/` default is migrated on load; existing
backup files are never moved or deleted. **Parent Folder** resets the
destination. The folder is created only when needed, so an AppImage in a
read-only directory can still start and let you choose a writable destination.

The library has aligned columns for game/save name, title ID, PS4/PS5 platform
and save count/selection. Click a column header to sort; click it again to
reverse direction. Click a game name to expand its individual saves. Sorting
preserves both save selection and expanded groups. Visible source buttons for
`online title lookup from: kytyPS5 | Homebrew-Catalog | ShadPS4` open the same
source URLs as Windows. The checkbox enables/disables online lookup for all
three sources; the linked names open their source pages.

The bundled payload listens on the fixed mounter port 9090, so the Linux GUI
does not offer a mounter-port setting and always targets that port. The ELF
loader port remains configurable. **FTP override (0 = embedded)** is retained
for compatible external FTP servers. A configuration file does exist:
`~/.config/ps5-savefile-batcher/settings.json` (or the corresponding
`XDG_CONFIG_HOME` location). Close the application before editing it manually.

On both platforms the progress bar turns green only after the complete batch
has finished successfully (including cleanup). Failed, cancelled, running or
empty batches do not show success. A new batch resets the completion color.

Cancellation waits for the outstanding console request and unmount cleanup.
Console requests can take up to 120 seconds. Do not force-kill the application
or stop the payload during cleanup. Completed backups and partial data are
preserved after interruption; reconnect before retrying.

Headless diagnostics are available with `--help`, `--version`, `--inventory`
and `--dump`. Inventory/dump explicitly connect to the supplied host and may
deploy the payload according to your saved settings; they are not offline tests.

Online title lookup is optional and follows the existing privacy behavior
explained in `SOURCES.md`. No backup contents are sent to title providers.

## Build locally

Prerequisites: Linux x86_64, .NET SDK 10, Bash, curl, sha256sum, Python 3 and a
C compiler/linker for the payload (LLVM/Clang and LLD with the pinned PS5 SDK).
Builds only download dependencies/tools; neither script deploys or uploads.

```bash
export PS5_PAYLOAD_SDK=/absolute/path/to/ps5-payload-sdk
bash build-payload.sh

bash build-linux.sh
```

`build-payload.sh` compiles the repository's actual payload sources using the
SDK headers, CRT, stubs and linker script. The bundled SDK source ZIP preserves
symlink metadata: extract it with a symlink-aware ZIP tool. If reconstructing
it with Python, restore the archive's symlinks, rather than treating their
link targets as C header contents. Upstream shell scripts also need POSIX line
endings when copied from a Windows archive. Our payload build script calls
Clang/LLD directly, avoiding those wrapper-script line endings.

`build-linux.sh` publishes a self-contained, untrimmed application and creates
an AppImage plus a SHA-256 file in `publish/`. It pins the official AppImage
build tool and runtime by SHA-256. The upstream `continuous` URLs can change:
a checksum mismatch is a hard error, not a reason to disable verification.
Review and update both pins together for a future tooling update.

Overrides: `DOTNET` for the SDK executable, `PAYLOAD_PATH` for a separately
built matching ELF, `PS5_BUILD_TOOLS` for the build-tool cache, and
`APPIMAGETOOL` / `APPIMAGE_RUNTIME` for explicitly supplied reviewed tools.
The optional first positional argument is the output directory. All paths may
contain spaces. The script never executes the bundled console payload locally.

## Desktop verification

The Linux GUI uses Avalonia 12.1.4 and its resolved Tmds.DBus.Protocol 0.94.1.
An earlier Avalonia 11.3.12 build emitted a D-Bus `TaskCanceledException` on
KDE when shutting down. Updating the Linux-only dependencies removed that
failure in repeated real-desktop smoke and window-manager close checks,
including closes shortly after the window appeared. D-Bus integration remains
enabled; exceptions are not swallowed. Automated smoke checks require both a
successful result and empty stderr rather than trusting exit code 0 alone.

## Release packages

The release workflow builds the versioned Windows ZIP and Linux AppImage.
Matching source, build scripts and upstream source snapshots are included in
GitHub's automatic **Source code (zip)** and **Source code (tar.gz)** downloads
on the same release page. Checksums are generated for local build
verification but are not attached as separate release downloads. New tagged
builds are staged as draft pre-releases for review before publication.
Nothing is uploaded by a local build. Do not publish the Linux binary without
the matching source archive and license notices. Personal settings, console
addresses, custom FTP overrides, logs, caches and backups are never included.

Development test projects and detailed validation notes are maintained locally,
not included in public source archives. Release CI still builds both platforms
and verifies the packaged applications' GUI/SQLite smoke checks. A successful
GUI startup does not establish that a real PS5 session succeeds on Linux.
Restore/resigning, power-loss recovery, ARM64 and native Wayland-only sessions
are not verified.
