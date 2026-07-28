/* playable-framework: platform shims (WindVane / MRAID preview / b64 img / query utils) */
(function () {
  var W = (window.WindVane = window.WindVane || {});
  W.call = function (svc, name, params, succ) {
    try {
      if (typeof succ === 'function') succ({});
    } catch (e) {}
  };
  W.callMethod = function () {};
  W.useIframe = function () {};
})();

(function () {
  if (window.mraid) return;
  window.mraid = {
    open: function (u) {
      try {
        window.open(u, '_blank');
      } catch (e) {}
    },
    addEventListener: function () {},
    removeEventListener: function () {},
    getState: function () {
      return 'default';
    },
    getVersion: function () {
      return '2.0';
    },
    isViewable: function () {
      return true;
    },
    useCustomClose: function () {},
    close: function () {}
  };
})();

(function () {
  var BLANK =
    'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';
  function normalize(u) {
    if (typeof u !== 'string' || u.indexOf('data:') !== 0) return u;
    var b = u.indexOf(';base64,');
    if (b < 0) return u;
    var m = u.slice(5, b);
    var s = u.slice(b + 8);
    var i = s.length;
    while (i > 0 && s.charCodeAt(i - 1) === 61) i--;
    var d = s.slice(0, i);
    while (d.length % 4 === 1) d = d.slice(0, -1);
    var p = (4 - (d.length % 4)) % 4;
    return 'data:' + m + ';base64,' + d + (p ? new Array(p + 1).join('=') : '');
  }
  var FILE = location.protocol === 'file:';
  var C = typeof Map === 'function' ? new Map() : null;
  function blobize(u) {
    if (!FILE || typeof u !== 'string' || u.indexOf('data:image/') !== 0) return u;
    if (C && C.has(u)) return C.get(u);
    var b = u.indexOf(';base64,');
    if (b < 0) return u;
    try {
      var mime = u.slice(5, b).split(';')[0];
      var s = u.slice(b + 8);
      var bin = atob(s);
      var L = bin.length;
      var a = new Uint8Array(L);
      for (var i = 0; i < L; i++) a[i] = bin.charCodeAt(i);
      var bu = URL.createObjectURL(new Blob([a], { type: mime }));
      if (C) C.set(u, bu);
      return bu;
    } catch (e) {
      return u;
    }
  }
  function fix(v) {
    if (typeof v !== 'string') return v;
    if (v === '') return BLANK;
    return blobize(normalize(v));
  }
  try {
    var P = HTMLImageElement.prototype;
    var D = Object.getOwnPropertyDescriptor(P, 'src');
    if (D && D.set) {
      Object.defineProperty(P, 'src', {
        set: function (v) {
          D.set.call(this, fix(v));
        },
        get: D.get,
        configurable: true,
        enumerable: true
      });
    }
    var O = Element.prototype.setAttribute;
    Element.prototype.setAttribute = function (a, v) {
      if (this.tagName === 'IMG' && a && String(a).toLowerCase() === 'src' && typeof v === 'string') {
        v = fix(v);
      }
      return O.call(this, a, v);
    };
  } catch (e) {}
})();

(function () {
  if (typeof window.getError !== 'function') {
    window.getError = function (c, s) {
      return 'Error ' + c + ': ' + s;
    };
  }
  function q(k) {
    try {
      var p = new URLSearchParams(location.search);
      var v = p.get(k);
      return v == null ? '' : v;
    } catch (e) {
      return '';
    }
  }
  var U = window.mv_utils || {};
  U.getQueryString = U.getQueryString || q;
  window.mv_utils = U;
  var M = window.m_util || {};
  M.getQueryString = M.getQueryString || q;
  window.m_util = M;
})();
