// Вибрация в браузере (Vibration API). Есть в Chrome на Android; в Safari на iPhone её нет —
// там вызов просто ничего не делает. Ошибки глушим: вибрация — украшение, игра от неё не зависит.
mergeInto(LibraryManager.library, {
  CardHome_Vibrate: function (ms) {
    try { if (navigator.vibrate) navigator.vibrate(ms); } catch (e) { }
  },

  CardHome_VibratePattern: function (a, b, c) {
    try { if (navigator.vibrate) navigator.vibrate([a, b, c]); } catch (e) { }
  }
});
