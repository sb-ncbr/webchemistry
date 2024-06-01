
function showInputDetailsStructure(event, id) {
    if (event) event.preventDefault();
    var s = PatternQueryResultModel.inputModel.structureMap[id];
    PatternQueryResultModel.inputModel.currentStructure(s);
}

function PatternQueryResultInputModel(input, vm) {
    "use strict";
    
    var self = this,
        downloading = false,
        shown = false,
        data,
        grid;

    this.structureMap = {};

    this.input = input;
    this.isSpecific = false;
    this.isFiltered = false;

    this.viewState = ko.observable("New");
    this.currentStructure = ko.observable();
    
    //this.isLoaded = ko.observable(false);
    //this.isBusy = ko.observable(false);

    switch (input.DataSource) {
        case 0: case "Db": this.sourceType = "the <a href='//www.pdbe.org' target='_blank'>PDBe.org</a> database"; break;
        case 1: case "List": this.sourceType = "specific PDB entries"; this.isSpecific = true; break;
        case 2: case "Filtered": this.sourceType = "the <a href='//www.pdbe.org' target='_blank'>PDBe.org</a> database"; this.isFiltered = true; break;
        default: this.sourceType = "unspecified source"; break;
    }

    if (input.Filters) {
        this.filters = _.map(input.Filters, function (f) {
            var info = mqFilterData.filterMapByProperty[f.PropertyName],
                comp = mqFilterData.comparisonTypeMap[f.ComparisonType];
            return {
                name: info ? info.name : f.PropertyName,
                desc: info ? info.description : f.PropertyName,
                comparison: comp ? comp.text : f.ComparisonType,
                filter: f.Value
            };
        });
    } else {
        this.filters = [];
    }

    this.setCurrentFromGrid = function (src, event) {
        if (!event) return;
        var type = event.target.getAttribute("data-show-type"), id;
        if (!type) return;
        id = event.target.getAttribute("data-id");

        if (type === 'current-structure') {
            event.preventDefault();
            self.currentStructure(self.structureMap[id]);
        }
    };

    this.showEntryList = function () {
        var data = "data:text/plain;charset=UTF-8," + encodeURIComponent((input.EntryList || []).join('\n'));
        var w = window.open(data, '_blank');
        w.focus();
    };

    function load() {
        var spinner = new Spinner({ hwaccel: true, radius: 25, length: 15, width: 8, lines: 13, color: '#000' }).spin(document.querySelector('#input-tab .spinner-host'));
        downloading = true;

        $.ajax({
            url: PatternQueryActions.inputDetailsAction,
            type: 'GET',
            dataType: 'json'
        })
        .done(function (result) {
            spinner.stop();
            downloading = false;
            self.structureMap = _.indexBy(result, "Id");
            data = new Slick.Data.DataView();
            data.setItems(result, "Id");
            init();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            spinner.stop();
            console.log(errorThrown);
            downloading = false;
            self.viewState("DownloadFailed");
        });
    }

    var fieldCache = {};
    var renderIdCell = function (vm, fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.id + "-" + dataContext.Id, val = fieldCache[k];
            if (val === undefined) {
                var cls;
                if (dataContext.ErrorType === "None") {
                    cls = (dataContext.ReaderWarnings.length > 0 ? 'mq-reader-warning' : 'mq-structure-valid');
                } else {
                    cls = 'mq-structure-error';
                }
                //val = "<a class='" + cls + "' href='#' onclick='javascript:showInputDetailsStructure(event, \"" + value + "\")'>" + value + "</a>";
                val = "<a class='" + cls + "' href='#' data-show-type='current-structure' data-id='" + value + "'>" + value + "</a>";
                fieldCache[k] = val;
            }
            return val;
        };
    }(vm, fieldCache);

    var renderLengthCell = function (row, cell, value, columnDef, dataContext) {
        return value.length;
    };

    var renderErrorCell = function (row, cell, value, columnDef, dataContext) {
        return value === "None" ? "No" : "Yes";
    };

    this.displayFilters = ko.observable([]);
    this.filterType = ko.observable();
    this.filterText = ko.observable("");
    this.viewCount = ko.observable(0);

    function updateFilter() {
        var regexp = new RegExp(self.filterText().trim(), 'i'),
            filter;

        switch (self.filterType().type) {
            case "WithWarning": filter = (function (re) { return function (e) { return e.ReaderWarnings.length > 0 && re.test(e.Id); }; })(regexp); break;
            case "WithError": filter = (function (re) { return function (e) { return e.ErrorType !== "None" && re.test(e.Id); }; })(regexp); break;
            case "WithNoMotif": filter = (function (re) { return function (e) { return e.PatternCount === 0 && re.test(e.Id); }; })(regexp); break;
            default: filter = (function (re) { return function (e) { return re.test(e.Id); }; })(regexp); break;
        }
        
        data.setFilter(filter);
        self.viewCount(data.getLength());
    }

    var updateWrap = mqResultUtils.throttle(updateFilter, 250);
    
    function init() {
        if (!shown || !data || self.viewState() === "Initialized") return;
        self.viewState("Initialized");

        if (data.getLength() > 0) {
            self.currentStructure(data.getItem(0));
        }

        var displayFiltersTemplate = [
            { type: "All", label: "All", cls: "", count: data.getLength() },
            { type: "WithWarning", label: "Has Warnings", cls: "", count: 0 },
            { type: "WithError", label: "Has Error", cls: "", count: 0 },
            { type: "WithNoMotif", label: "Has No Pattern", cls: "", count: 0 }];
        
        _.forEach(data.getItems(), function (e) {
            if (e.ReaderWarnings.length > 0) displayFiltersTemplate[1].count++;
            if (e.ErrorType !== "None") displayFiltersTemplate[2].count++;
            if (e.PatternCount === 0) displayFiltersTemplate[3].count++;
        });

        _.forEach(displayFiltersTemplate, function (f) { f.label += ' (' + f.count + ')'; });
        self.displayFilters(_.filter(displayFiltersTemplate, function (f) { return f.count > 0; }));
        if (self.displayFilters().length > 0) {
            self.filterType(self.displayFilters()[0]);
        }
        
        var gridOptions = {
            enableCellNavigation: false,
            enableColumnReorder: false,
            multiSelect: false,
            explicitInitialization: true,
            forceFitColumns: true,
            editable: false
        };

        grid = new Slick.Grid("#input-details-grid", data, [], gridOptions);
        data.onRowCountChanged.subscribe(function (e, args) { grid.updateRowCount(); grid.render(); });
        data.onRowsChanged.subscribe(function (e, args) { grid.invalidateRows(args.rows); grid.render(); });

        var columns = [
            { id: "id", field: "Id", name: "Id", sortable: true, width: 140, toolTip: "PDB identifier.", formatter: renderIdCell, resizable: false },
            { id: "mc", field: "PatternCount", name: "Pattern Count", sortable: true, width: 140, toolTip: "Total number of patterns across all queries.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "rw", field: "ReaderWarnings", name: "Input Warning Count", sortable: true, width: 140, toolTip: "Number of input warnings.", cssClass: 'mq-numeric-cell', formatter: renderLengthCell, resizable: false, numeric: true },
            { id: "et", field: "ErrorType", name: "Has Error", sortable: true, width: 140, toolTip: "Has any error occurred while loading the structure.", cssClass: 'mq-numeric-cell', formatter: renderErrorCell, resizable: false },
            { id: "lt", field: "LoadTimingMs", name: "Load Time", sortable: true, width: 140, toolTip: "Time it took to parse the entry.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "qt", field: "QueryTimingMs", name: "Query Time", sortable: true, width: 140, toolTip: "Time it took to query the entry.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "filler", field: "", name: "", width: 70, resizable: false, sortable: false }
        ];
        grid.setColumns(columns);

        grid.onSort.subscribe(function (e, args) {
            var sortCol = args.sortCol,
                sortFunc;

            if (sortCol.id === "rw") {
                sortFunc = function (a, b) {
                    var x = a.ReaderWarnings.length, y = b.ReaderWarnings.length;
                    return (x === y ? 0 : (x > y ? 1 : -1));
                };
            } else {
                sortFunc = (function (prop) {
                    return function (a, b) {
                        var x = a[prop], y = b[prop];
                        return (x === y ? 0 : (x > y ? 1 : -1));
                    };
                })(sortCol.field);
            }
            data.sort(sortFunc, args.sortAsc);
        });

        grid.init();
        self.viewCount(data.getLength());
        self.filterType.subscribe(updateFilter);
        self.filterText.subscribe(updateWrap);
    }

    this.showCsvList = function () {
        var cols = _.map(_.filter(grid.getColumns(), function (c) { return c.id !== 'filler'; }), function (c) {
            if (c.id === 'rw') return { header: c.name, isNumeric: c.numeric, getter: function (r) { return r.ReaderWarnings.length; } };
            if (c.id === 'et') return { header: c.name, isNumeric: c.numeric, getter: function (r) { return r.ErrorType === "None" ? "No" : "Yes"; } };
            return { header: c.name, isNumeric: c.numeric, getter: function (r) { return r[c.field]; } };
        });

        var csv = WebChemUtils.toCsv(data.getRows(), cols),
            uri = "data:text/plain;charset=UTF-8," + encodeURIComponent(csv);
        window.open(uri, '_blank').focus();
    };
    
    this.showTab = function () {
        shown = true;

        if (downloading) return;
        if (self.viewState() === "New") {
            if (!data) {
                load();
            } else {
                init();
            }
        }       
    };

    this.hideTab = function () {
        shown = false;
    };
}