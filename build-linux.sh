#!/usr/bin/env bash
# Build a self-contained Linux x86_64 AppImage; never upload or deploy anything.
set -euo pipefail
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
dotnet="${DOTNET:-dotnet}"
project="$root/linux/PS5SavefileBatcher.Linux.csproj"
output="${1:-$root/publish}"
mkdir -p -- "$output"
output="$(cd -- "$output" && pwd)"
cache="${PS5_BUILD_TOOLS:-${XDG_CACHE_HOME:-$HOME/.cache}/ps5-save-mounter-build}"
mkdir -p -- "$cache"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
if [[ $(uname -m) != x86_64 ]]; then
    echo 'This build script currently supports x86_64 Linux hosts only.' >&2; exit 1
fi
payload="${PAYLOAD_PATH:-$root/mounter_payload/savemounter.elf}"
if [[ ! -f "$payload" ]]; then
    echo 'Missing savemounter.elf: set PS5_PAYLOAD_SDK and run bash build-payload.sh first.' >&2; exit 1
fi
# Download official build tools only, pinning their reviewed SHA-256. A changed
# continuous-release asset fails closed; review/update both URL and hash together.
fetch_tool() {
    local name="$1" url="$2" hash="$3"
    if [[ ! -f "$cache/$name" ]]; then
        curl --fail --location --retry 2 --output "$cache/$name.download" "$url"
        printf '%s  %s\n' "$hash" "$cache/$name.download" | sha256sum --check --status
        mv -- "$cache/$name.download" "$cache/$name"
    fi
    printf '%s  %s\n' "$hash" "$cache/$name" | sha256sum --check --status
}
if [[ -z ${APPIMAGETOOL:-} ]]; then
    fetch_tool appimagetool-x86_64.AppImage \
        https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage \
        95cbe7cce9717fce90c484e34052ee7c7f1d7635b33c12525b4776826a7d29b6
    chmod +x "$cache/appimagetool-x86_64.AppImage"
    APPIMAGETOOL="$cache/appimagetool-x86_64.AppImage"
fi
if [[ -z ${APPIMAGE_RUNTIME:-} ]]; then
    fetch_tool runtime-x86_64 \
        https://github.com/AppImage/type2-runtime/releases/download/continuous/runtime-x86_64 \
        156f4bdbde9c52d01814600013e0a273f0118dc2de98975f3c8c63427ec79074
    APPIMAGE_RUNTIME="$cache/runtime-x86_64"
fi
work="$(mktemp -d "$output/.linux-build.XXXXXXXX")"
trap 'rm -rf -- "$work"' EXIT
appdir="$work/PS5-Savefile-Batcher.AppDir"
mkdir -p "$appdir/usr/bin" "$appdir/usr/share/doc/ps5-savefile-batcher" "$appdir/usr/share/applications" "$appdir/usr/share/icons/hicolor/scalable/apps"
"$dotnet" publish "$project" -c Release -r linux-x64 --self-contained true \
    -p:PublishSingleFile=false -p:PublishTrimmed=false -o "$appdir/usr/bin"
cp -- "$payload" "$appdir/usr/bin/savemounter.elf"
cp -- "$root/packaging/AppRun" "$appdir/AppRun"
chmod +x "$appdir/AppRun" "$appdir/usr/bin/PS5SavefileBatcher.Linux"
cp -- "$root/packaging/ps5-savefile-batcher.desktop" "$appdir/"
cp -- "$root/packaging/ps5-savefile-batcher.desktop" "$appdir/usr/share/applications/"
cp -- "$root/packaging/ps5-savefile-batcher.svg" "$appdir/"
cp -- "$root/packaging/ps5-savefile-batcher.svg" "$appdir/usr/share/icons/hicolor/scalable/apps/"
ln -s ps5-savefile-batcher.svg "$appdir/.DirIcon"
docs="$appdir/usr/share/doc/ps5-savefile-batcher"
for file in LICENSE README.md LINUX.md ADVANCED.md README.upstream.md SOURCES.md THIRD-PARTY-NOTICES.md; do
    cp -- "$root/$file" "$docs/"
done
cp -R -- "$root/Licenses" "$docs/"
version="$("$dotnet" msbuild "$project" -getProperty:Version -nologo)"
[[ "$version" =~ ^[a-zA-Z0-9.+-]+$ ]] || { echo 'Invalid version' >&2; exit 1; }
printf 'Matching source and build scripts:\nVersion: %s\nRelease: https://github.com/Piece-OK/PS5-Savefile-Batcher/releases/tag/v%s\nDownload the automatic Source code (zip) or Source code (tar.gz) from that release. Build scripts and upstream source snapshots are included.\n' "$version" "$version" > "$docs/SOURCE-CODE.txt"
# Preserve embedded package notices and a path-free resolved dependency inventory.
python3 "$root/packaging/collect-licenses.py" "$root/linux/obj/project.assets.json" "$docs/NuGet"
artifact="$output/PS5-Savefile-Batcher-linux-x86_64-v$version.AppImage"
ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "$APPIMAGETOOL" --no-appstream --comp zstd \
    --runtime-file "$APPIMAGE_RUNTIME" "$appdir" "$artifact"
chmod +x "$artifact"
(cd -- "$output" && sha256sum "$(basename -- "$artifact")" > "$(basename -- "$artifact").sha256")
printf '\nBuilt %s\nChecksum: %s.sha256\nNo upload performed.\n' "$artifact" "$artifact"
