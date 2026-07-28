/* playable-framework: analytics hooks (no-op by default; override Playable.onTrack) */
(function (P) {
  var events = [];

  function track(name, payload) {
    var row = { name: name, payload: payload || {}, t: Date.now() };
    events.push(row);
    try {
      if (typeof P.onTrack === 'function') P.onTrack(name, row.payload);
    } catch (e) {}
    try {
      if (typeof window.__PLAYABLE_ANALYTICS__ === 'function') {
        window.__PLAYABLE_ANALYTICS__(name, row.payload);
      }
    } catch (e) {}
  }

  P.track = track;
  P.getAnalyticsLog = function () {
    return events.slice();
  };
})(window.Playable || (window.Playable = {}));
