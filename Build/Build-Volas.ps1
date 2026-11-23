param(
    [string]$GamePath = "",
    [switch]$Install,
    [string]$Framework = ""
)
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
if ($Framework -and $Framework -notin @("net472", "netstandard2.1")) {
    throw "Unsupported framework: $Framework. Use net472 or netstandard2.1."
}
function Test-GorillaTag([string]$path) {
    return ($path -and (Test-Path -LiteralPath (Join-Path $path "Gorilla Tag_Data\Managed\UnityEngine.CoreModule.dll")))
}
function Find-GorillaTag {
    $steamRoots = New-Object 'System.Collections.Generic.List[string]'
    foreach ($path in @([Environment]::GetFolderPath("ProgramFilesX86"), [Environment]::GetFolderPath("ProgramFiles"))) {
        if ($path) { $steamRoots.Add((Join-Path $path "Steam")) }
    }
    foreach ($drive in Get-PSDrive -PSProvider FileSystem) {
        foreach ($folder in @("SteamLibrary", "Steam")) { $steamRoots.Add((Join-Path $drive.Root $folder)) }
    }
    $allRoots = New-Object 'System.Collections.Generic.List[string]'
    foreach ($root in $steamRoots) {
        $allRoots.Add($root)
        $vdf = Join-Path $root "steamapps\libraryfolders.vdf"
        if (Test-Path -LiteralPath $vdf) {
            $content = Get-Content -LiteralPath $vdf -Raw
            foreach ($match in [regex]::Matches($content, '"path"\s*"([^"]+)"')) {
                $allRoots.Add($match.Groups[1].Value.Replace('\\', '\'))
            }
        }
    }
    foreach ($root in ($allRoots | Select-Object -Unique)) {
        $candidate = Join-Path $root "steamapps\common\Gorilla Tag"
        if (Test-GorillaTag $candidate) { return $candidate }
    }
    return $null
}
$selected = $GamePath
if (-not $selected) { $selected = $env:VOLAS_GAME_DIR }
if (-not $selected) { $selected = Find-GorillaTag }
if (-not (Test-GorillaTag $selected)) {
    throw "Gorilla Tag was not found. Run build.bat -GamePath 'D:\SteamLibrary\steamapps\common\Gorilla Tag' (replace with your actual directory)."
}
$selected = (Resolve-Path -LiteralPath $selected).Path
$managed = Join-Path $selected "Gorilla Tag_Data\Managed"
$bepinex = Join-Path $selected "BepInEx\core\BepInEx.dll"
if (-not (Test-Path -LiteralPath $bepinex)) {
    throw "BepInEx 5 Mono is missing: $bepinex"
}
$required = @("UnityEngine.CoreModule.dll", "UnityEngine.IMGUIModule.dll", "UnityEngine.InputLegacyModule.dll", "UnityEngine.PhysicsModule.dll", "UnityEngine.XRModule.dll", "UnityEngine.TextRenderingModule.dll", "UnityEngine.AudioModule.dll")
$missing = @($required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $managed $_)) })
if ($missing.Count -gt 0) {
    throw ("Missing Unity assemblies: " + ($missing -join ", "))
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "Missing .NET SDK. Install .NET SDK or Visual Studio .NET desktop development."
}
# Remember this path so Ctrl+Shift+B works in Visual Studio afterwards.
$escaped = [System.Security.SecurityElement]::Escape($selected)
$props = @"
<Project>
  <PropertyGroup>
    <GorillaTagDir>$escaped</GorillaTagDir>
  </PropertyGroup>
</Project>
"@
Set-Content -LiteralPath (Join-Path $projectRoot "Volas.Local.props") -Value $props -Encoding UTF8
# Unity versions differ in their managed runtime profile. When no framework is forced,
# try the detected target first and fall back to the other supported target on build failure.
# BepInEx 5 Mono is commonly built against .NET Framework.  Presence of
# netstandard.dll alone does not establish the correct target framework.
$candidates = if ($Framework) {
    @($Framework)
} else {
    @("net472", "netstandard2.1")
}
Write-Host "Building the ENTIRE Volas.sln into one Volas.dll..." -ForegroundColor Cyan
Write-Host "Game: $selected"
Write-Host "Unity assemblies found: $(@(Get-ChildItem -LiteralPath $managed -Filter 'UnityEngine*.dll').Count)"
Write-Host "Source folders: Core, Features, Game, GunLib, Settings, UI, src"
$built = $false
foreach ($candidate in $candidates) {
    Write-Host "Trying target framework: $candidate" -ForegroundColor Cyan
    $argsList = @(
        "build", (Join-Path $projectRoot "Volas.sln"), "-c", "Release",
        "--nologo", "-p:GorillaTagDir=$selected",
        "-p:VolasTargetFramework=$candidate"
    )
    if ($Install) { $argsList += "-p:InstallToGame=true" }
    & dotnet @argsList
    if ($LASTEXITCODE -eq 0) {
        $Framework = $candidate
        $built = $true
        break
    }
    if (-not $Framework) {
        Write-Warning "Build failed for $candidate. Attempting the other supported framework."
    }
}
if (-not $built) {
    throw "Volas could not build with the installed game assemblies. Copy the FIRST CS error from the output and check Docs/TROUBLESHOOTING.md. Ensure BepInEx 5 Mono matches this installed game."
}
# Visual Studio should use the SAME framework that succeeded here.
$props = @"
<Project>
  <PropertyGroup>
    <GorillaTagDir>$escaped</GorillaTagDir>
    <VolasTargetFramework>$Framework</VolasTargetFramework>
  </PropertyGroup>
</Project>
"@
Set-Content -LiteralPath (Join-Path $projectRoot "Volas.Local.props") -Value $props -Encoding UTF8
$dll = Join-Path $projectRoot ("bin\Release\" + $Framework + "\Volas.dll")
if (-not (Test-Path -LiteralPath $dll)) { throw "Expected output was not generated: $dll" }
Write-Host "SUCCESS: $dll" -ForegroundColor Green
if ($Install) { Write-Host "Installed into BepInEx\plugins" -ForegroundColor Green }
