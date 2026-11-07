# Qotoba Workbench

Project management and project documentation for the Qotoba programme. The owner directs Claude Code in chat; the Workbench writes down what happens and shows the state of the project. It never refuses anything the owner says.

Two places where the owner acts: **chat**, on Claude ("break down Login screen", "push", "make a FTR for…", "record that"), and the **inbox**, on what Claude asks. Everything else is information with progressive disclosure.

## What is in it

| Part | What it is | Who writes |
|---|---|---|
| **Work** | A plain WBS: projects, items under items at any depth. An item is a unit of development work, never an FTR, BUG or PER. | The owner shapes it, on the tree or by dictating in chat; Claude writes it (`items_write`). Claude keeps its own steps on an item (`steps_write`), which give the item its progress. |
| **FPBs** | FTR, BUG and PER: a register. Attached to the item they came up in, or to the project. Ids `WB-BUG-003`, never renumbered. Eight states: Open, Verify first, Designing, Designed, In progress, Done, Exists, Won't do. | Always Claude: an FTR from the owner's words in chat, a BUG or PER from its own checks (`fpb_raise`, `fpb_update`, `link_add`). The owner prunes in chat. |
| **Decisions** | The owner's rulings in their own words, dated, with the item they were said about. Every brief carries the ones that apply. | Claude, the moment the owner says it (`decision_record`). The owner withdraws one on its page. |
| **Inbox** | Three kinds reach the owner: a **page** (custom HTML with its own saved answers, for a change that has no designer), a **deviation** (Claude must leave the agreed design, or did something the owner asked to be told about), **stuck** (three failed attempts). Every card takes the owner's own words. | Claude (`page_submit`, `deviation_report`, `stuck_report`). |
| **Progress** | Per project: items done, work left, forecast from measured minutes, what waits on the owner, the last test results with the one before. | Nobody: computed from the tree, the hooks and the ledger. |

Not in it: git (Claude commits, merges and pushes on the owner's word; the Workbench records it), test running (the hooks record `dotnet test` and `dotnet build` results per project), design approval (that happens in the designers: Language Designer, BREEO Walker, UI prototypes).

## Projects

| Project | What it is |
|---|---|
| `Qotoba.Workbench.Model` | Entities, enums, `Caps` and the few shape `Rules` (none refuses the owner). |
| `Qotoba.Workbench.Store` | EF Core on SQLite, migrations, `ProgrammeSeed` (the eight projects), and `Ops` (every change goes through `WorkbenchOps`). |
| `Qotoba.Workbench.Contracts` | DTOs shared with the Host and the UI: hook payloads, press requests, card payloads, MCP inputs. |
| `Qotoba.Workbench.Mcp` | The MCP tools a session calls. There is no tool that creates a project. |
| `Qotoba.Workbench.Service` | One origin: `/api` for the UI, `/hooks` for Claude Code, `/mcp` for sessions, static UI. `Render/Snapshot.cs` projects the store into the object the UI renders. |
| `Qotoba.Workbench.Host` | `qotoba-host`: registers the machine, says which branch a checkout is on, reads file lines, opens a file in Rider, indexes `//+` comments. It runs no git and no tests. |
| `Qotoba.Workbench.Ui` | The approved prototype, with `boot.js` (fetches the snapshot, refreshes on change) and the button wiring. |
| `Qotoba.Workbench.Seed` | Writes the eight projects into a fresh database. |
| `Qotoba.Workbench.Tests` | Rules, operations against in-memory SQLite, and the three surfaces through `WebApplicationFactory`. |

## Run it on this machine

```bash
dotnet run --project src/Workbench/Qotoba.Workbench.Service
```

The service prints an address with the token (`http://localhost:5290/?token=...`). Open it once; the page keeps the token as a cookie. The database and the token live in `%USERPROFILE%\.qotoba-workbench`. Both programs also run as logon tasks; `publish.ps1` republishes them.

```bash
dotnet run --project src/Workbench/Qotoba.Workbench.Host -- serve --checkout D:\Development\repos\Qotoba
```

Claude Code reaches the Workbench in two ways. The MCP server is in the repository's `.mcp.json`, which reads the token from the `QOTOBA_WB_TOKEN` environment variable. The hooks are one script, `hooks/qotoba-hook.mjs`, registered at user level once per machine:

```bash
pwsh src/Workbench/install-hooks.ps1
```

The script only talks to the service when the session's folder is inside a checkout whose `.mcp.json` names `qotoba-workbench`; elsewhere it exits silently.

## The loop

1. The owner says what the work is, in chat or on the tree: "Login screen", with the items under it. Claude writes it. The owner opens the node to work on; opening a sibling of an open node takes them to the one already open.
2. A session started anywhere in the checkout belongs to the open node there: the checkout's bound item, else the item on the checkout's branch, else the one open top item; then the open node under it. Nobody names an item to a session.
3. The session reads its brief, writes its steps, and works. The branch is a convention it keeps itself: one branch per opened top item (`wb/1-login-screen`), shared by everything under it, recorded with `branch_note`.
4. A design that spans several classes, a concept, a data change, a review: the session writes a page and the owner answers on it and approves; the answers are the agreed design. Small blocked questions are not cards: the session takes the recommended answer and records an assumption.
5. A rule the owner states in chat is recorded as a decision there and then. A feature the owner asks for is an FTR; a defect or a slow path the session finds is a BUG or a PER, raised by the session, attached to the item.
6. Nothing closes, merges or pushes on its own. When the work is finished the session advises in chat (tests green, FPBs fixed, ready to merge, push and mark done) and the owner decides. Then the session does what was said and records done in one sentence, written against the item.

There is no concept called a lane (ruling R-1). The opened item is the thing.

## Moving to Azure later

The Service never touches a disk, talks to the Host over HTTP only, and reads every setting from `appsettings.json` or the environment. Moving means a container app, a Postgres server (regenerate the migrations), and new URLs in the Host's `--workbench`, `.mcp.json` and the hook script.
