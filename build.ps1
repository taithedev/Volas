param(
    [string]$GamePath = "",
    [switch]$Install
)
# Backward-compatible entry point. The implementation lives in Build/.
$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "Build\Build-Volas.ps1") -GamePath $GamePath -Install:$Install
