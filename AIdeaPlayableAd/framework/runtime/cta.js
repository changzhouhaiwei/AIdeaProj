/* playable-framework: unified CTA / openStore */
(function (P) {
  function storeUrl() {
    var c = P.config || {};
    var ios = c.iosStoreUrl || '';
    var and = c.androidStoreUrl || '';
    var ua = navigator.userAgent || '';
    if (/android/i.test(ua)) return and || ios;
    return ios || and;
  }

  function openStore() {
    if (P.track) P.track('cta_click', { state: P.state });
    var url = storeUrl();

    try {
      if (typeof window.install === 'function') {
        window.install();
        return true;
      }
    } catch (e) {}

    try {
      if (window.mraid && typeof window.mraid.open === 'function') {
        window.mraid.open(url || '');
        return true;
      }
    } catch (e) {}

    try {
      if (url) {
        window.open(url, '_blank');
        return true;
      }
    } catch (e) {}

    return false;
  }

  P.openStore = openStore;
  P.getStoreUrl = storeUrl;
})(window.Playable || (window.Playable = {}));
