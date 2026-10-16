# Third-party notices

- Original Save Mounter and this derivative: GNU GPL version 3; see LICENSE and README.upstream.md for upstream credits. Upstream: https://github.com/n0llptr/Playstation-5-Save-Mounter
- Microsoft .NET runtime and Windows Desktop runtime 10.0.12: MIT and bundled third-party notices. Copies of the runtime license and notices are in Licenses in the desktop package. Source: https://github.com/dotnet/runtime and https://github.com/dotnet/winforms
- Microsoft.Data.Sqlite / Microsoft.Data.Sqlite.Core 10.0.12: MIT. Source: https://github.com/dotnet/efcore
- SQLitePCLRaw components 2.1.12: Apache-2.0. Source: https://github.com/ericsink/SQLitePCL.raw
- SQLite native library: public domain. https://www.sqlite.org/copyright.html

- Embedded FTP adapted PASV logic: ps5-payload-dev/ftpsrv, GPL-3.0-or-later, copyright John Törnblom. Exact source snapshot and license are included in Source/third_party; modified notices are in Source/mounter_payload/batch_ftp.c.
- PS5 payload SDK v0.44: GPL-3.0-or-later with file-specific licenses, including BSD headers. Exact source snapshot and license are included in Source/third_party. Source: https://github.com/ps5-payload-dev/sdk
- elfldr v0.26: GPL-3.0-or-later, external prerequisite only, not bundled. Source: https://github.com/ps5-payload-dev/elfldr

- Game-title facts: fetched at runtime from KytyPS5's published src/data/games.json, https://github.com/KytyPS5/kytyps5.github.io. Website/emulator code and assets are not incorporated; source attribution is shown in the app. The website code's GPL-2.0 license is not used to relicense this project. See SOURCES.md for data-only use and caching.
- Homebrew-title facts: fetched from https://homebrew.page/api/v1/index.json, published by blackbearreloaded/ps5-homebrew-catalog (repository license GPL-3.0). Only titleid/name fields are consumed by independently written code. No app binaries, catalog code or assets are incorporated. Source attribution appears in the GUI and log; see SOURCES.md.
- PS4-title facts: exact CUSA names queried at runtime from the official shadPS4 compatibility website, https://shadps4.net/compatibility/. No emulator/site code, artwork or compatibility reports are incorporated. The website reserves its rights; a factual ID/name parser is independently implemented. Queries, caching and attribution are described in SOURCES.md and ADVANCED.md.

Dependency versions are recorded in PS5SavefileBatcher.csproj, linux/PS5SavefileBatcher.Linux.csproj and the NuGet restore graph.

## Linux package

- Avalonia 12.1.4: MIT. Source: https://github.com/AvaloniaUI/Avalonia
- Tmds.DBus.Protocol 0.94.1: MIT. Source: https://github.com/tmds/Tmds.DBus
- SkiaSharp, HarfBuzzSharp and native dependencies: upstream license metadata and embedded notices are preserved per package under `usr/share/doc/ps5-savefile-batcher/NuGet/` inside the AppImage. Its `packages.json` records the exact resolved versions without local build paths.
- AppImage type-2 runtime: MIT with statically linked musl, libfuse, squashfuse, zstd and zlib components under their own terms. See `Licenses/AppImage-runtime-LICENSE.txt` for the upstream notice and component license links. Source: https://github.com/AppImage/type2-runtime
- The self-contained Linux runtime in the locally verified AppImage is .NET 10.0.12; the SDK used for build/tests is 10.0.402. Runtime version is pinned in the Linux project and reported by `--smoke`; tests run on the SDK's installed runtime.

The supplied development-build savemounter.elf is built from the modified payload source in Source/mounter_payload. GPLv3-or-later components are incorporated under GPLv3, compatible with the original project's GPLv3 license. See SOURCES.md for exact references, adaptations and build instructions.
