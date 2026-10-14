mergeInto(LibraryManager.library, {
  // язык браузера: две буквы
  PlatformLanguage: function () {
    var code = (navigator.language || "en").substring(0, 2).toLowerCase();
    var size = lengthBytesUTF8(code) + 1;
    var buffer = _malloc(size);
    stringToUTF8(code, buffer, size);
    return buffer;
  },

  PlatformVibrate: function (ms) {
    if (navigator.vibrate) navigator.vibrate(ms);
  }
});
