param(
    [Parameter(Mandatory=$true)][string]$SdkPath,
    [Parameter(Mandatory=$true)][string]$LlvmBin,
    [string]$OutputPath = (Join-Path $PSScriptRoot 'mounter_payload\savemounter.elf')
)
$ErrorActionPreference = 'Stop'
$SdkPath = (Resolve-Path -LiteralPath $SdkPath).Path
$LlvmBin = (Resolve-Path -LiteralPath $LlvmBin).Path
$payloadSource = Join-Path $PSScriptRoot 'mounter_payload'
$clang = Join-Path $LlvmBin 'clang.exe'
$linker = Join-Path $LlvmBin 'ld.lld.exe'
foreach ($unit in @('main', 'batch_ftp')) {
    & $clang -target x86_64-sie-ps5 -isystem (Join-Path $SdkPath 'target\include') -fno-stack-protector -fno-plt -femulated-tls -fPIC -Wall -Werror -c (Join-Path $payloadSource "$unit.c") -o (Join-Path $payloadSource "$unit.o")
    if ($LASTEXITCODE -ne 0) { throw "Compile failed: $unit" }
}
& $linker -m elf_x86_64 -pie --eh-frame-hdr -z max-page-size=0x4000 -T (Join-Path $SdkPath 'ldscripts\elf_x86_64.x') -L (Join-Path $SdkPath 'target\lib') (Join-Path $SdkPath 'target\lib\crt1.o') (Join-Path $payloadSource 'main.o') (Join-Path $payloadSource 'batch_ftp.o') -lc -lkernel_web -lSceLibcInternal -lSceNet -lSceUserService -lSceSystemService -lSceFsInternalForVsh --hash-style=gnu -o $OutputPath
if ($LASTEXITCODE -ne 0) { throw 'ELF linking failed' }
Write-Output "Built $OutputPath using the pinned PS5 SDK and LLVM."
