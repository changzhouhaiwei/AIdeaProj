/* playable-framework: EndCard show/hide contract (UI owned by game or host) */
(function (P) {
  var visible = false;
  var reason = null;
  var handlers = { show: [], hide: [] };

  function on(type, fn) {
    if (!handlers[type] || typeof fn !== 'function') return function () {};
    handlers[type].push(fn);
    return function () {
      var i = handlers[type].indexOf(fn);
      if (i >= 0) handlers[type].splice(i, 1);
    };
  }

  function emit(type, data) {
    var list = handlers[type] || [];
    for (var i = 0; i < list.length; i++) {
      try {
        list[i](data);
      } catch (e) {}
    }
  }

  function showEndCard(r, payload) {
    visible = true;
    reason = r || 'complete';
    emit('show', { reason: reason, payload: payload || {} });
    if (P.track) P.track('endcard_show', { reason: reason });
  }

  function hideEndCard() {
    if (!visible) return;
    visible = false;
    emit('hide', { reason: reason });
    reason = null;
  }

  P.onEndCard = on;
  P.showEndCard = showEndCard;
  P.hideEndCard = hideEndCard;
  P.isEndCardVisible = function () {
    return visible;
  };
  P.getEndCardReason = function () {
    return reason;
  };
})(window.Playable || (window.Playable = {}));
