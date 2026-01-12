mergeInto(LibraryManager.library, {
  ShareImage: function(base64Ptr, filenamePtr) {
    var base64 = UTF8ToString(base64Ptr);
    var filename = UTF8ToString(filenamePtr);

    function base64ToBlob(base64Data, contentType) {
      contentType = contentType || '';
      var sliceSize = 1024;
      var byteCharacters = atob(base64Data);
      var bytesLength = byteCharacters.length;
      var slices = Math.ceil(bytesLength / sliceSize);
      var byteArrays = new Array(slices);

      for (var offset = 0; offset < slices; ++offset) {
        var start = offset * sliceSize;
        var end = Math.min(start + sliceSize, bytesLength);
        var size = end - start;
        var byteNumbers = new Array(size);
        for (var i = 0; i < size; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(start + i);
        }
        byteArrays[offset] = new Uint8Array(byteNumbers);
      }

      return new Blob(byteArrays, { type: contentType });
    }

    try {
      var blob = base64ToBlob(base64, 'image/png');
      var file = new File([blob], filename, { type: 'image/png' });

      // Try Web Share API (files) - works in modern browsers/contexts (HTTPS + user gesture)
      if (navigator.canShare && navigator.canShare({ files: [file] })) {
        navigator.share({
          files: [file],
          title: 'My screenshot',
          text: 'Check out my screenshot'
        }).catch(function (err) {
          console.log('Share failed:', err);
        });
      } else {
        // Fallback: open image in new tab so user can save / share manually
        var url = URL.createObjectURL(blob);
        var w = window.open(url, '_blank');
        if (!w) {
          // Popup blocked: trigger a download instead
          var a = document.createElement('a');
          a.href = url;
          a.download = filename;
          document.body.appendChild(a);
          a.click();
          a.remove();
        }
        // Clean up object URL later
        setTimeout(function () {
          URL.revokeObjectURL(url);
        }, 10000);
      }
    } catch (e) {
      console.log('ShareImage exception', e);
    }
  }
});