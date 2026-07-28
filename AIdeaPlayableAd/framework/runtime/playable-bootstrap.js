/* playable-framework: merge config + expose Playable namespace */
(function () {
  var injected = window.__PLAYABLE_CONFIG__ || {};
  var P = (window.Playable = window.Playable || {});
  P.config = Object.assign(
    {
      title: 'Playable',
      iosStoreUrl: '',
      androidStoreUrl: '',
      timeoutMs: 30000,
      waitForViewable: true,
      maxSizeBytes: 5 * 1024 * 1024,
      endcard: {
        closable: false
      }
    },
    injected,
    P.config || {}
  );
  window.__PLAYABLE_CONFIG__ = P.config;
  P.version = '1.0.0';
})();
