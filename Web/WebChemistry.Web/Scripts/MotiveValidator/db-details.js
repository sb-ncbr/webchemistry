function downloadByModelDetails() {
    var ret = {};
    downloadDetails(ValidatorDbParams.modelSummary, "model-details-view", createByModelView, ret);
    return ret;
}

function downloadByStructureDetails() {
    var ret = {};
    downloadDetails(ValidatorDbParams.structureSummary, "structure-details-view", createByStructureView, ret);
    return ret;
}

function downloadDetails(action, target, createViewFunc, ret) {
    var spinner = new Spinner({ hwaccel: true, radius: 25, length: 15, width: 8, lines: 13 }).spin(document.getElementById(target));

    $.ajax({
        url: action,
        type: 'GET',
        dataType: 'text',
        cache: false
    })
    .done(function (data) { showDetails(data, "#" + target, createViewFunc, ret); })
    .fail(function (jqXHR, textStatus, errorThrown) { $("#" + target).text("Downloading details failed. Try refreshing the page."); })
    .always(function () { spinner.stop(); });
}

var DetailsValueFormat = 0,
    DetailsValueFormatValue = ko.observable("Value");

DetailsValueFormatValue.subscribe(function (value) {
    DetailsValueFormat = value === "Value" ? 0 : 1;
});

function formatValue(value, ctx, col) {
    if (DetailsValueFormat === 0 || col.field === "Analyzed") {
        if (value) {
            return value;
        } else {
            return "-";
        }
    } else {
        if (value) {
            var t = (ctx.Analyzed || 0) + (ctx.NotAnalyzed || 0);
            return t === 0 ? "-" : (100 * value / t).toFixed(2) + "%";
        } else {
            return "-";
        }
    }
}

////function colorValueFormatter(color, inv) {
////    var cache = {};
////    return function (row, cell, value, columnDef, dataContext) {
////        var key = columnDef.id + "-" + row + "-" + DetailsValueFormat, val = cache[key];

////        if (val === undefined) {
////            if (value) {
////                val = "<span style='color:" + color + "'>" + formatValue(value, dataContext, columnDef) + "</span>";
////            } else {
////                val = "<span style='color:#aaa'>-</span>";
////            }
////            cache[key] = val;
////        }
////        return val;
////    };
////}


function colorValueFormatter(color, inv) {
    return function (row, cell, value, columnDef, dataContext) {
        var val;
        if (value) {
            val = "<span style='color:" + color + "'>" + formatValue(value, dataContext, columnDef) + "</span>";
        } else {
            val = "<span style='color:#aaa'>-</span>";
        }
        return val;
    };
}


function makeColumn(field, name, tooltip, width, cls, headerCls, shortcut) {
    if (width === undefined) { width = 57; }
    if (cls === undefined) { cls = ""; }    
    tooltip = "[" + name + "] " + tooltip;
    if (shortcut !== undefined) {
        name = shortcut;
    }
    if (headerCls === undefined) { headerCls = 'mv-header-default'; }
    headerCls = "mv-header-base " + headerCls;
    return { id: field, field: field, name: name, sortable: true, width: width, toolTip: tooltip, resizable: false, cssClass: "details-cell " + cls, headerCssClass: headerCls };
}

function makeCommonColumn(field) {
    var props = MotiveValidatorGlobals[field];
    var shortcut = props.shortName,
        name = props.name,
        tooltip = "[" + props.name + "] " + props.tooltip,
        width = 57,
        cls = "numeric-details-cell",
        headerCls = "mv-header-base " + field + "-summary-header",
        color = props.color;
    
    return { isSelected: ko.observable(false), id: field, field: field, name: shortcut, sortable: true, width: width, toolTip: tooltip, resizable: false, formatter: colorValueFormatter(color), cssClass: "details-cell " + cls, headerCssClass: headerCls };
    //if (color) {
    //} else {
    //    return { isSelected: ko.observable(false), id: field, field: field, name: shortcut, sortable: true, width: width, toolTip: tooltip, resizable: false, cssClass: "details-cell " + cls, headerCssClass: headerCls };
    //}
}

var defaultColumns = [
    "Analyzed",
    "Missing_Atoms",
    "Missing_Rings",
    //"Missing_Disconnected",
    "Missing_Degenerate",

    "HasAll_GoodChirality",
    "HasAll_GoodChirality_IgnorePlanarAndNonSingleBondErrors",

    "HasAll_BadChirality",
    "HasAll_BadChirality_Carbon",
    "HasAll_WrongBonds",

    //"HasAll_Substitutions",
    //"HasAll_Foreign",
    //"HasAll_NameMismatch",
    //"HasAll_ZeroRmsd"
];

function makeCommonColumns() {
    return _.mapValues(MotiveValidatorGlobals, function (v, k) { return makeCommonColumn(k); });
}

var commonGridColumns;

// make header background styles
$(function () {
    var style = document.createElement('style');
    style.type = 'text/css';
    var styles = "";
    $.each(MotiveValidatorGlobals, function (name, props) {
        styles += "." + name + "-summary-header { background: " + props.color + " !important; color: white } ";
    });
    style.innerHTML = styles;
    document.getElementsByTagName('head')[0].appendChild(style);

    commonGridColumns = _.mapValues(MotiveValidatorGlobals, function (v, k) { return makeCommonColumn(k); });
    _.forEach(defaultColumns, function (c) { commonGridColumns[c].isSelected(true); });
});

function createByModelView(csvString) {
    function renderIdCell(row, cell, value, columnDef, dataContext) {
        return "<a href='" + ValidatorDbParams.perModelAction.replace("-id-", dataContext["Name"]) + "' style='color: blue' target='_blank'>" + dataContext["Name"] + "</a>";
    }

    //var data = $.csv.toArrays(csvString, {});
    //var header = data[0];
    //data.shift();
    
    var columns = [
        { id: "Name", field: "Name", name: "Molecule", sortable: true, width: 60, toolTip: "Molecule Identifier", resizable: false, formatter: renderIdCell, cssClass: "details-cell light-details-cell", headerCssClass: 'mv-header-base mv-header-default' },
        makeColumn("AtomCount", "Atoms", "Number of atoms", undefined, "numeric-details-cell"),
        makeColumn("ChiralAtomCount", "Chiral Atoms", "Number of chiral atoms", undefined, "numeric-details-cell"),
        makeColumn("StructureCount", "PDB Entries", "Number of PDB entries that the residue appears in", undefined, "numeric-details-cell details-cell-separator"),
    ];

    var data = $.parse(csvString, { delimiter: ',', header: true, dynamicTyping: true, forceStringFields: { 'Name': true } });
    var filtered = _.filter(data.results.rows, function (e) { return e["Analyzed"] + (e["NotAnalyzed"] || 0) > 0; });
    
    var dataView = new Slick.Data.DataView();
    dataView.setItems(filtered, "Name");

    return {
        view: dataView,
        prefixColumns: columns,
        idColumn: "Name",
        filterProvider: function (val) { var regex = new RegExp(val, "i"); return function (e) { return regex.test(e.Name); }; }
    };
}

function createByStructureView(csvString) {
    function renderIdCell(row, cell, value, columnDef, dataContext) {
        return "<a href='" + ValidatorDbParams.perStructureAction.replace("-id-", dataContext["Id"]) + "' style='color: blue' target='_blank'>" + dataContext["Id"] + "</a>";
    }

    //var data = $.csv.toArrays(csvString, { });
    //var header = data[0];

    var columns = [
        { id: "Id", field: "Id", name: "PDB ID", sortable: true, width: 60, toolTip: "PDB ID", resizable: false, formatter: renderIdCell, cssClass: "details-cell light-details-cell", headerCssClass: 'mv-header-base mv-header-default' },           
        makeColumn("Models", "Molecules", "Molecule Identifiers", 171, "light-details-cell details-cell-separator"),
    ];

    var data = $.parse(csvString, { delimiter: ',', header: true, dynamicTyping: true, forceStringFields: { 'Id': true, "Models": true } });
    
    var dataView = new Slick.Data.DataView();
    dataView.setItems(data.results.rows, "Id");

    return {
        view: dataView,
        prefixColumns: columns,
        idColumn: "Id",
        columns: makeCommonColumns(),
        filterProvider: function (val) { var regex = new RegExp(val, "i"); return function (e) { return regex.test(e.Id) || regex.test(e.Models); }; }
    };
}

var suppressColumnUpdate = false;
function ColumnSelectionModel(grid, prefixCols) {
    var self = this;
    this.columnArray = _.map(MotiveValidatorGlobalsColumnOrdering, function (c) { return commonGridColumns[c]; });
    
    function updateColumns(grid, prefix, columns) {
        var totalWidth = 684;
        var w = Math.floor(totalWidth / columns.length);
        _.forEach(columns, function (c) { c.width = w; });
        grid.setColumns(prefix.concat(columns));
    }
       

    function update() {
        if (suppressColumnUpdate) {
            return;
        }

        updateColumns(grid, prefixCols, _.filter(self.columnArray, function (c) { return c.isSelected(); }));
    }

    _.forEach(this.columnArray, function (c) { c.isSelected.subscribe(update); });

    this.update = update;

    this.resetColumns = function () {
        suppressColumnUpdate = true;
        _.forEach(self.columnArray, function (c) { c.isSelected(false); });
        _.forEach(defaultColumns, function (c) { commonGridColumns[c].isSelected(true); });
        suppressColumnUpdate = false;
        update();
    };
}

function showDetails(csvString, target, viewFunc, ret) {
    "use strict";

    var sortCol = "", sortDir = -1, idColumn = "";

    function sortByIdComparer(a, b) {
        var x = a[idColumn], y = b[idColumn];
        return (x === y ? 0 : (x > y ? 1 : -1));
    }

    function sortPercentComparer(a, b) {
        var tx = (a.Analyzed || 0) + (a.NotAnalyzed || 0), ty = (b.Analyzed || 0) + (b.NotAnalyzed || 0), x, y;

        if (tx === 0) {
            if (ty === 0) return sortByIdComparer(a, b);
            return -1;
        }
        if (ty === 0) return 1;

        x = a[sortCol] / tx;
        y = b[sortCol] / ty;
        return (x === y ? sortByIdComparer(a, b) : (x > y ? 1 : -1));
    }

    function sortValueComparer(a, b) {
        var x = a[sortCol], y = b[sortCol];
        return (x === y ? sortByIdComparer(a, b) : (x > y ? 1 : -1));
    }
    
    var data = viewFunc(csvString);

    var dataView = data.view, columns = data.columns;
    idColumn = data.idColumn;

    var gridOptions = {
        enableCellNavigation: false,
        enableColumnReorder: false,
        multiSelect: false,
        forceFitColumns: true,
        editable: false
    };

    var grid = new Slick.Grid(target, dataView, [], gridOptions);
    dataView.onRowCountChanged.subscribe(function (e, args) {
        grid.updateRowCount();
        grid.render();
    });
    dataView.onRowsChanged.subscribe(function (e, args) {
        grid.invalidateRows(args.rows);
        grid.render();
    });
    grid.onSort.subscribe(function (e, args) {
        sortCol = args.sortCol.field;
        sortDir = args.sortAsc;

        var func = undefined;
        
        if (sortCol === idColumn) {
            func = sortByIdComparer;
        } else if (DetailsValueFormat === 0
            || sortCol === "Analyzed"
            || _.some(data.prefixColumns, function (c) { return c.field === sortCol; })) {
            func = sortValueComparer;
        } else {
            func = sortPercentComparer;
        }

        dataView.sort(func, args.sortAsc);
    });

    var colSelModel = new ColumnSelectionModel(grid, data.prefixColumns);
    colSelModel.update();
    
    var filterModel = {
        filterString: ko.observable()
    };

    var timer = null;
    var oldFilterValue = "";
    filterModel.filterString.subscribe(function (value) {
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
            dataView.setFilter(data.filterProvider(value));
        }, 500);
    });
    
    ko.applyBindings(filterModel, $(target + "-filter").get(0));
    ko.applyBindings(colSelModel, $(target + "-columns").get(0));
    $(target + "-columns label").tooltip();
    $(target + "-columns .btn").tooltip();

    DetailsValueFormatValue.subscribe(function () {
        grid.invalidate();
        //if (sortCol) {
        //    dataView.sort(DetailsValueFormat === 0 || sortCol === "Analyzed" ? sortValueComparer : sortPercentComparer, sortDir);
        //}
    });

    ret.refresh = function () { colSelModel.update(); };
}