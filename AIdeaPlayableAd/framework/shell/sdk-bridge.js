/* playable-framework: multi-SDK CTA bridge (Mindworks / Mintegral / Luna / ExitApi) */
(function () {
  if (typeof window.gameStart !== 'function') window.gameStart = function () {};
  if (typeof window.gameClose !== 'function') window.gameClose = function () {};
  if (typeof window.gameEnd !== 'function') window.gameEnd = function () {};

  function _callEnd() {
    try {
      if (typeof window.gameEnd === 'function') window.gameEnd();
    } catch (e) {}
  }
  function _callInstall() {
    try {
      if (typeof window.install === 'function') {
        window.install();
        return true;
      }
    } catch (e) {}
    return false;
  }

  var _origExit = (window.ExitApi && window.ExitApi.exit) || null;
  window.ExitApi = window.ExitApi || {};
  window.ExitApi.exit = function () {
    _callEnd();
    if (_callInstall()) return;
    if (typeof _origExit === 'function')
      try {
        _origExit();
      } catch (e) {}
  };

  function _bridgeSuperHtml() {
    var S = window.super_html;
    if (!S) return;
    var _d = S.download;
    if (typeof _d === 'function' && !_d.__mtg_bridged) {
      S.download = function () {
        _callEnd();
        if (_callInstall()) return;
        if (typeof _d === 'function')
          try {
            return _d.apply(this, arguments);
          } catch (e) {}
      };
      S.download.__mtg_bridged = true;
    }
    var _ge = S.game_end;
    if (typeof _ge === 'function' && !_ge.__mtg_bridged) {
      S.game_end = function () {
        _callEnd();
        if (typeof _ge === 'function')
          try {
            return _ge.apply(this, arguments);
          } catch (e) {}
      };
      S.game_end.__mtg_bridged = true;
    }
  }
  _bridgeSuperHtml();

  var _rbN = 0;
  var _rbIv = setInterval(function () {
    _rbN++;
    _bridgeSuperHtml();
    var x = window.ExitApi && window.ExitApi.exit;
    if (typeof x === 'function' && !x.__mtg_bridged) {
      var ox = x;
      window.ExitApi = window.ExitApi || {};
      window.ExitApi.exit = function () {
        _callEnd();
        if (_callInstall()) return;
        if (typeof ox === 'function')
          try {
            ox.apply(window.ExitApi, arguments);
          } catch (e) {}
      };
      window.ExitApi.exit.__mtg_bridged = true;
    }
    var Mr = window.mraid;
    if (Mr && typeof Mr.open === 'function' && !Mr.open.__mtg_bridged) {
      var mo = Mr.open;
      Mr.open = function (u) {
        _callEnd();
        if (_callInstall()) return;
        if (typeof mo === 'function') return mo.call(Mr, u);
      };
      Mr.open.__mtg_bridged = true;
    }
    if (typeof window.open === 'function' && !window.open.__mtg_bridged) {
      var wn = window.open;
      window.open = function (u, n, f) {
        if (typeof u === 'string' && u && !/^(javascript|data|blob):/i.test(u)) {
          _callEnd();
          if (_callInstall()) return;
        }
        return wn.call(window, u, n, f);
      };
      window.open.__mtg_bridged = true;
    }
    if (_rbN >= 100) clearInterval(_rbIv);
  }, 100);

  function _wrapFn(obj, name, handler) {
    if (!obj) return false;
    var cur = obj[name];
    if (cur && cur.__mtg_bridged) return true;
    obj[name] = handler(cur);
    obj[name].__mtg_bridged = true;
    return true;
  }
  function _bridgeLunaUnity() {
    var L = window.Luna && window.Luna.Unity;
    var ok = false;
    if (L && L.Playable) {
      ok =
        _wrapFn(L.Playable, 'InstallFullGame', function (orig) {
          return function () {
            _callEnd();
            if (_callInstall()) return;
            if (typeof orig === 'function')
              try {
                return orig.apply(this, arguments);
              } catch (e) {}
            try {
              return window.ExitApi && window.ExitApi.exit && window.ExitApi.exit();
            } catch (e) {}
          };
        }) || ok;
    }
    if (L && L.LifeCycle) {
      ok =
        _wrapFn(L.LifeCycle, 'GameEnded', function (orig) {
          return function () {
            _callEnd();
            if (typeof orig === 'function')
              try {
                return orig.apply(this, arguments);
              } catch (e) {}
          };
        }) || ok;
    }
    return ok;
  }
  _bridgeLunaUnity();
  window.addEventListener &&
    window.addEventListener('luna:ended', function () {
      _callEnd();
    });
  var _lunaRetry = 0;
  var _lunaTid = setInterval(function () {
    _lunaRetry++;
    _bridgeLunaUnity();
    if (_lunaRetry > 100) clearInterval(_lunaTid);
  }, 100);

  var _grTries = 0;
  var _grMax = 60;
  var _grIv = setInterval(function () {
    _grTries++;
    try {
      if (typeof window.gameReady === 'function') window.gameReady();
    } catch (e) {}
    if (_grTries >= _grMax) clearInterval(_grIv);
  }, 500);

  var M = window.mraid;
  if (M && typeof M.open === 'function' && !M.open.__mtg_bridged) {
    var _mo = M.open;
    M.open = function (u) {
      _callEnd();
      if (_callInstall()) return;
      if (typeof _mo === 'function') return _mo.call(M, u);
    };
    M.open.__mtg_bridged = true;
  }
  var _wo = window.open;
  window.open = function (u, n, f) {
    if (typeof u === 'string' && u && !/^(javascript|data|blob):/i.test(u)) {
      _callEnd();
      if (_callInstall()) return;
    }
    return _wo.call(window, u, n, f);
  };
  window.open.__mtg_bridged = true;

  function _fallbackStoreUrl() {
    try {
      var f = window.__force_store_redirect__;
      if (typeof f === 'string' && f) return f;
      if (typeof window.__playable_store_target__ === 'function') {
        var t = window.__playable_store_target__();
        if (t) return t;
      }
      var c = (window.Playable && window.Playable.config) || window.__PLAYABLE_CONFIG__ || {};
      var ios = c.iosStoreUrl || '';
      var and = c.androidStoreUrl || '';
      var ua = navigator.userAgent || '';
      if (/android/i.test(ua)) return and || ios || '';
      return ios || and || '';
    } catch (e) {}
    return '';
  }

  if (typeof window.install !== 'function') {
    window.install = function () {
      _callEnd();
      try {
        var u = _fallbackStoreUrl();
        if (u) _wo.call(window, u, '_blank');
        else _wo.call(window, '__playable_install__://store', '_blank');
      } catch (e) {}
    };
    window.install.__mtg_bridged = true;
  }
})();
