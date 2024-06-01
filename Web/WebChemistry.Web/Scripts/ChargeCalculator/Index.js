"use strict";

function ChargeCalculatorUploadModel() {

    var self = this,
        uploadBtn = null,
        uploaded = false,
        maxUploadSizeInBytes = 50 * 1024 * 1024,
        $files = $("#fileToUpload"),
        $uploadBtn = $("#upload-btn");


    function getMBString(bytes) {
        return (bytes / (1024 * 1024)).toFixed(2).toString();
    }

    function setUploadBtnClass(cls) {
        $uploadBtn.removeClass("btn-info btn-danger btn-success btn-warning btn-primary");
        $uploadBtn.addClass(cls);
    }


    this.canUpload = ko.observable(false);
    this.uploadLabel = ko.observable("Select a File to Upload");

    function updateCanUpload() {
        var file = $files[0].files[0];

        if (!file) {
            self.canUpload(false);
            setUploadBtnClass("btn-primary");
            self.uploadLabel("Select a File to Upload");
            return;
        }

        if (file) {
            if (file.size > maxUploadSizeInBytes) {
                self.canUpload(false);
                setUploadBtnClass("btn-danger");
                self.uploadLabel("The file you selected is too big (" + getMBString(file.size) + " MB, max is " + getMBString(maxUploadSizeInBytes) + " MB)");
            } else {
                self.canUpload(true);
                setUploadBtnClass("btn-success");
                self.uploadLabel("Upload");
            }
        }
    }


    this.exampleData = [
        { name: "Paracetamol", title: "<b>Paracetamol</b> <small>[Drug-like]</small>", tooltip: "Drug-like molecule example." },
        { name: "Protegrin", title: "<b>Protegrin-1</b> <small>[Antimicrobial peptide]</small>", tooltip: "Antimicrobial peptide example." },
        { name: "Proteasome", title: "<b>Proteasome</b> <small>[Complex]</small>", tooltip: "Biomacromolecular complex example." }
    ];

    this.exampleDataMap = _.indexBy(this.exampleData, "name");
    

    this.loadingExample = ko.observable(false);
    this.showExamples = ko.observable(false);

    this.toggleExamples = function () {
        self.showExamples(!self.showExamples());
    };

    this.doExample = function (data) {
        self.uploadLabel("Creating example computation...");
        self.canUpload(false);
        self.loadingExample(true);
        $.ajax({
            type: "POST",
            url: ChargeCalculatorActions.exampleAction.replace("-id-", data.name),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (result) {
            if (result["ok"]) {
                self.uploadLabel("Example created. Redirecting...");
                RecentlySubmittedComputations.submit("ChargeCalculator", result.id + "?example=" + data.name, "Example: " + data.name);
                var href = ChargeCalculatorActions.configureExampleAction.replace("-id-", result.id).replace("-example-", data.name);
                window.location.href = href;
                setTimeout(function () {
                    $("#redirectLink").attr("href", href).show();
                    self.uploadLabel("If you are not being redirected, please click the link below.");
                }, 2500);
            } else {
                updateCanUpload();
                setUploadBtnClass("btn-warning");
                self.uploadLabel(result.message);
                self.loadingExample(false);
            }
        }).fail(function (x, y, message) {
            updateCanUpload();
            setUploadBtnClass("btn-warning");
            self.uploadLabel("Failed to create the example. Please try again later.");
            self.loadingExample(false);
        });
    };

    this.upload = function () {
        var fd = new FormData();
        var file = document.getElementById('fileToUpload').files[0];
        fd.append("file", file);

        var hint = file ? file.name : "Unknown filename";

        self.canUpload(false);
        setUploadBtnClass("btn-success");
        self.uploadLabel("Uploading...");
        uploadAjaxFormData(fd, ChargeCalculatorActions.uploadAction, {
            maxUploadSizeInBytes: maxUploadSizeInBytes,
            onComplete: function (response) {
                if (response.ok) {
                    uploaded = true;
                    self.uploadLabel("Done. Redirecting...");
                    var href = ChargeCalculatorActions.configureAction.replace("-id-", response.id);
                    RecentlySubmittedComputations.submit("ChargeCalculator", response.id, hint);
                    window.location.href = href;
                    setTimeout(function () {
                        $("#redirectLink").attr("href", href).show();
                        self.uploadLabel("If you are not being redirected, please click the link below.");
                    }, 2500);
                } else {
                    setUploadBtnClass("btn-warning");
                    self.canUpload(true);
                    self.uploadLabel("Error: " + response["message"]);
                }
            },
            onProgress: function (current, total) {
                if (current !== undefined) {
                    var percentComplete = Math.round(current * 100 / total);
                    self.uploadLabel("Uploading... " + percentComplete.toFixed(2).toString() + "% (" + getMBString(current) + " / " + getMBString(total) + " MB)");
                } else {
                    self.uploadLabel("Uploading...");
                }
            },
            onFailed: function () {
                self.canUpload(true);
                setUploadBtnClass("btn-warning");
                self.uploadLabel("Uploading Error. Please try again later.");
            },
            onAborted: function () {
                self.canUpload(true);
                setUploadBtnClass("btn-warning");
                self.uploadLabel("Upload Aborted");
            }
        });
    };

    $files.change(function () {
        updateCanUpload();
    });

    updateCanUpload();
}

$(function () {
    $("#fileToUpload").replaceWith($("#fileToUpload").clone());

    var viewModel = new ChargeCalculatorUploadModel();
    ko.applyBindings(viewModel, document.getElementById('upload-form'));
    ko.applyBindings(viewModel, document.getElementById('charges-samples'));
});