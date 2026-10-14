param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'publish\win-x64'),
    [string]$PayloadPath = (Join-Path $PSScriptRoot 'mounter_payload\savemounter.elf')
)
$ErrorActionPreference = 'Stop'
dotnet publish (Join-Path $PSScriptRoot 'PS4Saves.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o $OutputPath
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
if (Test-Path -LiteralPath $PayloadPath) { Copy-Item -LiteralPath $PayloadPath -Destination (Join-Path $OutputPath 'savemounter.elf') }
else { throw 'Build savemounter.elf before publishing; the EXE and payload must be packaged together.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination $OutputPath
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.dumper.md') -Destination (Join-Path $OutputPath 'README.md')
foreach ($doc in @('README.upstream.md', 'SOURCES.md', 'THIRD-PARTY-NOTICES.md', 'VALIDATION.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $doc) -Destination $OutputPath
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Licenses') -Destination $OutputPath -Recurse -Force
