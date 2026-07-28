/* playable-framework: LOADING | PLAY | ENDCARD (+ MRAID viewable gate) */
(function (P) {
  var STATES = { LOADING: 'LOADING', PLAY: 'PLAY', ENDCARD: 'ENDCARD' };
  var state = STATES.LOADING;
  var assetsReady = false;
  var viewable = false;
  var ended = false;
  var playWaiters = [];
  var listeners = {};

  function cfg() {
    return P.config || window.__PLAYABLE_CONFIG__ || {};
  }

  function emit(name, data) {
    var list = listeners[name] || [];
    for (var i = 0; i < list.length; i++) {
      try {
        list[i](data);
      } catch (e) {}
    }
  }

  function on(name, fn) {
    if (typeof fn !== 'function') return function () {};
    if (!listeners[name]) listeners[name] = [];
    listeners[name].push(fn);
    return function () {
      var i = listeners[name].indexOf(fn);
      if (i >= 0) listeners[name].splice(i, 1);
    };
  }

  function setState(next) {
    if (state === next) return;
    var prev = state;
    state = next;
    P.state = state;
    if (P.track) P.track('state_change', { from: prev, to: next });
    emit('state', { from: prev, to: next });
    emit(next.toLowerCase(), { from: prev });
  }

  function tryEnterPlay() {
    if (ended || state !== STATES.LOADING) return;
    if (!assetsReady) return;
    var needViewable = cfg().waitForViewable !== false;
    if (needViewable && !viewable) return;
    setState(STATES.PLAY);
    if (P.track) P.track('playable_start');
    var q = playWaiters.slice();
    playWaiters.length = 0;
    for (var i = 0; i < q.length; i++) {
      try {
        q[i]();
      } catch (e) {}
    }
    emit('play', {});
  }

  function ready() {
    assetsReady = true;
    if (P.track) P.track('assets_ready');
    tryEnterPlay();
  }

  function end(reason) {
    if (ended) return;
    ended = true;
    reason = reason || 'complete';
    setState(STATES.ENDCARD);
    if (typeof P.showEndCard === 'function') P.showEndCard(reason);
    if (P.track) P.track('playable_end', { reason: reason });
    // SDK gameEnd is fired on CTA via sdk-bridge / openStore, not here.
    emit('end', { reason: reason });
  }

  function whenPlayable(fn) {
    if (typeof fn !== 'function') return;
    if (state === STATES.PLAY) fn();
    else playWaiters.push(fn);
  }

  function setViewable(v) {
    viewable = !!v;
    if (viewable) tryEnterPlay();
    emit('viewable', { viewable: viewable });
  }

  function bindMraid() {
    var M = window.mraid;
    if (!M) {
      setViewable(true);
      return;
    }
    try {
      if (typeof M.isViewable === 'function' && M.isViewable()) setViewable(true);
      else if (typeof M.getState === 'function' && M.getState() === 'default') setViewable(true);
    } catch (e) {
      setViewable(true);
    }
    try {
      if (typeof M.addEventListener === 'function') {
        M.addEventListener('viewableChange', function (v) {
          setViewable(!!v);
          if (!v) emit('pause', {});
          else emit('resume', {});
        });
      }
    } catch (e) {}
    // Preview shim / missing events: fail-open after short delay
    setTimeout(function () {
      if (!viewable) setViewable(true);
    }, 800);
  }

  P.STATES = STATES;
  P.state = state;
  P.ready = ready;
  P.end = end;
  P.whenPlayable = whenPlayable;
  P.on = on;
  P.setViewable = setViewable;
  P.isPlayable = function () {
    return state === STATES.PLAY;
  };

  // Host callbacks for games that want explicit hooks
  P.on('viewable', function (d) {
    if (typeof P.onViewable === 'function' && d.viewable) P.onViewable();
  });
  P.on('pause', function () {
    if (typeof P.onPause === 'function') P.onPause();
  });
  P.on('resume', function () {
    if (typeof P.onResume === 'function') P.onResume();
  });

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bindMraid);
  } else {
    bindMraid();
  }
})(window.Playable || (window.Playable = {}));
