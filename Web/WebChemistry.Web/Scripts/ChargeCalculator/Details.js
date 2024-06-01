"use strict";

var mainViewModel = {
    template: ko.observable({ name: "empty-template", data: null })
};

function prepareResult(result) {

    var entry = result.Summary;
    // normalize the result
    if (!entry.ReaderWarnings) entry.ReaderWarnings = [];
    if (!entry.ComputationWarnings) entry.ComputationWarnings = [];
    if (!result.BondTypes) result.BondTypes = {};
    if (!result.Charges) result.Charges = {};
    if (!result.Partitions) result.Partitions = {};
    if (!result.StructureJson) result.StructureJson = "";

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
    result.atomCountHtml = atomCountHtml;
    result.atomCount = atomCount;

    result.computedChargeNames = Object.keys(result.Charges).sort();

    var totalCharges = {};
    _.forEach(result.computedChargeNames, function (n) {
        var match = n.match(/chrg_([^_]*)/i);
        if (match && match[1]) totalCharges[match[1]] = true;
    });

    result.presentTotalCharges = _.keys(totalCharges).sort().join("; ");

    var paritionNames = Object.keys(result.Partitions);
    result.dataModel = {
        partitionNames: paritionNames,
        currentPartitionName: ko.observable(paritionNames[0]),
        labelFilter: ko.observable(""),
        rowCount: ko.observable(0)
    };

    result.dataModel.currentPartitionName.subscribe(function (name) {
        result.setDataPartition(name);
    });

    result.setDataPartition = function (name) {
        if (!name) {
            result.data.view.setItems([]);
            return;
        }
        var p = result.Partitions[name];

        $.each(result.data.grid.getColumns(), function (i, c) {
            if (c.id !== "id") {
                c.src = p.Charges[c.name].GroupCharges;
            }
        });

        var groups = Object.keys(p.Groups).map(function (g) { return { id: +g, label: p.Groups[g].Label }; });
        result.data.view.setItems(groups);
        result.dataModel.rowCount(result.data.view.getLength());
        result.data.grid.invalidate();
    };

    // filtering of data
    function filterProvider(value) {
        var val = value.trim();
        if (!val) {
            return function () { return true; };
        }

        var regex = new RegExp(val, "i");

        return function (e) {
            return regex.test(e.label);  //_.indexOf(e.label, value) >= 0;
        };
    }

    var timer = null;
    var oldFilterValue = "";
    result.dataModel.labelFilter.subscribe(function (value) {
        if (value === oldFilterValue) {
            return;
        }

        oldFilterValue = value;
        if (timer !== null) {
            clearTimeout(timer);
            timer = null;
        }
        timer = setTimeout(function () {
            timer = null;
            result.data.view.setFilter(filterProvider(value));
            result.dataModel.rowCount(result.data.view.getLength());
        }, 500);
    });
}

function makeDataView(result) {
    function renderValueCell(row, cell, value, columnDef, dataContext) {
        var val = columnDef.src[value];
        return isNaN(val) ? "n/a" : (Math.round(100000 * val) / 100000).toFixed(3);
    }

    var gridOptions = {
        enableCellNavigation: false,
        enableColumnReorder: false,
        multiSelect: false,
        explicitInitialization: true,
        forceFitColumns: false,
        editable: false
    };

    var dataView = new Slick.Data.DataView();

    var grid = new Slick.Grid("#details-data-view", dataView, [], gridOptions);
    dataView.onRowCountChanged.subscribe(function (e, args) {
        grid.updateRowCount();
        grid.render();
    });
    dataView.onRowsChanged.subscribe(function (e, args) {
        grid.invalidateRows(args.rows);
        grid.render();
    });
    var columns = [
        { id: "id", field: "label", name: "Label", sortable: true, width: 160, toolTip: "Unique identifiers of atoms and residues.", resizable: false },
    ];

    $.each(result.computedChargeNames, function (i, n) {
        columns.push({ id: n, field: "id", name: n, sortable: true, width: 120, toolTip: n, resizable: false, formatter: renderValueCell, cssClass: 'charges-numeric-cell', src: null });
    });

    var sortCol = undefined;
    var sortComparer = function (a, b) {
        var x = sortCol[a.id], y = sortCol[b.id];

        if (isNaN(x)) {
            if (isNaN(y)) {
                return 0;
            }
            return 1;
        }
        if (isNaN(y)) {
            return -1;
        }

        return (x === y ? 0 : (x > y ? 1 : -1));
    };

    var sortComparerId = function (a, b) {
        var x = a.id, y = b.id;
        return (x === y ? 0 : (x > y ? 1 : -1));
    };

    grid.onSort.subscribe(function (e, args) {
        sortCol = args.sortCol.src;
        dataView.sort(args.sortCol.id === "id" ? sortComparerId : sortComparer, args.sortAsc);
    });
    
    grid.setColumns(columns);

    result.data = {
        view: dataView,
        grid: grid
    };
}

function showResult(result) {
    prepareResult(result);    

    if (result.computedChargeNames.length > 0) {
        initAggregates(result);
        initCorrelations(result);
        init3d(result);
    }

    mainViewModel.template({ name: "result-template", data: result });
    
    makeDataView(result);

    var initialized = {};
    $('a[data-toggle="tab"]').on('shown', function (e) {
        //e.target // activated tab
        //e.relatedTarget // previous tab
        
        if ($(e.target).attr('href') === "#data") {
            if (!initialized["#data"]) {
                initialized["#data"] = true;
                result.data.grid.init();
                $(window).resize(function () { result.data.grid.resizeCanvas(); });
                result.setDataPartition(result.dataModel.partitionNames[0]);
            } else {
                result.setDataPartition(result.dataModel.currentPartitionName());
            }
        } else if ($(e.target).attr('href') === "#aggregates" && !initialized["#aggregates"]) {
            initialized["#aggregates"] = true;
            result.aggregates.init();
        } else if ($(e.target).attr('href') === "#correlations" && !initialized["#correlations"]) {
            initialized["#correlations"] = true;
            result.correlations.init();
        } else if ($(e.target).attr('href') === "#model3d" && !initialized["#model3d"]) {
            initialized["#model3d"] = true;
            result.view3d.init();
        }

        if ($(e.target).attr('href') !== "#data") {
            result.setDataPartition();
        }
    });
}

function showError(error) {
    mainViewModel.template({ name: "error-template", data: { message: error } });
}

$(function () {
    var computationWrapper = StatusAndResultWrapper({
        mainViewModel: mainViewModel,
        isFinished: true,
        resultAction: ChargeCalculatorParams.dataAction,
        onResult: showResult,
        onError: showError
    });
    ko.applyBindings(mainViewModel, document.getElementById("mainView"));
    computationWrapper.run();
});