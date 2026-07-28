/* tileAd2 scaffold — validates Playable framework contract without real gameplay. */
(function () {
  var ended = false;
  var statusEl;

  function paint(msg) {
    if (!statusEl) return;
    statusEl.textContent = msg;
  }

  function buildUi() {
    var root = document.getElementById('game-container');
    root.innerHTML = '';
    root.style.cssText =
      'display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px;color:#fff;font-family:Arial,sans-serif;';

    var title = document.createElement('div');
    title.textContent = (Playable.config && Playable.config.title) || 'tileAd2';
    title.style.fontSize = '22px';
    root.appendChild(title);

    statusEl = document.createElement('div');
    statusEl.style.opacity = '0.85';
    root.appendChild(statusEl);

    var playBtn = document.createElement('button');
    playBtn.textContent = 'Simulate Clear';
    playBtn.style.cssText =
      'padding:12px 20px;font-size:16px;border:0;border-radius:8px;background:#2ecc71;color:#fff;cursor:pointer;';
    playBtn.onclick = function () {
      if (ended) return;
      ended = true;
      paint('EndCard: complete');
      endcard.style.display = 'flex';
      Playable.end('complete');
    };
    root.appendChild(playBtn);

    var endcard = document.createElement('div');
    endcard.style.cssText =
      'display:none;position:fixed;inset:0;background:rgba(0,0,0,0.65);align-items:center;justify-content:center;flex-direction:column;gap:12px;';
    var cta = document.createElement('button');
    cta.textContent = 'Install';
    cta.style.cssText =
      'padding:14px 28px;font-size:18px;border:0;border-radius:8px;background:#3498db;color:#fff;cursor:pointer;';
    cta.onclick = function () {
      Playable.openStore();
    };
    endcard.appendChild(cta);
    document.body.appendChild(endcard);

    var timeoutMs = (Playable.config && Playable.config.timeoutMs) || 30000;
    setTimeout(function () {
      if (ended) return;
      ended = true;
      paint('EndCard: timeout');
      endcard.style.display = 'flex';
      Playable.end('timeout');
    }, timeoutMs);

    paint('PLAY — state=' + Playable.state);
    Playable.track && Playable.track('tileAd2_scaffold_play');
  }

  Playable.whenPlayable(function () {
    buildUi();
  });
  Playable.ready();
})();
