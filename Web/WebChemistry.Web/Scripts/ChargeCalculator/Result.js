var mainViewModel = {
    template: ko.observable({ name: "empty-template", data: null })   
};

var entriesView = undefined;
var currentErrorsModel = { entry: ko.observable() };

function prepareEntries(result) {
    "use strict";

    for (var key in result.Entries) {
        if (!result.Entries.hasOwnProperty(key)) {
            continue;
        }
        var entry = result.Entries[key];
        
        var atomCountHtml = "";
        var elementsArray = Object.keys(entry.AtomCounts).sort();
        var atomCount = 0;
        for (var i in elementsArray) {
            var el = elementsArray[i];
            atomCountHtml += el;
            var cc = entry.AtomCounts[el];
            atomCount += cc;
            if (cc > 1) {
                atomCountHtml += "<sub>" + cc.toString() + "</sub>";
            }
        }
        entry.atomCountHtml = atomCountHtml;
        entry.atomCount = atomCount;

        entry.ComputationWarnings = entry.ComputationWarnings || [];
        entry.ReaderWarnings = entry.ReaderWarnings || [];
        entry.Errors = entry.Errors || [];

        if (entry.IsValid) {
            entry.color = entry.ComputationWarnings.length > 0 || entry.ReaderWarnings.length > 0 || entry.Errors.length > 0 ? "#DB890F" : "blue";
        } else {
            entry.color = "red";
        }

        entry.warningCount = entry.ComputationWarnings.length + entry.ReaderWarnings.length;
        entry.errorCount = entry.Errors.length;
    }
}

function renderAtomCountsCell(row, cell, value, columnDef, dataContext) {
    return dataContext['atomCountHtml'];
}

function renderStructureIdCell(row, cell, value, columnDef, dataContext) {
    return "<a href='" + ChargeCalculatorParams.detailsAction.replace("-id-", dataContext.Id) + "' target='_blank' style='color:" + dataContext.color + "'>" + dataContext.Id + "</a>";
}

function renderErrorCell(row, cell, value, columnDef, dataContext) {
    if (value === 0) {
        return value;
    }
    var color = columnDef.id === "warningCount" ? "#DB890F" : "red";
    return "<a style='color:" + color + "' href='#' onClick='javascript:showErrorDetails(event, \"" + dataContext.Id + "\")'>" + value + "</a>";
}

function showErrorDetails(e, id) {
    e.preventDefault();
    var entry = entriesView.view.getItemById(id);
    if (!entry) { return; }
    currentErrorsModel.entry(entry);
    $("#errors-modal").modal();
}

function createStructuresGrid(result) {
    "use strict";

    var gridOptions = {
        enableCellNavigation: false,
        enableColumnReorder: false,
        multiSelect: false,
        forceFitColumns: true,
        editable: false
    };

    var structuresDataView = new Slick.Data.DataView();
    structuresDataView.setItems(result.Entries, "Id");

    $("#results-view").height(Math.min(30 * 24 + 10, (result.Entries.length + 1) * 24 + 10));
    var structuresGrid = new Slick.Grid("#results-view", structuresDataView, [], gridOptions);
    structuresDataView.onRowCountChanged.subscribe(function (e, args) {
        structuresGrid.updateRowCount();
        structuresGrid.render();
    });
    structuresDataView.onRowsChanged.subscribe(function (e, args) {
        structuresGrid.invalidateRows(args.rows);
        structuresGrid.render();
    });
    structuresGrid.setColumns([
        { id: "id", field: "Id", name: "Molecule Id", sortable: false, width: 200, toolTip: "Molecule name obtained from its filename.", formatter: renderStructureIdCell, resizable: false },
        { id: "atomCount", field: "atomCount", name: "#Atoms", sortable: false, width: 80, toolTip: "Atom count.", resizable: false },
        { id: "atoms", field: "atomCountHtml", name: "Atoms", sortable: false, width: 200, toolTip: "Atoms.", formatter: renderAtomCountsCell, resizable: false },
        { id: "timing", field: "TimingMs", name: "Time (ms)", sortable: false, width: 100, toolTip: "Computation time in milliseconds.", resizable: false },
        { id: "warningCount", field: "warningCount", name: "#Warnings", sortable: false, width: 100, toolTip: "Warning count.", formatter: renderErrorCell, resizable: false },
        { id: "errorCount", field: "errorCount", name: "#Errors", sortable: false, width: 100, toolTip: "Error count.", formatter: renderErrorCell, resizable: false },
        //{ id: "totalCharge", field: "totalChargeString", name: "Total Charge", sortable: false, width: 100, toolTip: "Total Charge.", editor: Slick.Editors.Text, validator: totalChargeValidator, resizable: false },
        //{ id: "msg", field: "message", name: "Message", sortable: false },
        { id: "filler", field: "", name: "", sortable: false }
    ]);

    return {
        view: structuresDataView,
        grid: structuresGrid
    };
}

function showResult(result, size) {
    prepareEntries(result);
    mainViewModel.template({ name: "result-template", data: result });
    entriesView = createStructuresGrid(result);
    ko.applyBindings(currentErrorsModel, document.getElementById("errors-modal"));
    ////if (size) {
    ////    $('#charges-result-size').text(size);
    ////}
}

function showError(error) {
    mainViewModel.template({ name: "error-template", data: { message: error } });
}

function defaultMessageFormatter(status) {
    if (status.IsIndeterminate) {
        return status.Message;
    } else {
        return status.Message + " " + status.CurrentProgress + "/" + status.MaxProgress;
    }
}

function statusMessageFormatter(status) {
    "use strict";

    try {
        var custom = JSON.parse(status.CustomState);
        if (!custom) {
            return defaultMessageFormatter(status);
        }
        var msg;

        if (status.IsIndeterminate) {
            msg = status.Message;
        } else {
            msg = status.Message + " <b>" + status.CurrentProgress + "/" + status.MaxProgress + "</b>";
        }

        if (custom.Message) {
            if (custom.IsIndeterminate) {
                msg += "<br />" + custom.Message;
            } else {
                msg += "<br />" + custom.Message + " <b>" + custom.CurrentProgress + "/" + custom.MaxProgress + "</b>";
            }
        }

        if (custom.IsCurrentSetProgressAvailable) {
            msg += "<br />Set progress: <b>" + custom.CurrentSetProgress + "/" + custom.CurrentSetMaxProgress + "</b>";
        }

        return msg;
    } catch (e) {
        return defaultMessageFormatter(status);
    }
}

$(function () {
    "use strict";

    var computationWrapper = StatusAndResultWrapper({
        mainViewModel: mainViewModel,
        isFinished: ChargeCalculatorParams.isFinished,
        statusAction: ChargeCalculatorParams.statusAction,
        resultAction: ChargeCalculatorParams.summaryAction,
        resultSize: ChargeCalculatorParams.resultSize,
        onResult: showResult,
        messageFormatter: statusMessageFormatter,

        canCancel: true,
        canceledTemplateName: "canceled-template",
        onCancel: function () {
            if (confirm("Do you really want to cancel the computation?")) {
                window.location.href = ChargeCalculatorParams.killAction;
            }
        }
    });
    ko.applyBindings(mainViewModel, document.getElementById("mainView"));
    computationWrapper.run();
});