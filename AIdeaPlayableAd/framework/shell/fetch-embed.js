/* playable-framework: optional Cocos/Luna fetch/XHR embed trap (no-op without __PLAYABLE_FETCH_EMBED__) */
(function () {
  function k(u) {
    if (typeof u !== 'string') {
      try {
        u = String((u && u.href) || u);
      } catch (e) {
        return null;
      }
    }
    var i = u.indexOf('cocos-js/');
    return i < 0 ? null : u.slice(i).split(/[?#]/)[0];
  }
  function lk(s) {
    var M = window.__PLAYABLE_FETCH_EMBED__;
    if (!M) return null;
    if (M[s]) return s;
    for (var a in M) if (s === a || s.slice(-a.length) === a) return a;
    var b = s.split('/').pop();
    if (b) for (var a in M) if (a.split('/').pop() === b) return a;
    return null;
  }
  function buf(b64) {
    var t = atob(b64);
    if (t.indexOf('data:') === 0) {
      var b = t.indexOf(';base64,');
      if (b >= 0) {
        var s = t.slice(b + 8),
          bin = atob(s),
          n = bin.length,
          u = new Uint8Array(n);
        for (var i = 0; i < n; i++) u[i] = bin.charCodeAt(i);
        return u.buffer;
      }
    }
    var n = t.length,
      u = new Uint8Array(n);
    for (var i = 0; i < n; i++) u[i] = t.charCodeAt(i);
    return u.buffer;
  }
  function rsp(b64, ek) {
    var ct = 'application/octet-stream';
    if (/\.wasm$/i.test(ek)) ct = 'application/wasm';
    return new Response(buf(b64), { status: 200, headers: { 'Content-Type': ct } });
  }
  function urlOf(inp) {
    if (typeof inp === 'string') return inp;
    if (inp && typeof Request !== 'undefined' && inp instanceof Request) return inp.url;
    if (inp && inp.url) return inp.url;
    return null;
  }
  function hit(u) {
    if (typeof u !== 'string') return null;
    var sk = k(u),
      ek = sk && lk(sk);
    return ek ? { ek: ek, b64: window.__PLAYABLE_FETCH_EMBED__[ek] } : null;
  }
  function rememberUrl(x, u) {
    if (typeof u !== 'string') return;
    x.__pu = u;
    try {
      x._url = u;
    } catch (e) {}
    try {
      x._requestURL = u;
    } catch (e) {}
  }
  function urlOfXHR(x) {
    var u = x.__pu || x._url || x._requestURL;
    if (typeof u === 'string') return u;
    try {
      if (typeof x.responseURL === 'string' && x.responseURL) return x.responseURL;
    } catch (e) {}
    return null;
  }
  function deliverXHR(x, h) {
    var ab = buf(h.b64),
      url = urlOfXHR(x) || '';
    function done() {
      try {
        x.readyState = 4;
        x.status = 200;
        x.response = ab;
        x.responseText = '';
        x.responseURL = url;
      } catch (e) {}
      if (x.onreadystatechange)
        try {
          x.onreadystatechange();
        } catch (e) {}
      if (x.onload)
        try {
          x.onload();
        } catch (e) {}
      if (x.onloadend)
        try {
          x.onloadend();
        } catch (e) {}
    }
    setTimeout(done, 0);
  }
  function wrapInstance(x) {
    if (x.__playableInst) return;
    x.__playableInst = 1;
    var oo = x.open,
      os = x.send;
    x.open = function (m, u) {
      if (arguments.length >= 2) rememberUrl(x, u);
      else if (typeof m === 'string') rememberUrl(x, m);
      return oo.apply(x, arguments);
    };
    x.send = function (b) {
      var u = urlOfXHR(x);
      if (!u) rememberUrl(x, '');
      else rememberUrl(x, u);
      var h = u && hit(u);
      if (h) {
        deliverXHR(x, h);
        return;
      }
      return os.apply(x, arguments);
    };
  }
  function wrapFetch(inner) {
    return function (inp, init) {
      var u = urlOf(inp),
        h = u && hit(u);
      if (h) return Promise.resolve(rsp(h.b64, h.ek));
      return inner.apply(this, arguments);
    };
  }
  function patchXHR() {
    var X = window['XMLHttp' + 'Request'];
    if (!X || !X.prototype) return;
    var oo = X.prototype.open,
      os = X.prototype.send;
    if (typeof os !== 'function' || os.__playableOuter === 2) return;
    function openWrap(m, u) {
      if (arguments.length >= 2) rememberUrl(this, u);
      else if (typeof m === 'string') rememberUrl(this, m);
      return oo.apply(this, arguments);
    }
    function sendWrap(b) {
      var u = urlOfXHR(this);
      if (!u) rememberUrl(this, '');
      else rememberUrl(this, u);
      var h = u && hit(u);
      if (h) {
        deliverXHR(this, h);
        return;
      }
      return os.apply(this, arguments);
    }
    openWrap.__playableOuter = 1;
    sendWrap.__playableOuter = 2;
    X.prototype.open = openWrap;
    X.prototype.send = sendWrap;
    if (!X.__playableCtorHook) {
      X.__playableCtorHook = 1;
      var Native = X;
      function Hooked() {
        var x = new Native();
        wrapInstance(x);
        return x;
      }
      Hooked.prototype = Native.prototype;
      window['XMLHttp' + 'Request'] = Hooked;
    }
  }
  function install() {
    if (!window.__PLAYABLE_FETCH_EMBED__) return;
    patchXHR();
    if (typeof fetch !== 'function') return;
    var inner = fetch.bind(window),
      w = wrapFetch(inner);
    w.__playableWrap = 1;
    try {
      if (!window.__PLAYABLE_FETCH_TRAP__) {
        window.__PLAYABLE_FETCH_TRAP__ = 1;
        Object.defineProperty(window, 'fetch', {
          get: function () {
            return w;
          },
          set: function (fn) {
            inner = typeof fn === 'function' ? fn.bind(window) : fn;
            w = wrapFetch(inner);
            w.__playableWrap = 1;
          },
          configurable: true
        });
      } else {
        window.fetch = w;
      }
    } catch (e) {
      window.fetch = w;
    }
  }
  function hookGameReady() {
    if (window.__PLAYABLE_GR_HOOK__) return;
    window.__PLAYABLE_GR_HOOK__ = 1;
    var L = console.log;
    console.log = function () {
      try {
        if (arguments.length && String(arguments[0]).indexOf('game ready') >= 0) {
          install();
          setTimeout(install, 0);
          setTimeout(install, 100);
          setTimeout(install, 500);
        }
      } catch (e) {}
      return L.apply(console, arguments);
    };
  }
  install();
  hookGameReady();
  setInterval(install, 50);
  [15000, 30000, 60000, 120000].forEach(function (ms) {
    setTimeout(install, ms);
  });
})();
