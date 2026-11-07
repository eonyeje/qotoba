/* Qotoba Workbench prototype · sample data.
   One plain object, window.WB. Nothing in index.html or app.js carries sample content.
   References are written "kind:id" (for files "file:id:from-to"); inside prose they are [[kind:id]] and render as chips.
   Snapshot time: Friday 6 November 2026, 05:12 Lagos time. */
var WB = {};

WB.meta = {
  programme: "Qotoba",
  owner: "Emmanuel",
  now: "Fri 6 Nov 2026 · 05:12",
  today: "6 Nov",
  avgDecisionSec: 48,
  decisionsMeasured: 212,
  decisionsSince: "1 Nov",
  workLeft: { coarseWeeks: 67, measuredWeeks: 2 }
};

WB.budgets = [
  { key: "questions", label: "Open questions per item", value: 2, def: 2, note: "A third question is refused until one is answered." },
  { key: "found", label: "Found items per session", value: 5, def: 5, note: "Found items go to the item's tray, never into the tree." },
  { key: "planSteps", label: "Steps per plan", value: 7, def: 7, note: "A longer plan is refused; split the item instead." },
  { key: "noteLines", label: "Lines in a Claude note", value: 2, def: 2, note: "Anything longer belongs in a question or the packet." }
];

/* ---------------------------------------------------------------- projects */
WB.projects = [
  { id: "wb", code: "WB", name: "Workbench", color: "wb", state: "open", coarseWeeks: 9, measuredWeeks: 2, factor: 1.2,
    purpose: "The place I decide: inbox, tree, packets and rulings, fed by Claude Code sessions on three machines through the hook endpoint.",
    children: ["WB-1", "WB-2", "WB-3", "WB-4", "WB-5", "WB-6"],
    lanes: ["A", "B", "C"],
    burn: [52, 50, 49, 46, 44, 41, 38], burnDays: ["31 Oct", "1 Nov", "2 Nov", "3 Nov", "4 Nov", "5 Nov", "6 Nov"],
    criticalPath: ["item:WB-2.1", "item:WB-2.2", "item:WB-2.4", "item:WB-3", "item:WB-4", "item:WB-6"],
    nextDecision: "card:pk-runs",
    repo: "Qotoba (monorepo) · src/Qotoba.Workbench.*" },
  { id: "addins", code: "ADD", name: "Add-ins", color: "ade", state: "coarse", coarseWeeks: 8, measuredWeeks: 0, factor: null,
    purpose: "Rider and VS Code add-ins: diagnostics, completion, hover, go-to, format and the Workbench tool window.",
    children: [], closedWhy: "Waits on the language server (Language, phase C). Opens after the Workbench tree slice." },
  { id: "lang", code: "LNG", name: "Language", color: "lang", state: "coarse", coarseWeeks: 14, measuredWeeks: 0, factor: null,
    purpose: "The Qotoba language: parser and printer, checker, and the C# and TypeScript emitters.",
    children: [], preview: ["LNG-1", "LNG-2", "LNG-3", "LNG-4", "LNG-5"],
    closedWhy: "Closed. Its four children are a preview of what appears when an approved plan opens it." },
  { id: "fw", code: "FW", name: "Framework", color: "fw", state: "coarse", coarseWeeks: 10, measuredWeeks: 0, factor: null,
    purpose: "The Qotoba framework carried over from Quanta: intents, widgets, interactions, services, the MAUI and server hosts.",
    children: [], preview: ["FW-1"], closedWhy: "Closed until the Language emitters exist; its Quanta baseline is recorded in the plan." },
  { id: "dope", code: "DOPE", name: "Dope", color: "apps", state: "coarse", coarseWeeks: 6, measuredWeeks: 0, factor: null,
    purpose: "The media library rewritten in Qotoba: 1,356 movies, seven providers, the library and movie pages.",
    children: [],
    closedWhy: "Closed (phase D). Two issues from the seed are attached to the project and stay unscheduled until it opens." },
  { id: "ks", code: "KS", name: "KraveSoccer", color: "apps", state: "coarse", coarseWeeks: 3, measuredWeeks: 0, factor: null,
    purpose: "A small football app that proves the mobile head and the first device contracts.",
    children: [], closedWhy: "Closed (phase E). Its feature list is recovered from the Dialect code before design starts." },
  { id: "ent", code: "ENT", name: "Entrepreneur", color: "apps", state: "coarse", coarseWeeks: 5, measuredWeeks: 0, factor: null,
    purpose: "A business simulation with a map: processes, state machines and custom drawing.",
    children: [], closedWhy: "Closed (phase E). Nothing on KraveFlow's path depends on it." },
  { id: "kf", code: "KF", name: "KraveFlow", color: "agents", state: "coarse", coarseWeeks: 12, measuredWeeks: 0, factor: null,
    purpose: "Distributor ordering through AI agents on WhatsApp, SMS and email; fifteen modules on Qotoba.Server.",
    children: [], closedWhy: "Closed. Its backend lane starts in phase D once the services family exists." }
];

WB.lanes = {
  A: { id: "A", name: "runs", factor: 1.3, was: 1.2, path: ["item:WB-2", "item:WB-2.2"], machine: "machine:desk-pc", state: "waiting on you" },
  B: { id: "B", name: "hooks", factor: 1.1, was: 1.1, path: ["item:WB-2", "item:WB-2.3"], machine: "machine:laptop", state: "stuck" },
  C: { id: "C", name: "chips", factor: 1.2, was: null, path: ["item:WB-2", "item:WB-2.4"], machine: "machine:cloud-1", state: "proposed by plan", proposed: true }
};

/* ---------------------------------------------------------------- items
   kind: FTR | BUG | PER | task.  state: done | building | review | planning | planned | blocked | coarse.
   isOpen marks the open path. Steps: done | current | blocked | todo. Times in minutes; estimates in Claude-minutes. */
WB.items = {
  "WB-1": {
    kind: "task", title: "Hello card across two machines", project: "wb", parent: null, children: [], state: "done", isOpen: false,
    lane: "A", machine: "desk-pc", branch: "br-1", coarse: "~3 days",
    goal: "A card typed on desk-pc and one typed on laptop both reach my phone through the hook endpoint, each naming its machine.",
    steps: [
      { t: "Host posts a hello event to /hooks/v1", est: 30, state: "done", proof: "run:1036" },
      { t: "Store the card with its machine and session", est: 25, state: "done", proof: "run:1033" },
      { t: "Inbox renders the card on the phone", est: 35, state: "done", proof: "run:1035" },
      { t: "Second machine registers and posts", est: 30, state: "done", proof: "run:1038" },
      { t: "Smoke: hello card from both machines", est: 30, state: "done", proof: "run:1038" }
    ],
    checks: ["run:1031", "run:1033", "run:1035", "run:1036", "run:1038"],
    changes: { files: [["file:hookendpoint", 43, 0], ["file:hellocard", 24, 0], ["file:claudemd", 12, 0]], types: ["type:HookEvent"] },
    time: { so: 190, est: 150, factor: 1.3, forecast: 190 },
    needs: [], blocks: [{ ref: "item:WB-2", min: 0, note: "done" }],
    commentScope: ["c8", "c9"],
    note: { text: "Both machines post and the phone shows each card with its machine. The laptop clock is 1.8 s behind desk-pc; it did not matter here.", date: "5 Nov 18:29", session: "session:51b0" },
    rulings: ["ruling:R-01", "ruling:R-04", "ruling:R-05", "ruling:R-10"],
    sessions: [["session:51b0", 190]],
    packet: "card:pk-hello",
    history: [
      ["4 Nov 21:02", "opened", "You added the item under Workbench"],
      ["5 Nov 14:58", "plan approved", "5 steps, 150 Claude-min"],
      ["5 Nov 16:40", "question answered", "[[question:Q-01]] became [[ruling:R-04]]"],
      ["5 Nov 18:31", "packet drawn", "Session [[session:51b0]] ended with [[card:pk-hello]]"],
      ["5 Nov 18:42", "packet approved", "Merged as [[commit:a41c9e2]]"]
    ],
    info: { tpl: "FTR",
      goal: "Prove the round trip end to end before anything else is built: a Host on each machine posts, the service stores, the phone renders.",
      scopeIn: ["Host 0.4.2 posting to /hooks/v1 with a machine token", "Card stored with machine and session", "Phone inbox renders one card per event", "Second machine registration"],
      scopeOut: ["Questions, packets and plans (later slices)", "Ordering across machines (became [[item:WB-2.3]])", "Notifications beyond one per card"],
      parity: ["The Quanta Workbench took cards only from the machine it ran on; this removes that limit.", "The old card showed the session's own summary text; the hello card shows only fixed fields."],
      touched: ["Inbox › Info card", "Machines › registration", "[[file:hookendpoint]]"],
      acceptance: [{ t: "A card from each machine reaches the phone within 5 s", proof: "run:1038" }, { t: "An event from an unregistered machine is refused", proof: "run:1033" }]
    }
  },

  "WB-2": {
    kind: "task", title: "Task card with real runs", project: "wb", parent: null, children: ["WB-2.1", "WB-2.2", "WB-2.3", "WB-2.4"],
    state: "building", isOpen: true, lane: "A + B", machine: "desk-pc", branch: null, coarse: "~1 week",
    goal: "The task card shows checks that come from real runs on a named machine, never a session saying tests passed.",
    steps: [
      { t: "Runs table fills from hook events", item: "item:WB-2.1", est: 100, state: "done", proof: "run:1045", note: "in review" },
      { t: "Run page with output", item: "item:WB-2.2", est: 130, state: "current", proof: "nyw:RunOutputBlock" },
      { t: "Hook events arrive out of order", item: "item:WB-2.3", est: 110, state: "blocked", proof: "run:1056" },
      { t: "Check chips on the summary block", item: "item:WB-2.4", est: 125, state: "todo", proof: "nyw:SummaryChecks" },
      { t: "Slice smoke: hello card shows real checks", est: 105, state: "todo", proof: "nyw:smoke-slice" }
    ],
    checks: ["run:1041", "run:1042", "run:1043", "run:1044", "run:1045", "run:1049", "run:1051", "run:1056"],
    time: { so: 242, est: 570, factor: 1.2, forecast: 685 },
    needs: [{ ref: "item:WB-1", min: 0, note: "done" }],
    blocks: [{ ref: "item:WB-3", min: 400 }, { ref: "item:WB-4", min: 960 }],
    commentScope: ["c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9"],
    note: { text: "Runs table is in review; the run page waits on two questions. Lane B is stuck on the 1.8 s clock skew between desk-pc and laptop.", date: "6 Nov 05:03", session: "session:7f3a" },
    rulings: ["ruling:R-03", "ruling:R-05", "ruling:R-06", "ruling:R-07", "ruling:R-09", "ruling:R-10"],
    sessions: [["session:c21e", 125], ["session:7f3a", 46], ["session:b915", 71], ["session:2a6d", 32]],
    history: [
      ["5 Nov 19:10", "opened", "You opened the node after [[item:WB-1]] closed"],
      ["5 Nov 19:34", "plan approved", "5 steps; 4 children created by you from the plan"],
      ["5 Nov 22:10", "question answered", "[[question:Q-03]] became [[ruling:R-09]]"],
      ["5 Nov 23:05", "lane added", "Lane B for [[item:WB-2.3]], parallel to lane A"],
      ["6 Nov 03:58", "packet drawn", "[[card:pk-runs]] for [[item:WB-2.1]]"],
      ["6 Nov 04:38", "stuck", "[[card:st-23]] on [[item:WB-2.3]]"],
      ["6 Nov 04:58", "plan submitted", "[[card:pl-24]] for [[item:WB-2.4]]"]
    ],
    info: { tpl: "FTR",
      goal: "A check on the card is a chip that opens the run behind it. A claim with no run is marked unverified in red.",
      scopeIn: ["Runs table filled from PostToolUse hook events", "Run page with the last 40 lines of output", "Ordering of hook events across machines", "Check chips on the summary block", "The slice smoke on the hello card"],
      scopeOut: ["Run retention policy (runs stay; see [[assumption:a11]])", "CI servers posting runs", "Dope runs, which arrive by the seed only"],
      parity: ["The Quanta task card showed 'tests passed' as text typed by the session. That text is gone.", "The Quanta Workbench had no run page; output lived in the chat.", "Kept: the last-run date beside each check, which the old card had."],
      touched: ["Item › Runs tab", "Run page", "Summary block › Checks", "[[file:runrecord]]", "[[file:hookeventqueue]]", "[[file:runpage]]"],
      acceptance: [{ t: "The hello card shows build, tests and smoke as chips from runs", proof: "nyw:smoke-slice" }, { t: "A typed claim without a run renders as unverified", proof: null }, { t: "Runs from desk-pc and laptop list in the order they ran", proof: "run:1056" }]
    }
  },

  "WB-2.1": {
    kind: "task", title: "Runs table fills from hook events", project: "wb", parent: "WB-2", children: [], state: "review", isOpen: false,
    lane: "A", machine: "laptop", branch: "br-21", coarse: "~half a day",
    goal: "Every check a session runs lands in the runs table with its machine, session, result and output, straight from the hook.",
    steps: [
      { t: "RunRecord and the runs table", est: 20, state: "done", proof: "run:1044" },
      { t: "Hook endpoint writes a run on PostToolUse for a check", est: 25, state: "done", proof: "run:1042" },
      { t: "Drain the queue in batches", est: 15, state: "done", proof: "run:1042" },
      { t: "Runs tab lists runs from the store", est: 20, state: "done", proof: "run:1043" },
      { t: "Smoke: hello card shows a real run", est: 20, state: "done", proof: "run:1045" }
    ],
    checks: ["run:1041", "run:1042", "run:1043", "run:1044", "run:1045"],
    changes: { files: [["file:runrecord", 40, 0], ["file:runstable", 58, 0], ["file:hookendpoint", 34, 12], ["file:hookeventqueue", 41, 18], ["file:runstab", 77, 6], ["file:runduration", 22, 0], ["file:runstabletests", 40, 5]],
      types: ["type:RunRecord", "type:RunResult", "type:RunId", "type:RunsTable", "type:RunDurationFormatter"] },
    time: { so: 125, est: 100, factor: 1.25, forecast: 125 },
    needs: [{ ref: "item:WB-1", min: 0, note: "done" }],
    blocks: [{ ref: "item:WB-2.2", min: 52 }, { ref: "item:WB-2.4", min: 150 }],
    commentScope: ["c1", "c2", "c3", "c5", "c6", "c8", "c9"],
    note: { text: "Runs arrive from the hook and show on the Runs tab. Durations go through a small formatter; out-of-order events are WB-2.3's job.", date: "6 Nov 03:56", session: "session:c21e" },
    rulings: ["ruling:R-03", "ruling:R-05", "ruling:R-09", "ruling:R-10"],
    sessions: [["session:c21e", 125]],
    packet: "card:pk-runs",
    history: [
      ["5 Nov 19:34", "created", "Created by you from the approved plan of [[item:WB-2]]"],
      ["5 Nov 21:48", "plan approved", "5 steps, 100 Claude-min"],
      ["5 Nov 22:10", "question answered", "[[question:Q-03]] became [[ruling:R-09]]"],
      ["6 Nov 01:53", "session started", "[[session:c21e]] on [[machine:laptop]]"],
      ["6 Nov 03:58", "packet drawn", "[[card:pk-runs]] waits for you"]
    ],
    info: { tpl: "task",
      purpose: "Make runs a fact the Workbench records, so a card can show a check only when a run proves it.",
      rulings: ["ruling:R-03", "ruling:R-09", "ruling:R-10"],
      files: [["file:runrecord", "the record a run becomes"], ["file:runstable", "insert and query"], ["file:hookendpoint", "receives PostToolUse"], ["file:hookeventqueue", "batches events to the store"]],
      acceptance: [{ t: "A Bash check on the hello item appears on its Runs tab", proof: "run:1045" }, { t: "A retried post makes one row, not two", proof: "run:1042" }],
      risks: ["Events from two machines may arrive out of order (owned by [[item:WB-2.3]]).", "Large outputs: capped by [[ruling:R-09]]."],
      unblocks: [{ ref: "item:WB-2.2", min: 52 }, { ref: "item:WB-2.4", min: 150 }]
    }
  },

  "WB-2.2": {
    kind: "task", title: "Run page with output", project: "wb", parent: "WB-2", children: [], state: "building", isOpen: true,
    lane: "A", machine: "desk-pc", branch: "br-22", coarse: "~half a day",
    goal: "Tapping a check opens its run: the command, the result, where and when it ran, and the end of its output.",
    steps: [
      { t: "Route /runs/{id} and the header from RunRecord", est: 25, state: "done", proof: "run:1049" },
      { t: "Output block: the last 40 lines from the run row", est: 30, state: "done", proof: "run:1051" },
      { t: "After-the-run or live output", est: 35, state: "blocked", proof: "nyw:RunOutputBlock", needs: ["question:Q-07"] },
      { t: "Exit code and result pill", est: 20, state: "blocked", proof: "nyw:RunOutputBlock", needs: ["question:Q-08"] },
      { t: "Smoke: open a run from the hello card", est: 20, state: "todo", proof: "nyw:smoke-runpage" }
    ],
    checks: ["run:1049", "run:1051", "run:1048"],
    changes: { files: [["file:runpage", 31, 0], ["file:runpagetests", 26, 0]], types: ["type:RunPage"] },
    time: { so: 46, est: 130, factor: 1.3, forecast: 169 },
    needs: [{ ref: "item:WB-2.1", min: 0, note: "in review" }, { ref: "question:Q-07", min: 0 }, { ref: "question:Q-08", min: 0 }],
    blocks: [{ ref: "item:WB-2.4", min: 45 }, { ref: "item:WB-2", min: 105 }],
    commentScope: ["c2", "c3"],
    note: { text: "Header and 40-line tail are built and tested. Steps 3 and 4 wait on the two questions in your inbox.", date: "6 Nov 05:02", session: "session:7f3a" },
    claims: [{ t: "The header shows machine and session chips", proof: "run:1051" }, { t: "Output of 64 KB renders in under 100 ms", proof: null }],
    rulings: ["ruling:R-06", "ruling:R-09"],
    sessions: [["session:7f3a", 46]],
    history: [
      ["5 Nov 19:34", "created", "Created by you from the approved plan of [[item:WB-2]]"],
      ["6 Nov 04:20", "plan approved", "5 steps, 130 Claude-min"],
      ["6 Nov 04:26", "session started", "[[session:7f3a]] on [[machine:desk-pc]]"],
      ["6 Nov 04:51", "question asked", "[[question:Q-07]] blocks step 3"],
      ["6 Nov 05:02", "question asked", "[[question:Q-08]] blocks step 4 · questions 2/2"]
    ],
    info: { tpl: "task",
      purpose: "When a chip says tests 212, one tap should show me those 212 tests ran, where, and how they ended.",
      rulings: ["ruling:R-06", "ruling:R-09", "ruling:R-10"],
      files: [["file:runpage", "the page"], ["file:runrecord", "the data it shows"], ["nyw:RunOutputBlock", "the output block (step 3)"], ["file:runduration", "formats durations"]],
      acceptance: [{ t: "A run opens from a chip in one tap", proof: "nyw:smoke-runpage" }, { t: "The last 40 lines show without scrolling the page sideways on a phone", proof: "run:1051" }, { t: "Output of 64 KB renders in under 100 ms", proof: null }],
      risks: ["Live streaming would need a push channel the Host does not have.", "RunRecord has no exit code yet ([[question:Q-08]])."],
      unblocks: [{ ref: "item:WB-2.4", min: 45 }, { ref: "item:WB-2", min: 105 }]
    }
  },

  "WB-2.3": {
    kind: "task", title: "Order hook events across machines", project: "wb", parent: "WB-2", children: [], state: "blocked", isOpen: true,
    lane: "B", machine: "laptop", branch: "br-23", coarse: "~half a day",
    goal: "Runs and events from two machines list in the order they happened, whatever the clocks say.",
    steps: [
      { t: "Reproduce with desk-pc and laptop posting together", est: 20, state: "done", proof: "run:1047" },
      { t: "Order by session, turn and Host time in the queue", est: 35, state: "blocked", proof: "run:1056", note: "failed 3 times" },
      { t: "Order by the hook's sequence number (the + comment)", est: 30, state: "blocked", proof: "nyw:HookEvent.Sequence", needs: ["question:Q-09"] },
      { t: "Regression: 500 events from two machines arrive ordered", est: 25, state: "todo", proof: "nyw:HookOrderingRegression" }
    ],
    checks: ["run:1047", "run:1050", "run:1053", "run:1056"],
    changes: { files: [["file:hookeventqueue", 14, 3], ["file:hookendpoint", 7, 1], ["file:hookorderingtests", 61, 0]], types: [] },
    time: { so: 71, est: 110, factor: 1.1, forecast: 121 },
    needs: [{ ref: "question:Q-09", min: 0 }, { ref: "card:st-23", min: 0 }],
    blocks: [{ ref: "item:WB-2", min: 110 }],
    commentScope: ["c4", "c5", "c6", "c7", "c8"],
    note: { text: "Timestamps cannot order events across machines: laptop runs 1.8 s behind desk-pc. A per-session sequence from the Host would fix it.", date: "6 Nov 04:40", session: "session:b915" },
    rulings: ["ruling:R-03", "ruling:R-10"],
    sessions: [["session:b915", 71]],
    history: [
      ["5 Nov 19:34", "created", "Created by you from the approved plan of [[item:WB-2]]"],
      ["5 Nov 23:05", "lane added", "Lane B so it runs beside the run page"],
      ["6 Nov 03:55", "plan approved", "4 steps, 110 Claude-min"],
      ["6 Nov 04:01", "session started", "[[session:b915]] on [[machine:laptop]]"],
      ["6 Nov 04:38", "stuck", "[[run:1056]] failed for the third time · [[card:st-23]]"],
      ["6 Nov 04:47", "question asked", "[[question:Q-09]] from [[comment:c4]]"]
    ],
    info: { tpl: "task",
      purpose: "Runs from two machines must list in the order they happened, so a check chip never points at an older run than the one I expect.",
      rulings: ["ruling:R-03", "ruling:R-10"],
      files: [["file:hookeventqueue", "where events are ordered"], ["file:hookendpoint", "what the Host posts"], ["file:hookorderingtests", "the two-machine test"]],
      acceptance: [{ t: "40 interleaved events from two machines drain in order", proof: "run:1056" }, { t: "500 events from two machines drain in order", proof: null }],
      risks: ["Clock skew between machines (laptop is 1.8 s behind desk-pc).", "A Host change means reinstalling on three machines."],
      unblocks: [{ ref: "item:WB-2", min: 110 }]
    }
  },

  "WB-2.4": {
    kind: "task", title: "Check chips on the summary block", project: "wb", parent: "WB-2", children: [], state: "planning", isOpen: false,
    lane: "C", machine: "cloud-1", branch: "br-24", coarse: "~half a day",
    goal: "Every check on an item's summary is a chip from a real run, never typed text.",
    steps: [],
    checks: [],
    changes: { files: [], types: [] },
    time: { so: 32, est: 125, factor: 1.2, forecast: 150 },
    needs: [{ ref: "item:WB-2.1", min: 0, note: "in review" }, { ref: "item:WB-2.2", min: 45 }],
    blocks: [{ ref: "item:WB-2", min: 105 }],
    commentScope: [],
    note: { text: "Plan of 6 steps submitted with three settle questions. Reading SummaryBlock.cs for the chip slot while it waits.", date: "6 Nov 04:58", session: "session:2a6d" },
    rulings: ["ruling:R-06", "ruling:R-09"],
    sessions: [["session:2a6d", 32]],
    plan: "card:pl-24",
    history: [
      ["5 Nov 19:34", "created", "Created by you from the approved plan of [[item:WB-2]]"],
      ["6 Nov 04:40", "session started", "[[session:2a6d]] on [[machine:cloud-1]], read only"],
      ["6 Nov 04:58", "plan submitted", "[[card:pl-24]] · 6 steps, 125 Claude-min"]
    ],
    info: { tpl: "task",
      purpose: "The summary block is what I read first. Its checks must be chips from runs, so 'tests 212' always opens the run.",
      rulings: ["ruling:R-06", "ruling:R-09"],
      files: [["file:summaryblock", "where the chips go"], ["nyw:SummaryChecks", "the chip row (step 2)"], ["file:runstable", "latest run per check"]],
      acceptance: [{ t: "Each check chip opens its run", proof: "nyw:SummaryChecks" }, { t: "A check that never ran shows a dashed 'not run' chip", proof: null }],
      risks: ["Depends on [[item:WB-2.1]] merging first.", "Stale rule must be settled before step 4."],
      unblocks: [{ ref: "item:WB-2", min: 105 }]
    }
  },

  "WB-3": { kind: "task", title: "Question cards with chips and budgets", project: "wb", parent: null, children: [], state: "planned", isOpen: false, coarse: "~1 week",
    goal: "A question reaches me as one sentence with options, chips and the item's budget, never as an essay.",
    plannedNote: "Next in lane A after WB-2 closes. A plan is drawn when it opens.",
    info: { tpl: "coarse", text: "Question Card with breadcrumb, pulled code, options with reasons, cost to change, and the 2-per-item budget enforced by the Model. Assumptions batch and found tray come with it." } },
  "WB-4": { kind: "task", title: "Tree with the one-open rule", project: "wb", parent: null, children: [], state: "coarse", isOpen: false, coarse: "~2 weeks",
    info: { tpl: "coarse", text: "Projects and items of any depth, one open node per level per lane, lanes, coarse sizes and measured time left. Carries the tree-query issue [[fpb:PER-1]]." } },
  "WB-5": { kind: "task", title: "File view with comments", project: "wb", parent: null, children: [], state: "coarse", isOpen: false, coarse: "~1 week",
    info: { tpl: "coarse", text: "Index of + comments by file with open, handled and stale states, versions, and proof diffs. Read only: the Workbench never edits a comment ([[ruling:R-08]])." } },
  "WB-6": { kind: "task", title: "Seed from the old Workbench", project: "wb", parent: null, children: [], state: "coarse", isOpen: false, coarse: "~4 days",
    info: { tpl: "coarse", text: "Import rulings, findings and runs from the Quanta Workbench. A one-off Dope seed already ran by hand on 4 Nov (38 findings, 5 runs); this item makes it a repeatable import." } },

  "LNG-1": { kind: "task", title: "Parser and printer", project: "lang", parent: null, children: [], state: "coarse", preview: true, coarse: "~4 weeks", info: { tpl: "coarse", text: "Round-trip parse and print for every .qo file; thirty seeded mutations still parse." } },
  "LNG-2": { kind: "task", title: "Checker", project: "lang", parent: null, children: [], state: "coarse", preview: true, coarse: "~3 weeks", info: { tpl: "coarse", text: "Names, types and intent-family rules with diagnostics the add-ins show." } },
  "LNG-3": { kind: "task", title: "C# emitter", project: "lang", parent: null, children: [], state: "coarse", preview: true, coarse: "~4 weeks", info: { tpl: "coarse", text: "Emitted C# against golden baselines; builds with warnings as errors." } },
  "LNG-5": { kind: "task", title: "Core constructs", project: "lang", parent: null, children: [], state: "coarse", preview: true, coarse: "~5 weeks", info: { tpl: "coarse", text: "class, record, enum and union in QDL, each parsed, checked and emitted to both targets." } },
  "FW-1": { kind: "task", title: "Observable core", project: "fw", parent: null, children: [], state: "coarse", preview: true, coarse: "~2 weeks", info: { tpl: "coarse", text: "Change notification for members and collections, carried over from Quanta's observable layer." } },
  "LNG-4": { kind: "task", title: "TypeScript emitter", project: "lang", parent: null, children: [], state: "coarse", preview: true, coarse: "~3 weeks", info: { tpl: "coarse", text: "Emitted TypeScript against baselines; tsc --strict clean." } }
};

/* ---------------------------------------------------------------- questions
   Open questions are pushed as Question Cards; answered ones live on the item. */
WB.questions = {
  "Q-07": {
    item: "WB-2.2", session: "7f3a", state: "open", card: "q-design", shape: "design",
    short: "Run output after the run, or live?",
    text: "Should the run page show output after the run ends, or stream it live?",
    blocks: "step 3 of 5", unblockMin: 130, asked: "6 Nov 04:51", waiting: "21 min",
    context: "Step 3 of [[item:WB-2.2]] renders a run's output in [[file:runpage:14-31]]. [[ruling:R-09]] keeps 40 lines on a card; [[run:1042]] printed 212 lines, so the page needs a rule for the rest.",
    code: "file:runpage:14-25",
    options: [
      { t: "After the run ends: last 40 lines, then Load all", rec: true, why: "Matches the card cap and needs no new endpoint.", support: ["ruling:R-09", "ruling:R-06"] },
      { t: "Stream live over the hook endpoint", why: "Needs a push channel per page; the Host would post every line.", support: ["file:hookendpoint:13-28", "ruling:R-10"] },
      { t: "Store all output and page through it, 200 lines at a time", why: "Grows the database; output above 64 KB is already capped.", support: ["comment:c3"] }
    ],
    cost: "small, one method (RunPage.LoadOutput)",
    tried: ["Rendered all 212 lines of run 1042: the page scrolled nine screens on the phone specimen.", "Tried a 40-line tail with Load all; the specimen passed, but it settles R-09's scope, so it asks."]
  },
  "Q-08": {
    item: "WB-2.2", session: "7f3a", state: "open", card: "q-unseen", shape: "unseen",
    short: "Exit code on RunRecord?",
    text: "Should RunRecord get an exit code for the result pill?",
    blocks: "step 4 of 5", unblockMin: 65, asked: "6 Nov 05:02", waiting: "10 min",
    context: "[[file:runrecord]] arrived in [[card:pk-runs]] and is new to you. Step 4 shows a result pill; [[type:RunResult]] has four values but no exit code, and [[run:1056]] ended with code 1.",
    code: "file:runrecord:11-22",
    unseenFile: "file:runrecord",
    options: [
      { t: "Add int? ExitCode, null for older rows", rec: true, why: "One property and one migration; old rows stay valid.", support: ["ruling:R-03", "run:1056"] },
      { t: "Derive the pill from RunResult only", why: "No schema change; loses the difference between a crash and a failed test.", support: ["type:RunResult"] },
      { t: "Parse the code out of Output", why: "No schema change; fragile, depends on each tool's wording.", support: ["ruling:R-06"] }
    ],
    cost: "small, one property and one migration",
    tried: ["Read RunRecord on the packet branch: no exit code is stored.", "Checked the Host payload: exit_code is present on PostToolUse for Bash calls."]
  },
  "Q-09": {
    item: "WB-2.3", session: "b915", state: "open", card: "q-comment", shape: "comment",
    short: "No sequence number for the + comment",
    text: "The comment asks to order events by the hook's sequence number, but the Host sends none. What should I do?",
    blocks: "step 3 of 4", unblockMin: 85, asked: "6 Nov 04:47", waiting: "25 min",
    context: "[[comment:c4]] on [[file:hookeventqueue:12-17]] asks for sequence order. [[machine:laptop]] and [[machine:desk-pc]] run Host 0.4.2, which posts session, turn and a timestamp ([[ruling:R-10]]) but no sequence.",
    code: "file:hookeventqueue:6-17",
    comment: "c4",
    options: [
      { t: "Add a per-session sequence in Host 0.5 and order by it", rec: true, why: "Fixes the clock skew for good; Host reinstall on three machines.", support: ["run:1056", "ruling:R-10"] },
      { t: "Order by session and turn, accept drift across sessions", why: "No Host change; two sessions can still interleave wrongly.", support: ["run:1053"] },
      { t: "Keep arrival order and record out-of-order events as findings", why: "Cheapest and matches errors are findings, but runs may list in the wrong order.", support: ["ruling:R-03"] }
    ],
    cost: "medium, the Host payload and one queue method",
    tried: ["Searched the Host 0.4.2 payload for a sequence field: none; only ts and turn.", "Ordered by turn within a session: passes for one machine, fails across two (run 1053)."]
  },
  "Q-01": { item: "WB-1", session: "51b0", state: "answered", short: "Which machine is home?", text: "When both machines post hello cards for one item, which is the item's home machine?", asked: "5 Nov 16:02", answered: "5 Nov 16:40", answer: "The machine whose worktree holds the branch. One home at a time; moving is a reassign.", ruling: "ruling:R-04" },
  "Q-02": { item: "WB-1", session: "51b0", state: "answered", short: "Notifications per card?", text: "Should the phone get one notification per card or one per batch?", asked: "5 Nov 17:10", answered: "5 Nov 17:14", answer: "One per blocking card. Info cards never notify.", ruling: null },
  "Q-03": { item: "WB-2.1", session: "c21e", state: "answered", short: "Where does run output live?", text: "Where does a run's output live: in the row, on disk, or both?", asked: "5 Nov 21:55", answered: "5 Nov 22:10", answer: "Last 40 lines on the card from the row (capped at 64 KB); the full log stays on the machine.", ruling: "ruling:R-09" },
  "Q-04": { item: "WB-2.1", session: "c21e", state: "answered", short: "Key runs by tool_use_id?", text: "Should a run be keyed by the hook's tool_use_id or by a hash of the command and time?", asked: "6 Nov 02:12", answered: "6 Nov 02:20", answer: "tool_use_id. A retried post must not make a second row.", ruling: null },
  "Q-10": { item: "FTR-2", session: null, state: "open", card: "q-f2a", shape: "design", short: "Do computed members cache?",
    text: "Should a computed member cache its value until a member it reads changes?",
    blocks: "step 4 of 6", unblockMin: 90, asked: "4 Nov 21:10", waiting: "1 day",
    context: "Step 4 of [[fpb:FTR-2]] emits computed members. Caching needs the observable core ([[item:FW-1]]); not caching recomputes on every read.",
    options: [
      { t: "Cache, and invalidate when a member it reads changes", rec: true, why: "Cheap reads in lists; needs the observable core.", support: ["item:FW-1", "ruling:R-14"] },
      { t: "Recompute on every read", why: "Simplest; slow in a 1,356-card rail.", support: ["fpb:PER-2"] }
    ],
    cost: "medium, the emitter for computed members", tried: ["Spike emitted both shapes for Movie.label; caching needed 14 more lines."] },
  "Q-11": { item: "FTR-2", session: null, state: "open", card: "q-f2b", shape: "design", short: "Defaults: constructor or initialiser?",
    text: "Should default values emit as constructor parameters or as property initialisers?",
    blocks: "step 5 of 6", unblockMin: 60, asked: "4 Nov 21:12", waiting: "1 day",
    context: "Quanta used initialisers. The TypeScript target has no initialisers on records, so the two targets would differ.",
    options: [
      { t: "Property initialisers in C#, constructor defaults in TypeScript", rec: true, why: "Each target reads naturally; the parity row stays 'same'.", support: ["ruling:R-06"] },
      { t: "Constructor parameters on both targets", why: "One shape; every Dope model's construction call changes.", support: [] }
    ],
    cost: "small, one emitter rule per target", tried: ["Spike used initialisers for C# only."] },
  "Q-12": { item: "BUG-2", session: "e5c1", state: "answered", short: "May recovery discard tokens?", text: "When recovery skips text, may it discard the tokens, or must they survive as trivia?", asked: "5 Nov 01:40", answered: "5 Nov 01:52", answer: "Never discard. Skipped text becomes trivia on the error node so the file prints back.", ruling: "ruling:R-13" },
  "Q-05": { item: "WB-2", session: "c21e", state: "answered", short: "Hook order in this slice?", text: "Is the out-of-order hook bug part of this slice or its own item?", asked: "5 Nov 22:58", answered: "5 Nov 23:05", answer: "Part of this slice, in its own lane B.", ruling: null }
};

/* ---------------------------------------------------------------- assumptions (14, two sessions) */
WB.assumptions = {
  a1: { session: "7f3a", item: "WB-2.2", state: "unreviewed", text: "The run page route is /runs/{id}, matching the item route shape." },
  a2: { session: "7f3a", item: "WB-2.2", state: "unreviewed", text: "Output uses the mono font at 13 px with no wrapping; long lines scroll inside the block." },
  a3: { session: "7f3a", item: "WB-2.2", state: "unreviewed", text: "The result pill reuses the status chip styles: good, warn, bad." },
  a4: { session: "7f3a", item: "WB-2.2", state: "unreviewed", text: "Durations read like 1m 54s, through RunDurationFormatter." },
  a5: { session: "7f3a", item: "WB-2.2", state: "unreviewed", text: "A run with no output shows 'no output captured' instead of an empty block." },
  a6: { session: "7f3a", item: "WB-2.2", state: "unreviewed", text: "Machine and session chips sit in the run page header, not the footer." },
  a7: { session: "7f3a", item: "WB-2.2", state: "unreviewed", text: "Times show in your zone (Lagos), not the machine's." },
  a8: { session: "7f3a", item: "WB-2.2", state: "unreviewed", text: "The command is shown verbatim, including dotnet flags." },
  a9: { session: "c21e", item: "WB-2.1", state: "fine", text: "Runs are keyed by the hook's tool_use_id, so a retried post makes no second row." },
  a10: { session: "c21e", item: "WB-2.1", state: "fine", text: "A check is a Bash call whose command matches the check list in CLAUDE.md." },
  a11: { session: "c21e", item: "WB-2.1", state: "fine", text: "Runs older than 30 days stay; retention is out of scope." },
  a12: { session: "c21e", item: "WB-2.1", state: "fine", text: "The Runs tab lists newest first." },
  a13: { session: "c21e", item: "WB-2.1", state: "unreviewed", text: "A run that times out records as Failed, with the timeout in Output." },
  a15: { session: "e5c1", item: "BUG-2", state: "unreviewed", text: "A comment inside an unterminated string belongs to the string, not to the trivia after it." },
  a14: { session: "c21e", item: "WB-2.1", state: "unreviewed", text: "A smoke run spanning two machines records once, under the machine that started it." }
};

/* ---------------------------------------------------------------- found items (4, in trays) */
WB.found = {
  f1: { kind: "BUG", title: "Inbox count lags one card after Approve", session: "c21e", item: "WB-2.1", state: "in tray", found: "6 Nov 03:20",
        why: "In the specimen the queue meter still said 7 after an approve; it caught up on the next poll.", evidence: "run:1043", suggest: "WB-3" },
  f2: { kind: "BUG", title: "Hook endpoint returns 500 on empty tool_input", session: "c21e", item: "WB-2.1", state: "in tray", found: "6 Nov 03:31",
        why: "Stop events from Host 0.4.2 carry tool_input {} and Parse throws. The open [[comment:c8]] asks for the same.", evidence: "file:hookendpoint:30-41", suggest: "WB-2.3" },
  f3: { kind: "PER", title: "Tree query takes 400 ms on 40 nodes", session: "7f3a", item: "WB-2.2", state: "promoted", promoted: "fpb:PER-1", found: "6 Nov 04:20",
        why: "Opening an item reloads the whole tree; [[run:1048]] timed 400 ms for 40 nodes on desk-pc.", evidence: "run:1048", suggest: "WB-4" },
  f5: { kind: "BUG", title: "Two tool calls in the same second drain out of order", session: "b915", item: "WB-2.3", state: "promoted", promoted: "fpb:BUG-1", found: "6 Nov 04:29",
        why: "Two PostToolUse events from one session in the same second came out reversed in [[run:1050]].", evidence: "run:1050", suggest: "WB-2.3" },
  f4: { kind: "FTR", title: "Copy link on a run row", session: "7f3a", item: "WB-2.2", state: "in tray", found: "6 Nov 04:44",
        why: "Sharing a run with a session needs its link; today only the page has one.", evidence: "file:runpage:1-12", suggest: "WB-2.4" }
};

/* ---------------------------------------------------------------- rulings (12) */
WB.rulings = {
  "R-01": { project: "wb", title: "Claude never creates structure", date: "1 Nov", neverReopened: true,
    text: "Projects and items are made by me. A session fills a few capped slots on the item it was given: a plan, questions, assumptions, found items, a note and run reports.",
    origin: "Set by you in the programme plan", question: null, item: null, related: ["ruling:R-02", "claudemd:slots"] },
  "R-02": { project: "wb", title: "One open node per level", date: "2 Nov", neverReopened: true,
    text: "At each level of the tree at most one node is open in a lane. Siblings stay one line with a coarse size until an approved plan opens them.",
    origin: "Set by you in the tooling plan", question: null, item: "item:WB-4", related: ["ruling:R-01", "ruling:R-07"] },
  "R-03": { project: "wb", title: "Errors are findings", date: "2 Nov", neverReopened: true,
    text: "Every error a session meets becomes a finding with a chip. Nothing is swallowed and nothing is retried silently.",
    origin: "Carried over from the Quanta Workbench", question: null, item: null, related: ["ruling:R-10"] },
  "R-04": { project: "wb", title: "A task has one home machine at a time", date: "5 Nov", neverReopened: true,
    text: "A task's worktree lives on one machine. Moving it is a reassign press, never a second checkout.",
    origin: "Answered on a question card", question: "question:Q-01", item: "item:WB-1", related: ["ruling:R-05", "machine:desk-pc"] },
  "R-05": { project: "wb", title: "Sessions carry out git only on a delivered press", date: "3 Nov", neverReopened: true,
    text: "Commit, merge and push happen only when I press and the Workbench delivers the exact instruction. What git reports is shown, never acted on.",
    origin: "Set by you in the tooling plan", question: null, item: null, related: ["claudemd:git", "ruling:R-04"] },
  "R-06": { project: "wb", title: "Code is pulled, never pasted", date: "3 Nov", neverReopened: true,
    text: "Cards show lines pulled from the file, about twelve at most, with open in Rider and the diff since master. Nobody pastes code into a card.",
    origin: "Set by you in the tooling plan", question: null, item: null, related: ["ruling:R-09"] },
  "R-07": { project: "wb", title: "Estimates in Claude-minutes, actuals from hooks", date: "4 Nov", neverReopened: true,
    text: "Every estimate is in Claude-minutes. Actual time comes from the session hooks, and each lane carries its own factor.",
    origin: "Set by you in design chat", question: null, item: null, related: ["ruling:R-02"] },
  "R-08": { project: "wb", title: "The Workbench reads + comments, never edits them", date: "4 Nov", neverReopened: true,
    text: "+ comments belong to the source file. The Workbench indexes and shows them; sessions write the handled note and I clear them.",
    origin: "Set by you in design chat", question: null, item: "item:WB-5", related: ["claudemd:comments"] },
  "R-09": { project: "wb", title: "Run cards keep the last 40 lines", date: "5 Nov", neverReopened: true,
    text: "A run shows its last 40 lines on a card, from a row capped at 64 KB. The full log stays on the machine.",
    origin: "Answered on a question card", question: "question:Q-03", item: "item:WB-2.1", related: ["ruling:R-06"] },
  "R-10": { project: "wb", title: "Hook events carry session, turn and Host time", date: "5 Nov", neverReopened: false,
    text: "Every hook event names its machine, session and turn and carries the Host's timestamp. The Model refuses an event without them.",
    origin: "A Model invariant, set by you", question: null, item: "item:WB-1", related: ["ruling:R-03", "question:Q-09"],
    reopenNote: "Q-09 may add a sequence number to it." },
  "R-11": { project: "dope", title: "Card verbs build on first tap", date: "30 Oct", neverReopened: true,
    text: "Library cards create their verb menus when first tapped, not when the page loads.",
    origin: "Imported with the Dope seed (Quanta finding)", question: null, item: "fpb:FTR-1", related: ["ruling:R-12"] },
  "R-12": { project: "dope", title: "The library opens in under 1 s cold", date: "30 Oct", neverReopened: true,
    text: "The Dope library page is open and scrollable in under 1 s, cold, on the laptop, with the reference library of 1,356 movies.",
    origin: "Imported with the Dope seed (Quanta bar)", question: null, item: "fpb:PER-2", related: ["ruling:R-11"] },
  "R-13": { project: "lang", title: "Recovery never discards tokens", date: "5 Nov", neverReopened: true,
    text: "When the parser recovers from an error, skipped text becomes trivia on the error node. Nothing typed is lost, so every file prints back.",
    origin: "Answered on a question card", question: "question:Q-12", item: "fpb:BUG-2", related: ["fpb:FTR-3"] },
  "R-14": { project: "lang", title: "Members opt in to observability", date: "4 Nov", neverReopened: true,
    text: "A class member raises change events only when it is marked observable. Nothing is observable by default.",
    origin: "Chosen among three options on the design", question: null, item: "fpb:FTR-2", related: ["item:FW-1", "ruling:R-15"] },
  "R-15": { project: "lang", title: "No implicit change events on fields", date: "4 Nov", neverReopened: true,
    text: "Quanta raised a change event on every field write. QDL drops that; only observable members notify.",
    origin: "Set by you on the parity table", question: null, item: "fpb:FTR-2", related: ["ruling:R-14"] }
};

/* Proposed rulings from a design conversation. The only path from chat into the store. */
WB.proposed = {
  chat: "Design chat · inbox deferral and clearing", when: "5 Nov 21:40", turns: 23,
  items: [
    { id: "P-1", text: "A deferred card returns to the inbox after 24 hours.", ticked: true },
    { id: "P-2", text: "Info cards clear themselves after seven days.", ticked: true },
    { id: "P-3", text: "A run without a machine chip is not evidence.", ticked: false },
    { id: "P-4", text: "The queue meter counts only Needs you cards.", ticked: true }
  ]
};

/* ---------------------------------------------------------------- files
   Lines are the file as it stands on the item's branch. "seen" is whether you have opened the current version;
   newLines is how many lines changed since you last did. added: new against master (diff is every line). */
WB.files = {
  runrecord: { path: "src/Qotoba.Workbench.Model/Runs/RunRecord.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Model", folder: "Runs", branch: "br-21",
    seen: false, newLines: 40, lastSeen: "6 Nov 01:45", added: true,
    lines: [
      "namespace Qotoba.Workbench.Model.Runs;",
      "",
      "using Qotoba.Workbench.Model.Items;",
      "using Qotoba.Workbench.Model.Sessions;",
      "",
      "/// <summary>How a check ended, as reported by the Host's PostToolUse hook.</summary>",
      "public enum RunResult { Passed, Failed, Warned, Skipped }",
      "",
      "//+ keep this immutable; the runs table never updates a row",
      "//+= handled 2026-11-06 · session c21e · record is sealed; the update path is removed",
      "public sealed record RunRecord",
      "{",
      "    public required RunId Id { get; init; }",
      "    public required ItemId Item { get; init; }",
      "    public required SessionId Session { get; init; }",
      "    public required MachineId Machine { get; init; }",
      "    public required string Check { get; init; }",
      "    public required string Command { get; init; }",
      "    public required RunResult Result { get; init; }",
      "    public required DateTimeOffset StartedAt { get; init; }",
      "",
      "    //+ store milliseconds as long so SQLite sorts it",
      "    public required TimeSpan Duration { get; init; }",
      "",
      "    public int? Passed { get; init; }",
      "    public int? Total { get; init; }",
      "",
      "    //+ cap at 64 KB, keep the tail",
      "    public string Output { get; init; } = \"\";",
      "",
      "    public string Summary => Total is int t && t > 0",
      "        ? $\"{Check} {Passed}/{t}\"",
      "        : $\"{Check} {Result.ToString().ToLowerInvariant()}\";",
      "",
      "    public bool Proves(ItemId item, DateTimeOffset since) =>",
      "        Item == item && Result == RunResult.Passed && StartedAt >= since;",
      "}",
      "",
      "public readonly record struct RunId(long Value)",
      "{ public override string ToString() => $\"run-{Value}\"; }"
    ] },

  hookeventqueue: { path: "src/Qotoba.Workbench.Service/Hooks/HookEventQueue.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Service", folder: "Hooks", branch: "br-23",
    seen: false, newLines: 14, lastSeen: "6 Nov 03:58",
    lines: [
      "namespace Qotoba.Workbench.Service.Hooks;",
      "",
      "using System.Threading.Channels;",
      "using Qotoba.Workbench.Model.Hooks;",
      "",
      "//+ one queue per machine, not global",
      "public sealed class HookEventQueue(IOptions<HookOptions> options, ILogger<HookEventQueue> log)",
      "{",
      "    private readonly Channel<HookEvent> _events = Channel.CreateUnbounded<HookEvent>();",
      "    private readonly List<HookEvent> _window = new();",
      "",
      "    //+ order events by the hook's sequence number, not arrival time",
      "    public ValueTask Enqueue(HookEvent e, CancellationToken ct)",
      "    {",
      "        log.LogDebug(\"hook {Kind} from {Machine} session {Session} turn {Turn}\", e.Kind, e.Machine, e.Session, e.Turn);",
      "        return _events.Writer.WriteAsync(e, ct);",
      "    }",
      "",
      "    //+ drain in batches of 50 so the tree query isn't starved",
      "    //+= handled 2026-11-06 · session c21e · batch size read from HookOptions.DrainBatch, default 50",
      "    public IReadOnlyList<HookEvent> Drain()",
      "    {",
      "        var batch = new List<HookEvent>(options.Value.DrainBatch);",
      "        while (batch.Count < options.Value.DrainBatch && _events.Reader.TryRead(out var e))",
      "            batch.Add(e);",
      "",
      "        _window.AddRange(batch);",
      "        var cutoff = DateTimeOffset.UtcNow - options.Value.ReorderWindow;",
      "        var ready = _window",
      "            .Where(x => x.HostTimestamp <= cutoff)",
      "            .OrderBy(x => x.Session).ThenBy(x => x.Turn).ThenBy(x => x.HostTimestamp)",
      "            .ToList();",
      "",
      "        _window.RemoveAll(ready.Contains);",
      "        return ready;",
      "    }",
      "",
      "    public int Pending => _events.Reader.Count + _window.Count;",
      "}"
    ],
    diff: [
      "@@ -18,9 +18,21 @@ public sealed class HookEventQueue",
      "     //+ drain in batches of 50 so the tree query isn't starved",
      "+    //+= handled 2026-11-06 · session c21e · batch size read from HookOptions.DrainBatch, default 50",
      "-    public async Task<IReadOnlyList<HookEvent>> Drain(CancellationToken ct)",
      "+    public IReadOnlyList<HookEvent> Drain()",
      "     {",
      "-        var batch = new List<HookEvent>();",
      "-        while (_events.Reader.TryRead(out var e))",
      "+        var batch = new List<HookEvent>(options.Value.DrainBatch);",
      "+        while (batch.Count < options.Value.DrainBatch && _events.Reader.TryRead(out var e))",
      "             batch.Add(e);",
      "-        return batch;",
      "+",
      "+        _window.AddRange(batch);",
      "+        var cutoff = DateTimeOffset.UtcNow - options.Value.ReorderWindow;",
      "+        var ready = _window",
      "+            .Where(x => x.HostTimestamp <= cutoff)",
      "+            .OrderBy(x => x.Session).ThenBy(x => x.Turn).ThenBy(x => x.HostTimestamp)",
      "+            .ToList();",
      "+",
      "+        _window.RemoveAll(ready.Contains);",
      "+        return ready;",
      "     }"
    ] },

  hookendpoint: { path: "src/Qotoba.Workbench.Service/Endpoints/HookEndpoint.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Service", folder: "Endpoints", branch: "br-23",
    seen: true, newLines: 0, lastSeen: "6 Nov 04:25",
    lines: [
      "namespace Qotoba.Workbench.Service.Endpoints;",
      "",
      "using Qotoba.Workbench.Model.Hooks;",
      "using Qotoba.Workbench.Service.Hooks;",
      "",
      "public static class HookEndpoint",
      "{",
      "    //+ route stays /hooks/v1 until Host 0.5",
      "    public const string Route = \"/hooks/v1\";",
      "",
      "    //+ reject events from machines without a token",
      "    //+= handled 2026-11-06 · session b915 · 401 plus a finding; proof HookEndpointTests.RejectsUnknownMachine",
      "    public static IEndpointRouteBuilder MapHookEndpoint(this IEndpointRouteBuilder app)",
      "    {",
      "        app.MapPost(Route, async (HttpRequest req, MachineTokens tokens, HookEventQueue queue, Findings findings, CancellationToken ct) =>",
      "        {",
      "            if (!tokens.TryResolve(req.Headers.Authorization, out var machine))",
      "            {",
      "                findings.Record(\"hook.unknown-machine\", req.Headers[\"X-Qotoba-Machine\"].ToString());",
      "                return Results.Unauthorized();",
      "            }",
      "",
      "            var e = await Parse(req, machine, ct);",
      "            await queue.Enqueue(e, ct);",
      "            return Results.Accepted();",
      "        });",
      "        return app;",
      "    }",
      "",
      "    //+ empty tool_input is valid, not a 500",
      "    private static async Task<HookEvent> Parse(HttpRequest req, MachineId machine, CancellationToken ct)",
      "    {",
      "        using var doc = await JsonDocument.ParseAsync(req.Body, cancellationToken: ct);",
      "        var root = doc.RootElement;",
      "        return new HookEvent(",
      "            Machine: machine,",
      "            Session: new SessionId(root.GetProperty(\"session_id\").GetString()!),",
      "            Turn: root.GetProperty(\"turn\").GetInt32(),",
      "            Kind: Enum.Parse<HookKind>(root.GetProperty(\"hook_event_name\").GetString()!),",
      "            HostTimestamp: root.GetProperty(\"ts\").GetDateTimeOffset(),",
      "            ToolInput: root.GetProperty(\"tool_input\").GetRawText());",
      "    }",
      "}"
    ],
    diff: [
      "@@ -9,10 +9,18 @@ public static class HookEndpoint",
      "     //+ reject events from machines without a token",
      "+    //+= handled 2026-11-06 · session b915 · 401 plus a finding; proof HookEndpointTests.RejectsUnknownMachine",
      "     public static IEndpointRouteBuilder MapHookEndpoint(this IEndpointRouteBuilder app)",
      "     {",
      "-        app.MapPost(Route, async (HttpRequest req, HookEventQueue queue, CancellationToken ct) =>",
      "+        app.MapPost(Route, async (HttpRequest req, MachineTokens tokens, HookEventQueue queue, Findings findings, CancellationToken ct) =>",
      "         {",
      "-            var e = await Parse(req, MachineId.FromHeader(req), ct);",
      "+            if (!tokens.TryResolve(req.Headers.Authorization, out var machine))",
      "+            {",
      "+                findings.Record(\"hook.unknown-machine\", req.Headers[\"X-Qotoba-Machine\"].ToString());",
      "+                return Results.Unauthorized();",
      "+            }",
      "+",
      "+            var e = await Parse(req, machine, ct);",
      "             await queue.Enqueue(e, ct);"
    ] },

  hookevent: { path: "src/Qotoba.Workbench.Model/Hooks/HookEvent.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Model", folder: "Hooks", branch: "br-1",
    seen: true, newLines: 0, lastSeen: "5 Nov 18:35",
    lines: [
      "namespace Qotoba.Workbench.Model.Hooks;",
      "",
      "/// <summary>One event posted by a Host. Invariant (R-10): machine, session, turn and Host time are required.</summary>",
      "public sealed record HookEvent(",
      "    MachineId Machine,",
      "    SessionId Session,",
      "    int Turn,",
      "    HookKind Kind,",
      "    DateTimeOffset HostTimestamp,",
      "    string ToolInput,",
      "    int Order = 0);",
      "",
      "public enum HookKind { SessionStart, PreToolUse, PostToolUse, Stop, Notification }"
    ] },

  runstable: { path: "src/Qotoba.Workbench.Service/Runs/RunsTable.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Service", folder: "Runs", branch: "br-21",
    seen: true, newLines: 0, lastSeen: "6 Nov 04:02", added: true,
    lines: [
      "namespace Qotoba.Workbench.Service.Runs;",
      "",
      "using Qotoba.Workbench.Model.Runs;",
      "",
      "public sealed class RunsTable(WorkbenchDb db)",
      "{",
      "    public async Task Insert(RunRecord run, CancellationToken ct)",
      "    {",
      "        // keyed by the hook's tool_use_id: a retried post is a no-op (Q-04)",
      "        await db.Runs.Upsert(run).On(r => r.Id).NoUpdate().RunAsync(ct);",
      "    }",
      "",
      "    public Task<RunRecord?> Get(RunId id, CancellationToken ct) =>",
      "        db.Runs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);",
      "",
      "    public Task<List<RunRecord>> ForItem(ItemId item, CancellationToken ct) =>",
      "        db.Runs.AsNoTracking().Where(r => r.Item == item)",
      "            .OrderByDescending(r => r.StartedAt).Take(200).ToListAsync(ct);",
      "",
      "    public Task<List<RunRecord>> LatestPerCheck(ItemId item, CancellationToken ct) =>",
      "        db.Runs.AsNoTracking().Where(r => r.Item == item)",
      "            .GroupBy(r => r.Check)",
      "            .Select(g => g.OrderByDescending(r => r.StartedAt).First())",
      "            .ToListAsync(ct);",
      "}"
    ] },

  runduration: { path: "src/Qotoba.Workbench.Service/Runs/RunDurationFormatter.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Service", folder: "Runs", branch: "br-21",
    seen: false, newLines: 12, lastSeen: null, added: true,
    lines: [
      "namespace Qotoba.Workbench.Service.Runs;",
      "",
      "/// <summary>Formats run durations the way cards show them: 48 s, 1m 54s, 2h 05m.</summary>",
      "public static class RunDurationFormatter",
      "{",
      "    public static string Format(TimeSpan d) => d switch",
      "    {",
      "        { TotalSeconds: < 60 } => $\"{d.TotalSeconds:0} s\",",
      "        { TotalMinutes: < 60 } => $\"{(int)d.TotalMinutes}m {d.Seconds:00}s\",",
      "        _ => $\"{(int)d.TotalHours}h {d.Minutes:00}m\",",
      "    };",
      "}"
    ] },

  runstab: { path: "src/Qotoba.Workbench.Web/Items/RunsTab.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Web", folder: "Items", branch: "br-21",
    seen: true, newLines: 0, lastSeen: "6 Nov 04:03",
    lines: [
      "namespace Qotoba.Workbench.Web.Items;",
      "",
      "using Qotoba.Workbench.Service.Runs;",
      "",
      "public sealed class RunsTab(RunsTable runs, ChipRenderer chips) : ItemTab(\"Runs\")",
      "{",
      "    public override async Task<TabView> Render(ItemId item, CancellationToken ct)",
      "    {",
      "        var rows = await runs.ForItem(item, ct);",
      "        if (rows.Count == 0)",
      "            return TabView.Empty(\"Runs appear here when a session runs a check on this item.\");",
      "",
      "        return TabView.Table(",
      "            [\"Check\", \"Result\", \"Duration\", \"Machine\", \"Session\", \"Time\"],",
      "            rows.Select(r => new Row(",
      "                r.Summary, r.Result.ToPill(), RunDurationFormatter.Format(r.Duration),",
      "                chips.Machine(r.Machine), chips.Session(r.Session), r.StartedAt.InOwnerZone().ToString(\"d MMM HH:mm\"))",
      "            { Link = $\"/runs/{r.Id.Value}\" }));",
      "    }",
      "}"
    ],
    diff: [
      "@@ -4,9 +4,16 @@",
      "-public sealed class RunsTab : ItemTab(\"Runs\")",
      "+public sealed class RunsTab(RunsTable runs, ChipRenderer chips) : ItemTab(\"Runs\")",
      " {",
      "-    public override Task<TabView> Render(ItemId item, CancellationToken ct) =>",
      "-        Task.FromResult(TabView.Empty(\"No runs yet.\"));",
      "+    public override async Task<TabView> Render(ItemId item, CancellationToken ct)",
      "+    {",
      "+        var rows = await runs.ForItem(item, ct);",
      "+        if (rows.Count == 0)",
      "+            return TabView.Empty(\"Runs appear here when a session runs a check on this item.\");"
    ] },

  runstabletests: { path: "tests/Qotoba.Workbench.Service.Tests/Runs/RunsTableTests.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Service.Tests", folder: "Runs", branch: "br-21",
    seen: true, newLines: 0, lastSeen: "6 Nov 04:04", added: true,
    lines: [
      "namespace Qotoba.Workbench.Service.Tests.Runs;",
      "",
      "public sealed class RunsTableTests(DbFixture db) : IClassFixture<DbFixture>",
      "{",
      "    [Fact]",
      "    public async Task RetriedPostMakesOneRow()",
      "    {",
      "        var run = Sample.Run(check: \"tests\", toolUseId: \"toolu_01Hk7\");",
      "        await db.Runs.Insert(run, default);",
      "        await db.Runs.Insert(run with { Output = \"again\" }, default);",
      "        Assert.Single(await db.Runs.ForItem(run.Item, default));",
      "    }",
      "",
      "    [Fact]",
      "    public async Task LatestPerCheckPicksNewest()",
      "    {",
      "        var item = Sample.Item(\"WB-2.1\");",
      "        await db.Runs.Insert(Sample.Run(item, \"tests\", at: \"03:30\"), default);",
      "        await db.Runs.Insert(Sample.Run(item, \"tests\", at: \"03:44\"), default);",
      "        var latest = await db.Runs.LatestPerCheck(item, default);",
      "        Assert.Equal(\"03:44\", latest.Single().StartedAt.ToString(\"HH:mm\"));",
      "    }",
      "}"
    ] },

  runpage: { path: "src/Qotoba.Workbench.Web/Runs/RunPage.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Web", folder: "Runs", branch: "br-22",
    seen: true, newLines: 0, lastSeen: "6 Nov 04:52", added: true,
    lines: [
      "namespace Qotoba.Workbench.Web.Runs;",
      "",
      "using Qotoba.Workbench.Model.Runs;",
      "using Qotoba.Workbench.Web.Chips;",
      "",
      "[Route(\"/runs/{id:long}\")]",
      "public sealed class RunPage(RunsTable runs, ChipRenderer chips) : WorkbenchPage",
      "{",
      "    public RunRecord Run { get; private set; } = default!;",
      "",
      "    public override async Task Load(long id, CancellationToken ct) =>",
      "        Run = await runs.Get(new RunId(id), ct) ?? throw new NotFound($\"run-{id}\");",
      "",
      "    public Header Header => new(",
      "        Title: Run.Check,",
      "        Pill: Run.Result.ToPill(),",
      "        Chips: [chips.Machine(Run.Machine), chips.Session(Run.Session), chips.Item(Run.Item)],",
      "        Meta: $\"{RunDurationFormatter.Format(Run.Duration)} · {Run.StartedAt.InOwnerZone():d MMM HH:mm}\");",
      "",
      "    public const int TailLines = 40;",
      "",
      "    // Step 3 waits on Q-07: after the run, or live?",
      "    public IReadOnlyList<string> Tail =>",
      "        Run.Output.Split('\\n').TakeLast(TailLines).ToArray();",
      "",
      "    public bool HasMore => Run.Output.Count(c => c == '\\n') >= TailLines;",
      "",
      "    public string Command => Run.Command;",
      "",
      "    public string Empty => \"no output captured\";",
      "}"
    ] },

  runpagetests: { path: "tests/Qotoba.Workbench.Web.Tests/Runs/RunPageTests.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Web.Tests", folder: "Runs", branch: "br-22",
    seen: true, newLines: 0, lastSeen: "6 Nov 04:52", added: true,
    lines: [
      "namespace Qotoba.Workbench.Web.Tests.Runs;",
      "",
      "public sealed class RunPageTests(WebFixture web) : IClassFixture<WebFixture>",
      "{",
      "    [Fact]",
      "    public async Task HeaderShowsMachineAndSessionChips()",
      "    {",
      "        var page = await web.Open<RunPage>(\"/runs/1042\");",
      "        Assert.Equal([\"machine:laptop\", \"session:c21e\", \"item:WB-2.1\"], page.Header.Chips.Select(c => c.Ref));",
      "    }",
      "",
      "    [Fact]",
      "    public async Task TailKeepsLastFortyLines()",
      "    {",
      "        var page = await web.Open<RunPage>(\"/runs/1042\");",
      "        Assert.Equal(40, page.Tail.Count);",
      "        Assert.True(page.HasMore);",
      "    }",
      "}"
    ] },

  hookorderingtests: { path: "tests/Qotoba.Workbench.Service.Tests/Hooks/HookOrderingTests.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Service.Tests", folder: "Hooks", branch: "br-23",
    seen: false, newLines: 61, lastSeen: null, added: true,
    lines: [
      "namespace Qotoba.Workbench.Service.Tests.Hooks;",
      "",
      "using Qotoba.Workbench.Model.Hooks;",
      "using Qotoba.Workbench.Service.Hooks;",
      "",
      "/// <summary>WB-2.3: events from two machines must drain in the order they happened.</summary>",
      "public sealed class HookOrderingTests",
      "{",
      "    private static readonly MachineId Desk = new(\"desk-pc\");",
      "    private static readonly MachineId Laptop = new(\"laptop\");",
      "",
      "    private static HookEventQueue Queue(TimeSpan window) => new(",
      "        Options.Create(new HookOptions { DrainBatch = 50, ReorderWindow = window }),",
      "        NullLogger<HookEventQueue>.Instance);",
      "",
      "    [Fact]",
      "    public async Task OneMachineKeepsTurnOrder()",
      "    {",
      "        var q = Queue(TimeSpan.Zero);",
      "        for (var turn = 1; turn <= 20; turn++)",
      "            await q.Enqueue(Sample.Event(Desk, \"7f3a\", turn), default);",
      "",
      "        var drained = q.Drain();",
      "        Assert.Equal(Enumerable.Range(1, 20), drained.Select(e => e.Turn));",
      "    }",
      "",
      "    [Fact]",
      "    public void EmptyQueueDrainsNothing()",
      "    {",
      "        var q = Queue(TimeSpan.Zero);",
      "        Assert.Empty(q.Drain());",
      "        Assert.Equal(0, q.Pending);",
      "    }",
      "",
      "    // Two sessions on two machines, interleaved in real time.",
      "    // The events carry Order only in the test; the Host does not send it yet (Q-09).",
      "    // laptop's clock runs 1.8 s behind desk-pc (Host heartbeat, 6 Nov 04:30).",
      "    [Fact]",
      "    public async Task TwoMachinesInterleave()",
      "    {",
      "        var q = Queue(TimeSpan.FromSeconds(2));",
      "        var clock = new FakeClock(\"2026-11-06T04:36:00Z\");",
      "        var expected = new List<int>();",
      "",
      "        for (var n = 1; n <= 40; n++)",
      "        {",
      "            var fromLaptop = n % 3 == 1;",
      "            var e = fromLaptop",
      "                ? Sample.Event(Laptop, \"b915\", n, clock.Now - TimeSpan.FromSeconds(1.8), order: n)",
      "                : Sample.Event(Desk, \"7f3a\", n, clock.Now, order: n);",
      "            await q.Enqueue(e, default);",
      "            expected.Add(n);",
      "            clock.Advance(TimeSpan.FromMilliseconds(300));",
      "        }",
      "",
      "        clock.Advance(TimeSpan.FromSeconds(3));",
      "        var drained = q.Drain();",
      "        Assert.Equal(expected, drained.Select(e => e.Order));",
      "    }",
      "}",
      ""
    ] },

  summaryblock: { path: "src/Qotoba.Workbench.Web/Items/SummaryBlock.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Web", folder: "Items", branch: "br-1",
    seen: true, newLines: 0, lastSeen: "5 Nov 19:20",
    lines: [
      "namespace Qotoba.Workbench.Web.Items;",
      "",
      "/// <summary>The fixed shape every item is read in.</summary>",
      "public sealed class SummaryBlock(ItemView item, ChipRenderer chips)",
      "{",
      "    public Row Goal => Row.Text(\"Goal\", item.Goal);",
      "    public Row Steps => Row.Ticks(\"Steps\", item.Steps, s => chips.Proof(s.Proof));",
      "",
      "    // WB-2.4: checks become chips from the latest run per check",
      "    public Row Checks => Row.Text(\"Checks\", string.Join(\", \", item.ClaimedChecks));",
      "",
      "    public Row Changed => Row.Chips(\"Changed\", item.Git.Files.Select(chips.File));",
      "    public Row Time => Row.Text(\"Time\", item.Time.ToSummary());",
      "    public Row Open => Row.Text(\"Open\", $\"questions {item.OpenQuestions}/{Budgets.QuestionsPerItem}\");",
      "    public Row Note => Row.Note(item.Note, maxLines: Budgets.NoteLines);",
      "}"
    ] },

  hellocard: { path: "src/Qotoba.Workbench.Web/Inbox/HelloCard.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Web", folder: "Inbox", branch: "br-1",
    seen: true, newLines: 0, lastSeen: "5 Nov 18:36", added: true,
    lines: [
      "namespace Qotoba.Workbench.Web.Inbox;",
      "",
      "public sealed record HelloCard(MachineId Machine, SessionId Session, DateTimeOffset At) : InfoCard",
      "{",
      "    public override string Headline => $\"Hello from {Machine}\";",
      "    public override IEnumerable<Chip> Chips => [Chip.Machine(Machine), Chip.Session(Session)];",
      "    public override string Origin => $\"Host on {Machine} · {At.InOwnerZone():d MMM HH:mm}\";",
      "}"
    ] },

  treequery: { path: "src/Qotoba.Workbench.Service/Tree/TreeQuery.cs", repo: "Qotoba", proj: "Qotoba.Workbench.Service", folder: "Tree", branch: "br-1",
    seen: true, newLines: 0, lastSeen: "4 Nov 22:10",
    lines: [
      "namespace Qotoba.Workbench.Service.Tree;",
      "",
      "public sealed class TreeQuery(WorkbenchDb db)",
      "{",
      "    // Loads the whole programme on every item open (found f3: 400 ms on 40 nodes).",
      "    public async Task<TreeNode> Load(CancellationToken ct)",
      "    {",
      "        var items = await db.Items.AsNoTracking()",
      "            .Include(i => i.Steps).Include(i => i.Questions).Include(i => i.Runs)",
      "            .ToListAsync(ct);",
      "        return TreeNode.Build(items);",
      "    }",
      "}"
    ] },

  claudemd: { path: "CLAUDE.md", repo: "Qotoba", proj: "(repo root)", folder: "", branch: "br-1",
    seen: true, newLines: 0, lastSeen: "5 Nov 18:40", lang: "md",
    lines: [
      "# Qotoba · how sessions work here",
      "",
      "## Slots you may fill",
      "You never create projects or items. On the item you were given you may submit:",
      "- a plan, 7 steps at most, each with an estimate in Claude-minutes and the check that proves it",
      "- questions, 2 open per item; a third is refused until one is answered",
      "- assumptions, as many as you need, sent in batches",
      "- found items, 5 per session; they go to the item's tray",
      "- a note, 2 lines",
      "- run reports, which the hook sends for you",
      "",
      "## How to ask",
      "One sentence. Options with the recommended one first, each with a one-line reason.",
      "Say what it costs to change later. Quote code by file and line; never paste it.",
      "",
      "## + comments",
      "Lines starting //+ are instructions from the owner. Do what they ask within the item's scope,",
      "then write //+= handled <date> · session <id> · <note> under them. Never delete one.",
      "If you cannot do what a comment asks, ask one question that quotes it.",
      "",
      "## Checks",
      "build: dotnet build Qotoba.sln -warnaserror",
      "tests: dotnet test (the test project nearest the change)",
      "specimens: qwb specimens --item <id>",
      "round-trip: qwb check round-trip",
      "smoke hello-card: qwb smoke hello-card",
      "",
      "## Git",
      "Git runs only on a delivered press. Never commit, merge or push on your own.",
      "What git reports is shown to the owner, never acted on."
    ] },

  librarycardrail: { path: "src/Dope/Library/LibraryCardRail.cs", repo: "Dope", proj: "Dope", folder: "Library", branch: null,
    seen: true, newLines: 0, lastSeen: "30 Oct 01:20",
    lines: [
      "namespace Dope.Library;",
      "",
      "public sealed class LibraryCardRail : VirtualRail<MovieCard>",
      "{",
      "    public const int PageSize = 60;",
      "    public const int Realised = 24;",
      "",
      "    protected override IQuery<MovieCard> Query(LibraryFilter f) =>",
      "        Movies.Where(f).OrderBy(m => m.SortTitle).Page(PageSize);",
      "",
      "    protected override MovieCard Realise(Movie m) => new(m)",
      "    {",
      "        Poster = Posters.DecodeOffThread(m.PosterPath, cache: 120),",
      "        Verbs = Lazy.Verbs(m),   // R-11: built on first tap",
      "    };",
      "}"
    ],
    diff: [
      "@@ -1,14 +1,16 @@",
      "-public sealed class LibraryCardRail : StackPanel<MovieCard>",
      "+public sealed class LibraryCardRail : VirtualRail<MovieCard>",
      " {",
      "-    protected override IEnumerable<MovieCard> Items => Movies.All().Select(m => new MovieCard(m));",
      "+    public const int PageSize = 60;",
      "+    public const int Realised = 24;",
      "+",
      "+    protected override IQuery<MovieCard> Query(LibraryFilter f) =>",
      "+        Movies.Where(f).OrderBy(m => m.SortTitle).Page(PageSize);"
    ] },

  cardverbs: { path: "src/Dope/Library/CardVerbs.cs", repo: "Dope", proj: "Dope", folder: "Library", branch: null,
    seen: true, newLines: 0, lastSeen: "30 Oct 01:42",
    lines: [
      "namespace Dope.Library;",
      "",
      "public static class Lazy",
      "{",
      "    public static Lazy<VerbMenu> Verbs(Movie m) => new(() => VerbMenu.For(m,",
      "        Verb.Play, Verb.Trailer, Verb.MarkWatched.WithUndo(),",
      "        Verb.AddToCollection.WithConfirm(), Verb.Remove.WithConfirm().WithUndo()));",
      "}"
    ],
    diff: [
      "@@ -1,9 +1,8 @@",
      "-public static class CardVerbs",
      "+public static class Lazy",
      " {",
      "-    public static VerbMenu Build(Movie m) => VerbMenu.For(m,",
      "+    public static Lazy<VerbMenu> Verbs(Movie m) => new(() => VerbMenu.For(m,",
      "         Verb.Play, Verb.Trailer, Verb.MarkWatched.WithUndo(),",
      "-        Verb.AddToCollection.WithConfirm(), Verb.Remove.WithConfirm().WithUndo());",
      "+        Verb.AddToCollection.WithConfirm(), Verb.Remove.WithConfirm().WithUndo()));"
    ] }
};

/* ---------------------------------------------------------------- types */
WB.types = {
  RunRecord: { kind: "sealed record", ns: "Qotoba.Workbench.Model.Runs", at: "file:runrecord:11-37", seen: false, newIn: "card:pk-runs", item: "WB-2.1",
    summary: "One check run by a session on a machine. Immutable; the runs table never updates a row." },
  RunResult: { kind: "enum", ns: "Qotoba.Workbench.Model.Runs", at: "file:runrecord:6-7", seen: false, newIn: "card:pk-runs", item: "WB-2.1",
    summary: "Passed, Failed, Warned, Skipped. No exit code ([[question:Q-08]])." },
  RunId: { kind: "record struct", ns: "Qotoba.Workbench.Model.Runs", at: "file:runrecord:39-40", seen: false, newIn: "card:pk-runs", item: "WB-2.1",
    summary: "A run's number; prints as run-1042." },
  RunsTable: { kind: "class", ns: "Qotoba.Workbench.Service.Runs", at: "file:runstable:5-25", seen: true, newIn: "card:pk-runs", item: "WB-2.1",
    summary: "Insert, get, list per item and latest per check." },
  RunDurationFormatter: { kind: "static class", ns: "Qotoba.Workbench.Service.Runs", at: "file:runduration:4-12", seen: false, newIn: "card:pk-runs", item: "WB-2.1",
    summary: "Formats 48 s, 1m 54s, 2h 05m. Built beyond the plan; justified in the packet.", beyondPlan: true },
  RunPage: { kind: "class", ns: "Qotoba.Workbench.Web.Runs", at: "file:runpage:6-31", seen: true, newIn: null, item: "WB-2.2",
    summary: "The run page at /runs/{id}: header, tail, command." },
  HookEvent: { kind: "sealed record", ns: "Qotoba.Workbench.Model.Hooks", at: "file:hookevent:4-11", seen: true, newIn: "card:pk-hello", item: "WB-1",
    summary: "One Host event. Machine, session, turn and Host time are required ([[ruling:R-10]])." },
  HookEventQueue: { kind: "class", ns: "Qotoba.Workbench.Service.Hooks", at: "file:hookeventqueue:6-39", seen: false, newIn: null, item: "WB-2.3",
    summary: "Buffers hook events and drains them in batches; where the ordering bug lives." }
};

/* Code that a plan names but nobody has written yet: rendered as dashed chips. */
WB.nyw = {
  RunOutputBlock: { title: "RunOutputBlock", where: "src/Qotoba.Workbench.Web/Runs/RunOutputBlock.cs", step: "step 3 of [[item:WB-2.2]]" },
  SummaryChecks: { title: "SummaryChecks", where: "src/Qotoba.Workbench.Web/Items/SummaryChecks.cs", step: "step 2 of [[card:pl-24]]" },
  "smoke-slice": { title: "smoke · task-card slice", where: "qwb smoke task-card --item WB-2", step: "step 5 of [[item:WB-2]]" },
  "smoke-runpage": { title: "smoke · run page from hello card", where: "qwb smoke run-page", step: "step 5 of [[item:WB-2.2]]" },
  "HookEvent.Sequence": { title: "HookEvent.Sequence", where: "src/Qotoba.Workbench.Model/Hooks/HookEvent.cs", step: "step 3 of [[item:WB-2.3]], after [[question:Q-09]]" },
  HookOrderingRegression: { title: "tests · 500 events ordered", where: "HookOrderingTests.FiveHundredEventsTwoMachines", step: "step 4 of [[item:WB-2.3]]" }
};

/* ---------------------------------------------------------------- + comments (9 in 3 files) */
WB.comments = {
  c1: { file: "runrecord", line: 9, symbol: "RunRecord", symbolKind: "record", text: "keep this immutable; the runs table never updates a row", state: "handled", item: "WB-2.1",
    handled: { date: "2026-11-06", time: "02:31", session: "c21e", note: "record is sealed; the update path is removed", proof: "commit:3be07f1" },
    versions: [{ date: "6 Nov 01:40", text: "keep this immutable" }, { date: "6 Nov 01:42", text: "keep this immutable; the runs table never updates a row" }] },
  c2: { file: "runrecord", line: 22, symbol: "RunRecord.Duration", symbolKind: "property", text: "store milliseconds as long so SQLite sorts it", state: "stale", item: "WB-2.2",
    staleWhy: "The line under it changed from long DurationMs to TimeSpan Duration in [[commit:7c4e9a1]] after you wrote it. Re-read the code, then keep, rewrite or clear the comment.",
    versions: [{ date: "6 Nov 01:44", text: "store milliseconds as long so SQLite sorts it", above: "public required long DurationMs { get; init; }" }] },
  c3: { file: "runrecord", line: 28, symbol: "RunRecord.Output", symbolKind: "property", text: "cap at 64 KB, keep the tail", state: "open", item: "WB-2.2",
    versions: [{ date: "6 Nov 01:45", text: "cap at 64 KB, keep the tail" }] },
  c4: { file: "hookeventqueue", line: 12, symbol: "HookEventQueue.Enqueue", symbolKind: "method", text: "order events by the hook's sequence number, not arrival time", state: "open", item: "WB-2.3",
    question: "question:Q-09",
    versions: [{ date: "4 Nov 23:10", text: "order events by sequence number" }, { date: "5 Nov 23:02", text: "order events by the hook's sequence number, not arrival time" }] },
  c5: { file: "hookeventqueue", line: 19, symbol: "HookEventQueue.Drain", symbolKind: "method", text: "drain in batches of 50 so the tree query isn't starved", state: "handled", item: "WB-2.1",
    handled: { date: "2026-11-06", time: "03:05", session: "c21e", note: "batch size read from HookOptions.DrainBatch, default 50", proof: "commit:8c2d5a0" },
    versions: [{ date: "5 Nov 23:04", text: "drain in batches of 50 so the tree query isn't starved" }] },
  c6: { file: "hookeventqueue", line: 6, symbol: "HookEventQueue", symbolKind: "class", text: "one queue per machine, not global", state: "open", item: "WB-2.3",
    versions: [{ date: "5 Nov 23:00", text: "one queue per machine, not global" }] },
  c7: { file: "hookendpoint", line: 11, symbol: "HookEndpoint.MapHookEndpoint", symbolKind: "method", text: "reject events from machines without a token", state: "handled", item: "WB-2.3",
    handled: { date: "2026-11-06", time: "04:18", session: "b915", note: "401 plus a finding; proof HookEndpointTests.RejectsUnknownMachine", proof: "commit:51f9e07" },
    versions: [{ date: "5 Nov 16:15", text: "reject unknown machines" }, { date: "5 Nov 18:48", text: "reject events from machines without a token" }] },
  c8: { file: "hookendpoint", line: 30, symbol: "HookEndpoint.Parse", symbolKind: "method", text: "empty tool_input is valid, not a 500", state: "open", item: "WB-2.3",
    versions: [{ date: "5 Nov 18:50", text: "empty tool_input is valid, not a 500" }] },
  c9: { file: "hookendpoint", line: 8, symbol: "HookEndpoint.Route", symbolKind: "constant", text: "route stays /hooks/v1 until Host 0.5", state: "open", item: "WB-2.3",
    versions: [{ date: "5 Nov 16:20", text: "route stays /hooks/v1 until Host 0.5" }] }
};

/* ---------------------------------------------------------------- commits and branches */
WB.commits = {
  a41c9e2: { msg: "Merge WB-1 Hello card across two machines", date: "5 Nov 18:44", session: "51b0", branch: "br-1", files: 6, plus: 79, minus: 0,
    diff: ["+public sealed record HelloCard(MachineId Machine, SessionId Session, DateTimeOffset At) : InfoCard", "+{", "+    public override string Headline => $\"Hello from {Machine}\";", "+    public override IEnumerable<Chip> Chips => [Chip.Machine(Machine), Chip.Session(Session)];", "+    public override string Origin => $\"Host on {Machine} · {At.InOwnerZone():d MMM HH:mm}\";", "+}"] },
  "3be07f1": { msg: "RunRecord sealed; drop the update path", date: "6 Nov 02:31", session: "c21e", branch: "br-21", files: 2, plus: 18, minus: 9, proves: "c1",
    diff: ["-public class RunRecord", "+//+= handled 2026-11-06 · session c21e · record is sealed; the update path is removed", "+public sealed record RunRecord", "-    public void Update(RunResult result, string output) => (Result, Output) = (result, output);", "-    public RunId Id { get; set; }", "+    public required RunId Id { get; init; }"] },
  "7c4e9a1": { msg: "RunRecord: Duration as TimeSpan", date: "6 Nov 03:10", session: "c21e", branch: "br-21", files: 3, plus: 6, minus: 6, stales: "c2",
    diff: ["     //+ store milliseconds as long so SQLite sorts it", "-    public required long DurationMs { get; init; }", "+    public required TimeSpan Duration { get; init; }", "@@ RunsTab.cs @@", "-                $\"{r.DurationMs} ms\",", "+                RunDurationFormatter.Format(r.Duration),"] },
  "8c2d5a0": { msg: "Drain hook events in batches of HookOptions.DrainBatch", date: "6 Nov 03:05", session: "c21e", branch: "br-21", files: 2, plus: 9, minus: 4, proves: "c5",
    diff: ["+    //+= handled 2026-11-06 · session c21e · batch size read from HookOptions.DrainBatch, default 50", "-        var batch = new List<HookEvent>();", "-        while (_events.Reader.TryRead(out var e))", "+        var batch = new List<HookEvent>(options.Value.DrainBatch);", "+        while (batch.Count < options.Value.DrainBatch && _events.Reader.TryRead(out var e))", "             batch.Add(e);"] },
  "51f9e07": { msg: "Reject hook events from machines without a token", date: "6 Nov 04:18", session: "b915", branch: "br-23", files: 2, plus: 21, minus: 2, proves: "c7",
    diff: ["+    //+= handled 2026-11-06 · session b915 · 401 plus a finding; proof HookEndpointTests.RejectsUnknownMachine", "+            if (!tokens.TryResolve(req.Headers.Authorization, out var machine))", "+            {", "+                findings.Record(\"hook.unknown-machine\", req.Headers[\"X-Qotoba-Machine\"].ToString());", "+                return Results.Unauthorized();", "+            }"] },
  "6d02b14": { msg: "Run page header with machine and session chips", date: "6 Nov 04:38", session: "7f3a", branch: "br-22", files: 2, plus: 57, minus: 0,
    diff: ["+    public Header Header => new(", "+        Title: Run.Check,", "+        Pill: Run.Result.ToPill(),", "+        Chips: [chips.Machine(Run.Machine), chips.Session(Run.Session), chips.Item(Run.Item)],", "+        Meta: $\"{RunDurationFormatter.Format(Run.Duration)} · {Run.StartedAt.InOwnerZone():d MMM HH:mm}\");", "+    public const int TailLines = 40;"] }
};

WB.branches = {
  "br-1": { name: "wb/1-hello-card", item: "WB-1", machine: "desk-pc", state: "merged", ahead: 0, behind: 0, pushed: true, merged: "commit:a41c9e2", worktree: "removed 5 Nov 18:45" },
  "br-21": { name: "wb/2.1-runs-table", item: "WB-2.1", machine: "laptop", state: "pushed", ahead: 4, behind: 0, pushed: true, worktree: "C:\\src\\Qotoba-wt\\wb-2.1-runs-table", commits: ["commit:3be07f1", "commit:8c2d5a0", "commit:7c4e9a1"] },
  "br-22": { name: "wb/2.2-run-page", item: "WB-2.2", machine: "desk-pc", state: "local", ahead: 2, behind: 1, pushed: false, worktree: "D:\\Development\\repos\\Qotoba-wt\\wb-2.2-run-page", commits: ["commit:6d02b14"] },
  "br-23": { name: "wb/2.3-hook-order", item: "WB-2.3", machine: "laptop", state: "local · uncommitted changes", ahead: 3, behind: 1, pushed: false, worktree: "C:\\src\\Qotoba-wt\\wb-2.3-hook-order", commits: ["commit:51f9e07"], dirty: "HookEventQueue.cs (attempt 3, uncommitted)" },
  "br-24": { name: "wb/2.4-check-chips", item: "WB-2.4", machine: "cloud-1", state: "not yet created", ahead: 0, behind: 0, pushed: false, worktree: "created when you approve the plan" }
};

/* ---------------------------------------------------------------- CLAUDE.md sections */
WB.claudemd = {
  slots: { title: "Slots you may fill", file: "file:claudemd:3-10", n: "§1" },
  ask: { title: "How to ask", file: "file:claudemd:12-14", n: "§2" },
  comments: { title: "+ comments", file: "file:claudemd:16-19", n: "§3" },
  checks: { title: "Checks", file: "file:claudemd:21-26", n: "§4" },
  git: { title: "Git", file: "file:claudemd:28-30", n: "§5" }
};

/* ---------------------------------------------------------------- runs
   result: pass | fail | warn. Durations in seconds. Seed runs came from the Quanta Workbench on 4 Nov. */
WB.runs = {
  "1031": { check: "build", item: "WB-1", session: "51b0", machine: "desk-pc", result: "pass", dur: 51, at: "5 Nov 17:58", cmd: "dotnet build Qotoba.sln -warnaserror",
    out: ["  Determining projects to restore...", "  All projects are up-to-date for restore.", "  Qotoba.Workbench.Model -> D:\\Development\\repos\\Qotoba-wt\\wb-1-hello-card\\src\\Qotoba.Workbench.Model\\bin\\Debug\\net9.0\\Qotoba.Workbench.Model.dll", "  Qotoba.Workbench.Service -> ...\\Qotoba.Workbench.Service.dll", "  Qotoba.Workbench.Web -> ...\\Qotoba.Workbench.Web.dll", "", "Build succeeded.", "    0 Warning(s)", "    0 Error(s)", "", "Time Elapsed 00:00:50.84"] },
  "1033": { check: "tests", item: "WB-1", session: "51b0", machine: "desk-pc", result: "pass", passed: 164, total: 164, dur: 92, at: "5 Nov 18:04", cmd: "dotnet test tests/Qotoba.Workbench.Service.Tests",
    out: ["Test run for Qotoba.Workbench.Service.Tests.dll (.NETCoreApp,Version=v9.0)", "VSTest version 17.12.0 (x64)", "", "Starting test execution, please wait...", "  Passed HookEndpointTests.AcceptsRegisteredMachine [38 ms]", "  Passed HookEndpointTests.RefusesEventWithoutSession [4 ms]", "  Passed HookEndpointTests.RefusesEventWithoutTurn [3 ms]", "  Passed HelloCardTests.NamesTheMachine [11 ms]", "", "Passed!  - Failed:     0, Passed:   164, Skipped:     0, Total:   164, Duration: 1 m 29 s"] },
  "1035": { check: "specimens", item: "WB-1", session: "51b0", machine: "desk-pc", result: "pass", passed: 9, total: 9, dur: 41, at: "5 Nov 18:09", cmd: "qwb specimens --item WB-1",
    out: ["specimens for WB-1 · 9 pages · phone 390 px, rider 460 px, desktop 1280 px", "  inbox/hello-card.phone ............ match", "  inbox/hello-card.rider ............ match", "  inbox/hello-card.desktop .......... match", "  inbox/hello-card.dark.phone ....... match", "  inbox/two-machines.phone .......... match", "  inbox/two-machines.desktop ........ match", "  machines/list.phone ............... match", "  machines/list.desktop ............. match", "  machines/register.phone ........... match", "9/9 match"] },
  "1036": { check: "round-trip", item: "WB-1", session: "51b0", machine: "desk-pc", result: "pass", dur: 19, at: "5 Nov 18:12", cmd: "qwb check round-trip --tables cards,machines",
    out: ["round-trip · cards: 48 rows written, read, compared · ok", "round-trip · machines: 2 rows written, read, compared · ok", "round-trip ok"] },
  "1038": { check: "smoke hello-card", item: "WB-1", session: "51b0", machine: "desk-pc", also: "laptop", result: "pass", dur: 34, at: "5 Nov 18:20", cmd: "qwb smoke hello-card --from desk-pc,laptop --to phone",
    out: ["smoke hello-card · from desk-pc, laptop · to phone", "  desk-pc  Host 0.4.2  POST /hooks/v1  202  41 ms", "  laptop   Host 0.4.2  POST /hooks/v1  202  77 ms", "  service  stored card hello/desk-pc (session 51b0)", "  service  stored card hello/laptop (session 51b0)", "  phone    inbox shows 2 cards in 2.3 s", "  phone    card 1 names machine desk-pc", "  phone    card 2 names machine laptop", "smoke passed · 2/2 cards within 5 s"] },

  "1041": { check: "build", item: "WB-2.1", session: "c21e", machine: "laptop", result: "pass", dur: 48, at: "6 Nov 03:41", cmd: "dotnet build Qotoba.sln -warnaserror",
    out: ["  Determining projects to restore...", "  All projects are up-to-date for restore.", "  Qotoba.Workbench.Model -> C:\\src\\Qotoba-wt\\wb-2.1-runs-table\\src\\Qotoba.Workbench.Model\\bin\\Debug\\net9.0\\Qotoba.Workbench.Model.dll", "  Qotoba.Workbench.Service -> ...\\Qotoba.Workbench.Service.dll", "  Qotoba.Workbench.Web -> ...\\Qotoba.Workbench.Web.dll", "  Qotoba.Workbench.Service.Tests -> ...\\Qotoba.Workbench.Service.Tests.dll", "", "Build succeeded.", "    0 Warning(s)", "    0 Error(s)", "", "Time Elapsed 00:00:47.92"] },
  "1042": { check: "tests", item: "WB-2.1", session: "c21e", machine: "laptop", result: "pass", passed: 212, total: 212, dur: 114, at: "6 Nov 03:44", cmd: "dotnet test tests/Qotoba.Workbench.Service.Tests",
    out: [
      "  Determining projects to restore...",
      "  All projects are up-to-date for restore.",
      "  Qotoba.Workbench.Model -> C:\\src\\Qotoba-wt\\wb-2.1-runs-table\\src\\Qotoba.Workbench.Model\\bin\\Debug\\net9.0\\Qotoba.Workbench.Model.dll",
      "  Qotoba.Workbench.Service -> C:\\src\\Qotoba-wt\\wb-2.1-runs-table\\src\\Qotoba.Workbench.Service\\bin\\Debug\\net9.0\\Qotoba.Workbench.Service.dll",
      "  Qotoba.Workbench.Service.Tests -> C:\\src\\Qotoba-wt\\wb-2.1-runs-table\\tests\\Qotoba.Workbench.Service.Tests\\bin\\Debug\\net9.0\\Qotoba.Workbench.Service.Tests.dll",
      "Test run for C:\\src\\Qotoba-wt\\wb-2.1-runs-table\\tests\\Qotoba.Workbench.Service.Tests\\bin\\Debug\\net9.0\\Qotoba.Workbench.Service.Tests.dll (.NETCoreApp,Version=v9.0)",
      "VSTest version 17.12.0 (x64)",
      "",
      "Starting test execution, please wait...",
      "A total of 1 test files matched the specified pattern.",
      "  Passed Runs.RunsTableTests.RetriedPostMakesOneRow [41 ms]",
      "  Passed Runs.RunsTableTests.LatestPerCheckPicksNewest [12 ms]",
      "  Passed Runs.RunsTableTests.ForItemListsNewestFirst [9 ms]",
      "  Passed Runs.RunsTableTests.ForItemCapsAt200 [63 ms]",
      "  Passed Runs.RunRecordTests.SummaryWithTotals [1 ms]",
      "  Passed Runs.RunRecordTests.SummaryWithoutTotals [1 ms]",
      "  Passed Runs.RunRecordTests.ProvesOnlyAfterSince [2 ms]",
      "  Passed Runs.RunDurationFormatterTests.Seconds [1 ms]",
      "  Passed Runs.RunDurationFormatterTests.Minutes [1 ms]",
      "  Passed Runs.RunDurationFormatterTests.Hours [1 ms]",
      "  Passed Hooks.HookEndpointTests.AcceptsRegisteredMachine [36 ms]",
      "  Passed Hooks.HookEndpointTests.PostToolUseForCheckWritesRun [58 ms]",
      "  Passed Hooks.HookEndpointTests.PostToolUseForOtherToolWritesNothing [14 ms]",
      "  Passed Hooks.HookEndpointTests.RefusesEventWithoutSession [4 ms]",
      "  Passed Hooks.HookEndpointTests.RefusesEventWithoutTurn [3 ms]",
      "  Passed Hooks.HookEventQueueTests.DrainsInBatchesOfFifty [22 ms]",
      "  Passed Hooks.HookEventQueueTests.DrainBatchFromOptions [6 ms]",
      "  Passed Hooks.HookEventQueueTests.PendingCountsWindow [3 ms]",
      "  Passed Cards.HelloCardTests.NamesTheMachine [11 ms]",
      "  Passed Cards.InboxTests.QueueCountsNeedsYouOnly [17 ms]",
      "  Passed Model.ItemInvariantTests.ThirdOpenQuestionIsRefused [5 ms]",
      "  Passed Model.ItemInvariantTests.OneHomeMachine [4 ms]",
      "  Passed Model.ItemInvariantTests.ClaudeCannotCreateItem [3 ms]",
      "  Passed Model.ItemInvariantTests.PlanOfEightStepsIsRefused [3 ms]",
      "  Passed Model.ItemInvariantTests.SixthFoundItemIsRefused [4 ms]",
      "  ... 177 more passed",
      "",
      "Passed!  - Failed:     0, Passed:   212, Skipped:     0, Total:   212, Duration: 1 m 48 s - Qotoba.Workbench.Service.Tests.dll (net9.0)",
      "",
      "run reported by hook PostToolUse · tool_use_id toolu_01Hk7Wq2 · session c21e · turn 61"
    ] },
  "1043": { check: "specimens", item: "WB-2.1", session: "c21e", machine: "laptop", result: "pass", passed: 14, total: 14, dur: 38, at: "6 Nov 03:47", cmd: "qwb specimens --item WB-2.1",
    out: ["specimens for WB-2.1 · 14 pages", "  item/runs-tab.phone ................ match", "  item/runs-tab.rider ................ match", "  item/runs-tab.desktop .............. match", "  item/runs-tab.dark.phone ........... match", "  item/runs-tab.empty.phone .......... match", "  item/runs-tab.200-rows.desktop ..... match", "  inbox/after-approve.phone .......... match (queue meter refreshed on next poll; found f1)", "  ... 7 more match", "14/14 match"] },
  "1044": { check: "round-trip", item: "WB-2.1", session: "c21e", machine: "laptop", result: "pass", dur: 22, at: "6 Nov 03:49", cmd: "qwb check round-trip --tables runs",
    out: ["round-trip · runs: 212 rows written, read, compared · ok", "round-trip · runs: retried post with same tool_use_id · 1 row · ok", "round-trip ok"] },
  "1045": { check: "smoke hello-card", item: "WB-2.1", session: "c21e", machine: "laptop", result: "pass", dur: 31, at: "6 Nov 03:52", cmd: "qwb smoke hello-card --with-run tests",
    out: ["smoke hello-card · with run · from laptop · to phone", "  laptop   Host 0.4.2  POST /hooks/v1 (PostToolUse, dotnet test)  202  52 ms", "  service  run 1044 stored for WB-2.1", "  phone    hello card shows chip 'tests 212' in 1.9 s", "  phone    chip opens run 1044", "smoke passed"] },

  "1047": { check: "tests · repro", item: "WB-2.3", session: "b915", machine: "laptop", result: "fail", expected: true, passed: 11, total: 12, dur: 9, at: "6 Nov 04:02", cmd: "dotnet test --filter HookOrderingTests",
    out: ["  Failed HookOrderingTests.TwoMachinesInterleave [205 ms]", "  Expected events in order 1..40, got 1, 4, 7, 2, 3, 10, 5, 6 ... (14 out of place)", "  This failure reproduces the bug (step 1).", "Test Run Failed. Total 12, Passed 11, Failed 1"] },
  "1050": { check: "tests", item: "WB-2.3", session: "b915", machine: "laptop", result: "fail", passed: 11, total: 12, dur: 10, at: "6 Nov 04:12", cmd: "dotnet test --filter HookOrderingTests", attempt: 1,
    out: ["  Failed HookOrderingTests.TwoMachinesInterleave [198 ms]", "  Expected events in order 1..40, got 1, 4, 7, 2, 3, 10, 5, 6 ... (13 out of place)", "  Sorting by HostTimestamp puts every laptop event 1.8 s early.", "    at HookOrderingTests.TwoMachinesInterleave() in HookOrderingTests.cs:line 58", "Test Run Failed. Total 12, Passed 11, Failed 1"] },
  "1053": { check: "tests", item: "WB-2.3", session: "b915", machine: "laptop", result: "fail", passed: 11, total: 12, dur: 10, at: "6 Nov 04:24", cmd: "dotnet test --filter HookOrderingTests", attempt: 2,
    out: ["  Failed HookOrderingTests.TwoMachinesInterleave [203 ms]", "  Expected events in order 1..40, got 1..13, 16, 14, 15, 17..40", "  Session then turn orders each session, not the two together.", "    at HookOrderingTests.TwoMachinesInterleave() in HookOrderingTests.cs:line 58", "Test Run Failed. Total 12, Passed 11, Failed 1"] },
  "1056": { check: "tests", item: "WB-2.3", session: "b915", machine: "laptop", result: "fail", passed: 11, total: 12, dur: 11, at: "6 Nov 04:36", cmd: "dotnet test --filter HookOrderingTests", attempt: 3, exitCode: 1,
    out: [
      "Failed HookOrderingTests.TwoMachinesInterleave [212 ms]",
      "  Expected events in order 1..40, got 1..17, 19, 18, 20..40",
      "  Event 18  desk-pc  turn 9  ts 04:36:11.204",
      "  Event 19  laptop   turn 4  ts 04:36:09.391",
      "  at HookOrderingTests.TwoMachinesInterleave() in HookOrderingTests.cs:line 58",
      "Test Run Failed. Total 12, Passed 11, Failed 1",
      "",
      "  Stack Trace:",
      "     at Xunit.Assert.Equal[T](IEnumerable`1 expected, IEnumerable`1 actual)",
      "     at Qotoba.Workbench.Service.Tests.Hooks.HookOrderingTests.TwoMachinesInterleave() in C:\\src\\Qotoba-wt\\wb-2.3-hook-order\\tests\\Qotoba.Workbench.Service.Tests\\Hooks\\HookOrderingTests.cs:line 58",
      "",
      "  Standard Output Messages:",
      "   reorder window 2.0 s · drained 40 events · 1 out of place",
      "   laptop offset from desk-pc: -1.8 s (Host heartbeat 04:30)",
      "",
      "Total tests: 12",
      "     Passed: 11",
      "     Failed: 1",
      " Total time: 10.6 s",
      "",
      "run reported by hook PostToolUse · exit_code 1 · session b915 · turn 41"
    ] },

  "1048": { check: "tree-query-timing", item: "WB-2.2", session: "7f3a", machine: "desk-pc", result: "warn", dur: 6, at: "6 Nov 04:20", cmd: "qwb time tree-query --nodes 40 --repeat 5", value: "400 ms on 40 nodes",
    out: ["tree-query-timing · 40 nodes · 5 repeats · desk-pc", "  run 1   412 ms", "  run 2   398 ms", "  run 3   401 ms", "  run 4   396 ms", "  run 5   405 ms", "median 400 ms · budget 100 ms · warn", "  Load() includes Steps, Questions and Runs for every item", "  rows read: 40 items, 131 steps, 9 questions, 22 runs", "found f3 submitted to the tray of WB-2.2"] },
  "1049": { check: "build", item: "WB-2.2", session: "7f3a", machine: "desk-pc", result: "pass", dur: 52, at: "6 Nov 04:31", cmd: "dotnet build Qotoba.sln -warnaserror",
    out: ["  All projects are up-to-date for restore.", "  Qotoba.Workbench.Web -> D:\\Development\\repos\\Qotoba-wt\\wb-2.2-run-page\\src\\Qotoba.Workbench.Web\\bin\\Debug\\net9.0\\Qotoba.Workbench.Web.dll", "", "Build succeeded.", "    0 Warning(s)", "    0 Error(s)", "", "Time Elapsed 00:00:51.37"] },
  "1051": { check: "tests", item: "WB-2.2", session: "7f3a", machine: "desk-pc", result: "pass", passed: 38, total: 38, dur: 27, at: "6 Nov 04:44", cmd: "dotnet test tests/Qotoba.Workbench.Web.Tests",
    out: ["Starting test execution, please wait...", "  Passed Runs.RunPageTests.HeaderShowsMachineAndSessionChips [64 ms]", "  Passed Runs.RunPageTests.TailKeepsLastFortyLines [18 ms]", "  Passed Runs.RunPageTests.EmptyOutputSaysSo [9 ms]", "  Passed Runs.RunPageTests.NoHorizontalScrollAt390 [140 ms]", "  ... 34 more passed", "", "Passed!  - Failed:     0, Passed:    38, Skipped:     0, Total:    38, Duration: 24 s"] },

  "0912": { check: "library-open-timing", item: "PER-2", session: null, seed: true, machine: "laptop", result: "fail", dur: 41, at: "28 Oct 22:40", cmd: "dope-bench library-open --library reference --repeat 5", value: "4.8 s",
    out: ["library-open · reference library · 1,356 movies, 237 collections · Quanta build 2026.10.27", "  open 1   4.91 s", "  open 2   4.77 s", "  open 3   4.80 s", "  open 4   4.83 s", "  open 5   4.79 s", "median 4.80 s · target 1.00 s cold · fail", "  realised cards: 1,356 · verb menus built: 1,356", "imported from the Quanta Workbench seed · 4 Nov 20:05"] },
  "0931": { check: "library-open-timing", item: "PER-2", session: null, seed: true, machine: "laptop", result: "pass", dur: 6, at: "30 Oct 01:15", cmd: "dope-bench library-open --library reference --repeat 5", value: "0.6 s",
    out: ["library-open · reference library · 1,356 movies, 237 collections · Quanta build 2026.10.30", "  open 1   0.63 s", "  open 2   0.59 s", "  open 3   0.60 s", "  open 4   0.61 s", "  open 5   0.60 s", "median 0.60 s · target 1.00 s cold · pass", "  realised cards: 24 · verb menus built: 0", "imported from the Quanta Workbench seed · 4 Nov 20:05"] },
  "0933": { check: "memory-per-visit", item: "PER-2", session: null, seed: true, machine: "desk-pc", result: "pass", dur: 95, at: "30 Oct 01:40", cmd: "dope-bench memory --page library --visits 10", value: "−130 MB",
    out: ["memory per library visit · 10 visits · working set after GC", "  before (eager verbs)   412 MB", "  after  (lazy verbs)    282 MB", "  saving per visit       130 MB", "imported from the Quanta Workbench seed · 4 Nov 20:05"] },
  "0927": { check: "tests", item: "FTR-1", session: null, seed: true, machine: "desk-pc", result: "pass", passed: 919, total: 919, dur: 204, at: "30 Oct 01:22", cmd: "dotnet run --project Dope.Console.Checks",
    out: ["Dope console checks · WinUI rebuild reference", "  library ............ 214 passed", "  movie page ......... 188 passed", "  providers .......... 301 passed", "  collections ........ 216 passed", "919/919 passed", "imported from the Quanta Workbench seed · 4 Nov 20:05"] },
  "0928": { check: "screen", item: "FTR-1", session: null, seed: true, machine: "desk-pc", result: "pass", passed: 6, total: 6, dur: 58, at: "30 Oct 01:30", cmd: "dope-bench screens --pages library,movie,person,collections,settings,dashboard",
    out: ["screen gate · 6 pages against goldens", "  library ........ match", "  movie .......... match", "  person ......... match", "  collections .... match", "  settings ....... match", "  dashboard ...... match", "imported from the Quanta Workbench seed · 4 Nov 20:05"] }
};

WB.runs["0934"] = { check: "allocations-per-open", item: "PER-2", session: null, seed: true, machine: "laptop", result: "pass", dur: 22, at: "30 Oct 01:44", cmd: "dope-bench allocations --page library --open cold", value: "1.92 M → 0.21 M",
  out: ["allocations per cold library open · .NET event counters", "  before   1,920,412 objects   148 MB", "  after      208,930 objects    19 MB", "imported from the Quanta Workbench seed · 4 Nov 20:05"] };
WB.runs["0940"] = { check: "library-open-guard", item: "PER-2", session: null, seed: true, machine: "laptop", result: "pass", dur: 7, at: "4 Nov 20:20", cmd: "dope-bench library-open --library reference --cold --guard 1.2s", value: "0.60 s of 1.2 s",
  out: ["library-open guard · cold · laptop · limit 1.2 s (fail above)", "  open 1   0.61 s", "  open 2   0.60 s", "  open 3   0.59 s", "median 0.60 s · margin 0.60 s · pass"] };
WB.runs["0950"] = { check: "specimens · class parse", item: "FTR-2", session: null, seed: true, machine: "desk-pc", result: "pass", passed: 6, total: 6, dur: 12, at: "4 Nov 19:30", cmd: "qwb specimens --construct class --target parse,print",
  out: ["specimens · class construct · parse and print", "  class-basic.qdl ............ round-trip ok", "  class-defaults.qdl ......... round-trip ok", "  class-computed.qdl ......... round-trip ok", "  class-observable.qdl ....... round-trip ok", "  class-nullable.qdl ......... round-trip ok", "  class-comments.qdl ......... round-trip ok", "6/6 · spike from the Quanta repo, imported 4 Nov"] };
WB.runs["0951"] = { check: "specimens · class emit C#", item: "FTR-2", session: null, seed: true, machine: "desk-pc", result: "pass", passed: 3, total: 3, dur: 48, at: "4 Nov 19:41", cmd: "qwb specimens --construct class --target cs --build",
  out: ["specimens · class construct · emit C# and build -warnaserror", "  class-basic.cs ............. builds", "  class-defaults.cs .......... builds", "  class-computed.cs .......... builds", "3/3 · spike, imported 4 Nov"] };
WB.runs["1020"] = { check: "round-trip · parser corpus", item: "BUG-2", session: "e5c1", machine: "desk-pc", result: "fail", passed: 1203, total: 1204, dur: 64, at: "5 Nov 01:18", cmd: "qwb check round-trip --corpus parser --file recovery/unterminated-string.qo", exitCode: 1,
  out: ["round-trip · parser corpus · 1,204 files", "  recovery/unterminated-string.qo ... MISMATCH", "    line 3  expected: let greeting = \"hello   // greet the user", "    line 3  printed:  let greeting = \"hello", "    missing trivia: CommentTrivia \"// greet the user\" (17 chars)", "  1,203 files ok", "round-trip failed · 1 of 1,204 files", "exit code 1", "", "Quanta.Parser 2026.10.30 · Debug · net9.0"] };
WB.runs["1021"] = { check: "trivia trace", item: "BUG-2", session: "e5c1", machine: "desk-pc", result: "pass", dur: 3, at: "5 Nov 01:31", cmd: "qwb trace trivia recovery/unterminated-string.qo --line 3",
  out: ["trivia trace · line 3", "  token StringStart  col 16", "  error UnterminatedString  col 16..34", "  recovery StringRecovery.SkipToLineEnd skipped 18 chars", "  skipped: \"hello   // greet the user", "  attached to error node: none", "  dropped: CommentTrivia (17 chars)  <- cause confirmed"] };

/* ---------------------------------------------------------------- sessions */
WB.sessions = {
  "7f3a": { title: "Run page with output", machine: "desk-pc", item: "WB-2.2", lane: "A", state: "waiting", started: "6 Nov 04:26", minutes: 46, turns: 31, tools: 118,
    step: "3 of 5 · after-the-run or live output", lastAction: "asked about live output", waitingSince: "04:51", waitingOn: ["card:q-design", "card:q-unseen"],
    checks: ["run:1048", "run:1049", "run:1051"], questions: ["question:Q-07", "question:Q-08"], assumptions: ["a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8"], found: ["found:f3", "found:f4"],
    ledger: [
      ["04:26", "start", "Brief delivered: WB-2.2, 6 rulings, Skills list"],
      ["04:27", "read", "RunRecord.cs and RunsTable.cs on the packet branch"],
      ["04:29", "plan", "Plan was approved at 04:20; started step 1"],
      ["04:31", "check", "build passed · run 1049"],
      ["04:33", "write", "RunPage.cs header with three chips"],
      ["04:38", "commit", "Local commit 6d02b14 (no push, no merge)"],
      ["04:40", "assume", "Batch of 6 assumptions sent"],
      ["04:42", "write", "40-line tail and Empty text"],
      ["04:44", "check", "tests 38/38 passed · run 1051"],
      ["04:44", "found", "Copy link on a run row · f4"],
      ["04:47", "try", "Rendered 212 lines; nine screens on phone"],
      ["04:51", "ask", "Q-07 · after the run or live"],
      ["04:56", "assume", "2 more assumptions sent"],
      ["05:02", "ask", "Q-08 · exit code on RunRecord · questions 2/2"],
      ["05:02", "note", "Note written (2 lines)"]
    ] },
  "b915": { title: "Order hook events", machine: "laptop", item: "WB-2.3", lane: "B", state: "waiting", stuck: true, started: "6 Nov 04:01", minutes: 71, turns: 44, tools: 162,
    step: "2 of 4 · order in the queue", lastAction: "third ordering attempt failed", waitingSince: "04:38", waitingOn: ["card:st-23", "card:q-comment"],
    checks: ["run:1047", "run:1050", "run:1053", "run:1056"], questions: ["question:Q-09"], assumptions: [], found: ["found:f5"],
    ledger: [
      ["04:01", "start", "Brief delivered: WB-2.3, 4 rulings"],
      ["04:02", "check", "repro fails as expected · run 1047"],
      ["04:09", "write", "Order by HostTimestamp in Drain"],
      ["04:12", "check", "tests failed · run 1050"],
      ["04:18", "commit", "Local commit 51f9e07 for comment c7"],
      ["04:20", "write", "Order by session, turn, then timestamp"],
      ["04:24", "check", "tests failed · run 1053"],
      ["04:29", "found", "Same-second events reversed · f5, later promoted by you"],
      ["04:30", "read", "Host heartbeat: laptop -1.8 s vs desk-pc"],
      ["04:33", "write", "2 s reorder window before draining"],
      ["04:36", "check", "tests failed · run 1056"],
      ["04:38", "stuck", "Check failed three times; stopped"],
      ["04:40", "note", "Note written (2 lines)"],
      ["04:47", "ask", "Q-09 · the + comment cannot be done on Host 0.4.2"]
    ] },
  "2a6d": { title: "Check chips plan", machine: "cloud-1", item: "WB-2.4", lane: "C", state: "working", started: "6 Nov 04:40", minutes: 32, turns: 18, tools: 57,
    step: "plan submitted · reading for the settle pass", lastAction: "reading SummaryBlock for chip slot", waitingSince: null, waitingOn: [],
    checks: [], questions: [], assumptions: [], found: [],
    ledger: [
      ["04:40", "start", "Brief delivered: WB-2.4, read-only checkout"],
      ["04:42", "read", "SummaryBlock.cs, RunsTable.cs (packet branch)"],
      ["04:49", "read", "Old Quanta task card renderer"],
      ["04:55", "plan", "6 steps, 125 Claude-min, 3 settle questions"],
      ["04:58", "submit", "Plan sent for approval · pl-24"],
      ["05:04", "read", "RunsTable.LatestPerCheck for step 1"],
      ["05:10", "read", "SummaryBlock.Checks row shape"]
    ] },
  "c21e": { title: "Runs table", machine: "laptop", item: "WB-2.1", lane: "A", state: "ended", started: "6 Nov 01:53", ended: "6 Nov 03:58", minutes: 125, turns: 76, tools: 291,
    step: "packet drawn", lastAction: "ended with packet waiting", waitingSince: null, waitingOn: [], packet: "card:pk-runs",
    checks: ["run:1041", "run:1042", "run:1043", "run:1044", "run:1045"], questions: ["question:Q-03", "question:Q-04"], assumptions: ["a9", "a10", "a11", "a12", "a13", "a14"], found: ["found:f1", "found:f2"],
    ledger: [
      ["01:53", "start", "Brief delivered: WB-2.1, 5 rulings"],
      ["02:12", "ask", "Q-04 · key runs by tool_use_id"],
      ["02:31", "commit", "Local commit 3be07f1 for comment c1"],
      ["03:05", "commit", "Local commit 8c2d5a0 for comment c5"],
      ["03:10", "commit", "Local commit 7c4e9a1 · Duration as TimeSpan"],
      ["03:20", "found", "Inbox count lags after Approve · f1"],
      ["03:31", "found", "500 on empty tool_input · f2"],
      ["03:41", "check", "build passed · run 1041"],
      ["03:44", "check", "tests 212/212 · run 1042"],
      ["03:47", "check", "specimens 14/14 · run 1043"],
      ["03:49", "check", "round-trip passed · run 1044"],
      ["03:52", "check", "smoke hello-card passed · run 1045"],
      ["03:56", "note", "Note written (2 lines)"],
      ["03:58", "packet", "Packet pk-runs drawn; session ended"]
    ] },
  "e5c1": { title: "Parser recovery investigation", machine: "desk-pc", item: "BUG-2", lane: "-", state: "ended", started: "5 Nov 00:58", ended: "5 Nov 02:06", minutes: 68, turns: 39, tools: 141,
    step: "cause located, fix chosen", lastAction: "ended after the fix was chosen", waitingSince: null, waitingOn: [],
    checks: ["run:1020", "run:1021"], questions: ["question:Q-12"], assumptions: ["a15"], found: [],
    ledger: [
      ["00:58", "start", "Brief delivered: BUG-2, imported from the old register"],
      ["01:18", "check", "round-trip fails on the corpus file · run 1020"],
      ["01:31", "check", "trivia trace confirms the dropped comment · run 1021"],
      ["01:40", "ask", "Q-12 · may recovery discard tokens"],
      ["01:52", "answer", "You answered; ruling R-13 recorded"],
      ["02:01", "note", "Three fixes compared; you chose the second"],
      ["02:06", "end", "Session ended; nothing to merge"]
    ] },
  "51b0": { title: "Hello card", machine: "desk-pc", item: "WB-1", lane: "A", state: "ended", started: "5 Nov 15:21", ended: "5 Nov 18:31", minutes: 190, turns: 94, tools: 344,
    step: "packet approved", lastAction: "ended, packet approved", waitingSince: null, waitingOn: [], packet: "card:pk-hello",
    checks: ["run:1031", "run:1033", "run:1035", "run:1036", "run:1038"], questions: ["question:Q-01", "question:Q-02"], assumptions: [], found: [],
    ledger: [
      ["15:21", "start", "Brief delivered: WB-1"],
      ["16:02", "ask", "Q-01 · which machine is home"],
      ["17:10", "ask", "Q-02 · notifications per card"],
      ["17:58", "check", "build passed · run 1031"],
      ["18:04", "check", "tests 164/164 · run 1033"],
      ["18:20", "check", "smoke hello-card from two machines · run 1038"],
      ["18:31", "packet", "Packet pk-hello drawn; session ended"]
    ] }
};

/* ---------------------------------------------------------------- machines */
WB.machines = {
  "desk-pc": { os: "Windows 11 Pro 24H2", host: "0.4.2", online: true, lastSeen: "05:12", clock: "reference clock", token: "3 Nov 20:14", kind: "machine",
    checkouts: [
      { path: "D:\\Development\\repos\\Qotoba", branch: "master", item: null, worktree: false },
      { path: "D:\\Development\\repos\\Qotoba-wt\\wb-2.2-run-page", branch: "br-22", item: "WB-2.2", worktree: true },
      { path: "D:\\Development\\repos\\Dope", branch: "main", item: null, worktree: false }
    ], homed: ["WB-2.2"], sessions: ["7f3a", "51b0", "e5c1"] },
  "laptop": { os: "Windows 11 Home 24H2", host: "0.4.2", online: true, lastSeen: "05:11", clock: "1.8 s behind desk-pc", clockWarn: true, token: "3 Nov 20:31", kind: "machine",
    checkouts: [
      { path: "C:\\src\\Qotoba", branch: "master", item: null, worktree: false },
      { path: "C:\\src\\Qotoba-wt\\wb-2.1-runs-table", branch: "br-21", item: "WB-2.1", worktree: true },
      { path: "C:\\src\\Qotoba-wt\\wb-2.3-hook-order", branch: "br-23", item: "WB-2.3", worktree: true }
    ], homed: ["WB-2.1", "WB-2.3"], sessions: ["b915", "c21e"] },
  "cloud-1": { os: "Claude Code cloud session · Ubuntu 24.04", host: "0.4.2", online: true, lastSeen: "05:12", clock: "0.0 s (NTP)", token: "5 Nov 22:14", kind: "cloud", ephemeral: true,
    checkouts: [
      { path: "/home/claude/Qotoba", branch: "master", item: "WB-2.4", worktree: false, note: "read only until the plan is approved" }
    ], homed: ["WB-2.4"], sessions: ["2a6d"] }
};

WB.tokens = [
  { client: "phone", kind: "Workbench app", issued: "4 Nov 09:12", last: "05:12" },
  { client: "desk-pc", kind: "Host 0.4.2", issued: "3 Nov 20:14", last: "05:12" },
  { client: "laptop", kind: "Host 0.4.2", issued: "3 Nov 20:31", last: "05:11" },
  { client: "cloud-1", kind: "Host 0.4.2 (cloud)", issued: "5 Nov 22:14", last: "05:12" },
  { client: "Rider", kind: "tool window", issued: "4 Nov 11:40", last: "05:06" },
  { client: "VS Code", kind: "panel", issued: "4 Nov 11:52", last: "5 Nov 23:30" },
  { client: "claude.ai connector", kind: "MCP connector", issued: "5 Nov 10:05", last: "5 Nov 21:40" }
];

WB.install = [
  "Download Host 0.4.2 for Windows or Linux from Settings › Host.",
  "Run: qotoba-host register --workbench https://<workbench-host> --name <machine-name>",
  "Paste the one-time code shown on your phone (valid 10 minutes).",
  "The Host installs the Claude Code hooks for SessionStart, PreToolUse, PostToolUse and Stop.",
  "Check: qotoba-host hello posts a hello card that names the machine."
];

/* ---------------------------------------------------------------- inbox cards
   Only three things are pushed: Question Cards, Action Cards (packet, plan, stuck) and Info Cards.
   weight = minutes of estimated work waiting behind the card ("unblocks ~2h 10m"). */
WB.cards = {
  "q-design": { type: "question", question: "Q-07", item: "WB-2.2", project: "wb", session: "7f3a", weight: 130, waiting: "21 min", at: "04:51",
    headline: "Show run output after the run ends, or stream it live?",
    why: "Step 3 of the run page needs a rule for output longer than the 40-line card. The recommended answer needs no new endpoint.",
    evidence: [{ ref: "run:1051" }, { ref: "file:runpage" }, { text: "questions 2/2", tone: "warn" }] },
  "q-unseen": { type: "question", question: "Q-08", item: "WB-2.2", project: "wb", session: "7f3a", weight: 65, waiting: "10 min", at: "05:02",
    headline: "Add an exit code to RunRecord for the result pill?",
    why: "The result pill can't tell a crash from a failed test without it. RunRecord changed since you last opened it: read the diff first.",
    evidence: [{ ref: "file:runrecord" }, { text: "new to you · 40 lines", tone: "accent" }, { ref: "run:1056" }] },
  "q-comment": { type: "question", question: "Q-09", item: "WB-2.3", project: "wb", session: "b915", weight: 85, waiting: "25 min", at: "04:47",
    headline: "Your + comment asks for a sequence number the Host doesn't send.",
    why: "The session can't order by sequence on Host 0.4.2. Laptop's clock is 1.8 s behind desk-pc, so timestamps can't do it either.",
    evidence: [{ ref: "comment:c4" }, { ref: "machine:laptop" }, { ref: "run:1056" }] },
  "pk-runs": { type: "packet", item: "WB-2.1", project: "wb", session: "c21e", weight: 270, waiting: "1h 14m", at: "03:58",
    headline: "Runs from the hook now show on every item's Runs tab",
    why: "All five steps proved by runs. Two assumptions are unreviewed and two found items are in the tray. One helper was built beyond the plan.",
    evidence: [{ text: "checks 5/5", tone: "good" }, { text: "7 files · +312 −41", tone: "neutral" }, { text: "new to you · 2 files", tone: "accent" }] },
  "pl-24": { type: "plan", item: "WB-2.4", project: "wb", session: "2a6d", weight: 180, waiting: "14 min", at: "04:58",
    headline: "Plan: check chips on the summary block, 6 steps",
    why: "125 Claude-min, about 2h 30m at the default factor 1.2. Approving opens WB-2.4 in lane C beside lanes A and B. Three settle questions have defaults.",
    evidence: [{ text: "6/7 steps", tone: "neutral" }, { text: "3 settle questions", tone: "accent" }, { ref: "item:WB-2.1" }] },
  "st-23": { type: "stuck", item: "WB-2.3", project: "wb", session: "b915", weight: 110, waiting: "34 min", at: "04:38",
    headline: "HookOrderingTests failed three times on laptop",
    why: "Three ways of ordering by time all fail: laptop's clock runs 1.8 s behind desk-pc. The session stopped instead of trying a fourth.",
    evidence: [{ ref: "run:1056" }, { text: "3 attempts", tone: "bad" }, { ref: "machine:laptop" }] },
  "pk-dope": { type: "packet", item: "FTR-1", project: "dope", session: null, weight: 0, waiting: "1 day", at: "4 Nov 20:05", preview: true,
    headline: "Library visits use 130 MB less: card verbs build on first tap",
    why: "Preview built from the Dope seed to show how a Dope packet will read. Dope is closed, so approving only records the ruling it carries.",
    evidence: [{ ref: "run:0933" }, { text: "checks 3/3", tone: "good" }, { text: "preview", tone: "neutral" }] },
  "pk-hello": { type: "packet", item: "WB-1", project: "wb", session: "51b0", weight: 0, waiting: "", at: "5 Nov 18:31", approved: "5 Nov 18:42",
    headline: "Hello card reaches the phone from two machines",
    why: "Approved and merged. Kept as the item's packet so the record of what was approved stays readable.",
    evidence: [{ text: "checks 5/5", tone: "good" }, { text: "approved", tone: "good" }, { ref: "commit:a41c9e2" }] },

  "q-f2a": { type: "question", question: "Q-10", item: "FTR-2", project: "lang", session: null, weight: 90, waiting: "1 day", at: "4 Nov 21:10", deferred: true,
    headline: "Should computed members cache their value?",
    why: "Deferred until Language opens. The class construct cannot emit computed members until this is settled.",
    evidence: [{ ref: "fpb:FTR-2" }] },
  "q-f2b": { type: "question", question: "Q-11", item: "FTR-2", project: "lang", session: null, weight: 60, waiting: "1 day", at: "4 Nov 21:12", deferred: true,
    headline: "Default values: constructor or initialiser?",
    why: "Deferred until Language opens. Decides how both emit targets write defaults.",
    evidence: [{ ref: "fpb:FTR-2" }] },

  "i1": { type: "info", project: "wb", at: "6 Nov 03:58", age: "1h 14m",
    headline: "Lane A factor is now 1.3 (was 1.2)", chip: "lane:A",
    body: "WB-2.1 took 125 minutes against 100 Claude-minutes. Lane A forecasts now use 1.3.",
    origin: "Computed by the Workbench after [[card:pk-runs]] was drawn" },
  "i2": { type: "info", project: "wb", at: "5 Nov 22:14", age: "7h",
    headline: "cloud-1 registered with Host 0.4.2", chip: "machine:cloud-1",
    body: "A Claude Code cloud session joined as a third machine. It holds WB-2.4 read only until its plan is approved.",
    origin: "Host on cloud-1, one-time code confirmed on your phone" },
  "i3": { type: "info", project: "dope", at: "4 Nov 20:05", age: "1 day",
    headline: "Dope seed imported: 5 runs, 2 issues, 38 findings", chip: "fpb:PER-2",
    body: "Imported from the Quanta Workbench. The two issues are attached to Dope and stay unscheduled until Dope opens.",
    origin: "seed-once.ps1, run by you on desk-pc" },
  "i4": { type: "info", project: "wb", at: "6 Nov 03:57", age: "1h 15m",
    headline: "wb/2.1-runs-table pushed to origin, 4 commits", chip: "branch:br-21",
    body: "Branches on all three machines follow wb/<item>-<slug>. This looks settled.",
    origin: "Host on laptop, reported after a push you pressed on 6 Nov 03:57",
    ruling: "Branches are named wb/<item>-<slug> on every machine." },
  "i5": { type: "info", project: "wb", at: "6 Nov 03:58", age: "1h 14m",
    headline: "Session c21e ended with a packet waiting", chip: "session:c21e",
    body: "Its worktree stays on laptop until you approve or ask for changes.",
    origin: "Stop hook from laptop · [[card:pk-runs]]" },
  "i6": { type: "info", project: "dope", at: "4 Nov 20:06", age: "1 day",
    headline: "Dope library opens in 0.6 s cold, inside the 1 s target", chip: "run:0931",
    body: "The seed run measured 0.60 s on the reference library, on the laptop. The target is [[ruling:R-12]].",
    origin: "Seed import, checked against Dope's rulings" }
};

/* ---------------------------------------------------------------- packets */
WB.packets = {
  "pk-runs": {
    item: "WB-2.1", session: "c21e", machine: "laptop", state: "waiting",
    outcome: "Runs from the hook now show on every item's Runs tab",
    time: { actual: 125, est: 100, factor: 1.25 },
    checks: [
      { label: "build", ref: "run:1041", text: "build" },
      { label: "tests", ref: "run:1042", text: "tests 212" },
      { label: "specimens", ref: "run:1043", text: "specimens 14/14" },
      { label: "round-trip", ref: "run:1044", text: "round-trip" },
      { label: "smoke", ref: "run:1045", text: "smoke" }
    ],
    assumptions: ["a9", "a10", "a11", "a12", "a13", "a14"],
    found: ["f1", "f2"],
    commentsHandled: ["c1", "c5"],
    commentsOpen: ["c3", "c6", "c8", "c9"],
    beyond: [{ t: "1 helper added: RunDurationFormatter", type: "type:RunDurationFormatter",
      why: "Durations were formatted three ways (card, Runs tab, run page). One 12-line formatter keeps them the same; no other new public type." }],
    claims: [{ t: "A retried post makes one row", proof: "run:1044" }, { t: "200 rows render on the Runs tab without paging", proof: "run:1043" }, { t: "Draining 50 events takes under 5 ms", proof: null }],
    note: { text: "Runs arrive from the hook and show on the Runs tab. Durations go through a small formatter; out-of-order events are WB-2.3's job.", date: "6 Nov 03:56" },
    deliver: "Host on laptop · session c21e has ended, so the Host runs git for it",
    toast: "Instruction sent to the Host on laptop for session c21e",
    instruction: [
      "# delivered to Host on laptop · packet pk-runs · approved by owner 6 Nov",
      "cd C:\\src\\Qotoba-wt\\wb-2.1-runs-table",
      "git add src/Qotoba.Workbench.Model/Runs/RunRecord.cs src/Qotoba.Workbench.Service/Runs/RunsTable.cs \\",
      "        src/Qotoba.Workbench.Service/Endpoints/HookEndpoint.cs src/Qotoba.Workbench.Service/Hooks/HookEventQueue.cs \\",
      "        src/Qotoba.Workbench.Web/Items/RunsTab.cs src/Qotoba.Workbench.Service/Runs/RunDurationFormatter.cs \\",
      "        tests/Qotoba.Workbench.Service.Tests/Runs/RunsTableTests.cs",
      "git commit -m \"WB-2.1 Runs table fills from hook events\" -m \"Packet pk-runs approved 2026-11-06\"",
      "git -C C:\\src\\Qotoba switch master",
      "git -C C:\\src\\Qotoba pull --ff-only",
      "git -C C:\\src\\Qotoba merge --no-ff wb/2.1-runs-table -m \"Merge WB-2.1 Runs table fills from hook events\"",
      "git -C C:\\src\\Qotoba push origin master",
      "git -C C:\\src\\Qotoba worktree remove C:\\src\\Qotoba-wt\\wb-2.1-runs-table",
      "# git output is shown on the item's History; nothing further runs on it"
    ]
  },
  "pk-hello": {
    item: "WB-1", session: "51b0", machine: "desk-pc", state: "approved", approved: "5 Nov 18:42", merged: "commit:a41c9e2",
    outcome: "Hello card reaches the phone from two machines",
    time: { actual: 190, est: 150, factor: 1.3 },
    checks: [
      { label: "build", ref: "run:1031", text: "build" },
      { label: "tests", ref: "run:1033", text: "tests 164" },
      { label: "specimens", ref: "run:1035", text: "specimens 9/9" },
      { label: "round-trip", ref: "run:1036", text: "round-trip" },
      { label: "smoke", ref: "run:1038", text: "smoke" }
    ],
    assumptions: [], found: [], commentsHandled: [], commentsOpen: ["c8", "c9"],
    beyond: [],
    claims: [{ t: "Both machines' cards reach the phone within 5 s", proof: "run:1038" }],
    note: { text: "Both machines post and the phone shows each card with its machine. The laptop clock is 1.8 s behind desk-pc; it did not matter here.", date: "5 Nov 18:29" },
    deliver: "session 51b0 on desk-pc",
    toast: "Already approved and merged as a41c9e2",
    instruction: [
      "# delivered to session 51b0 on desk-pc · packet pk-hello · approved by owner 5 Nov 18:42",
      "cd D:\\Development\\repos\\Qotoba-wt\\wb-1-hello-card",
      "git add -A src/Qotoba.Workbench.Service src/Qotoba.Workbench.Web/Inbox CLAUDE.md",
      "git commit -m \"WB-1 Hello card across two machines\"",
      "git -C D:\\Development\\repos\\Qotoba merge --no-ff wb/1-hello-card",
      "git -C D:\\Development\\repos\\Qotoba push origin master",
      "# reported: merged as a41c9e2, worktree removed 18:45"
    ]
  },
  "pk-dope": {
    item: "FTR-1", session: null, machine: "desk-pc", state: "preview", preview: true,
    outcome: "Library visits use 130 MB less: card verbs build on first tap",
    time: { actual: null, est: 300, factor: null },
    checks: [
      { label: "tests", ref: "run:0927", text: "tests 919" },
      { label: "screen", ref: "run:0928", text: "screen 6/6" },
      { label: "memory", ref: "run:0933", text: "memory −130 MB" }
    ],
    assumptions: [], found: [], commentsHandled: [], commentsOpen: [],
    beyond: [],
    claims: [{ t: "Memory per visit falls by 130 MB", proof: "run:0933" }, { t: "First tap shows verbs within 50 ms", proof: null }],
    note: { text: "Built from the seed to preview a Dope packet. When Dope opens, this becomes a real packet with a session and a branch.", date: "4 Nov 20:05" },
    deliver: "nothing is delivered for a preview",
    toast: "Preview only: nothing is sent while Dope is closed",
    instruction: ["# preview · no branch, no session, no instruction is sent", "# when Dope opens, this section shows the exact git for the Dope repo"]
  }
};

/* ---------------------------------------------------------------- plan approval */
WB.plans = {
  "pl-24": {
    item: "WB-2.4", session: "2a6d", machine: "cloud-1", lane: "C", factor: 1.2, factorNote: "lane C is new; the programme default 1.2 applies",
    goal: "Every check on an item's summary is a chip from a real run, never typed text.",
    steps: [
      { t: "Read the latest run per check from the runs table", est: 20, needs: ["item:WB-2.1"], proof: "tests SummaryChecksTests" },
      { t: "One chip per check with count and state", est: 25, needs: ["ruling:R-06"], proof: "specimens" },
      { t: "Tapping a chip opens its run page", est: 15, needs: ["item:WB-2.2"], proof: "smoke run-page" },
      { t: "Stale marker when the run is older than the last commit", est: 30, needs: ["settle:s1"], proof: "tests" },
      { t: "A check that never ran shows a dashed 'not run' chip", est: 15, needs: ["settle:s2"], proof: "specimens" },
      { t: "Specimens for the four chip states", est: 20, needs: [], proof: "specimens 4 pages" }
    ],
    settle: [
      { id: "s1", q: "When is a check stale?", options: ["Older than the last commit on the branch", "Older than 24 hours", "Never stale"], rec: 0 },
      { id: "s2", q: "Show checks that never ran?", options: ["Dashed 'not run' chip", "Hide them"], rec: 0 },
      { id: "s3", q: "Order of the chips?", options: ["Pipeline order: build, tests, specimens, round-trip, smoke", "Failures first"], rec: 0 }
    ],
    opensBranch: "br-24"
  }
};

/* ---------------------------------------------------------------- stuck */
WB.stucks = {
  "st-23": {
    item: "WB-2.3", session: "b915", machine: "laptop", step: "step 2 of 4",
    check: "run:1056", checkName: "tests · HookOrderingTests.TwoMachinesInterleave",
    attempts: [
      { t: "Sort by Host timestamp in Drain", result: "13 out of place", run: "run:1050" },
      { t: "Sort by session and turn, then timestamp", result: "3 out of place", run: "run:1053" },
      { t: "Hold a 2 s reorder window before draining", result: "1 out of place", run: "run:1056" }
    ],
    diagnosis: "Laptop's clock runs 1.8 s behind desk-pc, so no ordering by Host time can pass the two-machine test. A per-session sequence from the Host would; that is [[question:Q-09]].",
    reassign: ["desk-pc", "cloud-1"]
  }
};

/* ---------------------------------------------------------------- settings */
WB.settings = {
  notifications: [
    { t: "One OS notification per blocking card", on: true, note: "Question, Approve, Plan and Stuck cards. Info cards never notify." },
    { t: "Quiet hours", on: true, note: "22:30 to 06:30 Lagos; cards wait in the inbox." },
    { t: "Daily summary at 07:00", on: false, note: "One line: queue size and time to clear it." }
  ],
  addresses: [
    { t: "Database", v: "postgres://workbench@<db-host>:5432/qotoba_workbench" },
    { t: "Service", v: "https://<workbench-host>/api" },
    { t: "Hook endpoint", v: "https://<workbench-host>/hooks/v1" },
    { t: "MCP endpoint (claude.ai connector)", v: "https://<workbench-host>/mcp" }
  ]
};

/* ---------------------------------------------------------------- talk (chat) */
WB.chat = {
  explain: "Talk opens a design chat in claude.ai with this card's brief already typed. Chat never writes to the Workbench: the only way back is a list of proposed rulings, one line each, that you tick on Rulings › Proposed from chat.",
  last: "Last design chat: 'Inbox deferral and clearing', 5 Nov 21:40, produced 4 proposed rulings."
};

window.WB = WB;

/* ================================================================ issues: FTR, BUG and PER (FPBs)
   Issues are not tasks. Tasks are the work breakdown of a project; an issue arises while building a task and is
   attached to it (or to a project with no task yet). Issues carry their own report and their own dependencies,
   which may point at other issues or at tasks. */

/* Old register IDs from the Quanta Workbench, shown as chips. */
WB.register = {
  "QP-0412": { title: "Unterminated string loses the trailing comment", date: "12 Sep", where: "Quanta parser register", fpb: "BUG-2" },
  "QP-0377": { title: "Block comment lost after recovery", date: "3 Sep", where: "Quanta parser register", note: "closed as a duplicate of QP-0412" },
  "QD-0188": { title: "Library freezes on open", date: "14 Oct", where: "Quanta Dope register", fpb: "PER-2" },
  "QD-0207": { title: "Card verbs built eagerly", date: "19 Oct", where: "Quanta Dope register", fpb: "FTR-1" },
  "QWB-P0188": { title: "Packet: library rail virtualised", date: "30 Oct", where: "Quanta Workbench packets", fpb: "PER-2" }
};

/* Section order per kind. Every report renders these in this order, skipping empty ones. */
WB.reportOrder = {
  FTR: [["goal", "Goal"], ["story", "Story"], ["scope", "Scope"], ["design", "Design"], ["acceptance", "Acceptance"], ["parity", "Parity"], ["risks", "Risks"], ["impact", "Impact"], ["plan", "Plan"], ["note", "Claude's note"]],
  BUG: [["symptom", "Symptom"], ["reproduce", "Reproduce"], ["evidence", "Evidence"], ["environment", "Environment"], ["frequency", "Frequency"], ["cause", "Cause"], ["fixes", "Possible fixes"], ["workaround", "Workaround"], ["acceptance", "Acceptance"], ["note", "Claude's note"]],
  PER: [["headline", "Headline"], ["target", "Target"], ["comparison", "Comparison"], ["series", "Series"], ["profile", "Profile"], ["resources", "Resources"], ["cause", "Cause"], ["fixes", "Possible fixes"], ["guard", "Guard"], ["environment", "Environment and method"], ["note", "Claude's note"]]
};
/* Sections closed until you open them, per kind (your own choices are remembered per kind). */
WB.reportFolded = {
  FTR: ["story", "scope", "design", "parity", "risks", "impact", "plan", "note"],
  BUG: ["environment", "workaround"],
  PER: ["profile", "resources", "environment"]
};
WB.subtypes = {
  FTR: ["new construct", "new screen or component", "API or service", "migration or parity", "tooling", "documentation"],
  BUG: ["crash", "wrong result", "regression", "build or tooling break", "flaky", "data", "UI or visual", "security"],
  PER: ["latency", "startup", "throughput", "memory", "allocations", "CPU", "size", "build time", "tool responsiveness", "battery"]
};
WB.states = {
  FTR: ["idea", "shaped", "designed", "planned", "building", "review", "done", "dropped"],
  BUG: ["reported", "confirmed", "located", "fixing", "fixed", "verified", "closed", "cannot reproduce", "won't fix"],
  PER: ["observed", "targeted", "analysed", "planned", "improving", "measured", "guarded", "done", "accepted as is"]
};

WB.fpbs = {
  /* ---------------------------------------------------------------- FTR · simple */
  "FTR-1": {
    kind: "FTR", subtype: "new screen or component", title: "Lazy card verbs on the library page", project: "dope", attach: "project:dope",
    state: "shaped", machine: null, branch: null, lane: null,
    time: { est: 300, estBy: "coarse, set by you", actual: 0, factor: null, forecast: null },
    source: { type: "imported", reg: "reg:QD-0207", date: "4 Nov 20:05" },
    sections: {
      goal: { author: "me", text: "Library cards build their verb menus on the first tap, not when the page loads, so a visit to the library costs about 130 MB less." },
      acceptance: { author: "me", lines: [
        { t: "Memory per library visit falls by at least 120 MB", check: "memory-per-visit", run: "run:0933" },
        { t: "The first tap on a card shows its verbs within 50 ms", check: "tap-latency", run: null }
      ] }
    },
    steps: [], checks: ["run:0933", "run:0927", "run:0928"],
    changes: { files: [["file:cardverbs", 29, 47]], types: [] }, commentScope: [], sessions: [],
    rulings: ["ruling:R-11"],
    history: [
      ["19 Oct", "seed", "Reported in the Quanta Dope register as [[reg:QD-0207]]"],
      ["30 Oct 01:40", "machine:desk-pc", "[[run:0933]] measured 130 MB less per visit"],
      ["4 Nov 20:05", "you", "Imported with the Dope seed and attached to Dope"]
    ]
  },

  /* ---------------------------------------------------------------- FTR · complex */
  "FTR-2": {
    kind: "FTR", subtype: "new construct", title: "class construct in QDL", project: "lang", attach: "item:LNG-5",
    state: "planned", machine: "desk-pc", branch: null, lane: null,
    time: { est: 460, estBy: "refined by the approved plan", actual: 95, factor: 1.2, forecast: 552 },
    source: { type: "created", date: "3 Nov 22:15" },
    sections: {
      goal: { author: "me", text: "Let a developer declare a class in QDL with typed members, defaults, computed members and per-member observability, and get an observable C# class and a TypeScript record from it with no hand-written code." },
      story: { author: "me", text: "When I rewrite Dope's models, I declare class Movie once and both targets follow." },
      scope: { author: "me",
        in: ["Typed fields with defaults", "Computed members (=> expression)", "Observability per member", "Emit to C# and TypeScript", "Round-trip print"],
        out: ["Inheritance", "Generics", "Methods with bodies", "Attributes other than observable"] },
      design: { author: "claude", session: "session:9c40",
        surfaces: ["nyw:ClassDecl", "nyw:ClassEmitter", "item:LNG-3", "item:LNG-4", "item:FW-1"],
        syntax: { status: "proposed", lang: "qdl", where: "specimens/class/movie.qdl", lines: [
          "class Movie",
          "  title: text",
          "  year: int = 0",
          "  rating: number?          observable",
          "  watched: bool = false    observable",
          "  label => \"{title} ({year})\""
        ] },
        options: [
          { t: "Every member observable by default", why: "Simple to write; 1,356 cards would raise events nobody reads." },
          { t: "Members opt in with the observable keyword", why: "Events only where a screen binds; matches how Dope cards use them.", chosen: true, ruling: "ruling:R-14" },
          { t: "Whole class observable or not at all", why: "Coarse; Dope needs 'watched' observable but not 'title'." }
        ],
        rulings: ["ruling:R-14", "ruling:R-15", "ruling:R-06"],
        questions: ["question:Q-10", "question:Q-11"] },
      acceptance: { author: "me", lines: [
        { t: "A class with five kinds of member parses and prints back unchanged", check: "specimens · class parse", run: "run:0950" },
        { t: "The emitted C# class builds with warnings as errors", check: "specimens · class emit C#", run: "run:0951" },
        { t: "Observable members raise change events through the observable core", check: "specimens · class observable", run: null },
        { t: "The emitted TypeScript record passes tsc --strict", check: "specimens · class emit TS", run: null }
      ] },
      parity: { author: "claude", session: "session:9c40", against: "the Quanta class declaration", rows: [
        { what: "Typed fields", old: "public string Title { get; set; }", now: "title: text", status: "same" },
        { what: "Default values", old: "property initialisers", now: "= value, per target", status: "pending", ref: "question:Q-11" },
        { what: "Computed properties", old: "get { … } block", now: "=> expression", status: "changed" },
        { what: "Change events", old: "raised on every field write", now: "only on observable members", status: "dropped", ref: "ruling:R-15" },
        { what: "Validation attributes", old: "[Required], [Range]", now: "moves to the schema family", status: "changed" }
      ] },
      risks: { author: "claude", session: "session:9c40", rows: [
        { t: "Observable members need the Framework observable core, which is not open.", likelihood: "high", limit: "Steps 1 to 5 need no observability; only step 6 waits." },
        { t: "The two targets may write defaults differently.", likelihood: "medium", limit: "Q-11 settles one rule per target; specimens compare both." },
        { t: "Dope's 12 model classes rely on field-level change events.", likelihood: "medium", limit: "R-15 drops them; the parity table lists every use." }
      ] },
      impact: { author: "machine", source: "code index", affects: ["Dope models · 12 classes", "KraveFlow schema · 31 classes, later"] },
      plan: { author: "claude", session: "session:9c40" },
      note: { author: "claude", session: "session:9c40", date: "4 Nov 19:38", text: "Parse, print and C# emit work for five member kinds in the spike. Observability waits on the Framework core; two questions are deferred until Language opens." }
    },
    steps: [
      { t: "Grammar and syntax tree for class and members", est: 60, state: "done", proof: "run:0950" },
      { t: "Printer round-trip for class", est: 40, state: "done", proof: "run:0950" },
      { t: "Checker rules: duplicate members, unknown types", est: 90, state: "todo", proof: "nyw:ClassDecl" },
      { t: "Emit computed members", est: 90, state: "todo", proof: "nyw:ClassEmitter", needs: ["question:Q-10"] },
      { t: "Emit defaults on both targets", est: 60, state: "todo", proof: "nyw:ClassEmitter", needs: ["question:Q-11"] },
      { t: "Observable members through the observable core", est: 120, state: "todo", proof: "nyw:check-observable", needs: ["item:FW-1"] }
    ],
    checks: ["run:0950", "run:0951"], changes: { files: [], types: [] }, commentScope: [], sessions: [["session:9c40", 95]],
    rulings: ["ruling:R-14", "ruling:R-15", "ruling:R-06"],
    history: [
      ["3 Nov 22:15", "you", "Created under [[item:LNG-5]]"],
      ["4 Nov 18:05", "session:9c40", "Spike started on [[machine:desk-pc]] in the Quanta repo"],
      ["4 Nov 19:41", "machine:desk-pc", "[[run:0951]] emitted C# builds with warnings as errors"],
      ["4 Nov 20:40", "you", "Goal edited", ["- Let a developer declare a class in QDL and get a C# class from it.", "+ Let a developer declare a class in QDL with typed members, defaults, computed members and per-member observability, and get an observable C# class and a TypeScript record from it."]],
      ["4 Nov 21:00", "you", "Chose opt-in observability ([[ruling:R-14]]) and dropped implicit field events ([[ruling:R-15]])"],
      ["4 Nov 21:30", "you", "Plan of 6 steps approved; [[question:Q-10]] and [[question:Q-11]] deferred until Language opens"]
    ]
  },

  /* ---------------------------------------------------------------- FTR · small, referenced by BUG-2 */
  "FTR-3": {
    kind: "FTR", subtype: "tooling", title: "Round-trip check for every .qo file", project: "lang", attach: "item:LNG-1",
    state: "idea", machine: null, branch: null, lane: null,
    time: { est: 120, estBy: "coarse, set by you", actual: 0, factor: null, forecast: null },
    source: { type: "created", date: "5 Nov 02:10" },
    sections: {
      goal: { author: "me", text: "Every .qo file in a change prints back to its exact source after parsing, and the check runs on every packet." },
      acceptance: { author: "me", lines: [{ t: "The parser corpus of 1,204 files round-trips with no mismatch", check: "round-trip · parser corpus", run: null }] }
    },
    steps: [], checks: [], changes: { files: [], types: [] }, commentScope: [], sessions: [], rulings: ["ruling:R-13"],
    history: [["5 Nov 02:10", "you", "Created after [[fpb:BUG-2]] showed the corpus check is not run on every change"]]
  },

  /* ---------------------------------------------------------------- BUG · simple */
  "BUG-1": {
    kind: "BUG", subtype: "wrong result", title: "Hook events arrive out of order when two tool calls finish in the same second", project: "wb", attach: "item:WB-2.3",
    state: "reported", machine: "laptop", branch: null, lane: null,
    severity: { level: "minor", reason: "Two runs swap places on screen for under a second; nothing is lost.", by: "session:b915", confirmed: false },
    time: { est: null, estBy: null, actual: 0, factor: null, forecast: null },
    source: { type: "promoted", found: "found:f5", session: "session:b915", item: "item:WB-2.3", date: "6 Nov 05:05" },
    sections: {
      symptom: { author: "claude", session: "session:b915", text: "Two tool calls from one session that finish in the same second can drain in reverse order.", expected: "Turn 7 before turn 8.", actual: "Turn 8 listed first on the Runs tab." }
    },
    steps: [], checks: [], changes: { files: [], types: [] }, commentScope: [], sessions: [], rulings: [],
    history: [
      ["6 Nov 04:29", "session:b915", "Found while working on [[item:WB-2.3]]; went to the tray as [[found:f5]]"],
      ["6 Nov 05:05", "you", "Promoted from the tray and attached to [[item:WB-2.3]]"]
    ]
  },

  /* ---------------------------------------------------------------- BUG · complex */
  "BUG-2": {
    kind: "BUG", subtype: "wrong result", title: "Recovery drops the trailing comment on an unterminated string", project: "lang", attach: "item:LNG-1",
    state: "located", machine: "desk-pc", branch: null, lane: null,
    severity: { level: "serious", reason: "The round-trip check fails on any file with this pattern.", by: "you", confirmed: true },
    time: { est: 45, estBy: "refined by the chosen fix", actual: 68, factor: 1.2, forecast: 122 },
    source: { type: "imported", reg: "reg:QP-0412", date: "4 Nov 20:05" },
    sections: {
      symptom: { author: "me", text: "When a string literal is not closed, recovery resumes on the next line and the comment after the string disappears from the tree.", expected: "The comment stays as trivia and prints back.", actual: "The printed file has no comment; round-trip reports a mismatch." },
      reproduce: { author: "claude", session: "session:e5c1", run: "run:1020", steps: [
        { t: "Open recovery/unterminated-string.qo from the parser corpus." },
        { t: "Line 3 reads: let greeting = \"hello   // greet the user", expect: "The string has no closing quote." },
        { t: "Parse the file with Quanta.Parser 2026.10.30.", expect: "One UnterminatedString diagnostic at column 16." },
        { t: "Print the tree back to text." },
        { t: "Compare the printed text with the source.", expect: "Identical text." },
        { t: "Look at line 3 of the printed text.", expect: "The comment is still there." },
        { t: "Run qwb check round-trip on the file.", expect: "Pass; it fails with one mismatch." }
      ] },
      evidence: { author: "machine", run: "run:1020", lines: 8,
        stack: { frames: [
          { f: "Quanta.Parser.Recovery.StringRecovery.SkipToLineEnd(ParserState, Token)", at: "StringRecovery.cs:40", own: true, first: true },
          { f: "Quanta.Parser.Recovery.StringRecovery.Recover(ParserState, Token)", at: "StringRecovery.cs:14", own: true },
          { f: "Quanta.Parser.Recovery.RecoveryTable.Apply(SyntaxKind, ParserState)", at: "RecoveryTable.cs:62", own: true },
          { f: "Quanta.Parser.Parser.ParseExpression()", at: "Parser.cs:418", own: true },
          { f: "Quanta.Parser.Parser.ParseLet()", at: "Parser.cs:233", own: true },
          { f: "Quanta.Parser.Parser.ParseFile()", at: "Parser.cs:74", own: true },
          { f: "System.Linq.Enumerable.SelectListIterator`2.MoveNext()", at: "", own: false }
        ], code: "file:stringrecovery:33-44" },
        attachments: [{ name: "printed-vs-source.png", kind: "screenshot", note: "line 3 side by side" }, { name: "trivia-trace.txt", kind: "text", note: "from run 1021" }] },
      environment: { author: "machine", machine: "machine:desk-pc", commit: "commit:2f8a1d4", tags: ["Windows 11 Pro 24H2", ".NET 9.0.0", "Debug", "master", "parser corpus · 1,204 files"] },
      frequency: { author: "machine", level: "always", count: "12 of 12 runs over 3 days" },
      cause: { author: "claude", session: "session:e5c1", status: "confirmed", confidence: "high", text: "SkipToLineEnd reads every token to the newline as string content. Comment tokens it reads are traced and then dropped, never attached as trivia.", code: "file:stringrecovery:36-43", run: "run:1021" },
      fixes: { author: "claude", session: "session:e5c1", chosen: 1, rows: [
        { t: "Stop recovery at the comment start", size: 20, risk: "medium", confidence: "medium", regression: "proposed", files: ["file:stringrecovery"],
          approach: ["Treat // as the end of the unterminated string.", "Resume parsing at the comment so it lexes normally.", "Leaves a stray quote token the printer must keep."] },
        { t: "Attach skipped tokens as trivia on the error node", size: 45, risk: "low", confidence: "high", regression: "exists", files: ["file:stringrecovery", "file:triviaattacher"],
          approach: ["Keep the buffered tokens from SkipToLineEnd.", "Attach them as trailing trivia on the StringLiteral error node.", "The printer already prints trivia, so the file comes back unchanged."] },
        { t: "Re-lex the line after recovery", size: 90, risk: "high", confidence: "medium", regression: "proposed", files: ["file:qlexer", "file:stringrecovery"],
          approach: ["Rewind the lexer to the quote after the error.", "Lex the rest of the line as normal tokens.", "Touches the lexer, which every file goes through."] }
      ] },
      workaround: { author: "claude", session: "session:e5c1", text: "Close the string before saving; the Rider add-in flags the unterminated string as you type.", who: "Anyone editing .qo files by hand. Generated files are not affected." },
      acceptance: { author: "me", lines: [{ t: "The new specimen with an unterminated string and a trailing comment round-trips", check: "specimen round-trip/unterminated-string-comment", run: null, nyw: "nyw:spec-unterminated" }] },
      note: { author: "claude", session: "session:e5c1", date: "5 Nov 02:01", text: "Cause confirmed by the trivia trace. You chose fix 2, which keeps the tokens as trivia as R-13 now requires." },
      extra: [{ title: "Fuzzer notes", author: "machine", run: "run:1020", lines: ["seeded 30 mutations around string literals", "3 of 30 drop trivia, all through StringRecovery.SkipToLineEnd", "0 crashes"] }]
    },
    steps: [], checks: ["run:1020", "run:1021"], changes: { files: [], types: [] }, commentScope: [], sessions: [["session:e5c1", 68]],
    rulings: ["ruling:R-13"],
    history: [
      ["12 Sep", "seed", "Reported in the Quanta parser register as [[reg:QP-0412]]"],
      ["4 Nov 20:05", "you", "Imported and attached to [[item:LNG-1]]"],
      ["5 Nov 01:18", "machine:desk-pc", "[[run:1020]] reproduces it: 1 of 1,204 files mismatched"],
      ["5 Nov 01:31", "session:e5c1", "Cause located with [[run:1021]]: comment tokens dropped in recovery"],
      ["5 Nov 01:52", "you", "Answered [[question:Q-12]]; it became [[ruling:R-13]]"],
      ["5 Nov 02:01", "you", "Chose fix 2 and set severity", ["- severity: minor (proposed)", "+ severity: serious · the round-trip check fails"]]
    ]
  },

  /* ---------------------------------------------------------------- PER · simple */
  "PER-1": {
    kind: "PER", subtype: "latency", title: "Tree query takes 400 ms on 40 nodes", project: "wb", attach: "item:WB-4",
    state: "targeted", machine: "desk-pc", branch: null, lane: null,
    time: { est: 120, estBy: "coarse, set by you", actual: 0, factor: null, forecast: null },
    source: { type: "promoted", found: "found:f3", session: "session:7f3a", item: "item:WB-2.2", date: "6 Nov 04:58" },
    sections: {
      headline: { author: "machine", single: { v: 400, unit: "ms", run: "run:1048", label: "median of 5, 40 nodes" } },
      target: { author: "me", v: 50, unit: "ms", text: "Under 50 ms warm for a tree of 40 nodes.", by: "you", date: "6 Nov 05:00" },
      environment: { author: "machine", machine: "machine:desk-pc", method: "run:1048", iterations: 5, tags: ["warm", "40 nodes", "median of 5", "Debug"] }
    },
    steps: [], checks: ["run:1048"], changes: { files: [], types: [] }, commentScope: [], sessions: [], rulings: [],
    history: [
      ["6 Nov 04:20", "session:7f3a", "Found while opening items; [[run:1048]] timed 400 ms"],
      ["6 Nov 04:58", "you", "Promoted from the tray and attached to [[item:WB-4]]"],
      ["6 Nov 05:00", "you", "Target set", ["+ under 50 ms warm for 40 nodes"]]
    ]
  },

  /* ---------------------------------------------------------------- PER · complex */
  "PER-2": {
    kind: "PER", subtype: "startup", title: "Library opens in 0.6 s instead of 4.8 s", project: "dope", attach: "project:dope",
    state: "guarded", machine: "laptop", branch: null, lane: null,
    time: { est: 180, estBy: "refined by the chosen fix", actual: 210, factor: 1.17, forecast: 210 },
    source: { type: "imported", reg: "reg:QD-0188", date: "4 Nov 20:05" },
    sections: {
      headline: { author: "machine", before: { v: 4.8, unit: "s", run: "run:0912" }, after: { v: 0.6, unit: "s", run: "run:0931" } },
      target: { author: "me", v: 1, unit: "s", text: "Under 1 s cold on the laptop, with the reference library.", by: "you", date: "29 Oct 23:10", ruling: "ruling:R-12" },
      comparison: { author: "machine", cond: ["1,356 movies, 237 collections", "cold start", "5 opens", "p50"] },
      series: { author: "machine", x: ["200", "1,000", "5,000", "20,000"], xLabel: "library size (items)", unit: "s",
        before: { p50: [0.9, 3.7, 17.2, 66], p90: [1.1, 4.4, 19.8, 74] },
        after: { p50: [0.31, 0.48, 0.71, 1.4], p90: [0.38, 0.6, 0.92, 1.9] },
        target: 1, runs: ["run:0912", "run:0931"],
        caption: "Before, open time grows with library size; after, it stays under 1 s up to 5,000 items and reaches 1.4 s at 20,000." },
      profile: { author: "machine", source: "library-open-before.nettrace", run: "run:0912", frames: [
        { f: "Dope.Library.LibraryPage.OnNavigatedTo", self: 12, total: 4790 },
        { f: "Dope.Library.LibraryCardRail.Realise (×1,356)", self: 210, total: 4410 },
        { f: "Dope.Library.MovieCard..ctor", self: 160, total: 4180 },
        { f: "Dope.Library.CardVerbs.Build", self: 980, total: 3920 },
        { f: "Quanta.Interactions.VerbMenu.For", self: 1240, total: 2940 },
        { f: "Quanta.Interactions.Basket.Attach", self: 1100, total: 1700 },
        { f: "Quanta.Ui.Commands.CommandCollection.Add", self: 600, total: 600 },
        { f: "SkiaSharp.SKImage.FromEncodedData", self: 380, total: 380 }
      ] },
      resources: { author: "machine", rows: [
        { metric: "Memory per visit", before: "412 MB", after: "282 MB", delta: "−130 MB", run: "run:0933" },
        { metric: "Allocations per open", before: "1.92 M", after: "0.21 M", delta: "−89%", run: "run:0934" },
        { metric: "Main-thread CPU, first 2 s", before: "96%", after: "38%", delta: "−58 pts", run: "run:0931" }
      ] },
      cause: { author: "claude", session: null, by: "Claude, Quanta Workbench (imported)", status: "confirmed", confidence: "high", text: "Every card built its full verb menu in its constructor, and the rail realised all 1,356 cards on open: 1,356 menus and their basket entries before the first frame.", code: "file:moviecard:5-15", run: "run:0931" },
      fixes: { author: "claude", session: null, by: "Claude, Quanta Workbench (imported)", chosen: 0, rows: [
        { t: "Virtualise the card rail and page the query", size: 180, risk: "low", confidence: "high", regression: "exists", files: ["file:librarycardrail"],
          approach: ["Realise only the 24 visible cards.", "Page the library query by 60 rows.", "Verb menus are built only for realised cards."] },
        { t: "Cache verb menus across visits", size: 120, risk: "medium", confidence: "medium", regression: "proposed", files: ["file:cardverbs", "file:moviecard"],
          approach: ["Build the 1,356 menus once and keep them between visits.", "Invalidate when a provider updates a movie.", "Still slow on the first visit; menus can go stale."] }
      ] },
      guard: { author: "machine", metric: "library open · cold · p50 · laptop", limit: 1.2, unit: "s", direction: "fail above", run: "run:0940", current: 0.6 },
      environment: { author: "machine", machine: "machine:laptop", method: "run:0931", iterations: 5, tags: ["Quanta build 2026.10.30", ".NET 9.0", "Release", "reference library · 1,356 movies", "cold start"] },
      note: { author: "claude", session: null, by: "Claude, Quanta Workbench (imported)", date: "30 Oct 01:20", text: "Virtualising the rail took open time from 4.8 s to 0.6 s. Lazy verbs (the Dope FTR) would cut memory further but not open time." }
    },
    steps: [], checks: ["run:0912", "run:0931", "run:0933", "run:0934", "run:0940"], changes: { files: [["file:librarycardrail", 38, 61]], types: [] }, commentScope: [], sessions: [],
    rulings: ["ruling:R-12", "ruling:R-11"],
    history: [
      ["28 Oct 22:40", "machine:laptop", "[[run:0912]] measured 4.8 s cold"],
      ["29 Oct 23:10", "you", "Target set", ["- under 500 ms", "+ under 1 s cold on the laptop"]],
      ["30 Oct 01:15", "machine:laptop", "[[run:0931]] measured 0.6 s after the rail change"],
      ["30 Oct 01:50", "you", "Approved the Quanta packet [[reg:QWB-P0188]] that measured it"],
      ["4 Nov 20:05", "you", "Imported with the Dope seed as [[reg:QD-0188]]'s successor"],
      ["4 Nov 20:20", "machine:laptop", "Guard [[run:0940]] passed: 0.60 s against 1.2 s"]
    ]
  }
};

/* Dependencies between issues and tasks. "needs" edges point from the dependent to what it needs;
   Blocks is derived. by: you or claude. min: minutes of work waiting behind the edge. */
WB.edges = [
  { id: "e1", from: "fpb:FTR-1", to: "fpb:PER-2", type: "needs", hard: true, reason: "Lazy verbs only pay off once the rail realises 24 cards, not 1,356.", by: "claude", session: null, confirmed: false, min: 300 },
  { id: "e2", from: "fpb:FTR-2", to: "item:LNG-1", type: "needs", hard: true, reason: "The class grammar extends the QDL parser.", by: "you", confirmed: true, min: 460 },
  { id: "e3", from: "fpb:FTR-2", to: "item:FW-1", type: "needs", hard: false, reason: "Only step 6, observable members, needs the observable core.", by: "claude", session: "session:9c40", confirmed: false, min: 120 },
  { id: "e4", from: "item:LNG-3", to: "fpb:FTR-2", type: "needs", hard: false, reason: "The C# emitter's class output follows this design.", by: "you", confirmed: true, min: 600 },
  { id: "e5", from: "fpb:BUG-2", to: "fpb:FTR-3", type: "needs", hard: true, reason: "The fix is proved by the round-trip check running on every .qo file.", by: "you", confirmed: true, min: 45 },
  { id: "e6", from: "fpb:BUG-1", to: "item:WB-2.3", type: "related", reason: "Same queue: WB-2.3 orders across machines, this one within one session.", by: "you", confirmed: true },
  { id: "e7", from: "fpb:BUG-2", to: "reg:QP-0377", type: "duplicate", reason: "Same drop, reported in September for block comments.", by: "you", confirmed: true },
  { id: "e8", from: "fpb:PER-1", to: "fpb:PER-2", type: "related", reason: "Same pattern: load everything, then show a little.", by: "claude", session: "session:7f3a", confirmed: true }
];

/* the spike session that drew FTR-2's plan, and its runs and questions */
WB.sessions["9c40"] = { title: "Class construct spike", machine: "desk-pc", item: "FTR-2", lane: "-", state: "ended", started: "4 Nov 18:05", ended: "4 Nov 19:40", minutes: 95, turns: 52, tools: 188,
  step: "plan drawn, steps 1 and 2 done", lastAction: "ended with two questions deferred", waitingSince: null, waitingOn: [],
  checks: ["run:0950", "run:0951"], questions: ["question:Q-10", "question:Q-11"], assumptions: [], found: [],
  ledger: [["18:05", "start", "Brief delivered: FTR-2, spike in the Quanta repo"], ["18:40", "write", "Grammar for class and members"], ["19:30", "check", "specimens 6/6 · run 0950"], ["19:32", "ask", "Q-10 · do computed members cache"], ["19:36", "ask", "Q-11 · defaults per target"], ["19:38", "note", "Note written (2 lines)"], ["19:41", "check", "C# emit builds · run 0951"]] };
WB.machines["desk-pc"].sessions.push("9c40");
["0950", "0951"].forEach(function (r) { WB.runs[r].session = "9c40"; WB.runs[r].seed = false; });
WB.questions["Q-10"].session = "9c40"; WB.questions["Q-10"].asked = "4 Nov 19:32";
WB.questions["Q-11"].session = "9c40"; WB.questions["Q-11"].asked = "4 Nov 19:36";

/* code the issues pull */
WB.files.stringrecovery = { path: "src/Quanta.Parser/Recovery/StringRecovery.cs", repo: "Quanta", proj: "Quanta.Parser", folder: "Recovery", branch: null,
  seen: false, newLines: 45, lastSeen: null,
  lines: [
    "namespace Quanta.Parser.Recovery;",
    "",
    "using Quanta.Parser.Syntax;",
    "using Quanta.Parser.Lexing;",
    "",
    "/// <summary>Recovers from a string literal that reaches the end of the line without a closing quote.</summary>",
    "internal sealed class StringRecovery : IRecoveryRule",
    "{",
    "    public SyntaxKind Handles => SyntaxKind.UnterminatedString;",
    "",
    "    public RecoveryResult Recover(ParserState state, Token start)",
    "    {",
    "        var error = state.Errors.Add(DiagnosticCode.UnterminatedString, start.Span);",
    "        var skipped = SkipToLineEnd(state, start);",
    "        var node = SyntaxFactory.ErrorNode(SyntaxKind.StringLiteral, start, skipped.Span);",
    "",
    "        // the node keeps its leading trivia; nothing is attached after it",
    "        return RecoveryResult.Resume(node, state.Position);",
    "    }",
    "",
    "    /// <summary>Skips to the end of the line so parsing can resume on the next one.</summary>",
    "    private static SkippedText SkipToLineEnd(ParserState state, Token start)",
    "    {",
    "        var lexer = state.Lexer;",
    "        var from = start.Span.Start;",
    "        var buffer = new List<Token>();",
    "",
    "        while (!lexer.AtEnd)",
    "        {",
    "            var token = lexer.PeekRaw();",
    "            if (token.Kind == TokenKind.NewLine)",
    "                break;",
    "            buffer.Add(lexer.ReadRaw());",
    "        }",
    "",
    "        // everything up to the newline is treated as string content;",
    "        // comment tokens read here never reach the trivia list",
    "        var span = TextSpan.FromBounds(from, lexer.Position);",
    "        foreach (var token in buffer)",
    "            if (token.Kind == TokenKind.Comment)",
    "                state.Diagnostics.Trace(\"recovery.skip\", token);",
    "",
    "        return new SkippedText(span, buffer.Count);",
    "    }",
    "}"
  ] };
WB.files.triviaattacher = { path: "src/Quanta.Parser/Syntax/TriviaAttacher.cs", repo: "Quanta", proj: "Quanta.Parser", folder: "Syntax", branch: null, seen: true, newLines: 0, lastSeen: "5 Nov 01:45",
  lines: ["namespace Quanta.Parser.Syntax;", "", "internal static class TriviaAttacher", "{", "    public static SyntaxNode AttachTrailing(SyntaxNode node, IReadOnlyList<Token> skipped) =>", "        node.WithTrailingTrivia(node.TrailingTrivia.AddRange(skipped.Select(Trivia.FromSkipped)));", "}"] };
WB.files.qlexer = { path: "src/Quanta.Parser/Lexing/Lexer.cs", repo: "Quanta", proj: "Quanta.Parser", folder: "Lexing", branch: null, seen: true, newLines: 0, lastSeen: "5 Nov 01:20",
  lines: ["namespace Quanta.Parser.Lexing;", "", "public sealed partial class Lexer", "{", "    public void Rewind(int position) => _position = position;", "    public Token PeekRaw() => Scan(_position, raw: true);", "    public Token ReadRaw() { var t = PeekRaw(); _position = t.Span.End; return t; }", "}"] };
WB.files.moviecard = { path: "src/Dope/Library/MovieCard.cs", repo: "Dope", proj: "Dope", folder: "Library", branch: null, seen: true, newLines: 0, lastSeen: "28 Oct 23:00",
  lines: [
    "namespace Dope.Library;",
    "",
    "public sealed class MovieCard : CardView<Movie>",
    "{",
    "    public MovieCard(Movie movie) : base(movie)",
    "    {",
    "        Title = movie.Title;",
    "        Year = movie.Year;",
    "        Poster = Posters.Decode(movie.PosterPath);",
    "",
    "        // every card builds its full verb menu up front",
    "        Verbs = CardVerbs.Build(movie);",
    "        foreach (var verb in Verbs)",
    "            Basket.Attach(verb);",
    "    }",
    "",
    "    public VerbMenu Verbs { get; }",
    "}"
  ] };
WB.commits["2f8a1d4"] = { msg: "Quanta.Parser 2026.10.30 baseline", date: "30 Oct 12:00", session: null, branch: null, files: 212, plus: 0, minus: 0,
  diff: ["@@ baseline tag, no diff @@", " Quanta.Parser 2026.10.30", " the version every parser issue is reproduced against"] };
WB.nyw.ClassDecl = { title: "ClassDecl", where: "src/Qotoba.Language/Syntax/ClassDecl.cs", step: "step 3 of [[fpb:FTR-2]]" };
WB.nyw.ClassEmitter = { title: "ClassEmitter", where: "src/Qotoba.Language/Emit/ClassEmitter.cs", step: "steps 4 and 5 of [[fpb:FTR-2]]" };
WB.nyw["check-observable"] = { title: "specimens · class observable", where: "qwb specimens --construct class --target observable", step: "step 6 of [[fpb:FTR-2]]" };
WB.nyw["spec-unterminated"] = { title: "specimen round-trip/unterminated-string-comment", where: "specimens/recovery/unterminated-string-comment.qo", step: "the acceptance of [[fpb:BUG-2]]" };

/* ================================================================ round 1 (redo) · content and sample data */
/* 1.1 why each pushed card reached the inbox, in plain words */
WB.cardWhy = {
  'q-design': 'Blocking question on step 3',
  'q-unseen': 'Blocking question on step 4, about a file you have not read yet',
  'q-comment': 'Your + comment asks for something the session cannot do',
  'pk-runs': 'All checks passed, ready to merge',
  'pk-dope': 'All checks passed on the seed; a preview, nothing to merge while Dope is closed',
  'pk-hello': 'All checks passed; you approved it and it merged',
  'pl-24': 'Plan needs your approval',
  'st-23': 'Failed three times on the same step',
  'q-f2a': 'Blocking question on step 4, deferred until Language opens',
  'q-f2b': 'Blocking question on step 5, deferred until Language opens',
  'i1': 'A lane factor changed', 'i2': 'A machine registered', 'i3': 'A seed was imported',
  'i4': 'A branch was pushed', 'i5': 'A session ended', 'i6': 'A seeded run met its target'
};

/* 2.2 the cost to change later, on every option, not only the recommended one */
(function (c) { Object.keys(c).forEach(function (q) { c[q].forEach(function (cost, i) { if (WB.questions[q] && WB.questions[q].options[i]) WB.questions[q].options[i].cost = cost; }); }); })({
  'Q-07': ['small, one method', 'large, a push channel per page and a Host change', 'medium, a paging query and a migration'],
  'Q-08': ['small, one property and one migration', 'small now; a migration later to get the difference back', 'small now, but every tool\'s wording becomes a dependency'],
  'Q-09': ['medium, the Host payload and one queue method', 'small, one sort key in the queue', 'small, one findings rule'],
  'Q-10': ['medium, the emitter for computed members', 'small, one emitter rule'],
  'Q-11': ['small, one emitter rule per target', 'large, every Dope model\'s construction call']
});

/* 1.4 minutes each running session has spent on its current step */
WB.sessions['7f3a'].stepMin = 21;
WB.sessions['b915'].stepMin = 34;
WB.sessions['2a6d'].stepMin = 14;

/* 1.9 where each ruling came from, as a short phrase for the list */
WB.rulingFrom = {
  'R-01': 'set by you', 'R-02': 'set by you', 'R-05': 'set by you', 'R-06': 'set by you', 'R-10': 'set by you', 'R-15': 'set by you',
  'R-07': 'from a chat proposal', 'R-08': 'from a chat proposal',
  'R-04': 'answered on a question card', 'R-09': 'answered on a question card', 'R-13': 'answered on a question card',
  'R-14': 'from a settle pass',
  'R-03': 'carried over from the Quanta Workbench',
  'R-11': 'imported with the Dope seed', 'R-12': 'imported with the Dope seed'
};

/* 2.5 durations in the sample data carry spaced units: "1 h 14 min", "7 h" */
(function () {
  function u(t) { return String(t).replace(/(\d+)h (\d+)m\b/g, '$1 h $2 min').replace(/(\d+)h\b/g, '$1 h').replace(/(\d+)m\b/g, '$1 min'); }
  Object.keys(WB.cards).forEach(function (k) { var c = WB.cards[k]; if (c.waiting) c.waiting = u(c.waiting); if (c.age) c.age = u(c.age); });
  Object.keys(WB.plans).forEach(function (k) { var p = WB.plans[k]; if (p.why) p.why = u(p.why); });
})();

/* ================================================================ round 2 · the inbox is the only place the owner acts
   Reports (FTR, BUG, PER) are information. Each carries "When implementing": the lines a task will have to settle.
   Decisions reach the owner only through a task: its plan card, a question card at a step, or its packet. */
(function (W) {
  function hist(list, find, repl) { for (var i = 0; i < list.length; i++) if (JSON.stringify(list[i]).indexOf(find) >= 0) { list[i] = repl; return; } }
  function without(list, x) { var i = list.indexOf(x); if (i >= 0) list.splice(i, 1); }

  /* ---------------------------------------------------------------- BUG-2, taken on by LNG-1.1: the whole chain, settled and proved */
  W.items['LNG-1.1'] = {
    kind: 'task', title: 'Keep comments when string recovery skips a line', project: 'lang', parent: 'LNG-1', children: [], state: 'done', isOpen: false,
    lane: null, machine: 'desk-pc', branch: 'br-lng11', coarse: '~1 day', implements: ['fpb:BUG-2'], plan: 'card:pl-lng11', packet: 'card:pk-lng11',
    goal: 'Recovery from an unterminated string keeps every token it skips, so the file prints back with its comment.',
    steps: [
      { t: 'Reproduce on the parser corpus', est: 10, state: 'done', proof: 'run:1020' },
      { t: 'Trace where the comment is dropped', est: 10, state: 'done', proof: 'run:1021' },
      { t: 'Attach skipped tokens as trivia on the error node', est: 20, state: 'done', proof: 'run:1022' },
      { t: 'Prove the BUG-2 acceptance line with a run', est: 10, state: 'done', proof: 'run:1022' }
    ],
    checks: ['run:1020', 'run:1021', 'run:1022'], changes: { files: [['file:stringrecovery', 9, 3], ['file:triviaattacher', 6, 0]], types: [] },
    time: { so: 68, est: 50, factor: 1.2, forecast: 68 }, needs: [{ ref: 'fpb:FTR-3', note: 'accepted with the plan' }], blocks: [], commentScope: [],
    note: { text: 'Skipped tokens now ride on the error node as trivia; the corpus round-trips 1,204 of 1,204.', date: '5 Nov 02:04', session: 'session:e5c1' },
    rulings: ['ruling:R-13', 'ruling:R-06'], sessions: [['session:e5c1', 68]],
    info: { tpl: 'task', purpose: 'A one-off fix opened by you while Language is closed: it implements BUG-2 and nothing else.',
      rulings: ['ruling:R-13', 'ruling:R-06'], files: [['file:stringrecovery', 'where recovery skips to the line end'], ['file:triviaattacher', 'attaches trivia to error nodes']],
      acceptance: [{ t: 'The specimen with an unterminated string and a trailing comment round-trips', proof: 'run:1022' }], risks: ['A stray quote token stays in the tree; the printer keeps it.'], unblocks: [] },
    history: [
      ['4 Nov 21:30', 'opened', 'You added the item under [[item:LNG-1]] to implement [[fpb:BUG-2]]'],
      ['5 Nov 01:02', 'plan submitted', 'Session [[session:e5c1]] drew a plan of 4 steps from the report\'s When implementing lines'],
      ['5 Nov 01:05', 'plan approved', 'You approved [[card:pl-lng11]]; Fix 2 and the FTR-3 dependency settled with it'],
      ['5 Nov 01:52', 'question answered', 'You answered [[question:Q-12]] at step 2; it became [[ruling:R-13]]'],
      ['5 Nov 02:06', 'packet drawn', '[[card:pk-lng11]] with [[run:1022]] proving the acceptance line'],
      ['5 Nov 08:10', 'packet approved', 'You approved; merged as [[commit:c7d21b4]]']
    ]
  };
  W.items['LNG-1'].children = ['LNG-1.1'];
  W.branches['br-lng11'] = { name: 'lng/1.1-string-recovery-trivia', item: 'LNG-1.1', machine: 'desk-pc', state: 'merged', ahead: 0, behind: 0, pushed: true, merged: 'commit:c7d21b4', worktree: 'removed 5 Nov 08:14' };
  W.commits['c7d21b4'] = { msg: 'Merge LNG-1.1 Keep comments when string recovery skips a line', date: '5 Nov 08:12', session: 'e5c1', branch: 'br-lng11', files: 2, plus: 15, minus: 3,
    diff: ['-        SkipToLineEnd(reader);', '+        var skipped = SkipToLineEnd(reader);', '+        node.TrailingTrivia.AddRange(skipped.Select(Trivia.From));', '+        // R-13: recovery never discards tokens'] };
  W.runs['1022'] = { check: 'round-trip · parser corpus', item: 'LNG-1.1', session: 'e5c1', machine: 'desk-pc', result: 'pass', passed: 1204, total: 1204, dur: 61, at: '5 Nov 02:00',
    cmd: 'qwb check round-trip --corpus parser', exitCode: 0, out: ['round-trip · parser corpus · 1,204 files', '  recovery/unterminated-string.qo ... ok', '1,204 of 1,204 print back unchanged'] };
  W.runs['1020'].item = 'LNG-1.1'; W.runs['1021'].item = 'LNG-1.1';
  var e5 = W.sessions['e5c1'];
  e5.item = 'LNG-1.1'; e5.title = 'Keep comments in string recovery'; e5.step = 'packet drawn'; e5.lastAction = 'ended with the packet waiting'; e5.packet = 'card:pk-lng11';
  e5.checks = ['run:1020', 'run:1021', 'run:1022']; e5.assumptions = [];
  e5.ledger = [['00:58', 'start', 'Brief delivered: LNG-1.1, which implements BUG-2'], ['01:02', 'plan', 'Plan of 4 steps drawn from BUG-2 · When implementing'], ['01:05', 'approved', 'You approved the plan; Fix 2 settled'],
    ['01:18', 'check', 'round-trip fails on the corpus file · run 1020'], ['01:31', 'check', 'trivia trace confirms the dropped comment · run 1021'], ['01:40', 'ask', 'Q-12 at step 2 · may recovery discard tokens'],
    ['01:52', 'answer', 'You answered: never discard · R-13'], ['02:00', 'check', 'round-trip 1,204 of 1,204 · run 1022'], ['02:06', 'packet', 'Packet drawn; session ended']];
  var q12 = W.questions['Q-12'];
  q12.item = 'LNG-1.1'; q12.card = 'q-lng11'; q12.blocks = 'step 2 of 4'; q12.unblockMin = 30; q12.cost = 'high, every recovery path and the round-trip guarantee';
  q12.context = 'Step 2 of [[item:LNG-1.1]] traced the drop to StringRecovery.SkipToLineEnd: it reads to the newline and throws the tokens away. The report [[fpb:BUG-2]] lists this as a blocking decision.';
  q12.options = [
    { t: 'Never discard: skipped text becomes trivia on the error node', why: 'Every file prints back; the printer already emits trivia.', rec: true, support: ['ruling:R-06'], cost: 'low, one attach call' },
    { t: 'Discard skipped tokens and record a finding', why: 'Simpler, but the file no longer prints back.', support: [], cost: 'high, every recovery path and the round-trip guarantee' }
  ];
  q12.tried = ['Kept the tokens in a side list; the printer ignored them.'];
  W.rulings['R-13'].item = 'item:LNG-1.1';
  W.plans['pl-lng11'] = { item: 'LNG-1.1', session: 'e5c1', machine: 'desk-pc', lane: '-', factor: 1.2, factorNote: 'Language has no lane yet; the programme default 1.2 applies', approved: '5 Nov 01:05',
    goal: 'Recovery from an unterminated string keeps every token it skips, so the file prints back with its comment.',
    steps: [
      { t: 'Reproduce on the parser corpus', est: 10, needs: [], proof: 'round-trip · parser corpus' },
      { t: 'Trace where the comment is dropped', est: 10, needs: [], proof: 'trivia trace' },
      { t: 'Attach skipped tokens as trivia on the error node', est: 20, needs: ['settle:s1'], proof: 'round-trip · parser corpus' },
      { t: 'Prove the BUG-2 acceptance line with a run', est: 10, needs: ['settle:s2'], proof: 'specimen round-trip' }
    ],
    settle: [
      { id: 's1', q: 'Which fix?', options: ['Fix 2: attach skipped tokens as trivia on the error node', 'Fix 1: stop recovery at the comment start', 'Fix 3: re-lex the line after recovery'], rec: 0, from: 'fpb:BUG-2' },
      { id: 's2', q: 'Depends on FTR-3: the round-trip check proves the fix', options: ['Accept', 'Not needed'], rec: 0, from: 'fpb:BUG-2' },
      { id: 's3', q: 'A comment inside the unterminated string belongs to the string', options: ['Yes, keep it with the string', 'No, lex it as a comment'], rec: 0, from: 'fpb:BUG-2' }
    ], opensBranch: 'br-lng11' };
  W.packets['pk-lng11'] = { item: 'LNG-1.1', session: 'e5c1', machine: 'desk-pc', state: 'approved', approved: '5 Nov 08:10', merged: 'commit:c7d21b4',
    outcome: 'Comments survive string recovery; the corpus round-trips 1,204 of 1,204', time: { actual: 68, est: 50, factor: 1.2 },
    checks: [{ label: 'round-trip', ref: 'run:1022', text: 'round-trip 1204/1204' }, { label: 'trace', ref: 'run:1021', text: 'trivia trace' }],
    assumptions: [], found: [], commentsHandled: [], commentsOpen: [], beyond: [],
    claims: [{ t: 'The BUG-2 specimen prints back with its comment', proof: 'run:1022' }, { t: 'No other file in the corpus changed', proof: 'run:1022' }],
    note: { text: 'Fix 2 as settled in the plan. The stray quote token stays in the tree and prints back.', date: '5 Nov 02:04' },
    deliver: 'session e5c1 on desk-pc', toast: 'Already approved and merged as c7d21b4',
    instruction: ['# delivered to session e5c1 on desk-pc · packet pk-lng11 · approved by owner 5 Nov 08:10', 'git -C D:\\Development\\repos\\Qotoba merge --no-ff lng/1.1-string-recovery-trivia', '# reported: merged as c7d21b4, worktree removed 08:14'] };
  W.cards['pl-lng11'] = { type: 'plan', item: 'LNG-1.1', project: 'lang', session: 'e5c1', weight: 0, waiting: '', at: '5 Nov 01:02', approved: '5 Nov 01:05', headline: 'Plan: keep comments when string recovery skips a line, 4 steps' };
  W.cards['q-lng11'] = { type: 'question', question: 'Q-12', item: 'LNG-1.1', project: 'lang', session: 'e5c1', weight: 30, waiting: '', at: '5 Nov 01:40', answered: '5 Nov 01:52', headline: 'May recovery discard the tokens it skips?' };
  W.cards['pk-lng11'] = { type: 'packet', item: 'LNG-1.1', project: 'lang', session: 'e5c1', weight: 0, waiting: '', at: '5 Nov 02:06', approved: '5 Nov 08:10', headline: 'Comments survive string recovery' };

  var b2 = W.fpbs['BUG-2'];
  b2.state = 'verified'; b2.takenOnBy = ['LNG-1.1']; b2.checks = ['run:1020', 'run:1021', 'run:1022'];
  b2.sections.acceptance.lines[0].run = 'run:1022'; b2.sections.acceptance.lines[0].nyw = null;
  b2.severity = { level: 'serious', reason: 'The round-trip check fails on any file with this pattern.', by: 'claude', confirmed: false };
  b2.history = b2.history.filter(function (h) { return h[0] !== '5 Nov 02:01' && h[0] !== '5 Nov 01:52'; }).concat([
    ['4 Nov 21:30', 'you', 'Added [[item:LNG-1.1]] to take this on'],
    ['5 Nov 01:05', 'you', 'Approved the plan [[card:pl-lng11]]; Fix 2 and the FTR-3 dependency settled'],
    ['5 Nov 01:52', 'you', 'Answered [[question:Q-12]] on its question card; it became [[ruling:R-13]]'],
    ['5 Nov 02:00', 'machine:desk-pc', '[[run:1022]] proves the acceptance line'],
    ['5 Nov 08:10', 'you', 'Approved [[card:pk-lng11]]; merged as [[commit:c7d21b4]]']]);
  b2.sections.implementing = { author: 'claude', lines: [
    { kind: 'decision', text: 'Which fix', rec: 'Fix 2, attach skipped tokens as trivia', why: 'Low risk, high confidence, and a regression check already exists.', cost: 'low', status: 'settled', ref: 'card:pl-lng11', date: '5 Nov 01:05', note: 'Fix 2 chosen · plan for LNG-1.1' },
    { kind: 'decision', text: 'May recovery discard the tokens it skips?', rec: 'Never discard', why: 'Every file must print back.', cost: 'high', status: 'settled', ref: 'ruling:R-13', date: '5 Nov 01:52', note: 'Answered on a question card at step 2 of LNG-1.1 · became R-13', via: 'card:q-lng11' },
    { kind: 'decision', text: 'Does a comment inside the unterminated string belong to the string?', rec: 'Yes, keep it with the string', why: 'Claude assumed this while investigating; the recommendation is already applied.', cost: 'low', status: 'settled', ref: 'card:pl-lng11', date: '5 Nov 01:05', note: 'Accepted as a default in the settle pass' },
    { kind: 'decision', text: 'Fix 1, stop recovery at the comment start', status: 'dropped', note: 'superseded by Fix 2: it leaves a stray quote the printer must special-case' },
    { kind: 'dependency', text: 'Depends on FTR-3: the round-trip check runs on every .qo file', status: 'settled', ref: 'card:pl-lng11', date: '5 Nov 01:05', note: 'Accepted with the plan for LNG-1.1' },
    { kind: 'precondition', text: 'The parser corpus holds the recovery specimens', status: 'proved', ref: 'run:1020', date: '5 Nov 01:18' },
    { kind: 'proof', text: 'The cause is confirmed by a run', status: 'proved', ref: 'run:1021', date: '5 Nov 01:31' },
    { kind: 'proof', text: 'Acceptance: the specimen with an unterminated string and a trailing comment round-trips', status: 'proved', ref: 'run:1022', date: '5 Nov 02:00' }
  ] };
  delete W.assumptions.a15;

  /* ---------------------------------------------------------------- PER-1, taken on by WB-2.5: target settled in the plan, comparison proved by a run */
  W.found.f3.session = '51b0'; W.found.f3.item = 'WB-1'; W.found.f3.found = '5 Nov 17:20';
  without(W.sessions['7f3a'].found, 'found:f3'); W.sessions['51b0'].found = ['found:f3'];
  W.runs['1048'].item = 'WB-1'; W.runs['1048'].session = '51b0'; W.runs['1048'].at = '5 Nov 17:18';
  without(W.sessions['7f3a'].checks, 'run:1048'); without(W.items['WB-2.2'].checks, 'run:1048');
  W.items['WB-2.5'] = {
    kind: 'task', title: 'Tree query under 50 ms', project: 'wb', parent: 'WB-2', children: [], state: 'done', isOpen: false, lane: 'A', machine: 'desk-pc', branch: 'br-25', coarse: '~1 day',
    implements: ['fpb:PER-1'], plan: 'card:pl-25', packet: 'card:pk-25',
    goal: 'Opening an item loads one level of the tree, so the query stays under 50 ms warm for 40 nodes.',
    steps: [
      { t: 'Measure the tree query on 40 nodes', est: 10, state: 'done', proof: 'run:1048' },
      { t: 'Load one level at a time; children on expand', est: 25, state: 'done', proof: 'run:1061' },
      { t: 'Prove the PER-1 comparison with a run', est: 10, state: 'done', proof: 'run:1061' }
    ],
    checks: ['run:1048', 'run:1061'], changes: { files: [['file:treequery', 14, 9]], types: [] },
    time: { so: 58, est: 45, factor: 1.3, forecast: 58 }, needs: [], blocks: [], commentScope: [],
    note: { text: 'One level per query; 38 ms warm for 40 nodes, under the 50 ms target settled in the plan.', date: '5 Nov 20:02', session: 'session:a71c' },
    rulings: ['ruling:R-02'], sessions: [['session:a71c', 58]],
    info: { tpl: 'task', purpose: 'Implements PER-1: the tree reloads one level, not the whole tree.', rulings: ['ruling:R-02'], files: [['file:treequery', 'the query behind the tree']],
      acceptance: [{ t: 'Under 50 ms warm for a tree of 40 nodes', proof: 'run:1061' }], risks: ['Deep trees now take one query per level opened.'], unblocks: [] },
    history: [
      ['5 Nov 18:50', 'opened', 'You added the item under [[item:WB-2]] to implement [[fpb:PER-1]]'],
      ['5 Nov 19:05', 'plan approved', 'You approved [[card:pl-25]]; the 50 ms target settled with it'],
      ['5 Nov 19:58', 'check', '[[run:1061]] times 38 ms, was 400 ms'],
      ['5 Nov 20:30', 'packet approved', 'You approved [[card:pk-25]]; merged as [[commit:e93b0d1]]']
    ]
  };
  W.items['WB-2'].children.push('WB-2.5');
  W.files['treequery'] = { path: 'src/Qotoba.Workbench.Service/Tree/TreeQuery.cs', repo: 'Qotoba', proj: 'Workbench', folder: 'src/Qotoba.Workbench.Service/Tree', branch: 'br-25', seen: true, newLines: 0, lastSeen: '5 Nov 20:40', added: false,
    lines: ['namespace Qotoba.Workbench.Service.Tree;', '', 'public sealed class TreeQuery(WorkbenchDb db)', '{', '    // One level per call: the tree opens a node, then asks for its children.', '    public Task<IReadOnlyList<TreeRow>> Level(ItemId? parent, CancellationToken ct) =>', '        db.Items.Where(i => i.Parent == parent)', '                .OrderBy(i => i.Order)', '                .Select(TreeRow.From)', '                .ToListAsync(ct);', '}'] };
  W.branches['br-25'] = { name: 'wb/2.5-tree-query', item: 'WB-2.5', machine: 'desk-pc', state: 'merged', ahead: 0, behind: 0, pushed: true, merged: 'commit:e93b0d1', worktree: 'removed 5 Nov 20:34' };
  W.commits['e93b0d1'] = { msg: 'Merge WB-2.5 Tree query under 50 ms', date: '5 Nov 20:32', session: 'a71c', branch: 'br-25', files: 1, plus: 14, minus: 9,
    diff: ['-    public Task<IReadOnlyList<TreeRow>> All(CancellationToken ct) =>', '-        db.Items.Select(TreeRow.From).ToListAsync(ct);', '+    public Task<IReadOnlyList<TreeRow>> Level(ItemId? parent, CancellationToken ct) =>', '+        db.Items.Where(i => i.Parent == parent)'] };
  W.runs['1061'] = { check: 'tree-query-timing', item: 'WB-2.5', session: 'a71c', machine: 'desk-pc', result: 'pass', value: '38 ms', dur: 4, at: '5 Nov 19:58',
    cmd: 'qwb time tree --nodes 40 --warm --runs 5', exitCode: 0, out: ['tree-query-timing · 40 nodes · warm · 5 runs', '  median 38 ms (was 400 ms in run 1048)', '  target under 50 ms · pass'] };
  W.sessions['a71c'] = { title: 'Tree query under 50 ms', machine: 'desk-pc', item: 'WB-2.5', lane: 'A', state: 'ended', started: '5 Nov 19:06', ended: '5 Nov 20:04', minutes: 58, turns: 22, tools: 71,
    step: 'packet drawn', lastAction: 'ended with the packet waiting', waitingSince: null, waitingOn: [], packet: 'card:pk-25', checks: ['run:1061'], questions: [], assumptions: [], found: [],
    ledger: [['19:06', 'start', 'Brief delivered: WB-2.5, which implements PER-1'], ['19:20', 'step', 'one level per query'], ['19:58', 'check', 'tree-query-timing 38 ms · run 1061'], ['20:04', 'packet', 'Packet drawn; session ended']] };
  W.machines['desk-pc'].sessions.push('a71c');
  W.plans['pl-25'] = { item: 'WB-2.5', session: 'a71c', machine: 'desk-pc', lane: 'A', factor: 1.3, factorNote: 'lane A factor 1.3', approved: '5 Nov 19:05',
    goal: 'Opening an item loads one level of the tree, so the query stays under 50 ms warm for 40 nodes.',
    steps: [{ t: 'Measure the tree query on 40 nodes', est: 10, needs: [], proof: 'tree-query-timing' }, { t: 'Load one level at a time; children on expand', est: 25, needs: ['settle:s1'], proof: 'tree-query-timing' }, { t: 'Prove the PER-1 comparison with a run', est: 10, needs: [], proof: 'tree-query-timing' }],
    settle: [{ id: 's1', q: 'Performance target', options: ['Under 50 ms warm for a tree of 40 nodes', 'Under 100 ms', 'No target'], rec: 0, from: 'fpb:PER-1' }], opensBranch: 'br-25' };
  W.packets['pk-25'] = { item: 'WB-2.5', session: 'a71c', machine: 'desk-pc', state: 'approved', approved: '5 Nov 20:30', merged: 'commit:e93b0d1', outcome: 'The tree query takes 38 ms for 40 nodes, was 400 ms',
    time: { actual: 58, est: 45, factor: 1.3 }, checks: [{ label: 'timing', ref: 'run:1061', text: 'tree-query-timing 38 ms' }], assumptions: [], found: [], commentsHandled: [], commentsOpen: [], beyond: [],
    claims: [{ t: 'PER-1 comparison: 0.4 s → 38 ms, under the 50 ms target', proof: 'run:1061' }], note: { text: 'One level per query. Deep trees cost one query per level opened.', date: '5 Nov 20:02' },
    deliver: 'session a71c on desk-pc', toast: 'Already approved and merged as e93b0d1', instruction: ['# delivered to session a71c on desk-pc · packet pk-25 · approved by owner 5 Nov 20:30', 'git -C D:\\Development\\repos\\Qotoba merge --no-ff wb/2.5-tree-query', '# reported: merged as e93b0d1'] };
  W.cards['pl-25'] = { type: 'plan', item: 'WB-2.5', project: 'wb', session: 'a71c', weight: 0, waiting: '', at: '5 Nov 19:02', approved: '5 Nov 19:05', headline: 'Plan: tree query under 50 ms, 3 steps' };
  W.cards['pk-25'] = { type: 'packet', item: 'WB-2.5', project: 'wb', session: 'a71c', weight: 0, waiting: '', at: '5 Nov 20:04', approved: '5 Nov 20:30', headline: 'The tree query takes 38 ms, was 400 ms' };
  var p1 = W.fpbs['PER-1'];
  p1.state = 'verified'; p1.takenOnBy = ['WB-2.5']; p1.checks = ['run:1048', 'run:1061'];
  p1.source = { type: 'promoted', found: 'found:f3', session: 'session:51b0', item: 'item:WB-1', date: '5 Nov 17:40' };
  p1.sections.headline = { author: 'machine', before: { v: 400, unit: 'ms', run: 'run:1048', label: 'median of 5, 40 nodes' }, after: { v: 38, unit: 'ms', run: 'run:1061', label: 'median of 5, 40 nodes' } };
  p1.sections.target = { author: 'claude', v: 50, unit: 'ms', text: 'Under 50 ms warm for a tree of 40 nodes.', by: 'claude', date: '5 Nov 17:42' };
  p1.history = [['5 Nov 17:20', 'session:51b0', 'Found while opening items; [[run:1048]] timed 400 ms'], ['5 Nov 17:40', 'you', 'Promoted from the found tray'],
    ['5 Nov 18:50', 'you', 'Added [[item:WB-2.5]] to take this on'], ['5 Nov 19:05', 'you', 'Approved [[card:pl-25]]; the 50 ms target settled'], ['5 Nov 19:58', 'machine:desk-pc', '[[run:1061]] proves the comparison: 38 ms']];
  p1.sections.implementing = { author: 'claude', lines: [
    { kind: 'decision', text: 'Performance target', rec: 'Under 50 ms warm for 40 nodes', why: 'Opening an item should feel instant; 50 ms is below what you notice.', cost: 'low', status: 'settled', ref: 'card:pl-25', date: '5 Nov 19:05', note: 'Under 50 ms chosen · plan for WB-2.5' },
    { kind: 'precondition', text: 'A repeatable timing on 40 nodes exists', status: 'proved', ref: 'run:1048', date: '5 Nov 17:18' },
    { kind: 'proof', text: 'The comparison: 400 ms before, under 50 ms after', status: 'proved', ref: 'run:1061', date: '5 Nov 19:58' }
  ] };

  /* ---------------------------------------------------------------- BUG-1, taken on by WB-2.3 (in progress): one line asked, one scheduled */
  W.items['WB-2.3'].implements = ['fpb:BUG-1'];
  var b1 = W.fpbs['BUG-1'];
  b1.takenOnBy = ['WB-2.3']; b1.state = 'in progress';
  if (b1.severity) { b1.severity.by = 'claude'; b1.severity.confirmed = false; }
  b1.sections.implementing = { author: 'claude', lines: [
    { kind: 'decision', text: 'How to order two events from one session', rec: 'The Host\'s per-session sequence number (Host 0.5)', why: 'Fixes clock skew for good.', cost: 'medium', status: 'asked', ref: 'card:q-comment', date: '6 Nov 04:38' },
    { kind: 'proof', text: 'Two tool calls finishing in the same second keep their order', status: 'scheduled', ref: 'item:WB-2.3', note: 'Will be proved at step 4 of Order hook events across machines' }
  ] };

  /* ---------------------------------------------------------------- FTR-1, at rest: no task has taken it on */
  W.fpbs['FTR-1'].takenOnBy = [];
  W.fpbs['FTR-1'].sections.implementing = { author: 'claude', lines: [
    { kind: 'decision', text: 'Build the verbs on first tap or on first hover?', rec: 'First tap', why: 'Phones have no hover; one rule for both.', cost: 'low', status: 'open' },
    { kind: 'proof', text: 'Acceptance: the first tap on a card shows its verbs within 50 ms', status: 'open' },
    { kind: 'dependency', text: 'Likely depends on PER-2: lazy verbs pay off once the rail realises 24 cards, not 1,356', status: 'open' },
    { kind: 'precondition', text: 'Dope is open, so a task can be added under it', status: 'open' }
  ] };

  /* ---------------------------------------------------------------- FTR-2: the two deferred questions become decision lines */
  ['Q-10', 'Q-11'].forEach(function (q) { W.questions[q].state = 'not asked'; W.questions[q].card = null; });
  delete W.cards['q-f2a']; delete W.cards['q-f2b'];
  hist(W.fpbs['FTR-2'].history, 'Plan of 6 steps approved', ['4 Nov 21:30', 'you', 'Spike plan of 6 steps recorded; [[question:Q-10]] and [[question:Q-11]] became When implementing lines for whichever task takes this on']);
  W.sessions['9c40'].lastAction = 'ended; two decisions recorded on the report';
  W.fpbs['FTR-2'].takenOnBy = [];
  W.fpbs['FTR-2'].sections.implementing = { author: 'claude', lines: [
    { kind: 'decision', text: 'Should a computed member cache its value until a member it reads changes?', rec: 'Cache, and invalidate when a member it reads changes', why: 'Cheap reads in lists; needs the observable core.', cost: 'medium', status: 'open', ref: 'question:Q-10' },
    { kind: 'decision', text: 'Default values: constructor or initialiser?', rec: 'Property initialisers in C#, constructor defaults in TypeScript', why: 'Each target reads naturally; the parity row stays the same.', cost: 'low', status: 'open', ref: 'question:Q-11' },
    { kind: 'proof', text: 'Acceptance: observable members raise change events through the observable core', status: 'open' },
    { kind: 'proof', text: 'Acceptance: the emitted TypeScript record passes tsc --strict', status: 'open' },
    { kind: 'dependency', text: 'Step 6, observable members, needs Observable core', status: 'open', ref: 'item:FW-1' },
    { kind: 'precondition', text: 'The QDL parser handles the class grammar', status: 'open', ref: 'item:LNG-1' }
  ] };

  /* ---------------------------------------------------------------- FTR-3 and PER-2 */
  W.fpbs['FTR-3'].takenOnBy = [];
  W.fpbs['FTR-3'].sections.implementing = { author: 'claude', lines: [
    { kind: 'decision', text: 'Run on every .qo file, or only the changed ones?', rec: 'Every file', why: 'The corpus runs in about a minute; partial runs miss printer regressions.', cost: 'low', status: 'open' },
    { kind: 'proof', text: 'Acceptance: the parser corpus of 1,204 files round-trips with no mismatch', status: 'proved', ref: 'run:1022', date: '5 Nov 02:00', note: 'proved while LNG-1.1 proved its own line' }
  ] };
  W.fpbs['PER-2'].takenOnBy = [];
  if (W.fpbs['PER-2'].sections.target) { W.fpbs['PER-2'].sections.target.author = 'claude'; W.fpbs['PER-2'].sections.target.by = 'claude'; }
  W.fpbs['PER-2'].sections.implementing = { author: 'claude', lines: [
    { kind: 'decision', text: 'Performance target', rec: 'Under 1 s cold on the laptop', why: 'The user-visible budget for opening the library.', cost: 'low', status: 'settled', ref: 'ruling:R-12', date: '29 Oct', note: 'Under 1 s · imported with the Dope seed as R-12' },
    { kind: 'proof', text: 'The library opens under 1 s cold', status: 'proved', ref: 'run:0931', date: '30 Oct' },
    { kind: 'precondition', text: 'Dope is open before any further work', status: 'open' }
  ] };
  Object.keys(W.reportOrder).forEach(function (k) { W.reportOrder[k].unshift(['implementing', 'When implementing']); });

  /* ---------------------------------------------------------------- assumptions and found items: the packet card carries 4 and 3 */
  delete W.assumptions.a10; delete W.assumptions.a12;
  W.sessions.c21e.assumptions = ['a9', 'a11', 'a13', 'a14']; W.packets['pk-runs'].assumptions = ['a9', 'a11', 'a13', 'a14'];
  W.found.f6 = { kind: 'FTR', title: 'Runs table could group by machine', session: 'c21e', item: 'WB-2.1', state: 'in tray', found: '6 Nov 03:44',
    why: 'With two machines posting, the table interleaves their runs; a machine group reads faster.', evidence: 'run:1043', suggest: 'WB-3' };
  W.sessions.c21e.found.push('found:f6'); W.packets['pk-runs'].found.push('f6');

  /* ---------------------------------------------------------------- rulings proposed from chat: one Action card */
  W.cards['rp-chat'] = { type: 'proposals', item: 'WB-2', project: 'wb', session: null, weight: 0, waiting: '7 h 32 min', at: '5 Nov 21:40', headline: '4 rulings proposed from chat' };
  W.proposed.card = 'rp-chat'; W.proposed.item = 'WB-2';

  /* ---------------------------------------------------------------- a preview built from the seed is not pushed: it waits for Dope to open */
  W.cards['pk-dope'].preview = true;

  /* ---------------------------------------------------------------- every card: its task, its step, and where it came from */
  W.cardOrigin = {
    'q-design': { step: 'step 3 of 5', from: ['session:7f3a'] },
    'q-unseen': { step: 'step 4 of 5', from: ['session:7f3a', 'file:runrecord'] },
    'q-comment': { step: 'step 3 of 4', from: ['fpb:BUG-1', 'comment:c4'], fromText: 'a When implementing line of BUG-1 and your + comment' },
    'pk-runs': { step: 'packet, after step 5 of 5', from: ['session:c21e'] },
    'pl-24': { step: 'plan, before step 1', from: ['session:2a6d'] },
    'st-23': { step: 'step 2 of 4', from: ['session:b915'] },
    'rp-chat': { step: 'step 3 of 5, the inbox design', from: [], fromText: 'the design chat on 5 Nov at 21:40, about deferral and clearing' },
    'pk-dope': { step: 'no step: a preview', from: ['fpb:FTR-1'] },
    'pk-hello': { step: 'packet, after step 5 of 5', from: ['session:51b0'] },
    'pl-lng11': { step: 'plan, before step 1', from: ['session:e5c1', 'fpb:BUG-2'], fromText: 'BUG-2 · When implementing' },
    'q-lng11': { step: 'step 2 of 4', from: ['fpb:BUG-2', 'session:e5c1'], fromText: 'a blocking decision line of BUG-2' },
    'pk-lng11': { step: 'packet, after step 4 of 4', from: ['session:e5c1'] },
    'pl-25': { step: 'plan, before step 1', from: ['session:a71c', 'fpb:PER-1'], fromText: 'PER-1 · When implementing' },
    'pk-25': { step: 'packet, after step 3 of 3', from: ['session:a71c'] },
    'i1': { step: 'packet, after step 5 of 5', from: ['session:c21e'] }, 'i4': { step: 'packet, after step 5 of 5', from: ['session:c21e'] }, 'i5': { step: 'packet, after step 5 of 5', from: ['session:c21e'] },
    'i2': { step: null, from: ['machine:cloud-1'], fromText: 'not a task: you registered a machine' },
    'i3': { step: null, from: [], fromText: 'not a task: you imported the Dope seed' },
    'i6': { step: null, from: ['fpb:PER-2'], fromText: 'not a task: a seeded run on a report' }
  };
  ['i1', 'i4', 'i5'].forEach(function (k) { if (!W.cards[k].item) W.cards[k].item = 'WB-2.1'; });
  W.cardWhy = {
    'q-design': 'Blocking question on step 3 of Run page with output',
    'q-unseen': 'Blocking question on step 4 of Run page with output, about a file you have not read yet',
    'q-comment': 'Blocking decision from BUG-1, step 3 of Order hook events across machines',
    'pk-runs': 'All checks passed on Runs table fills from hook events, ready to merge',
    'pk-dope': 'A preview of Lazy card verbs built from the Dope seed; nothing to merge while Dope is closed',
    'pk-hello': 'All checks passed on Hello card across two machines; you approved it and it merged',
    'pl-24': 'Plan for Check chips on the summary block needs your approval before step 1',
    'st-23': 'Failed three times on step 2 of Order hook events across machines',
    'rp-chat': 'Four rulings proposed in the design chat about WB-2 need your tick before they are recorded',
    'pl-lng11': 'Plan for Keep comments when string recovery skips a line needed your approval; it carried defaults from BUG-2',
    'q-lng11': 'Blocking decision from BUG-2, step 2 of Keep comments when string recovery skips a line',
    'pk-lng11': 'All checks passed on Keep comments when string recovery skips a line; it proves the BUG-2 acceptance line',
    'pl-25': 'Plan for Tree query under 50 ms needed your approval; it carried the target from PER-1',
    'pk-25': 'All checks passed on Tree query under 50 ms; it proves the PER-1 comparison',
    'i1': 'A lane factor changed when Runs table fills from hook events finished', 'i2': 'A machine registered', 'i3': 'A seed was imported',
    'i4': 'A branch was pushed for Runs table fills from hook events', 'i5': 'A session ended on Runs table fills from hook events', 'i6': 'A seeded run met the PER-2 target'
  };
})(WB);

/* ================================================================ round 2, follow-up · test data only
   FTR-2's spike becomes a task that implements it; tasks record the decisions made for them;
   decision lines carry their options, each with its own cost to change. */
(function (W) {
  function hist(list, find, repl) { for (var i = 0; i < list.length; i++) if (JSON.stringify(list[i]).indexOf(find) >= 0) { list[i] = repl; return true; } return false; }

  /* ---------------------------------------------------------------- FTR-2: the spike is task LNG-5.1, which implements it */
  var f2 = W.fpbs['FTR-2'];
  W.items['LNG-5.1'] = {
    kind: 'task', title: 'Class construct spike', project: 'lang', parent: 'LNG-5', children: [], state: 'paused', isOpen: false,
    lane: null, machine: 'desk-pc', branch: null, coarse: '~1 week', implements: ['fpb:FTR-2'], plan: 'card:pl-lng51',
    goal: 'Prove the class construct end to end in the Quanta repo: parse, print, check and emit both targets, with observable members.',
    steps: [
      { t: 'Grammar and syntax tree for class and members', est: 60, state: 'done', proof: 'run:0950' },
      { t: 'Printer round-trip for class', est: 40, state: 'done', proof: 'run:0950' },
      { t: 'Checker rules: duplicate members, unknown types', est: 90, state: 'todo', proof: 'nyw:ClassDecl' },
      { t: 'Emit computed members', est: 90, state: 'todo', proof: 'nyw:ClassEmitter', needs: ['question:Q-10'] },
      { t: 'Emit defaults on both targets', est: 60, state: 'todo', proof: 'nyw:ClassEmitter', needs: ['question:Q-11'] },
      { t: 'Observable members through the observable core', est: 120, state: 'todo', proof: 'nyw:check-observable', needs: ['item:FW-1'] }
    ],
    checks: ['run:0950', 'run:0951'], changes: { files: [], types: [] },
    time: { so: 95, est: 460, factor: 1.2, forecast: 552 }, needs: [{ ref: 'item:LNG-1', note: 'the class grammar extends the parser' }, { ref: 'item:FW-1', note: 'step 6 only · proposed by Claude' }], blocks: [], commentScope: [],
    note: { text: 'Parse, print and C# emit work for five member kinds. Paused after step 2: Language is closed, and steps 4 and 5 wait on two decisions recorded on FTR-2.', date: '4 Nov 19:38', session: 'session:9c40' },
    rulings: ['ruling:R-14', 'ruling:R-15', 'ruling:R-06'], sessions: [['session:9c40', 95]],
    info: { tpl: 'task', purpose: 'A spike opened by you while Language is closed: it implements FTR-2 in the Quanta repo and pauses until Language opens.',
      rulings: ['ruling:R-14', 'ruling:R-15', 'ruling:R-06'], files: [],
      acceptance: [{ t: 'A class with five kinds of member parses and prints back unchanged', proof: 'run:0950' }, { t: 'The emitted C# class builds with warnings as errors', proof: 'run:0951' }],
      risks: ['Observable members wait on the Framework\'s observable core.'], unblocks: [] },
    history: [
      ['4 Nov 18:02', 'opened', 'You added the item under [[item:LNG-5]] to implement [[fpb:FTR-2]]'],
      ['4 Nov 18:10', 'plan submitted', 'Session [[session:9c40]] drew a plan of 6 steps from the report\'s When implementing lines'],
      ['4 Nov 18:12', 'plan approved', 'You approved [[card:pl-lng51]]; the spike runs in the Quanta repo while Language is closed'],
      ['4 Nov 19:36', 'decisions recorded', '[[question:Q-10]] and [[question:Q-11]] were not asked: steps 4 and 5 were not reached. They wait on FTR-2 as When implementing lines'],
      ['4 Nov 19:40', 'paused', 'Session ended after step 2; the spike resumes when Language opens']
    ],
    decisions: [
      { t: 'Language is closed: run the spike in the Quanta repo', a: 'Accept', how: 'settle', ref: 'card:pl-lng51', from: 'fpb:FTR-2', date: '4 Nov 18:12' }
    ]
  };
  W.items['LNG-5'].children = ['LNG-5.1'];
  W.plans['pl-lng51'] = { item: 'LNG-5.1', session: '9c40', machine: 'desk-pc', lane: '-', factor: 1.2, factorNote: 'Language has no lane yet; the programme default 1.2 applies', approved: '4 Nov 18:12',
    goal: 'Prove the class construct end to end in the Quanta repo: parse, print, check and emit both targets, with observable members.',
    steps: [
      { t: 'Grammar and syntax tree for class and members', est: 60, needs: ['settle:s1'], proof: 'specimens · class parse' },
      { t: 'Printer round-trip for class', est: 40, needs: [], proof: 'specimens · class parse' },
      { t: 'Checker rules: duplicate members, unknown types', est: 90, needs: [], proof: 'specimens · class check' },
      { t: 'Emit computed members', est: 90, needs: [], proof: 'specimens · class emit C#' },
      { t: 'Emit defaults on both targets', est: 60, needs: [], proof: 'specimens · class emit TS' },
      { t: 'Observable members through the observable core', est: 120, needs: ['item:FW-1'], proof: 'specimens · class observable' }
    ],
    settle: [{ id: 's1', q: 'Language is closed: run the spike in the Quanta repo', options: ['Accept', 'Wait for Language to open'], rec: 0, from: 'fpb:FTR-2' }], opensBranch: null };
  W.cards['pl-lng51'] = { type: 'plan', item: 'LNG-5.1', project: 'lang', session: '9c40', weight: 0, waiting: '', at: '4 Nov 18:10', approved: '4 Nov 18:12', headline: 'Plan: class construct spike, 6 steps' };
  W.cardOrigin['pl-lng51'] = { step: 'plan, before step 1', from: ['session:9c40', 'fpb:FTR-2'], fromText: 'FTR-2 · When implementing' };
  W.cardWhy['pl-lng51'] = 'Plan for Class construct spike needed your approval; it carried a precondition from FTR-2';

  ['0950', '0951'].forEach(function (r) { W.runs[r].item = 'LNG-5.1'; });
  var s9 = W.sessions['9c40'];
  s9.item = 'LNG-5.1'; s9.step = 'steps 1 and 2 done; paused while Language is closed'; s9.lastAction = 'ended after step 2; two decisions recorded on FTR-2';
  s9.ledger = [['18:05', 'start', 'Brief delivered: LNG-5.1, which implements FTR-2, in the Quanta repo'], ['18:10', 'plan', 'Plan of 6 steps drawn from FTR-2 · When implementing'], ['18:12', 'approved', 'You approved the plan'],
    ['18:40', 'write', 'Grammar for class and members'], ['19:30', 'check', 'specimens 6/6 · run 0950'],
    ['19:32', 'note', 'Q-10 · do computed members cache: recorded on FTR-2 for step 4'], ['19:36', 'note', 'Q-11 · defaults per target: recorded on FTR-2 for step 5'],
    ['19:38', 'note', 'Note written (2 lines)'], ['19:41', 'check', 'C# emit builds · run 0951']];
  ['Q-10', 'Q-11'].forEach(function (q) { W.questions[q].item = 'LNG-5.1'; W.questions[q].report = 'FTR-2'; });

  // the report keeps what it is about; the plan, runs, sessions and time belong to the task
  delete f2.steps; delete f2.checks; delete f2.changes; delete f2.commentScope; delete f2.machine; delete f2.branch; delete f2.lane;
  f2.sessions = []; delete f2.sections.plan;
  f2.time = { est: 460, estBy: 'from the plan of Class construct spike, which takes this on' };
  f2.state = 'in progress'; f2.takenOnBy = ['LNG-5.1'];
  f2.sections.note.text = 'Parse, print and C# emit work for five member kinds in the spike. Observability waits on the Framework core; two decisions wait as When implementing lines until the spike resumes.';
  hist(f2.history, 'Spike started on', ['4 Nov 18:02', 'you', 'Added [[item:LNG-5.1]] to take this on; its session [[session:9c40]] ran the spike on [[machine:desk-pc]] in the Quanta repo']);
  hist(f2.history, 'Spike plan of 6 steps recorded', ['4 Nov 19:40', 'session:9c40', 'Paused after step 2; [[question:Q-10]] and [[question:Q-11]] were not asked and stay here as When implementing lines']);
  f2.history.push(['4 Nov 18:12', 'you', 'Approved [[card:pl-lng51]] for [[item:LNG-5.1]]']);
  function tkey(e) { var m = /(\d+) Nov (\d+):(\d+)/.exec(e[0]); return m ? (+m[1]) * 10000 + (+m[2]) * 100 + (+m[3]) : 0; }
  f2.history.sort(function (a, b) { return tkey(a) - tkey(b); });
  f2.sections.implementing.lines = [
    { kind: 'decision', q: 'Q-10', text: 'Should a computed member cache its value until a member it reads changes?', status: 'scheduled', ref: 'item:LNG-5.1', note: 'Will be asked on a Question Card when Class construct spike reaches step 4' },
    { kind: 'decision', q: 'Q-11', text: 'Default values: constructor or initialiser?', status: 'scheduled', ref: 'item:LNG-5.1', note: 'Will be a default in the settle pass when Class construct spike resumes' },
    { kind: 'proof', text: 'Acceptance: observable members raise change events through the observable core', status: 'scheduled', ref: 'item:LNG-5.1', note: 'Will be proved at step 6 of Class construct spike' },
    { kind: 'proof', text: 'Acceptance: the emitted TypeScript record passes tsc --strict', status: 'scheduled', ref: 'item:LNG-5.1', note: 'Will be proved at step 5 of Class construct spike' },
    { kind: 'dependency', text: 'Step 6, observable members, needs Observable core (FW-1)', status: 'scheduled', ref: 'item:LNG-5.1', note: 'Will be confirmed in the settle pass when Class construct spike resumes' },
    { kind: 'precondition', text: 'Language is closed, so the spike runs in the Quanta repo', status: 'settled', ref: 'card:pl-lng51', date: '4 Nov 18:12', note: 'Accepted with the plan for LNG-5.1' },
    { kind: 'precondition', text: 'The QDL parser handles the class grammar', status: 'proved', ref: 'run:0950', date: '4 Nov 19:30' }
  ];

  /* ---------------------------------------------------------------- "unblocks" weight is a card's; a report's links carry no minutes */
  W.edges.forEach(function (e) { if (/^fpb:/.test(e.from) || /^fpb:/.test(e.to)) delete e.min; });

  /* ---------------------------------------------------------------- tasks record the decisions made for them */
  W.items['LNG-1.1'].decisions = [
    { t: 'Which fix?', a: 'Fix 2: attach skipped tokens as trivia on the error node', how: 'settle', ref: 'card:pl-lng11', from: 'fpb:BUG-2', date: '5 Nov 01:05' },
    { t: 'Depends on FTR-3: the round-trip check proves the fix', a: 'Accept', how: 'settle', ref: 'card:pl-lng11', from: 'fpb:BUG-2', date: '5 Nov 01:05' },
    { t: 'A comment inside the unterminated string belongs to the string', a: 'Yes, keep it with the string', how: 'settle', ref: 'card:pl-lng11', from: 'fpb:BUG-2', date: '5 Nov 01:05' },
    { t: 'May recovery discard the tokens it skips?', a: 'Never discard: skipped text becomes trivia on the error node', how: 'question', ref: 'card:q-lng11', from: 'fpb:BUG-2', date: '5 Nov 01:52', ruling: 'ruling:R-13' }
  ];
  W.items['WB-2.5'].decisions = [
    { t: 'Performance target', a: 'Under 50 ms warm for a tree of 40 nodes', how: 'settle', ref: 'card:pl-25', from: 'fpb:PER-1', date: '5 Nov 19:05' }
  ];

  /* ---------------------------------------------------------------- decision lines carry their options, each with its reason and cost to change */
  function opts(fpb, text, list) { var l = W.fpbs[fpb].sections.implementing.lines.filter(function (x) { return x.kind === 'decision' && x.text.indexOf(text) === 0; })[0]; if (l) l.options = list; }
  opts('BUG-2', 'Which fix', [
    { t: 'Fix 2: attach skipped tokens as trivia on the error node', why: 'Low risk, high confidence, and a regression check already exists', cost: 'low, one attach call', rec: true },
    { t: 'Fix 1: stop recovery at the comment start', why: 'Leaves a stray quote the printer must special-case', cost: 'medium, a printer rule per string kind' },
    { t: 'Fix 3: re-lex the line after recovery', why: 'Sure to work, but 90 min and high risk to the lexer', cost: 'high, the lexer\'s recovery contract' }]);
  opts('BUG-2', 'Does a comment inside', [
    { t: 'Yes, keep it with the string', why: 'The lexer already reads to the line end; the printer gives it back as written', cost: 'low, one test', rec: true },
    { t: 'No, lex it as a comment', why: 'Reads better, but changes how every unterminated string recovers', cost: 'medium, every recovery specimen' }]);
  opts('FTR-1', 'Build the verbs', [
    { t: 'On first tap', why: 'Phones have no hover; one rule for both', cost: 'low, one event handler', rec: true },
    { t: 'On first hover', why: 'A little faster on desktop, nothing on phones', cost: 'low, one event handler' }]);
  opts('FTR-3', 'Run on every', [
    { t: 'Every .qo file', why: 'The corpus runs in about a minute; partial runs miss printer regressions', cost: 'low, one check setting', rec: true },
    { t: 'Only the changed files', why: 'Faster, but misses regressions in files nobody touched', cost: 'low, one check setting' }]);
  opts('PER-1', 'Performance target', [
    { t: 'Under 50 ms warm for a tree of 40 nodes', why: 'Below what you notice when an item opens', cost: 'low, the check\'s limit', rec: true },
    { t: 'Under 100 ms', why: 'Noticeable on the laptop', cost: 'low, the check\'s limit' },
    { t: 'No target', why: 'Nothing to prove the fix against', cost: 'medium, a guard written later' }]);
  opts('PER-2', 'Performance target', [
    { t: 'Under 1 s cold on the laptop', why: 'The user-visible budget for opening the library', cost: 'low, the guard\'s limit', rec: true },
    { t: 'Under 2 s cold', why: 'Feels slow the first time the library opens', cost: 'low, the guard\'s limit' }]);
})(window.WB);
