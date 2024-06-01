function setCurrentMotif(event, id) {
    if (event) event.preventDefault();
    mqExplorerModel.motifs.setCurrentMotif(id);
    mqExplorerModel.fromPdb.hide();
}

function PatternQueryExplorerMotifsViewModel(vm, gridTarget) {
    "use strict";

    var self = this,
        data = new Slick.Data.DataView(),
        grid;

    var gridOptions = {
        enableCellNavigation: false,
        enableColumnReorder: false,
        multiSelect: false,
        explicitInitialization: true,
        forceFitColumns: true,
        editable: false,
        //resizable: true,
        //rerenderOnResize: true,
    };

    this.countString = ko.observable('showing 0 entries');

    grid = new Slick.Grid("#" + gridTarget, data, [], gridOptions);
    data.onRowCountChanged.subscribe(function (e, args) {
        grid.updateRowCount();
        grid.render();
        self.countString('showing ' + mqResultUtils.pluralize(data.getLength(), 'pattern', 'patterns'));
    });
    data.onRowsChanged.subscribe(function (e, args) { grid.invalidateRows(args.rows); grid.render(); });
    $(window).resize(function () { grid.resizeCanvas(); });

    var fieldCache = { cache: {} };
    var signatureCellFormatter = function (fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.id + "-" + dataContext.Id, sig = fieldCache.cache[k];
            if (sig === undefined) {
                sig = "<span title='" + value + "'>" + mqResultUtils.formatSignature(value) + "</span>";
                fieldCache.cache[k] = sig;
            }
            return sig;
        };
    }(fieldCache);
    //return "<a href='#' style='color: blue' onclick='javascript:filterStructureMotifs(event, \"" + dataContext.Id + "\")'>" + value + "</a>";
    var idCellFormatter =  function (fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.id + "-" + dataContext.Id, val = fieldCache.cache[k];
            if (val === undefined) {
                //val = "<a href='#' style='color: #0052CC;' class='mq-explorer-structure-id' onclick='javascript:setCurrentMotif(event, \"" + value + "\")' title='" + value + "'>" + value + "</a>";
                val = "<a href='#' style='color: #0052CC;' class='mq-explorer-structure-id' data-motif-id='" + value + "' title='" + value + "'>" + value + "</a>";
                fieldCache.cache[k] = val;
            }
            return val;
        };
    }(fieldCache);

    var columns = [
        { id: "id", field: "Id", name: "Id", sortable: true, width: 100, toolTip: "Identifier (Parent_Serial[_Tag]).", formatter: idCellFormatter, resizable: false },
        { id: "ac", field: "AtomCount", name: "Atoms", sortable: true, width: 50, toolTip: "Number of atoms.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
        { id: "rc", field: "ResidueCount", name: "Residues", sortable: true, width: 50, toolTip: "Number of residues.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
        { id: "sg", field: "Signature", name: "Signature", sortable: true, width: 200, toolTip: "Types of residues in the pattern.", formatter: signatureCellFormatter, resizable: false }
    ];

    grid.setColumns(columns);

    grid.onSort.subscribe(function (e, args) {
        var sortFunc;
        
        if (args.sortCol.id !== 'id') {
            sortFunc = (function (prop) {
                return function (a, b) {
                    var x = a[prop], y = b[prop];
                    return (x === y ? 0 : (x > y ? 1 : -1));
                };
            })(args.sortCol.field);
        } else {
            sortFunc = function (a, b) {
                var x = a.ParentId, y = b.ParentId;
                if (x === y) {
                    var u = a.Serial, v = b.Serial;
                    return (u === v ? 0 : (u > v ? 1 : -1));
                }
                return (x === y ? 0 : (x > y ? 1 : -1));
            };
        }
        data.sort(sortFunc, args.sortAsc);
    });

    grid.init();

    this.isDownloadAvailable = ko.observable(false);
    this.downloadSize = ko.observable('');

    this.applyResult = function (result, setStructures) {

        var counts = {};
        fieldCache.cache = {};
        data.setItems(result.Patterns, 'Id');
        _.forEach(result.Patterns, function (m) {
            counts[m.ParentId] = (counts[m.ParentId] || 0) + 1;            
        });
        _.forEach(result.Structures, function (s) {
            counts[s.Id] = counts[s.Id] || 0;
        });
        if (setStructures) vm.structures.set(result.Structures);
        vm.structures.setMotifCounts(counts);
        self.isDownloadAvailable(result.IsZipAvailable && result.Patterns.length > 0);
        var dlSize = (result.ZipSizeInBytes / 1024 / 1024).toFixed(2);
        if (dlSize < 0.01) dlSize = "<0.01";
        self.downloadSize(result.IsZipAvailable && result.Patterns.length > 0 ? dlSize.toString() + ' MB' : '');
        self.currentMotif(result.Patterns[0]);
        grid.invalidateAllRows();
        grid.render();
    };

    this.downloadResult = function () {
        window.open(PatternQueryExplorerActions.downloadResultAction, '_blank').focus();
    };

    this.filterText = ko.observable('');
    var updateWrap = mqResultUtils.throttle(updateFilter, 50);
    this.filterText.subscribe(updateWrap);

    function updateFilter() {
        var regexp = new RegExp(self.filterText().trim(), 'i'),
            filter = (function (re) { return function (e) { return re.test(e.Id); }; })(regexp);
        data.setFilter(filter);
    }

    this.currentMotif = ko.observable();

    this.setCurrentMotif = function (id) {
        self.currentMotif(data.getItemById(id));
    };

    this.setCurrentMotifFromGrid = function (data, event) {
        if (!event) return;

        var id = event.target.getAttribute('data-motif-id')
        if (!id) return;
        event.preventDefault();
        self.setCurrentMotif(id);
    };

    this.clear = function (skipConfirm) {
        if (!skipConfirm && !confirm('Do you really want to remove all patterns?')) return;

        fieldCache.cache = {};
        data.setItems([], 'Id');
        self.currentMotif(undefined);
        self.isDownloadAvailable(false);
        self.downloadSize('');
    };
}