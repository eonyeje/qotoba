/* Qotoba Workbench · boot.
   Fetches the snapshot the service renders (the same shape the prototype's data.js had), installs it as window.WB,
   loads app.js, and refreshes on every change the service announces. Presses go to /api/press. */
(function () {
  'use strict';
  var loaded = false, pending = false, es = null;

  function toast(msg) { if (window.WB_TOAST) window.WB_TOAST(msg); else console.log(msg); }
  function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }

  function fail(title, text) {
    document.getElementById('detail').innerHTML = '<div class="pane" style="padding:40px 24px;max-width:560px"><h1 style="font-family:var(--display);font-size:22px;margin:0 0 12px">' + esc(title) + '</h1><p>' + esc(text) + '</p></div>';
  }

  function api(method, url, body) {
    return fetch(url, { method: method, headers: body ? { 'Content-Type': 'application/json' } : {}, body: body ? JSON.stringify(body) : undefined, credentials: 'same-origin' })
      .then(function (r) {
        if (r.status === 401) { throw new Error('The Workbench token is missing. Open the address the service printed; it carries the token once.'); }
        return r.json().then(function (j) { if (!r.ok && !(j && j.message)) throw new Error('The service answered ' + r.status); return j; });
      });
  }

  function refresh() {
    if (pending) return; pending = true;
    api('GET', '/api/snapshot').then(function (wb) {
      pending = false;
      window.WB = wb;
      if (!loaded) { loaded = true; var s = document.createElement('script'); s.src = 'app.js?v=' + (window.QWB_STAMP || Date.now()); document.body.appendChild(s); }
      else if (window.WB_RERENDER) window.WB_RERENDER();
    }).catch(function (e) { pending = false; if (!loaded) fail('Cannot reach the Workbench', e.message); else toast(esc(e.message)); });
  }

  function listen() {
    if (es) return;
    try {
      es = new EventSource('/api/events');
      var timer = null;
      es.addEventListener('change', function () { clearTimeout(timer); timer = setTimeout(refresh, 150); });
      es.onerror = function () { es.close(); es = null; setTimeout(listen, 3000); };
    } catch (e) { setInterval(refresh, 5000); }
  }

  window.WBAPI = {
    refresh: refresh,
    press: function (card, action, extra) {
      extra = extra || {};
      return api('POST', '/api/press', { card: card, action: action, value: extra.value, text: extra.text, target: extra.target, index: extra.index })
        .then(function (r) { toast(esc(r.message)); if (r.ok && r.navigate) location.hash = r.navigate; refresh(); return r; })
        .catch(function (e) { toast(esc(e.message)); });
    },
    addItem: function (body) {
      return api('POST', '/api/items', body).then(function (r) { if (r.message) { toast(esc(r.message)); return r; } toast('Added ' + esc(r.id) + ', closed, coarse'); refresh(); return r; }).catch(function (e) { toast(esc(e.message)); });
    },
    patchItem: function (id, body) {
      return api('PATCH', '/api/items/' + encodeURIComponent(id), body).then(function (r) { if (r.message) toast(esc(r.message)); if (r.navigate) location.hash = r.navigate; refresh(); return r; }).catch(function (e) { toast(esc(e.message)); });
    },
    pageAnswer: function (card, key, value) {
      return api('POST', '/api/cards/' + encodeURIComponent(card) + '/answers', { key: key, value: value }).then(function (r) { if (!r.ok) toast(esc(r.message)); return r; }).catch(function (e) { toast(esc(e.message)); });
    },
    message: function (session, text) {
      return api('POST', '/api/sessions/' + encodeURIComponent(session) + '/message', { text: text }).then(function () { toast('Queued for session ' + esc(session) + '; it is read at the next stop'); refresh(); }).catch(function (e) { toast(esc(e.message)); });
    },
    ruling: function (body) {
      return api('POST', '/api/rulings', body).then(function (r) { if (r.message) { toast(esc(r.message)); return r; } toast('Recorded as ' + esc(r.id)); refresh(); location.hash = '#/ruling/' + r.id; return r; }).catch(function (e) { toast(esc(e.message)); });
    },
    seen: function (file, type) {
      return api('POST', '/api/seen', { file: file, type: type }).then(function () { toast('Marked as seen'); refresh(); }).catch(function (e) { toast(esc(e.message)); });
    },
    report: function (id, patch) {
      return api('PATCH', '/api/reports/' + encodeURIComponent(id), patch).then(function (r) { toast(r.message ? esc(r.message) : 'Recorded by you'); refresh(); return r; }).catch(function (e) { toast(esc(e.message)); });
    },
    edge: function (id, confirmed) {
      return api('PATCH', '/api/edges/' + encodeURIComponent(id), { confirmed: confirmed }).then(function () { toast(confirmed ? 'Dependency confirmed · the unblock weight is updated' : 'Dependency removed'); refresh(); }).catch(function (e) { toast(esc(e.message)); });
    },
    addEdge: function (from, to) {
      return api('POST', '/api/edges', { from: from, to: to }).then(function (r) { toast(r.message ? esc(r.message) : 'Dependency recorded by you'); refresh(); }).catch(function (e) { toast(esc(e.message)); });
    },
    open: function (machine, path, line) {
      // inside Rider the pane opens the file itself; elsewhere the Host on that machine does
      if (window.QOTOBA_RIDER && window.QOTOBA_RIDER.open) { window.QOTOBA_RIDER.open(path, line); toast('Opening ' + esc(path.split(/[\\/]/).pop()) + ':' + line); return Promise.resolve(); }
      return api('POST', '/api/open', { machine: machine, file: path, line: line }).then(function (r) { toast(r.ok ? 'Opening ' + esc(path.split(/[\\/]/).pop()) + ':' + line + ' in Rider on ' + esc(machine) : 'The Host on ' + esc(machine) + ' could not open Rider'); }).catch(function (e) { toast(esc(e.message)); });
    }
  };

  // an embedding host (the Rider pane) says so with ?embed=1: the shell fills the pane and the layout follows its width,
  // unless the host names a fixed layout with ?layout=phone|rider|desktop
  var embedded = false, fixedLayout = null;
  try {
    var qp = new URLSearchParams(location.search), lay = qp.get('layout');
    embedded = qp.get('embed') === '1';
    if (lay && /^(phone|rider|desktop)$/.test(lay)) { fixedLayout = lay; localStorage.setItem('qwb.layout', lay); }
    if (embedded) document.documentElement.setAttribute('data-embed', '1');
  } catch (e) { }
  function layoutForWidth(w) { return w < 600 ? 'phone' : w < 1000 ? 'rider' : 'desktop'; }
  function applyAutoLayout(first) {
    if (!embedded || fixedLayout) return;
    var want = layoutForWidth(window.innerWidth), have = null;
    try { have = localStorage.getItem('qwb.layout'); } catch (e) { }
    if (want === have) return;
    try { localStorage.setItem('qwb.layout', want); } catch (e) { }
    if (!first && window.WB_RERENDER) window.WB_RERENDER();
  }
  applyAutoLayout(true);
  var resizeTimer = null;
  window.addEventListener('resize', function () { clearTimeout(resizeTimer); resizeTimer = setTimeout(function () { applyAutoLayout(false); }, 200); });

  // a page card's frame posts the owner's answers; the Workbench keeps them with the card
  window.addEventListener('message', function (ev) {
    var d = ev.data; if (!d || d.type !== 'wb-answer' || !window.WB_PAGE_CARD) return;
    window.WBAPI.pageAnswer(window.WB_PAGE_CARD, String(d.key || ''), String(d.value == null ? '' : d.value));
  });

  refresh();
  listen();
})();
