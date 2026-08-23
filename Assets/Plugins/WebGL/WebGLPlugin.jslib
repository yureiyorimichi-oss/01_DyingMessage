mergeInto(LibraryManager.library, {
  openShareModal: function (base64Ptr) {
    var base64Str = UTF8ToString(base64Ptr);
    if (typeof window.openShareModal === 'function') {
      window.openShareModal(base64Str);
    }
  }
});