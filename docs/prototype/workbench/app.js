/* Qotoba Workbench prototype · app.
   Vanilla JavaScript, no build step. A hash router draws every screen from window.WB (data.js).
   Buttons that would change data only raise a toast; every navigation is real. */
(function () {
  'use strict';
  var D = window.WB;

  /* ================================================================ utilities */
  function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
  function fmtMin(m) { if (m == null) return '—'; m = Math.round(m); if (m < 60) return m + 'm'; var h = Math.floor(m / 60), r = m % 60; return h + 'h ' + (r < 10 ? '0' : '') + r + 'm'; }
  function fmtSec(s) { if (s < 60) return s + ' s'; var m = Math.floor(s / 60), r = s % 60; return m + 'm ' + (r < 10 ? '0' : '') + r + 's'; }
  function trunc(s, n) { s = String(s); return s.length > n ? s.slice(0, n - 1) + '…' : s; }
  function plural(n, one, many) { return n + ' ' + (n === 1 ? one : (many || one + 's')); }
  function by(arr, f) { return arr.slice().sort(function (a, b) { var x = f(a), y = f(b); return x < y ? -1 : x > y ? 1 : 0; }); }
  function keys(o) { return Object.keys(o || {}); }
  function store(k, v) { try { if (v === undefined) return localStorage.getItem('qwb.' + k); localStorage.setItem('qwb.' + k, v); } catch (e) { return null; } }
  function timeKey(at) { var m = /(\d+) Nov (\d+):(\d+)/.exec(at || ''); if (m) return (+m[1]) * 1440 + (+m[2]) * 60 + (+m[3]); var t = /(\d+):(\d+)/.exec(at || ''); return t ? 6 * 1440 + (+t[1]) * 60 + (+t[2]) : 0; }
  function projOf(id) { return D.projects.filter(function (p) { return p.id === id; })[0]; }

  /* ================================================================ icons (16px stroke) */
  var P = {
    ruling: 'M8 2.5v11M4.5 13.5h7M3 5h10M3 5l-1.8 4.2a1.9 1.9 0 0 0 3.6 0zM13 5l-1.8 4.2a1.9 1.9 0 0 0 3.6 0z',
    item: 'M3 2.5h10v11H3zM5.5 6h5M5.5 9h5M5.5 12h2.5',
    project: 'M1.8 4.2h4.4l1.4 1.5h6.6v7.8H1.8z',
    question: 'M8 14.2A6.2 6.2 0 1 0 8 1.8a6.2 6.2 0 0 0 0 12.4zM6.3 6.2a1.8 1.8 0 1 1 2.5 1.7c-.6.3-.8.6-.8 1.3M8 11.2v.1',
    assumption: 'M4 14V2.2h7.5L10 5.2l1.5 3H4',
    found: 'M2 9.2 3.8 3h8.4L14 9.2V13H2zM2 9.2h3.6l.9 1.6h3l.9-1.6H14',
    file: 'M4 1.8h5.2L12.5 5v9.2H4zM9 1.8V5h3.5',
    type: 'M6 2.2c-1.6 0-1.8.9-1.8 2.4S4 7.8 2.6 8c1.4.2 1.6 1.9 1.6 3.4s.2 2.4 1.8 2.4M10 2.2c1.6 0 1.8.9 1.8 2.4s.2 3.2 1.6 3.4c-1.4.2-1.6 1.9-1.6 3.4s-.2 2.4-1.8 2.4',
    run: 'M5 3.2v9.6L12.8 8z',
    session: 'M2 3h12v10H2zM4.6 6.4 6.8 8l-2.2 1.6M8.4 10.2h3',
    machine: 'M2 3h12v8H2zM5.8 14h4.4M8 11v3',
    comment: 'M2 3h12v8.2H7.4L4.3 14v-2.8H2zM8 5v4M6 7h4',
    claudemd: 'M3.5 2h9v12h-9zM5.6 5h4.8M5.6 7.8h4.8M5.6 10.6h3',
    commit: 'M1.5 8h3.7M10.8 8h3.7M8 10.6a2.6 2.6 0 1 0 0-5.2 2.6 2.6 0 0 0 0 5.2z',
    branch: 'M5 2.5v11M5 10.5c0-3.5 6-2 6-6M5 13.5a1.4 1.4 0 1 0 0-.1M11 4.5a1.4 1.4 0 1 0 0-.1',
    card: 'M2 4h12v9H2zM2 7h12M4.5 10h4',
    lane: 'M3 2v12M8 2v12M13 2v12M3 6h5M8 10h5',
    nyw: 'M3 3h10v10H3zM6 8h4',
    inbox: 'M1.8 9 3.6 3h8.8L14.2 9v4.2H1.8zM1.8 9h3.8l1 1.6h2.8l1-1.6h3.8',
    work: 'M3 2.5v11M3 5h4M3 9.5h4M7 3h6.5v4H7zM7 7.5h6.5v4H7z',
    now: 'M1.5 8.5h3l2-5 3 9 2-4h3',
    files: 'M4 1.8h5.2L12.5 5v9.2H4zM9 1.8V5h3.5M6.5 9.5h3.5M8.2 7.8v3.4',
    rulings: 'M8 2.5v11M4.5 13.5h7M3 5h10M3 5l-1.8 4.2a1.9 1.9 0 0 0 3.6 0zM13 5l-1.8 4.2a1.9 1.9 0 0 0 3.6 0z',
    more: 'M3.5 8h.1M8 8h.1M12.5 8h.1',
    projects: 'M2 2.5h5v5H2zM9 2.5h5v5H9zM2 9.5h5v4H2zM9 9.5h5v4H9z',
    machines: 'M1.5 3h8v6h-8zM4 12h3M5.5 9v3M11 5h3.5v8.5H11zM12.2 11h1',
    settings: 'M2.5 4h11M2.5 8h11M2.5 12h11M5 2.6v2.8M10.5 6.6v2.8M7 10.6v2.8',
    search: 'M7 12a5 5 0 1 0 0-10 5 5 0 0 0 0 10zM10.6 10.6 14 14',
    screens: 'M2 3h12v10H2zM6 3v10M8.5 6h3.5M8.5 8.5h3.5',
    back: 'M10 3 5 8l5 5',
    close: 'M3.5 3.5l9 9M12.5 3.5l-9 9',
    dots: 'M3.5 8h.1M8 8h.1M12.5 8h.1',
    rider: 'M2 2h12v12H2zM4.5 11.5h4',
    diff: 'M5 2.5v6M2 5.5h6M9 11.5h5M11 13.5V7.5',
    grip: 'M6 4h.1M10 4h.1M6 8h.1M10 8h.1M6 12h.1M10 12h.1',
    trash: 'M3 4.5h10M6.5 4.5V3h3v1.5M4.5 4.5l.6 9h5.8l.6-9',
    plus: 'M8 3v10M3 8h10',
    move: 'M8 2v12M2 8h12M6 4l2-2 2 2M6 12l2 2 2-2M4 6 2 8l2 2M12 6l2 2-2 2',
    edit: 'M2.5 13.5 3 11l7.5-7.5 2 2L5 13zM9.5 4.5l2 2',
    open: 'M6 3H3v10h10v-3M9 2h5v5M14 2 7.5 8.5',
    link: 'M6.8 9.2a2.6 2.6 0 0 0 3.7 0l2.2-2.2a2.6 2.6 0 0 0-3.7-3.7l-.6.6M9.2 6.8a2.6 2.6 0 0 0-3.7 0L3.3 9a2.6 2.6 0 0 0 3.7 3.7l.6-.6',
    sun: 'M8 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM8 1.5v1.3M8 13.2v1.3M1.5 8h1.3M13.2 8h1.3',
    talk: 'M2 3h12v8.2H7.4L4.3 14v-2.8H2z',
    ftr: 'M8 2l1.7 3.6 3.9.5-2.9 2.7.7 3.9L8 10.8l-3.4 1.9.7-3.9-2.9-2.7 3.9-.5z',
    bug: 'M5.2 6.5h5.6v4a2.8 2.8 0 0 1-5.6 0zM6 6.5a2 2 0 0 1 4 0M2.5 9h2.7M10.8 9h2.7M3 13l2.3-1.3M13 13l-2.3-1.3M3.5 5l2 1.5M12.5 5l-2 1.5',
    per: 'M2.5 11.5a5.5 5.5 0 1 1 11 0M8 11.5l2.8-3.4M8 11.5h.1',
    reg: 'M2 3h12v3H2zM3 6v7h10V6M6.5 8.5h3',
    issues: 'M8 2l1.7 3.6 3.9.5-2.9 2.7.7 3.9L8 10.8l-3.4 1.9.7-3.9-2.9-2.7 3.9-.5z'
  };
  function ic(name) { return '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="' + (P[name] || P.item) + '"/></svg>'; }

  /* ================================================================ state */
  var S = {
    layout: store('layout') || (window.innerWidth < 700 ? 'phone' : 'desktop'),
    theme: store('theme') || 'system',
    project: 'all',
    sort: 'weight',
    showDeferred: false,
    cleared: false,
    dismissed: {},
    work: { view: 'outline', sel: 'WB-2', kinds: {}, state: 'all', unsched: false, expanded: { root: 1, wb: 1, 'WB-2': 1, dope: 1 } },
    filesScope: 'all', filesItem: 'WB-2.2',
    rulingQ: '',
    stack: []
  };

  /* ================================================================ references */
  var COLL = { fpb: 'fpbs', reg: 'register', ruling: 'rulings', item: 'items', question: 'questions', assumption: 'assumptions', found: 'found', file: 'files', type: 'types', run: 'runs', session: 'sessions', machine: 'machines', comment: 'comments', claudemd: 'claudemd', commit: 'commits', branch: 'branches', card: 'cards', lane: 'lanes', nyw: 'nyw' };
  var KIND_LABEL = { fpb: 'Issue', reg: 'Old register', ruling: 'Ruling', item: 'Item', project: 'Project', question: 'Question', assumption: 'Assumption', found: 'Found item', file: 'File and lines', type: 'Type', run: 'Run', session: 'Session', machine: 'Machine', comment: '+ comment', claudemd: 'CLAUDE.md section', commit: 'Commit', branch: 'Branch', card: 'Card', lane: 'Lane', nyw: 'Not yet written' };
  var REF_RE = /\b(fpb|reg|ruling|item|question|assumption|found|file|type|run|session|machine|comment|claudemd|commit|branch|project|card|lane|nyw):([A-Za-z0-9][A-Za-z0-9._\-]*(?::\d+(?:-\d+)?)?)/g;

  function parseRef(ref) {
    var i = ref.indexOf(':'), kind = ref.slice(0, i), rest = ref.slice(i + 1), range = null;
    if (kind === 'file') { var p = rest.split(':'); rest = p[0]; if (p[1]) { var ab = p[1].split('-').map(Number); range = [ab[0], ab[1] || ab[0]]; } }
    return { kind: kind, id: rest, range: range, base: kind + ':' + rest };
  }
  function get(ref) {
    var r = parseRef(ref);
    if (r.kind === 'project') return projOf(r.id);
    var c = D[COLL[r.kind]];
    return c ? c[r.id] : null;
  }
  function work(id) { return D.items[id] || D.fpbs[id] || null; }
  function workRef(id) { return D.fpbs[id] ? 'fpb:' + id : 'item:' + id; }
  function workHref(id) { return D.fpbs[id] ? '#/fpb/' + id : '#/item/' + id; }
  function itemTitle(id) { var it = work(id); return it ? it.title : id; }

  var STATE_LABEL = { done: 'done', building: 'building', review: 'review', planning: 'planning', planned: 'planned', blocked: 'blocked', coarse: 'coarse' };
  var STATE_TONE = { done: 'good', building: 'acc', review: 'warn', planning: 'acc', planned: 'neutral', blocked: 'bad', coarse: 'neutral', open: 'acc' };
  function statePill(s) { return '<span class="st ' + (STATE_TONE[s] || 'neutral') + '">' + esc(STATE_LABEL[s] || s) + '</span>'; }
  function kindPill(k) { return '<span class="st kind-' + esc(k) + '">' + esc(k) + '</span>'; }
  function resultTag(r) { return '<span class="tag ' + (r === 'pass' ? 'good' : r === 'warn' ? 'warn' : 'bad') + '">' + (r === 'pass' ? 'passed' : r === 'warn' ? 'warned' : 'failed') + '</span>'; }

  function chipInfo(ref) {
    var r = parseRef(ref), e = get(ref), k = r.kind;
    if (!e) return { kind: k, title: r.id, meta: 'missing', id: '', cls: 'missing' };
    switch (k) {
      case 'fpb': return { kind: k, title: e.title, meta: e.state, id: r.id, icon: e.kind.toLowerCase(), cls: 'fk-' + e.kind };
      case 'reg': return { kind: k, title: e.title, meta: e.date, id: r.id };
      case 'ruling': return { kind: k, title: e.title, meta: e.date, id: r.id };
      case 'item': return { kind: k, title: e.title, meta: e.unscheduled ? 'unscheduled' : e.preview ? 'preview' : STATE_LABEL[e.state], id: r.id };
      case 'project': return { kind: k, title: e.name, meta: e.state, id: e.code };
      case 'question': return { kind: k, title: e.short, meta: e.state, id: r.id };
      case 'assumption': return { kind: k, title: e.text, meta: e.state, id: r.id };
      case 'found': return { kind: k, title: e.title, meta: e.kind, id: r.id };
      case 'file':
        var name = e.path.split('/').pop();
        return { kind: k, title: name + (r.range ? ' :' + r.range[0] + (r.range[1] !== r.range[0] ? '–' + r.range[1] : '') : ''), meta: e.seen ? '' : 'new to you', id: e.proj.replace('Qotoba.Workbench.', ''), cls: e.seen ? '' : 'unseen' };
      case 'type': return { kind: k, title: r.id, meta: e.kind, id: e.ns.split('.').pop() };
      case 'run':
        var t = e.check + (e.total ? ' ' + e.passed + '/' + e.total : e.value ? ' · ' + e.value : '');
        return { kind: k, title: t, meta: (e.result === 'pass' ? 'passed' : e.result === 'warn' ? 'warned' : 'failed') + ' ' + e.at.replace(/^\d+ Nov /, ''), id: 'run-' + r.id, cls: e.result === 'pass' ? '' : e.result };
      case 'session': return { kind: k, title: e.title, meta: e.stuck ? 'stuck' : e.state === 'waiting' ? 'waiting on you' : e.state, id: r.id };
      case 'machine': return { kind: k, title: r.id, meta: e.online ? 'online' : 'offline', id: e.kind === 'cloud' ? 'cloud' : '' };
      case 'comment': return { kind: k, title: '//+ ' + e.text, meta: e.state, id: r.id };
      case 'claudemd': return { kind: k, title: 'CLAUDE.md › ' + e.title, meta: '', id: e.n };
      case 'commit': return { kind: k, title: e.msg, meta: e.date.replace(/ \d+:\d+$/, ''), id: r.id };
      case 'branch': return { kind: k, title: e.name, meta: e.state.split(' · ')[0], id: '' };
      case 'card': return { kind: k, title: e.headline, meta: e.approved ? 'approved' : e.preview ? 'preview' : ({ question: 'question', packet: 'approve', plan: 'plan', stuck: 'stuck', info: 'info' })[e.type], id: r.id };
      case 'lane': return { kind: k, title: 'lane ' + r.id + ' · ' + e.name, meta: '×' + e.factor, id: '' };
      case 'nyw': return { kind: k, title: e.title, meta: 'not yet written', id: '', cls: 'nyw' };
    }
    return { kind: k, title: r.id, meta: '', id: '' };
  }
  function chip(ref, opt) {
    opt = opt || {};
    var c = chipInfo(ref);
    var full = c.title;
    var t = trunc(c.title, opt.max || 38);
    return '<button type="button" class="chip k-' + c.kind + ' ' + (c.cls || '') + (opt.lite ? ' lite' : '') + '" data-ref="' + esc(ref) + '" title="' + esc(KIND_LABEL[c.kind] + ': ' + full + (c.id ? ' · ' + c.id : '')) + '">' +
      '<span class="ic">' + ic(c.icon || c.kind) + '</span><span class="t">' + esc(opt.label || t) + '</span>' +
      (c.meta && opt.meta ? '<span class="m">' + esc(c.meta) + '</span>' : '') +
      (c.id && opt.id ? '<span class="id">' + esc(c.id) + '</span>' : '') + '</button>';
  }
  function chips(refs, opt) { return '<span class="chips">' + (refs || []).map(function (r) { return chip(r, opt); }).join('') + '</span>'; }
  function rich(text) {
    var out = '', last = 0, re = /\[\[([^\]]+)\]\]/g, m;
    text = String(text || '');
    while ((m = re.exec(text))) { out += esc(text.slice(last, m.index)) + chip(m[1], { nometa: true }); last = re.lastIndex; }
    return out + esc(text.slice(last));
  }

  function hrefFor(ref) {
    var r = parseRef(ref), e = get(ref);
    if (!e) return '#/inbox';
    switch (r.kind) {
      case 'fpb': return '#/fpb/' + r.id;
      case 'reg': return e.fpb ? '#/fpb/' + e.fpb : '#/issues';
      case 'ruling': return '#/ruling/' + r.id;
      case 'item': return '#/item/' + r.id;
      case 'project': return '#/project/' + r.id;
      case 'question': return e.state === 'open' && e.card ? '#/card/' + e.card : workHref(e.item) + '/questions';
      case 'assumption': return workHref(e.item) + '/assumptions';
      case 'found': return e.promoted ? hrefFor(e.promoted) : '#/item/' + e.item + '/found';
      case 'file': return '#/file/' + r.id + (r.range ? '?l=' + r.range[0] + '-' + r.range[1] : '');
      case 'type': return '#/type/' + r.id;
      case 'run': return '#/run/' + r.id;
      case 'session': return '#/session/' + r.id;
      case 'machine': return '#/machine/' + r.id;
      case 'comment': return '#/comment/' + r.id;
      case 'claudemd': return '#/claudemd/' + r.id;
      case 'commit': return '#/commit/' + r.id;
      case 'branch': return '#/branch/' + r.id;
      case 'card': return '#/card/' + r.id;
      case 'lane': return '#/work/' + e.path[e.path.length - 1].split(':')[1];
      case 'nyw': var m = /\[\[([^\]]+)\]\]/.exec(e.step); return m ? hrefFor(m[1]) : '#/work';
    }
    return '#/inbox';
  }

  /* back-links: who references what. Built once from every collection. */
  var BACK = {};
  (function buildBack() {
    var sources = [['fpb', D.fpbs], ['ruling', D.rulings], ['item', D.items], ['question', D.questions], ['found', D.found], ['type', D.types], ['comment', D.comments], ['card', D.cards], ['session', D.sessions], ['commit', D.commits], ['nyw', D.nyw]];
    var extra = { card: [D.packets, D.plans, D.stucks] };
    sources.forEach(function (pair) {
      var kind = pair[0], coll = pair[1];
      keys(coll).forEach(function (id) {
        var src = kind + ':' + id, blobs = [coll[id]];
        (extra[kind] || []).forEach(function (x) { if (x[id]) blobs.push(x[id]); });
        var s = JSON.stringify(blobs), m; REF_RE.lastIndex = 0;
        var seen = {};
        while ((m = REF_RE.exec(s))) {
          var base = parseRef(m[1] + ':' + m[2]).base;
          if (base === src || seen[base]) continue; seen[base] = 1;
          (BACK[base] = BACK[base] || []).push(src);
        }
      });
    });
    // bare references held as plain ids
    keys(D.questions).forEach(function (q) { add(workRef(D.questions[q].item), 'question:' + q); if (D.questions[q].session) add('session:' + D.questions[q].session, 'question:' + q); });
    keys(D.assumptions).forEach(function (a) { add(workRef(D.assumptions[a].item), 'assumption:' + a); add('session:' + D.assumptions[a].session, 'assumption:' + a); });
    keys(D.runs).forEach(function (r) { add(workRef(D.runs[r].item), 'run:' + r); if (D.runs[r].session) add('session:' + D.runs[r].session, 'run:' + r); add('machine:' + D.runs[r].machine, 'run:' + r); });
    keys(D.comments).forEach(function (c) { add('file:' + D.comments[c].file, 'comment:' + c); });
    function add(t, s) { var l = (BACK[t] = BACK[t] || []); if (l.indexOf(s) < 0) l.push(s); }
  })();
  function backlinks(ref) { return BACK[parseRef(ref).base] || []; }

  /* ================================================================ derived counts */
  function descendants(id) { var it = D.items[id], out = []; (it && it.children || []).forEach(function (c) { out.push(c); out = out.concat(descendants(c)); }); return out; }
  function scope(id) { return [id].concat(descendants(id)); }
  function openQuestions(id) { return keys(D.questions).filter(function (q) { return D.questions[q].item === id && D.questions[q].state === 'open'; }); }
  function itemQuestions(ids) { return keys(D.questions).filter(function (q) { return ids.indexOf(D.questions[q].item) >= 0; }); }
  function itemAssumptions(ids) { return keys(D.assumptions).filter(function (a) { return ids.indexOf(D.assumptions[a].item) >= 0; }); }
  function itemFound(ids) { return keys(D.found).filter(function (f) { return ids.indexOf(D.found[f].item) >= 0; }); }
  function itemRuns(ids) { return by(keys(D.runs).filter(function (r) { return ids.indexOf(D.runs[r].item) >= 0; }), function (r) { return -timeKey(D.runs[r].at); }); }
  function commentCounts(list) { var o = { n: 0, open: 0, handled: 0, stale: 0 }; (list || []).forEach(function (c) { var x = D.comments[c]; if (!x) return; o.n++; o[x.state]++; }); return o; }
  function needsCards(filter) {
    return keys(D.cards).filter(function (id) { var c = D.cards[id]; return c.type !== 'info' && !c.approved && !c.deferred && (filter === 'all' || !filter || c.project === filter); });
  }
  function cardsForScope(ids) { return needsCards('all').filter(function (id) { return ids.indexOf(D.cards[id].item) >= 0; }); }
  function queueText(n) { var sec = n * D.meta.avgDecisionSec; return 'about ' + Math.max(1, Math.round(sec / 60)) + ' min'; }
  function itemChanges(id) {
    var ids = scope(id), files = {}, types = [];
    ids.forEach(function (i) { var ch = work(i) && work(i).changes; if (!ch) return; ch.files.forEach(function (f) { var k = f[0]; if (!files[k]) files[k] = [k, 0, 0]; files[k][1] += f[1]; files[k][2] += f[2]; }); ch.types.forEach(function (t) { if (types.indexOf(t) < 0) types.push(t); }); });
    var list = keys(files).map(function (k) { return files[k]; });
    return { files: list, types: types, plus: list.reduce(function (a, f) { return a + f[1]; }, 0), minus: list.reduce(function (a, f) { return a + f[2]; }, 0) };
  }
  function crumbRefs(itemId) {
    var f = D.fpbs[itemId];
    if (f) { var at = parseRef(f.attach); return (at.kind === 'item' ? crumbRefs(at.id) : ['project:' + f.project]).concat(['fpb:' + itemId]); }
    var out = [], it = D.items[itemId];
    while (it) { out.unshift('item:' + itemId); itemId = it.parent; it = itemId ? D.items[itemId] : null; }
    if (!out.length) return [];
    var first = D.items[parseRef(out[0]).id];
    return ['project:' + first.project].concat(out);
  }
  function crumbs(refs) { return '<nav class="crumbs" aria-label="Breadcrumb">' + refs.map(function (r, i) { return (i ? '<span class="sep">/</span>' : '') + '<a class="crumb" href="' + hrefFor(r) + '" title="' + esc(KIND_LABEL[parseRef(r).kind] + ': ' + chipInfo(r).title) + '">' + esc(trunc(chipInfo(r).title, 32)) + '</a>'; }).join('') + '</nav>'; }

  /* ================================================================ layers: popover, sheet, menu, toast */
  var layer = function () { return document.getElementById('layer'); };
  function closeLayers() { layer().innerHTML = ''; }
  function toast(html, ms) {
    var t = document.createElement('div'); t.className = 'toast'; t.innerHTML = html;
    var box = document.getElementById('toasts'); box.appendChild(t);
    setTimeout(function () { t.remove(); }, ms || 3400);
  }
  function placeNear(el, anchor, w) {
    var shell = document.getElementById('shell').getBoundingClientRect();
    if (S.layout !== 'desktop') return;
    if (!anchor) { el.style.left = Math.round((shell.width - w) / 2) + 'px'; el.style.top = '90px'; return; }
    var r = anchor.getBoundingClientRect();
    var left = Math.min(Math.max(8, r.left - shell.left), shell.width - w - 8);
    el.style.left = left + 'px';
    var h = el.offsetHeight, top = r.bottom - shell.top + 6;
    if (top + h > shell.height - 8) top = Math.max(8, r.top - shell.top - h - 6);
    el.style.top = top + 'px';
  }
  function openPop(ref, anchor) {
    var c = chipInfo(ref), body = popBody(ref);
    var back = backlinks(ref);
    var more = (body.origin ? '<p class="small"><span class="muted">From</span> ' + body.origin + '</p>' : '') +
      (back.length ? '<p class="small muted" style="margin:10px 0 6px">' + esc(backLabel(c.kind, back)) + '</p>' + chips(back.slice(0, 12), { max: 30 }) + (back.length > 12 ? '<div class="tiny">and ' + (back.length - 12) + ' more</div>' : '') : '<p class="small muted">Not referenced elsewhere yet.</p>');
    var html = '<div class="scrim pop-scrim" data-act="close"></div><div class="pop k-' + c.kind + '" role="dialog" aria-label="' + esc(KIND_LABEL[c.kind]) + '">' +
      '<div class="pop-h"><span class="kind"><span class="ic">' + ic(c.kind) + '</span>' + esc(KIND_LABEL[c.kind]) + (c.meta ? ' · ' + esc(c.meta) : '') + '</span>' +
      '<button class="icon-btn x" data-act="close" aria-label="Close">' + ic('close') + '</button></div>' +
      '<div class="pop-b"><p class="pt">' + esc(c.title) + '</p>' + body.html +
      '<details class="mini"><summary>Where it came from' + (back.length ? ' · ' + (c.kind === 'ruling' ? 'cited by ' : 'referenced by ') + back.length : '') + (c.id ? ' · <span class="mono">' + esc(c.id) + '</span>' : '') + '</summary>' + more + '</details>' +
      '</div><div class="pop-f"><a class="btn primary" href="' + hrefFor(ref) + '" data-act="close-after">Open</a><button class="btn" data-act="copy" data-href="' + esc(hrefFor(ref)) + '">Copy link</button></div></div>';
    layer().innerHTML = html;
    placeNear(layer().querySelector('.pop'), anchor, 380);
  }
  function backLabel(kind, back) {
    var items = back.filter(function (b) { return b.indexOf('item:') === 0; }).length;
    if (kind === 'ruling') return 'Cited by ' + plural(back.length, 'record') + (items ? ' · ' + plural(items, 'item') : '');
    return 'Referenced by ' + plural(back.length, 'record');
  }
  function openSheet(title, body, foot) {
    layer().innerHTML = '<div class="scrim" data-act="close"></div><div class="sheet" role="dialog" aria-label="' + esc(title) + '"><div class="pop-h"><b class="pt" style="font-family:var(--display);font-size:16px">' + esc(title) + '</b><button class="icon-btn x" data-act="close" aria-label="Close">' + ic('close') + '</button></div><div class="pop-b">' + body + '</div>' + (foot ? '<div class="pop-f">' + foot + '</div>' : '') + '</div>';
  }
  function openMenu(anchor, html) {
    layer().innerHTML = '<div class="scrim pop-scrim" data-act="close"></div><div class="menu" role="menu">' + html + '</div>';
    var m = layer().querySelector('.menu');
    if (S.layout === 'desktop' && anchor) {
      var shell = document.getElementById('shell').getBoundingClientRect(), r = anchor.getBoundingClientRect();
      m.style.left = Math.min(r.right - shell.left - 260, shell.width - 270) + 'px';
      var top = r.bottom - shell.top + 4; if (top + m.offsetHeight > shell.height - 8) top = r.top - shell.top - m.offsetHeight - 4;
      m.style.top = Math.max(8, top) + 'px';
    }
  }

  /* code pulled from files: never pasted, capped near twelve lines */
  function lineClass(t) { var s = t.trim(); return s.indexOf('//+=') === 0 ? 'pc h' : s.indexOf('//+') === 0 ? 'pc' : ''; }
  function codeLines(f, a, b, focus) {
    var out = '';
    for (var i = a; i <= b && i <= f.lines.length; i++) out += '<span class="ln ' + lineClass(f.lines[i - 1]) + (focus && i === focus ? ' focus' : '') + '"><span class="n">' + i + '</span>' + esc(f.lines[i - 1]) + '</span>';
    return out;
  }
  function codeBlock(ref, opt) {
    opt = opt || {};
    var r = parseRef(ref), f = D.files[r.id];
    if (!f) return '';
    var a = r.range ? r.range[0] : 1, b = r.range ? r.range[1] : f.lines.length;
    var cap = opt.cap || 12, shown = Math.min(b, a + cap - 1), more = b - shown;
    return '<div class="code' + (opt.small ? ' small' : '') + '"><div class="code-h">' + chip(ref, { nometa: true }) +
      (!f.seen ? '<span class="marker new">new to you · ' + f.newLines + ' lines</span>' : '') +
      '<span class="acts"><button class="btn sm" data-toast="Opening ' + esc(f.path.split('/').pop()) + ':' + a + ' in Rider on ' + esc(homeMachineForFile(f)) + '">' + ic('rider') + 'Open in Rider</button>' +
      '<a class="btn sm" href="#/diff/' + r.id + '">' + ic('diff') + 'Diff since master</a></span></div>' +
      '<pre><code>' + codeLines(f, a, shown, opt.focus) + '</code></pre>' +
      (more > 0 ? '<div class="more">' + more + ' more lines · <a href="' + hrefFor(ref) + '">show all</a></div>' : '') + '</div>';
  }
  function homeMachineForFile(f) { var b = f.branch && D.branches[f.branch]; return b ? b.machine : 'desk-pc'; }
  function diffBlock(lines, opt) {
    opt = opt || {};
    return '<div class="code' + (opt.small ? ' small' : '') + '">' + (opt.head ? '<div class="code-h">' + opt.head + '</div>' : '') + '<pre><code>' + lines.map(function (l) {
      var c = l.indexOf('@@') === 0 ? 'hunk' : l[0] === '+' ? 'add' : l[0] === '-' ? 'del' : '';
      return '<span class="ln ' + c + '"><span class="n">' + (c === 'add' ? '+' : c === 'del' ? '−' : '') + '</span>' + esc(c === 'hunk' ? l : l.slice(1)) + '</span>';
    }).join('') + '</code></pre></div>';
  }
  function fileDiff(id) { var f = D.files[id]; if (!f) return []; if (f.diff) return f.diff; return ['@@ -0,0 +1,' + f.lines.length + ' @@ new file ' + f.path].concat(f.lines.map(function (l) { return '+' + l; })); }

  /* progressive disclosure: one line closed, the detail when opened */
  function drill(label, summary, body, opt) {
    opt = opt || {};
    return '<details class="drill' + (opt.attn ? ' attn' : '') + '"' + (opt.open ? ' open' : '') + (opt.id ? ' id="' + opt.id + '"' : '') + '><summary><span class="dl">' + label + '</span><span class="ds">' + summary + '</span><span class="chev" aria-hidden="true"></span></summary><div class="db">' + body + '</div></details>';
  }
  function drills(rows, title) { return (title ? '<div class="deeper-h">' + title + '</div>' : '') + '<div class="drills">' + rows.filter(Boolean).join('') + '</div>'; }
  function dhead(kind, cls, right, title, sub) {
    return '<div class="dh"><div class="dk"><span class="kind-label kl-' + cls + '">' + kind + '</span>' + (right ? '<span class="dk-r">' + right + '</span>' : '') + '</div><h1 class="dt">' + title + '</h1>' + (sub ? '<p class="dsub">' + sub + '</p>' : '') + '</div>';
  }
  function itemLink(id) { return '<a href="' + workHref(id) + '">' + esc(itemTitle(id)) + '</a>'; }

  /* ================================================================ popover bodies, one per chip kind */
  function outLines(run, n) { return '<div class="code small"><pre><code>' + run.out.slice(0, n).map(function (l, i) { return '<span class="ln"><span class="n">' + (i + 1) + '</span>' + esc(l) + '</span>'; }).join('') + '</code></pre></div>'; }
  function popBody(ref) {
    var r = parseRef(ref), e = get(ref), k = r.kind;
    if (!e) return { html: '<p>This reference points at nothing in the store.</p>' };
    switch (k) {
      case 'fpb':
        var need = fpbNeeds(r.id);
        return { html: '<p class="small muted">' + esc(e.kind + ' · ' + e.subtype + ' · ' + e.state) + (e.severity ? ' · ' + esc(e.severity.level) : '') + '</p>' + (fpbLead(r.id) ? '<p>' + esc(trunc(fpbLead(r.id), 160)) + '</p>' : '') +
          (need.length ? '<p class="small tone-warn">' + plural(need.length, 'thing') + ' waiting on you</p>' : ''),
          origin: 'Attached to ' + chip(e.attach) + ' · ' + sourceText(e) };
      case 'reg':
        return { html: '<p class="small muted">' + esc(e.where) + ' · ' + esc(e.date) + '</p>' + (e.note ? '<p>' + esc(e.note) + '</p>' : '') + (e.fpb ? '<p>Now ' + chip('fpb:' + e.fpb) + '</p>' : ''), origin: 'Imported from the Quanta Workbench with the seed, 4 Nov' };
      case 'ruling':
        return { html: '<p>' + esc(e.text) + '</p><div class="meta-line">' + (e.neverReopened ? '<span class="tag good">never reopened</span>' : '<span class="tag warn">may reopen · ' + esc(e.reopenNote || '') + '</span>') + '<span>' + esc(projOf(e.project).name) + '</span></div>',
          origin: esc(e.origin) + ', ' + esc(e.date) + (e.question ? ' · ' + chip(e.question, { nometa: true }) : '') + (e.item ? ' · ' + chip(e.item, { nometa: true }) : '') };
      case 'item':
        var q = openQuestions(r.id).length, steps = e.steps || [];
        return { html: '<div class="meta-line">' + kindPill(e.kind) + statePill(e.unscheduled ? 'coarse' : e.state) + (e.coarse ? '<span class="tag">coarse · ' + esc(e.coarse) + '</span>' : '') + (e.lane ? chip('lane:' + e.lane.charAt(0), { nometa: true }) : '') + '</div>' +
          (e.goal ? '<p style="margin-top:8px">' + esc(e.goal) + '</p>' : e.info && e.info.text ? '<p style="margin-top:8px">' + rich(e.info.text) + '</p>' : '') +
          (steps.length ? '<div class="kvs"><span><b>' + steps.filter(function (s) { return s.state === 'done'; }).length + '/' + steps.length + '</b> steps</span>' + (e.time ? '<span><b>' + fmtMin(e.time.so) + '</b> so far</span><span>forecast <b>' + fmtMin(e.time.forecast) + '</b></span>' : '') + '<span>questions <b>' + q + '/2</b></span></div>' : ''),
          origin: e.project === 'dope' && e.unscheduled ? 'Imported by you with the Dope seed, 4 Nov' : 'Created by you' + (e.parent ? ' from the plan of ' + chip('item:' + e.parent, { nometa: true }) : ' under ' + chip('project:' + e.project, { nometa: true })) };
      case 'project':
        return { html: '<p>' + esc(e.purpose) + '</p><div class="kvs"><span>state <b>' + esc(e.state) + '</b></span><span>work left <b>about ' + e.coarseWeeks + ' weeks</b>' + (e.measuredWeeks ? ', ' + e.measuredWeeks + ' measured' : '') + '</span></div>', origin: 'Created by you in the programme plan' };
      case 'question':
        var h = '<p>' + esc(e.text) + '</p>';
        if (e.state === 'answered') h += '<div class="panel"><h4>Your answer · ' + esc(e.answered) + '</h4><p>' + esc(e.answer) + '</p>' + (e.ruling ? '<div class="meta-line">became ' + chip(e.ruling) + '</div>' : '<div class="tiny">No ruling: it settled this item only.</div>') + '</div>';
        else h += '<div class="meta-line"><span class="tag accent">open</span><span>blocks ' + esc(e.blocks) + '</span><span>unblocks ~' + fmtMin(e.unblockMin) + '</span></div><ol class="small" style="margin:8px 0 0;padding-left:18px">' + e.options.map(function (o) { return '<li>' + esc(o.t) + (o.rec ? ' <span class="recmark">recommended</span>' : '') + '</li>'; }).join('') + '</ol>';
        return { html: h, origin: 'Asked ' + (e.session ? 'by ' + chip('session:' + e.session) + ' ' : '') + 'on ' + chip(workRef(e.item)) + ', ' + esc(e.asked) };
      case 'assumption':
        return { html: '<div class="meta-line"><span class="tag ' + (e.state === 'fine' ? 'good' : 'accent') + '">' + esc(e.state) + '</span><span>batched, never pushed to the inbox</span></div>', origin: 'Made by ' + chip('session:' + e.session) + ' on ' + chip(workRef(e.item)) };
      case 'found':
        return { html: '<div class="meta-line">' + kindPill(e.kind) + '<span class="tag">in tray</span></div><p style="margin-top:8px">' + rich(e.why) + '</p><div class="meta-line">evidence ' + chip(e.evidence, { nometa: true }) + '</div>',
          origin: 'Found by ' + chip('session:' + e.session, { nometa: true }) + ' on ' + chip('item:' + e.item, { nometa: true }) + ', ' + esc(e.found) + ' · it never enters the tree until you make it a task' };
      case 'file':
        return { html: '<div class="addr">' + esc(e.repo + ' · ' + e.path) + '</div>' + (e.seen ? '<div class="meta-line" style="margin-top:6px"><span class="marker seen">seen ' + esc(e.lastSeen) + '</span></div>' : '<div class="meta-line" style="margin-top:6px"><span class="marker new">new to you · ' + e.newLines + ' lines</span></div>') + codeBlock(ref, { small: true }),
          origin: (e.branch ? 'On ' + chip('branch:' + e.branch, { nometa: true }) : 'Dope repository, from the seed') };
      case 'type':
        return { html: '<div class="meta-line"><span class="tag">' + esc(e.kind) + '</span><span class="mono tiny">' + esc(e.ns) + '</span>' + (e.seen ? '<span class="marker seen">seen</span>' : '<span class="marker new">new to you</span>') + (e.beyondPlan ? '<span class="marker beyond">beyond plan</span>' : '') + '</div><p style="margin-top:8px">' + rich(e.summary) + '</p>' + codeBlock(e.at, { small: true, cap: 8 }),
          origin: e.newIn ? 'New in ' + chip(e.newIn, { nometa: true }) : 'Existing type' };
      case 'run':
        return { html: '<div class="meta-line">' + resultTag(e.result) + '<span>' + fmtSec(e.dur) + '</span>' + chip('machine:' + e.machine, { nometa: true }) + (e.session ? chip('session:' + e.session, { nometa: true }) : '<span class="tag">seed</span>') + '</div><div class="addr" style="margin:8px 0 4px">$ ' + esc(e.cmd) + '</div>' + outLines(e, 6),
          origin: (e.seed ? 'Imported from the Quanta Workbench seed' : 'Reported by the PostToolUse hook') + ', ' + esc(e.at) + ' · ' + chip(workRef(e.item)) };
      case 'session':
        return { html: '<div class="meta-line"><span class="dot ' + e.state + '"></span><b>' + esc(e.stuck ? 'stuck · waiting on you' : e.state === 'waiting' ? 'waiting on you' : e.state) + '</b>' + chip('machine:' + e.machine, { nometa: true }) + '</div><div class="kvs" style="margin-top:8px"><span><b>' + e.minutes + '</b> min</span><span><b>' + e.turns + '</b> turns</span><span><b>' + e.tools + '</b> tool calls</span></div><p style="margin-top:6px">' + esc(e.step) + '</p>' + (e.waitingOn.length ? '<div class="meta-line">waiting on ' + chips(e.waitingOn, { nometa: true, max: 30 }) + '</div>' : ''),
          origin: 'Started ' + esc(e.started) + ' on ' + chip(workRef(e.item)) };
      case 'machine':
        return { html: '<div class="meta-line"><span class="dot ' + (e.online ? 'online' : 'offline') + '"></span><b>' + (e.online ? 'online' : 'offline') + '</b><span>Host ' + esc(e.host) + '</span><span class="' + (e.clockWarn ? 'tone-warn' : '') + '">clock ' + esc(e.clock) + '</span></div><p class="small" style="margin-top:6px">' + esc(e.os) + '</p><div class="meta-line">homes ' + chips(e.homed.map(function (i) { return 'item:' + i; }), { nometa: true, max: 28 }) + '</div>',
          origin: 'Registered by you, token issued ' + esc(e.token) };
      case 'comment':
        var f = D.files[e.file];
        return { html: '<div class="plus-comment"><div class="meta-line"><span class="tag ' + (e.state === 'handled' ? 'good' : e.state === 'stale' ? 'warn' : 'accent') + '">' + esc(e.state) + '</span><span>above <b class="mono">' + esc(e.symbol) + '</b></span></div><div class="txt">//+ ' + esc(e.text) + '</div>' + (e.handled ? '<div class="txt" style="color:var(--good);font-size:12px">//+= handled ' + esc(e.handled.date) + ' · session ' + esc(e.handled.session) + ' · ' + esc(e.handled.note) + '</div>' : '') + '</div>' +
          (e.state === 'stale' ? '<p class="small">' + rich(e.staleWhy) + '</p>' : '') + (e.handled ? '<div class="meta-line">proof ' + chip(e.handled.proof) + '</div>' : '') + codeBlock('file:' + e.file + ':' + Math.max(1, e.line - 1) + '-' + Math.min(f.lines.length, e.line + 4), { small: true, focus: e.line }),
          origin: 'Written by you in ' + chip('file:' + e.file, { nometa: true }) + ', ' + esc(e.versions[e.versions.length - 1].date) + ' · ' + plural(e.versions.length, 'version') };
      case 'claudemd':
        return { html: codeBlock(e.file, { small: true }), origin: 'CLAUDE.md at the repo root, written by you' };
      case 'commit':
        return { html: '<div class="meta-line"><span class="mono">' + esc(r.id) + '</span><span>' + e.files + ' files · +' + e.plus + ' −' + e.minus + '</span></div>' + diffBlock(e.diff.slice(0, 6), { small: true }),
          origin: (e.session ? 'Committed by ' + chip('session:' + e.session) + ' on ' + chip('branch:' + e.branch) : 'A tagged baseline') + ', ' + esc(e.date) };
      case 'branch':
        return { html: '<div class="meta-line"><span class="tag">' + esc(e.state) + '</span><span>ahead ' + e.ahead + ' · behind ' + e.behind + '</span>' + chip('machine:' + e.machine, { nometa: true }) + '</div><div class="addr" style="margin-top:8px">' + esc(e.worktree) + '</div>',
          origin: 'Worktree for ' + chip('item:' + e.item, { nometa: true }) };
      case 'card':
        return { html: '<div class="meta-line"><span class="kind-label kl-' + (e.type === 'packet' ? 'approve' : e.type) + '">' + esc(cardKindLabel(e)) + '</span>' + (e.item ? chip(workRef(e.item)) : '') + '</div><p style="margin-top:8px">' + rich(e.why || e.body) + '</p>',
          origin: e.type === 'info' ? rich(e.origin) : (e.session ? 'Raised by ' + chip('session:' + e.session, { nometa: true }) + ', ' + esc(e.at) : 'Built from the seed, ' + esc(e.at)) };
      case 'lane':
        return { html: '<div class="kvs"><span>factor <b>×' + e.factor + '</b>' + (e.was && e.was !== e.factor ? ' (was ×' + e.was + ')' : '') + '</span><span><b>' + esc(e.state) + '</b></span></div><div class="meta-line" style="margin-top:6px">path ' + chips(e.path, { nometa: true, max: 28 }) + '</div>', origin: e.proposed ? 'Proposed by ' + chip('card:pl-24', { nometa: true }) : 'Added by you' };
      case 'nyw':
        return { html: '<p><span class="marker pv">not yet written</span></p><p class="small">Planned at <span class="mono">' + esc(e.where) + '</span>, in ' + rich(e.step) + '. No line exists, so nothing is pulled and nothing is claimed.</p>', origin: 'Named by an approved or proposed plan' };
    }
    return { html: '' };
  }
  function cardKindLabel(c) { return c.type === 'question' ? 'Question' : c.type === 'packet' ? (c.approved ? 'Approved' : c.preview ? 'Approve · preview' : 'Approve') : c.type === 'plan' ? 'Plan' : c.type === 'stuck' ? 'Stuck' : 'Info'; }

  /* ================================================================ shared fragments */
  function budgetQ(itemId) {
    var n = openQuestions(itemId).length, max = D.budgets[0].value;
    return '<span class="budget' + (n >= max ? ' full' : '') + '">questions ' + n + ' of ' + max + '<span class="pips">' + [0, 1].map(function (i) { return '<i class="' + (i < n ? 'on' : '') + '"></i>'; }).join('') + '</span>' + (n >= max ? '<span class="muted"> · a third is refused until one is answered</span>' : '') + '</span>';
  }
  function claimsList(list) {
    return '<ul class="plain">' + list.map(function (c) { return '<li><span>' + esc(c.t) + '</span>' + (c.proof ? chip(c.proof) : '<span class="marker unv">unverified</span>') + '</li>'; }).join('') + '</ul>';
  }
  function head(refs, extra) { return '<div class="pane-head"><button class="back" data-act="back" aria-label="Back">' + ic('back') + '<span>Back</span></button>' + (refs ? crumbs(refs) : '<span class="crumbs"></span>') + (extra || '') + '</div>'; }
  function empty(title, sentence) { return '<div class="empty"><b>' + esc(title) + '</b><span>' + esc(sentence) + '</span></div>'; }
  function talkBtn(ctx) { return '<button class="btn text" data-act="talk" data-ctx="' + esc(ctx) + '">Talk</button>'; }
  function replyBtn(id, label) { return '<button class="btn text" data-act="reply" data-card="' + id + '">' + (label || 'Write your own') + '</button>'; }
  function replyBox(id, open) {
    var c = D.cards[id], s = c && c.session ? D.sessions[c.session] : null;
    var to = s ? (s.state === 'ended' ? 'the Host on ' + s.machine + ' (session ' + c.session + ' resumes to read it)' : 'session ' + c.session + ' on ' + s.machine) : (c && c.type === 'info' ? 'your notes on this card' : 'the item, read when a session next opens it');
    return '<div class="reply" id="reply-' + id + '"' + (open ? '' : ' hidden') + '><input type="text" placeholder="Say it in your own words" aria-label="Your message" data-act="reply-input" data-card="' + id + '"><button class="btn primary" data-act="send-reply" data-card="' + id + '" data-to="' + esc(to) + '">Send</button><p class="small muted">Goes to ' + esc(to) + ' instead of the buttons above.</p></div>';
  }
  function kindClass(c) { return c.type === 'packet' ? 'approve' : c.type; }
  function ago(c) { return c.waiting ? c.waiting : c.age || ''; }
  function plainList(items) { return '<ul class="plain">' + items.map(function (x) { return '<li>' + x + '</li>'; }).join('') + '</ul>'; }

  /* ================================================================ 1. Inbox */
  function vInboxList(sel) {
    var f = S.project;
    var needs = needsCards(f);
    needs = S.sort === 'weight' ? by(needs, function (id) { return -D.cards[id].weight; }) : by(needs, function (id) { return (S.sort === 'newest' ? -1 : 1) * timeKey(D.cards[id].at); });
    var info = keys(D.cards).filter(function (id) { var c = D.cards[id]; return c.type === 'info' && (f === 'all' || c.project === f) && !S.dismissed[id]; });
    info = by(info, function (id) { return -timeKey(D.cards[id].at); });
    var h = '<div class="lpane"><div class="lhead"><h2>Inbox</h2><p class="muted">' + (needs.length ? plural(needs.length, 'decision') + ' · ' + queueText(needs.length) : 'Nothing waits on you') + (f !== 'all' ? ' · ' + esc(projOf(f).name) + ' only' : '') + '</p></div>';
    h += '<div class="lctrl"><label class="quiet-select">Sort by <select data-act="sort" aria-label="Sort"><option value="weight"' + (S.sort === 'weight' ? ' selected' : '') + '>work it unblocks</option><option value="newest"' + (S.sort === 'newest' ? ' selected' : '') + '>newest</option><option value="oldest"' + (S.sort === 'oldest' ? ' selected' : '') + '>oldest</option></select></label>' +
      '<label class="toggle"><input type="checkbox" data-act="deferred"' + (S.showDeferred ? ' checked' : '') + '>Show deferred</label></div>';
    h += needs.length ? '<div class="qlist">' + needs.map(function (id) { return qcard(id, sel === id); }).join('') + '</div>' : empty('Nothing needs you', 'Question, Approve, Plan and Stuck cards land here when a session cannot go on without you.');
    if (S.showDeferred) { var def = keys(D.cards).filter(function (id) { var c = D.cards[id]; return c.deferred && (f === 'all' || c.project === f); }); h += '<div class="lsec"><div class="lsec-h">Deferred · not counted in your queue</div>' + (def.length ? '<div class="qlist">' + def.map(function (id) { return qcard(id, sel === id); }).join('') + '</div>' : empty('No deferred cards', 'A card you defer waits here and comes back after 24 hours.')) + '</div>'; }
    var shown = S.cleared ? [] : info;
    h += '<details class="fyk"' + (S.fykOpen ? ' open' : '') + ' data-act="fyk"><summary><span>For you to know</span><span class="muted">' + (shown.length ? shown.length + ' updates, nothing to decide' : 'none') + '</span><span class="chev"></span></summary>';
    h += shown.length ? '<div class="fyk-b">' + shown.map(function (id) { var c = D.cards[id]; return '<a class="fyk-row' + (sel === id ? ' sel' : '') + '" href="#/card/' + id + '"><span>' + esc(c.headline) + '</span><span class="muted">' + esc(c.age) + '</span></a>'; }).join('') + '<button class="btn text" data-act="clear-info">Clear all</button></div>'
      : '<div class="fyk-b">' + empty('Nothing new to know', 'Info cards appear here: a lane factor changed, a machine registered, a branch pushed, a session ended.') + '</div>';
    return h + '</details></div>';
  }
  function qcard(id, sel) {
    var c = D.cards[id];
    return '<a class="qcard kc-' + kindClass(c) + (sel ? ' sel' : '') + '" href="#/card/' + id + '">' +
      '<span class="qtop"><span class="kind-label kl-' + kindClass(c) + '">' + cardKindLabel(c) + '</span><span class="qw">' + (c.weight ? '~' + fmtMin(c.weight) : 'preview') + '</span></span>' +
      '<span class="qh">' + esc(c.headline) + '</span>' +
      '<span class="qm">' + esc(itemTitle(c.item)) + ' · ' + esc(ago(c)) + '</span></a>';
  }

  /* ================================================================ card detail router */
  function vCard(id, q) {
    var c = D.cards[id];
    if (!c) return head(null) + '<div class="pane">' + empty('No such card', 'The card may have been cleared.') + '</div>';
    if (c.type === 'question') return vQuestion(id);
    if (c.type === 'packet') return vPacket(id, q);
    if (c.type === 'plan') return vPlan(id, q);
    if (c.type === 'stuck') return vStuck(id);
    return vInfo(id);
  }
  function cardHead(c) { return head(crumbRefs(c.item)); }

  /* ================================================================ 2. Question Card */
  function vQuestion(id) {
    var c = D.cards[id], q = D.questions[c.question], s = D.sessions[q.session] || { machine: 'no machine yet' };
    var h = cardHead(c) + '<div class="pane">';
    h += dhead('Question', 'question', 'unblocks ~' + fmtMin(q.unblockMin), esc(q.text), 'Blocks ' + esc(q.blocks) + ' of ' + itemLink(q.item));
    if (q.unseenFile) {
      var uid = parseRef(q.unseenFile).id, uf = D.files[uid];
      h += '<div class="notice"><p><b>' + esc(uf.path.split('/').pop()) + ' is new to you.</b> ' + uf.newLines + ' lines changed since you last opened it at ' + esc(uf.lastSeen) + '.</p><details class="inline"><summary>Read the diff before answering</summary>' + diffBlock(fileDiff(uid), { small: true }) + '</details></div>';
    }
    if (q.comment) {
      var cm = D.comments[q.comment];
      h += '<blockquote class="quote"><span class="muted small">Your + comment</span><code>//+ ' + esc(cm.text) + '</code></blockquote>';
    }
    h += '<div class="choices" role="radiogroup" aria-label="Options">' + q.options.map(function (o) {
      return '<label class="choice' + (o.rec ? ' rec' : '') + '"><input type="radio" name="opt-' + id + '"' + (o.rec ? ' checked' : '') + '><span class="cb"><span class="ct">' + esc(o.t) + (o.rec ? '<span class="recmark">Recommended</span>' : '') + '</span><span class="cr">' + esc(o.why) + '</span></span></label>';
    }).join('') + '</div>';
    h += '<div class="actions"><button class="btn primary" data-act="pick" data-card="' + id + '">Pick</button><button class="btn" data-toast="Assumed “' + esc(q.options[0].t) + '” · sent to session ' + q.session + ' on ' + s.machine + '">Use recommended</button><span class="spacer"></span>' + replyBtn(id) + talkBtn('card:' + id) + '<button class="btn text" data-toast="Deferred for 24 hours · it comes back on 7 Nov">Defer</button></div>' + replyBox(id);
    var support = [];
    q.options.forEach(function (o) { o.support.forEach(function (r) { if (support.indexOf(r) < 0) support.push(r); }); });
    h += drills([
      drill('Context', esc(trunc(q.context.replace(/\[\[[^\]]+\]\]/g, function (m) { return chipInfo(m.slice(2, -2)).title; }), 70)), '<p class="rich">' + rich(q.context) + '</p>'),
      q.code ? drill('The code', esc(D.files[parseRef(q.code).id].path.split('/').pop()) + ', lines ' + parseRef(q.code).range.join('–'), codeBlock(q.code)) : '',
      support.length ? drill('What supports each option', plural(support.length, 'reference'), q.options.map(function (o, i) { return '<div class="sub-block"><p class="small"><b>' + (i + 1) + '. ' + esc(o.t) + '</b></p>' + chips(o.support) + '</div>'; }).join('')) : '',
      drill('Cost to change later', esc(q.cost), '<p>If you pick differently later, the change is <b>' + esc(q.cost) + '</b>.</p>'),
      q.tried.length ? drill('What the session tried', plural(q.tried.length, 'attempt'), plainList(q.tried.map(esc))) : '',
      drill('Asked by', 'session ' + q.session + ' on ' + s.machine + ' · ' + esc(q.asked), '<div class="chips">' + chip('session:' + q.session, { meta: true }) + (D.machines[s.machine] ? chip('machine:' + s.machine) : '') + '</div><p class="small" style="margin-top:12px">' + budgetQ(q.item) + '</p>')
    ], 'Go deeper');
    return h + '</div>';
  }

  /* ================================================================ 3. Packet */
  function vPacket(id, qs) {
    var c = D.cards[id], pk = D.packets[id], it = work(pk.item), ch = it.changes || { files: [], types: [] };
    var plus = ch.files.reduce(function (a, f) { return a + f[1]; }, 0), minus = ch.files.reduce(function (a, f) { return a + f[2]; }, 0);
    var unseen = ch.files.filter(function (f) { return !D.files[parseRef(f[0]).id].seen; });
    var unrev = pk.assumptions.filter(function (a) { return D.assumptions[a].state === 'unreviewed'; });
    var unver = pk.claims.filter(function (x) { return !x.proof; });
    var label = pk.state === 'approved' ? 'Approved' : pk.preview ? 'Approve · preview' : 'Approve';
    var timeLine = pk.time.actual != null ? fmtMin(pk.time.actual) + ' against an estimate of ' + fmtMin(pk.time.est) : 'not measured yet';
    var h = cardHead(c) + '<div class="pane">';
    h += dhead(label, 'approve', c.weight ? 'unblocks ~' + fmtMin(c.weight) : '', esc(pk.outcome), itemLink(pk.item) + ' · ' + timeLine);
    if (pk.preview) h += '<div class="notice"><p>A preview built from the Dope seed. No session or branch exists while Dope is closed.</p></div>';
    var glance = [];
    if (unseen.length) glance.push('<a href="#" data-act="open-drill" data-target="d-changes">' + plural(unseen.length, 'file') + ' new to you</a>');
    if (unrev.length) glance.push('<a href="#" data-act="open-drill" data-target="d-assume">' + unrev.length + ' assumptions to review</a>');
    if (pk.beyond.length) glance.push('<a href="#" data-act="open-drill" data-target="d-plan">' + pk.beyond.length + ' helper built beyond the plan</a>');
    if (unver.length) glance.push('<a href="#" data-act="open-drill" data-target="d-claims">' + unver.length + ' claim without a run</a>');
    h += '<div class="verdict"><p class="ok">' + ic('run') + 'All ' + pk.checks.length + ' checks passed</p>' + (glance.length ? '<p class="glance"><span class="muted">Worth a glance:</span> ' + glance.join(' · ') + '</p>' : '') + '</div>';
    if (pk.state === 'approved') h += '<div class="actions"><span class="done-line">Approved ' + esc(pk.approved) + ' and merged</span>' + chip(pk.merged) + '</div>';
    else {
      h += '<div class="actions"><button class="btn primary" data-act="approve" data-card="' + id + '">Approve &amp; merge</button><button class="btn" data-act="reply" data-card="' + id + '">Ask for changes</button><span class="spacer"></span><a class="btn text" href="#/diff/' + id + '">Full diff</a></div>' + replyBox(id, qs && qs.changes);
    }
    var filesBody = ch.files.length ? '<div class="list">' + ch.files.map(function (f) { var fe = D.files[parseRef(f[0]).id]; return '<div class="li"><span class="li-main">' + chip(f[0]) + '</span><span class="li-meta"><span class="tone-good">+' + f[1] + '</span> <span class="tone-bad">−' + f[2] + '</span>' + (fe.seen ? '' : ' <span class="marker new">new to you</span>') + '</span><a class="li-act" href="#/diff/' + parseRef(f[0]).id + '">diff</a></div>'; }).join('') + '</div>' + (ch.types.length ? '<p class="small muted" style="margin:16px 0 8px">New types</p>' + chips(ch.types) : '') : empty('No files', 'A packet lists every file git reports as changed on the branch.');
    h += drills([
      drill('Checks', pk.checks.length + ' of ' + pk.checks.length + ' passed', '<div class="chips">' + pk.checks.map(function (k) { return chip(k.ref, { meta: true }); }).join('') + '</div>'),
      drill('Changes', plural(ch.files.length, 'file') + ' · +' + plus + ' −' + minus + (unseen.length ? ' · ' + unseen.length + ' new to you' : ''), filesBody, { id: 'd-changes', attn: unseen.length > 0 }),
      drill('Assumptions', pk.assumptions.length ? pk.assumptions.length + (unrev.length ? ' · ' + unrev.length + ' to review' : ' · all fine') : 'none', pk.assumptions.length ? assumptionBatch(pk.assumptions) : empty('No assumptions', 'Assumptions a session makes are batched here.'), { id: 'd-assume', attn: unrev.length > 0 }),
      drill('Found items', pk.found.length ? pk.found.length + ' in the tray' : 'none', pk.found.length ? foundTray(pk.found) : empty('Tray is empty', 'Found items wait here for Task, Later or Drop.')),
      drill('+ comments', pk.commentsHandled.length + ' handled · ' + pk.commentsOpen.length + ' still open', commentsBody(pk)),
      drill('Plan', it.steps.length ? it.steps.length + ' of ' + it.steps.length + ' steps done' + (pk.beyond.length ? ' · ' + pk.beyond.length + ' extra helper' : '') : 'no plan yet', planBody(pk, it, id), { id: 'd-plan', attn: pk.beyond.length > 0 }),
      drill('Claims', (pk.claims.length - unver.length) + ' proved by runs' + (unver.length ? ' · ' + unver.length + ' unverified' : ''), claimsList(pk.claims), { id: 'd-claims', attn: unver.length > 0 }),
      drill('Claude’s note', esc(trunc(pk.note.text, 60)), '<p>' + esc(pk.note.text) + '</p><p class="small muted">' + (pk.session ? 'Session ' + pk.session : 'Seed') + ' · ' + esc(pk.note.date) + '</p>'),
      drill('What Approve sends', esc(pk.deliver), '<pre class="instr">' + esc(pk.instruction.join('\n')) + '</pre>')
    ], 'Go deeper');
    return h + '</div>';
  }
  function commentsBody(pk) {
    var h = pk.commentsHandled.map(function (cid) { var cm = D.comments[cid]; return '<div class="sub-block"><code class="cmt">//+ ' + esc(cm.text) + '</code><code class="cmt done">//+= ' + esc(cm.handled.note) + '</code><div class="chips" style="margin-top:8px"><span class="small muted">proof</span>' + chip(cm.handled.proof) + '</div></div>'; }).join('');
    if (pk.commentsOpen.length) h += '<p class="small muted" style="margin:16px 0 8px">Still open in the changed files</p>' + chips(pk.commentsOpen.map(function (x) { return 'comment:' + x; }));
    return h || empty('No comments', 'Comments the session handled under this item show here with their proof.');
  }
  function planBody(pk, it, pkId) {
    if (!it.steps.length) return empty('No approved plan', 'Seeded items carry measurements but no plan; one is drawn when Dope opens.');
    var h = '<ol class="steps-simple">' + it.steps.map(function (st) { return '<li class="done"><span class="tick">✓</span><span class="st-t">' + esc(st.t) + '</span>' + chip(st.proof) + '</li>'; }).join('') + '</ol>';
    pk.beyond.forEach(function (b, i) { h += '<div class="notice soft"><p><b>Beyond the plan:</b> ' + esc(b.t) + '</p><p class="small">' + esc(b.why) + '</p><div class="chips">' + chip(b.type) + '<button class="btn text" data-act="beyond" data-pk="' + pkId + '" data-i="' + i + '">See the code</button></div></div>'; });
    return h;
  }
  function assumptionBatch(ids) {
    var groups = {};
    ids.forEach(function (a) { var s = D.assumptions[a].session; (groups[s] = groups[s] || []).push(a); });
    return keys(groups).map(function (s) {
      var un = groups[s].filter(function (a) { return D.assumptions[a].state === 'unreviewed'; }).length;
      return '<div class="group"><div class="group-h"><span>Session ' + s + ' · ' + plural(groups[s].length, 'assumption') + (un ? ', ' + un + ' to review' : '') + '</span><button class="btn small" data-toast="' + groups[s].length + ' assumptions from session ' + s + ' marked fine">All fine</button></div>' +
        groups[s].map(function (a) { var x = D.assumptions[a]; return '<div class="li"><span class="li-main">' + esc(x.text) + '</span><span class="li-meta">' + (x.state === 'fine' ? '<span class="tone-good">fine</span>' : 'to review') + '</span><button class="li-act" data-act="flip">Flip</button></div>'; }).join('') + '</div>';
    }).join('');
  }
  function foundTray(ids) {
    var places = ['WB-2', 'WB-2.4', 'WB-3', 'WB-4', 'WB-5', 'WB-6'];
    return ids.map(function (f) {
      var x = D.found[f];
      return '<div class="found"><p><span class="kt">' + x.kind + '</span> <b>' + esc(x.title) + '</b></p><p class="small rich">' + rich(x.why) + '</p>' +
        '<div class="found-acts"><select aria-label="Place">' + places.map(function (p) { return '<option' + (p === x.suggest ? ' selected' : '') + '>under ' + esc(trunc(itemTitle(p), 30)) + '</option>'; }).join('') + '</select>' +
        '<button class="btn small" data-toast="Task created by you from found item ' + f + '">Task</button><button class="btn small" data-toast="' + f + ' kept for later">Later</button><button class="btn small text" data-toast="' + f + ' dropped">Drop</button></div></div>';
    }).join('');
  }

  /* ================================================================ 4. Plan approval */
  function vPlan(id, qs) {
    var c = D.cards[id], pl = D.plans[id], it = D.items[pl.item];
    var total = pl.steps.reduce(function (a, s) { return a + s.est; }, 0);
    var h = cardHead(c) + '<div class="pane' + (qs && qs.edit ? ' editing' : '') + '" id="plan-pane">';
    h += dhead('Plan', 'plan', 'unblocks ~' + fmtMin(c.weight), esc(it.title), esc(pl.goal));
    h += '<ol class="plan-list">' + pl.steps.map(function (s, i) { return '<li><span class="handle" title="Drag to reorder">' + ic('grip') + '</span><span class="num">' + (i + 1) + '</span><span class="pt">' + esc(s.t) + '</span><span class="pe">' + s.est + ' min</span><button class="icon-btn rm" title="Remove step" data-toast="Step ' + (i + 1) + ' would be removed from the plan">' + ic('trash') + '</button></li>'; }).join('') + '</ol>';
    h += '<p class="muted small">' + pl.steps.length + ' of ' + D.budgets[2].value + ' steps · ' + total + ' Claude-min · about ' + fmtMin(total * pl.factor) + ' at ×' + pl.factor + '</p>';
    h += '<div class="actions"><button class="btn primary" data-toast="Plan approved · WB-2.4 opens in lane C on cloud-1; branch wb/2.4-check-chips is created">Approve plan</button><button class="btn" data-act="plan-edit">' + (qs && qs.edit ? 'Done changing' : 'Change') + '</button><span class="spacer"></span>' + replyBtn(id) + talkBtn('card:' + id) + '</div>' + replyBox(id);
    h += drills([
      drill('Settle pass', pl.settle.length + ' questions · defaults chosen', '<div class="actions" style="margin:0 0 16px"><button class="btn small" data-toast="All three recommended defaults accepted">Accept all recommended</button></div><div class="settle">' + pl.settle.map(function (s, i) {
        return '<div class="sq"><p><b>' + esc(s.q) + '</b></p><div class="so">' + s.options.map(function (o, j) { return '<button type="button" class="' + (j === s.rec ? 'on' : '') + '" data-act="settle">' + esc(o) + '</button>'; }).join('') + '</div></div>';
      }).join('') + '</div>', { attn: true }),
      drill('What each step needs and proves', plural(pl.steps.length, 'step'), '<div class="list">' + pl.steps.map(function (s, i) { return '<div class="li col"><span class="li-main"><b>' + (i + 1) + '.</b> ' + esc(s.t) + '</span><span class="li-sub"><span class="muted">needs</span> ' + (s.needs.length ? s.needs.map(function (n) { return n.indexOf('settle:') === 0 ? '<span class="marker">settle ' + n.slice(8) + '</span>' : chip(n); }).join(' ') : 'nothing') + ' <span class="muted">· proof</span> ' + esc(s.proof) + '</span></div>'; }).join('') + '</div>'),
      drill('Where it runs', 'lane C on ' + pl.machine, '<p>Approving opens the item in lane C, beside lanes A and B: one open node per level in each lane.</p><div class="chips">' + chip('lane:' + pl.lane, { meta: true }) + chip('machine:' + pl.machine) + chip('branch:' + pl.opensBranch, { meta: true }) + chip('ruling:R-02') + '</div><p class="small muted" style="margin-top:12px">' + esc(pl.factorNote) + '</p>'),
      drill('Submitted by', 'session ' + pl.session + ' · ' + esc(c.at), chips(['session:' + pl.session, 'machine:' + pl.machine]))
    ], 'Go deeper');
    return h + '</div>';
  }

  /* ================================================================ 5. Stuck */
  function vStuck(id) {
    var c = D.cards[id], st = D.stucks[id], run = D.runs[parseRef(st.check).id];
    var h = cardHead(c) + '<div class="pane">';
    h += dhead('Stuck', 'stuck', 'unblocks ~' + fmtMin(c.weight), 'A check failed three times', itemLink(st.item) + ' · ' + esc(st.step) + ' · the session stopped instead of trying again');
    h += '<blockquote class="quote"><span class="muted small">The session’s diagnosis</span><span class="rich">' + rich(st.diagnosis) + '</span></blockquote>';
    h += '<div class="actions"><button class="btn" data-act="reassign" data-card="' + id + '">Reassign</button><button class="btn" data-toast="One more attempt allowed · session b915 on laptop retries step 2">Retry once more</button><button class="btn" data-toast="Step 2 dropped on WB-2.3 · session b915 moves to step 3">Drop the step</button><span class="spacer"></span>' + replyBtn(id) + talkBtn('card:' + id) + '</div>' + replyBox(id);
    h += drills([
      drill('Last failure', esc(st.checkName.replace('tests · ', '')), '<pre class="instr">' + esc(run.out.slice(0, 6).join('\n')) + '</pre><p class="small"><a href="#/run/' + parseRef(st.check).id + '">Full output</a></p>'),
      drill('Three attempts', st.attempts.map(function (a) { return a.result; }).join(' → '), '<div class="list">' + st.attempts.map(function (a, i) { return '<div class="li"><span class="li-main"><b>' + (i + 1) + '.</b> ' + esc(a.t) + '</span><span class="li-meta tone-bad">' + esc(a.result) + '</span>' + chip(a.run) + '</div>'; }).join('') + '</div>'),
      drill('Where', 'session ' + st.session + ' on ' + st.machine + ' · since ' + esc(c.at), chips(['session:' + st.session, 'machine:' + st.machine, st.check]))
    ], 'Go deeper');
    return h + '</div>';
  }

  /* ================================================================ 6. Info Card */
  function vInfo(id) {
    var c = D.cards[id];
    var h = head(['project:' + c.project]) + '<div class="pane">';
    h += dhead('For you to know', 'info', esc(c.at), esc(c.headline), rich(c.body));
    h += '<div class="chips" style="margin:-8px 0 24px">' + chip(c.chip, { meta: true }) + '</div>';
    h += '<div class="actions"><button class="btn" data-act="dismiss" data-card="' + id + '">Dismiss</button>' + (c.ruling ? '<button class="btn primary" data-act="make-ruling" data-card="' + id + '">Make a ruling</button>' : '') + '<span class="spacer"></span>' + replyBtn(id, 'Write a note') + '</div>' + replyBox(id);
    if (c.ruling) h += '<p class="small muted">Looks like a settled convention: “' + esc(c.ruling) + '”</p>';
    h += drills([drill('Where it came from', esc(trunc(c.origin.replace(/\[\[[^\]]+\]\]/g, 'the packet'), 60)), '<p class="rich">' + rich(c.origin) + '</p>')]);
    return h + '</div>';
  }

  /* ================================================================ 7. Work: the tree */
  function nodeChildren(id) {
    if (id === 'root') return D.projects.filter(function (p) { return S.project === 'all' || p.id === S.project; }).map(function (p) { return p.id; });
    var p = projOf(id);
    if (p) return p.children.concat(p.unscheduled || []).concat(S.work.expanded[id] && p.preview ? p.preview : []);
    var it = D.items[id];
    return it ? it.children : [];
  }
  function hasKids(id) { if (id === 'root') return true; var p = projOf(id); if (p) return p.children.length || (p.unscheduled || []).length || (p.preview || []).length; var it = D.items[id]; return it && it.children.length; }
  function nodeParent(id) { if (id === 'root') return null; if (projOf(id)) return 'root'; var it = D.items[id]; return it ? (it.parent || it.project) : null; }
  function ancestors(id) { var out = []; var p = nodeParent(id); while (p) { out.unshift(p); p = nodeParent(p); } return out; }
  function onOpenPath(id) { if (id === 'root') return true; var p = projOf(id); if (p) return p.state === 'open'; var it = D.items[id]; return it && it.isOpen; }
  function canOpen(id) {
    if (id === 'root') return { ok: false, why: 'The programme is always open.' };
    var p = projOf(id);
    if (p) return p.state === 'open' ? { ok: false, why: 'Already open.' } : { ok: false, why: 'Workbench is open at this level. One open node per level (R-02): close it or start a lane first.' };
    var it = D.items[id];
    if (it.preview) return { ok: false, why: 'Language is closed. These four appear as real items when an approved plan opens it.' };
    if (it.unscheduled) return { ok: false, why: 'Unscheduled: no open parent path. Dope must open first.' };
    if (it.isOpen) return { ok: false, why: 'Already open in lane ' + it.lane + '.' };
    if (it.state === 'done') return { ok: false, why: 'Done items do not reopen; add a BUG under it instead.' };
    if (it.state === 'review') return { ok: false, why: 'In review: approve or ask for changes on its packet.' };
    if (it.plan) return { ok: true, why: 'Opens through its plan in lane C.', go: '#/card/' + parseRef(it.plan).id };
    var sib = it.parent ? 'WB-2.2 (lane A) and WB-2.3 (lane B) are open at this level' : 'WB-2 is open at this level in lanes A and B';
    return { ok: false, why: sib + '. One open node per level per lane (R-02).' };
  }
  function nodeTitle(id) { if (id === 'root') return D.meta.programme + ' programme'; var p = projOf(id); return p ? p.name : itemTitle(id); }
  function matchesFilter(id) {
    var it = D.items[id]; if (!it) return false;
    var k = keys(S.work.kinds).filter(function (x) { return S.work.kinds[x]; });
    if (k.length && k.indexOf(it.kind) < 0) return false;
    if (S.work.state !== 'all' && it.state !== S.work.state) return false;
    if (S.work.unsched && !it.unscheduled) return false;
    return true;
  }
  function filterActive() { return keys(S.work.kinds).some(function (k) { return S.work.kinds[k]; }) || S.work.state !== 'all' || S.work.unsched; }
  function visibleSet() {
    if (!filterActive()) return null;
    var vis = {};
    keys(D.items).forEach(function (id) { if (matchesFilter(id) && !(D.items[id].preview)) { vis[id] = 1; ancestors(id).forEach(function (a) { vis[a] = 1; }); } });
    return vis;
  }
  function blockedOnYou(id) { var ids = projOf(id) ? keys(D.items).filter(function (i) { return D.items[i].project === id; }) : id === 'root' ? keys(D.items) : scope(id); return cardsForScope(ids); }
  function stepsDone(it) { return (it.steps || []).filter(function (s) { return s.state === 'done'; }).length; }
  function nodeMeta(id) {
    var p = projOf(id), it = D.items[id];
    if (id === 'root') return '8 projects';
    if (p) return p.state === 'open' ? 'open · ~' + p.coarseWeeks + ' weeks' : '~' + p.coarseWeeks + ' weeks';
    if (it.preview) return 'preview · ' + it.coarse;
    if (it.unscheduled) return 'unscheduled · ' + it.coarse;
    if (it.state === 'coarse') return it.coarse;
    if (it.state === 'planned') return 'planned · ' + it.coarse;
    if (it.state === 'review') return 'in review';
    if (it.state === 'done') return 'done';
    if (it.state === 'planning') return 'plan waiting';
    var st = it.steps || [];
    return (it.state === 'blocked' ? 'blocked' : 'building') + (st.length ? ' · ' + stepsDone(it) + ' of ' + st.length : '');
  }
  function treeRow(id, d, vis) {
    var p = projOf(id), it = D.items[id], isRoot = id === 'root';
    var exp = !!S.work.expanded[id] || !!vis, kids = hasKids(id);
    var you = !isRoot && blockedOnYou(id).length;
    var cls = 'tr' + (isRoot ? ' root' : p ? ' proj' : '') + (onOpenPath(id) ? ' path' : '') + (S.work.sel === id && S.layout === 'desktop' ? ' sel' : '') + (it && it.preview ? ' preview' : '') + (it && (it.state === 'coarse' || it.state === 'planned') ? ' closed' : '');
    var disc = kids ? '<button class="disc" data-act="toggle" data-node="' + id + '" aria-label="' + (exp ? 'Collapse' : 'Expand') + '" title="' + (p && p.preview && !exp ? esc(p.closedWhy) : '') + '">' + (exp ? '▾' : '▸') + '</button>' : '<span class="disc none"></span>';
    var row = '<div class="' + cls + '" style="--d:' + d + '">' + disc + '<div class="main" data-act="node" data-node="' + id + '">' +
      (p ? '<span class="pdot" style="--pc:var(--' + p.color + ')"></span>' : it && (it.kind === 'BUG' || it.kind === 'PER') ? '<span class="kt">' + it.kind + '</span>' : '') +
      '<span class="ttl">' + esc(nodeTitle(id)) + '</span>' + issueBadge(id) + '<span class="tmeta">' + esc(nodeMeta(id)) + '</span>' + (you ? '<span class="you" title="' + plural(you, 'card') + ' waiting on you"></span>' : '') + '</div>' +
      (isRoot ? '' : '<button class="rowact" data-act="rowmenu" data-node="' + id + '" aria-label="Actions for ' + esc(nodeTitle(id)) + '">' + ic('dots') + '</button>') + '</div>';
    var out = row;
    if (kids && exp) nodeChildren(id).forEach(function (k) { if (!vis || vis[k]) out += treeRow(k, d + 1, vis); });
    return out;
  }
  function issueBadge(id) {
    if (id === 'root') return '';
    var p = projOf(id), list = p ? fpbsAt('project:' + id) : fpbsAt('item:' + id);
    if (!list.length) return '';
    return '<button class="ibadge" data-act="go" data-href="' + (p ? '#/issues?attach=' + id : '#/item/' + id + '/issues') + '" title="' + esc(fpbCountLabel(list)) + ' attached here">' + ic(list.length === 1 ? D.fpbs[list[0]].kind.toLowerCase() : 'issues') + '<span class="ib-l">' + esc(fpbCountLabel(list)) + '</span><span class="ib-n">' + list.length + '</span></button>';
  }
  function filterCount() { return keys(S.work.kinds).filter(function (k) { return S.work.kinds[k]; }).length + (S.work.state !== 'all' ? 1 : 0) + (S.work.unsched ? 1 : 0); }
  function filterMenu() {
    return '<div class="mi-h">Filter the tree</div><div class="menu-sec"><p class="small muted">Kind</p><div class="chips">' + ['FTR', 'BUG', 'PER', 'task'].map(function (k) { return '<button class="fchip' + (S.work.kinds[k] ? ' on' : '') + '" data-act="wkind" data-k="' + k + '">' + k + '</button>'; }).join('') + '</div>' +
      '<p class="small muted">State</p><select data-act="wstate" aria-label="State" style="width:100%"><option value="all">any state</option>' + ['planning', 'planned', 'building', 'review', 'blocked', 'done', 'coarse'].map(function (s) { return '<option' + (S.work.state === s ? ' selected' : '') + '>' + s + '</option>'; }).join('') + '</select>' +
      '<label class="toggle" style="margin-top:12px"><input type="checkbox" data-act="wunsched"' + (S.work.unsched ? ' checked' : '') + '>Unscheduled only (no open parent path)</label>' +
      '<p class="small muted" style="margin-top:12px">The project filter sits in the header and applies everywhere.</p></div>';
  }
  function vWork(sel) {
    if (sel) S.work.sel = sel;
    var vis = visibleSet(), fc = filterCount();
    var tb = '<div class="wtools" style="margin-bottom:12px"><div class="seg"><button class="on">Tasks</button><button data-go="#/issues">Issues · ' + fpbList().length + '</button></div></div><div class="wtools"><div class="seg"><button class="' + (S.work.view === 'outline' ? 'on' : '') + '" data-act="wview" data-v="outline">Outline</button><button class="' + (S.work.view === 'columns' ? 'on' : '') + '" data-act="wview" data-v="columns">Columns</button></div>' +
      '<button class="btn small" data-act="wfilter">Filter' + (fc ? ' · ' + fc : '') + '</button>' + (fc ? '<button class="btn small text" data-act="wclear">Clear</button>' : '') + '<span class="spacer"></span><button class="btn small" data-act="additem" data-node="WB-2">' + ic('plus') + 'Add item</button></div>';
    var body = S.work.view === 'columns' ? columnsView(vis) : '<div class="tree">' + treeRow('root', 0, vis) + (vis && keys(vis).length === 0 ? empty('Nothing matches', 'Items of the chosen kind and state would list here with their parents.') : '') + '</div>';
    var left = '<div class="lpane wide"><div class="lhead"><h2>Work</h2><p class="muted">The open path is marked. Closed nodes stay one line until a plan opens them.</p></div>' + tb + body + '</div>';
    return '<div class="split"><div>' + left + '</div><div class="side">' + nodeSummary(S.work.sel) + '</div></div>';
  }
  function columnsView(vis) {
    var path = ancestors(S.work.sel).concat([S.work.sel]);
    if (path[0] !== 'root') path.unshift('root');
    var cols = path.filter(function (n) { return hasKids(n); });
    return '<div class="cols-wrap"><div class="cols">' + cols.map(function (node) {
      var kids = nodeChildren(node).filter(function (k) { return !vis || vis[k]; });
      return '<div class="col"><h5>' + esc(nodeTitle(node)) + '</h5>' + (kids.length ? kids.map(function (k) {
        return '<div class="ci' + (path.indexOf(k) >= 0 ? ' path' : '') + (S.work.sel === k ? ' sel' : '') + '" data-act="colsel" data-node="' + k + '"><span class="t">' + esc(nodeTitle(k)) + '</span><span class="tmeta">' + esc(nodeMeta(k).split(' · ')[0]) + '</span>' + (hasKids(k) ? '<span class="arr">›</span>' : '') + '</div>';
      }).join('') : '<p class="small muted" style="padding:8px 14px">No children yet.</p>') + '</div>';
    }).join('') + '</div></div>';
  }
  function nodeSummary(id) {
    var p = projOf(id), it = D.items[id], isRoot = id === 'root';
    var proj = p || (it ? projOf(it.project) : projOf('wb'));
    var coarse = isRoot ? D.meta.workLeft.coarseWeeks : proj.coarseWeeks, measured = isRoot ? D.meta.workLeft.measuredWeeks : proj.measuredWeeks;
    var big, sub;
    if (it && it.time && it.time.forecast != null && it.state !== 'done') { big = fmtMin(Math.max(0, it.time.forecast - it.time.so)) + ' left'; sub = 'measured · ' + fmtMin(it.time.so) + ' so far'; }
    else if (it && it.state === 'done') { big = 'Done'; sub = fmtMin(it.time.so) + ' against an estimate of ' + fmtMin(it.time.est); }
    else if (it) { big = it.coarse; sub = 'coarse · measured once a plan opens it'; }
    else { big = 'about ' + coarse + ' weeks'; sub = measured + ' measured, the rest coarse'; }
    var bo = blockedOnYou(id), co = canOpen(id);
    var h = '<div class="pane side-pane"><p class="eyebrow-s">' + (isRoot ? 'Programme' : p ? 'Project' : esc(it.kind) + ' · ' + esc(nodeMeta(id))) + '</p><h2 class="side-t">' + esc(nodeTitle(id)) + '</h2>';
    h += '<p class="bignum">' + esc(big) + '</p><p class="muted small">' + esc(sub) + '</p>';
    h += '<div class="actions">' + (it ? '<a class="btn primary" href="#/item/' + id + '">Open item</a>' : p ? '<a class="btn primary" href="#/project/' + id + '">Open project</a>' : '<a class="btn primary" href="#/projects">All projects</a>') + (isRoot ? '' : '<button class="btn' + (co.ok ? '' : ' dis') + '" data-act="opennode" data-node="' + id + '" title="' + esc(co.why) + '">Open this node</button>') + '</div>';
    h += drills([
      drill('Waiting on you', bo.length ? plural(bo.length, 'card') : 'nothing', bo.length ? '<div class="list">' + bo.map(function (c) { return '<a class="li link" href="#/card/' + c + '"><span class="li-main">' + esc(D.cards[c].headline) + '</span><span class="li-meta">~' + fmtMin(D.cards[c].weight) + '</span></a>'; }).join('') + '</div>' : empty('Nothing waits on you here', 'Cards that block this node or its children list here.'), { attn: bo.length > 0 }),
      drill('Work left', (isRoot ? 'programme' : esc(proj.name)) + ' · ' + measured + ' of ~' + coarse + ' weeks measured', '<div class="wbar"><span class="m" style="width:' + Math.round(100 * measured / coarse) + '%"></span><span class="c"></span></div><div class="wbar-legend"><span><i class="lm"></i>measured ' + measured + ' wk</span><span><i class="lc"></i>coarse ' + (coarse - measured) + ' wk</span></div>'),
      proj.factor ? drill('Factor', '×' + proj.factor + (proj.id === 'wb' ? ' · lanes A ×1.3, B ×1.1, C ×1.2' : ''), '<p>Actual time over estimate, measured from session hooks.</p>' + (proj.lanes ? chips(proj.lanes.map(function (l) { return 'lane:' + l; }), { meta: true }) : '')) : '',
      proj.criticalPath ? drill('Critical path', proj.criticalPath.length + ' nodes', '<div class="critical">' + proj.criticalPath.map(function (r, i) { return (i ? '<span class="arrow">→</span>' : '') + chip(r); }).join('') + '</div>') : '',
      isRoot ? '' : drill('Opening rule', esc(trunc(co.why, 48)), '<p>' + esc(co.why) + '</p>')
    ]);
    return h + '</div>';
  }
  function vTreeCompact(selItem) {
    var h = '<div class="lpane"><div class="lhead"><h2>Work</h2><p class="muted"><a href="#/work">Open the full tree</a></p></div><div class="tree compact">';
    function walk(id, d) {
      var p = projOf(id), it = D.items[id];
      if (id !== 'root') h += '<a class="tr' + (onOpenPath(id) ? ' path' : '') + (selItem === id ? ' sel' : '') + (it && (it.state === 'coarse' || it.state === 'planned') ? ' closed' : '') + '" style="--d:' + (d - 1) + '" href="' + (it ? '#/item/' + id : '#/project/' + id) + '"><span class="main">' + (p ? '<span class="pdot" style="--pc:var(--' + p.color + ')"></span>' : '') + '<span class="ttl">' + esc(nodeTitle(id)) + '</span><span class="tmeta">' + esc(nodeMeta(id).split(' · ')[0]) + '</span></span></a>';
      var showKids = id === 'root' || (p && (p.state === 'open' || p.unscheduled)) || (it && it.children.length);
      if (showKids) nodeChildren(id).forEach(function (k) { if (!(D.items[k] && D.items[k].preview)) walk(k, d + 1); });
    }
    walk('root', 0);
    return h + '</div></div>';
  }

  /* ================================================================ 8. Item: six tabs, each section one line until opened */
  var TABS = [['overview', 'Overview'], ['about', 'About'], ['plan', 'Plan'], ['changes', 'Changes'], ['issues', 'Issues'], ['decisions', 'Decisions'], ['activity', 'Activity']];
  var TAB_ALIAS = { questions: ['decisions', 'questions'], assumptions: ['decisions', 'assumptions'], found: ['decisions', 'found'], comments: ['changes', 'comments'], runs: ['activity', 'runs'], sessions: ['activity', 'sessions'], history: ['activity', 'history'], rulings: ['overview', 'rulings'] };
  function vItem(id, tab) {
    var it = D.items[id];
    if (!it) return head(null) + '<div class="pane">' + empty('No such item', 'It may have been moved or renamed.') + '</div>';
    var openSec = null;
    if (TAB_ALIAS[tab]) { openSec = TAB_ALIAS[tab][1]; tab = TAB_ALIAS[tab][0]; }
    tab = tab || 'overview';
    var ids = scope(id), waiting = cardsForScope(ids).length;
    var h = head(crumbRefs(id)) + '<div class="pane">';
    h += dhead(esc(it.kind) + ' · ' + esc(nodeMeta(id)), 'item-' + it.kind, waiting ? '<a href="#/item/' + id + '/decisions">' + plural(waiting, 'decision') + ' waiting</a>' : '', esc(it.title), it.goal ? esc(it.goal) : '');
    h += '<nav class="tabs" aria-label="Item sections">' + TABS.map(function (t) { return '<a class="tab' + (t[0] === tab ? ' on' : '') + '" href="#/item/' + id + '/' + t[0] + '">' + t[1] + '</a>'; }).join('') + '</nav>';
    h += ({ overview: tabOverview, about: tabAbout, plan: tabPlan, changes: tabChanges, issues: tabIssues, decisions: tabDecisions, activity: tabActivity }[tab] || tabOverview)(id, openSec);
    return h + '</div>';
  }
  function isCoarse(it) { return (it.state === 'coarse' && !it.goal) || it.preview || it.state === 'planned'; }
  function coarseView(id) {
    var it = D.items[id], co = canOpen(id);
    return '<p class="lead-p">' + rich(it.info ? it.info.text : '') + '</p><p class="muted">Closed: one line with a coarse size (' + esc(it.coarse) + ') and no children until an approved plan opens it.</p>' +
      '<div class="actions"><button class="btn' + (co.ok ? ' primary' : ' dis') + '" data-act="opennode" data-node="' + id + '" title="' + esc(co.why) + '">Open this node</button><button class="btn" data-act="additem" data-node="' + id + '">Add item</button></div><p class="small muted">' + esc(co.why) + '</p>';
  }
  function tabOverview(id, openSec) {
    var it = D.items[id];
    if (isCoarse(it)) return coarseView(id);
    var ids = scope(id), ch = itemChanges(id), st = it.steps || [];
    var cur = st.filter(function (s) { return s.state === 'current' || s.state === 'blocked'; })[0];
    var left = it.time && it.time.forecast != null && it.state !== 'done' ? fmtMin(Math.max(0, it.time.forecast - it.time.so)) + ' left' : it.state === 'done' ? 'done in ' + fmtMin(it.time.so) : 'not started';
    var h = '<div class="status"><div><p class="bignum">' + esc(left) + '</p><p class="muted small">' + (st.length ? stepsDone(it) + ' of ' + st.length + ' steps done' + (cur ? ' · now: ' + esc(cur.t) : '') : esc(nodeMeta(id))) + '</p></div></div>';
    var oqHere = openQuestions(id).length, oqKids = ids.slice(1).reduce(function (a, k) { return a + openQuestions(k).length; }, 0);
    var as = itemAssumptions(ids), asUn = as.filter(function (a) { return D.assumptions[a].state === 'unreviewed'; }).length, fd = itemFound(ids).length;
    var cc = commentCounts(it.commentScope);
    var runs = it.checks || [], failing = runs.filter(function (r) { var x = D.runs[parseRef(r).id]; return x && x.result !== 'pass'; }).length;
    var stepsBody = st.length ? stepsList(it) : (it.plan ? '<p>The plan waits for you: ' + chip(it.plan) + '</p>' : '<p class="muted">' + (it.unscheduled ? 'Unscheduled until Dope opens.' : 'A plan is drawn when the node opens.') + '</p>');
    h += drills([
      drill('Steps', st.length ? stepsDone(it) + ' of ' + st.length + ' done' : 'no plan yet', stepsBody, { open: openSec === 'steps' }),
      drill('Checks', runs.length ? plural(runs.length, 'run') + (failing ? ' · ' + failing + ' not passing' : ' · all passing') : 'no runs yet', runs.length ? chips(runs, { meta: true }) : empty('No runs', 'Checks appear when a session runs one.'), { attn: failing > 0 }),
      drill('Changed', ch.files.length ? plural(ch.files.length, 'file') + ' · +' + ch.plus + ' −' + ch.minus : 'nothing yet', ch.files.length ? chips(ch.files.map(function (f) { return f[0]; })) + '<p class="small" style="margin-top:12px"><a href="#/item/' + id + '/changes">See every change</a></p>' : empty('Nothing changed', 'Files git reports on the branch appear here.')),
      drill('Time', it.time && it.time.factor ? fmtMin(it.time.so) + ' so far · forecast ' + fmtMin(it.time.forecast) : it.time ? 'estimate ' + fmtMin(it.time.est) + ' · not started' : esc(it.coarse), it.time ? '<dl class="kv"><dt>So far</dt><dd>' + fmtMin(it.time.so) + ', measured from hooks</dd><dt>Estimate</dt><dd>' + fmtMin(it.time.est) + ' Claude-minutes</dd><dt>Factor</dt><dd>' + (it.time.factor ? '×' + it.time.factor : 'not measured') + '</dd><dt>Forecast</dt><dd>' + fmtMin(it.time.forecast) + '</dd></dl>' : ''),
      drill('Needs and blocks', ((it.needs || []).filter(function (n) { return !n.note || n.note !== 'done'; }).length ? 'needs ' + (it.needs || []).filter(function (n) { return n.note !== 'done'; }).length : 'needs nothing') + ' · blocks ' + plural((it.blocks || []).length, 'item'), '<p class="small muted">Needs</p>' + ((it.needs || []).length ? '<div class="list">' + it.needs.map(function (n) { return '<div class="li"><span class="li-main">' + chip(n.ref) + '</span><span class="li-meta">' + esc(n.note || '') + '</span></div>'; }).join('') + '</div>' : '<p>nothing</p>') + '<p class="small muted" style="margin-top:12px">Blocks</p>' + ((it.blocks || []).length ? '<div class="list">' + it.blocks.map(function (b) { return '<div class="li"><span class="li-main">' + chip(b.ref) + '</span><span class="li-meta">' + (b.min ? '~' + fmtMin(b.min) + ' behind' : esc(b.note || '')) + '</span></div>'; }).join('') + '</div>' : '<p>nothing</p>')),
      drill('Open', 'questions ' + oqHere + ' of 2' + (oqKids ? ' (+' + oqKids + ' below)' : '') + ' · ' + as.length + ' assumptions · ' + fd + ' found', '<p>' + budgetQ(id) + '</p>' + (oqKids ? '<p class="small">' + oqKids + ' more open in children.</p>' : '') + '<p class="small"><a href="#/item/' + id + '/decisions">Questions, assumptions and found items</a>' + (asUn ? ' · ' + asUn + ' assumptions to review' : '') + '</p>', { attn: asUn > 0 }),
      drill('Issues', fpbsUnder(id).length ? fpbCountLabel(fpbsUnder(id)) + ' attached' : 'none', fpbsUnder(id).length ? '<div class="slist">' + fpbsUnder(id).map(function (i) { return issueRow(i); }).join('') + '</div>' : empty('No issues', 'Features, bugs and performance problems raised on this task appear here.'), { attn: fpbsUnder(id).some(function (i) { return fpbNeeds(i).length; }) }),
      drill('Comments in scope', cc.n ? cc.n + ' · ' + cc.open + ' open' + (cc.stale ? ' · ' + cc.stale + ' stale' : '') : 'none', cc.n ? '<p>' + cc.open + ' open, ' + cc.handled + ' handled, ' + cc.stale + ' stale.</p><p class="small"><a href="#/item/' + id + '/comments">See the comments</a></p>' : empty('No comments', '+ comments in this item\'s files appear here.'), { attn: cc.stale > 0 }),
      drill('Claude’s note', it.note ? esc(trunc(it.note.text, 56)) : 'none', it.note ? '<p>' + esc(it.note.text) + '</p><p class="small muted">' + (it.note.session ? 'Session ' + parseRef(it.note.session).id : 'Seed import') + ' · ' + esc(it.note.date) + '</p>' + (it.claims ? '<p class="small muted" style="margin-top:12px">Claims</p>' + claimsList(it.claims) : '') : ''),
      drill('Rulings that apply', plural((it.rulings || []).length, 'ruling'), (it.rulings || []).length ? '<div class="list">' + it.rulings.map(function (r) { return '<a class="li link" href="' + hrefFor(r) + '"><span class="li-main">' + esc(get(r).title) + '</span></a>'; }).join('') + '</div>' : empty('No rulings', 'Rulings this item\'s brief carries appear here.'), { open: openSec === 'rulings' }),
      drill('Where it lives', [it.machine, it.branch && D.branches[it.branch] ? D.branches[it.branch].name : null, it.lane ? 'lane ' + it.lane : null].filter(Boolean).join(' · ') || 'nowhere yet', '<div class="chips">' + (it.machine ? chip('machine:' + it.machine) : '') + (it.branch && D.branches[it.branch] ? chip('branch:' + it.branch, { meta: true }) : '') + (it.lane ? it.lane.split(' + ').map(function (l) { return chip('lane:' + l, { meta: true }); }).join('') : '') + '</div>')
    ]);
    return h;
  }
  function stepsList(it) {
    return '<ol class="steps-simple">' + it.steps.map(function (s) {
      return '<li class="' + s.state + '"><span class="tick">' + (s.state === 'done' ? '✓' : s.state === 'blocked' ? '!' : '') + '</span><span class="st-t">' + esc(s.t) + (s.state === 'current' ? ' <span class="marker">now</span>' : s.state === 'blocked' ? ' <span class="marker bad">blocked</span>' : '') + '</span>' + chip(s.item || s.proof) + '</li>';
    }).join('') + '</ol>';
  }
  function accList(list) { return claimsList(list); }
  function tabAbout(id) {
    var it = D.items[id], inf = it.info;
    if (!inf || inf.tpl === 'coarse') return coarseView(id);
    var label = { task: 'Task', FTR: 'Slice', BUG: 'Bug', PER: 'Performance' }[inf.tpl];
    var pre = '<p class="muted small" style="margin-bottom:20px">' + label + ' page, drawn from the ' + inf.tpl + ' template.</p>';
    if (inf.tpl === 'task') return pre + '<p class="lead-p">' + esc(inf.purpose) + '</p>' + drills([
      drill('Design decisions that apply', plural(inf.rulings.length, 'ruling'), '<div class="list">' + inf.rulings.map(function (r) { return '<a class="li link" href="' + hrefFor(r) + '"><span class="li-main">' + esc(get(r).title) + '</span></a>'; }).join('') + '</div>'),
      drill('Files in scope', plural(inf.files.length, 'file'), '<div class="list">' + inf.files.map(function (f) { return '<div class="li"><span class="li-main">' + chip(f[0]) + '</span><span class="li-meta">' + esc(f[1]) + '</span></div>'; }).join('') + '</div>'),
      drill('Acceptance checks', inf.acceptance.length + (inf.acceptance.some(function (a) { return !a.proof; }) ? ' · 1 unverified' : ''), accList(inf.acceptance), { attn: inf.acceptance.some(function (a) { return !a.proof; }) }),
      drill('Risks', plural(inf.risks.length, 'risk'), plainList(inf.risks.map(rich))),
      drill('What this unblocks', plural(inf.unblocks.length, 'item'), '<div class="list">' + inf.unblocks.map(function (u) { return '<div class="li"><span class="li-main">' + chip(u.ref) + '</span><span class="li-meta">~' + fmtMin(u.min) + '</span></div>'; }).join('') + '</div>')
    ]);
    if (inf.tpl === 'FTR') return pre + '<p class="lead-p">' + esc(inf.goal) + '</p>' + drills([
      drill('In scope', plural(inf.scopeIn.length, 'thing'), plainList(inf.scopeIn.map(rich))),
      drill('Out of scope', plural(inf.scopeOut.length, 'thing'), plainList(inf.scopeOut.map(rich))),
      drill('Parity with the old app', plural(inf.parity.length, 'note'), plainList(inf.parity.map(esc))),
      drill('Screens and constructs touched', String(inf.touched.length), plainList(inf.touched.map(function (t) { return t.indexOf('[[') === 0 ? rich(t) : esc(t); }))),
      drill('Acceptance', inf.acceptance.length + (inf.acceptance.some(function (a) { return !a.proof; }) ? ' · 1 unverified' : ''), accList(inf.acceptance), { attn: inf.acceptance.some(function (a) { return !a.proof; }) })
    ]);
    if (inf.tpl === 'BUG') return pre + '<p class="lead-p">' + esc(inf.symptom) + '</p><p class="muted small">' + esc(inf.severity) + '</p>' + drills([
      drill('How to reproduce', inf.repro.length + ' steps', '<ol class="plain-ol">' + inf.repro.map(function (s) { return '<li>' + esc(s) + '</li>'; }).join('') + '</ol><div class="chips" style="margin-top:12px"><span class="small muted">reproduced by</span>' + chip(inf.reproRun, { meta: true }) + '</div>'),
      drill('Suspected location', esc(D.files[parseRef(inf.suspected).id].path.split('/').pop()) + ', lines ' + parseRef(inf.suspected).range.join('–'), codeBlock(inf.suspected)),
      drill('Evidence', esc(inf.evidence[0]), '<pre class="instr">' + esc(inf.evidence.join('\n')) + '</pre>'),
      drill('Fixed when', esc(trunc(inf.fixedWhen.t, 50)), '<p>' + esc(inf.fixedWhen.t) + '</p>' + chip(inf.fixedWhen.check))
    ]);
    if (inf.tpl === 'PER') return pre + perHero(inf) + drills([
      drill('Method', esc(trunc(inf.method, 52)), '<p>' + esc(inf.method) + '</p>' + chip(inf.methodRun, { meta: true })),
      drill('What changed', plural(inf.changed.length, 'change'), plainList(inf.changed.map(esc)) + '<div style="margin-top:12px">' + chip('file:librarycardrail') + '</div>'),
      drill('Memory and CPU', 'memory +12 MB · CPU one thread for 2 s', '<dl class="kv">' + inf.side.map(function (s) { return '<dt>' + esc(s.t) + '</dt><dd class="rich">' + rich(s.v) + '</dd>'; }).join('') + '</dl>'),
      drill('Regression check', 'warns at 0.66 s, fails at 0.75 s', '<p>' + esc(inf.regression.t) + '</p>' + chip(inf.regression.check))
    ]);
    return '';
  }
  function perHero(inf) {
    var b = inf.before, a = inf.after, bud = inf.budget, max = b.v, W = 320, x0 = 64, w = W - x0 - 50;
    var bw = Math.round(w * b.v / max), aw = Math.max(4, Math.round(w * a.v / max)), bx = x0 + Math.round(w * bud.v / max);
    var svg = '<svg class="per-svg" viewBox="0 0 ' + W + ' 104" role="img" aria-label="Before ' + b.v + ' s, after ' + a.v + ' s, bar ' + bud.v + ' s">' +
      '<text x="0" y="29">before</text><rect class="bar-b" x="' + x0 + '" y="14" width="' + bw + '" height="20" rx="4"/><text class="v" x="' + (x0 + bw + 6) + '" y="29">' + b.v + ' s</text>' +
      '<text x="0" y="69">after</text><rect class="bar-a" x="' + x0 + '" y="54" width="' + aw + '" height="20" rx="4"/><text class="v" x="' + (x0 + aw + 6) + '" y="69">' + a.v + ' s</text>' +
      '<line class="bud" x1="' + bx + '" y1="6" x2="' + bx + '" y2="84"/><text x="' + (bx + 4) + '" y="100">bar ' + bud.v + ' s</text></svg>';
    return '<div class="per"><div class="per-nums"><span class="n from">' + b.v + '<small> s</small></span><span class="arrow-big">→</span><span class="n to">' + a.v + '<small> s</small></span></div><p class="muted">' + Math.round(b.v / a.v) + '× faster, still ' + Math.round((a.v - bud.v) * 1000) + ' ms over the ' + bud.v + ' s bar.</p>' + svg + '<div class="chips">' + chip(b.run, { meta: true }) + chip(a.run, { meta: true }) + chip(bud.ruling) + '</div></div>';
  }
  function tabPlan(id) {
    var it = work(id);
    if (!(it.steps || []).length) return it.plan ? '<p class="lead-p">The plan waits for your approval.</p><div class="actions"><a class="btn primary" href="#/card/' + parseRef(it.plan).id + '">Review the plan</a></div>' : empty('No plan yet', 'An approved plan of up to 7 steps, each with an estimate and the check that proves it, appears here.');
    var total = it.steps.reduce(function (a, s) { return a + s.est; }, 0);
    var approved = (it.history || []).filter(function (e) { return e[1] === 'plan approved'; })[0];
    var h = '<p class="muted small" style="margin-bottom:16px">Approved by you' + (approved ? ' ' + esc(approved[0]) : '') + ' · ' + it.steps.length + ' of ' + D.budgets[2].value + ' steps · ' + total + ' Claude-min' + (it.time && it.time.factor ? ' · forecast ' + fmtMin(it.time.forecast) : '') + '</p>';
    h += '<div class="drills">' + it.steps.map(function (s, i) {
      return drill('<span class="sn ' + s.state + '">' + (s.state === 'done' ? '✓' : i + 1) + '</span>' + esc(s.t), (s.state === 'current' ? 'now · ' : s.state === 'blocked' ? 'blocked · ' : '') + s.est + ' min', '<dl class="kv"><dt>Proof</dt><dd>' + chip(s.proof, { meta: true }) + '</dd>' + (s.item ? '<dt>Child</dt><dd>' + chip(s.item, { meta: true }) + '</dd>' : '') + (s.needs ? '<dt>Needs</dt><dd>' + chips(s.needs, { meta: true }) + '</dd>' : '') + (s.note ? '<dt>Note</dt><dd>' + esc(s.note) + '</dd>' : '') + '</dl>', { attn: s.state === 'blocked' });
    }).join('') + '</div>';
    if (D.fpbs[id]) h += '<p class="small" style="margin-top:16px"><a href="#/fpb/' + id + '/report">Back to the report</a></p>';
    if (id === 'WB-2.1') h += '<div class="notice soft" style="margin-top:24px"><p><b>Beyond the plan:</b> 1 helper added, justified.</p><button class="btn text" data-act="beyond" data-pk="pk-runs" data-i="0">See why</button></div>';
    return h;
  }
  function tabChanges(id, openSec) {
    var it = work(id), ch = itemChanges(id), list = it.commentScope || [], cc = commentCounts(list);
    var unseen = ch.files.filter(function (f) { return !D.files[parseRef(f[0]).id].seen; }).length;
    if (!ch.files.length && !list.length) return empty('No changes', 'Files git reports as changed on this item\'s branch list here, with whether you have seen them.');
    var filesBody = ch.files.length ? '<div class="list">' + ch.files.map(function (f) { var fe = D.files[parseRef(f[0]).id]; return '<div class="li"><span class="li-main">' + chip(f[0]) + '</span><span class="li-meta"><span class="tone-good">+' + f[1] + '</span> <span class="tone-bad">−' + f[2] + '</span>' + (fe.seen ? '' : ' <span class="marker new">new to you</span>') + '</span><a class="li-act" href="#/diff/' + parseRef(f[0]).id + '">diff</a></div>'; }).join('') + '</div><p class="small" style="margin-top:12px"><a href="#/diff/' + (it.packet ? parseRef(it.packet).id : 'item-' + id) + '">Read the whole diff</a></p>' : empty('No files', 'Nothing changed on the branch yet.');
    return drills([
      drill('Files', ch.files.length ? plural(ch.files.length, 'file') + ' · +' + ch.plus + ' −' + ch.minus + (unseen ? ' · ' + unseen + ' new to you' : '') : 'none', filesBody, { open: openSec === 'files', attn: unseen > 0 }),
      drill('New types', ch.types.length ? plural(ch.types.length, 'type') : 'none', ch.types.length ? '<div class="list">' + ch.types.map(function (t) { var te = D.types[parseRef(t).id]; return '<div class="li"><span class="li-main">' + chip(t) + '</span><span class="li-meta">' + (te.seen ? '' : '<span class="marker new">new to you</span>') + (te.beyondPlan ? ' <span class="marker warn">beyond plan</span>' : '') + '</span></div>'; }).join('') + '</div>' : empty('No new types', 'Public types added on the branch appear here.')),
      drill('+ comments', cc.n ? cc.n + ' · ' + cc.open + ' open · ' + cc.handled + ' handled' + (cc.stale ? ' · ' + cc.stale + ' stale' : '') : 'none', list.length ? '<p class="small muted" style="margin-bottom:12px">Read from the source files; the Workbench never edits them.</p>' + list.map(function (c) { return commentCard(c); }).join('') : empty('No comments in scope', '+ comments in the files this item touches list here.'), { open: openSec === 'comments', attn: cc.stale > 0 })
    ]);
  }
  function tabDecisions(id, openSec) {
    var ids = scope(id);
    var qs = itemQuestions(ids), open = qs.filter(function (q) { return D.questions[q].state === 'open'; }), ans = qs.filter(function (q) { return D.questions[q].state !== 'open'; });
    var as = itemAssumptions(ids), un = as.filter(function (a) { return D.assumptions[a].state === 'unreviewed'; }).length, fd = itemFound(ids);
    function qrow(qid) {
      var q = D.questions[qid];
      return q.state === 'open' ? '<a class="li link" href="#/card/' + q.card + '"><span class="li-main">' + esc(q.text) + '</span><span class="li-meta">answer ›</span></a>'
        : '<div class="li col"><span class="li-main">' + esc(q.text) + '</span><span class="li-sub"><span class="tone-good">' + esc(q.answer) + '</span>' + (q.ruling ? ' → ' + chip(q.ruling) : '') + '</span></div>';
    }
    var qBody = '<p style="margin-bottom:12px">' + budgetQ(id) + '</p>' + (qs.length ? (open.length ? '<p class="small muted">Open</p><div class="list">' + open.map(qrow).join('') + '</div>' : '') + (ans.length ? '<p class="small muted" style="margin-top:16px">Answered</p><div class="list">' + ans.map(qrow).join('') + '</div>' : '') : empty('No questions', 'Open questions (2 per item at most) and answered ones with their rulings list here.'));
    return drills([
      drill('Questions', open.length ? open.length + ' open · ' + ans.length + ' answered' : ans.length + ' answered', qBody, { open: openSec === 'questions', attn: open.length > 0 }),
      drill('Assumptions', as.length ? as.length + (un ? ' · ' + un + ' to review' : ' · all fine') : 'none', as.length ? '<p class="small muted" style="margin-bottom:12px">Never pushed to the inbox. Glance, then mark fine or flip one.</p>' + assumptionBatch(as) : empty('No assumptions', 'Assumptions a session makes are batched here by session.'), { open: openSec === 'assumptions', attn: un > 0 }),
      drill('Found items', fd.length ? fd.length + ' in the tray' : 'none', fd.length ? '<p class="small muted" style="margin-bottom:12px">Only you make them tasks; at most ' + D.budgets[1].value + ' per session.</p>' + foundTray(fd) : empty('Tray is empty', 'Found items wait here for Task, Later or Drop.'), { open: openSec === 'found' })
    ]);
  }
  function tabActivity(id, openSec) {
    var it = D.items[id], r = itemRuns(scope(id)), s = it.sessions || [], hi = it.history || [];
    var tot = s.reduce(function (a, x) { return a + x[1]; }, 0);
    return drills([
      drill('Runs', r.length ? plural(r.length, 'run') : 'none', r.length ? runsTable(r) : empty('No runs', 'Runs appear when a session runs a check on this item.'), { open: openSec === 'runs' }),
      drill('Sessions', s.length ? plural(s.length, 'session') + ' · ' + fmtMin(tot) : 'none', s.length ? '<div class="list">' + s.map(function (x) { return '<div class="li"><span class="li-main">' + chip(x[0], { meta: true }) + '</span><span class="li-meta">' + fmtMin(x[1]) + '</span></div>'; }).join('') + '</div>' : empty('No sessions', 'Sessions that worked on this item list here with their minutes.'), { open: openSec === 'sessions' }),
      drill('History', hi.length ? plural(hi.length, 'event') + ' · last ' + esc(hi[hi.length - 1][1]) : 'none', hi.length ? '<div class="list">' + hi.slice().reverse().map(function (e) { return '<div class="li col"><span class="li-main">' + esc(e[1]) + ' <span class="muted small">· ' + esc(e[0]) + '</span></span><span class="li-sub rich">' + rich(e[2]) + '</span></div>'; }).join('') + '</div>' : empty('No history', 'Events land here: opened, plan approved, question answered, packet approved.'), { open: openSec === 'history' })
    ]);
  }
  function commentCard(cid, opt) {
    var c = D.comments[cid], f = D.files[c.file];
    var state = c.state === 'stale' ? '<span class="marker warn" title="Stale: the code under this comment changed after it was written. Re-read it, then keep, rewrite or clear it in Rider.">stale ⓘ</span>' : c.state === 'handled' ? '<span class="marker good">handled</span>' : '<span class="marker">open</span>';
    var body = '<p class="small muted">Above <span class="mono">' + esc(c.symbol) + '</span>, line ' + c.line + ' of ' + esc(f.path.split('/').pop()) + '</p>' +
      (c.handled ? '<code class="cmt done">//+= handled ' + esc(c.handled.date) + ' · session ' + esc(c.handled.session) + ' · ' + esc(c.handled.note) + '</code><div class="chips" style="margin:8px 0 12px"><span class="small muted">proof</span>' + chip(c.handled.proof) + '</div>' : '') +
      (c.state === 'stale' ? '<p class="small rich">' + rich(c.staleWhy) + '</p>' : '') +
      (c.question ? '<div class="chips" style="margin-bottom:12px"><span class="small muted">became</span>' + chip(c.question) + '</div>' : '') +
      '<details class="inline"><summary>Show the lines</summary>' + codeBlock('file:' + c.file + ':' + Math.max(1, c.line - 1) + '-' + Math.min(f.lines.length, c.line + 6), { small: true, focus: c.line }) + '</details>' +
      '<details class="inline"><summary>History · ' + plural(c.versions.length, 'version') + '</summary><ul class="versions">' + c.versions.map(function (v, i) { return '<li><span class="small muted">v' + (i + 1) + ' · ' + esc(v.date) + '</span><code>//+ ' + esc(v.text) + '</code></li>'; }).join('') + '</ul></details>' +
      '<div class="actions small-gap"><button class="btn small" data-toast="Opening ' + esc(f.path.split('/').pop()) + ':' + c.line + ' in Rider">Open in Rider</button><a class="btn small text" href="#/comment/' + cid + '">Comment page</a></div>';
    return '<details class="cmt-row ' + c.state + '"><summary><code>//+ ' + esc(c.text) + '</code>' + state + '<span class="chev"></span></summary><div class="db">' + body + '</div></details>';
  }
  function runsTable(runIds) {
    return '<div class="list">' + runIds.map(function (r) {
      var x = D.runs[r];
      return '<a class="li link" href="#/run/' + r + '"><span class="dot ' + (x.result === 'pass' ? 'online' : x.result === 'warn' ? 'waiting' : 'bad') + '"></span><span class="li-main">' + esc(x.check) + (x.total ? ' ' + x.passed + '/' + x.total : x.value ? ' · ' + esc(x.value) : '') + '</span><span class="li-meta">' + esc(x.machine) + ' · ' + esc(x.at.replace(/^\d+ Nov /, '')) + '</span></a>';
    }).join('') + '</div>';
  }
  function rowMenu(id) {
    var co = canOpen(id), it = D.items[id];
    return '<div class="mi-h">' + esc(nodeTitle(id)) + '</div>' +
      (it ? '<a class="mi" href="#/item/' + id + '" data-act="close-after">' + ic('open') + 'Open item page</a>' : '<a class="mi" href="#/project/' + id + '" data-act="close-after">' + ic('open') + 'Open project page</a>') +
      '<button class="mi' + (co.ok ? '' : ' dis') + '" data-act="opennode" data-node="' + id + '">' + ic('work') + '<span>Open this node<span class="why">' + esc(co.why) + '</span></span></button>' +
      '<button class="mi" data-act="additem" data-node="' + id + '">' + ic('plus') + '<span>Add item<span class="why">Owner only · FTR, BUG, PER or task</span></span></button>' +
      '<button class="mi" data-act="move" data-node="' + id + '">' + ic('move') + 'Move…</button>' +
      '<button class="mi" data-act="rename" data-node="' + id + '">' + ic('edit') + 'Rename…</button>' +
      '<button class="mi" data-act="summary" data-node="' + id + '">' + ic('projects') + 'Show summary</button>';
  }
  function placeOptions(selId) {
    var out = [];
    function walk(id, d) { if (id !== 'root') out.push('<option' + (id === selId ? ' selected' : '') + '>' + new Array(d).join('— ') + esc(nodeTitle(id)) + '</option>'); nodeChildren(id).forEach(function (k) { if (!(D.items[k] && D.items[k].preview)) walk(k, d + 1); }); }
    walk('root', 0);
    return out.join('');
  }

  /* ================================================================ 10. Now */
  var SESSION_ORDER = ['7f3a', 'b915', '2a6d', 'c21e', 'e5c1', '9c40', '51b0'];
  function stateLabel(s) { return s.stuck ? 'stuck · waiting on you' : s.state === 'waiting' ? 'waiting on you' : s.state; }
  function vNowList(sel) {
    var list = SESSION_ORDER.filter(function (id) { return S.project === 'all' || work(D.sessions[id].item).project === S.project; });
    var live = list.filter(function (id) { return D.sessions[id].state !== 'ended'; }), ended = list.filter(function (id) { return D.sessions[id].state === 'ended'; });
    var waiting = live.filter(function (id) { return D.sessions[id].state === 'waiting'; }).length;
    var h = '<div class="lpane"><div class="lhead"><h2>Now</h2><p class="muted">' + (live.length ? plural(live.length, 'session') + ' running' + (waiting ? ', ' + waiting + ' waiting on you' : '') : 'No live sessions') + '</p></div>';
    function row(id) {
      var s = D.sessions[id];
      return '<a class="srow' + (sel === id ? ' sel' : '') + '" href="#/session/' + id + '"><span class="dot ' + s.state + '"></span><span class="sb-main"><span class="st1">' + esc(itemTitle(s.item)) + '</span><span class="st2">' + esc(stateLabel(s)) + ' · ' + esc(s.machine) + ' · ' + s.minutes + ' min' + (s.waitingSince ? ' · since ' + esc(s.waitingSince) : '') + '</span></span></a>';
    }
    h += live.length ? '<div class="slist">' + live.map(row).join('') + '</div>' : empty('No sessions', 'Claude Code sessions on your machines appear here with their state and step.');
    if (ended.length) h += '<details class="fyk"><summary><span>Ended</span><span class="muted">' + plural(ended.length, 'session') + '</span><span class="chev"></span></summary><div class="slist">' + ended.map(row).join('') + '</div></details>';
    h += '<details class="fyk"' + (S.machinesOpen ? ' open' : '') + '><summary><span>Machines</span><span class="muted">' + keys(D.machines).length + ' online · laptop clock 1.8 s behind</span><span class="chev"></span></summary><div class="slist">' + keys(D.machines).map(function (m) { var x = D.machines[m]; return '<a class="srow" href="#/machine/' + m + '"><span class="dot ' + (x.online ? 'online' : 'offline') + '"></span><span class="sb-main"><span class="st1">' + esc(m) + '</span><span class="st2">' + plural(x.homed.length, 'item') + ' homed · Host ' + esc(x.host) + ' · seen ' + esc(x.lastSeen) + '</span></span></a>'; }).join('') + '</div></details>';
    return h + '</div>';
  }
  function machineBody(id) {
    var m = D.machines[id];
    return '<dl class="kv"><dt>System</dt><dd>' + esc(m.os) + '</dd><dt>Host</dt><dd>' + esc(m.host) + '</dd><dt>Last seen</dt><dd>' + esc(m.lastSeen) + '</dd><dt>Clock</dt><dd class="' + (m.clockWarn ? 'tone-warn' : '') + '">' + esc(m.clock) + '</dd><dt>Token</dt><dd>issued ' + esc(m.token) + '</dd></dl>';
  }
  function checkoutsBody(id) {
    var m = D.machines[id];
    return '<div class="list">' + m.checkouts.map(function (c) { return '<div class="li col"><span class="li-main mono small">' + esc(c.path) + '</span><span class="li-sub">' + (c.worktree ? 'worktree · ' : '') + (D.branches[c.branch] ? chip('branch:' + c.branch) : esc(c.branch)) + (c.item ? ' ' + chip('item:' + c.item) : '') + (c.note ? ' · ' + esc(c.note) : '') + '</span></div>'; }).join('') + '</div>';
  }
  function vSession(id) {
    var s = D.sessions[id];
    if (!s) return head(null) + '<div class="pane">' + empty('No such session', 'It may have ended long ago.') + '</div>';
    var h = head(crumbRefs(s.item).concat(['session:' + id])) + '<div class="pane">';
    h += dhead('Session ' + id, s.state === 'waiting' ? 'stuck' : s.state === 'working' ? 'approve' : 'info', '<span class="dot ' + s.state + '"></span> ' + esc(stateLabel(s)), esc(itemTitle(s.item)), esc(s.step) + ' · ' + esc(s.machine) + ' · ' + s.minutes + ' min');
    if (s.waitingOn.length) h += '<div class="notice"><p><b>Waiting on you.</b> ' + s.waitingOn.map(function (c) { return '<a href="#/card/' + parseRef(c).id + '">' + esc(D.cards[parseRef(c).id].headline) + '</a>'; }).join(' · ') + '</p></div>';
    h += '<div class="actions"><button class="btn" data-act="message" data-session="' + id + '"' + (s.state === 'ended' ? ' disabled' : '') + '>Message</button><button class="btn" data-toast="Stop sent to session ' + id + '"' + (s.state === 'ended' ? ' disabled' : '') + '>Stop</button><button class="btn" data-toast="Resume sent to the Host on ' + s.machine + ' for session ' + id + '"' + (s.state === 'working' ? ' disabled' : '') + '>Resume</button></div>';
    h += drills([
      drill('Effort', s.minutes + ' min · ' + s.turns + ' turns · ' + s.tools + ' tool calls', '<dl class="kv"><dt>Started</dt><dd>' + esc(s.started) + '</dd>' + (s.ended ? '<dt>Ended</dt><dd>' + esc(s.ended) + '</dd>' : '') + '<dt>Minutes</dt><dd>' + s.minutes + '</dd><dt>Turns</dt><dd>' + s.turns + '</dd><dt>Tool calls</dt><dd>' + s.tools + '</dd></dl>'),
      drill('Checks run', s.checks.length ? plural(s.checks.length, 'run') : 'none', s.checks.length ? runsTable(s.checks.map(function (r) { return parseRef(r).id; })) : empty('No checks yet', 'Runs reported by the hook for this session appear here.')),
      drill('Questions asked', s.questions.length ? String(s.questions.length) : 'none', s.questions.length ? chips(s.questions, { meta: true }) : empty('No questions', 'Questions this session asked appear here.')),
      drill('Assumptions', s.assumptions.length ? String(s.assumptions.length) : 'none', s.assumptions.length ? assumptionBatch(s.assumptions) : empty('No assumptions', 'Assumptions this session made appear here.')),
      drill('Found items', s.found.length + ' of ' + D.budgets[1].value, s.found.length ? chips(s.found) : empty('None found', 'At most five per session; they go to the item\'s tray.')),
      s.packet ? drill('Packet', esc(D.cards[parseRef(s.packet).id].headline), chip(s.packet, { meta: true })) : '',
      drill('Turn ledger', plural(s.ledger.length, 'entry', 'entries'), '<div class="list">' + s.ledger.map(function (l) { return '<div class="li"><span class="li-meta mono" style="min-width:44px">' + esc(l[0]) + '</span><span class="li-main">' + esc(l[2]) + '</span></div>'; }).join('') + '</div>'),
      drill('Machine', esc(s.machine), chip('machine:' + s.machine, { meta: true }) + (D.lanes[s.lane] ? chip('lane:' + s.lane, { meta: true }) : ''))
    ], 'Go deeper');
    return h + '</div>';
  }
  function vMachine(id) {
    var m = D.machines[id];
    if (!m) return head(null) + '<div class="pane">' + empty('No such machine', 'Machines appear when a Host registers.') + '</div>';
    var h = head(['machine:' + id]) + '<div class="pane">' + dhead(m.kind === 'cloud' ? 'Cloud session' : 'Machine', 'info', '<span class="dot ' + (m.online ? 'online' : 'offline') + '"></span> ' + (m.online ? 'online' : 'offline'), esc(id), esc(m.os) + ' · Host ' + esc(m.host));
    if (m.clockWarn) h += '<div class="notice"><p><b>Clock ' + esc(m.clock) + '.</b> Timestamps from this machine cannot order events against desk-pc.</p></div>';
    h += drills([
      drill('Homes', plural(m.homed.length, 'item'), chips(m.homed.map(function (i) { return 'item:' + i; }), { meta: true }), { open: true }),
      drill('Checkouts and worktrees', plural(m.checkouts.length, 'checkout'), checkoutsBody(id)),
      drill('Sessions', plural(m.sessions.length, 'session'), chips(m.sessions.map(function (s) { return 'session:' + s; }), { meta: true })),
      drill('Details', 'Host ' + m.host + ' · token ' + m.token, machineBody(id) + '<div class="actions"><button class="btn small" data-toast="Token for ' + id + ' would be revoked; its Host stops posting at once">Revoke token</button></div>')
    ]);
    return h + '</div>';
  }
  /* 14. Machines */
  function vMachines() {
    var h = '<div class="pane"><div class="lhead flush"><h2>Machines</h2><p class="muted">' + keys(D.machines).length + ' registered · a task has one home machine at a time</p></div>';
    h += '<div class="drills">' + keys(D.machines).map(function (id) {
      var m = D.machines[id];
      return drill('<span class="dot ' + (m.online ? 'online' : 'offline') + '"></span> ' + esc(id), plural(m.homed.length, 'item') + ' · Host ' + m.host + (m.clockWarn ? ' · clock 1.8 s behind' : ''), machineBody(id) + '<p class="small muted" style="margin:16px 0 8px">Checkouts</p>' + checkoutsBody(id) + '<div class="actions small-gap"><a class="btn small" href="#/machine/' + id + '">Machine page</a><button class="btn small text" data-toast="Token for ' + id + ' would be revoked">Revoke token</button></div>', { attn: !!m.clockWarn });
    }).join('') + drill('Install Host on a new machine', '5 steps', '<ol class="plain-ol">' + D.install.map(function (s) { return '<li>' + esc(s) + '</li>'; }).join('') + '</ol>') + '</div>';
    return h + '</div>';
  }

  /* ================================================================ 11. Files */
  function filesWithComments() { var o = {}; keys(D.comments).forEach(function (c) { o[D.comments[c].file] = 1; }); return keys(o); }
  function countText(files) {
    var o = 0, hd = 0, s = 0;
    keys(D.comments).forEach(function (c) { var x = D.comments[c]; if (files.indexOf(x.file) < 0) return; if (x.state === 'open') o++; else if (x.state === 'handled') hd++; else s++; });
    return [o ? o + ' open' : '', hd ? hd + ' handled' : '', s ? '<span class="tone-warn" title="Stale: the code under the comment changed after it was written">' + s + ' stale</span>' : ''].filter(Boolean).join(' · ');
  }
  function vFilesList(sel) {
    var files = filesWithComments();
    var scopeList = S.filesScope === 'item' ? (D.items[S.filesItem].commentScope || []) : null;
    if (scopeList) files = files.filter(function (f) { return scopeList.some(function (c) { return D.comments[c].file === f; }); });
    if (S.project !== 'all' && S.project !== 'wb') files = [];
    var h = '<div class="lpane"><div class="lhead"><h2>Files</h2><p class="muted">Files with + comments. Read only.</p></div>';
    h += '<div class="lctrl"><div class="seg"><button class="' + (S.filesScope === 'all' ? 'on' : '') + '" data-act="fscope" data-v="all">All</button><button class="' + (S.filesScope === 'item' ? 'on' : '') + '" data-act="fscope" data-v="item">One item</button></div>' +
      (S.filesScope === 'item' ? '<select data-act="fitem" aria-label="Item">' + ['WB-2', 'WB-2.1', 'WB-2.2', 'WB-2.3', 'WB-2.4'].map(function (i) { return '<option value="' + i + '"' + (S.filesItem === i ? ' selected' : '') + '>' + esc(trunc(itemTitle(i), 28)) + '</option>'; }).join('') + '</select>' : '') + '</div>';
    if (!files.length) return h + empty('No + comments here', S.project !== 'all' && S.project !== 'wb' ? 'This project\'s files carry no + comments yet.' : 'No file in this item\'s scope carries a + comment.') + '</div>';
    var groups = {};
    files.forEach(function (f) { var fe = D.files[f]; (groups[fe.proj] = groups[fe.proj] || []).push(f); });
    keys(groups).sort().forEach(function (g) {
      h += '<p class="lgroup">' + esc(g) + '</p><div class="slist">' + groups[g].map(function (f) { var fe = D.files[f]; return '<a class="srow' + (sel === f ? ' sel' : '') + '" href="#/file/' + f + '"><span class="sb-main"><span class="st1">' + esc(fe.path.split('/').pop()) + '</span><span class="st2">' + countText([f]) + '</span></span></a>'; }).join('') + '</div>';
    });
    return h + '</div>';
  }
  function vFile(id, q) {
    var f = D.files[id];
    if (!f) return head(null) + '<div class="pane">' + empty('No such file', 'It may have been renamed on its branch.') + '</div>';
    var cs = by(keys(D.comments).filter(function (c) { return D.comments[c].file === id; }), function (c) { return D.comments[c].line; });
    var range = q && q.l ? q.l.split('-').map(Number) : null;
    var h = head(['file:' + id]) + '<div class="pane">' + dhead('File', 'info', f.seen ? 'seen ' + esc(f.lastSeen) : '<span class="marker new">new to you · ' + f.newLines + ' lines</span>', esc(f.path.split('/').pop()), '<span class="mono small">' + esc(f.path) + '</span>');
    if (!f.seen) h += '<div class="notice"><p>You have not seen the current version. <a href="#/diff/' + id + '">Read the ' + f.newLines + ' changed lines first</a>.</p></div>';
    h += cs.length ? '<div class="cmts">' + cs.map(function (c) { return commentCard(c); }).join('') + '</div>' : empty('No + comments in this file', 'Lines starting //+ appear here in file order.');
    h += drills([
      drill(range ? 'Lines ' + range[0] + '–' + range[1] : 'All lines', f.lines.length + ' lines', codeBlock('file:' + id + (range ? ':' + range[0] + '-' + range[1] : ':1-' + f.lines.length), { cap: range ? 12 : 400, focus: range ? range[0] : null }), { open: !!range }),
      drill('Diff since master', f.added ? 'new file' : 'changed', diffBlock(fileDiff(id), { small: true })),
      drill('Branch', f.branch ? esc(D.branches[f.branch].name) : 'Dope repository', f.branch ? chip('branch:' + f.branch, { meta: true }) : '<p>From the Dope seed.</p>')
    ]);
    return h + '</div>';
  }
  function vComment(id) {
    var c = D.comments[id];
    if (!c) return head(null) + '<div class="pane">' + empty('No such comment', 'It may have been cleared from the source.') + '</div>';
    var f = D.files[c.file];
    var h = head(['file:' + c.file, 'comment:' + id]) + '<div class="pane">' + dhead('+ comment · ' + c.state, c.state === 'handled' ? 'approve' : c.state === 'stale' ? 'stuck' : 'question', esc(f.path.split('/').pop()) + ' · line ' + c.line, '<code class="big-code">//+ ' + esc(c.text) + '</code>', 'Above <span class="mono">' + esc(c.symbol) + '</span> (' + esc(c.symbolKind) + ')');
    if (c.state === 'stale') h += '<div class="notice"><p class="rich"><b>Stale.</b> ' + rich(c.staleWhy) + '</p></div>';
    if (c.handled) h += '<div class="notice soft"><p><b>Handled</b> by session ' + esc(c.handled.session) + ' on ' + esc(c.handled.date) + ' ' + esc(c.handled.time) + '</p><code class="cmt done">//+= ' + esc(c.handled.note) + '</code></div>';
    if (c.question) h += '<p>Became a question: ' + chip(c.question, { meta: true }) + '</p>';
    var cm = c.handled ? D.commits[parseRef(c.handled.proof).id] : null;
    h += drills([
      drill('The lines', 'lines ' + Math.max(1, c.line - 2) + '–' + Math.min(f.lines.length, c.line + 8), codeBlock('file:' + c.file + ':' + Math.max(1, c.line - 2) + '-' + Math.min(f.lines.length, c.line + 8), { focus: c.line }), { open: true }),
      cm ? drill('Proof diff', esc(cm.msg), diffBlock(cm.diff) + chip(c.handled.proof, { meta: true })) : '',
      drill('Versions', plural(c.versions.length, 'version'), '<ul class="versions">' + c.versions.map(function (v, i) { return '<li><span class="small muted">v' + (i + 1) + ' · ' + esc(v.date) + '</span><code>//+ ' + esc(v.text) + '</code></li>'; }).join('') + '</ul>'),
      drill('Item', esc(itemTitle(c.item)), chip('item:' + c.item, { meta: true }))
    ]);
    return h + '</div>';
  }

  /* ================================================================ 12. Rulings */
  function citedCount(rid) { return backlinks('ruling:' + rid).length; }
  function vRulingsList(sel, mode) {
    var q = S.rulingQ.toLowerCase();
    var h = '<div class="lpane"><div class="lhead"><h2>Rulings</h2><p class="muted">Decisions in your words. ' + keys(D.rulings).length + ' recorded.</p></div>';
    h += '<div class="lctrl"><div class="search inline"><svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="' + P.search + '"/></svg><input type="search" placeholder="Search rulings" value="' + esc(S.rulingQ) + '" data-act="rq" aria-label="Search rulings"></div></div>';
    h += '<a class="proposal-link' + (mode === 'proposed' ? ' sel' : '') + '" href="#/rulings/proposed"><span><b>' + D.proposed.items.length + ' proposed from chat</b><span class="muted small"> · tick the ones you mean</span></span><span class="chev"></span></a>';
    var groups = {};
    keys(D.rulings).forEach(function (id) { var r = D.rulings[id]; if (S.project !== 'all' && r.project !== S.project) return; if (q && (r.title + ' ' + r.text).toLowerCase().indexOf(q) < 0) return; (groups[r.project] = groups[r.project] || []).push(id); });
    var gk = D.projects.map(function (p) { return p.id; }).filter(function (p) { return groups[p]; });
    if (!gk.length) return h + empty('No rulings match', 'Rulings appear here grouped by project.') + '</div>';
    gk.forEach(function (p) {
      h += '<p class="lgroup">' + esc(projOf(p).name) + '</p><div class="slist">' + groups[p].map(function (id) { var r = D.rulings[id]; return '<a class="srow' + (sel === id ? ' sel' : '') + '" href="#/ruling/' + id + '"><span class="sb-main"><span class="st1">' + esc(r.title) + '</span><span class="st2">' + esc(r.date) + ' · cited by ' + citedCount(id) + '</span></span></a>'; }).join('') + '</div>';
    });
    return h + '</div>';
  }
  function vRuling(id) {
    var r = D.rulings[id];
    if (!r) return head(null) + '<div class="pane">' + empty('No such ruling', 'It may be a proposal not yet recorded.') + '</div>';
    var back = backlinks('ruling:' + id);
    var h = head(['project:' + r.project, 'ruling:' + id]) + '<div class="pane">' + dhead('Ruling · ' + esc(r.date), 'question', r.neverReopened ? 'never reopened' : '<span class="tone-warn">may reopen</span>', esc(r.title), '');
    h += '<p class="ruling-text">' + esc(r.text) + '</p>';
    h += drills([
      drill('Where it came from', esc(r.origin), '<p>' + esc(r.origin) + ', ' + esc(r.date) + '.</p>' + (r.question || r.item ? '<div class="chips">' + (r.question ? chip(r.question, { meta: true }) : '') + (r.item ? chip(r.item, { meta: true }) : '') + '</div>' : '') + (r.reopenNote ? '<p class="small tone-warn" style="margin-top:12px">' + esc(r.reopenNote) + '</p>' : '')),
      drill('Cited by', plural(back.length, 'record'), back.length ? chips(back, { meta: true }) : empty('Not cited yet', 'Items, cards and questions that carry this ruling appear here.')),
      drill('Related', plural(r.related.length, 'link'), chips(r.related))
    ]);
    return h + '</div>';
  }
  function vProposed() {
    var p = D.proposed;
    var h = head(null) + '<div class="pane">' + dhead('Proposed from chat', 'question', esc(p.when), esc(p.chat), 'The only way anything from a chat reaches the store. Tick what you mean; the rest is not kept.');
    h += '<div class="ticks">' + p.items.map(function (x) { return '<label class="tick-row"><input type="checkbox"' + (x.ticked ? ' checked' : '') + '><span>' + esc(x.text) + '</span></label>'; }).join('') + '</div>';
    h += '<div class="actions"><button class="btn primary" data-act="record-ticked">Record ticked</button><button class="btn text" data-toast="Batch discarded · the chat stays in claude.ai">Discard</button></div>';
    return h + '</div>';
  }

  /* ================================================================ 13. Projects */
  function spark(p) {
    var v = p.burn, W = 240, H = 48, max = Math.max.apply(null, v), min = Math.min.apply(null, v) - 4;
    var pts = v.map(function (y, i) { return [Math.round(i * (W - 10) / (v.length - 1)) + 5, Math.round(6 + (H - 20) * (1 - (y - min) / (max - min)))]; });
    var line = pts.map(function (q, i) { return (i ? 'L' : 'M') + q[0] + ' ' + q[1]; }).join(' ');
    return '<svg class="spark" viewBox="0 0 ' + W + ' ' + H + '" preserveAspectRatio="none" role="img" aria-label="Measured hours left: ' + v.join(', ') + '"><path class="a" d="' + line + ' L' + pts[pts.length - 1][0] + ' ' + (H - 12) + ' L5 ' + (H - 12) + ' Z"/><path class="l" d="' + line + '"/><circle cx="' + pts[pts.length - 1][0] + '" cy="' + pts[pts.length - 1][1] + '" r="3"/><text x="5" y="' + (H - 1) + '">' + p.burnDays[0] + ' · ' + v[0] + 'h</text><text x="' + (W - 5) + '" y="' + (H - 1) + '" text-anchor="end">' + p.burnDays[p.burnDays.length - 1] + ' · ' + v[v.length - 1] + 'h</text></svg>';
  }
  function kindCounts(pid) { var c = { FTR: 0, BUG: 0, PER: 0, task: 0 }; keys(D.items).forEach(function (i) { var it = D.items[i]; if (it.project === pid && !it.preview) c[it.kind]++; }); return c; }
  function projectCard(p) {
    var bo = blockedOnYou(p.id);
    return '<article class="pcard" data-act="pfilter-go" data-p="' + p.id + '" style="--pc:var(--' + p.color + ')"><div class="pc-top"><span class="pdot"></span><h3>' + esc(p.name) + '</h3><span class="muted small">' + (p.state === 'open' ? 'open' : 'coarse') + '</span></div>' +
      '<p class="pc-num">~' + p.coarseWeeks + ' weeks<span class="muted small">' + (p.measuredWeeks ? ' · ' + p.measuredWeeks + ' measured' : '') + '</span></p>' +
      '<div class="wbar"><span class="m" style="width:' + Math.round(100 * p.measuredWeeks / p.coarseWeeks) + '%"></span><span class="c"></span></div>' +
      (p.burn ? spark(p) : '') +
      '<p class="small muted pc-foot">' + (bo.length ? '<span class="tone-warn">' + plural(bo.length, 'decision') + ' waiting</span>' : p.state === 'open' ? 'nothing waiting' : esc(trunc(p.closedWhy, 70))) + '</p>' +
      '<a class="btn small text" href="#/project/' + p.id + '">Project page</a></article>';
  }
  function vProjects() {
    return '<div class="pane wide-pane"><div class="lhead flush"><h2>Projects</h2><p class="muted">About ' + D.meta.workLeft.coarseWeeks + ' weeks of work, ' + D.meta.workLeft.measuredWeeks + ' measured. Tap a project to filter everything to it.</p></div><div class="pgrid">' + D.projects.map(projectCard).join('') + '</div></div>';
  }
  function vProject(id) {
    var p = projOf(id);
    if (!p) return head(null) + '<div class="pane">' + empty('No such project', 'Projects are created by you only.') + '</div>';
    var items = keys(D.items).filter(function (i) { return D.items[i].project === id; });
    var kc = kindCounts(id), bo = blockedOnYou(id), rs = keys(D.rulings).filter(function (r) { return D.rulings[r].project === id; });
    var h = head(['project:' + id]) + '<div class="pane">' + dhead('Project · ' + p.state, 'info', bo.length ? '<span class="tone-warn">' + plural(bo.length, 'decision') + ' waiting</span>' : '', esc(p.name), esc(p.purpose));
    h += '<p class="bignum">about ' + p.coarseWeeks + ' weeks</p><p class="muted small">' + (p.measuredWeeks ? p.measuredWeeks + ' measured, the rest coarse' : 'none measured yet') + '</p><div class="wbar" style="max-width:420px"><span class="m" style="width:' + Math.round(100 * p.measuredWeeks / p.coarseWeeks) + '%"></span><span class="c"></span></div>';
    h += '<div class="actions"><button class="btn primary" data-act="pfilter-go" data-p="' + id + '">Filter to ' + esc(p.name) + '</button><button class="btn" data-act="additem" data-node="' + id + '">Add item</button></div>';
    h += drills([
      p.burn ? drill('Burn-down', p.burn[0] + 'h → ' + p.burn[p.burn.length - 1] + 'h measured, since ' + p.burnDays[0], spark(p)) : '',
      drill('Items', items.length ? items.length + ' · ' + ['FTR', 'BUG', 'PER', 'task'].filter(function (k) { return kc[k]; }).map(function (k) { return kc[k] + ' ' + k; }).join(', ') : 'none yet', items.length ? '<div class="list">' + items.map(function (i) { return '<a class="li link" href="#/item/' + i + '"><span class="li-main">' + esc(itemTitle(i)) + '</span><span class="li-meta">' + esc(nodeMeta(i)) + '</span></a>'; }).join('') + '</div>' : empty('No items yet', (p.closedWhy || '') + ' Items appear when you add them.')),
      bo.length ? drill('Waiting on you', plural(bo.length, 'card'), '<div class="list">' + bo.map(function (c) { return '<a class="li link" href="#/card/' + c + '"><span class="li-main">' + esc(D.cards[c].headline) + '</span><span class="li-meta">~' + fmtMin(D.cards[c].weight) + '</span></a>'; }).join('') + '</div>', { attn: true }) : '',
      p.criticalPath ? drill('Critical path', p.criticalPath.length + ' nodes', '<div class="critical">' + p.criticalPath.map(function (r, i) { return (i ? '<span class="arrow">→</span>' : '') + chip(r); }).join('') + '</div>') : '',
      p.lanes ? drill('Lanes', p.lanes.length + ' · factor ×' + p.factor, '<div class="list">' + p.lanes.map(function (l) { var ln = D.lanes[l]; return '<div class="li"><span class="li-main">' + chip('lane:' + l, { meta: true }) + '</span><span class="li-meta">' + esc(ln.state) + '</span></div>'; }).join('') + '</div>') : '',
      drill('Issues', (function () { var l = fpbList().filter(function (i) { return D.fpbs[i].project === id; }); return l.length ? fpbCountLabel(l) : 'none'; })(), (function () { var l = fpbList().filter(function (i) { return D.fpbs[i].project === id; }); return l.length ? '<div class="slist">' + l.map(function (i) { return issueRow(i); }).join('') + '</div>' : empty('No issues', 'Features, bugs and performance problems on this project\'s tasks appear here.'); })()),
      drill('Rulings', plural(rs.length, 'ruling'), rs.length ? '<div class="list">' + rs.map(function (r) { return '<a class="li link" href="#/ruling/' + r + '"><span class="li-main">' + esc(D.rulings[r].title) + '</span></a>'; }).join('') + '</div>' : empty('No rulings', 'Rulings recorded on this project appear here.'))
    ]);
    return h + '</div>';
  }

  /* ================================================================ 15. Settings */
  function vSettings() {
    var h = '<div class="pane"><div class="lhead flush"><h2>Settings</h2></div>';
    var on = D.settings.notifications.filter(function (n) { return n.on; }).length;
    h += drills([
      drill('Budgets', D.budgets.map(function (b) { return b.value; }).join(' · ') + ' (questions, found, steps, note lines)', D.budgets.map(function (b) { return '<div class="setting"><div class="grow"><p>' + esc(b.label) + '</p><p class="small muted">' + esc(b.note) + '</p></div><input type="number" value="' + b.value + '" min="1" max="20" aria-label="' + esc(b.label) + '" data-act="budget"><span class="small muted">default ' + b.def + '</span></div>'; }).join(''), { open: true }),
      drill('Your decision time', D.meta.avgDecisionSec + ' s average', '<p>Measured over ' + D.meta.decisionsMeasured + ' decisions since ' + D.meta.decisionsSince + '. Read only.</p><p class="small muted">Queue now: ' + plural(needsCards('all').length, 'card') + ' × ' + D.meta.avgDecisionSec + ' s = ' + queueText(needsCards('all').length) + '.</p>'),
      drill('Notifications', on + ' of ' + D.settings.notifications.length + ' on · quiet 22:30–06:30', D.settings.notifications.map(function (n) { return '<div class="setting"><div class="grow"><p>' + esc(n.t) + '</p><p class="small muted">' + esc(n.note) + '</p></div><button class="switch' + (n.on ? ' on' : '') + '" role="switch" aria-checked="' + n.on + '" aria-label="' + esc(n.t) + '" data-act="switch"></button></div>'; }).join('')),
      drill('Tokens', D.tokens.length + ' clients', '<div class="list">' + D.tokens.map(function (t) { return '<div class="li"><span class="li-main"><b>' + esc(t.client) + '</b> <span class="muted small">' + esc(t.kind) + '</span></span><span class="li-meta">issued ' + esc(t.issued) + '</span><button class="li-act" data-toast="Token for ' + esc(t.client) + ' would be revoked">Revoke</button></div>'; }).join('') + '</div>'),
      drill('Database and service', D.settings.addresses.length + ' addresses', '<dl class="kv">' + D.settings.addresses.map(function (a) { return '<dt>' + esc(a.t) + '</dt><dd class="mono small">' + esc(a.v) + '</dd>'; }).join('') + '</dl>'),
      drill('Theme', S.theme === 'system' ? 'follows system' : S.theme, '<div class="seg">' + ['system', 'light', 'dark'].map(function (t) { return '<button class="' + (S.theme === t ? 'on' : '') + '" data-act="theme" data-v="' + t + '">' + (t === 'system' ? 'Follow system' : t[0].toUpperCase() + t.slice(1)) + '</button>'; }).join('') + '</div>')
    ]);
    return h + '</div>';
  }

  /* ================================================================ 16. Search */
  function searchAll(q) {
    q = (q || '').toLowerCase().trim();
    if (!q) return [];
    function hit(s) { return String(s || '').toLowerCase().indexOf(q) >= 0; }
    function ctx(s) { s = String(s || '').replace(/\[\[[^\]]*\]\]/g, '…'); var i = s.toLowerCase().indexOf(q); if (i < 0) return trunc(s, 90); var a = Math.max(0, i - 36); return (a ? '…' : '') + s.slice(a, i + q.length + 50) + (i + q.length + 50 < s.length ? '…' : ''); }
    var g = [];
    function group(name, coll, kind, fields) {
      var rows = [];
      keys(coll).forEach(function (id) { var e = coll[id]; var f = fields.map(function (k) { return typeof k === 'function' ? k(e) : e[k]; }).filter(function (x) { return hit(x); })[0]; if (f) rows.push({ ref: kind + ':' + id, ctx: ctx(f) }); });
      if (rows.length) g.push({ name: name, rows: rows });
    }
    group('Rulings', D.rulings, 'ruling', ['title', 'text', 'origin']);
    group('Items', D.items, 'item', ['title', 'goal', function (e) { return e.info && (e.info.text || e.info.symptom || e.info.purpose || e.info.goal); }, function (e) { return e.note && e.note.text; }]);
    group('Issues', D.fpbs, 'fpb', ['title', function (e) { return keys(e.sections).map(function (k) { var x = e.sections[k]; return x && (x.text || ''); }).join(' · '); }]);
    group('Questions', D.questions, 'question', ['text', 'context', 'answer', function (e) { return (e.options || []).map(function (o) { return o.t + ' ' + o.why; }).join(' · '); }]);
    group('Files', D.files, 'file', ['path', function (e) { return e.lines.join('\n'); }]);
    group('Comments', D.comments, 'comment', ['text', function (e) { return e.handled && e.handled.note; }]);
    group('Sessions', D.sessions, 'session', ['title', 'lastAction', 'step', function (e) { return e.ledger.map(function (l) { return l[2]; }).join(' · '); }]);
    group('Found items', D.found, 'found', ['title', 'why']);
    return g;
  }
  function vSearch(q) {
    var g = searchAll(q);
    var h = '<div class="pane"><div class="lhead flush"><h2>Search</h2></div><div class="search big"><svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="' + P.search + '"/></svg><input type="search" value="' + esc(q || '') + '" data-act="search-page" aria-label="Search everything" placeholder="Search items, rulings, questions, files, sessions"></div>';
    if (!q) return h + empty('Type to search', 'Results group into rulings, items, questions, files, comments and sessions.') + '</div>';
    if (!g.length) return h + empty('Nothing found for “' + q + '”', 'Search covers titles, rulings, questions and their options, file lines, comments and session ledgers.') + '</div>';
    h += '<p class="muted small" style="margin:20px 0 4px">' + g.reduce(function (a, x) { return a + x.rows.length; }, 0) + ' results for “' + esc(q) + '”</p>';
    h += drills(g.map(function (x, i) { return drill(x.name, plural(x.rows.length, 'result'), '<div class="list">' + x.rows.map(function (r) { return '<a class="li col link" href="' + hrefFor(r.ref) + '"><span class="li-main">' + esc(chipInfo(r.ref).title) + '</span><span class="li-sub">' + esc(r.ctx) + '</span></a>'; }).join('') + '</div>', { open: i < 2 }); }));
    return h + '</div>';
  }

  /* ================================================================ templates: run, commit, branch, type, CLAUDE.md, diff, more */
  function vRun(id) {
    var r = D.runs[id];
    if (!r) return head(null) + '<div class="pane">' + empty('No such run', 'Runs are reported by the PostToolUse hook.') + '</div>';
    var title = esc(r.check) + (r.total ? ' ' + r.passed + '/' + r.total : r.value ? ' · ' + esc(r.value) : '');
    var h = head(crumbRefs(r.item).concat(['run:' + id])) + '<div class="pane">' + dhead('Run ' + id + (r.attempt ? ' · attempt ' + r.attempt + ' of 3' : '') + (r.seed ? ' · seed' : ''), r.result === 'pass' ? 'approve' : r.result === 'warn' ? 'plan' : 'stuck', (r.result === 'pass' ? 'passed' : r.result === 'warn' ? 'warned' : 'failed') + ' · ' + fmtSec(r.dur), title, esc(r.machine) + (r.session ? ' · session ' + r.session : '') + ' · ' + esc(r.at));
    h += '<div class="code output"><pre><code>' + r.out.slice(0, 12).map(function (l, i) { return '<span class="ln"><span class="n">' + (i + 1) + '</span>' + esc(l) + '</span>'; }).join('') + '</code></pre></div>';
    h += drills([
      r.out.length > 12 ? drill('The first 40 lines', r.out.length + ' lines captured', '<div class="code output"><pre><code>' + r.out.slice(0, 40).map(function (l, i) { return '<span class="ln"><span class="n">' + (i + 1) + '</span>' + esc(l) + '</span>'; }).join('') + '</code></pre></div><p class="small muted">The full log stays on ' + esc(r.machine) + '.</p>') : '',
      drill('Command', '<span class="mono">' + esc(trunc(r.cmd, 48)) + '</span>', '<pre class="instr">$ ' + esc(r.cmd) + '</pre>'),
      drill('Where', esc(r.machine) + (r.session ? ' · session ' + r.session : ' · seed'), '<div class="chips">' + chip('machine:' + r.machine, { meta: true }) + (r.also ? chip('machine:' + r.also) : '') + (r.session ? chip('session:' + r.session, { meta: true }) : '') + chip(workRef(r.item), { meta: true }) + '</div>' + (r.exitCode != null ? '<p class="small" style="margin-top:12px">exit code ' + r.exitCode + '</p>' : ''))
    ]);
    return h + '</div>';
  }
  function vCommit(id) {
    var c = D.commits[id];
    if (!c) return head(null) + '<div class="pane">' + empty('No such commit', 'Commits appear as sessions make them locally.') + '</div>';
    var br = c.branch ? D.branches[c.branch] : null;
    var h = head((br ? ['branch:' + c.branch] : []).concat(['commit:' + id])) + '<div class="pane">' + dhead('Commit · ' + esc(c.date), 'info', '<span class="mono">' + id + '</span>', esc(c.msg), c.files + ' files' + (c.plus || c.minus ? ' · +' + c.plus + ' −' + c.minus : '') + (br ? ' · local, merged only on your press' : ' · a tagged baseline'));
    if (c.proves) h += '<p>Proves ' + chip('comment:' + c.proves, { meta: true }) + '</p>';
    if (c.stales) h += '<p>Made ' + chip('comment:' + c.stales, { meta: true }) + ' stale</p>';
    h += diffBlock(c.diff) + (c.session ? drills([drill('Who and where', 'session ' + c.session + (br ? ' · ' + esc(br.name) : ''), chips(['session:' + c.session].concat(br ? ['branch:' + c.branch] : []).concat(['ruling:R-05'])))]) : '');
    return h + '</div>';
  }
  function vBranch(id) {
    var b = D.branches[id];
    if (!b) return head(null) + '<div class="pane">' + empty('No such branch', 'Branches are created when you approve a plan.') + '</div>';
    var h = head(crumbRefs(b.item).concat(['branch:' + id])) + '<div class="pane">' + dhead('Branch · ' + esc(b.state), 'info', b.machine, '<span class="mono">' + esc(b.name) + '</span>', 'ahead ' + b.ahead + ' · behind ' + b.behind + (b.pushed ? ' · pushed' : ''));
    if (b.dirty) h += '<div class="notice"><p>' + esc(b.dirty) + '</p></div>';
    h += drills([
      drill('Commits', b.commits ? plural(b.commits.length, 'commit') : b.merged ? 'merged' : 'none', b.commits ? chips(b.commits, { meta: true }) : b.merged ? '<p>Merged as ' + chip(b.merged) + '</p>' : empty('No commits', 'Local commits appear here.'), { open: true }),
      drill('Worktree', '<span class="mono">' + esc(trunc(b.worktree, 44)) + '</span>', '<p class="mono small">' + esc(b.worktree) + '</p>'),
      drill('Item and machine', esc(itemTitle(b.item)) + ' · ' + b.machine, chips(['item:' + b.item, 'machine:' + b.machine]))
    ]);
    return h + '</div>';
  }
  function vType(id) {
    var t = D.types[id];
    if (!t) return head(null) + '<div class="pane">' + empty('No such type', 'Types are indexed from the files on each branch.') + '</div>';
    var h = head(crumbRefs(t.item).concat(['type:' + id])) + '<div class="pane">' + dhead(esc(t.kind), 'info', t.seen ? 'seen' : '<span class="marker new">new to you</span>', '<span class="mono">' + esc(id) + '</span>', rich(t.summary));
    if (t.beyondPlan) h += '<div class="notice soft"><p>Built beyond the plan, justified in the packet.</p></div>';
    h += codeBlock(t.at, { cap: 40 }) + drills([drill('Namespace', '<span class="mono">' + esc(t.ns) + '</span>', '<p class="mono small">' + esc(t.ns) + '</p>' + (t.newIn ? '<p>New in ' + chip(t.newIn) + '</p>' : ''))]);
    return h + '</div>';
  }
  function vClaudeMd(id) {
    var c = D.claudemd[id];
    if (!c) return head(null) + '<div class="pane">' + empty('No such section', 'CLAUDE.md sections are indexed by heading.') + '</div>';
    var h = head(['file:claudemd', 'claudemd:' + id]) + '<div class="pane">' + dhead('CLAUDE.md ' + c.n, 'info', '', esc(c.title), 'What every session reads before it starts.') + codeBlock(c.file, { cap: 20 });
    h += drills([drill('Other sections', plural(keys(D.claudemd).length - 1, 'section'), '<div class="list">' + keys(D.claudemd).filter(function (k) { return k !== id; }).map(function (k) { return '<a class="li link" href="#/claudemd/' + k + '"><span class="li-main">' + esc(D.claudemd[k].title) + '</span></a>'; }).join('') + '</div>')]);
    return h + '</div>';
  }
  function vDiff(id) {
    var files = [], title, refs;
    if (D.packets[id]) { var it = work(D.packets[id].item); files = (it.changes ? it.changes.files : []).map(function (f) { return parseRef(f[0]).id; }); title = D.packets[id].outcome; refs = crumbRefs(D.packets[id].item).concat(['card:' + id]); }
    else if (id.indexOf('item-') === 0) { var iid = id.slice(5); files = itemChanges(iid).files.map(function (f) { return parseRef(f[0]).id; }); title = itemTitle(iid); refs = crumbRefs(iid); }
    else if (D.files[id]) { files = [id]; title = D.files[id].path.split('/').pop(); refs = ['file:' + id]; }
    var h = head(refs) + '<div class="pane">' + dhead('Diff since master', 'info', plural(files.length, 'file'), esc(title || 'Diff'), 'Files you have not seen are open; the rest are one line.');
    if (!files.length) return h + empty('Nothing to diff', 'A branch with changes against master shows its diff here.') + '</div>';
    h += drills(files.map(function (f) { var fe = D.files[f]; var d = fileDiff(f); return drill(esc(fe.path.split('/').pop()), (fe.seen ? 'seen' : 'new to you · ' + fe.newLines + ' lines') + ' · ' + d.filter(function (l) { return l[0] === '+' || l[0] === '-'; }).length + ' changed lines', diffBlock(d, { small: true }) + '<div class="actions small-gap"><button class="btn small" data-toast="Marked ' + esc(fe.path.split('/').pop()) + ' as seen">Mark seen</button><a class="btn small text" href="#/file/' + f + '">File page</a></div>', { open: !fe.seen || files.length === 1, attn: !fe.seen }); }));
    return h + '</div>';
  }
  function vMore() {
    return '<div class="pane"><div class="lhead flush"><h2>More</h2></div><div class="slist">' +
      [['#/projects', 'Projects', 'Work left, lanes and critical paths'], ['#/machines', 'Machines', 'Hosts, checkouts and tokens'], ['#/settings', 'Settings', 'Budgets, notifications, theme']].map(function (r) { return '<a class="srow" href="' + r[0] + '"><span class="sb-main"><span class="st1">' + r[1] + '</span><span class="st2">' + r[2] + '</span></span></a>'; }).join('') +
      '<button class="srow" data-act="drawer"><span class="sb-main"><span class="st1">Screens</span><span class="st2">Every screen and sample, for review</span></span></button></div></div>';
  }

  /* ================================================================ issues (FTR, BUG, PER): data helpers */
  S.edgeState = {}; S.fix = {}; S.sev = {}; S.target = {}; S.iss = { kind: 'all', needs: false };
  try { localStorage.removeItem('qwb.folds'); } catch (e) {}
  S.folds = (function () { try { return JSON.parse(store('folds2') || '{}'); } catch (e) { return {}; } })();
  var KIND_NAME = { FTR: 'Feature', BUG: 'Bug', PER: 'Performance' };
  function fpbList() { return keys(D.fpbs); }
  function edgeConfirmed(e) { var st = S.edgeState[e.id]; return st ? st === 'confirmed' : e.confirmed; }
  function edgeLive(e) { return S.edgeState[e.id] !== 'removed'; }
  function needsOf(ref) { return D.edges.filter(function (e) { return edgeLive(e) && e.type === 'needs' && e.from === ref; }); }
  function blocksOf(ref) { return D.edges.filter(function (e) { return edgeLive(e) && e.type === 'needs' && e.to === ref; }); }
  function relOf(ref, type) { return D.edges.filter(function (e) { return edgeLive(e) && e.type === type && (e.from === ref || e.to === ref); }); }
  function unblockMin(ref) { return blocksOf(ref).filter(edgeConfirmed).reduce(function (a, e) { return a + (e.min || 0); }, 0); }
  function pendingMin(ref) { return blocksOf(ref).filter(function (e) { return !edgeConfirmed(e); }).reduce(function (a, e) { return a + (e.min || 0); }, 0); }
  function chosenFix(id) { var sec = D.fpbs[id].sections.fixes; if (!sec) return undefined; return (id in S.fix) ? S.fix[id] : sec.chosen; }
  function severityOf(id) { var sv = D.fpbs[id].severity; if (!sv) return null; return S.sev[id] ? { level: S.sev[id], reason: sv.reason, confirmed: true, by: 'you' } : sv; }
  function targetOf(id) { return S.target[id] || D.fpbs[id].sections.target || null; }
  function refProject(ref) { var r = parseRef(ref); if (r.kind === 'item') return D.items[r.id] && D.items[r.id].project; if (r.kind === 'fpb') return D.fpbs[r.id].project; if (r.kind === 'project') return r.id; return null; }
  function attachOpen(f) { var at = parseRef(f.attach); if (at.kind === 'project') return projOf(at.id).state === 'open' && false; return onOpenPath(at.id); }
  function unscheduled(f) { return projOf(f.project).state !== 'open'; }
  function fpbsAt(ref) { return fpbList().filter(function (id) { return D.fpbs[id].attach === ref; }); }
  function fpbsUnder(itemId) { var ids = scope(itemId).map(function (i) { return 'item:' + i; }); return fpbList().filter(function (id) { return ids.indexOf(D.fpbs[id].attach) >= 0; }); }
  function fpbCountLabel(ids) {
    if (!ids.length) return '';
    var k = ids.map(function (i) { return D.fpbs[i].kind; }), same = k.every(function (x) { return x === k[0]; });
    var word = same ? { FTR: 'feature', BUG: 'bug', PER: 'perf issue' }[k[0]] : 'issue';
    return plural(ids.length, word);
  }
  function fpbLead(id) { var s = D.fpbs[id].sections; return (s.goal && s.goal.text) || (s.symptom && s.symptom.text) || (targetOf(id) && targetOf(id).text) || ''; }
  function sourceText(f) {
    var s = f.source;
    if (s.type === 'promoted') return 'promoted by you from the found tray, ' + esc(s.date) + ' · found by ' + chip(s.session) + ' on ' + esc(D.sessions[parseRef(s.session).id].machine) + ' as ' + chip(s.found);
    if (s.type === 'imported') return 'imported from the old register as ' + chip(s.reg) + ', ' + esc(s.date);
    if (s.type === 'split') return 'split from ' + chip(s.from) + ', ' + esc(s.date);
    if (s.type === 'duplicate') return 'duplicate of ' + chip(s.of) + ', ' + esc(s.date);
    return 'created by you, ' + esc(s.date);
  }
  function fpbNeeds(id) {
    var f = D.fpbs[id], ref = 'fpb:' + id, out = [];
    openQuestions(id).forEach(function (q) { var x = D.questions[q]; out.push({ key: 'q', html: '<a class="need-row" href="' + hrefFor('question:' + q) + '"><span class="nk">Answer</span><span class="nt">' + esc(x.text) + '</span><span class="chev"></span></a>' }); });
    D.edges.filter(function (e) { return edgeLive(e) && !edgeConfirmed(e) && (e.from === ref || e.to === ref); }).forEach(function (e) {
      var other = e.from === ref ? e.to : e.from;
      out.push({ key: 'edge', html: '<button class="need-row" data-act="edge" data-edge="' + e.id + '"><span class="nk">Confirm</span><span class="nt">' + (e.from === ref ? 'Needs ' : 'Blocks ') + esc(chipInfo(other).title) + ' · proposed by Claude</span><span class="chev"></span></button>' });
    });
    var un = itemAssumptions([id]).filter(function (a) { return D.assumptions[a].state === 'unreviewed'; });
    if (un.length) out.push({ key: 'as', html: '<a class="need-row" href="#/fpb/' + id + '/assumptions"><span class="nk">Review</span><span class="nt">' + plural(un.length, 'assumption') + ' not reviewed</span><span class="chev"></span></a>' });
    if (f.kind === 'PER' && !targetOf(id)) out.push({ key: 'target', html: '<button class="need-row" data-act="set-target" data-fpb="' + id + '"><span class="nk">Set</span><span class="nt">a target: value, unit and conditions</span><span class="chev"></span></button>' });
    if (f.sections.fixes && chosenFix(id) == null) out.push({ key: 'fix', html: '<button class="need-row" data-act="open-sec" data-target="sec-fixes"><span class="nk">Choose</span><span class="nt">a fix: ' + f.sections.fixes.rows.length + ' compared</span><span class="chev"></span></button>' });
    var sv = severityOf(id);
    if (sv && !sv.confirmed) out.push({ key: 'sev', html: '<button class="need-row" data-act="sev" data-fpb="' + id + '"><span class="nk">Confirm</span><span class="nt">severity: ' + esc(sv.level) + ', proposed by Claude</span><span class="chev"></span></button>' });
    return out;
  }
  function stateTone(st) { return /done|verified|closed|fixed|guarded|measured/.test(st) ? 'good' : /dropped|won't|cannot/.test(st) ? 'bad' : /reported|idea|observed/.test(st) ? '' : 'accent'; }
  var SEV_TONE = { blocking: 'bad', serious: 'bad', minor: 'warn', cosmetic: '' };

  /* ================================================================ Issues list */
  function vIssuesList(sel, q) {
    q = q || {};
    var ids = fpbList().filter(function (id) {
      var f = D.fpbs[id];
      if (S.project !== 'all' && f.project !== S.project) return false;
      if (S.iss.kind !== 'all' && f.kind !== S.iss.kind) return false;
      if (S.iss.needs && !fpbNeeds(id).length) return false;
      if (q.attach && f.attach !== (q.attach.indexOf(':') > 0 ? q.attach : 'project:' + q.attach)) return false;
      return true;
    });
    var all = fpbList(), needN = all.filter(function (id) { return fpbNeeds(id).length; }).length;
    var tasks = {}; all.forEach(function (id) { tasks[D.fpbs[id].attach] = 1; });
    var h = '<div class="lpane"><div class="lhead"><h2>Issues</h2><p class="muted">' + plural(all.length, 'issue') + ' on ' + keys(tasks).length + ' tasks and projects · ' + needN + ' waiting on you</p></div>';
    h += '<div class="lctrl"><div class="seg"><button data-go="#/work">Tasks</button><button class="on">Issues</button></div></div>';
    h += '<div class="lctrl"><div class="seg">' + ['all', 'FTR', 'BUG', 'PER'].map(function (k) { return '<button class="' + (S.iss.kind === k ? 'on' : '') + '" data-act="iss-kind" data-k="' + k + '">' + (k === 'all' ? 'All' : k) + '</button>'; }).join('') + '</div><label class="toggle"><input type="checkbox" data-act="iss-needs"' + (S.iss.needs ? ' checked' : '') + '>Needs me</label></div>';
    if (q.attach) h += '<div class="notice soft"><p>Only issues on ' + chip(q.attach.indexOf(':') > 0 ? q.attach : 'project:' + q.attach) + ' · <a href="#/issues">show all</a></p></div>';
    if (!ids.length) return h + empty('No issues match', 'Features, bugs and performance issues raised while building a task appear here, grouped by the task they belong to.') + '</div>';
    var groups = {}, order = [];
    ids.forEach(function (id) { var a = D.fpbs[id].attach; if (!groups[a]) { groups[a] = []; order.push(a); } groups[a].push(id); });
    order.sort(function (a, b) { return (refProject(a) || '').localeCompare(refProject(b) || ''); });
    order.forEach(function (a) {
      var p = projOf(refProject(a));
      h += '<p class="lgroup"><a href="' + hrefFor(a) + '">' + esc(chipInfo(a).title) + '</a>' + (parseRef(a).kind === 'item' ? ' <span class="muted">· ' + esc(p.name) + '</span>' : ' <span class="muted">· no task yet</span>') + '</p><div class="slist">' + groups[a].map(function (id) { return issueRow(id, sel === id); }).join('') + '</div>';
    });
    return h + '</div>';
  }
  function issueRow(id, sel) {
    var f = D.fpbs[id], n = fpbNeeds(id).length, sv = severityOf(id);
    return '<a class="srow irow' + (sel ? ' sel' : '') + '" href="#/fpb/' + id + '"><span class="ik ik-' + f.kind + '">' + ic(f.kind.toLowerCase()) + '</span><span class="sb-main"><span class="st1">' + esc(f.title) + '</span><span class="st2">' + f.kind + ' · ' + esc(f.state) + (sv ? ' · ' + esc(sv.level) : '') + (n ? ' · <span class="tone-warn">' + plural(n, 'thing') + ' for you</span>' : '') + '</span></span></a>';
  }

  /* ================================================================ report page */
  var FPB_TABS = [['report', 'Report'], ['plan', 'Plan'], ['changes', 'Changes'], ['decisions', 'Decisions'], ['activity', 'Activity']];
  var FPB_ALIAS = { overview: ['report'], questions: ['decisions', 'questions'], assumptions: ['decisions', 'assumptions'], found: ['decisions', 'found'], comments: ['changes', 'comments'], runs: ['activity', 'runs'], sessions: ['activity', 'sessions'], history: ['activity', 'history'], rulings: ['report', 'rulings'] };
  function heroFor(id) {
    var f = D.fpbs[id], x = f.sections, t = targetOf(id);
    if (f.kind === 'PER' && x.headline) {
      var tt = t ? 'target ' + (t.short || t.v + ' ' + t.unit) : 'no target yet';
      if (x.headline.single) return [x.headline.single.v + ' ' + x.headline.single.unit, 'one measurement · ' + tt];
      return [x.headline.after.v + ' ' + x.headline.after.unit, 'was ' + x.headline.before.v + ' ' + x.headline.before.unit + ' · ' + tt];
    }
    if (f.kind === 'BUG') {
      var ch = chosenFix(id), fr = x.frequency ? ' · fails ' + x.frequency.count : '';
      if (x.fixes && ch != null) return ['Fix ' + (ch + 1) + ' chosen', trunc(x.fixes.rows[ch].t, 60) + ' · ' + x.fixes.rows[ch].size + ' min'];
      if (x.fixes) return ['Choose a fix', x.fixes.rows.length + ' compared' + fr];
      if (x.cause) return ['Cause ' + x.cause.status, x.cause.confidence + ' confidence' + fr];
      if (x.reproduce) return ['Reproduced', 'cause not found yet' + fr];
      return ['Not reproduced yet', 'reported ' + (f.source.date || '')];
    }
    var acc = x.acceptance ? x.acceptance.lines.filter(function (l) { return l.run; }).length + ' of ' + x.acceptance.lines.length + ' acceptance lines proved' : 'no acceptance lines yet';
    var st = f.steps || [];
    if (st.length) return [st.filter(function (q) { return q.state === 'done'; }).length + ' of ' + st.length + ' steps done', acc];
    return ['Not planned yet', acc];
  }
  function vFpb(id, tab) {
    var f = D.fpbs[id];
    if (!f) return head(null) + '<div class="pane">' + empty('No such issue', 'Issues are features, bugs and performance problems attached to tasks.') + '</div>';
    var openSec = null;
    if (FPB_ALIAS[tab]) { openSec = FPB_ALIAS[tab][1] || null; tab = FPB_ALIAS[tab][0]; }
    tab = tab || 'report';
    var sv = severityOf(id), need = fpbNeeds(id), ub = unblockMin('fpb:' + id);
    var right = need.length ? '<span class="tone-warn">' + plural(need.length, 'thing') + ' for you</span>' : ub ? 'unblocks ~' + fmtMin(ub) : '';
    var lead = f.kind === 'PER' ? '' : esc(fpbLead(id));
    var h = head(crumbRefs(id)) + '<div class="pane report r-' + f.kind + '">';
    h += dhead(f.kind + ' · ' + esc(f.state) + (sv ? ' · ' + esc(sv.level) : ''), 'fk-' + f.kind, right, esc(f.title), lead);
    if (need.length) h += '<div class="need"><p class="need-h">What I need from you</p>' + need.map(function (n) { return n.html; }).join('') + '</div>';
    h += '<nav class="tabs" aria-label="Issue sections">' + FPB_TABS.map(function (x) { return '<a class="tab' + (x[0] === tab ? ' on' : '') + '" href="#/fpb/' + id + '/' + x[0] + '">' + x[1] + '</a>'; }).join('') + '</nav>';
    if (tab === 'report') {
      var hero = heroFor(id);
      h += '<div class="status"><div><p class="bignum">' + esc(hero[0]) + '</p><p class="muted small">' + esc(hero[1]) + '</p></div></div>';
      h += reportSections(id, openSec) + aboutRows(id, openSec);
      var kf = S.folds[f.kind] || {};
      if (keys(kf).some(function (k) { return kf[k]; })) h += '<p class="fold-reset"><button class="btn text small" data-act="fold-reset" data-kind="' + f.kind + '">Close all sections</button><span class="small muted">Open sections are remembered for every ' + KIND_NAME[f.kind].toLowerCase() + '.</span></p>';
    }
    else if (tab === 'plan') h += tabPlan(id);
    else if (tab === 'changes') h += tabChanges(id, openSec);
    else if (tab === 'decisions') h += tabDecisions(id, openSec);
    else h += fpbActivity(id, openSec);
    return h + '</div>';
  }
  var SRC_SHORT = { promoted: 'promoted from a found tray', imported: 'imported from the old register', split: 'split from another issue', duplicate: 'duplicate of another issue', created: 'created by you' };
  function aboutRows(id, openSec) {
    var f = D.fpbs[id], sv = severityOf(id), t = f.time, at = parseRef(f.attach);
    var waits = at.kind === 'item' && !onOpenPath(at.id);
    var rows = [];
    if (sv) rows.push(fold(f.kind, 'severity', 'Severity', esc(sv.level) + (sv.confirmed ? ' · confirmed by you' : ' · proposed by Claude'),
      '<p>' + esc(sv.reason) + '</p><div class="actions" style="margin-top:12px"><button class="btn small" data-act="sev" data-fpb="' + id + '">' + (sv.confirmed ? 'Change' : 'Confirm or change') + '</button></div>', { attn: !sv.confirmed }));
    rows.push(depsPanel(id, openSec));
    rows.push(fold(f.kind, 'time', 'Time', t.forecast ? 'forecast ' + fmtMin(t.forecast) : t.est != null ? 'estimate ' + t.est + ' Claude-min' : 'no estimate yet',
      '<dl class="kv"><dt>Estimate</dt><dd>' + (t.est != null ? t.est + ' Claude-min, ' + esc(t.estBy) : 'not yet') + '</dd><dt>Actual</dt><dd>' + (t.actual ? fmtMin(t.actual) + ' from sessions' : 'no session time yet') + '</dd>' + (t.factor ? '<dt>Factor</dt><dd>×' + t.factor + '</dd>' : '') + (t.forecast ? '<dt>Forecast</dt><dd>' + fmtMin(t.forecast) + '</dd>' : '') + '</dl>'));
    rows.push(fold(f.kind, 'origin', 'Where it came from', esc(SRC_SHORT[f.source.type] || 'created by you') + (f.source.date ? ', ' + esc(f.source.date) : ''),
      '<dl class="kv"><dt>Source</dt><dd class="rich">' + sourceText(f).replace(/^./, function (c) { return c.toUpperCase(); }) + '</dd><dt>Attached to</dt><dd>' + chip(f.attach) + (waits ? ' <span class="muted small">waits for its task to open</span>' : '') + '</dd>' +
      ((f.machine || f.branch || f.lane) ? '<dt>Works on</dt><dd><div class="chips">' + (f.machine ? chip('machine:' + f.machine) : '') + (f.branch ? chip('branch:' + f.branch) : '') + (f.lane ? '<span class="marker">lane ' + esc(f.lane) + '</span>' : '') + '</div></dd>' : '') +
      (unscheduled(f) ? '<dt>Schedule</dt><dd>Unscheduled: ' + esc(projOf(f.project).name) + ' is not open yet.</dd>' : '') + '</dl>'));
    return drills(rows, 'About this issue');
  }
  function fold(kind, key, label, summary, body, opt) {
    opt = opt || {};
    var open = opt.force || foldOpen(kind, key, false);
    return '<details class="drill' + (opt.attn ? ' attn' : '') + (opt.cls ? ' ' + opt.cls : '') + '" id="sec-' + key + '" data-fold="' + kind + ':' + key + '"' + (open ? ' open' : '') + '><summary><span class="dl">' + label + '</span><span class="ds">' + summary + '</span><span class="chev" aria-hidden="true"></span></summary><div class="db">' + body + '</div></details>';
  }

  /* dependencies: needs, blocks (derived), related, duplicates, and what this unblocks */
  function edgeRow(e, other, dir) {
    var conf = edgeConfirmed(e), op = refProject(other), self = null;
    var p = op ? projOf(op) : null;
    var cross = p && dir.project && op !== dir.project;
    return '<div class="edge' + (conf ? '' : ' unconf') + '"><div class="e-main">' + chip(other) + (cross ? '<span class="xproj' + (p.state === 'open' ? '' : ' warn') + '">' + esc(p.name) + (p.state === 'open' ? '' : ' · not open yet') + '</span>' : '') + '</div>' +
      '<p class="e-why">' + esc(e.reason) + '</p><div class="e-tags">' + (e.type === 'needs' ? '<span>' + (e.hard ? 'hard' : 'soft') + '</span>' : '') + '<span>by ' + (e.by === 'you' ? 'you' : 'Claude') + '</span>' + (e.min ? '<span>~' + fmtMin(e.min) + '</span>' : '') +
      (conf ? '<span class="tone-good">confirmed</span>' : '<button class="btn small" data-act="edge" data-edge="' + e.id + '">Confirm or remove</button>') + '</div></div>';
  }
  function depsPanel(id, openSec) {
    var f = D.fpbs[id], ref = 'fpb:' + id;
    var nd = needsOf(ref), bl = blocksOf(ref), rl = relOf(ref, 'related'), du = relOf(ref, 'duplicate');
    var unc = nd.concat(bl).filter(function (e) { return !edgeConfirmed(e); }).length;
    var total = nd.length + bl.length + rl.length + du.length;
    var sum = total ? [nd.length ? 'needs ' + nd.length : '', bl.length ? 'blocks ' + bl.length : '', rl.length ? 'related ' + rl.length : '', du.length ? 'duplicate ' + du.length : ''].filter(Boolean).join(' · ') + (unc ? ' · ' + unc + ' to confirm' : '') : 'none';
    var ctx = { project: f.project };
    function grp(title, list, pick) { return list.length ? '<p class="egroup">' + title + '</p>' + list.map(function (e) { return edgeRow(e, pick(e), ctx); }).join('') : ''; }
    var body = total ? grp('Needs', nd, function (e) { return e.to; }) + grp('Blocks · worked out from what needs this', bl, function (e) { return e.from; }) + grp('Related', rl, function (e) { return e.from === ref ? e.to : e.from; }) + grp('Duplicates', du, function (e) { return e.from === ref ? e.to : e.from; }) : '<p class="muted">No links to other issues or tasks yet.</p>';
    if (bl.length) body += '<p class="egroup">What this unblocks</p><p>' + (unblockMin(ref) ? '<b>~' + fmtMin(unblockMin(ref)) + '</b> of confirmed work' : 'Nothing confirmed yet') + (pendingMin(ref) ? ' <span class="tone-warn">· ~' + fmtMin(pendingMin(ref)) + ' more if you confirm</span>' : '') + '</p>';
    return fold(f.kind, 'deps', 'Dependencies', sum, body, { attn: unc > 0, force: openSec === 'deps' });
  }

  /* sections: fixed order per kind, present only with content, three author styles */
  function foldOpen(kind, key, def) { var k = S.folds[kind]; return k && (key in k) ? k[key] : def; }
  function authorBadge(a, sess, by) {
    if (a === 'me') return '<span class="au au-me">You</span>';
    if (a === 'machine') return '<span class="au au-mc">Machine</span>';
    return '<span class="au au-cl">Claude</span>';
  }
  function wrap(a, body, opt) {
    opt = opt || {};
    if (a === 'me') return '<div class="by-me">' + body + '</div>';
    if (a === 'machine') return '<div class="by-mc">' + (opt.chips && opt.chips.length ? '<div class="mc-h">' + chips(opt.chips, { meta: true }) + '</div>' : '') + body + '</div>';
    return '<div class="by-cl"><div class="cl-h">' + (opt.session ? chip(opt.session) : '<span class="small muted">' + esc(opt.by || 'Claude') + '</span>') + (opt.cap ? '<span class="cap">' + esc(opt.cap) + '</span>' : '') + '</div>' + body + '</div>';
  }
  var AU_WORD = { me: 'you', claude: 'Claude', machine: 'machine' };
  function rsec(kind, key, title, a, summary, body, attn, force) {
    return fold(kind, key, esc(title) + '<span class="au-w au-w-' + a + '">' + AU_WORD[a] + '</span>', summary, body, { attn: attn, force: force, cls: 'sec a-' + a });
  }
  var NEEDS_YOU = { fixes: function (f, id) { return chosenFix(id) == null; }, target: function (f, id) { return !targetOf(id); } };
  function reportSections(id, openSec) {
    var f = D.fpbs[id], out = [];
    D.reportOrder[f.kind].forEach(function (pair) {
      var key = pair[0], sec = f.sections[key];
      if (key === 'plan' && !(f.steps || []).length) return;
      if (key === 'goal') return;
      if (key === 'headline' && f.sections.comparison) return;
      if (!sec && key !== 'impact' && key !== 'plan' && !(key === 'target' && f.kind === 'PER')) return;
      if (key === 'impact' && !sec && !blocksOf('fpb:' + id).length) return;
      var r = SECTIONS[key] ? SECTIONS[key](f, sec || {}, id) : null;
      if (r) out.push(rsec(f.kind, key, pair[1], (sec && sec.author) || r.author || 'claude', r.sum, r.body, NEEDS_YOU[key] && NEEDS_YOU[key](f, id), openSec === key));
    });
    (f.sections.extra || []).forEach(function (x, i) {
      var body = x.lines ? '<pre class="mono-out">' + esc(x.lines.join('\n')) + '</pre>' : '<p>' + esc(x.text || '') + '</p>';
      out.push(rsec(f.kind, 'extra' + i, x.title, x.author || 'claude', esc(trunc((x.lines || [x.text])[0], 48)), wrap(x.author, body, { chips: x.run ? [x.run] : [] }) + '<p class="small muted" style="margin-top:10px">No template for this section yet, so it shows as written.</p>'));
    });
    if (!out.length) return empty('Nothing written yet', 'Sections appear here as they get content; the order never changes.');
    return drills(out);
  }
  function subfold(label, body) { return '<details class="inline"><summary>' + label + '</summary>' + body + '</details>'; }
  function runResult(run) { var x = D.runs[parseRef(run).id]; return x ? (x.result === 'pass' ? 'passed' : x.result === 'warn' ? 'warned' : 'failed') : ''; }
  function codePanelProposed(sy) {
    return '<div class="code proposed"><div class="code-h"><span class="chip nyw"><span class="ic">' + ic('nyw') + '</span><span class="t">' + esc(sy.where) + '</span><span class="m">not yet written</span></span><span class="small muted">proposed ' + esc(sy.lang) + '</span></div><pre><code>' + sy.lines.map(function (l, i) { return '<span class="ln"><span class="n">' + (i + 1) + '</span>' + esc(l) + '</span>'; }).join('') + '</code></pre></div>';
  }
  function accLines(sec) {
    return '<ul class="acc">' + sec.lines.map(function (l) {
      var x = l.run ? D.runs[parseRef(l.run).id] : null, ok = x && x.result === 'pass';
      return '<li class="' + (x ? (ok ? 'ok' : 'bad') : 'none') + '"><span class="tick">' + (x ? (ok ? '✓' : '✗') : '') + '</span><span class="acc-t">' + esc(l.t) + '<span class="acc-c">proved by ' + esc(l.check) + ' ' + (l.run ? chip(l.run, { meta: true }) : l.nyw ? chip(l.nyw) : '<span class="marker">no check yet</span>') + '</span></span></li>';
    }).join('') + '</ul>';
  }
  function fixesBody(f, sec, id) {
    var ch = chosenFix(id);
    return '<div class="fixes">' + sec.rows.map(function (r, i) {
      var chosen = ch === i;
      return '<details class="fxrow' + (chosen ? ' chosen' : '') + '"><summary class="fx"><span class="fx-n">' + (i + 1) + '</span><span class="fx-t">' + esc(r.t) + (chosen ? ' <span class="marker good">chosen</span>' : '') + '</span><span class="fx-m">' + r.size + ' min · <span class="' + (r.risk === 'high' ? 'tone-bad' : r.risk === 'medium' ? 'tone-warn' : 'tone-good') + '">' + esc(r.risk) + ' risk</span></span><span class="chev"></span></summary>' +
        '<div class="fx-b"><dl class="kv"><dt>Confidence</dt><dd>' + esc(r.confidence) + '</dd><dt>Regression check</dt><dd>' + (r.regression === 'exists' ? 'exists' : 'proposed, not written') + '</dd></dl>' +
        subfold('Approach · ' + r.approach.length + ' steps', '<ol class="plain-ol">' + r.approach.map(function (a) { return '<li>' + esc(a) + '</li>'; }).join('') + '</ol><div class="chips" style="margin:10px 0"><span class="small muted">files</span>' + chips(r.files) + '</div>') +
        '<div class="actions" style="margin-top:12px"><button class="btn small' + (chosen ? '' : ' primary') + '" data-act="choose-fix" data-fpb="' + id + '" data-i="' + i + '">' + (chosen ? 'Unchoose' : 'Choose this fix') + '</button></div></div></details>';
    }).join('') + '</div>';
  }
  function causeBody(f, sec) {
    return '<p><span class="marker ' + (sec.status === 'confirmed' ? 'good' : 'warn') + '">' + esc(sec.status) + '</span> <span class="marker ' + (sec.confidence === 'high' ? 'good' : sec.confidence === 'medium' ? 'warn' : '') + '">' + esc(sec.confidence) + ' confidence</span></p><p style="margin-top:8px">' + esc(sec.text) + '</p>' + (sec.code ? subfold('The code · ' + esc(chipInfo(sec.code).title), codeBlock(sec.code)) : '') + (sec.run ? '<p class="small">Confirmed by ' + chip(sec.run, { meta: true }) + '</p>' : '<p class="small"><span class="marker unv">unverified</span> no run confirms it yet</p>');
  }
  function bars(before, after, unit, target) {
    var max = Math.max(before.v, after.v, target || 0), W = 320, x0 = 58, w = W - x0 - 52;
    function bw(v) { return Math.max(4, Math.round(w * v / max)); }
    var tx = target ? x0 + Math.round(w * target / max) : null;
    return '<svg class="per-svg" viewBox="0 0 ' + W + ' 100" role="img" aria-label="Before ' + before.v + ' ' + unit + ', after ' + after.v + ' ' + unit + '"><text x="0" y="27">before</text><rect class="bar-b" x="' + x0 + '" y="12" width="' + bw(before.v) + '" height="20" rx="4"/><text class="v" x="' + (x0 + bw(before.v) + 6) + '" y="27">' + before.v + ' ' + unit + '</text><text x="0" y="67">after</text><rect class="bar-a" x="' + x0 + '" y="52" width="' + bw(after.v) + '" height="20" rx="4"/><text class="v" x="' + (x0 + bw(after.v) + 6) + '" y="67">' + after.v + ' ' + unit + '</text>' + (tx ? '<line class="bud" x1="' + tx + '" y1="4" x2="' + tx + '" y2="82"/><text x="' + (tx + 4) + '" y="96">target ' + target + ' ' + unit + '</text>' : '') + '</svg>';
  }
  function seriesChart(sec) {
    var W = 340, H = 210, L = 40, R = 28, T = 12, B = 38, n = sec.x.length;
    var all = [].concat(sec.before.p50, sec.before.p90, sec.after.p50, sec.after.p90), lo = 0.1, hi = 100;
    function x(i) { return L + i * (W - L - R) / (n - 1); }
    function y(v) { return T + (H - T - B) * (1 - (Math.log10(v) - Math.log10(lo)) / (Math.log10(hi) - Math.log10(lo))); }
    function path(arr) { return arr.map(function (v, i) { return (i ? 'L' : 'M') + x(i).toFixed(1) + ' ' + y(v).toFixed(1); }).join(' '); }
    var g = [0.1, 1, 10, 100].map(function (v) { return '<line class="grid" x1="' + L + '" x2="' + (W - R) + '" y1="' + y(v) + '" y2="' + y(v) + '"/><text class="ax" x="' + (L - 6) + '" y="' + (y(v) + 4) + '" text-anchor="end">' + v + '</text>'; }).join('');
    var xs = sec.x.map(function (t, i) { return '<text class="ax" x="' + x(i) + '" y="' + (H - B + 16) + '" text-anchor="middle">' + t + '</text>'; }).join('');
    var tl = sec.target ? '<line class="bud" x1="' + L + '" x2="' + (W - R) + '" y1="' + y(sec.target) + '" y2="' + y(sec.target) + '"/><text class="ax" x="' + (W - R) + '" y="' + (y(sec.target) + 14) + '" text-anchor="end">target ' + sec.target + ' ' + sec.unit + '</text>' : '';
    var dots = function (arr, cls) { return arr.map(function (v, i) { return '<circle class="' + cls + '" cx="' + x(i) + '" cy="' + y(v) + '" r="3"/>'; }).join(''); };
    return '<svg class="series" viewBox="0 0 ' + W + ' ' + H + '" role="img" aria-label="' + esc(sec.caption) + '">' + g + xs + tl +
      '<path class="s-b90" d="' + path(sec.before.p90) + '"/><path class="s-b50" d="' + path(sec.before.p50) + '"/><path class="s-a90" d="' + path(sec.after.p90) + '"/><path class="s-a50" d="' + path(sec.after.p50) + '"/>' + dots(sec.before.p50, 'd-b') + dots(sec.after.p50, 'd-a') +
      '<text class="ax" x="' + ((L + W - R) / 2) + '" y="' + (H - 4) + '" text-anchor="middle">' + esc(sec.xLabel) + ' · seconds, log scale</text></svg>' +
      '<div class="legend"><span><i class="l-b50"></i>before p50</span><span><i class="l-b90"></i>before p90</span><span><i class="l-a50"></i>after p50</span><span><i class="l-a90"></i>after p90</span></div>';
  }
  var SECTIONS = {
    goal: function (f, s) { return { sum: esc(trunc(s.text, 80)), body: wrap('me', '<p class="lead-p" style="margin:0">' + esc(s.text) + '</p>') }; },
    story: function (f, s) { return { sum: esc(trunc(s.text, 80)), body: wrap('me', '<p>' + esc(s.text) + '</p>') }; },
    scope: function (f, s) { return { sum: s.in.length + ' in · ' + s.out.length + ' out', body: wrap('me', '<div class="two"><div><p class="small muted">In</p>' + plainList(s.in.map(esc)) + '</div><div><p class="small muted">Out</p>' + plainList(s.out.map(esc)) + '</div></div>') }; },
    design: function (f, s) {
      var chosen = (s.options || []).filter(function (o) { return o.chosen; })[0];
      var b = '<p class="sub-h">Surfaces touched</p>' + chips(s.surfaces) +
        (s.syntax ? subfold('Proposed syntax · ' + (s.syntax.lines ? s.syntax.lines.length + ' lines' : 'from a file'), s.syntax.status === 'proposed' ? codePanelProposed(s.syntax) : codeBlock(s.syntax.ref)) : '') +
        (s.options ? '<p class="sub-h">Options considered</p><div class="list">' + s.options.map(function (o, i) { return '<div class="li col' + (o.chosen ? ' chosen-opt' : '') + '"><span class="li-main"><b>' + (i + 1) + '. ' + esc(o.t) + '</b>' + (o.chosen ? ' <span class="marker good">chosen</span> ' + chip(o.ruling) : '') + '</span><span class="li-sub">' + esc(o.why) + '</span></div>'; }).join('') + '</div>' : '') +
        (s.rulings ? '<p class="sub-h">Rulings that apply</p>' + chips(s.rulings) : '') +
        (s.questions ? '<p class="sub-h">Open questions</p>' + chips(s.questions, { meta: true }) : '');
      return { sum: (s.options ? s.options.length + ' options' + (chosen ? ', 1 chosen' : '') : 'surfaces') + (s.questions ? ' · ' + plural(s.questions.length, 'open question') : '') + (s.syntax && s.syntax.status === 'proposed' ? ' · syntax proposed' : ''), body: wrap('claude', b, { session: s.session, cap: 'one line per option' }) };
    },
    acceptance: function (f, s) { var ran = s.lines.filter(function (l) { return l.run; }).length; return { sum: ran + ' of ' + s.lines.length + ' proved by runs', body: wrap('me', accLines(s)) }; },
    parity: function (f, s) {
      var c = {}; s.rows.forEach(function (r) { c[r.status] = (c[r.status] || 0) + 1; });
      var b = '<p class="small muted" style="margin-bottom:8px">Against ' + esc(s.against) + '</p><div class="ptable"><div class="pr ph"><span>What</span><span>Before</span><span>Now</span><span>Status</span></div>' + s.rows.map(function (r) { return '<div class="pr"><span class="p-w">' + esc(r.what) + '</span><code data-l="before">' + esc(r.old) + '</code><code data-l="now">' + esc(r.now) + '</code><span><span class="marker ' + ({ same: 'good', changed: 'warn', dropped: 'bad', pending: '' }[r.status]) + '">' + r.status + '</span>' + (r.ref ? ' ' + chip(r.ref) : '') + '</span></div>'; }).join('') + '</div>';
      return { sum: s.rows.length + ' rows · ' + keys(c).map(function (k) { return c[k] + ' ' + k; }).join(', '), body: wrap('claude', b, { session: s.session }) };
    },
    risks: function (f, s) { return { sum: plural(s.rows.length, 'risk') + ' · ' + s.rows.filter(function (r) { return r.likelihood === 'high'; }).length + ' high', body: wrap('claude', '<div class="list">' + s.rows.map(function (r) { return '<div class="li col"><span class="li-main">' + esc(r.t) + ' <span class="marker ' + (r.likelihood === 'high' ? 'bad' : r.likelihood === 'medium' ? 'warn' : '') + '">' + r.likelihood + '</span></span><span class="li-sub">Limited by: ' + esc(r.limit) + '</span></div>'; }).join('') + '</div>', { session: s.session, cap: 'max three' }) }; },
    impact: function (f, s, id) { var bl = blocksOf('fpb:' + id); return { author: 'machine', sum: (s.affects ? s.affects.length + ' areas' : '') + (bl.length ? ' · unblocks ' + plural(bl.length, 'item') : ''), body: wrap('machine', (s.affects ? '<p class="sub-h" style="margin-top:0">Affects</p>' + plainList(s.affects.map(esc)) : '') + (bl.length ? '<p class="sub-h">Unblocks</p><div class="list">' + bl.map(function (e) { return '<div class="li"><span class="li-main">' + chip(e.from) + '</span><span class="li-meta">~' + fmtMin(e.min) + '</span></div>'; }).join('') + '</div>' : ''), { chips: [] }) }; },
    plan: function (f, s, id) { var done = f.steps.filter(function (x) { return x.state === 'done'; }).length; return { author: 'claude', sum: done + ' of ' + f.steps.length + ' steps done', body: wrap('claude', stepsList(f) + '<p class="small" style="margin-top:10px"><a href="#/fpb/' + id + '/plan">Open the Plan tab</a></p>', { session: s.session, cap: 'max seven steps' }) }; },
    note: function (f, s) { return { sum: esc(trunc(s.text, 70)), body: wrap('claude', '<p>' + esc(s.text) + '</p><p class="small muted" style="margin-top:6px">' + esc(s.date) + '</p>', { session: s.session, by: s.by, cap: 'two lines' }) }; },
    symptom: function (f, s) { return { sum: s.expected ? 'expected: ' + esc(trunc(s.expected, 70)) : esc(trunc(s.text, 80)), body: wrap(s.author, '<p>' + esc(s.text) + '</p><dl class="kv" style="margin-top:12px"><dt>Expected</dt><dd>' + esc(s.expected) + '</dd><dt>Actual</dt><dd>' + esc(s.actual) + '</dd></dl>', { session: s.session, cap: 'max three lines' }) }; },
    reproduce: function (f, s) {
      var x = D.runs[parseRef(s.run).id];
      return { sum: s.steps.length + ' steps · reproduces in run ' + parseRef(s.run).id, body: wrap('claude', '<div class="repro r1"><ol class="steps-n">' + s.steps.map(function (st) { return '<li><span>' + esc(st.t) + '</span>' + (st.expect ? '<span class="exp">expect: ' + esc(st.expect) + '</span>' : '') + '</li>'; }).join('') + '</ol><div class="repro-run"><p class="small muted">Reproduced by</p>' + chip(s.run) + '<p class="small ' + (x.result === 'pass' ? 'tone-good' : 'tone-bad') + '" style="margin-top:4px">' + runResult(s.run) + ' · ' + esc(x.at) + '</p></div></div>', { session: s.session, cap: 'max seven steps' }) };
    },
    evidence: function (f, s) {
      var x = D.runs[parseRef(s.run).id];
      var st = s.stack ? '<details class="inline"><summary>Stack · ' + s.stack.frames.length + ' frames</summary><ol class="frames">' + s.stack.frames.map(function (fr) { return '<li class="' + (fr.first ? 'first' : '') + (fr.own ? '' : ' ext') + '"><code>' + esc(fr.f) + '</code>' + (fr.at ? '<span class="small muted"> ' + esc(fr.at) + '</span>' : '') + '</li>'; }).join('') + '</ol>' + codeBlock(s.stack.code) + '</details>' : '';
      var att = s.attachments ? subfold('Attachments · ' + s.attachments.length, '<div class="thumbs">' + s.attachments.map(function (a) { return '<button class="thumb" data-toast="Opens ' + esc(a.name) + ' full size"><span class="th-i">' + ic(a.kind === 'screenshot' ? 'screens' : 'file') + '</span><span class="th-n">' + esc(a.name) + '</span><span class="th-d">' + esc(a.note) + '</span></button>'; }).join('') + '</div>') : '';
      return { sum: s.lines + ' lines of output' + (s.stack ? ' · stack' : '') + (s.attachments ? ' · ' + plural(s.attachments.length, 'attachment') : ''), body: wrap('machine', '<pre class="mono-out">' + esc(x.out.slice(0, s.lines).join('\n')) + '</pre>' + st + att, { chips: [s.run] }) };
    },
    environment: function (f, s) { return { sum: [s.machine ? parseRef(s.machine).id : null].concat(s.tags.slice(0, 3)).filter(Boolean).join(' · '), body: wrap('machine', '<div class="chips">' + (s.machine ? chip(s.machine, { meta: true }) : '') + (s.commit ? chip(s.commit) : '') + s.tags.map(function (t) { return '<span class="tag">' + esc(t) + '</span>'; }).join('') + '</div>' + (s.method ? '<p class="small" style="margin-top:12px">Method ' + chip(s.method, { meta: true }) + ' · ' + s.iterations + ' iterations</p>' : ''), { chips: [] }) }; },
    frequency: function (f, s) { return { sum: esc(s.level) + ' · ' + esc(s.count), body: wrap('machine', '<p class="bignum" style="font-size:24px">' + esc(s.level) + '</p><p class="small muted">' + esc(s.count) + '</p>') }; },
    cause: function (f, s) { return { sum: esc(s.status) + ' · ' + esc(s.confidence) + ' confidence', body: wrap('claude', causeBody(f, s), { session: s.session, by: s.by, cap: 'max three lines' }) }; },
    fixes: function (f, s, id) { var ch = chosenFix(id); return { sum: s.rows.length + ' compared · ' + (ch != null ? 'fix ' + (ch + 1) + ' chosen' : '<span class="tone-warn">none chosen</span>'), body: wrap('claude', fixesBody(f, s, id), { session: s.session, by: s.by, cap: 'max three fixes' }) }; },
    workaround: function (f, s) { return { sum: esc(trunc(s.text, 70)), body: wrap('claude', '<p>' + esc(s.text) + '</p><p class="small muted" style="margin-top:6px">Applies to: ' + esc(s.who) + '</p>', { session: s.session, cap: 'two lines' }) }; },
    headline: function (f, s) {
      if (s.single) return { sum: s.single.v + ' ' + s.single.unit + ' · one measurement', body: wrap('machine', '<div class="per-nums"><span class="n">' + s.single.v + '<small> ' + s.single.unit + '</small></span></div><p class="small muted">' + esc(s.single.label) + '</p>', { chips: [s.single.run] }) };
      var d = Math.round((s.before.v - s.after.v) * 100) / 100;
      return { sum: s.before.v + ' ' + s.before.unit + ' → ' + s.after.v + ' ' + s.after.unit, body: wrap('machine', '<div class="per-nums"><span class="n from">' + s.before.v + '<small> ' + s.before.unit + '</small></span><span class="arrow-big">→</span><span class="n to">' + s.after.v + '<small> ' + s.after.unit + '</small></span></div><p class="muted">−' + d + ' ' + s.after.unit + ' · ' + Math.round(s.before.v / s.after.v) + '× faster</p>', { chips: [s.before.run, s.after.run] }) };
    },
    target: function (f, s, id) {
      var t = targetOf(id);
      if (!t) return { author: 'me', sum: '<span class="tone-warn">not set</span>', body: wrap('me', '<p>No target yet.</p><button class="btn small primary" data-act="set-target" data-fpb="' + id + '">Set a target</button>') };
      return { author: 'me', sum: esc(trunc(t.text, 70)), body: wrap('me', '<p class="lead-p" style="margin:0 0 6px">' + esc(t.text) + '</p><p class="small muted">' + t.v + ' ' + esc(t.unit) + ' · set by you' + (t.date ? ', ' + esc(t.date) : '') + '</p>' + (t.ruling ? '<p style="margin-top:8px">' + chip(t.ruling) + '</p>' : '') + '<button class="btn small text" style="margin-left:-10px;margin-top:6px" data-act="set-target" data-fpb="' + id + '">Change target</button>') };
    },
    comparison: function (f, s, id) { var hd = f.sections.headline, t = targetOf(id); return { sum: hd.before.v + ' → ' + hd.after.v + ' ' + hd.after.unit + ' · ' + esc(s.cond.join(', ')), body: wrap('machine', bars(hd.before, hd.after, hd.after.unit, t && t.unit === hd.after.unit ? t.v : null) + '<div class="chips">' + chips([hd.before.run, hd.after.run], { meta: true }) + '</div><p class="small muted" style="margin-top:8px">' + esc(s.cond.join(' · ')) + '</p>') }; },
    series: function (f, s) { return { sum: esc(trunc(s.caption, 80)), body: wrap('machine', seriesChart(s) + '<p class="caption">' + esc(s.caption) + '</p>', { chips: s.runs }) }; },
    profile: function (f, s) {
      var max = s.frames[0].total;
      return { sum: s.frames.length + ' frames from ' + esc(s.source), body: wrap('machine', '<div class="prof"><div class="pf ph"><span>Frame</span><span>Self</span><span>Total</span></div>' + s.frames.map(function (fr) { return '<div class="pf"><code>' + esc(fr.f) + '</code><span data-l="self">' + fr.self.toLocaleString('en-GB') + ' ms</span><span data-l="total">' + fr.total.toLocaleString('en-GB') + ' ms<i style="width:' + Math.round(100 * fr.total / max) + '%"></i></span></div>'; }).join('') + '</div>', { chips: [s.run] }) };
    },
    resources: function (f, s) { return { sum: s.rows.map(function (r) { return r.metric.split(' ')[0].toLowerCase() + ' ' + r.delta; }).join(' · '), body: wrap('machine', '<div class="list">' + s.rows.map(function (r) { return '<div class="li"><span class="li-main"><b>' + esc(r.metric) + '</b><br><span class="small muted">' + esc(r.before) + ' → ' + esc(r.after) + '</span></span><span class="li-meta tone-good">' + esc(r.delta) + '</span>' + chip(r.run) + '</div>'; }).join('') + '</div>') }; },
    guard: function (f, s) {
      var margin = Math.round(100 * (s.limit - s.current) / s.limit);
      return { sum: esc(s.direction) + ' ' + s.limit + ' ' + s.unit + ' · ' + margin + '% margin', body: wrap('machine', '<dl class="kv"><dt>Metric</dt><dd>' + esc(s.metric) + '</dd><dt>Limit</dt><dd>' + s.limit + ' ' + s.unit + ', ' + esc(s.direction) + '</dd><dt>Now</dt><dd>' + s.current + ' ' + s.unit + '</dd></dl><div class="margin"><span style="width:' + Math.round(100 * s.current / s.limit) + '%"></span></div><p class="small muted">' + (s.limit - s.current).toFixed(1) + ' ' + s.unit + ' of margin before the check fails</p>', { chips: [s.run] }) };
    }
  };

  /* activity for an issue: runs, sessions, and a history with actors and small diffs */
  function fpbActivity(id, openSec) {
    var f = D.fpbs[id], r = itemRuns([id]), s = f.sessions || [];
    var hist = f.history.slice().reverse().map(function (e) {
      var actor = e[1] === 'you' ? '<span class="au au-me">You</span>' : e[1] === 'seed' ? '<span class="au">Old register</span>' : e[1].indexOf('machine:') === 0 ? '<span class="au au-mc">Machine</span> ' + chip(e[1]) : chip(e[1]);
      return '<div class="li col"><span class="li-main">' + actor + ' <span class="small muted">' + esc(e[0]) + '</span></span><span class="li-sub rich">' + rich(e[2]) + '</span>' + (e[3] ? diffBlock(e[3], { small: true }) : '') + '</div>';
    }).join('');
    return drills([
      drill('History', plural(f.history.length, 'event'), '<div class="list">' + hist + '</div>', { open: openSec === 'history' }),
      drill('Runs', r.length ? plural(r.length, 'run') : 'none', r.length ? runsTable(r) : empty('No runs', 'Runs that measure or reproduce this issue appear here.'), { open: openSec === 'runs' }),
      drill('Sessions', s.length ? plural(s.length, 'session') : 'none', s.length ? '<div class="list">' + s.map(function (x) { return '<div class="li"><span class="li-main">' + chip(x[0], { meta: true }) + '</span><span class="li-meta">' + fmtMin(x[1]) + '</span></div>'; }).join('') + '</div>' : empty('No sessions', 'Sessions that worked on this issue list here with their minutes.'), { open: openSec === 'sessions' }),
      drill('Rulings cited', plural((f.rulings || []).length, 'ruling'), (f.rulings || []).length ? chips(f.rulings) : empty('No rulings', 'Rulings this issue relies on appear here.'))
    ]);
  }

  /* issues per task, on the item page */
  function tabIssues(id) {
    var list = fpbsUnder(id);
    var h = '<div class="actions" style="margin-bottom:20px"><button class="btn small" data-act="add-issue" data-node="' + id + '">' + ic('plus') + 'Add an issue to this task</button><a class="btn small text" href="#/issues">All issues</a></div>';
    if (!list.length) return h + empty('No issues on this task', 'Features, bugs and performance problems raised while building this task are attached here, separate from its plan.');
    return h + '<div class="slist">' + list.map(function (i) { return issueRow(i); }).join('') + '</div>';
  }

  /* ================================================================ header, navigation, drawer */
  var NAV = [['inbox', 'Inbox', '#/inbox'], ['work', 'Work', '#/work'], ['now', 'Now', '#/now'], ['files', 'Files', '#/files'], ['rulings', 'Rulings', '#/rulings']];
  var NAV_WIDE = [['inbox', 'Inbox', '#/inbox'], ['work', 'Work', '#/work'], ['issues', 'Issues', '#/issues'], ['now', 'Now', '#/now'], ['files', 'Files', '#/files'], ['rulings', 'Rulings', '#/rulings']];
  var MORE = [['projects', 'Projects', '#/projects'], ['machines', 'Machines', '#/machines'], ['settings', 'Settings', '#/settings']];
  function waitingSessions() { return keys(D.sessions).filter(function (s) { return D.sessions[s].state === 'waiting'; }).length; }
  function projectSelect() {
    return '<select class="pselect" data-act="set-project" aria-label="Project filter"><option value="all">All projects</option>' + D.projects.map(function (p) { var c = needsCards(p.id).length; return '<option value="' + p.id + '"' + (S.project === p.id ? ' selected' : '') + '>' + esc(p.name) + (c ? ' · ' + c : '') + '</option>'; }).join('') + '</select>';
  }
  function renderHeader() {
    var n = needsCards(S.project).length;
    var lay = [['phone', 'P', 'Phone'], ['rider', 'R', 'Rider'], ['desktop', 'D', 'Desktop']];
    var h = '<div class="hdr-row"><a class="brand" href="#/inbox" aria-label="Qotoba Workbench home"><span class="mark">言</span><b>' + esc(D.meta.programme) + '</b></a>' +
      '<a class="meter" href="#/inbox" title="Needs you cards × your average ' + D.meta.avgDecisionSec + ' s per decision"><i></i>' + n + ' <span class="long">waiting</span> · ' + queueText(n).replace('about ', '~') + '</a>' +
      '<div class="search" id="hsearch"><svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="' + P.search + '"/></svg><input type="search" placeholder="Search" data-act="hsearch" aria-label="Search items, rulings, questions, files, sessions" autocomplete="off"></div>' +
      '<span class="spacer"></span>' + (S.layout === 'desktop' ? projectSelect() : '') +
      '<button class="icon-btn search-btn" data-act="psearch" aria-label="Search">' + ic('search') + '</button>' +
      '<div class="seg review" role="group" aria-label="Layout">' + lay.map(function (l) { return '<button class="' + (S.layout === l[0] ? 'on' : '') + '" data-act="layout" data-v="' + l[0] + '" title="' + l[2] + ' layout"><span class="short">' + l[1] + '</span><span class="full">' + l[2] + '</span></button>'; }).join('') + '</div>' +
      (S.layout === 'desktop' ? '<button class="icon-btn" data-act="drawer" aria-label="Screens (?)" title="Screens · press ?">' + ic('screens') + '</button>' : '<button class="icon-btn" data-act="moremenu" aria-label="Menu: project filter, more pages, screens">' + ic('dots') + '</button>') + '</div>';
    if (S.project !== 'all') h += '<div class="filter-bar">Showing <b>' + esc(projOf(S.project).name) + '</b> only <button class="btn text small" data-act="pfilter" data-p="all">Show all</button></div>';
    document.getElementById('hdr').innerHTML = h;
  }
  function renderNav(section) {
    var n = needsCards(S.project).length, w = waitingSessions();
    var badge = { inbox: n ? '<span class="badge">' + n + '</span>' : '', now: w ? '<span class="ndot" title="' + w + ' sessions waiting on you"></span>' : '' };
    var moreOn = ['projects', 'machines', 'settings', 'more'].indexOf(section) >= 0;
    var issN = fpbList().filter(function (i) { return fpbNeeds(i).length; }).length; badge.issues = issN ? '<span class="ndot" title="' + issN + ' issues need you"></span>' : '';
    document.getElementById('nav').innerHTML = NAV_WIDE.map(function (x) { return '<a class="nv' + (section === x[0] ? ' on' : '') + '" href="' + x[2] + '" title="' + x[1] + '">' + ic(x[0]) + '<span>' + x[1] + '</span>' + (badge[x[0]] || '') + '</a>'; }).join('') +
      '<div class="nv-sep"></div><div class="nv-group">More</div>' + MORE.map(function (x) { return '<a class="nv' + (section === x[0] ? ' on' : '') + '" href="' + x[2] + '" title="' + x[1] + '">' + ic(x[0]) + '<span>' + x[1] + '</span></a>'; }).join('') +
      '<div class="nv-sep"></div><button class="nv" style="border:0;background:none;cursor:pointer;width:auto" data-act="drawer" title="Screens">' + ic('screens') + '<span>Screens <span class="kbd">?</span></span></button>';
    document.getElementById('bottom').innerHTML = NAV.map(function (x) { return '<a class="bn' + (section === x[0] || (x[0] === 'work' && section === 'issues') ? ' on' : '') + '" href="' + x[2] + '">' + ic(x[0]) + x[1] + (badge[x[0]] || '') + '</a>'; }).join('');
    if (moreOn) { /* more-entries are reached from the header on phone */ }
  }
  function drawerHtml() {
    function di(href, label, hint) { return '<a class="di" href="' + href + '" data-act="drawer-go">' + esc(label) + (hint ? '<span class="h">' + esc(hint) + '</span>' : '') + '</a>'; }
    var cards = keys(D.cards);
    var h = '<div class="drawer-scrim" data-act="drawer"></div><aside class="drawer" aria-label="Screens"><div class="drawer-h"><h2>Screens</h2><span class="kbd">?</span><button class="icon-btn" data-act="drawer" aria-label="Close">' + ic('close') + '</button></div><div class="drawer-b">';
    h += '<div class="review"><div class="rl"><span>Layout</span><div class="seg">' + [['phone', 'Phone 390'], ['rider', 'Rider 460'], ['desktop', 'Desktop']].map(function (l) { return '<button class="' + (S.layout === l[0] ? 'on' : '') + '" data-act="layout" data-v="' + l[0] + '">' + l[1] + '</button>'; }).join('') + '</div></div><div class="rl"><span>Theme</span><div class="seg">' + ['system', 'light', 'dark'].map(function (t) { return '<button class="' + (S.theme === t ? 'on' : '') + '" data-act="theme" data-v="' + t + '">' + t + '</button>'; }).join('') + '</div></div></div>';
    h += '<div class="dg">Navigation</div>' + di('#/inbox', '1 · Inbox', 'home') + di('#/work', '7 · Work · outline') + di('#/work/WB-2?view=columns', '7 · Work · Miller columns') + di('#/now', '10 · Now · sessions and machines') + di('#/files', '11 · Files') + di('#/rulings', '12 · Rulings') + di('#/rulings/proposed', '12 · Rulings · proposed from chat') + di('#/projects', '13 · Projects overview') + di('#/machines', '14 · Machines') + di('#/settings', '15 · Settings') + di('#/search?q=finding', '16 · Search results “finding”') + di('#/more', 'More (phone menu)');
    h += '<div class="dg">Issues · reports (FTR, BUG, PER)</div>' + di('#/issues', 'Issues list, grouped by task') + di('#/fpb/FTR-1', 'FTR simple · Lazy card verbs', 'FTR-1') + di('#/fpb/FTR-2', 'FTR complex · class construct in QDL', 'FTR-2') + di('#/fpb/BUG-1', 'BUG simple · same-second hook events', 'BUG-1') + di('#/fpb/BUG-2', 'BUG complex · recovery drops a comment', 'BUG-2') + di('#/fpb/PER-1', 'PER simple · tree query 400 ms', 'PER-1') + di('#/fpb/PER-2', 'PER complex · library opens in 0.6 s', 'PER-2') + di('#/fpb/FTR-2/plan', 'Issue · Plan tab') + di('#/fpb/BUG-2/activity', 'Issue · history with actors and diffs') + di('#/fpb/FTR-2/decisions', 'Issue · deferred questions') + di('#/item/WB-2.3/issues', 'Issues per task · Order hook events') + di('#/issues?attach=dope', 'Issues on a project with no task yet') + di('reports.html', 'Report pattern overview (separate page)');
    h += '<div class="dg">2 · Question cards</div>' + cards.filter(function (c) { return D.cards[c].type === 'question'; }).map(function (c) { var q = D.questions[D.cards[c].question]; return di('#/card/' + c, q.shape === 'design' ? 'Design choice, 3 options' : q.shape === 'comment' ? 'Anchored to a + comment' : 'About an unseen file', c); }).join('');
    h += '<div class="dg">3–5 · Action cards</div>' + di('#/card/pk-runs', '3 · Packet · Runs table (waiting)', 'pk-runs') + di('#/card/pk-hello', '3 · Packet · Hello card (approved)', 'pk-hello') + di('#/card/pk-dope', '3 · Packet · Dope lazy verbs (preview)', 'pk-dope') + di('#/card/pk-runs?changes=1', '3 · Packet · Ask for changes (free text)') + di('#/card/pl-24', '4 · Plan approval · check chips', 'pl-24') + di('#/card/pl-24?edit=1', '4 · Plan approval · Change mode') + di('#/card/st-23', '5 · Stuck · HookOrderingTests', 'st-23');
    h += '<div class="dg">6 · Info cards</div>' + cards.filter(function (c) { return D.cards[c].type === 'info'; }).map(function (c) { return di('#/card/' + c, D.cards[c].headline, c); }).join('');
    h += '<div class="dg">8 · Items</div>' + [['WB-2', 'Task card with real runs · building'], ['WB-1', 'Hello card across two machines · done'], ['WB-2.2', 'Run page with output · task'], ['WB-2.3', 'Order hook events across machines · blocked'], ['WB-2.1', 'Runs table · review'], ['WB-2.4', 'Check chips · planning'], ['WB-4', 'Tree with the one-open rule · coarse'], ['LNG-1', 'Parser and printer · preview']].map(function (x) { return di('#/item/' + x[0], x[1], x[0]); }).join('');
    h += '<div class="dg">8 · Item tabs (WB-2) · old tab names still work</div>' + TABS.map(function (t) { return di('#/item/WB-2/' + t[0], t[1]); }).join('');
    h += '<div class="dg">9 · Info page templates</div>' + di('#/item/WB-2.2', 'Task info', 'WB-2.2') + di('#/item/WB-2/about', 'Slice info', 'WB-2') + di('#/item/WB-2.3/about', 'Task info · hook ordering', 'WB-2.3') + di('#/card/pk-runs', 'Packet info', 'pk-runs') + di('#/ruling/R-02', 'Ruling page', 'R-02') + di('#/comment/c4', 'Comment page · open, became a question', 'c4') + di('#/comment/c2', 'Comment page · stale', 'c2') + di('#/comment/c5', 'Comment page · handled with proof', 'c5') + di('#/session/7f3a', 'Session page · waiting on you', '7f3a') + di('#/session/b915', 'Session page · stuck', 'b915') + di('#/run/1056', 'Run page · failed', 'run-1056') + di('#/run/1042', 'Run page · tests 212', 'run-1042') + di('#/project/wb', 'Project page', 'Workbench') + di('#/machine/laptop', 'Machine page', 'laptop') + di('#/commit/3be07f1', 'Commit page', '3be07f1') + di('#/branch/br-23', 'Branch page', 'wb/2.3') + di('#/type/RunRecord', 'Type page', 'RunRecord') + di('#/claudemd/slots', 'CLAUDE.md section', '§1') + di('#/diff/pk-runs', 'Full diff of a packet') + di('#/file/hookendpoint', 'File view · handled comment with proof') + di('#/file/runrecord', 'File view · stale comment, unseen file');
    h += '<div class="dg">Overlays</div>' + di('#/inbox?pop=ruling:R-03', 'Chip popover · ruling') + di('#/inbox?pop=file:hookeventqueue:27-35', 'Chip popover · file and lines') + di('#/inbox?pop=nyw:RunOutputBlock', 'Chip popover · not yet written') + di('#/inbox?pop=commit:51f9e07', 'Chip popover · proof diff') + di('#/card/q-design?sheet=talk', 'Talk · chat with the brief') + di('#/work?sheet=add', 'Add item · kind and place picker') + di('#/work?menu=WB-4', 'Row actions · Open this node disabled') + di('#/work?menu=WB-2.4', 'Row actions · Open this node enabled');
    h += '<div class="dg">Empty states</div>' + di('#/files', 'Files filtered to Dope (pick Dope in the header)') + di('#/item/WB-4/runs', 'Item with no runs') + di('#/search?q=zebra', 'Search with no results');
    return h + '</div></aside>';
  }
  function toggleDrawer(force) {
    var root = document.getElementById('drawer-root');
    var open = force != null ? force : !root.innerHTML;
    root.innerHTML = open ? drawerHtml() : '';
  }

  /* ================================================================ router */
  function parseHash() {
    var h = location.hash.replace(/^#\/?/, ''), i = h.indexOf('?');
    var p = i >= 0 ? h.slice(0, i) : h, qs = i >= 0 ? h.slice(i + 1) : '';
    var q = {}; qs.split('&').forEach(function (kv) { if (!kv) return; var x = kv.split('='); q[decodeURIComponent(x[0])] = decodeURIComponent((x[1] || '').replace(/\+/g, ' ')); });
    return { parts: p.split('/').filter(Boolean).map(decodeURIComponent), q: q };
  }
  function resolve(parts, q) {
    var a = parts[0] || 'inbox', b = parts[1], c = parts[2];
    function det(section, sel, fn) { return { section: section, sel: sel, detail: fn }; }
    switch (a) {
      case 'inbox': return { section: 'inbox', list: true };
      case 'card': return det('inbox', b, function () { return vCard(b, q); });
      case 'work': if (q.view) S.work.view = q.view; return { section: 'work', wide: true, page: function () { return vWork(b); } };
      case 'item': return det('work', b, function () { return vItem(b, c); });
      case 'issues': return { section: 'issues', list: true };
      case 'fpb': return det('issues', b, function () { return vFpb(b, c); });
      case 'run': return det('work', D.runs[b] && D.runs[b].item, function () { return vRun(b); });
      case 'commit': return det('work', null, function () { return vCommit(b); });
      case 'branch': return det('work', D.branches[b] && D.branches[b].item, function () { return vBranch(b); });
      case 'type': return det('work', D.types[b] && D.types[b].item, function () { return vType(b); });
      case 'claudemd': return det('work', null, function () { return vClaudeMd(b); });
      case 'diff': return det('work', D.packets[b] ? D.packets[b].item : null, function () { return vDiff(b); });
      case 'now': return { section: 'now', list: true };
      case 'session': return det('now', b, function () { return vSession(b); });
      case 'machine': return det('now', null, function () { return vMachine(b); });
      case 'files': return { section: 'files', list: true };
      case 'file': return det('files', b, function () { return vFile(b, q); });
      case 'comment': return det('files', D.comments[b] && D.comments[b].file, function () { return vComment(b); });
      case 'rulings': if (b === 'proposed') { var r = det('rulings', null, vProposed); r.mode = 'proposed'; return r; } return { section: 'rulings', list: true };
      case 'ruling': return det('rulings', b, function () { return vRuling(b); });
      case 'projects': return { section: 'projects', wide: true, page: vProjects };
      case 'project': var pr = det('projects', b, function () { return vProject(b); }); pr.wide = true; return pr;
      case 'machines': return { section: 'machines', wide: true, page: vMachines };
      case 'settings': return { section: 'settings', wide: true, page: vSettings };
      case 'search': return { section: 'search', wide: true, page: function () { return vSearch(q.q || ''); } };
      case 'more': return { section: 'more', wide: true, page: vMore };
    }
    return { section: 'inbox', list: true };
  }
  function listFor(r) {
    switch (r.section) {
      case 'inbox': return vInboxList(r.sel);
      case 'work': return vTreeCompact(r.sel);
      case 'issues': return vIssuesList(r.sel, S.q);
      case 'now': return vNowList(r.sel);
      case 'files': return vFilesList(r.sel);
      case 'rulings': return vRulingsList(r.sel, r.mode);
    }
    return '';
  }
  function defaultFor(r) {
    if (r.section === 'inbox') { var n = by(needsCards(S.project), function (id) { return -D.cards[id].weight; }); if (n.length) { r.sel = n[0]; return function () { return vCard(n[0], {}); }; } return function () { return '<div class="pane">' + empty('Inbox clear', 'Pick a card on the left when one arrives.') + '</div>'; }; }
    if (r.section === 'issues') { var l = fpbList().filter(function (i) { return (S.project === 'all' || D.fpbs[i].project === S.project) && (!S.q.attach || D.fpbs[i].attach === (S.q.attach.indexOf(':') > 0 ? S.q.attach : 'project:' + S.q.attach)); }); r.sel = l[0]; return function () { return l.length ? vFpb(l[0]) : '<div class="pane">' + empty('No issues', 'Pick another project or clear the filter.') + '</div>'; }; }
    if (r.section === 'now') { r.sel = '7f3a'; return function () { return vSession('7f3a'); }; }
    if (r.section === 'files') { r.sel = 'runrecord'; return function () { return vFile('runrecord', {}); }; }
    if (r.section === 'rulings') { r.sel = 'R-01'; return function () { return vRuling('R-01'); }; }
    return function () { return ''; };
  }
  var SECTION_ROOT = { issues: '#/issues', inbox: '#/inbox', work: '#/work', now: '#/now', files: '#/files', rulings: '#/rulings', projects: '#/projects', machines: '#/machines', settings: '#/settings', search: '#/search', more: '#/more' };
  function render(keep) {
    var hp = parseHash(); S.q = hp.q; var r = resolve(hp.parts, hp.q);
    S.route = r;
    var html = document.documentElement;
    html.setAttribute('data-layout', S.layout);
    if (S.theme === 'system') html.removeAttribute('data-theme'); else html.setAttribute('data-theme', S.theme);
    var list = document.getElementById('list'), det = document.getElementById('detail'), shell = document.getElementById('shell');
    var st = keep ? [list.scrollTop, det.scrollTop] : [0, 0];
    var same = S.lastHash === location.hash, wasOpen = same ? [].slice.call(document.querySelectorAll('#list details[open][id], #detail details[open][id]')).map(function (d) { return d.id; }) : [];
    S.lastHash = location.hash;
    renderHeader(); renderNav(r.section);
    shell.classList.toggle('wide', !!r.wide);
    if (S.layout === 'desktop') {
      shell.classList.remove('show-detail');
      if (r.wide) { list.innerHTML = ''; det.innerHTML = (r.page || r.detail)(); }
      else { var d = r.detail || defaultFor(r); list.innerHTML = listFor(r); det.innerHTML = d(); }
    } else {
      if (r.list || r.page) { det.innerHTML = ''; list.innerHTML = r.page ? r.page() : listFor(r); shell.classList.remove('show-detail'); }
      else { list.innerHTML = ''; det.innerHTML = r.detail(); shell.classList.add('show-detail'); }
    }
    wasOpen.forEach(function (i) { var d = document.getElementById(i); if (d) d.open = true; });
    list.scrollTop = st[0]; det.scrollTop = st[1];
    if (!keep) {
      closeLayers();
      if (hp.q.pop) setTimeout(function () { openPop(hp.q.pop, null); }, 0);
      if (hp.q.sheet === 'talk') openTalk(hp.q.ctx || (hp.parts[0] === 'card' ? 'card:' + hp.parts[1] : 'item:WB-2'));
      if (hp.q.sheet === 'add') addItemSheet('WB-2');
      if (hp.q.menu) setTimeout(function () { var el = document.querySelector('[data-act="rowmenu"][data-node="' + hp.q.menu + '"]'); openMenu(el, rowMenu(hp.q.menu)); }, 0);
      if (S.scrollTo) { var el = document.querySelector(S.scrollTo); if (el) el.scrollIntoView(); S.scrollTo = null; }
    }
  }
  function goBack() { if (S.stack.length > 1) history.back(); else location.hash = SECTION_ROOT[S.route.section] || '#/inbox'; }

  /* ================================================================ sheets with content */
  function briefFor(ctx) {
    var r = parseRef(ctx), lines = [];
    if (r.kind === 'card') {
      var c = D.cards[r.id], it = work(c.item);
      lines.push('Brief · talk about a card in Qotoba Workbench');
      lines.push('Item: ' + c.item + ' ' + it.title + ' (' + crumbRefs(c.item).map(function (x) { return chipInfo(x).title; }).join(' › ') + ')');
      lines.push('Card: ' + cardKindLabel(c) + ' · ' + c.headline);
      if (c.type === 'question') { var q = D.questions[c.question]; lines.push('Question ' + c.question + ': ' + q.text); q.options.forEach(function (o, i) { lines.push('  ' + (i + 1) + ') ' + o.t + (o.rec ? ' (recommended)' : '') + ' · ' + o.why); }); if (q.code) lines.push('Code: ' + D.files[parseRef(q.code).id].path + ':' + parseRef(q.code).range.join('-') + ' (pulled, not pasted)'); }
      if (c.type === 'stuck') { var s = D.stucks[r.id]; lines.push('Attempts: ' + s.attempts.map(function (a) { return a.t; }).join(' · ')); }
      if (c.type === 'plan') { lines.push('Steps: ' + D.plans[r.id].steps.map(function (s, i) { return (i + 1) + '. ' + s.t; }).join(' ')); }
      lines.push('Rulings that apply: ' + (it.rulings || []).map(function (x) { return parseRef(x).id + ' ' + D.rulings[parseRef(x).id].title; }).join('; '));
    } else { lines.push('Brief · talk about ' + chipInfo(ctx).title); }
    lines.push('End with proposed rulings, one line each, for me to tick.');
    return lines.join('\n');
  }
  function openTalk(ctx) {
    openSheet('Talk', '<p class="small">' + esc(D.chat.explain) + '</p><div class="chat-box"><textarea disabled aria-label="Brief">' + esc(briefFor(ctx)) + '</textarea><p class="tiny" style="margin:6px 0 0">Prototype: the chat is not live. ' + esc(D.chat.last) + '</p></div><p class="small" style="margin-top:10px">Back from chat: <a href="#/rulings/proposed" data-act="close-after">Rulings › Proposed from chat</a></p>',
      '<button class="btn primary dis" disabled title="Not live in the prototype">Open chat in claude.ai</button><button class="btn" data-act="close">Close</button>');
  }
  function addItemSheet(node) {
    openSheet('Add item', '<p class="small">Only you add items; Claude never creates structure (' + chip('ruling:R-01', { nometa: true }) + ').</p><div class="sec"><div class="sec-h"><h3>Kind</h3></div><div class="seg big" id="kindpick">' + ['FTR', 'BUG', 'PER', 'task'].map(function (k, i) { return '<button class="' + (i === 3 ? 'on' : '') + '" data-act="pickone">' + k + '</button>'; }).join('') + '</div></div>' +
      '<div class="sec"><div class="sec-h"><h3>Title, in your words</h3></div><input type="text" style="width:100%" placeholder="e.g. Runs page keeps its scroll position" aria-label="Title"></div>' +
      '<div class="sec"><div class="sec-h"><h3>Place</h3></div><select style="width:100%" aria-label="Place">' + placeOptions(node) + '</select><p class="tiny" style="margin-top:6px">It arrives closed, as one line with a coarse size, until a plan opens it.</p></div>' +
      '<div class="sec"><div class="sec-h"><h3>Coarse size</h3></div><div class="seg big">' + ['~half a day', '~3 days', '~1 week', '~3 weeks'].map(function (s, i) { return '<button class="' + (i === 1 ? 'on' : '') + '" data-act="pickone">' + s + '</button>'; }).join('') + '</div></div>',
      '<button class="btn primary" data-act="close" data-toast2="Item would be added under ' + esc(nodeTitle(node)) + ', closed, coarse">Add</button><button class="btn" data-act="close">Cancel</button>');
  }

  function refreshMenu() { var m = document.querySelector('#layer .menu'); if (m && m.querySelector('[data-act="wkind"]')) m.innerHTML = filterMenu(); }
  document.addEventListener('click', function (ev) {
    var sm = ev.target.closest && ev.target.closest('summary'); if (!sm) return;
    var d = sm.parentElement; if (!d || !d.hasAttribute('data-fold')) return;
    setTimeout(function () { var kk = d.getAttribute('data-fold').split(':'); S.folds[kk[0]] = S.folds[kk[0]] || {}; if (d.open) S.folds[kk[0]][kk[1]] = true; else delete S.folds[kk[0]][kk[1]]; store('folds2', JSON.stringify(S.folds)); }, 0);
  }, true);
  document.addEventListener('toggle', function (ev) { var t = ev.target; if (t.classList && t.classList.contains('fyk') && t.querySelector('.fyk-row, .fyk-b .empty')) S.fykOpen = t.open; }, true);

  /* ================================================================ events */
  function act(el, ev) {
    var a = el.getAttribute('data-act'), dv = el.dataset;
    switch (a) {
      case 'close': if (dv.toast2) toast(esc(dv.toast2)); closeLayers(); return true;
      case 'close-after': closeLayers(); return false;
      case 'copy': try { navigator.clipboard && navigator.clipboard.writeText(location.href.split('#')[0] + dv.href); } catch (e) { } toast('Link copied · <b>' + esc(dv.href) + '</b>'); return true;
      case 'back': goBack(); return true;
      case 'layout': S.layout = dv.v; store('layout', S.layout); render(true); if (document.getElementById('drawer-root').innerHTML) toggleDrawer(true); return true;
      case 'theme': S.theme = dv.v; store('theme', S.theme); render(true); if (document.getElementById('drawer-root').innerHTML) toggleDrawer(true); return true;
      case 'drawer': toggleDrawer(); return true;
      case 'drawer-go': toggleDrawer(false); return false;
      case 'pfilter': S.project = dv.p; render(true); toast(dv.p === 'all' ? 'Showing all projects' : 'Filtered to <b>' + esc(projOf(dv.p).name) + '</b> everywhere'); return true;
      case 'pfilter-go': if (ev.target.closest('a,[data-ref]')) return false; S.project = dv.p; location.hash = '#/work'; toast('Filtered to <b>' + esc(projOf(dv.p).name) + '</b> everywhere'); return true;
      case 'toggle': S.work.expanded[dv.node] = !S.work.expanded[dv.node]; render(true); return true;
      case 'node':
        if (dv.node === 'root') return true;
        if (S.layout === 'desktop') { S.work.sel = dv.node; location.hash = '#/work/' + dv.node; }
        else location.hash = D.items[dv.node] ? '#/item/' + dv.node : '#/project/' + dv.node;
        return true;
      case 'colsel': S.work.sel = dv.node; location.hash = '#/work/' + dv.node; return true;
      case 'rowmenu': openMenu(el, rowMenu(dv.node)); return true;
      case 'opennode': var co = canOpen(dv.node); if (co.ok) { closeLayers(); location.hash = co.go; } else toast(esc(co.why)); return true;
      case 'additem': addItemSheet(dv.node); return true;
      case 'move': openSheet('Move ' + nodeTitle(dv.node), '<p class="small">Pick the new parent. Moving keeps the item closed; it never moves an open node out of its lane.</p><select style="width:100%" aria-label="New parent">' + placeOptions(nodeParent(dv.node)) + '</select>', '<button class="btn primary" data-act="close" data-toast2="Move would be recorded by you">Move</button><button class="btn" data-act="close">Cancel</button>'); return true;
      case 'rename': openSheet('Rename', '<input type="text" style="width:100%" value="' + esc(nodeTitle(dv.node)) + '" aria-label="Title"><p class="tiny" style="margin-top:6px">Titles are yours; sessions cannot rename.</p>', '<button class="btn primary" data-act="close" data-toast2="Rename would be recorded by you">Rename</button><button class="btn" data-act="close">Cancel</button>'); return true;
      case 'summary': closeLayers(); S.work.sel = dv.node; S.scrollTo = '.split > .side'; location.hash = '#/work/' + dv.node; return true;
      case 'wkind': S.work.kinds[dv.k] = !S.work.kinds[dv.k]; render(true); refreshMenu(); return true;
      case 'wview': S.work.view = dv.v; render(true); return true;
      case 'clear-info': S.cleared = true; S.fykOpen = true; render(true); toast('Info cards cleared · <a href="#" data-act="undo-clear" style="color:inherit"><b>Undo</b></a>'); return true;
      case 'undo-clear': S.cleared = false; S.fykOpen = true; render(true); return true;
      case 'dismiss': S.dismissed[dv.card] = 1; toast('Dismissed'); location.hash = '#/inbox'; return true;
      case 'make-ruling': var c = D.cards[dv.card]; openSheet('Make a ruling', '<p class="small">From the info card “' + esc(c.headline) + '”. One line, in your words.</p><textarea style="width:100%;min-height:70px" aria-label="Ruling">' + esc(c.ruling) + '</textarea>', '<button class="btn primary" data-act="close" data-toast2="Ruling R-13 would be recorded under Workbench">Record ruling</button><button class="btn" data-act="close">Cancel</button>'); return true;
      case 'approve': var pc = D.cards[dv.card]; toast(esc(D.packets[dv.card].toast)); return true;
      case 'show-changes': var bx = document.getElementById('changes-box'); if (bx) { bx.style.display = ''; var inp = bx.querySelector('input'); inp && inp.focus(); } return true;
      case 'pick': var ch = document.querySelector('input[name="opt-' + dv.card + '"]:checked'); var lbl = ch ? ch.closest('.choice').querySelector('.ct').firstChild.textContent : ''; var qq = D.questions[D.cards[dv.card].question]; toast('Picked “' + esc(lbl) + '” · sent to session ' + qq.session + ' on ' + D.sessions[qq.session].machine); return true;
      case 'talk': openTalk(dv.ctx); return true;
      case 'reassign': openMenu(el, '<div class="mi-h">Reassign to another machine</div>' + D.stucks['st-23'].reassign.map(function (m) { return '<button class="mi" data-act="close" data-toast2="WB-2.3 would move to ' + m + ': worktree recreated there, laptop\'s removed (one home machine, R-04)">' + ic('machine') + '<span>' + m + '<span class="why">' + esc(D.machines[m].os) + ' · Host ' + D.machines[m].host + ' · clock ' + esc(D.machines[m].clock) + '</span></span></button>'; }).join('')); return true;
      case 'message': var s = D.sessions[dv.session]; openSheet('Message session ' + dv.session, '<p class="small">One line to ' + chip('session:' + dv.session, { nometa: true }) + ' on ' + chip('machine:' + s.machine, { nometa: true }) + '. It is read at the session\'s next turn and kept in its ledger.</p><input type="text" style="width:100%" placeholder="e.g. Use the recommended option for now" aria-label="Message">', '<button class="btn primary" data-act="close" data-toast2="Message queued for session ' + dv.session + ' on ' + s.machine + '">Send</button><button class="btn" data-act="close">Cancel</button>'); return true;
      case 'beyond': var b = D.packets[dv.pk].beyond[+dv.i]; openSheet('Built beyond the plan', '<p><span class="marker beyond">beyond plan</span> <b>' + esc(b.t) + '</b></p><p>' + esc(b.why) + '</p>' + chip(b.type) + codeBlock(D.types[parseRef(b.type).id].at, { small: true }), '<button class="btn" data-act="close" data-toast2="Accepted as justified">Fine</button><button class="btn" data-act="close" data-toast2="Changes asked: remove the helper and inline it">Ask to remove</button>'); return true;
      case 'settle': el.parentNode.querySelectorAll('button').forEach(function (x) { x.classList.remove('on'); }); el.classList.add('on'); return true;
      case 'pickone': el.parentNode.querySelectorAll('button').forEach(function (x) { x.classList.remove('on'); }); el.classList.add('on'); return true;
      case 'plan-edit': var pp = document.getElementById('plan-pane'); pp.classList.toggle('editing'); el.textContent = pp.classList.contains('editing') ? 'Done changing' : 'Change'; return true;
      case 'fold': var tg = document.getElementById(dv.target); if (tg) tg.hidden = !tg.hidden; return true;
      case 'flip': var row = el.closest('.li'); row.classList.toggle('flipped'); toast(row.classList.contains('flipped') ? 'Flipped: the session will treat the opposite as true' : 'Flip undone'); return true;
      case 'record-ticked': var n = document.querySelectorAll('.proposed input:checked').length; toast(n ? n + ' rulings would be recorded as R-13 to R-' + (12 + n) : 'Nothing ticked'); return true;
      case 'switch': el.classList.toggle('on'); el.setAttribute('aria-checked', el.classList.contains('on')); toast('Setting would be saved'); return true;
      case 'fscope': S.filesScope = dv.v; render(true); return true;
      case 'psearch': openSearchOverlay(); return true;
      case 'moremenu': openMenu(el, '<div class="mi-h">Project</div><div class="menu-sec">' + projectSelect() + '</div><div class="mi-h">More</div>' + MORE.map(function (x) { return '<a class="mi" href="' + x[2] + '" data-act="close-after">' + ic(x[0]) + x[1] + '</a>'; }).join('') + '<button class="mi" data-act="drawer-from-menu">' + ic('screens') + 'Screens <span class="kbd">?</span></button>'); return true;
      case 'wfilter': openMenu(el, filterMenu()); return true;
      case 'go': location.hash = dv.href; return true;
      case 'reply': var rb = document.getElementById('reply-' + dv.card); if (rb) { rb.hidden = false; var ri = rb.querySelector('input'); ri && ri.focus(); } return true;
      case 'send-reply': var inp2 = document.querySelector('#reply-' + dv.card + ' input'); var txt = inp2 ? inp2.value.trim() : ''; toast(txt ? '“' + esc(trunc(txt, 60)) + '” sent to ' + esc(dv.to) : 'Write a line first'); if (txt) { inp2.value = ''; document.getElementById('reply-' + dv.card).hidden = true; } return true;
      case 'iss-kind': S.iss.kind = dv.k; render(true); return true;
      case 'edge': var eg = D.edges.filter(function (x) { return x.id === dv.edge; })[0]; openMenu(el, '<div class="mi-h">' + esc(chipInfo(eg.from).title) + ' ' + (eg.type === 'needs' ? 'needs' : eg.type) + ' ' + esc(chipInfo(eg.to).title) + '</div><p class="small muted" style="padding:0 12px 8px">' + esc(eg.reason) + '</p><button class="mi" data-act="edge-set" data-edge="' + eg.id + '" data-v="confirmed">' + ic('run') + '<span>Confirm<span class="why">' + (eg.min ? '~' + fmtMin(eg.min) + ' of work waits behind it' : 'keeps the link') + '</span></span></button><button class="mi" data-act="edge-set" data-edge="' + eg.id + '" data-v="removed">' + ic('close') + '<span>Remove<span class="why">Claude proposed it; nothing waits on it then</span></span></button>'); return true;
      case 'edge-set': S.edgeState[dv.edge] = dv.v; closeLayers(); render(true); toast(dv.v === 'confirmed' ? 'Dependency confirmed · the unblock weight is updated' : 'Dependency removed'); return true;
      case 'choose-fix': var cur = chosenFix(dv.fpb); S.fix[dv.fpb] = cur === +dv.i ? null : +dv.i; render(true); toast(S.fix[dv.fpb] == null ? 'Fix unchosen · the strip asks you to choose' : 'Fix ' + (+dv.i + 1) + ' chosen'); return true;
      case 'sev': var svv = severityOf(dv.fpb); openMenu(el, '<div class="mi-h">Severity · proposed ' + esc(svv.level) + '</div>' + ['blocking', 'serious', 'minor', 'cosmetic'].map(function (l) { return '<button class="mi" data-act="sev-set" data-fpb="' + dv.fpb + '" data-v="' + l + '"><span>' + l + (l === svv.level ? '<span class="why">' + esc(svv.reason) + '</span>' : '') + '</span></button>'; }).join('')); return true;
      case 'sev-set': S.sev[dv.fpb] = dv.v; closeLayers(); render(true); toast('Severity set to ' + dv.v); return true;
      case 'set-target': var tg = targetOf(dv.fpb) || {}; openSheet('Target', '<p class="small">One line in your words, with the conditions.</p><div class="target-entry"><input type="number" step="any" value="' + (tg.v != null ? tg.v : '') + '" aria-label="Value" id="tg-v"><select id="tg-u" aria-label="Unit">' + ['ms', 's', 'MB', '%', 'items/s'].map(function (u) { return '<option' + (u === tg.unit ? ' selected' : '') + '>' + u + '</option>'; }).join('') + '</select></div><input type="text" style="width:100%;margin-top:10px" id="tg-c" aria-label="Conditions" placeholder="e.g. warm, 40 nodes, median of 5" value="' + esc(tg.text || '') + '">', '<button class="btn primary" data-act="save-target" data-fpb="' + dv.fpb + '">Save target</button><button class="btn" data-act="close">Cancel</button>'); return true;
      case 'save-target': var v = parseFloat(document.getElementById('tg-v').value), u = document.getElementById('tg-u').value, cnd = document.getElementById('tg-c').value.trim(); if (isNaN(v)) { toast('Enter a number'); return true; } S.target[dv.fpb] = { v: v, unit: u, text: cnd || ('Under ' + v + ' ' + u + '.'), by: 'you', date: '6 Nov' }; closeLayers(); render(true); toast('Target set: ' + v + ' ' + u); return true;
      case 'fold-reset': S.folds[dv.kind] = {}; store('folds2', JSON.stringify(S.folds)); [].forEach.call(document.querySelectorAll('#detail details[open]'), function (d) { d.open = false; }); render(false); return true;
      case 'open-sec': var sec = document.getElementById(dv.target); if (sec) { sec.open = true; sec.scrollIntoView({ block: 'start', behavior: 'smooth' }); } return true;
      case 'add-issue': openSheet('Add an issue', '<p class="small">Issues are separate from the plan: they record a feature, bug or performance problem raised while building this task.</p><div class="sec"><div class="sec-h"><h3>Kind</h3></div><div class="seg">' + ['FTR', 'BUG', 'PER'].map(function (k, i) { return '<button class="' + (i === 1 ? 'on' : '') + '" data-act="pickone">' + k + '</button>'; }).join('') + '</div></div><div class="sec"><div class="sec-h"><h3>Title, in your words</h3></div><input type="text" style="width:100%" aria-label="Title" placeholder="e.g. Login button stays disabled after a failed attempt"></div><div class="sec"><div class="sec-h"><h3>Attached to</h3></div><p>' + chip('item:' + dv.node) + '</p></div>', '<button class="btn primary" data-act="close" data-toast2="Issue would be added to ' + esc(itemTitle(dv.node)) + ', state reported">Add</button><button class="btn" data-act="close">Cancel</button>'); return true;
      case 'wclear': S.work.kinds = {}; S.work.state = 'all'; S.work.unsched = false; render(true); return true;
      case 'open-drill': var dd = document.getElementById(dv.target); if (dd) { dd.open = true; dd.scrollIntoView({ block: 'start', behavior: 'smooth' }); } return true;
      case 'drawer-from-menu': closeLayers(); toggleDrawer(true); return true;
      case 'close-search': var ov = document.querySelector('.soverlay'); ov && ov.remove(); return true;
    }
    return false;
  }
  function searchDrop(q) {
    var g = searchAll(q);
    if (!q) return '';
    if (!g.length) return '<div class="sdrop"><div class="sres">Nothing found for “' + esc(q) + '”.</div></div>';
    return '<div class="sdrop">' + g.map(function (x) { return '<h4>' + x.name + ' · ' + x.rows.length + '</h4>' + x.rows.slice(0, 3).map(function (r) { return '<div class="sres">' + chip(r.ref, { max: 44 }) + '<span class="ctx">' + esc(r.ctx) + '</span></div>'; }).join(''); }).join('') + '<a class="all" href="#/search?q=' + encodeURIComponent(q) + '">See all results for “' + esc(q) + '”</a></div>';
  }
  function openSearchOverlay() {
    var ov = document.createElement('div'); ov.className = 'soverlay';
    ov.innerHTML = '<div class="hdr-row"><button class="back" data-act="close-search">' + ic('back') + '</button><div class="search" style="display:block"><svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="' + P.search + '"/></svg><input type="search" placeholder="Search everything" data-act="osearch" aria-label="Search everything" value="finding"></div></div><div class="sres-box">' + searchDrop('finding') + '</div>';
    document.getElementById('shell').appendChild(ov);
    var inp = ov.querySelector('input'); inp.focus(); inp.select();
  }

  document.addEventListener('click', function (ev) {
    var t = ev.target;
    if (!t.closest('#hsearch')) { var dd = document.querySelector('#hsearch .sdrop'); if (dd) dd.remove(); }
    var refEl = t.closest('[data-ref]');
    if (refEl) { ev.preventDefault(); ev.stopPropagation(); var ov = document.querySelector('.soverlay'); if (ov && ov.contains(refEl)) ov.remove(); openPop(refEl.getAttribute('data-ref'), refEl); return; }
    var a = t.closest('[data-act]');
    if (a && a.tagName !== 'INPUT' && a.tagName !== 'SELECT' && a.tagName !== 'TEXTAREA') {
      var handled = act(a, ev);
      if (handled) { if (a.tagName === 'A') ev.preventDefault(); if (a.dataset.toast) toast(esc(a.dataset.toast)); return; }
    }
    var tt = t.closest('[data-toast]');
    if (tt && !tt.disabled) { toast(esc(tt.getAttribute('data-toast'))); return; }
    var g = t.closest('[data-go]');
    if (g) { var ov2 = document.querySelector('.soverlay'); if (ov2) ov2.remove(); location.hash = g.getAttribute('data-go'); }
  });
  document.addEventListener('change', function (ev) {
    var t = ev.target, a = t.getAttribute && t.getAttribute('data-act');
    if (a === 'set-project') { S.project = t.value; render(true); }
    else if (a === 'sort') { S.sort = t.value; render(true); }
    else if (a === 'deferred') { S.showDeferred = t.checked; render(true); }
    else if (a === 'wstate') { S.work.state = t.value; render(true); refreshMenu(); }
    else if (a === 'wunsched') { S.work.unsched = t.checked; if (t.checked) S.work.expanded.dope = 1; render(true); refreshMenu(); }
    else if (a === 'fitem') { S.filesItem = t.value; render(true); }
    else if (a === 'iss-needs') { S.iss.needs = t.checked; render(true); }
    else if (a === 'budget') toast('Budget would change to ' + esc(t.value) + '; sessions get it with their next brief');
  });
  document.addEventListener('input', function (ev) {
    var t = ev.target, a = t.getAttribute && t.getAttribute('data-act');
    if (a === 'hsearch') { var box = document.getElementById('hsearch'); var old = box.querySelector('.sdrop'); if (old) old.remove(); box.insertAdjacentHTML('beforeend', searchDrop(t.value.trim())); }
    else if (a === 'osearch') { t.closest('.soverlay').querySelector('.sres-box').innerHTML = searchDrop(t.value.trim()); }
    else if (a === 'rq') { S.rulingQ = t.value; var pos = t.selectionStart; render(true); var n = document.querySelector('[data-act="rq"]'); if (n) { n.focus(); try { n.setSelectionRange(pos, pos); } catch (e) { } } }
  });
  document.addEventListener('keydown', function (ev) {
    var typing = /INPUT|TEXTAREA|SELECT/.test((ev.target.tagName || ''));
    if (ev.key === 'Enter' && typing && ev.target.getAttribute('data-act') === 'reply-input') { var sb = document.querySelector('#reply-' + ev.target.dataset.card + ' [data-act="send-reply"]'); if (sb) act(sb, ev); return; }
    if (ev.key === 'Enter' && typing) { var a = ev.target.getAttribute('data-act'); if (a === 'hsearch' || a === 'osearch' || a === 'search-page') { var ov = document.querySelector('.soverlay'); if (ov) ov.remove(); location.hash = '#/search?q=' + encodeURIComponent(ev.target.value.trim()); } }
    if (ev.key === 'Escape') { if (document.getElementById('drawer-root').innerHTML) toggleDrawer(false); else if (layer().innerHTML) closeLayers(); else { var ov2 = document.querySelector('.soverlay'); if (ov2) ov2.remove(); var dd = document.querySelector('#hsearch .sdrop'); if (dd) dd.remove(); } }
    if (!typing && ev.key === '?') { ev.preventDefault(); toggleDrawer(); }
  });
  window.addEventListener('hashchange', function () {
    var h = location.hash;
    if (S.stack.length > 1 && S.stack[S.stack.length - 2] === h) S.stack.pop(); else S.stack.push(h);
    var ov = document.querySelector('.soverlay'); if (ov) ov.remove();
    render(false);
  });
  window.addEventListener('resize', function () { closeLayers(); });
  /* long press (or right click) on a tree row opens its actions */
  document.addEventListener('contextmenu', function (ev) {
    var row = ev.target.closest('.tr'); if (!row) return;
    var btn = row.querySelector('[data-act="rowmenu"]'); if (!btn) return;
    ev.preventDefault(); openMenu(btn, rowMenu(btn.getAttribute('data-node')));
  });

  /* expose a tiny hook for automated review */
  window.QWB = { render: render, state: S, resolve: resolve, needs: needsCards };

  if (!location.hash) history.replaceState(null, '', '#/inbox');
  S.stack.push(location.hash);
  render(false);
})();
