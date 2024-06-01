function uploadAjaxFormData(formData, actionUrl, options) {
    "use strict";

    options = options || {};

    var maxUploadSizeInBytes = options.maxUploadSizeInBytes || 50 * 1024 * 1024;

    var onProgress = options['onProgress'] || function () { };

    function uploadFile() {
        var xhr = new XMLHttpRequest();
        xhr.upload.addEventListener("progress", uploadProgress, false);
        xhr.addEventListener("load", uploadComplete, false);
        xhr.addEventListener("error", uploadFailed, false);
        xhr.addEventListener("abort", uploadCancelled, false);
        xhr.open("POST", actionUrl);
        xhr.send(formData);
        return xhr;
    }
    
    function uploadProgress(evt) {
        if (evt.lengthComputable) {
            onProgress(evt.loaded, evt.total);
        }
        else {
            onProgress();
        }
    }

    function uploadComplete(evt) {
        var response = JSON.parse(evt.target.responseText);
        if (options['onComplete'] !== undefined) options.onComplete(response);
    }

    function uploadFailed(evt) {
        if (options['onFailed'] !== undefined) options.onFailed();
    }

    function uploadCancelled(evt) {
        if (options['onAborted'] !== undefined) options.onAborted();
    }

    return uploadFile();
}