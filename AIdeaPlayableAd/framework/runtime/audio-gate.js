/* playable-framework: optional audio unlock / mute gate */
(function (P) {
  var unlocked = false;
  var muted = false;
  var waiters = [];

  function unlock() {
    if (unlocked) return;
    unlocked = true;
    for (var i = 0; i < waiters.length; i++) {
      try {
        waiters[i]();
      } catch (e) {}
    }
    waiters.length = 0;
    if (P.track) P.track('audio_unlocked');
  }

  function onFirstPointer() {
    unlock();
    document.removeEventListener('pointerdown', onFirstPointer, true);
    document.removeEventListener('touchstart', onFirstPointer, true);
  }

  document.addEventListener('pointerdown', onFirstPointer, true);
  document.addEventListener('touchstart', onFirstPointer, true);

  P.audio = {
    isUnlocked: function () {
      return unlocked;
    },
    isMuted: function () {
      return muted;
    },
    setMuted: function (v) {
      muted = !!v;
      if (P.track) P.track('audio_mute', { muted: muted });
    },
    whenUnlocked: function (fn) {
      if (typeof fn !== 'function') return;
      if (unlocked) fn();
      else waiters.push(fn);
    }
  };
})(window.Playable || (window.Playable = {}));
