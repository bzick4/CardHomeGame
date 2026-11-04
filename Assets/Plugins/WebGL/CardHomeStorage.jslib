// Сохранения игры в браузере. Unity в WebGL хранит PlayerPrefs в хранилище, привязанном к адресу сборки,
// а itch.io при каждой новой загрузке меняет адрес — и сохранения пропадали.
// localStorage привязан к домену (а не к сборке), поэтому переживает обновления игры на itch.
mergeInto(LibraryManager.library, {
  CardHome_StorageGet: function (keyPtr) {
    var value = null;
    try { value = window.localStorage.getItem(UTF8ToString(keyPtr)); } catch (e) { }
    if (value === null) return 0;
    var size = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(size);
    stringToUTF8(value, buffer, size);
    return buffer;
  },

  CardHome_StorageSet: function (keyPtr, valuePtr) {
    try { window.localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valuePtr)); } catch (e) { }
  },

  CardHome_StorageDelete: function (keyPtr) {
    try { window.localStorage.removeItem(UTF8ToString(keyPtr)); } catch (e) { }
  },

  // Удалить все ключи игры (по префиксу) — для «сбросить прогресс» и «новой игры».
  CardHome_StorageClear: function (prefixPtr) {
    try {
      var prefix = UTF8ToString(prefixPtr);
      var keys = [];
      for (var i = 0; i < window.localStorage.length; i++) {
        var k = window.localStorage.key(i);
        if (k && k.indexOf(prefix) === 0) keys.push(k);
      }
      keys.forEach(function (k) { window.localStorage.removeItem(k); });
    } catch (e) { }
  },
});
