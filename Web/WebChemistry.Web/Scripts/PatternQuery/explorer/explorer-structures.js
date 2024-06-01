function removeStructure(id) {
    //if (!confirm("Do you really want to remove '" + id + "'?")) return;

    mqExplorerModel.structures.remove([id]);
}

function filterStructureMotifs(event, id) {
    if (event) event.preventDefault();
    mqExplorerModel.motifs.filterText('^' + id + '_');
}


function showStructureWarnings(event, id) {
    if (event) event.preventDefault();
    var s = mqExplorerModel.structures.getById(id);
    mqExplorerModel.log.structureWarnings(s);
}

function PatternQueryExplorerStructuresViewModel(vm, gridTarget) {
    "use strict";

    var self = this,
        data = new Slick.Data.DataView(),
        grid,
        structureMap = {};
    
    var gridOptions = {
        enableCellNavigation: false,
        enableColumnReorder: false,
        multiSelect: false,
        explicitInitialization: true,
        forceFitColumns: true,
        editable: false
    };

    this.countString = ko.observable('showing 0 entries');

    this.getCount = function () {
        return data.getLength();
    };

    data.setItems([], '_id');
    grid = new Slick.Grid("#" + gridTarget, data, [], gridOptions);
    data.onRowCountChanged.subscribe(function (e, args) {
        grid.updateRowCount();
        grid.render();
        self.countString('showing ' + mqResultUtils.pluralize(data.getLength(), 'entry', 'entries'));
    });
    data.onRowsChanged.subscribe(function (e, args) { grid.invalidateRows(args.rows); grid.render(); });
    
    var removeCell = function (row, cell, value, columnDef, dataContext) {
        return "<button class='btn btn-link btn-small mq-explorer-remove-structure-button' title='Remove' onclick='javascript:removeStructure(\"" + value + "\")'><i class='icon-remove'></i></button>";
    };

    var renderIdCell = function (row, cell, value, columnDef, dataContext) {
        return "<span class='mq-explorer-structure-id' title='" + value + "'>" + value + "</span>"
            + " <a href='//www.ebi.ac.uk/pdbe/entry/pdb/" + value + "' target='_blank' class='mq-explorer-structure-pdborg-link' title='View in PDBe.org'><i class='icon-info-sign'></i></a>"
            + "<a href='" + PatternQueryExplorerActions.ligandValidationAction.replace('-id-', value) + "' target='_blank' class='mq-explorer-structure-valdb-link' title='View in ValidatorDB. "
            +     "Only applicable to structures from PDBe.org.'><i class='icon-ok-sign'></i></a>";
    };

    var motifsCell = function (row, cell, value, columnDef, dataContext) {
        if (value === undefined) {
            return "<span style='color: #999'>n/a</span>";
        }
        return "<a href='#' style='color: #0052CC' onclick='javascript:filterStructureMotifs(event, \"" + dataContext.Id + "\")'>" + value + "</a>";
    };

    var warningsCell = function (row, cell, value, columnDef, dataContext) {
        if (value === 0) return "0";
        return "<a href='#' style='color: #DB890F' onclick='javascript:showStructureWarnings(event, \"" + dataContext.Id + "\")'>" + value + "</a>";
    };

    var columns = [
            { id: "rm", field: "Id", name: "", sortable: false, width: 22, toolTip: "PDB identifier.", formatter: removeCell, resizable: false },
            { id: "id", field: "Id", name: "Id", sortable: true, width: 110, toolTip: "PDB identifier.", formatter: renderIdCell, resizable: false },
            { id: "mc", field: "motifCount", name: "Patterns", sortable: true, width: 65, toolTip: "Number of patterns.", formatter: motifsCell, cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "ac", field: "AtomCount", name: "Atoms", sortable: true, width: 65, toolTip: "Number of atoms.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "rc", field: "ResidueCount", name: "Residues", sortable: true, width: 65, toolTip: "Number of residues.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "rw", field: "warningCount", name: "Warnings", sortable: true, width: 65, toolTip: "Number of reader warnings.", cssClass: 'mq-numeric-cell', formatter: warningsCell, resizable: false, numeric: true },
            //{ id: "filler", field: "", name: "", width: 70, resizable: false, sortable: false }
    ];

    grid.setColumns(columns);

    grid.onSort.subscribe(function (e, args) {
        var sortFunc = (function (prop) {
            return function (a, b) {
                var x = a[prop], y = b[prop];
                return (x === y ? 0 : (x > y ? 1 : -1));
            };
        })(args.sortCol.field);
        data.sort(sortFunc, args.sortAsc);
    });

    grid.init();
    
    ////this.add = function (structures) {
    ////    _.forEach(structures, function (s) {
    ////        s.motifCount = undefined;
    ////        s.warningCount = s.Warnings.length;
    ////        s._id = s.Id.toLowerCase();

    ////        var old = data.getItemById(s._id);
    ////        if (old) {
    ////            s.motifCount = old.motifCount;
    ////            data.deleteItem(s._id);
    ////        }

    ////        structureMap[s.Id] = s;
    ////        data.addItem(s);
    ////    });
    ////};

    this.set = function (structures) {
        var motifCounts = _.mapValues(structureMap, 'motifCount');
        structureMap = { };
        _.forEach(structures, function (s) {
            s.warningCount = s.Warnings.length;
            s._id = s.Id.toLowerCase();
            s.motifCount = motifCounts[s.Id];
            structureMap[s.Id] = s;
        });
        data.setItems(structures, '_id');
    };

    this.setMotifCounts = function (counts) {
        _.forEach(data.getItems(), function (s) {
            s.motifCount = counts[s.Id];
        });
        grid.invalidateAllRows();
        grid.render();
    };

    this.getById = function (id) {
        return data.getItemById(id.toLowerCase());
    };

    this.remove = function (structures) {
        vm.status.setBusy(true);
        vm.status.message('Removing...');
        $.ajax({ url: PatternQueryExplorerActions.removeStructuresActionProvider(structures), type: 'GET', dataType: 'json' })
        .done(function (result) {
            vm.status.setBusy(false);
            if (result['error']) {
                vm.log.error("Error removing structure(s).");
                return;
            }
            data.beginUpdate();
            _.forEach(structures, function (id) {
                data.deleteItem(id.toLowerCase());
                delete structureMap[id];
            });
            data.endUpdate();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            console.log(errorThrown);
            vm.log.error("Error removing structure(s).");
            vm.status.setBusy(false);
        });
    };

    this.removeAll = function (skipConfirm) {
        if (!skipConfirm && !confirm('Do you really want to remove all structures?')) return;
        self.remove(_.map(data.getItems(), 'Id'));
    };
}