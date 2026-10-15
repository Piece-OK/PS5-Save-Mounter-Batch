#!/usr/bin/env bash
# Build only: this script never deploys a payload to a console.
set -euo pipefail
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
: "${PS5_PAYLOAD_SDK:?Set PS5_PAYLOAD_SDK to the installed pinned PS5 SDK}"
cc="${CLANG:-clang}"
ld="${LLD:-ld.lld}"
output="${1:-$root/mounter_payload/savemounter.elf}"
work="$(mktemp -d "${TMPDIR:-${XDG_CACHE_HOME:-$HOME/.cache}}/ps5-payload-build.XXXXXXXX")"
trap 'rm -rf -- "$work"' EXIT
for unit in main batch_ftp; do
    "$cc" -target x86_64-sie-ps5 -isystem "$PS5_PAYLOAD_SDK/target/include" \
        -fno-stack-protector -fno-plt -femulated-tls -fPIC -Wall -Werror \
        -c "$root/mounter_payload/$unit.c" -o "$work/$unit.o"
done
mkdir -p -- "$(dirname -- "$output")"
"$ld" -m elf_x86_64 -pie --eh-frame-hdr -z max-page-size=0x4000 \
    -T "$PS5_PAYLOAD_SDK/ldscripts/elf_x86_64.x" -L "$PS5_PAYLOAD_SDK/target/lib" \
    "$PS5_PAYLOAD_SDK/target/lib/crt1.o" "$work/main.o" "$work/batch_ftp.o" \
    -lc -lkernel_web -lSceLibcInternal -lSceNet -lSceUserService -lSceSystemService \
    -lSceFsInternalForVsh --hash-style=gnu -o "$output"
printf 'Built %s (not deployed)\n' "$output"
