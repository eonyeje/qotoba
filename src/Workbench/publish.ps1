# Publishes the Service and the Host to %LOCALAPPDATA%\Qotoba\Workbench and (re)starts their logon tasks.
# Run after a build you want the always-on copies to carry. No admin needed: the tasks are the current user's.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $env:LOCALAPPDATA 'Qotoba\Workbench'
$svc = Join-Path $out 'Service'
$host_ = Join-Path $out 'Host'

Write-Host "stopping tasks"
foreach ($t in 'Qotoba Workbench Service', 'Qotoba Workbench Host') {
    if (Get-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue) { Stop-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue }
}
Get-Process -Name 'Qotoba.Workbench.Service', 'qotoba-host' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "publishing service -> $svc"
dotnet publish (Join-Path $root 'Qotoba.Workbench.Service') -c Release -o $svc --nologo -v q
Write-Host "publishing host -> $host_"
dotnet publish (Join-Path $root 'Qotoba.Workbench.Host') -c Release -o $host_ --nologo -v q

Write-Host "starting tasks"
foreach ($t in 'Qotoba Workbench Service', 'Qotoba Workbench Host') {
    if (Get-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue) { Start-ScheduledTask -TaskName $t }
}
Write-Host "done"
