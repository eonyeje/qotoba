# Unpacks the built plugin into the newest Rider config's plugins folder. Restart Rider afterwards.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$zip = Get-ChildItem (Join-Path $root 'build\distributions') -Filter '*.zip' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $zip) { throw 'No plugin zip: run gradlew.bat buildPlugin first.' }
$config = Get-ChildItem "$env:APPDATA\JetBrains" -Directory | Where-Object Name -like 'Rider20*' | Sort-Object Name -Descending | Select-Object -First 1
if (-not $config) { throw 'No Rider config folder under %APPDATA%\JetBrains.' }
$plugins = Join-Path $config.FullName 'plugins'
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
$target = Join-Path $plugins 'qotoba-workbench-rider'
if (Test-Path $target) { Remove-Item $target -Recurse -Force }
Expand-Archive -Path $zip.FullName -DestinationPath $plugins -Force
Write-Host "installed $($zip.Name) into $plugins ($($config.Name)). Restart Rider."
