param([string]$GamePath = "")
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $GamePath) { $GamePath = $env:VOLAS_GAME_DIR }
$localProps = Join-Path $projectRoot "Volas.Local.props"
if (-not $GamePath -and (Test-Path -LiteralPath $localProps)) {
    $xml = [xml](Get-Content -LiteralPath $localProps -Raw)
    $GamePath = $xml.Project.PropertyGroup.GorillaTagDir
}
if (-not $GamePath) {
    Write-Host "Pass -GamePath to inspect your local Gorilla Tag installation."
    exit 1
}
$managed = Join-Path $GamePath "Gorilla Tag_Data\Managed"
$core = Join-Path $GamePath "BepInEx\core"
Write-Host "Volas local reference diagnostic" -ForegroundColor Cyan
Write-Host "GAME: $GamePath"
Write-Host "MANAGED: $managed"
Write-Host "BEPINEX: $core"
$files = @(
    (Join-Path $core "BepInEx.dll"),
    (Join-Path $managed "UnityEngine.dll"),
    (Join-Path $managed "UnityEngine.CoreModule.dll"),
    (Join-Path $managed "UnityEngine.IMGUIModule.dll"),
    (Join-Path $managed "UnityEngine.UI.dll"),
    (Join-Path $managed "UnityEngine.UIModule.dll"),
    (Join-Path $managed "UnityEngine.InputLegacyModule.dll"),
    (Join-Path $managed "UnityEngine.PhysicsModule.dll"),
    (Join-Path $managed "UnityEngine.XRModule.dll"),
    (Join-Path $managed "UnityEngine.TextRenderingModule.dll"),
    (Join-Path $managed "UnityEngine.AudioModule.dll"),
    (Join-Path $managed "netstandard.dll")
)
foreach ($file in $files) {
    $label = if (Test-Path -LiteralPath $file) { "OK     " } else { "MISSING" }
    Write-Host "$label  $file"
}
$facadeCandidates = @(
    (Join-Path $managed "UnityEngine.dll"),
    (Join-Path $core "UnityEngine.dll"),
    (Join-Path $GamePath "BepInEx\plugins\UnityEngine.dll")
)
$foundFacade = @($facadeCandidates | Where-Object { Test-Path -LiteralPath $_ })
if ($foundFacade.Count -gt 0) {
    Write-Host "UnityEngine.dll facade candidates:" -ForegroundColor Green
    $foundFacade | ForEach-Object {
        Write-Host "  $_"
        try { Write-Host "    $([System.Reflection.AssemblyName]::GetAssemblyName($_).FullName)" }
        catch { Write-Warning "    Not a readable managed assembly: $_" }
    }
} else {
    Write-Warning "No UnityEngine.dll facade found in game/BepInEx directories. Older BepInEx assemblies referencing UnityEngine can cause CS0012 MonoBehaviour errors. Use matching BepInEx and Unity assemblies; do not download arbitrary UnityEngine.dll files."
}
try {
    $loader = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $core "BepInEx.dll"))
    $loader.GetReferencedAssemblies() | Where-Object { $_.Name -like "UnityEngine*" } |
        ForEach-Object { Write-Host "BepInEx expects: $($_.FullName)" }
} catch { Write-Warning "Could not inspect BepInEx references: $_" }
Write-Host "If this still fails: paste the FIRST full CS error and the exact referenced assembly name."
