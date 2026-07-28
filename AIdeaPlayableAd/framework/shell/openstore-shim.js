/* playable-framework: Unity Ads mraid.openStoreURL shim */
(function () {
  var M = window.mraid;
  if (M && !M.__unityOpenStoreShim) {
    M.__unityOpenStoreShim = true;
    if (!M.openStoreURL) {
      M.openStoreURL = function (u) {
        return M.open(u || '');
      };
    }
  }
})();
