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

Claude Code in the Qotoba repository is already configured: `.mcp.json` (the MCP server) and `.claude/settings.json` (http hooks). Both read the token from the `QOTOBA_WB_TOKEN` environment variable, so set it once:

```bash
setx QOTOBA_WB_TOKEN <the token from %USERPROFILE%\.qotoba-workbench\token>
```

Then start `claude` in the repository. The session appears on **Now** within a second, and its brief tells it to call `session_hello`.

## The loop

1. The owner adds an item in the tree (Add item). Claude never does.
2. A session calls `session_hello`, reads its brief, and submits a plan with `plan_submit`. A plan card appears in the inbox.
3. The owner accepts the settle-pass defaults and approves. The session hears about it at its next stop.
4. The session works the steps. A blocked step becomes a question card (`question_ask`, 2 to 4 options, one recommended). The owner picks; `card_wait` returns the answer.
5. Checks run through the Host or the hooks; a step is done only with a run that proves it.
6. `packet_submit` draws the packet. The owner's Approve runs the git lines through the Host and closes the item.

## Moving to Azure later

The Service never touches a disk, talks to the Host over HTTP only, and reads every setting from `appsettings.json` or the environment. Moving means a container app, a Postgres server (regenerate the migrations), and new URLs in three places: the Host's `--workbench`, `.mcp.json` and `.claude/settings.json`.
