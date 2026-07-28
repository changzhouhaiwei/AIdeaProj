/* playable-framework: offline gameStart / webviewshow fallback */
(function () {
  var fired = false;
  function fire() {
    if (fired) return;
    fired = true;
    if (window.MUTIL_ONLINE || window.MW_INIT) return;
    try {
      document.dispatchEvent(new Event('webviewshow'));
    } catch (e) {}
  }
  window.addEventListener(
    'luna:started',
    function () {
      setTimeout(fire, 50);
    },
    false
  );
  setTimeout(fire, 5000);
})();
