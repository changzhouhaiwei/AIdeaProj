/* playable-framework: store URL redirect (reads Playable.config / __PLAYABLE_CONFIG__) */
(function () {
  function cfg() {
    return (window.Playable && window.Playable.config) || window.__PLAYABLE_CONFIG__ || {};
  }
  function target() {
    var c = cfg();
    var ios = c.iosStoreUrl || c.appstore_url || '';
    var and = c.androidStoreUrl || c.google_play_url || '';
    var ua = navigator.userAgent || '';
    if (/android/i.test(ua)) return and || ios;
    return ios || and;
  }
  window.__playable_tool_redirect__ = true;
  window.__playable_store_target__ = target;

  var S = window.super_html;
  if (typeof S === 'object' && S !== null) {
    var t0 = target();
    var c = cfg();
    if (c.iosStoreUrl) S.appstore_url = c.iosStoreUrl;
    if (c.androidStoreUrl) S.google_play_url = c.androidStoreUrl;
    if (!S.appstore_url && t0) S.appstore_url = t0;
    if (!S.google_play_url && t0) S.google_play_url = t0;
  }

  try {
    var _open = window.open;
    window.open = function (u, n, f) {
      if (typeof u === 'string' && u && !/^(javascript|data|blob):/i.test(u)) {
        var t = target();
        if (t) u = t;
      }
      return _open.call(window, u, n, f);
    };
  } catch (e) {}

  try {
    if (window.mraid) {
      var _mo = window.mraid.open;
      window.mraid.open = function (u) {
        u = target() || u;
        if (typeof _mo === 'function') return _mo.call(window.mraid, u);
        try {
          window.open(u, '_blank');
        } catch (e) {}
      };
    }
  } catch (e) {}

  try {
    var _la = window.location.assign;
    if (typeof _la === 'function') {
      window.location.assign = function (u) {
        if (typeof u === 'string' && /\.apple\.com|play\.google\.com|market:\/\//i.test(u)) {
          var t = target();
          if (t) u = t;
        }
        return _la.call(window.location, u);
      };
    }
  } catch (e) {}
})();
