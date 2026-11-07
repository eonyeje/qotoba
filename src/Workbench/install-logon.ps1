# Registers the Workbench Service and the Host as logon tasks for the current user, publishes both, and starts them.
# Re-run at any time; it replaces the tasks. Remove with: Unregister-ScheduledTask -TaskName 'Qotoba Workbench Service','Qotoba Workbench Host'
param([string]$Checkout = (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\..')).Path)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $env:LOCALAPPDATA 'Qotoba\Workbench'
$svcExe = Join-Path $out 'Service\Qotoba.Workbench.Service.exe'
$hostExe = Join-Path $out 'Host\qotoba-host.exe'
$logs = Join-Path $env:USERPROFILE '.qotoba-workbench\logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null

$user = "$env:USERDOMAIN\$env:USERNAME"
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 999 -RestartInterval (New-TimeSpan -Minutes 1) -MultipleInstances IgnoreNew
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited

$svcAction = New-ScheduledTaskAction -Execute $svcExe -WorkingDirectory (Split-Path $svcExe)
$hostAction = New-ScheduledTaskAction -Execute $hostExe -Argument "serve --checkout `"$Checkout`"" -WorkingDirectory (Split-Path $hostExe)

foreach ($t in 'Qotoba Workbench Service', 'Qotoba Workbench Host') {
    if (Get-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue) { Unregister-ScheduledTask -TaskName $t -Confirm:$false }
}
Register-ScheduledTask -TaskName 'Qotoba Workbench Service' -Action $svcAction -Trigger $trigger -Principal $principal -Settings $settings -Description 'Qotoba Workbench service on http://localhost:5290 (inbox, hooks, MCP).' | Out-Null
Register-ScheduledTask -TaskName 'Qotoba Workbench Host' -Action $hostAction -Trigger $trigger -Principal $principal -Settings $settings -Description 'Qotoba Workbench Host on http://localhost:5291 (git, runs, comment index).' | Out-Null
Write-Host "registered two logon tasks for $user"

& (Join-Path $root 'publish.ps1')
Start-Sleep -Seconds 3
Get-ScheduledTask -TaskName 'Qotoba Workbench*' | Select-Object TaskName, State | Format-Table -AutoSize
