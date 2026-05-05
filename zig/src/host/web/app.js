(function() {
  var ws = null;
  var logEl = null;
  var connected = false;

  function $(id) { return document.getElementById(id); }

  function showPage(name) {
    ['status', 'settings', 'logs'].forEach(function(p) {
      var el = $('page-' + p);
      if (el) el.style.display = p === name ? 'block' : 'none';
    });
    document.querySelectorAll('aside nav a').forEach(function(a, i) {
      a.classList.toggle('active', ['status','settings','logs'][i] === name);
    });
    if (name === 'settings') loadSettings();
    return false;
  }

  function addLog(level, msg) {
    if (!logEl) logEl = $('log-container');
    if (!logEl) return;
    var d = new Date();
    var ts = [d.getHours(), d.getMinutes(), d.getSeconds()]
      .map(function(v){return (v<10?'0':'')+v;}).join(':');
    var entry = document.createElement('div');
    entry.className = 'log-entry';
    entry.innerHTML = '<span class="ts">' + ts + '</span> <span class="level-' +
      level + '">[' + level + ']</span> ' + msg;
    logEl.appendChild(entry);
    logEl.scrollTop = logEl.scrollHeight;
    while (logEl.children.length > 500) logEl.removeChild(logEl.firstChild);
  }

  function updateStatus(data) {
    var fields = {
      's-state': data.state || '---',
      's-running': data.running ? 'Yes' : 'No',
      's-tick': String(data.tick || 0),
      's-hotspot': data.hotspot_zone || '---',
      's-units': String(data.units_count || 0),
      's-players': String(data.players_count || 0)
    };
    Object.keys(fields).forEach(function(id) {
      var el = $(id);
      if (el) {
        el.textContent = fields[id];
        el.className = 'value';
        if (id === 's-running') el.classList.add(data.running ? 'running' : 'stopped');
      }
    });
  }

  function loadSettings() {
    fetch('/api/settings')
      .then(function(r){ return r.json(); })
      .then(function(data) {
        var grid = $('settings-grid');
        if (!grid) return;
        grid.innerHTML = '';
        var keys = Object.keys(data).sort();
        keys.forEach(function(k) {
          var row = document.createElement('div');
          row.innerHTML = '<div class="key">' + k + '</div><div class="val">' +
            JSON.stringify(data[k]) + '</div>';
          grid.appendChild(row);
        });
      })
      .catch(function(err){ addLog('error', 'Failed to load settings: ' + err); });
  }

  function connectWs() {
    var proto = location.protocol === 'https:' ? 'wss:' : 'ws:';
    ws = new WebSocket(proto + '//' + location.host + '/ws');

    ws.onopen = function() {
      connected = true;
      addLog('info', 'WebSocket connected');
    };

    ws.onmessage = function(evt) {
      try {
        var data = JSON.parse(evt.data);
        updateStatus(data);
      } catch(e) {}
    };

    ws.onclose = function() {
      connected = false;
      addLog('warn', 'WebSocket disconnected, reconnecting in 3s...');
      setTimeout(connectWs, 3000);
    };

    ws.onerror = function() {
      addLog('error', 'WebSocket error');
    };
  }

  function pollStatus() {
    fetch('/api/status')
      .then(function(r){ return r.json(); })
      .then(updateStatus)
      .catch(function(){});
  }

  window.showPage = showPage;

  window.addEventListener('load', function() {
    connectWs();
    setInterval(function() {
      if (!connected) pollStatus();
    }, 2000);
    addLog('info', 'BloogBot monitor initialized');
  });
})();
