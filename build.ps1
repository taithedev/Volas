param(
    [string]$GamePath = "",
    [switch]$Install,
    [string]$Framework = ""
)
$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "Build\Build-Volas.ps1") -GamePath $GamePath -Install:$Install -Framework $Framework
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
