function StatusViewModel(onCancel) {
    "use strict";

    if (!onCancel) {
        onCancel = function () { };
    }
    
    return {
        message: ko.observable(),
        showCancel: ko.observable(false),
        cancel: onCancel
    };
}

function StatusAndResultWrapper(options) {
    "use strict";

    function getMBString(bytes) {
        return (bytes / (1024 * 1024)).toFixed(2).toString();
    }

    var statusVM = new StatusViewModel(options['onCancel']), canCancel = options['canCancel'];
    
    var messageFormatter = options.messageFormatter;
    if (!messageFormatter) {
        messageFormatter = function (status) {
            if (status.IsIndeterminate) {
                return status.Message;
            } else {
                return status.Message + " " + status.CurrentProgress + "/" + status.MaxProgress;
            }
        };
    }

    var updateStatus = function () {
        $.ajax({
            url: options.statusAction,
            cache: false,
            dataType: 'json'
        })
        .done(function (result) {
            if (!result['Exists']) {
                if (options.onComputationDoesNotExist) {
                    options.onComputationDoesNotExist();
                }
                return;
            }
            var showCancel = false;
            if (result.IsRunning) {
                statusVM.message(messageFormatter(result));

                if (canCancel) {
                    showCancel = true;
                }

                setTimeout(updateStatus, 3000);
            } else {
                if (options.failedTemplateName && result.State === 'Failed') {
                    statusVM.message(result.Message);
                    options.mainViewModel.template({ name: options.failedTemplateName, data: statusVM });
                } else if (canCancel && result.State === 'Terminated') {
                    options.mainViewModel.template({ name: options.canceledTemplateName, data: statusVM });
                } else {
                    getResult(result.ResultSize);
                }
                return;
            }

            statusVM.showCancel(showCancel);
        })
        .fail(function () { setTimeout(updateStatus, 3000); });
    };

    var cacheResult = options.cache;
    if (cacheResult === undefined) {
        cacheResult = true;
    }

    var getResult = function (resultSize) {
        statusVM.message("Downloading data...");

        var checkSize = undefined;
        var aborted = false;

        var download = $.ajax({
            url: options.resultAction,
            type: 'GET',
            dataType: 'json',
            cache: cacheResult,
            xhr: function () {
                try {
                    var xhr = new window.XMLHttpRequest();
                    xhr.addEventListener("progress", function (evt) {
                        if (evt.lengthComputable) {
                            if (checkSize(evt.total)) {
                                var percentComplete = 100 * evt.loaded / evt.total;
                                statusVM.message("Downloading data... " + getMBString(evt.loaded) + " MB loaded."); // + "/" + getMBString(evt.total) + "MB (" + percentComplete.toFixed(1).toString() + "%).");
                            }
                        }
                    }, false);

                    xhr.addEventListener("load", function (evt) {
                        statusVM.message("Loading...");
                    }, false);

                    return xhr;
                } catch (ex) {
                    return new window.XMLHttpRequest();
                }
            }
        })
            .done(function (result) { if (!aborted && options.onResult) { options.onResult(result, resultSize); } })
            .fail(function (jqXHR, textStatus, errorThrown) { if (!aborted && options.onError) { options.onError(errorThrown); } });

        var maxDownloadableSize = options.maxDownloadableSize ? options.maxDownloadableSize : 10 * 1024 * 1024 * 1024;
        checkSize = function (size) {
            if (aborted) return false;
            if (size > maxDownloadableSize && download) {
                aborted = true;
                download.abort();
                if (options.onSizeExceeded) options.onSizeExceeded(size, maxDownloadableSize);
                return false;
            }
            return true;
        };
    };
    
    var runFunc = function () {
        options.mainViewModel.template({ name: "busy-template", data: statusVM });

        var spinnerTarget = document.getElementById("busySpinner");
        if (spinnerTarget) {
            new Spinner({ hwaccel: true, radius: 25, length: 15, width: 8, lines: 13 }).spin(spinnerTarget);
        }

        if (options.isFinished) {
            getResult(options.resultSize || "n/a MB");
        } else {
            updateStatus();
        }
    };

    return {
        run: runFunc
    };
}