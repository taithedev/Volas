param(
    [string]$GamePath = "",
    [switch]$Install,
    [string]$Framework = "",
    [string]$UnityEngineFacadePath = ""
)
$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "Build\Build-Volas.ps1") -GamePath $GamePath -Install:$Install -Framework $Framework -UnityEngineFacadePath $UnityEngineFacadePath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
