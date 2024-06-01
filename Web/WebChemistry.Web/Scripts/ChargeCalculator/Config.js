var maxFullEemCount = 30000;

var mainViewModel = {
    template: ko.observable({ name: "empty-template", data: null })
};

var configViewModel,
    updateSummaryWhenSetSelectionChangesHandlerGlobal,
    checkExit = true,
    maxAtomCount = Number.MIN_VALUE;

function chargesPluralize(count, singular, plural) {
    if (count === 1) return "" + count + " " + singular;
    return "" + count + " " + plural;
}

function isSetUsableFilter(set) {
    "use strict";

    if (!set) {
        return false;
    }
    var target = set['Target'] ? set['Target'].toLowerCase() : "-";
    if (maxAtomCount > 255 && target !== 'biomacromolecules') {
        return false;
    }
    if (maxAtomCount <= 255 && target === 'biomacromolecules') {
        return false;
    }

    return set.hasAllParameters;
}

function prepareSet(set, requiredAtomTypes) {
    "use strict";

    set.id = set.Name;
    set.sel = ko.observable(false);
    set.sel.subscribe(updateSummaryWhenSetSelectionChangesHandlerGlobal);
    set.hasAllParameters = true;

    var missingParams = { };
    for (var el in requiredAtomTypes) {
        if (!_.contains(set.AvailableAtoms, el)) {
            set.hasAllParameters = false;
            missingParams[el] = true;
            //break;
        }
    }

    set.missingParamsString = _.keys(missingParams).sort().join(", ");
    
    set.availableAtomsString = set.AvailableAtoms.join(", ");
    _.forEach(set.Properties, function (prop) {
        set[prop[0]] = prop[1];
    });

    set.priorityValue = parseInt(set["Priority"]);
    set.approach =
        (set["QM Method"] ? set["QM Method"].toString().toUpperCase() : "-") + "/" +
        (set["Basis Set"] ? set["Basis Set"].toString().toUpperCase() : "-") + "/" +
        (set["Population Analysis"] ? set["Population Analysis"].toString().toUpperCase() : "-");

    set.groupKey = set.Target + ":|:" + set.approach;
}

function prepareEntriesAndSets(result, viewModel) {
    "use strict";

    var requiredAtomTypes = {};

    var minAtomCount = Number.MAX_VALUE,
        hasValidEntry = false,
        validCount = 0;

    for (var key in result.Entries) {
        if (!result.Entries.hasOwnProperty(key)) {
            continue;
        }
        var entry = result.Entries[key];
        entry.id = entry.StructureId;
        entry.isRemoved = false;

        if (entry.ErrorText.length > 0) {
            entry.message = entry.ErrorText;
        } else if (entry.Warnings.length > 0) {
            entry.message = entry.Warnings[0];
        } else {
            entry.message = "";
        }

        var atomCountHtml = "";
        var elementsArray = Object.keys(entry.AtomCounts).sort();
        var atomCount = 0;
        _.forEach(elementsArray, function (el) {
            requiredAtomTypes[el] = true;
            atomCountHtml += el;
            var cc = entry.AtomCounts[el];
            atomCount += cc;
            if (cc > 1) {
                atomCountHtml += "<sub>" + cc.toString() + "</sub>";
            }
        });
        entry.atomCountHtml = atomCountHtml;
        entry.atomCount = atomCount;
        entry.totalChargeString = entry.SuggestedCharge.toString();

        if (!entry.ReferenceChargeWarnings) {
            entry.ReferenceChargeWarnings = [];
        }

        if (entry.IsValid) {
            hasValidEntry = true;
            minAtomCount = Math.min(atomCount, minAtomCount);
            maxAtomCount = Math.max(atomCount, maxAtomCount);
            entry.color = entry.Warnings.length > 0 || entry.ReferenceChargeWarnings.length > 0 ? "#DB890F" : "blue";
            validCount++;
        } else {
            entry.color = "red";
        }
        
        entry.warningCount = entry.Warnings.length;
        entry.refChargeCount = entry.ReferenceChargeFilenames.length + (entry.HasBuiltInReferenceCharges ? 1 : 0);
        entry.refChargeString = (entry.HasBuiltInReferenceCharges ? "From input molecule (MOL2 or PQR)" : "") + (entry.refChargeCount > 1 ? "; " : "") + entry.ReferenceChargeFilenames.join("; ");
        entry.refChargeWarningCount = entry.ReferenceChargeWarnings.length;
        entry.refChargeWarningString = entry.ReferenceChargeWarnings.join("; ");
    }

    viewModel.hasValidEntry = hasValidEntry;
    viewModel.requiredAtomTypes = requiredAtomTypes;
    viewModel.minAtomCount = minAtomCount;
    viewModel.maxAtomCount = maxAtomCount;
    viewModel.validCount = validCount;

    for (var key in result.ParameterSets) {
        if (!result.ParameterSets.hasOwnProperty(key)) {
            continue;
        }
        prepareSet(result.ParameterSets[key], requiredAtomTypes);
    }

    result.ParameterSets.sort(function (a, b) {
        return a.priorityValue - b.priorityValue;
    });
    
    var usableSets = result.ParameterSets.filter(isSetUsableFilter);
    viewModel.noSetAvailable = usableSets.length === 0;

    viewModel.defaultSet = usableSets.length > 0 && validCount > 0 ? usableSets[0] : null;

    if (viewModel.defaultSet) {
        viewModel.defaultSet.sel(true);
    }
}

function renderAtomCountsCell(row, cell, value, columnDef, dataContext) {
    return dataContext['atomCountHtml'];
}

function showStructureDetails(e, id) {
    e.preventDefault();
    configViewModel.currentStructure(configViewModel.structures.view.getItemById(id));
    $("#structureDetailsModal").modal();
}

function removeStructure(e, id) {
    var s = configViewModel.structures.view.getItemById(id);
    s.isRemoved = true;
    configViewModel.structures.view.setFilter(function (s) { return !s.isRemoved; });
    configViewModel.showRefreshStructures(true);
    configViewModel.removedStructures[id] = true;
    configViewModel.updateCanCompute();

    var count = 0;
    _.forEach(configViewModel.structures.view.getItems(), function (ss) { if (!ss.isRemoved) count++; });
    configViewModel.computationSummary.structureCount(count);
}

function createSetDataLink(set) {
    var data = "data:text/plain;charset=UTF-8," + encodeURIComponent(set.Xml);
    var link = document.getElementById("viewSetDataLink");
    link.href = data;
}

function showParameterSetDetails(e, id) {
    e.preventDefault();
    var set = configViewModel.sets.view.getItemById(id);
    configViewModel.currentParameterSet(set);
}

function renderStructureRemoveCell(row, cell, value, columnDef, dataContext) {
    return "<button href='#' class='btn btn-link' title='Remove' style='padding: 0' onClick='javascript:removeStructure(event, \"" + dataContext.id + "\")'><i class='icon-remove'></i></button>";
}

function renderStructureIdCell(row, cell, value, columnDef, dataContext) {
    return "<a href='#' title='Click to show more info.' style='color:" + dataContext.color + "' onClick='javascript:showStructureDetails(event, \"" + dataContext.id + "\")'>" + dataContext.id + "</a>";
}

function renderStructureMessageCell(row, cell, value, columnDef, dataContext) {
    return "<a href='#' title='Click to show all messages.' style='color:" + dataContext.color + "' onClick='javascript:showStructureDetails(event, \"" + dataContext.id + "\")'>" + dataContext.message + "</a>";
}

function renderParameterSetIdCell(row, cell, value, columnDef, dataContext) {
    var tt = dataContext.approach + "; for " + dataContext.Target + "; by " + dataContext.Author;
    return "<a href='#' title='" + tt + "' style='color:blue' onClick='javascript:showParameterSetDetails(event, \"" + dataContext.id + "\")'>" + dataContext.id + "</a>";
}

function renderParameterSetMissingCell(row, cell, value, columnDef, dataContext) {
    if (!value) {
        return "<span style='color:#ccc'>-</span>";
    }
    return "<span class='charges-color-warning'>" + value + "</span>";
}

function totalChargeValidator(value) {
    return { valid: true, msg: null };
}

function createStructuresGrid(result) {
    "use strict";

    var gridOptions = {
        enableCellNavigation: true,
        enableColumnReorder: false,
        multiSelect: false,
        //explicitInitialization: true,
        forceFitColumns: true,
        editable: true,
        autoEdit: true
    };
        
    var structuresDataView = new Slick.Data.DataView();
    structuresDataView.setItems(result.Entries);

    $("#structuresView").height(Math.min(30 * 24 + 10, (result.Entries.length + 1) * 24 + 10));
    var structuresGrid = new Slick.Grid("#structuresView", structuresDataView, [], gridOptions);
    structuresDataView.onRowCountChanged.subscribe(function (e, args) {
        structuresGrid.updateRowCount();
        structuresGrid.render();
    });
    structuresDataView.onRowsChanged.subscribe(function (e, args) {
        structuresGrid.invalidateRows(args.rows);
        structuresGrid.render();
    });
    structuresGrid.setColumns([
        { id: "x", field: "id", name: "", sortable: false, width: 22, toolTip: "Remove.", formatter: renderStructureRemoveCell, resizable: false },
        { id: "id", field: "id", name: "Id", sortable: true, width: 100, toolTip: "Structure ID obtained from its filename.", formatter: renderStructureIdCell, resizable: false },
        { id: "atomCount", field: "atomCount", name: "#Atoms", sortable: true, width: 70, toolTip: "Atom count.", resizable: false },
        { id: "atoms", field: "atomCountHtml", name: "Atoms", sortable: true, width: 180, toolTip: "Atoms.", formatter: renderAtomCountsCell, resizable: false },
        { id: "totalCharge", field: "totalChargeString", name: "Total Charge", sortable: false, width: 100, toolTip: "Enter total charge for non-neutral molecules (multiple values can be separated by a comma).", cssClass: 'total-charge-cell', editor: SlickChargeTextEditor, validator: totalChargeValidator, resizable: false },
        { id: "msg", field: "message", name: "Message", toolTip: "Warnings/errors regarding potential issues.", formatter: renderStructureMessageCell, sortable: false }
        //{ id: "filler", field: "", name: "", sortable: false }
    ]);

    $("#structuresView").on('blur', 'input.editor-text', function () {
        Slick.GlobalEditorLock.commitCurrentEdit();
    });
    
    var sortCol = 'id';
    var sortComparer = function (a, b) {
        var x = a[sortCol], y = b[sortCol];
        return (x === y ? 0 : (x > y ? 1 : -1));
    };

    //structuresGrid.init();
    structuresGrid.onSort.subscribe(function (e, args) {
        sortCol = args.sortCol.field;
        structuresDataView.sort(sortComparer, args.sortAsc);
    });

    return {
        view: structuresDataView,
        grid: structuresGrid
    };
}

function selByTargetFilter(e, t) { return e.Target === t; }
function selByApproachFilter(e, t) { return e.groupKey === t; }

function selectSetBy(event, filter) {
    if (event) event.preventDefault();
    
    var name = $(event.target).data('target'),
        items = [], sel = false;
    //for (i = 0; i < view.getLength() ; i++) {
    //    it = view.getItem(i);
    //    if (!it["__group"]) {
    //        items.push(it);
    //    }
    //}

    //items = _.filter(items, function (e) { return filter(e, name); });

    //console.log(configViewModel.data);
    items = configViewModel.showOnlyUsefulSets()
        ? _.filter(configViewModel.data.ParameterSets, function (s) { return isSetUsableFilter(s); })
        : configViewModel.data.ParameterSets;
    items = _.filter(items, function (s) { return filter(s, name); })

    _.forEach(items, function (e) { if (e.sel()) { sel = true; return false; } return true; });
    sel = !sel;
    _.forEach(items, function (e) { e.sel(sel); });
    //configViewModel.sets.grid.invalidateAllRows();
    //configViewModel.sets.grid.render();
    configViewModel.sets.selector.update();
}

function createSetsGrid(result) {
    "use strict";

    var gridOptions = {
        enableCellNavigation: false,
        enableColumnReorder: false,
        //multiSelect: false,
        //explicitInitialization: true,
        forceFitColumns: true
    };

    var groupItemMetadataProvider = new Slick.Data.GroupItemMetadataProvider();
    var setsDataView = new Slick.Data.DataView({ groupItemMetadataProvider: groupItemMetadataProvider });
    setsDataView.setItems(result.ParameterSets);

    var btnFunc = function (val, isTarget) {
        var func = isTarget ? "selByTargetFilter" : "selByApproachFilter",
            onClick = "onclick='javascript:selectSetBy(event," + func + ")'";
        return " <button " + onClick + " data-target='" + val + "' class='btn btn-small btn-link' style='padding: 0;position: absolute; right: 5px' title='Toggle selection'>All/None</button> ";
    };

    setsDataView.setGrouping([{
        getter: "Target",
        formatter: function (g) {
            if (g.count > 1) {
                return btnFunc(g.value, true) + "<span class='slick-group-toggle'><b class='slick-group-toggle'>Target:</b> " + g.value + " [" + g.count + " sets]</span>";
            }
            return btnFunc(g.value, true) + "<span class='slick-group-toggle'><b class='slick-group-toggle'>Target:</b> " + g.value + " [" + g.count + " set]</span>";
        }
    },
    {
        getter: "approach",
        formatter: function (g) {
            if (g.count > 1) {
                return btnFunc(g.rows[0].groupKey, false) + "<span class='slick-group-toggle'><b class='slick-group-toggle'>Approach:</b> " + (g.value) + " [" + g.count + " sets]</span>";
            }
            return btnFunc(g.rows[0].groupKey, false) + "<span class='slick-group-toggle'><b class='slick-group-toggle'>Approach:</b> " + (g.value) + " [" + g.count + " set]</span>";
        }
    }]);

    //$("#setsView").height(Math.min(10 * 24 + 10, (result.ParameterSets.length + 1) * 24 + 10));
    var selector = new Slick.ChargesCheckboxSelectColumn(setsDataView, { cssClass: "slick-cell-checkboxsel" });
    var setsGrid = new Slick.Grid("#setsView", setsDataView, [], gridOptions);
    groupItemMetadataProvider.init(setsGrid);
    setsGrid.setSelectionModel(new Slick.RowSelectionModel({ selectActiveRow: false }));
    setsGrid.registerPlugin(selector);
    setsDataView.onRowCountChanged.subscribe(function (e, args) {
        setsGrid.updateRowCount();
        setsGrid.render();
    });
    setsDataView.onRowsChanged.subscribe(function (e, args) {
        setsGrid.invalidateRows(args.rows);
        setsGrid.render();
    });
    setsGrid.setColumns([
        selector.getColumnDefinition(),
        //{ id: "offset", field: "", name: "", sortable: false, width: 40, resizable: false },
        { id: "id", field: "id", name: "Id", sortable: false, width: 160, toolTip: "Set name.", resizable: false, formatter: renderParameterSetIdCell },
        { id: "priority", field: "priorityValue", name: "Priority", sortable: false, width: 50, toolTip: "Priority. Sets with lower priority are recommended.", resizable: false },
        { id: "missingAtoms", field: "missingParamsString", name: "Missing Atom Parameters", sortable: false, width: 100, toolTip: "Parameters for these atom types are not available.", resizable: false, formatter: renderParameterSetMissingCell },
        { id: "atoms", field: "availableAtomsString", name: "Atoms", sortable: false, width: 150, toolTip: "Atom types available in the set.", resizable: false },
        { id: "filler", field: "", name: "", sortable: false }
    ]);
    setsGrid.onSelectedRowsChanged.subscribe(function () {
        //var selectedSets = getSelected();

        //configViewModel.computationSummary.selectedSetCount(selectedSets.length);
        //var names = _.map(selectedSets, function (set) { return "<b>" + set.Name + "</b> <small style='color: #777'>" + set.approach + " for " + set.Target + "</small>"; });
        //if (names.length > 0) {
        //    configViewModel.computationSummary.selectedSetsString(names.join("<span style='color: #777'>; </span>"));
        //} else {
        //    configViewModel.computationSummary.selectedSetsString("None");
        //}
    });



    //structuresGrid.init();
    //detailsGrid.onSort.subscribe(function (e, args) {
    //    sortCol = args.sortCol.field;
    //    dataView.sort(comparer, args.sortAsc);
    //});

    var filtered = false;
    var filterUsable = function (showOnlyUsable) {
        filtered = showOnlyUsable;
        if (showOnlyUsable) {
            //var selected = setsGrid.getSelectedRows();
            setsGrid.setSelectedRows([]);
            setsGrid.invalidateAllRows();
            setsDataView.setFilter(isSetUsableFilter);
            var current = configViewModel.currentParameterSet();
            var filtered = setsDataView.getItems().filter(isSetUsableFilter);
            if (filtered.length === 0 || !isSetUsableFilter(current)) {
                configViewModel.currentParameterSet({});
            }
        } else {
            //var selected = setsGrid.getSelectedRows();
            setsGrid.invalidateAllRows();
            setsDataView.setFilter(function (set) { return true; });
        }
    };

    var getSelected = function () {
        var selectedData = _.filter(result.ParameterSets, function (set) { return set.sel(); });
        return selectedData;
    };

    var addSet = function (set) {
        //var wasFiltered = filtered;
        setsDataView.addItem(set);

        var ind = _.findIndex(setsDataView.getRows(), function (r) { return r === set });
        if (ind >= 0) {
            setsGrid.scrollRowIntoView(ind + 1);
        }
    };
    
    return { 
        view: setsDataView,
        grid: setsGrid,
        filterUsable: filterUsable,
        getSelected: getSelected,
        addSet: addSet,
        selector: selector
    };
}

function ConfigViewModel(result) {
    "use strict";

    var self = this;

    function updateSummaryWhenSetSelectionChanges() {
        if (!self.computationSummary) return;

        var selectedSets = _.filter(result.ParameterSets, function (set) { return set.sel(); });

        self.computationSummary.selectedSetCount(selectedSets.length);
        var names = _.map(selectedSets, function (set) { return "<b>" + set.Name + "</b> <small style='color: #777'>" + set.approach + " for " + set.Target + "</small>"; });
        if (names.length > 0) {
            self.computationSummary.selectedSetsString(names.join("<span style='color: #777'>; </span>"));
        } else {
            self.computationSummary.selectedSetsString("None");
        }
    }

    updateSummaryWhenSetSelectionChangesHandlerGlobal = _.debounce(updateSummaryWhenSetSelectionChanges, 50);
    
    prepareEntriesAndSets(result, this);


    this.data = result;
    this.currentStructure = ko.observable({});
    this.currentParameterSet = ko.observable({});
    this.showOnlyUsefulSets = ko.observable(false);
    this.showAllSets = ko.observable(true);
    this.showDefaultExecute = ko.observable(false);

    this.globalTotalCharge = ko.observable("");
    this.tooManyTotalChargesMessage = ko.observable("");

    this.computationSummary = {
        structureCount: ko.observable(result.Entries.length),
        selectedSetCount: ko.observable(0),
        selectedSetsString: ko.observable("None"),
        methodCount: ko.observable(0),
        jobCount: ko.observable(0)
    };

    function updateJobCount() {
        var totals = 0;
        _.forEach(result.Entries, function (e) {
            if (!e.isRemoved && e.totalChargeString) totals += Math.max(e.totalChargeString.split(",").length, 1);
        });
        self.computationSummary.jobCount(totals * self.computationSummary.selectedSetCount() * self.computationSummary.methodCount());
    }

    this.currentParameterSet.subscribe(function (set) {
        createSetDataLink(set);
    });
    
    this.showOnlyUsefulSets.subscribe(function (val) {
        self.sets.filterUsable(val);
    });
    
    this.applyGlobalTotalCharge = function () {
        var charge = self.globalTotalCharge();
        if (charge.length === 0) {
            updateCanCompute();
            return;
        }
        for (var key in result.Entries) {
            if (!result.Entries.hasOwnProperty(key)) continue;
            result.Entries[key].totalChargeString = charge;
        }
        self.structures.view.refresh();
        self.structures.grid.invalidate();
        self.structures.grid.render();

        updateCanCompute();
    };

    var setGroupsExpanded = true;
    this.toggleSetGroups = function () {
        if (setGroupsExpanded) {
            self.sets.view.collapseAllGroups();
        } else {
            self.sets.view.expandAllGroups();
        }
        setGroupsExpanded = !setGroupsExpanded;
    };

    this.addParameterSet = function() {
        self.addParameterSetModel.startAddSet();
    };

    this.addComputationMethod = function () {
        self.methodsViewModel.addMethod();
    };

    this.debugConfig = function () {
        var config = buildConfig(self);
        console.log(config);
    };

    this.removedStructures = {};
    this.showRefreshStructures = ko.observable(false);
    this.refreshStructures = function () {
        _.forEach(result.Entries, function (e) { e.isRemoved = false; });
        self.removedStructures = {};
        self.structures.view.setFilter(function (s) { return !s.isRemoved; });
        self.showRefreshStructures(false);
        self.computationSummary.structureCount(result.Entries.length);
        updateCanCompute();
    };

    this.compute = function () {
        keepDefaultExecute = true;
        updateCanCompute();
        keepDefaultExecute = false;

        if (!self.canCompute()) {
            return;
        }

        var config = buildConfig(self);
        self.canCompute(false);
        self.computeButtonLabel("Uploading computation data...");
        $.ajax({
            type: "POST",
            url: ChargeCalculatorParams.computeAction,
            data: JSON.stringify({ 'config': JSON.stringify(config) }),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (result) {
            if (result["ok"]) {
                checkExit = false;
                self.computeButtonLabel("Executing computation...");
                location.reload();
            } else {
                self.canCompute(true);
                self.computeButtonLabel("Compute");
                alert(result["message"]);
            }
        }).fail(function (x, y, message) {
            self.canCompute(true);
            self.computeButtonLabel("Compute");
            alert(message);
        });
    };

    this.computeButtonLabel = ko.observable("Compute");
    this.canCompute = ko.observable(false);

    _.forEach(this.data.Entries, function (e) {
        e.totalChargeChanged = function () {
            updateCanCompute();
        };
    });

    var validateTotalChangesCounts = function () {
        var entries = self.data.Entries;
        for (var ei in entries) {
            if (!entries.hasOwnProperty(ei)) {
                continue;
            }

            var entry = entries[ei];
            var isValid = true;
            var count = 0;
            _.forEach(entry.totalChargeString.split(","), function (cs) {
                var cv = +cs;
                if (isNaN(cv)) {
                    isValid = false;
                }
                count++;
            });

            if (!isValid) {
                return { ok: false, message: "'" + entry.StructureId + "': '" + entry.totalChargeString + "' is not a valid list of numbers for Total Charge(s)." };
            }

            if (count > 3) {
                return { ok: false, message: "'" + entry.StructureId + "': at most 3 values of total charge can be specified at the same time." };
            }
        }

        return { ok: true, message: "" };
    };
    
    var keepDefaultExecute = false;
    var updateCanCompute = function (keepDefault) {

        if (!keepDefaultExecute) {
            self.showDefaultExecute(false);
        }

        var totalCountValidation = validateTotalChangesCounts();

        if (!totalCountValidation.ok) {
            self.tooManyTotalChargesMessage(totalCountValidation.message);
        } else {
            self.tooManyTotalChargesMessage("");
        }

        if (!self.hasValidEntry) {
            self.computeButtonLabel("The are no valid molecules to compute charges on.");
            self.canCompute(false);
            return;
        }

        if (_.keys(self.removedStructures).length === result.Entries.length) {
            self.computeButtonLabel("Nothing to compute (refresh molecule list).");
            self.canCompute(false);
            return;
        }

        if (self.computationSummary.selectedSetCount() === 0) {
            self.computeButtonLabel("Select at least one parameter set.");
            self.canCompute(false);
            return;
        }
        if (self.computationSummary.methodCount() === 0) {
            self.computeButtonLabel("Add at least one computation method.");
            self.canCompute(false);
            return;
        }
        if (self.maxAtomCount > maxFullEemCount && self.methodsViewModel.isFullEemSelected()) {
            self.computeButtonLabel("Full EEM method cannot be used for molecules with more than " + maxFullEemCount + " atoms. Please select a different method.");
            self.canCompute(false);
            return;
        }

        if (!totalCountValidation.ok) {
            self.computeButtonLabel(totalCountValidation.message);
            self.canCompute(false);
            return;
        }

        updateJobCount();
        self.computeButtonLabel("Compute");
        self.canCompute(true);
    };
    this.updateCanCompute = updateCanCompute;

    updateCanCompute();
    
    self.computationSummary.selectedSetCount.subscribe(updateCanCompute);
    self.computationSummary.methodCount.subscribe(updateCanCompute);        
}

function setExampleParams(result) {
    if (configViewModel.defaultSet) {
        configViewModel.defaultSet.sel(false);
        configViewModel.defaultSet = null;
    }
    
    var selectedSets = {};
    _.forEach(ChargesExample.parameters, function (s) { selectedSets[s] = true; });
    _.forEach(result.ParameterSets, function (set) {
        if (selectedSets[set.Name]) {            
            set.sel(true);
            if (!configViewModel.defaultSet) {
                configViewModel.defaultSet = set;
            }
        }
    });

    _.forEach(result.Entries, function (entry) {
        var charges = ChargesExample.totalCharges[entry.id.toLowerCase()];
        if (!charges) return;
        entry.totalChargeString = charges.join(",");
    });
}

function setExampleMethods(vm) {
    vm.clearMethods();
    
    _.forEach(ChargesExample.methods, function (m) {
        vm.selectMethod(vm.methodMap[m.method]);
        vm.selectRadius(m.cutoff || 12.0);
        vm.selectIgnoreWaters = !!m.ignoreWaters;
        vm.selectPrecision(vm.precisionMap[m.precision || "Double"]);
        vm.addNewMethod();
    });
}

function showConfig(result) {
    "use strict";

    if (window['ChargesExample']) {
        _.forEach(ChargesExample.customSets, function (set) {
            result.ParameterSets.push(set);
        });
    }

    configViewModel = new ConfigViewModel(result);
    if (window['ChargesExample']) {
        setExampleParams(result);
    }

    mainViewModel.template({ name: "config-template", data: configViewModel });
    $("#mainView").find(".btn").tooltip();
    configViewModel.sets = createSetsGrid(result);
    configViewModel.structures = createStructuresGrid(result);
    configViewModel.methodsViewModel = new MethodConfigViewModel(configViewModel, "methodsView", "addMethodModal");
    configViewModel.showOnlyUsefulSets(true);

    if (window['ChargesExample']) {
        setExampleMethods(configViewModel.methodsViewModel);
    }

    ko.applyBindings(configViewModel, document.getElementById("structureDetailsModal"));
        
    if (window['ChargesExample']) {
    } else {
        if (configViewModel.validCount === 0) {
            configViewModel.showAllSets(false);
        } else {
            if (configViewModel.defaultSet) {
                configViewModel.showAllSets(false);
            } else {
                configViewModel.showOnlyUsefulSets(false);
            }
        }

    }

    if (configViewModel.defaultSet) {
        configViewModel.currentParameterSet(configViewModel.defaultSet);
    }

    var setGroupsExpanded = true;

    $("#viewSetDataLink").click(function (e) {
        var currentSet = configViewModel.currentParameterSet();
        if (!currentSet.Xml) {
            e.preventDefault();
        }
    });

    $("#resultUrl").click(function () {
        $(this).select();
    });

    $("#defaultSetCheck").change(function () {
        if (!configViewModel.defaultSet) {
            return;
        }
        
        var grid = configViewModel.sets.grid;
        grid.setSelectedRows($.grep(grid.getSelectedRows(), function (n) {            
            return grid.getDataItem(n)["Name"] !== configViewModel.defaultSet.Name;
        }));

        //for (var i = 0; i < configViewModel.sets.grid.getDataLength() ; i++) {
        //    if (configViewModel.sets.grid.getDataItem(i)["Name"] === configViewModel.defaultSet.Name) {
        //        configViewModel.sets.grid.invalidateRow(i);
        //        break;
        //    }
        //}
        //configViewModel.sets.grid.render();
    });

    configViewModel.setEditorCode = CodeMirror.fromTextArea(document.getElementById("setXmlEditor"), { mode: "text/xml", lineNumbers: true });
    configViewModel.addParameterSetModel = new SetEditorViewModel(result.EmptySetTemplate.Xml, configViewModel);

    $("#guideBtn").show().click(function () { startConfigIntro(); });

    if (configViewModel.canCompute()) {
        configViewModel.showDefaultExecute(true);
    }
}

ko.bindingHandlers.executeOnEnter = {
    init: function (element, valueAccessor, allBindingsAccessor, viewModel) {
        var allBindings = allBindingsAccessor();
        $(element).keypress(function (event) {
            var keyCode = (event.which ? event.which : event.keyCode);
            if (keyCode === 13) {
                allBindings.executeOnEnter.call(viewModel, element);
                return false;
            }
            return true;
        });
    }
};

$(function () {
    var computationWrapper = StatusAndResultWrapper({
        mainViewModel: mainViewModel,
        isFinished: ChargeCalculatorParams.isFinished,
        statusAction: ChargeCalculatorParams.statusAction,
        resultAction: ChargeCalculatorParams.dataAction,
        onResult: showConfig
    });
    ko.applyBindings(mainViewModel, document.getElementById("mainView"));
    computationWrapper.run();
});