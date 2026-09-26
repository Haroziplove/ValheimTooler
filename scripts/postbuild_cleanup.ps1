param([string]$TargetDir);

Get-ChildItem -Path $TargetDir *.pdb -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item -Path $_.FullName -Force }
Get-ChildItem -Path $TargetDir *.xml -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item -Path $_.FullName -Force }

$pluginDir = "F:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\ValheimAdminTool"
if (-not (Test-Path $pluginDir)) {
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
}

$names = @("ValheimAdminTool.dll", "ValheimAdminToolMod.dll", "SharpConfig.dll")
foreach ($name in $names) {
    $source = Join-Path $TargetDir $name
    if (Test-Path $source) {
        Copy-Item $source (Join-Path $pluginDir $name) -Force
    }
}
