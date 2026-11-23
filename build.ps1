param(
    [string]$GamePath = "",
    [switch]$Install
)

$ErrorActionPreference = "Stop"
if (-not $GamePath) { $GamePath = $env:VOLAS_GAME_DIR }
if (-not $GamePath) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Steam\steamapps\common\Gorilla Tag",
        "$env:ProgramFiles\Steam\steamapps\common\Gorilla Tag",
        "D:\SteamLibrary\steamapps\common\Gorilla Tag",
        "E:\SteamLibrary\steamapps\common\Gorilla Tag"
    )
    $GamePath = $candidates | Where-Object { Test-Path (Join-Path $_ "Gorilla Tag_Data\Managed") } | Select-Object -First 1
}
if (-not $GamePath -or -not (Test-Path (Join-Path $GamePath "Gorilla Tag_Data\Managed"))) {
    throw "Cannot find Gorilla Tag. Run: .\build.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Gorilla Tag' -Install"
}

if (-not (Test-Path (Join-Path $GamePath "BepInEx\core\BepInEx.dll"))) {
    throw "BepInEx 5 Mono is missing. Install BepInEx 5 in Gorilla Tag before compiling."
}

Write-Host "Building Volas for: $GamePath"
$args = @(
    "build",
    (Join-Path $PSScriptRoot "Volas.csproj"),
    "-c", "Release",
    "-p:GorillaTagDir=$GamePath"
)
if ($Install) { $args += "-p:InstallToGame=true" }
& dotnet @args
if ($LASTEXITCODE -ne 0) { throw "Volas build failed." }
Write-Host "Volas.dll is at bin\Release\net472\Volas.dll"
if ($Install) { Write-Host "Installed to: $GamePath\BepInEx\plugins\Volas.dll" }
