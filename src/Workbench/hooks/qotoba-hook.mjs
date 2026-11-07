// Qotoba Workbench · the Claude Code hook.
// One script for every hook event. Claude Code pipes the event JSON to stdin; this script posts it to the Workbench
// service and prints the reply, which Claude Code reads as the hook's output (additionalContext, deny, block).
//
// It is registered at user level (every session on this machine) because Claude Code reads a project's
// .claude/settings.json from the folder the session starts in, and Rider starts sessions in a solution folder,
// not the repository root. The script therefore decides for itself whether the session belongs to the Workbench:
// only when a folder from the session's cwd upwards holds a .mcp.json that names the qotoba-workbench server.
// Elsewhere it prints nothing and exits 0.
//
// The token comes from QOTOBA_WB_TOKEN or, failing that, %USERPROFILE%\.qotoba-workbench\token.
// The service address comes from QOTOBA_WB_URL or defaults to http://localhost:5290.
import { readFileSync, existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { homedir } from 'node:os';

const service = (process.env.QOTOBA_WB_URL || 'http://localhost:5290').replace(/\/$/, '');

function readStdin() {
  try { return readFileSync(0, 'utf8'); } catch { return ''; }
}

function isWorkbenchCheckout(cwd) {
  let dir = cwd;
  for (let i = 0; i < 40 && dir; i++) {
    const f = join(dir, '.mcp.json');
    if (existsSync(f)) {
      try { if (readFileSync(f, 'utf8').includes('qotoba-workbench')) return true; } catch { /* unreadable: keep climbing */ }
    }
    const up = dirname(dir);
    if (up === dir) break;
    dir = up;
  }
  return false;
}

function token() {
  const env = process.env.QOTOBA_WB_TOKEN;
  if (env && env.trim()) return env.trim();
  try { return readFileSync(join(homedir(), '.qotoba-workbench', 'token'), 'utf8').trim(); } catch { return ''; }
}

const raw = readStdin();
let ev = null;
try { ev = JSON.parse(raw); } catch { process.exit(0); }
if (!ev || !ev.hook_event_name) process.exit(0);
if (!isWorkbenchCheckout(ev.cwd || process.cwd())) process.exit(0);

// SessionEnd has the tightest budget; everything else may wait for the service a little longer.
const budgetMs = ev.hook_event_name === 'SessionEnd' ? 1200 : ev.hook_event_name === 'Stop' ? 12000 : 8000;
const ctrl = new AbortController();
const timer = setTimeout(() => ctrl.abort(), budgetMs);
try {
  const r = await fetch(service + '/hooks', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + token() },
    body: raw,
    signal: ctrl.signal
  });
  const text = await r.text();
  if (r.ok && text.trim().startsWith('{')) process.stdout.write(text);
} catch {
  // the service is down or slow: the session carries on without the Workbench
} finally {
  clearTimeout(timer);
}
