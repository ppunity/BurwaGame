mergeInto(LibraryManager.library, {
  EnableRequestCredentials: function () {
    // Patch fetch
    if (typeof window.fetch === 'function') {
      const origFetch = window.fetch;
      window.fetch = function(resource, init = {}) {
        init.credentials = 'include'; // send cookies
        return origFetch(resource, init);
      };
    }

    // Patch XMLHttpRequest
    if (typeof XMLHttpRequest !== 'undefined') {
      const origOpen = XMLHttpRequest.prototype.open;
      XMLHttpRequest.prototype.open = function() {
        this.withCredentials = true; // send cookies
        origOpen.apply(this, arguments);
      };
    }
  }
});