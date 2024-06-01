function handleFileInputChange(input) {
    var fileSelected = !!(input.val());
    var warn = input.data("warning");
    var disable = input.data("disable");

    if (warn) {
        if (fileSelected) {
            $("#" + warn).html("<small>The user is responsible for the correctness of uploaded model residue(s). To validate custom model residues against wwPDB CCD, use the Motif/Fragment Validation mode.</small>").show();
        } else {
            $("#" + warn).html("").hide();
        }
    }

    if (disable) {
        if (fileSelected) {
            $("#" + disable + " > input:text").attr("disabled", "disabled");
        } else {
            $("#" + disable + " > input:text").removeAttr("disabled", "disabled");
        }
    }
}

$(function () {
    ko.applyBindings({}, document.getElementById('recently-submitted'));

    $("#residueListExampleBtn").click(function (e) {
        e.preventDefault();
        $("#customModelNames").val("MAN,FUC,GAL");
    });

    $("#residueExampleBtn").click(function (e) {
        e.preventDefault();
        $("#customModelName").val("MAN");
    });

    $("#residueValidationPdbListExampleBtn").click(function (e) {
        e.preventDefault();
        $("#residueValidationPdbNames").val("1AX1,1LGC,2J0G");
    });

    $("#sugarValidationPdbListExampleBtn").click(function (e) {
        e.preventDefault();
        $("#sugarValidationPdbNames").val("1AX1,1LGC,2J0G");
    });

    $("#samplesBtn").click(function () {
        $("#samplesTable").toggle();
    });

    $("#cmdVersionLink").click(function (e) {
        e.preventDefault();
        $("#cmdTabLink").click();
    });

    $("#customModelNames").keyup(function () {
        var val = $(this).val().toString();
        var elem = $("#notifyMultipleLigands");
        if (val.length === 0) {
            elem.hide();
        } else {
            elem.show();
        }
    });

    $("input:file")
        .change(function () { handleFileInputChange($(this)); })
        .each(function () {
            //firefox fix
            var disable = $(this).data("disable");
            if (disable) {
                $("#" + disable + " > input:text").removeAttr("disabled", "disabled");
            }
        });

    function disableForm(formId, state) {

    }

    function getMBString(bytes) {
        return (bytes / (1024 * 1024)).toFixed(2).toString();
    }

    //function setBtnClass(btn, cls) {
    //    uploadBtn.removeClass("btn-info btn-danger btn-success btn-warning btn-primary");
    //    uploadBtn.addClass(cls);
    //}

    function makeFormData(formId, methodName) {
        var form = $("#" + formId);
        var inputs = [];

        var fd = new FormData();
        form.find("input:file").each(function (i, child) {
            var input = $(this);
            if (this.files.length > 0) {
                fd.append(input.attr('name'), this.files[0]);
                inputs.push(this.files[0].name);
            }
        });
        form.find("input:text").each(function (i, child) {
            var input = $(this);
            var val = input.val().trim();
            fd.append(input.attr('name'), val);
            if (val.length > 0) {
                inputs.push(val.length < 10 ? val : (val.substr(0, 7) + "..."));
            }
        });

        var hint = methodName + ": " + inputs.join(", ");
        return { data: fd, hint: hint };
    }

    function uploadData(btn, formId, hint, data, action) {
        btn.prop("disabled", true);
        btn.text("Uploading...");
        uploadAjaxFormData(data, action, {
            onComplete: function (response) {
                if (response.ok) {
                    btn.text("Done. Redirecting...");
                    var href = MotiveValidatorActions.resultAction.replace("-id-", response.id);
                    RecentlySubmittedComputations.submit("MotiveValidator", response.id, hint);
                    window.location.href = href;
                } else {
                    btn.prop("disabled", false);
                    btn.text("Error: " + response["message"]);
                }
            },
            onProgress: function (current, total) {
                if (current !== undefined) {
                    var percentComplete = Math.round(current * 100 / total);
                    btn.text("Uploading... " + percentComplete.toFixed(2).toString() + "% (" + getMBString(current) + " / " + getMBString(total) + " MB)");
                } else {
                    btn.text("Uploading...");
                }
            },
            onFailed: function () {
                btn.prop("disabled", false);
                btn.text("Uploading Error. Please try again later.");
            },
            onAborted: function () {
                btn.prop("disabled", false);
                btn.text("Upload Aborted");
            }
        }); 
    }

    $("button[data-do-upload='true']").prop("disabled", false).click(function (e) {
        var $this = $(this);

        var form = $this.data('upload-form');
        var action = $this.data('upload-action');
        var methodName = $this.data('method-name');
        var data = makeFormData(form, methodName);

        uploadData($this, form, data.hint, data.data, action);
    });
});