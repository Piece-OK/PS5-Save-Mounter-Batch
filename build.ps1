param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'publish\win-x64'),
    [string]$PayloadPath = (Join-Path $PSScriptRoot 'mounter_payload\savemounter.elf')
)
$ErrorActionPreference = 'Stop'
dotnet publish (Join-Path $PSScriptRoot 'PS5SavefileBatcher.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o $OutputPath
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
if (Test-Path -LiteralPath $PayloadPath) { Copy-Item -LiteralPath $PayloadPath -Destination (Join-Path $OutputPath 'savemounter.elf') }
else { throw 'Build savemounter.elf before publishing; the EXE and payload must be packaged together.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination $OutputPath
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $OutputPath 'README.md')
foreach ($doc in @('ADVANCED.md', 'README.upstream.md', 'SOURCES.md', 'THIRD-PARTY-NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $doc) -Destination $OutputPath
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Licenses') -Destination $OutputPath -Recurse -Force
$project = [xml](Get-Content -LiteralPath (Join-Path $PSScriptRoot 'PS5SavefileBatcher.csproj') -Raw)
$version = $project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
@"
Matching source code and build scripts for version ${version}:
https://github.com/Piece-OK/PS5-Savefile-Batcher/releases/tag/v$version
Download GitHub's automatic Source code (zip) or Source code (tar.gz) from that release. These include build scripts and upstream source snapshots and are not needed to run the app.
"@ | Set-Content -LiteralPath (Join-Path $OutputPath 'SOURCE-CODE.txt') -Encoding utf8
