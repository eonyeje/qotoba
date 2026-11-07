# Registers the Workbench hook with Claude Code for the current user, so every session this machine starts
# (from any folder, Rider's solution folders included) reaches the service. The hook itself only speaks to the
# service when the session's folder is inside a Workbench checkout (one with a .mcp.json naming qotoba-workbench).
# Re-run at any time; it replaces only the seven Workbench entries in %USERPROFILE%\.claude\settings.json.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $env:LOCALAPPDATA 'Qotoba\Workbench\hooks'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Copy-Item (Join-Path $root 'hooks\qotoba-hook.mjs') (Join-Path $out 'qotoba-hook.mjs') -Force

$script = (Join-Path $out 'qotoba-hook.mjs') -replace '\\', '/'
$command = "node `"$script`""
function entry([int]$timeout, [string]$matcher) {
    $h = [ordered]@{ hooks = @([ordered]@{ type = 'command'; command = $command; timeout = $timeout }) }
    if ($matcher) { $h = [ordered]@{ matcher = $matcher; hooks = $h.hooks } }
    return ,@($h)
}

$file = Join-Path $env:USERPROFILE '.claude\settings.json'
$settings = if (Test-Path $file) { Get-Content $file -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
if (-not $settings.ContainsKey('hooks') -or $null -eq $settings['hooks']) { $settings['hooks'] = @{} }
$hooks = $settings['hooks']
$hooks['SessionStart']       = entry 10 $null
$hooks['UserPromptSubmit']   = entry 10 $null
$hooks['PreToolUse']         = entry 10 'Bash'
$hooks['PostToolUse']        = entry 10 $null
$hooks['PostToolUseFailure'] = entry 10 $null
$hooks['Stop']               = entry 15 $null
$hooks['SessionEnd']         = entry 2 $null
$settings | ConvertTo-Json -Depth 12 | Set-Content $file -Encoding utf8
Write-Host "hook installed: $script"
Write-Host "registered in $file for SessionStart, UserPromptSubmit, PreToolUse(Bash), PostToolUse, PostToolUseFailure, Stop, SessionEnd"
