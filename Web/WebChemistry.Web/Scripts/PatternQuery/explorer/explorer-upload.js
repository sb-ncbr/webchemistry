function mqeAbortUpload() {
    mqExplorerModel.upload.abortUpload();
}

function PatternQueryExplorerUploadViewModel(vm) {
    "use strict";
    
    var self = this,
        uploadXhr = null,
        maxUploadSizeInBytes = 10 * 1024 * 1024; // 10 MB

    function doUpload(formData) {
        var status = vm.status, log = vm.log;
        var abortBtn = "<button class='btn btn-inverse' onclick='javascript:mqeAbortUpload()' style='margin-top: 20px; font-weight: bold'>Abort</button>";

        status.setBusy(true);
        status.message('Uploading... 0.0%');
        
        uploadXhr = uploadAjaxFormData(formData, PatternQueryExplorerActions.uploadFilesAction, {
            maxUploadSizeInBytes: 10 * 1024 * 1024, // 10MB
            onComplete: function (response) {
                status.setBusy(false);
                uploadXhr = null;
                if (response.error) {
                    log.error('Failed to upload the structures. Please try again later.');
                    return;
                }
                vm.structures.set(response.AllStructures);
                log.addStructures(response);
            },
            onProgress: function (current, total) {
                uploadXhr = null;
                status.message('Uploading... ' +  (100 * current / total).toFixed(1) + '%<br/>' + abortBtn);
            },
            onFailed: function () {
                uploadXhr = null;
                status.setBusy(false);
                log.error("Failed to upload structures.");
            },
            onAborted: function () {
                uploadXhr = null;
                status.setBusy(false);
                log.message("Upload aborted.");
            }
        });
    }

    var supportedTypes = _.map(PatternQueryExplorerActions.supportedFilenames, function (t) {
        return new RegExp('\\' + t + '$', 'i');
    });

    $('#fileUploadInput').change(function (e) {
        var files = e.target.files;
        if (files.length === 0) return;

        var form = new FormData(),
            size = 0,
            isOk = true;
        _.forEach(files, function (f, i) {
            size += f.size;
            if (!_.some(supportedTypes, function (t) { return t.test(f.name); })) {
                vm.log.message("'" + f.name + "' is not a supported file type.");
                isOk = false;
                return false;
            }
            form.append("file" + i, f);
        });

        if (size > maxUploadSizeInBytes) {
            vm.log.message('Selected file(s) exceed the maximum upload size (' + (maxUploadSizeInBytes / 1024 / 1024) + ' MB).');
            return;
        }

        if (isOk) doUpload(form);
    });

    this.uploadFiles = function () {
        $('#fileUploadInput').click();
    };

    this.abortUpload = function () {
        if (uploadXhr !== null) {
            try {
                uploadXhr.abort();
            } catch (e) { }
        }
    };
}