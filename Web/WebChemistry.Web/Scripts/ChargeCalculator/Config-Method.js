
function MethodDescriptor(method, radius, ignoreWaters, precision) {
    this.method = method;
    this.name = method.name;
    this.radius = radius;
    this.ignoreWaters = ignoreWaters;
    this.precision = precision;
    if (this.method.value === "Eem") {
        this.id = method.value + (ignoreWaters ? "_wtr" : "") + "_" + precision.value;
        this.parametersString = "Ignore Waters = " + ignoreWaters + ", Precision = " + precision.value;
    } else {
        this.id = method.value + radius.toString() + (ignoreWaters ? "_wtr" : "") + "_" + precision.value;
        this.parametersString = "Radius = " + radius + ", Ignore Waters = " + ignoreWaters + ", Precision = " + precision.value;
    }
}

var methodConfigVM;

function removeComputaionMethod(e, id) {
    e.preventDefault();
    methodConfigVM.removeMethod(id);
}

function MethodConfigViewModel(configVM, gridElement, addMethodModalElement) {
    var summary = configVM.computationSummary;

    this.availableMethods = [
        { name: "Full EEM", value: "Eem", wikiLink: "ChargeCalculator:Theoretical_background", description: "Solves the full EEM matrix. Recommended for systems with up to 10000 atoms (with 30000 atoms limit due to memory constraints)." },
        { name: "EEM Cutoff", value: "EemCutoff", wikiLink: "ChargeCalculator:Theoretical_background", description: "Solves a smaller EEM matrix for each atom. Accuracy determined by Cutoff Radius (use at least 8). Recommended for systems with up to 100000 atoms." },
        { name: "EEM Cutoff Cover", value: "EemCutoffCover", wikiLink: "ChargeCalculator:Theoretical_background", description: "Solves a smaller EEM matrix for about 25% of atoms and interpolates the values for the rest. Accuracy determined by Cutoff Radius (use at least 10). No limitation of system size." },
    ];


    this.availablePrecisions = [{ value: "Double", description: "Uses 64-bit numbers to represent charges." }, { value: "Single", description: "Uses 32-bit numbers to represent charges. Faster but less precise." }];

    this.methodMap = _.indexBy(this.availableMethods, 'value');
    this.precisionMap = _.indexBy(this.availablePrecisions, 'value');

    var self = this;
    this.selectMethod = ko.observable(this.availableMethods[2]);
    this.selectRadius = ko.observable(12);
    this.selectIgnoreWaters = false;
    this.selectPrecision = ko.observable(this.availablePrecisions[0]);
    this.addError = ko.observable("");
    this.isValid = ko.observable(true);

    this.selectMethod.subscribe(validateMethod);
    this.selectRadius.subscribe(validateMethod);
    
    var defaultMethod = configVM.maxAtomCount <= maxFullEemCount
        ? new MethodDescriptor(this.availableMethods[0], 0, false, this.availablePrecisions[0])
        : new MethodDescriptor(this.availableMethods[2], 12, false, this.availablePrecisions[0]);
    var dataView = new Slick.Data.DataView();
    this.dataView = dataView;
    dataView.setItems([defaultMethod]);
    var grid = new Slick.Grid("#" + gridElement, dataView, [], { enableCellNavigation: false, forceFitColumns: true, enableColumnReorder: false });
    dataView.onRowCountChanged.subscribe(function (e, args) { grid.updateRowCount(); grid.render(); });
    dataView.onRowsChanged.subscribe(function (e, args) { grid.invalidateRows(args.rows); grid.render(); });

    function actionCellFormatter(row, cell, value, columnDef, dataContext) {
        return "<a href='#' title='Remove' onClick='javascript:removeComputaionMethod(event, \"" + dataContext.id + "\")'><i class='icon icon-remove'></i></a>";
    }

    grid.setColumns([
        { id: "actions", field: "", name: "", sortable: false, width: 26, minWidth: 26, maxWidth: 26, resizable: false, formatter: actionCellFormatter },
        { id: "method", field: "name", name: "Method", sortable: false, width: 280, resizable: false },
        { id: "params", field: "parametersString", name: "Options", sortable: false },
        //{ id: "filler", field: "", name: "", sortable: false }
    ]);

    methodConfigVM = this;

    summary.methodCount(1);

    function validateMethod() {
        var method = self.selectMethod();
        if (method.value !== "Eem") {
            var rad = +self.selectRadius();
            if (isNaN(rad) || (rad) > 17) {
                self.addError("Radius must be a number <= 17.");
                self.isValid(false);
                return false;
            }
        }
        self.isValid(true);
        self.addError("");
        return true;
    }

    this.addNewMethod = function () {
        if (!validateMethod()) {
            return;
        }

        var method = new MethodDescriptor(self.selectMethod(), self.selectRadius(), self.selectIgnoreWaters, self.selectPrecision());
        if (dataView.getItemById(method.id)) {
            self.addError("A method with these parameters has already been added.");
            return;
        }

        dataView.addItem(method);
        grid.scrollRowIntoView(dataView.getLength());
        summary.methodCount(dataView.getLength());
        $("#addMethodModal").modal("hide");
    };

    this.removeMethod = function (id) {
        dataView.deleteItem(id);
        summary.methodCount(dataView.getLength());
    };

    this.clearMethods = function () {
        dataView.setItems([]);
        summary.methodCount(0);
    }

    this.addMethod = function () {
        $("#" + addMethodModalElement).modal();
    };

    this.isFullEemSelected = function () {
        for (var i = 0; i < dataView.getLength() ; i++) {
            if (dataView.getItem(i).method.value === "Eem") {
                return true;
            }
        }
        return false;
    };

    this.getMethods = function () {
        var items = [];
        for (var i = 0; i < dataView.getLength() ; i++) {
            items.push(dataView.getItem(i));
        }
        return items;
    };

    ko.applyBindings(this, document.getElementById(addMethodModalElement));
}

function buildConfig(configViewModel) {
    var sets = [];
    var setsXml = [];
    var jobs = [];

    var selectedSets = configViewModel.sets.getSelected();
    var methods = configViewModel.methodsViewModel.getMethods();
    
    for (var si in selectedSets) {
        var set = selectedSets[si];
        setsXml.push(set.Xml);
        for (var mi in methods) {
            var method = methods[mi];
            sets.push({
                "Name": set.Name,
                "Method": method.method.value,
                "CutoffRadius": method.method.value === "Eem" ? 0.0 : parseFloat(method.radius),
                "CorrectCutoffTotalCharge": true,
                "Precision": method.precision.value,
                "IgnoreWaters": method.ignoreWaters
            });
        }
    }

    var entries = configViewModel.data.Entries;
    for (var ei in configViewModel.data.Entries) {
        if (!entries.hasOwnProperty(ei)) {
            continue;
        }

        var entry = entries[ei];
        if (!entry.IsValid || entry.isRemoved) {
            continue;
        }

        var charges = [];
        _.forEach(entry.totalChargeString.split(","), function (cs) {
            var cv = parseFloat(cs);
            if (!isNaN(cv)) {
                charges.push(cv);
            }
        });

        if (charges.length === 0) {
            charges = [0];
        }

        if (charges.length > 0) {
            jobs.push({ Id: entry.StructureId, TotalCharges: charges });
        }
    }

    return {
        "Sets": sets,
        "Jobs": jobs,
        "SetsXml": setsXml
    };
}

function SetEditorViewModel(defaultXml, configViewModel) {
    var setBlank = true;
    var self = this;
    
    var xhr = null;

    this.startAddSet = function () {
        self.addEnabled(true);
        self.addLabel("Add");
        self.addError("");

        if (setBlank) {
            configViewModel.setEditorCode.doc.setValue(defaultXml);
            setBlank = false;
        }
        $("#addSetModal").modal();
        configViewModel.setEditorCode.refresh();
    };

    var addValidated = function (sets) {
        if (sets.length === 0) {
            setBlank = true;
            $("#addSetModal").modal("hide");
            return;
        }

        var names = {};
        _.forEach(sets, function (set) { set.Name = set.Name.trim(); names[set.Name.toLowerCase()] = true; });
                
        self.addEnabled(true);
        self.addLabel("Add");
        
        var conflictIndex = _.findIndex(configViewModel.data.ParameterSets, function (s) { return names[s.Name.toLowerCase()] === true; });
        if (conflictIndex >= 0) {
            self.addEnabled(true);
            self.addLabel("Add");
            self.addError("A set named '" + configViewModel.data.ParameterSets[conflictIndex].Name + "' already exists.");
            return;
        }
        
        _.forEach(sets, function (set) {
            prepareSet(set, configViewModel.requiredAtomTypes);
            configViewModel.sets.addSet(set);
        });
        setBlank = true;
        configViewModel.currentParameterSet(sets[0]);
        $("#addSetModal").modal("hide");
    };

    this.cancel = function () {
        if (xhr) {
            xhr.abort();
            xhr = null;
        }
    };

    this.addSet = function () {
        self.addEnabled(false);
        self.addLabel("Validating...");

        xhr = $.ajax({
            type: "POST",
            url: ChargeCalculatorParams.validateSetAction,
            data: JSON.stringify({ 'xml': configViewModel.setEditorCode.doc.getValue() }),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (result) {
            xhr = null;
            if (result["ok"]) {
                addValidated(result.sets);
            } else {
                self.addEnabled(true);
                self.addLabel("Add");
                self.addError(result.message);
            }
        }).fail(function (x, y, message) {
            self.addEnabled(true);
            self.addLabel("Add");
            self.addError(message);
        }).complete(function () { xhr = null; });
    };

    this.addError = ko.observable("");
    this.addLabel = ko.observable("Add");
    this.addEnabled = ko.observable(true);

    ko.applyBindings(this, document.getElementById("addSetModal"));
}