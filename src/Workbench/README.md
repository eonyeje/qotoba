# Qotoba Workbench

The place the owner decides. The inbox is the only screen where anything is acted on; every other screen is information. Claude Code sessions reach it through http hooks and an MCP server, and fill capped slots on items the owner created: a plan of at most 7 steps, at most 2 open questions, assumptions, at most 5 found items, a note of 2 lines, claims proved by runs, and a packet at the end.

## Projects

| Project | What it is |
|---|---|
| `Qotoba.Workbench.Model` | Entities, enums, `Caps` and `Rules` (the invariants, each with a named test). |
| `Qotoba.Workbench.Store` | EF Core on SQLite, migrations, `ProgrammeSeed` (the eight projects), and `Ops` (every change goes through `WorkbenchOps`). |
| `Qotoba.Workbench.Contracts` | DTOs shared with the Host and the UI: hook payloads, press requests, card payloads, MCP inputs. |
| `Qotoba.Workbench.Mcp` | The MCP tools a session may call. There is no tool that creates a project, item or lane. |
| `Qotoba.Workbench.Service` | One origin: `/api` for the UI, `/hooks` for Claude Code, `/mcp` for sessions, static UI. `Render/Snapshot.cs` projects the store into the object the UI renders. |
| `Qotoba.Workbench.Host` | `qotoba-host`: the only process that touches a disk. Registers the machine, runs git on an approved packet, runs checks, reads file lines. |
| `Qotoba.Workbench.Ui` | The approved prototype, unchanged except `boot.js` (fetches the snapshot, refreshes on change) and the button wiring. |
| `Qotoba.Workbench.Seed` | Writes the eight projects into a fresh database. |
| `Qotoba.Workbench.Tests` | Rules, operations against in-memory SQLite, and the three surfaces through `WebApplicationFactory`. |

## Run it on this machine

```bash
dotnet run --project src/Workbench/Qotoba.Workbench.Service
```

The service prints an address with the token (`http://localhost:5290/?token=...`). Open it once; the page keeps the token as a cookie. The database and the token live in `%USERPROFILE%\.qotoba-workbench`.

```bash
dotnet run --project src/Workbench/Qotoba.Workbench.Host -- serve --checkout D:\Development\repos\Qotoba
```

Claude Code reaches the Workbench in two ways. The MCP server is in the repository's `.mcp.json`, which reads the token from the `QOTOBA_WB_TOKEN` environment variable, so set it once (then restart Rider or the terminal that starts `claude`):

```bash
setx QOTOBA_WB_TOKEN <the token from %USERPROFILE%\.qotoba-workbench\token>
```

The hooks are one script, `hooks/qotoba-hook.mjs`, registered at user level because Claude Code reads a project's `.claude/settings.json` only from the folder a session starts in, and Rider starts sessions in a solution folder such as `src/Workbench`. Also, SessionStart does not run http hooks at all, so the brief has to come from a command hook. Register it once per machine:

```bash
pwsh src/Workbench/install-hooks.ps1
```

The script only talks to the service when the session's folder is inside a checkout whose `.mcp.json` names `qotoba-workbench`; elsewhere it exits silently. It reads the token from the environment variable or from `%USERPROFILE%\.qotoba-workbench\token`.

Then start `claude` anywhere in the repository. The session appears on **Now** within a second, and its brief (delivered by the SessionStart hook) gives it its session id and tells it to call `session_hello`.

## The loop

1. The owner adds an item in the tree (Add item), say "Login screen", and the items under it, and opens the ones to work. Claude never does.
2. A session started anywhere in the checkout belongs to the open node there: the checkout's bound item, else the item on the checkout's branch, else the one open top item; then the open node under it. Nobody names an item to a session.
3. The session calls `session_hello`, reads its brief, and submits a plan with `plan_submit`. A plan card appears in the inbox.
4. The owner accepts the settle-pass defaults and approves. That is when the branch exists: the top item's branch (`wb/1-login-screen`) is created from main and checked out in the owner's checkout through the Host. Everything under the top item shares it. The session hears about the approval at its next stop.
5. The session works the steps. A blocked step becomes a question card (`question_ask`, 2 to 4 options, one recommended). The owner picks; `card_wait` returns the answer.
6. Checks run through the Host or the hooks; a step is done only with a run that proves it.
7. `packet_submit` draws the packet. The owner's Approve merges the branch into main through the Host and closes the item; the branch carries on for the next item under the top. When the last one is done the top item closes, its branch is deleted and the checkout is back on main.

There is no concept called a lane (ruling R-1). The opened item is the thing; the number of branches is the number of top items open at once.

## Reports, dependencies and the code index

- **Reports (FTR, BUG, PER)** are information. The owner creates one (Add item with the kind, or "Add an issue to this task"); a session writes its sections with `report_write`, each section an object in the report's own section order, plus the **When implementing** lines: decisions (options, one recommended, with a cost to change), proofs, dependencies and preconditions. Nothing on a report waits on the owner.
- **Lines move only through a task.** When a task that implements the report submits a plan, low-cost decisions, dependencies and preconditions become settle-pass defaults on the plan card; high-cost decisions wait for a Question Card (`question_ask` with `implLine`); proofs wait for a run (`impl_prove`). The report shows each line's status with a chip to the plan, the card or the run that moved it, and its state follows its takers (fixing, fixed, verified).
- **Dependencies** are proposed by a session (`dependency_propose`) and confirmed by the owner in the next settle pass, or set by the owner directly. The critical path, the unblock weights that order the inbox, and the burn-down on project cards are computed from them.
- **The code index.** The Host parses every `.cs` file in its checkouts with Roslyn on every save and every minute: `//+` comments with the symbol they sit under, `//+=` lines that handle them, public types in files changed on the branch, the branch state and its commits. The Files screen, the comment, type, branch and commit pages, and the seen-state ("new to you · N lines") come from that index. The Workbench reads comments and never edits them.

## Moving to Azure later

The Service never touches a disk, talks to the Host over HTTP only, and reads every setting from `appsettings.json` or the environment. Moving means a container app, a Postgres server (regenerate the migrations), and new URLs in three places: the Host's `--workbench`, `.mcp.json` and `.claude/settings.json`.
