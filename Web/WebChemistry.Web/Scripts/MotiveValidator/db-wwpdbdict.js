var minorBondTolerance = 0.1;

var auditPlotColumns = [
    { field: "Ok", name: "Without Problem", tooltip: "Number of structures without a problem.", color: Highcharts.Color('#a6c96a').brighten(-0.25).get() },

    { field: "WrongBondLength_Model_Minor", name: "Minor Bond Length (Model)", tooltip: "Number of structures that fall slightly (" + minorBondTolerance.toString() + " ang) out of the 'usual' range.", color: '#a6c96a' },
    { field: "WrongBondLength_Model", name: "Bond Length (Model)", tooltip: "Number of structures with unusual bond lengths.", color: '#c42525' },

    { field: "WrongBondLength_Ideal_Minor", name: "Minor Bond Length (Ideal)", tooltip: "Number of structures that fall slightly (" + minorBondTolerance.toString() + " ang) out of the 'usual' range.", color: '#a6c96a' },
    { field: "WrongBondLength_Ideal", name: "Bond Length (Ideal)", tooltip: "Number of structures with unusual bond lengths.", color: '#c42525' },

    { field: "HasChiralAtoms", name: "Has Chiral Atoms", tooltip: "Number of structures with chiral atoms<br/>(only structures with all available coordinates are considered).", color: '#00CCCC' },
    { field: "MissingChiralAtoms", name: "Missing Chiral Atoms", tooltip: "Number of structures with missing chiral atoms<br/>(only structures with all available coordinates are considered).", color: "#BF3D3D" },
    { field: "ExtraChiralAtoms", name: "Extra Chiral Atoms", tooltip: "Number of structures with extra chiral atoms<br/>(only structures with all available coordinates are considered).", color: "#BC5353" },

    { field: "MissingCoordinateModel", name: "Missing Coordinate (Model)", tooltip: "Number of structures with missing model coordinate(s).", color: '#B5E8FF' },
    { field: "MissingCoordinateIdeal", name: "Missing Coordinate (Ideal)", tooltip: "Number of structures with missing ideal coordinate(s).", color: '#B5E8FF' },
];

function WwPdbDictViewModel() {
    "use strict";

    var self = this;
    var initialized = false;

    this.isBusy = ko.observable(false);
        
    var spinner;    
    function showResult(result) {
        self.isBusy(false);
        try {
            var map = {};
            self.bondRanges = result.BondRanges;
            self.audit = new WwPdbDictAuditViewModel(result, self);
            
            ko.applyBindings({ lastUpdated: result.AuditDate, modelCount: result.Entries.Length }, document.getElementById('audit-summary'));

            $('a[data-toggle="tab"]').on('shown', function (e) {
                if ($(e.target).attr('href') === "#wwpdbdict-details") {
                    self.audit.details.init("#wwpdbdict-details-view");
                } 
            });

            $("#wwpdbdict-summary-plot").highcharts({
                title: { text: '' },
                chart: { type: 'bar' },
                tooltip: { pointFormat: '<b>{point.seriesName}</b><br/>{point.tooltip}<br/><b>{point.value} of {point.total} ({point.y:.2f}%)</b><br/>' },
                xAxis: { categories: auditPlotColumns.map(function (c) { return c.name; }) },
                yAxis: { min: 0, max: 100, title: { text: '% of structures' }, labels: { enabled: true } },
                series: [ self.audit.series ],
                legend: { enabled: false },
                exporting: { enabled: false },
                credits: { enabled: false },
            });
        } catch (e) {
            showError(e.message);
        }
    }

    function showError(error) {
        console.log(error);
        self.isBusy(true);
        $("#wwpdbdict-overlay").text(error);
    }


    var bondKeyPattern = /^(\S+) (\S+) (\S+)-(\S+) (\S+) (\S+)$/;
    this.getBondRange = function (key, length) {
        var match = key.match(bondKeyPattern);
        var a = match[2], b = match[5], type = length.Type;
        var min, max, x, y, z;
        
        if ((x = self.bondRanges[a]) && (y = x[b]) && (z = y[type])) {
            return { min: z.Min, max: z.Max };
        } else if ((x = self.bondRanges[b]) && (y = x[a]) && (z = y[type])) {
            return { min: z.Min, max: z.Max };
        } else {
            return undefined;
        }
    };

    this.testMinorError = function (key, length) {
        var match = key.match(bondKeyPattern);
        var a = match[2], b = match[5], type = length.Type;
        var min, max, x, y, z;

        if ((x = self.bondRanges[a]) && (y = x[b]) && (z = y[type])) {
            min = z.Min; max = z.Max;
        } else if ((x = self.bondRanges[b]) && (y = x[a]) && (z = y[type])) {
            min = z.Min; max = z.Max;
        } else {
            return false;
        }

        if (length.Length <= min) {
            return min - length.Length <= minorBondTolerance;
        }
        return length.Length - max <= minorBondTolerance;
    }

    this.init = function () {
        if (initialized) {
            return;
        }

        initialized = true;

        ko.applyBindings({ isBusy: self.isBusy }, document.getElementById("wwpdbdict-overlay"));
        ko.applyBindings(auditEntryDetailsModel, document.getElementById("auditDetailsModal"));

        self.isBusy(true);
        spinner = new Spinner({ hwaccel: true, radius: 25, length: 15, width: 8, lines: 13 }).spin(document.getElementById("wwpdbdict-overlay"));
        $.ajax({
            url: ValidatorDbParams.auditAction,
            type: 'GET',
            dataType: 'json',
            cache: false
        })
        .done(function (data) { showResult(data); })
        .fail(function (jqXHR, textStatus, errorThrown) { console.log(errorThrown); showError("Downloading audit failed. Try refreshing the page."); })
        .always(function () { spinner.stop(); spinner = null; });
    };
}

function WwPdbDictAuditViewModel(audit, parent) {
    "use strict";
    var self = this;
    
    var summary = {
        "Ok": 0,
                
        "WrongBondLength_Model_Minor": 0,
        "WrongBondLength_Model": 0,
        "WrongBondLength_Ideal_Minor": 0,
        "WrongBondLength_Ideal": 0,
        
        "HasChiralAtoms": 0,
        "ExtraChiralAtoms": 0,
        "MissingChiralAtoms": 0,

        "MissingCoordinateModel": 0,
        "MissingCoordinateIdeal": 0,

        "NotConnected": 0
    };
        
    function sumBondEntry(entry, bonds, prefix, field) {
        var isOk = true;
        var unusualBondCount = 0, minorUnusualBondCount = 0;
        var isMinorError = true;
        var hasWrongLength = false;
        $.each(bonds, function (key, length) {
            if (!parent.testMinorError(key, length)) {
                isMinorError = false;
                unusualBondCount++;
            } else {
                minorUnusualBondCount++;
            }
            hasWrongLength = true;
        });
        if (hasWrongLength) {
            isOk = false;
            if (isMinorError) {
                summary[prefix + "_Minor"]++;
            } else {
                summary[prefix]++;
            }
        }
        entry[field] = unusualBondCount;
        entry[field + "_minor"] = minorUnusualBondCount;
        return isOk;
    }

    function sumEntry(entry) {
        var isOk = true;
        
        if (!sumBondEntry(entry, entry.UnusualBondsModel, "WrongBondLength_Model", "unusualModelBondCount")) {
            isOk = false;
        }
        if (!sumBondEntry(entry, entry.UnusualBondsIdeal, "WrongBondLength_Ideal", "unusualIdealBondCount")) {
            isOk = false;
        }
        
        entry.missingCoordinateIdealAtomCount = entry.MissingCoordinateIdealAtoms.length;
        if (entry.missingCoordinateIdealAtomCount > 0) {
            isOk = false;
            summary["MissingCoordinateIdeal"]++;
        }

        entry.missingCoordinateModelAtomCount = entry.MissingCoordinateModelAtoms.length;
        if (entry.missingCoordinateModelAtomCount > 0) {
            isOk = false;
            summary["MissingCoordinateModel"]++;
        }
        
        entry.chiralAtomCount = entry.ChiralAtoms.length;
        if (entry.chiralAtomCount > 0) {
            summary["HasChiralAtoms"]++;
        }

        entry.extraChiralAtomCount = entry.ExtraChiralAtoms.length;
        if (entry.extraChiralAtomCount > 0) {
            isOk = false;
            summary["ExtraChiralAtoms"]++;
        }

        entry.missingChiralAtomCount = entry.MissingChiralAtoms.length;
        if (entry.missingChiralAtomCount > 0) {
            isOk = false;
            summary["MissingChiralAtoms"]++;
        }

        if (!entry.IsConnected) {
            isOk = false;
            summary["NotConnected"]++;
        }
        
        entry.hasProblem = !isOk;

        if (isOk) {
            summary["Ok"]++;
        }
    };

    var entryMap = {};
    _.forEach(audit.Entries, function (e) {
        sumEntry(e);
        entryMap[e.Id] = e;
    });

    this.entryMap = entryMap;
    this.summary = summary;
    this.entries = audit.Entries;
    this.count = this.entries.length;
    this.id = audit.Id;
    this.name = "wwPDB CCD (mmCIF)";

    var seriesData = auditPlotColumns.map(function (c) {
        var value = summary[c.field];
        return { y: 100.0 * value / self.count, value: value, total: self.count, seriesName: self.name, tooltip: c.tooltip, color: c.color };
    });
        

    this.name = this.name;
    this.series = { name: self.name, data: seriesData };
    this.details = new WwPdbDictAuditDetailsViewModel(this);
}

function WwPdbDictAuditDetailsViewModel(auditModel) {
    "use strict";

    function transformColorFormatter(color, func) {
        return function (row, cell, value, columnDef, dataContext) {
            var val = func(dataContext);
            if (val) {
                return "<span style='color:" + color + "'>" + val + "</span>";
            } else {
                return "<span style='color:#aaa'>" + val + "</span>";
            }
        }
    }

    function transformColorFormatter(color, func) {
        return function (row, cell, value, columnDef, dataContext) {
            var val = func(dataContext);
            if (val) {
                return "<span style='color:" + color + "'>" + val + "</span>";
            } else {
                return "<span style='color:#aaa'>" + val + "</span>";
            }
        }
    }

    function transformFormatter(func) {
        return function (row, cell, value, columnDef, dataContext) {
            return func(dataContext);
        }
    }

    function makeColumn(field, name, tooltip, color, width, isInv) {
        if (width === undefined) width = 70;
        tooltip = "[" + name + "] " + tooltip;
        if (color) {
            return { id: field, field: field, name: name, sortable: true, width: width, toolTip: tooltip, resizable: false, formatter: colorValueFormatter(color, isInv), cssClass: "details-cell" };
        } else {
            return { id: field, field: field, name: name, sortable: true, width: width, toolTip: tooltip, resizable: false, cssClass: "details-cell" };
        }
    }

    function renderIdCell(row, cell, value, columnDef, dataContext) {
        //var href = '//ligand-expo.rcsb.org/reports/' + value.charAt(0).toUpperCase() + '/' + value.toUpperCase() + '/index.html';
        //return "<a href='" + href + "' style='color: blue' target='_blank'>" + value + "</a>";

        var color = dataContext.hasProblem ? "#DB890F" : "blue";
        var click = 'showAuditEntryDetails(event,"' + value + '")';
        return "<a href='#' onclick='" + click + "' style='color:" + color + "' target='_blank'>" + value + "</a>";
    }
    
    var columns = [
        { id: "Id", field: "Id", name: "Id", sortable: true, width: 70, toolTip: "Residue Name", resizable: false, formatter: renderIdCell, cssClass: "details-cell" },

        makeColumn("AtomCount", "Atom Count", "Total number of atoms in the structure.", "black"),
        makeColumn("chiralAtomCount", "Chiral Atom Count", "Total number of chiral atoms in the structure (only structures with all coordinates are considered).", "black"),

        makeColumn("ProblemCount", "Problems", "Total number of problems with the structure.", "red"),
        makeColumn("IsConnected", "Connected", "Determines if the structure is connected or not.", "red", 70, true),

        makeColumn("unusualModelBondCount", "Unusual Model Bonds", "Determines if the model structure has unusual bonds.", "red"),
        makeColumn("unusualModelBondCount_minor", "Unusual Model Bonds (Minor)", "Determines if the model structure has minor unusual bonds (" + minorBondTolerance.toString() + " ang tolerance).", "blue"),
        makeColumn("unusualIdealBondCount", "Unusual Ideal Bonds", "Determines if the model structure has unusual bonds.", "red"),
        makeColumn("unusualIdealBondCount_minor", "Unusual Ideal Bonds (Minor)", "Determines if the model structure has minor unusual bonds (" + minorBondTolerance.toString() + " ang tolerance).", "blue"),

        //makeColumn("overlappingAtomCount", "Overlapping Atoms", "Determines if the structure has overlapping atoms.", "red"),
        makeColumn("extraChiralAtomCount", "Extra Chiral Atoms", "Number of extra chiral atoms (only structures with all coordinates are considered).", "red"),
        makeColumn("missingChiralAtomCount", "Missing Chiral Atoms", "Number of missing chiral atoms (only structures with all coordinates are considered).", "red"),
        
        makeColumn("missingCoordinateModelAtomCount", "Missing Model Coordinates", "Number of missing model atom coordinates.", "red"),
        makeColumn("missingCoordinateIdealAtomCount", "Missing Ideal Coordinates", "Number of missing model atom coordinates.", "red")
        //makeColumn("wrongConnectivityCount", "Wrong Connectivity Atoms", "Atoms with wrong connectivity.", "red"),
    ];

    var self = this;
    var initialized = false;

    this.init = function (gridElement) {
        if (initialized) {
            return;
        }
        initialized = true;

        var sortCol = "";
        function sortComparer(a, b) {
            var x = a[sortCol], y = b[sortCol];
            return (x === y ? 0 : (x > y ? 1 : -1));
        }

        var dataView = new Slick.Data.DataView();
        dataView.setItems(auditModel.entries, "Id");

        var gridOptions = {
            enableCellNavigation: false,
            enableColumnReorder: false,
            multiSelect: false,
            forceFitColumns: true,
            editable: false
        };

        var grid = new Slick.Grid(gridElement, dataView, [], gridOptions);
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
            dataView.sort(sortComparer, args.sortAsc);
        });
        grid.setColumns(columns);

        function filterProvider(val) { var regex = new RegExp(val, "i"); return function (e) { return regex.test(e.Id); }; }
        
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
                dataView.setFilter(filterProvider(value));
            }, 500);
        });

        ko.applyBindings(filterModel, $(gridElement + "-filter").get(0));
    }
}

var auditEntryDetailsModel = {
    current: ko.observable()
};

function showAuditEntryDetails(e, entryId) {
    e.preventDefault();
    var entry = new AuditEntryDetailsView(wwPdbDictModel.audit.entryMap[entryId]);
    auditEntryDetailsModel.current(entry);
    $("#auditDetailsModal").modal();
}

function makeAuditHtmlFormula(formula) {
    try {
        var elem = /(?![{])([a-zA-Z]+?)(?![}])[{]([0-9]+?)[}]/g;
        var ret = "";
        var match = null;
        while (match = elem.exec(formula)) {
            if (parseInt(match[2]) > 1) {
                ret += match[1] + "<sub>" + match[2] + "</sub>";
            } else {
                ret += match[1];
            }
        }
        return ret;
    }
    catch (e) {
        //console.log(e.toString());
        return formula;
    }
}

function AuditEntryDetailsView(entry) {
    var self = this;

    this.audit = entry;
    this.color = entry.hasProblem ? "#DB890F" : "black";
    //this.href = '//ligand-expo.rcsb.org/reports/' + entry.Id.charAt(0).toUpperCase() + '/' + entry.Id.toUpperCase() + '/index.html';
    this.href = `//https://www.ebi.ac.uk/pdbe/static/files/pdbechem_v2/${entry.Id.toUpperCase()}_400.svg`;
    this.htmlFormula = makeAuditHtmlFormula(entry.SummaryFormula);
    
    this.unusualModelBonds = [];
    this.unusualModelBondsMinor = [];
    $.each(entry.UnusualBondsModel, function (key, value) {
        var range = wwPdbDictModel.getBondRange(key, value);
        var desc = { key: key, length: value.Length, type: value.Type, range: range }
        if (!desc.range) {
            desc.range = { min: 'n/a', max: 'n/a' };
            desc.error = 'n/a';
        } else {
            if (desc.length < desc.range.min) {
                desc.error = "-" + (desc.range.min - desc.length).toFixed(3).toString();
            } else if (desc.length > desc.range.max) {
                desc.error = "+" + (desc.length - desc.range.max).toFixed(3).toString();
            } else {
                desc.error = 'n/a';
            }
        }
        if (wwPdbDictModel.testMinorError(key, value)) {
            self.unusualModelBondsMinor.push(desc);
        } else {
            self.unusualModelBonds.push(desc);
        }
    });

    this.unusualIdealBonds = [];
    this.unusualIdealBondsMinor = [];
    $.each(entry.UnusualBondsIdeal, function (key, value) {
        var range = wwPdbDictModel.getBondRange(key, value);
        var desc = { key: key, length: value.Length, type: value.Type, range: range }
        if (!desc.range) {
            desc.range = { min: 'n/a', max: 'n/a' };
            desc.error = 'n/a';
        } else {
            if (desc.length < desc.range.min) {
                desc.error = "-" + (desc.range.min - desc.length).toFixed(3).toString();
            } else if (desc.length > desc.range.max) {
                desc.error = "+" + (desc.length - desc.range.max).toFixed(3).toString();
            } else {
                desc.error = 'n/a';
            }
        }
        if (wwPdbDictModel.testMinorError(key, value)) {
            self.unusualIdealBondsMinor.push(desc);
        } else {
            self.unusualIdealBonds.push(desc);
        }
    });
}