"use strict";

function getErrorStructureUrl(model, id, type, download) {
    var po = { model: model, type: type, sid: id };
    if (download) po["action"] = "download";
    var params = $.param(po);
    return validatorParams.structureAction + "?" + params;
}

function transformErrors(errors, modelName) {
    var ret = [];
    for (var e in errors) {
        if (!errors.hasOwnProperty(e)) { continue; }

        var html = e;
        if (modelName) {
            html = "<a href='#' title='Display 3D' onclick='showErrorDetails(\"" + e + '", "' + modelName + "\", event)'>" + e + "</a>"
            + " <a title='Download' href='" + getErrorStructureUrl(modelName, e, "notanalyzedpdb", true) + "' target='_blank'><i class='icon-download-alt'></i></a>"
            + " <a title='Show' href='" + getErrorStructureUrl(modelName, e, "notanalyzedpdb", false) + "' target='_blank'><i class='icon-eye-open'></i></a>";
        }
        ret.push([{title: e, html: html}, [errors[e]]])
    }
    ret.sort(function (a, b) {
        var keyA = a[0].title, keyB = b[0].title;
        if (keyA < keyB) { return -1; }
        if (keyA > keyB) { return 1; }
        return 0;
    });
    return ret;
}

function transformWarnings(warnings) {
    var ret = [];
    for (var e in warnings) {
        if (!warnings.hasOwnProperty(e)) { continue; }

        var w = warnings[e];
        ret.push([{ title: e, html: e }, w]);
    }
    ret.sort(function (a, b) {
        var keyA = a[0].title, keyB = b[0].title;
        if (keyA < keyB) { return -1; }
        if (keyA > keyB) { return 1; }
        return 0;
    });
    return ret;
}

function ValidatorErrorsModel(result) {
    var self = this;

    this.entries = [];
    
    var gec = Object.keys(result.Errors).length;

    var allModelErrors = [];
    var includedErrors = { };
    var allModelWarnings = [];
    var includedWarnings = { };
    var modelEntries = [];

    if (gec > 0) {
        this.entries.push({
            tag: "__generalerrors",
            title: "General Errors (" + gec + ")",
            header: "Errors",
            count: gec,
            data: transformErrors(result.Errors)
        });
    }

    result.Models.forEach(function (m) {

        if (m.errorCount > 0) {
            var transformed = transformErrors(m.Errors, m.ModelName);
            modelEntries.push({
                tag: m.ModelName + "err",
                title: m.ModelName.toUpperCase() + " Errors (" + m.errorCount + ")",
                header: "Errors",
                count: m.errorCount,
                data: transformed
            });

            transformed.forEach(function (e) {
                if (!includedErrors[e[0].title]) {
                    includedErrors[e[0].title] = true;
                    allModelErrors.push(e);
                }
            });
        }

        if (m.warningCount > 0) {
            var transformed = transformWarnings(m.Warnings);
            modelEntries.push({
                tag: m.ModelName + "wrr",
                title: m.ModelName.toUpperCase() + " Warnings (" + m.warningCount + ")",
                header: "Warnings",
                count: m.warningCount,
                data: transformed
            });

            transformed.forEach(function (e) {
                if (!allModelWarnings[e[0].title]) {
                    allModelWarnings[e[0].title] = true;
                    allModelWarnings.push(e);
                }
            });
        }
    });

    var sorter = function (a, b) {
        var x = a[0].title, y = b[0].title;
        if (x === y) { return 0; }
        if (x < y) { return -1; }
        return 1;
    }

    allModelErrors.sort(sorter);
    allModelWarnings.sort(sorter);

    if (allModelErrors.length > 0) {
        this.entries.push({
            tag: "__allerrors",
            title: "All Errors",// (" + allModelErrors.length + ")",
            header: "Errors",
            count: allModelErrors.length,
            data: allModelErrors
        });
    }

    if (allModelWarnings.length > 0) {
        this.entries.push({
            tag: "__allwarnings",
            title: "All Warnings",// (" + allModelWarnings.length + ")",
            header: "Warnings",
            count: allModelWarnings.length,
            data: allModelWarnings
        });
    }

    modelEntries.forEach(function (e) { self.entries.push(e); });

    this.currentEntry = ko.observable();
    if (this.entries.length > 0) {
        this.currentEntry(this.entries[0]);
    }

    this._show = function (tag) {
        console.log(tag);
        for (var e in self.entries) {
            if (self.entries[e].tag == tag) {
                self.currentEntry(self.entries[e]);
                break;
            }
        }
    };

    this.showWarnings = function (modelName) {
        var tag = modelName + "wrr";
        self._show(tag);
    };

    this.showErrors = function (modelName) {
        var tag = modelName + "err";
        self._show(tag);
    };
}

var errorDetailsModalModel = {
    id: ko.observable(""),
    modelName: ko.observable(""),
    status: ko.observable(""),
    current: ko.observable(null),
    visualizer: null
};

var errorDetailsVisualization = null;

$(function () {
    //errorDetailsModalModel.visualizer = createVisualizer("model3d-error");
    ko.applyBindings(errorDetailsModalModel, document.getElementById("errorDetailsModal"));

    $('#errorDetailsModal').on('hide', function () {
        if (errorDetailsVisualization != null) {
            errorDetailsVisualization.dispose();
            errorDetailsVisualization = null;
        }
    });
});

var errorDetailsModelCache = {};

function createErrorModelVisualization(sid, modelName) {
    var xhr = null;
    var cancel = function () {
        if (xhr != null) {
            xhr.abort();
            xhr = null;
        }
    };

    var visualizer = createVisualizer("model3d-error"); //errorDetailsModalModel.visualizer;

    errorDetailsModalModel.status("");
    if (!visualizer) {
        return;
    }

    var cacheKey = modelName + "-" + sid;

    if (errorDetailsModelCache[cacheKey]) {
        setTimeout(function () {
            try {
                var result = errorDetailsModelCache[cacheKey];
                var mol = molReader.read(result, 1);
                visualizer.clear();
                visualizer.loadContent([mol], []);
            } catch (ex) {
                visualizer.clear();
                errorDetailsModalModel.status("Failed to load the structure.");
            }
        }, 50);
    } else {
        errorDetailsModalModel.status("Loading...");
        xhr = $.ajax({
            url: validatorParams.structureAction,
            data: { model: modelName, sid: sid, type: "notanalyzedmol" },
            cache: false,
            dataType: 'text'
        })
        .done(function (result) {
            errorDetailsModalModel.status("");
            xhr = null;
            try {
                var mol = molReader.read(result, 1);
                visualizer.clear();
                visualizer.loadContent([mol], []);
                errorDetailsModelCache[cacheKey] = result;
            } catch (ex) {
                visualizer.clear();
                errorDetailsModalModel.status("Failed to load the structure.");
            }
        })
        .fail(function () { errorDetailsModalModel.status("Failed to load the structure."); xhr = null; });
    }

    return {
        dispose: function () {
            cancel();
            if (visualizer) {
                visualizer.clear();
                visualizer = null;
                $("#model3d-error-wrap > canvas").empty().remove();
            }
        }
    }
}

function showErrorDetails(sid, modelName, event) {
    if (event) {
        event.preventDefault();
    }

    errorDetailsVisualization = createErrorModelVisualization(sid, modelName);
    errorDetailsModalModel.id(sid);
    errorDetailsModalModel.modelName(modelName);
    $('#errorDetailsModal').modal();
};
