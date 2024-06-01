'use strict';

var mainViewModel = {
    template: ko.observable({ templateName: "empty-template", data: {} })
};

function applyMainBindings(template) {
    mainViewModel.template(template);
}

var validatorServiceMinVersion = "1.1.14.6.6";

var busyModel = 
    {
        message: ko.observable("Computing...")
    };

//var pdbReader = new ChemDoodle.io.PDBInterpreter();
var molReader = new ChemDoodle.io.MOLInterpreter();

function makeSpinner(elem) {
    var target = document.getElementById(elem);
    var spinner = new Spinner({ hwaccel: true, radius: 25, length: 15, width: 8, lines: 13 }).spin(target);
}

function updateBusy(message) {
    busyModel.message(message);
}

function showBusy(message) {
    busyModel.message(message);
    applyMainBindings({ templateName: "busy-template", data: busyModel });
    makeSpinner("busySpinner");
}

function showSummaryChart(total, summary, target) {    
    var colors = ['#c42525', '#a6c96a'];  //Highcharts.getOptions().colors;
    var categories = ["Missing Atoms or Rings", "Has All Atoms and Rings"];
    
    var data = [ {
        y: 100 * summary["Missing"] / total,
        color: colors[0],
        drilldown: {
            name: "Missing Atoms",
            categories: ["Missing Atoms", "Missing Rings"],
            colors: [Highcharts.Color(colors[0]).brighten(0.1).get(), Highcharts.Color(colors[0]).brighten(-0.1).get()],
            data: [100 * summary["Missing_Atoms"] / total, 100 * summary["Missing_Rings"] / total]
        }
    }, {
        y: 100 * summary["HasAll"] / total,
        color: colors[1],
        drilldown: {
            name: "With All Atoms",
            categories: ["Correct Chirality", "Wrong Chirality", "Uncertain Chirality" /*, "Wrong Bonds"*/],
            colors: [Highcharts.Color(colors[1]).brighten(-0.10).get(), '#D8C358', '#EFE3A7'],
            data: [
                100 * summary["HasAll_GoodChirality"] / total,
                100 * summary["HasAll_BadChirality"] / total,
                100 * summary["HasAll_WrongBonds"] / total//,
                //100 * summary["HasAll_WrongBonds"] / total
            ]
        }
    }];

    var topData = [];
    var bottomData = [];
    for (var i = 0; i < data.length; i++) {
        topData.push({
            name: categories[i],
            y: data[i].y,
            color: data[i].color
        });

        for (var j = 0; j < data[i].drilldown.data.length; j++) {
            //var brightness = 0.15 - (j / data[i].drilldown.data.length) / 2.0; // / 5;
            bottomData.push({
                name: data[i].drilldown.categories[j],
                y: data[i].drilldown.data[j],
                color: data[i].drilldown.colors[j]
                //color: Highcharts.Color(data[i].color).brighten(brightness).get()
            });
        }
    }


    $(target).highcharts({
        chart: {
            type: 'pie',
            margin: [40, 30, 40, 30]
        },
        title: {
            text: ''
        },
        yAxis: {
            title: {
                text: 'Percent'
            }
        },
        plotOptions: {
            pie: {
                shadow: false,
                center: ['50%', '50%']
            }
        },
        tooltip: {
            pointFormat: '<b>{point.y:.2f}%</b><br/>'
        },
        series: [{
            name: 'Atom Correctness',
            data: topData,
            size: '70%',
            dataLabels: {
                formatter: function () {
                    return '<b>' + this.point.name + ':</b> ' + this.y.toFixed(2) + '%';
                },
                color: 'black',
                distance: -50
            }
        }, {
            name: 'Subgroups',
            data: bottomData,
            size: '80%',
            innerSize: '70%',
            dataLabels: {
                formatter: function () {
                    // display only if larger than 1
                    return '<b>' + this.point.name + ':</b> ' + this.y.toFixed(2) + '%';
                },
                distance: 10
            }
        }]
    });
}

var dataView;
var dataViewFilter = function (item) { return true; };
var detailsGrid = undefined;
var countWidth = 40;
var wideWidth = 250;
var modelsMap = undefined;

function isMissing(m) {
    return m.MissingAtomCount > 0 || m.MissingRingCount > 0;
}

function hasAll(m) {
    return m.MissingAtomCount == 0 && m.MissingRingCount == 0;
}

function hasAllChirality(m) {
    return m.MissingAtomCount == 0 && m.MissingRingCount == 0 && m.WrongBondCount == 0;
}

function DataViewsModel() {
    this.currentModel = ko.observable();
    this.currentView = ko.observable();
    this.views = ko.observable();
    ////this.views = ko.computed(function () {
    ////    var model = this.currentModel();
    ////    if (!model) {
    ////        return [];
    ////    }
    ////    return model.dataViews;
    ////}, this);

    this.currentModel.subscribe(updateView_Model);
    this.currentView.subscribe(updateView_View);
}

var DataViews = new DataViewsModel();

var dataViewTemplates = [
    {
        name: "all",
        count: 0,
        title: "All Motifs",
        columns: [],
        filter: function (e) { return true; }
    },
    {
        name: "missing-atoms",
        count: 0,
        title: "With Missing Atoms or Rings",
        columns: [
            { id: "MissingAtomCount", field: "MissingAtomCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "MissingAtoms", field: "MissingAtoms", name: "Missing Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomList }
        ],
        filter: function (e) { return isMissing(e); }
    },
    {
        name: "missing-atoms-atoms",
        count: 0,
        title: ">  Missing Only Atoms",
        columns: [
            { id: "MissingAtomCount", field: "MissingAtomCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "MissingAtoms", field: "MissingAtoms", name: "Missing Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomList }
        ],
        filter: function (e) { return e.MissingAtomCount > 0 && e.MissingRingCount == 0; }
    },
    {
        name: "missing-atoms-rings",
        count: 0,
        title: ">  Missing Ring",
        columns: [
            { id: "MissingRingCount", field: "MissingRingCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "MissingRings", field: "MissingRings", name: "Missing Rings", sortable: false, maxWidth: wideWidth }
        ],
        filter: function (e) { return e.MissingRingCount > 0; }
    },
    {
        name: "missing-atoms-names",
        count: 0,
        title: ">  Different Atom Naming",
        columns: [
            { id: "MissingAtomCount", field: "MissingAtomCount", name: "Atom Count", sortable: true, maxWidth: countWidth / 1.5 },
            { id: "MissingAtoms", field: "MissingAtoms", name: "Atoms", sortable: false, maxWidth: wideWidth / 1.5, formatter: renderAtomList },
            { id: "NameMismatchCount", field: "NameMismatchCount", name: "Name Count", sortable: true, maxWidth: countWidth / 1.5 },
            { id: "NameMismatches", field: "NameMismatches", name: "Names", sortable: false, maxWidth: wideWidth / 1.5, formatter: renderAtomMap }],
        filter: function (e) { return isMissing(e) && e.NameMismatchCount > 0; }
    },
    //{
    //    name: "missing-atoms-bonds",
    //    count: 0,
    //    title: ">  Wrong Bonds",
    //    columns: [
    //        { id: "WrongBondCount", field: "WrongBondCount", name: "Count", sortable: true, maxWidth: countWidth }//,
    //        //{ id: "MissingRings", field: "MissingRings", name: "Rings", sortable: false, maxWidth: wideWidth }
    //    ],
    //    filter: function (e) { return isMissing(e) && e.WrongBondCount > 0; }
    //},
    {
        name: "all-atoms",
        count: 0,
        title: "With All Atoms",
        columns: [],
        filter: function (e) { return hasAll(e); }
    },
    {
        name: "all-atoms-good",
        count: 0,
        title: ">  Correct Chirality",
        columns: [],
        filter: function (e) { return hasAllChirality(e) && e.ChiralityMismatchCount == 0; }
    },
    {
        name: "all-atoms-bad",
        count: 0,
        title: ">  Wrong Chirality",
        columns: [
            { id: "ChiralityMismatchCount", field: "ChiralityMismatchCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "ChiralityMismatches", field: "ChiralityMismatches", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        filter: function (e) { return hasAllChirality(e) && e.ChiralityMismatchCount > 0; }
    },
    {
        name: "all-atoms-planarity-warning",
        count: 0,
        title: ">  Wrong Chirality Planarity Warning",
        columns: [
            { id: "ChiralityMismatchCount", field: "ChiralityMismatchCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "PlanarAtomsWithWrongChiralityCount", field: "PlanarAtomsWithWrongChiralityCount", name: "Planar Count", sortable: true, maxWidth: countWidth },
            { id: "ChiralityMismatches", field: "ChiralityMismatches", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        filter: function (e) { return hasAllChirality(e) && e['PlanarAtomsWithWrongChiralityCount'] > 0; }
    },
    {
        name: "all-atoms-uncertain",
        count: 0,
        title: ">  Uncertain Chirality",
        columns: [
            { id: "BondMismatches", field: "WrongBondCount", name: "Bond Mismatch Count", sortable: true, maxWidth: countWidth },
            { id: "ChiralityMismatchCount", field: "ChiralityMismatchCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "ChiralityMismatches", field: "ChiralityMismatches", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        filter: function (e) { return hasAll(e) && e.WrongBondCount > 0; }
    },
    {
        name: "all-atoms-subst",
        count: 0,
        title: ">  Substitutions",
        columns: [
            { id: "SubstitutionCount", field: "SubstitutionCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "Substitutions", field: "Substitutions", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        filter: function (e) { return hasAll(e) && e.SubstitutionCount > 0; }
    },
    {
        name: "all-atoms-foreign",
        count: 0,
        title: ">  Foreign Atoms",
        columns: [
            { id: "ForeignAtomCount", field: "ForeignAtomCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "ForeignAtoms", field: "ForeignAtoms", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        filter: function (e) { return hasAll(e) && e.ForeignAtomCount > 0; }
    },
    {
        name: "all-atoms-no-foreign",
        count: 0,
        title: ">  No Foreign Atoms",
        columns: [ ],
        filter: function (e) { return hasAll(e) && e.ForeignAtomCount == 0; }
    },
    {
        name: "all-atoms-names",
        count: 0,
        title: ">  Different Atom Naming",
        columns: [
            { id: "NameMismatchCount", field: "NameMismatchCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "NameMismatches", field: "NameMismatches", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        filter: function (e) { return hasAll(e) && e.NameMismatchCount > 0; }
    },
    {
        name: "all-atoms-zerormsd",
        count: 0,
        title: ">  Zero Model Rmsd",
        columns: [],
        filter: function (e) { return hasAll(e) && e.ModelRmsd < 0.0000001; }
    },
    {
        name: "with-warning",
        count: 0,
        title: "With Processing Warning",
        columns: [
            { id: "WarningCount", field: "warningCount", name: "#W", sortable: true, maxWidth: 40, toolTip: "Number of Warnings" },
            { id: "Warning", field: "warningText", name: "Warning", sortable: false, maxWidth: wideWidth }
        ],
        filter: function (e) { return e.warning; }
    }//,
    //{
    //    name: "all-atoms-bonds",
    //    count: 0,
    //    title: ">  Wrong Bonds",
    //    columns: [
    //        { id: "WrongBondCount", field: "WrongBondCount", name: "Count", sortable: true, maxWidth: countWidth }
    //    ],
    //    filter: function (e) { return hasAll(e) && e.WrongBondCount > 0; }
    //}
];

var defaultColumns = [
    //{ id: "model", name: "Model", field: "ModelName", sortable: true, maxWidth: 100 },
    { id: "id", name: "Id", field: "Id", sortable: true, formatter: renderIdCell, maxWidth: 120, toolTip: "The Id or name of each motif contains its PDB ID (or the ID of the input structure), a unique motif identifier assigned by MotiveValidator, and the serial number of the first atom in the motif, as it appears in the input PDB file." },
    { id: "mainresidue", name: "Validated Residue", sortable: true, field: "MainResidue", maxWidth: 95 },
    { id: "rc", name: "#R", field: "ResidueCount", sortable: true, maxWidth: 40, toolTip: "Number of Residues" },
    { id: "residues", name: "All Residues", sortable: true, field: "Residues", maxWidth: 220 }
];

//var warnIndicatorCol = { id: "warnFlag", name: "W", field: "warnFlag", sortable: true, maxWidth: 40, toolTip: "Warning Indicator" };

var warningColumn = { id: "residues", name: "Residues", sortable: true, field: "Residues", maxWidth: 220 };

var sortCol = undefined;

function comparer(a, b) {
    var x = a[sortCol], y = b[sortCol];
    return (x == y ? 0 : (x > y ? 1 : -1));
}

function createDataLink() {
    var len = dataView.getLength();
    var list = "";
    for (var i = 0; i < len; i++) {
        var item = dataView.getItem(i);
        list += item.ModelName + "/" + item.Id + "\n";
    }   

    var data = "data:text/plain;charset=UTF-8," + encodeURIComponent(list);
    var link = document.getElementById("exportListLink");
    link.href = data;
}

function updateView_Model(model) {
    if (!model) return;

    var oldView = DataViews.currentView();
    var newView = null;
    if (!oldView) {
        newView = model.dataViews[0];
    } else {
        for (var i in model.dataViews) {
            var v = model.dataViews[i];
            if (v.name === oldView.name) {
                newView = v;
                break;
            }
        }
        if (newView == null) {
            newView = model.dataViews[0];
        }
    }

    DataViews.views(model.dataViews);
    DataViews.currentView(newView);
}

function updateView_View(view) {
    if (!view) return;
    if (!DataViews.currentModel()) return;

    var columns = defaultColumns.concat(view.columns);//.concat([warnIndicatorCol, {  }]);
    $.each(columns, function (i, v) {
        v.headerCssClass = "mv-header-default mv-result-header-base";
        v.cssClass = "details-cell";
    });
    if (detailsGrid) detailsGrid.setColumns(columns);

    var filter = null;
    var fs = detailsFilterString();
    if (!fs) {
        filter = view.filter;
    } else {
        filter = (function (s, base) { return function (e) { return base(e) && e.Id.indexOf(s) !== -1; }; })(fs, view.filter);
    }
    
    dataView.setFilter(filter);
    createDataLink();
}

var gridOptions = {
    enableCellNavigation: true,
    enableColumnReorder: false,
    multiSelect: false,
    explicitInitialization: true,
    forceFitColumns: true
    //autoHeight: true
};

function getStructureUrl(id, type, download) {
    var item = dataView.getItemById(id);
    var po = { model: item.ModelName, type: type, sid: item.Id };
    if (download) po["action"] = "download";
    var params = $.param(po);
    return validatorParams.structureAction + "?" + params;
}

function getModelUrl(name, type, download) {
    var po = { model: name, type: type };
    if (download) po["action"] = "download";
    var params = $.param(po);
    return validatorParams.structureAction + "?" + params;
}

function renderIdCell(row, cell, value, columnDef, dataContext) {
    if (dataContext['warning']) {
        return "<a href='#' style='color: #DB890F' onClick='javascript:showDetails(" + dataContext.id + ")'>" + dataContext.Id + "</a>";
    } else {
        return "<a href='#' style='color: blue' onClick='javascript:showDetails(" + dataContext.id + ")'>" + dataContext.Id + "</a>";
    }
}

function atomListToString(id, value) {
    var ret = [];
    var model = modelsMap[dataView.getItemById(id).ModelName].ModelNames;
    value.forEach(function (v) { ret.push(model[v]); });
    return ret.sort().join(", ");
}

function atomMapToString(id, value) {
    var ret = [];
    var model = modelsMap[dataView.getItemById(id).ModelName].ModelNames;
    for (var v in value) {
        ret.push(model[v] + ": " + value[v]);
    }
    return ret.sort().join(", ");
}

function convertWrongBonds(id) {
    var item = dataView.getItemById(id);
    var model = modelsMap[item.ModelName];
    
    var ret = [];
    item.WrongBonds.forEach(function (bond) {
        var key = bond.ModelFrom.toString() + "-" + bond.ModelTo.toString();
        var type = model.ModelBonds[key];
        if (type === undefined) type = "None";
        var e = model.ModelNames[bond.ModelFrom] + "-" + model.ModelNames[bond.ModelTo] +
            ", got '" + bond.Type + "' expected '" + type + "', " + bond.MotiveAtoms;
        ret.push(e);
    });
    return ret.join("; ");
}

function renderAtomList(row, cell, value, columnDef, dataContext) {
    var ret = [];
    var model = modelsMap[dataContext.ModelName].ModelNames;
    value.forEach(function (v) { ret.push(model[v]); });
    return ret.sort().join(", ");
}

function renderAtomMap(row, cell, value, columnDef, dataContext) {
    var ret = [];
    var model = modelsMap[dataContext.ModelName].ModelNames;
    for (var v in value) {
        ret.push("<b>" + model[v] + ":</b> " + value[v]);
    }
    return ret.sort().join(", ");
}

function showDetailsList(modelName, viewName) {
    var model = modelsMap[modelName];
    var views = model.dataViews;

    var view = null;
    views.forEach(function (v) {
        if (v.name === viewName) {
            view = v;
        }
    });
    
    if (view != null) {
        $("#detailsTabLink").click();
        DataViews.currentModel(model);
        DataViews.currentView(view);
    }
}

var analysisRowWidth = 13;

function makeChiralityAnalysis(model) {
    var atoms = model.ModelNames;
    
    var data = [];
        
    var total = model.Summary.HasAll_BadChirality;
    var wrongCounts = model.ChiralityAnalysis.WrongCounts;

    for (var c in wrongCounts) {
        ////var motives = [];
        ////model.Entries.forEach(function (m) {
        ////    if (hasAllChirality(m) && m.ChiralityMismatches.hasOwnProperty(c)) {
        ////        motives.push({ id: m.id, name: m.Id });
        ////    }
        ////});

        var analysis = {
            id: parseInt(c),
            name: atoms[c],
            link: atoms[c] + "-chirality",
            count: wrongCounts[c],
            //expected: model.ModelChirality[c],
            total: total
            //motives: motives
        };

        data.push(analysis);

        var closure = (function (name, c, modelName) {
            return {
                name: name,
                count: 0,
                title: "Chirality Error on " + analysis.name,
                columns: [
                    { id: "ChiralityMismatchCount", field: "ChiralityMismatchCount", name: "Count", sortable: true, maxWidth: countWidth },
                    { id: "ChiralityMismatches", field: "ChiralityMismatches", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
                ],
                filter: function (e) { return e.ModelName === modelName && hasAllChirality(e) && e.ChiralityMismatches.hasOwnProperty(c); }
            }
        })(analysis.link, c, model.ModelName);
        model.dataViews.push(closure);
    }
    
    data.sort(function (a, b) { return a.id - b.id; });
            
    var rows = [];
    for (var i = 0; i < data.length; i += analysisRowWidth) {
        rows.push(data.slice(i, i + analysisRowWidth));
    }
    model.ChiralityAnalysis.DataRows = rows;
    model.ChiralityAnalysis.DataWidth = Math.min(10, data.length);
}

function makeMissingAtomAnalysis(model) {
    var atoms = model.ModelNames;

    var data = [];

    var total = model.Summary.Missing_Atoms;
    var wrongCounts = model.MissingAtomAnalysis.WrongCounts;

    for (var c in wrongCounts) {
        //var motives = [];
        var id = parseInt(c);
        ////model.Entries.forEach(function (m) {
        ////    if (m.MissingAtomCount > 0 && m.MissingRingCount == 0 &&  $.inArray(id, m.MissingAtoms) >= 0) {
        ////        motives.push({ id: m.id, name: m.Id });
        ////    }
        ////});

        var analysis = {
            id: id,
            name: atoms[c],
            link: atoms[c] + "-missing",
            count: wrongCounts[c],
            total: total//,
            //motives: motives
        };

        data.push(analysis);

        var closure = (function (name, c, modelName) {
            return {
                name: name,
                count: 0,
                title: "Missing Atom " + analysis.name,
                columns: [
                    { id: "MissingAtomCount", field: "MissingAtomCount", name: "Count", sortable: true, maxWidth: countWidth },
                    { id: "MissingAtoms", field: "MissingAtoms", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomList }
                ],
                filter: function (e) { return e.ModelName === modelName && e.MissingAtomCount > 0 && e.MissingRingCount == 0 && $.inArray(c, e.MissingAtoms) >= 0; }
            }
        })(analysis.link, analysis.id, model.ModelName);
        model.dataViews.push(closure);
    }

    data.sort(function (a, b) { return a.id - b.id; });

    var rows = [];
    for (var i = 0; i < data.length; i += analysisRowWidth) {
        rows.push(data.slice(i, i + analysisRowWidth));
    }
    model.MissingAtomAnalysis.DataRows = rows;
    model.MissingAtomAnalysis.DataWidth = Math.min(10, data.length);
}

function makeHtmlFormula(formula) {

    try
    {
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
    catch (e)
    {
        //console.log(e.toString());
        return formula;
    }
}

function makeViewsAndAnalysis(model) {
    model.dataViews = [];
    model.htmlFormula = makeHtmlFormula(model.Formula);
    model.Entries.forEach(function (e) {
        var w = model.Warnings[e.Id];
        if (w) {
            //var warningHTML = "<ul style='color: #DB890F'>";
            //for (var t in w) {
            //    warningHTML += "<li>" + w[t] + "</li>";
            //}
            //e.warnings = w.join(" | ");
            e.warning = true;
            e.warningText = w[w.length - 1];
            e.warningCount = w.length;
            e.warnings = w;

            //warningHTML += "</ul>";
            //w.warning = warningHTML;
            //e.warnFlag = "!!!";
        } else {
            e.warning = false;
            e.warningText = "";
            e.warningCount = 0;
            e.warnings = [];
            //e.warnFlag = "";
        }
    });

    dataViewTemplates.forEach(function (templ) {
        var v = (function (filter, modelName) {
            return {
                name: templ.name,
                count: 0,
                title: templ.title,
                columns: templ.columns,
                filter: function (e) { return e.ModelName === modelName && filter(e); }
            };
        })(templ.filter, model.ModelName);
        model.dataViews.push(v);
    });
    makeChiralityAnalysis(model);
    makeMissingAtomAnalysis(model);

    model.dataViews.forEach(function (v) {
        var count = 0;
        var f = v.filter;        
        model.Entries.forEach(function (e) {            
            if (f(e)) {
                count++;
            }
        });
        v.title = v.title + " (" + count + ")";
        v.count = count;
    });

    model.Entries.forEach(function (e) {

        e.modelVisualizationOptions = [
                { caption: "Validated Motif", type: "mol" },
                { caption: "Input Motif (Single Bonds Only)", type: "motifmol" },
                { caption: "Model", type: "modelmol" },
                { caption: "Superimposed Model and Input", type: "superimposedinput" },
                { caption: "Superimposed Model and Validated", type: "superimposedvalidated" }];
        
        e.modelVisualizationA = {
            isBusy: ko.observable(false),
            current: ko.observable(e.modelVisualizationOptions[0]),
            status: ko.observable()
        };
        e.modelVisualizationB = {
            isBusy: ko.observable(false),
            current: ko.observable(e.modelVisualizationOptions[2]),
            status: ko.observable()
        };

        var pIdIndex = e.Id.indexOf('_', 0);
        if (pIdIndex <= 0) pIdIndex = e.Id.length;
        var parentId = e.Id.substring(0, pIdIndex);
        e.parentId = parentId.toUpperCase();

        var bonds = e.WrongBonds;

        bonds.forEach(function (bond) {
            var key = bond.ModelFrom.toString() + '-' + bond.ModelTo.toString();
            bond.modelBond = model.ModelNames[bond.ModelFrom] + '-' + model.ModelNames[bond.ModelTo];
            var expected = model.ModelBonds[key];
            bond.expected = (expected === undefined) ? 'None' : expected;
        });

        var wrongChirality = [];
        var chiralityMismatches = e.ChiralityMismatches;
        Object.keys(chiralityMismatches).forEach(function (key) {
            //var atomChir = e.ChiralityMismatchTypes[key];
            //if (atomChir === "NotStereo") atomChir = "None";

            var atom = {
                modelAtom: model.ModelNames[key],
                //modelChirality: model.ModelChirality[key],
                atom: chiralityMismatches[key],
                //atomChirality: atomChir
            };
            wrongChirality.push(atom);
        });
        e.wrongChirality = wrongChirality;
    });
}


function processErrors(result) {
    var wCount = 0, eCount = 0;

    result.Models.forEach(function (m) {
        m.errorCount = Object.keys(m.Errors).length;
        m.errorPerc = (100.0 * m.errorCount / (m.errorCount + m.Entries.length)).toFixed(2);
        m.warningCount = Object.keys(m.Warnings).length;

        wCount += m.warningCount;
        eCount += m.errorCount;
    });

    result.errors = new ValidatorErrorsModel(result);
    result.errors.count = wCount + eCount;
    result.errors.warningCount = wCount;
    result.errors.errorCount = eCount;
}

function handleEmptyResult(result) {
    var errors = [];
    for (var e in result.Errors) {
        errors.push([e, result.Errors[e]]);
    }
    applyMainBindings({ templateName: "empty-result-template", data: { errors: errors } });
}

function handleOldResult(result) {
    applyMainBindings({ templateName: "old-version-template", data: { ValidationType: result.ValidationType, version: result.Version, minVersion: validatorServiceMinVersion } });
}

var detailsFilterString = ko.observable("");

function makeFilter(result) {
    result.detailsFilterString = detailsFilterString;

    var timer = null;
    var oldValue = "";
    
    detailsFilterString.subscribe(function (value) {
        if (value == oldValue) {
            return;
        }

        oldValue = value;
        if (timer != null) {
            clearTimeout(timer);
            timer = null;
        }
        timer = setTimeout(function () {
            timer = null;
            updateView_View(DataViews.currentView());
        }, 500);
    });
}

function isVersionOk(version) {
    var regex = /(\d+)\.(\d+)\.(\d+)\.(\d+)\.(\d+)(.*)/;

    var fields = regex.exec(version);

    if (!fields) {
        return false;
    }

    var minFields = regex.exec(validatorServiceMinVersion);
    var minVersion = [+minFields[1], +minFields[2], +minFields[3], +minFields[4], +minFields[5], minFields[6]];
    var currentVersion = [+fields[1], +fields[2], +fields[3], +fields[4], +fields[5], fields[6]];

    for (var i = 0; i < 6; i++) {
        if (currentVersion[i] > minVersion[i]) {
            return true;
        }
        if (currentVersion[i] < minVersion[i]) {
            return false;
        }
    }

    return true;
}

function showResult(result) {
    if (result["_error"]) {
        applyMainBindings({ templateName: "fail-template", data: { message: result["_message"] } });
        return;
    }

    if (result.Models.length === 0) {
        handleEmptyResult(result);
        return;
    }

    //if (!isVersionOk(result.Version)) {
    //    handleOldResult(result);
    //    return;
    //}
    
    modelsMap = {};
    
    result.Models.sort(function (x, y) {
        var a = x.ModelName, b = y.ModelName;
        if (a < b) return -1;
        else if (a === b) return 0;
        return 1;
    })

    var flatData = [];
    var id = 0;
    result.Models.forEach(function (m) {
        m.CountedName = m.ModelName.toUpperCase() + ' (' + m.Entries.length + ')';

        var modelNamesArray = [];
        var nameKeys = Object.keys(m.ModelNames);
        if (nameKeys.every(function (x) { return !isNaN(parseInt(x)); })) {
            modelNamesArray = nameKeys.sort(function (x, y) {
                return parseInt(x) - parseInt(y);
            }).map(function (x) { return m.ModelNames[x]; });            
        }

        m.modelNamesArray = modelNamesArray;
        modelsMap[m.ModelName] = m;
        m.Entries.forEach(function (e) {
            id++;
            e.id = id;
            e.model = m;
            flatData.push(e);
        });
        makeViewsAndAnalysis(m);
    });

    processErrors(result);
    makeFilter(result);

    dataView = new Slick.Data.DataView();
    dataView.setItems(flatData);

    var overview = new ResultOverviewViewModel(result, "overview-plot");
    result.overview = overview;

    applyMainBindings({ templateName: "result-template", data: result });
    createDataLink();
    
    $("#resultUrl").click(function () {
        $(this).select();
    });

    var showData = $("a[data-show-details='true']");

    showData.tooltip();
    showData.click(function (e) {
        e.preventDefault();
        var $this = $(this);
        showDetailsList($this.data("model-name"), $this.data("details-name"));
    });

    $("a[data-show-errors='true']").click(function (e) {
        e.preventDefault();
        var $this = $(this);
        $("#errorsTabLink").click();
        result.errors.showErrors($this.data("model-name"));
    });

    $("a[data-show-warnings='true']").click(function (e) {
        e.preventDefault();
        var $this = $(this);
        $("#errorsTabLink").click();
        result.errors.showWarnings($this.data("model-name"));
    });

    ////$("a[data-show-details-toggle='true']").click(function (e) {
    ////    e.preventDefault();
    ////    var $this = $(this);
    ////    $this.children("i").each(function () {
    ////        var $i = $(this);
    ////        if ($i.hasClass("icon-plus")) {
    ////            $i.removeClass("icon-plus").addClass("icon-minus");
    ////        } else {
    ////            $i.removeClass("icon-minus").addClass("icon-plus");
    ////        }
    ////    });
    ////    $this.parents("thead").siblings("tbody").each(function () {
    ////        $(this).toggle();
    ////    });
    ////});

    var gridInitialized = false;
    var parityChartsCreated = false;

    $('a[data-toggle="tab"]').on('shown', function (e) {
        //e.target // activated tab
        //e.relatedTarget // previous tab
        if (!gridInitialized && detailsGrid !== undefined && $(e.target).attr('href') === "#details") {
            detailsGrid.init();
            gridInitialized = true;
        }

        if ($(e.target).attr('href') === "#overview") {
            overview.init();
        }
    });

    result.Models.forEach(function (m) {
        if (m.Entries.length > 0) {
            showSummaryChart(m.Entries.length, m.Summary, "#summaryChart-" + m.ModelName);
        }
    });
            
    detailsGrid = new Slick.Grid("#detailsGrid", dataView, [], gridOptions);
    dataView.onRowCountChanged.subscribe(function (e, args) {
        detailsGrid.updateRowCount();
        detailsGrid.render();
    });
    dataView.onRowsChanged.subscribe(function (e, args) {
        detailsGrid.invalidateRows(args.rows);
        detailsGrid.render();
    });
    detailsGrid.onSort.subscribe(function (e, args) {
        sortCol = args.sortCol.field;
        dataView.sort(comparer, args.sortAsc);
    });

    if (result.Models.length > 0)
    {
        var model = result.Models[0];
        DataViews.currentModel(model);
    }
}

function getMBString(bytes) {
    return (bytes / (1024 * 1024)).toFixed(2).toString();
}

function showAborted(size, maxSize) {
    applyMainBindings({ templateName: "big-result-template", data: { size: getMBString(size), maxSize: getMBString(maxSize) } });
}

function getAjaxStatus(url) {
    return $.ajax({
        url: url,
        cache: false,
        dataType: 'json'
    });
}

function getResult() {
    showBusy("Downloading data...");
    //$.get(validatorParams.dataAction)
    //showBusy("Downloading data... " + percentComplete.toFixed(2).toString() + "% complete.");
    
    var checkSize = undefined;
    var aborted = false;

    var download = $.ajax({
        url: validatorParams.dataAction,
        type: 'GET',
        dataType: 'json',
        cache: false,
        xhr: function () {
            try {
                var xhr = new window.XMLHttpRequest();
                xhr.addEventListener("progress", function (evt) {
                    if (evt.lengthComputable) {
                        if (checkSize(evt.total)) {
                            var percentComplete = 100 * evt.loaded / evt.total;
                            //updateBusy("Downloading data... " + percentComplete.toFixed(1).toString() + "% complete.");
                            updateBusy("Downloading data... " + getMBString(evt.loaded) + " MB loaded."); //+ "/" + getMBString(evt.total) + "MB (" + percentComplete.toFixed(1).toString() + "%).");
                        }
                    }
                }, false);

                xhr.addEventListener("load", function (evt) {
                    updateBusy("Loading...");
                }, false);

                return xhr;
            } catch (ex) {
                return new window.XMLHttpRequest();
            }
        }
    })
        .done(function(result) { if (!aborted) { showResult(result); } })
        .fail(function (jqXHR, textStatus, errorThrown) { if (!aborted) { applyMainBindings({ templateName: "fail-template", data: { message: errorThrown } }); } });

    var maxDownloadableSize = 150 * 1024 * 1024;
    checkSize = function (size) {
        if (aborted) return false;
        if (size > maxDownloadableSize && download) {
            aborted = true;
            download.abort();
            showAborted(size, maxDownloadableSize);
            return false;
        }
        return true;
    };
}

function updateStatus() {
    getAjaxStatus(validatorParams.statusAction)
        .done(function (result) {
            if (!result['Exists']) {
                applyMainBindings({ templateName: "fail-template", data: { message: "Computation does not exist." } });
                return;
            }

            if (result.IsRunning) {
                var message;
                if (result.IsIndeterminate) {
                    message = result.Message;
                } else {
                    message = result.Message + " " + result.CurrentProgress + "/" + result.MaxProgress;
                }
                busyModel.message(message);
                setTimeout(updateStatus, 3000);
            } else {
                getResult();
            }
        })
    .fail(function () { setTimeout(updateStatus, 3000); });
}

function checkComputing() {
    showBusy("Computing...");
    updateStatus();
}

var modelVisualizationA = null, modelVisualizationB = null, visualizationXhr = null;

function cancelVisualization() {
    if (visualizationXhr != null) {
        visualizationXhr.abort();
        visualizationXhr = null;
    }
}

function setMolColor(mol, color) {
    $.each(mol.atoms, function (i, a) {
        var _color = color;
        a.getElementColor = function () {
            return _color;
        };
    });
}

function makeModelLabels(mol, modelName) {
    var modelNames = modelsMap[modelName]["modelNamesArray"];
    $.each(mol.atoms, function (i, a) {
        var label = modelNames[i];
        if (label) {
            a.altLabel = label;
        }
    });
}

//function clearMolecules(viz) {
//    if (!viz || !viz.gl) {
//        return;
//    }

//    var mols = viz.molecules;
//    viz.clear();
//    $.each(mols, function (k, m) {
//        if (m.labelMesh) {
//            $.each(m.labelMesh, function (v, b) {
//                if (m.labelMesh.hasOwnProperty(v)) {
//                    try {
//                        viz.gl.deleteBuffer(b);
//                    } catch (e) {
//                    }
//                }
//            });
//        }
//        m.labelMesh = undefined;
//        console.log(m);
//    });
//    //console.log(mols);
//}

function subscribeVisualization(item, viz, elem, displayType, visualizer) {
    var xhr = null;
    var cancel = function () {
        if (xhr != null) {
            xhr.abort();
            xhr = null;
        }
    };

    var visualizer = createVisualizer(elem);

    if (visualizer) {
        //visualizer.clear();
        visualizer.specs.atoms_displayLabels_3D = displayType.showLabels();
        visualizer.specs.set3DRepresentation(displayType.displayType());
    }
    
    viz.isBusy(true);
    viz.status("Loading...");

    var moleculeData = {};
    
    function update() {
        viz.status("Loading...");
        var mode = viz.current();
        if (!mode || !visualizer) {
            if (visualizer) visualizer.clear();
            viz.status("Unable to visualize...");
            return;
        }

        var molA, molB;
        try {
            //visualizer.clear();
            if (mode.type === "superimposedvalidated") {
                molA = molReader.read(moleculeData["mol"], 1);
                molB = molReader.read(moleculeData["modelmol"], 1);
                makeModelLabels(molB, item.ModelName);
                setMolColor(molA, "#0000ff");
                setMolColor(molB, "#ff0000");
                visualizer.loadContent([molA, molB], []);
            } else if (mode.type === "superimposedinput") {
                molA = molReader.read(moleculeData["motifmol"], 1);
                molB = molReader.read(moleculeData["modelmol"], 1);
                makeModelLabels(molB, item.ModelName);
                setMolColor(molA, "#0000ff");
                setMolColor(molB, "#ff0000");
                visualizer.loadContent([molA, molB], []);
            } else {
                molA = molReader.read(moleculeData[mode.type], 1);
                if (mode.type === "modelmol") {
                    makeModelLabels(molA, item.ModelName);
                }
                visualizer.loadContent([molA], []);
                //console.log(molA);
            }
            viz.status("");
        } catch (ex) {
            console.log("Failed to render: " + ex);
            viz.status("Failed");
        }
    }

    var sub = viz.current.subscribe(update);

    return {
        dispose: function () {
            if (sub) {
                sub.dispose();
                sub = null;
            }
            if (visualizer) {
                visualizer.clear();
                visualizer = null;
                $("#" + elem + "-wrap > canvas").empty().remove();
            }
            moleculeData = null;
        },
        setData: function (data) {
            if (!data) {
                viz.isBusy(true);
                viz.status("Failed.");
                return;
            }

            viz.isBusy(false);
            viz.status("");
            moleculeData = data;
            update();
        },
        isBig: false,
        visualizer: visualizer,
        elem: elem
    };
}

function toggleVisualizerSize(vizId) {
    var viz = vizId === 'A' ? modelVisualizationA : modelVisualizationB;
    var elem = $("#" + viz.elem + "-wrap");

    if (viz.isBig) {
        viz.isBig = false;
        elem.removeClass('details-big-visualizer').addClass('details-small-visualizer');
    } else {
        viz.isBig = true;
        elem.removeClass('details-small-visualizer').addClass('details-big-visualizer');
    }
    
    if (viz.isBig) {
        var  body = elem.closest('.modal-body');
        body.addClass('details-big-hide-overflow');
        if (body[0]) {
            body[0].scrollTop = 0;
        }
    } else {
        elem.closest('.modal-body').removeClass('details-big-hide-overflow');
    }

    if (viz.visualizer) {        
        var w = elem.width(), h = elem.height() - 35;
        viz.visualizer.specs.projectionWidthHeightRatio_3D = w / h;
        viz.visualizer.resize(w, h);
    }
}

var detailsModelCache = {};

var detailsModalModel = {
    visualizerA: null,
    visualizerB: null,
    current: ko.observable()
};

function showDetails(id) {
    var item = dataView.getItemById(id);

    if (modelVisualizationA != null) {
        modelVisualizationA.dispose();
        modelVisualizationA = null;
    }
    if (modelVisualizationB != null) {
        modelVisualizationB.dispose();
        modelVisualizationB = null;
    }
    
    detailsModalModel.current(item);
    //ko.applyBindings({ templateName: "details-template", data: detailsModalModel }, document.getElementById("detailsModal"));

    //if (!detailsModalModel.visualizerA && isWebGLavailable) {
    //    detailsModalModel.visualizerA = createVisualizer("model3d-A");
    //}
    //if (!detailsModalModel.visualizerB && isWebGLavailable) {
    //    detailsModalModel.visualizerB = createVisualizer("model3d-B");
    //}

    modelVisualizationA = subscribeVisualization(item, item.modelVisualizationA, "model3d-A", modelVisualizationAType);
    modelVisualizationB = subscribeVisualization(item, item.modelVisualizationB, "model3d-B", modelVisualizationBType);
    $('#detailsModal').modal();

    var cacheKey = item.ModelName + "-" + item.Id;

    if (detailsModelCache[cacheKey]) {
        var result = detailsModelCache[cacheKey];
        modelVisualizationA.setData(result);
        modelVisualizationB.setData(result);
    } else {
        visualizationXhr = $.ajax({
            url: validatorParams.structureAction,
            data: { model: item.ModelName, sid: item.Id, type: "visualization" /* mode.type */ },
            cache: false,
            dataType: 'json'
        })
        .done(function (result) {
            visualizationXhr = null;
            try {
                modelVisualizationA.setData(result);
                modelVisualizationB.setData(result);
                detailsModelCache[cacheKey] = result;
            } catch (ex) {
                modelVisualizationA.setData();
                modelVisualizationB.setData();
            }
        })
        .fail(function () { visualizationXhr = null; });
    }
}

function VisualizationTypeModel(vizId) {
    var self = this;
    this.showLabels = ko.observable(true);
    this.displayType = ko.observable("Ball and Stick");

    this.showLabels.subscribe(function (value) {
        var viz = vizId === 'A' ? modelVisualizationA : modelVisualizationB;
        if (!viz.visualizer) {
            return;
        }

        viz.visualizer.specs.atoms_displayLabels_3D = value;
        viz.visualizer.repaint();
    });

    this.displayType.subscribe(function (value) {
        var viz = vizId === 'A' ? modelVisualizationA : modelVisualizationB;
        if (!viz.visualizer) {
            return;
        }

        viz.visualizer.specs.set3DRepresentation(value);
        viz.visualizer.setupScene();
        viz.visualizer.repaint();
    });

    this.toggleLabels = function () {
        self.showLabels(!self.showLabels());
    };

    this.toggleDisplayType = function () {
        if (self.displayType() === "Ball and Stick") {
            self.displayType("Wireframe");
        } else {
            self.displayType("Ball and Stick");
        }
    };
}

var modelVisualizationAType = new VisualizationTypeModel('A'),
    modelVisualizationBType = new VisualizationTypeModel('B');

function createVisualizer(elem) {
    if (!isWebGLavailable) {
        $("#" + elem + "-wrap").html(webGLmessage);
        return null;
    } else {

        var target = $("<canvas id='" + elem + "' />");
        $("#" + elem + "-wrap").append(target);
        
        var transform = new ChemDoodle.TransformCanvas3D(elem, 350, 250);
        if (!transform.gl) {
            target.remove();
            $("#" + elem + "-wrap")
                .html(webGLmessage);
            currentTransform = null;
            return null;
        }
        transform.specs.atoms_displayLabels_3D = true;
        transform.specs.atoms_useJMOLColors = true;
        transform.specs.projectionWidthHeightRatio_3D = 350.0 / 250.0;
        transform.specs.set3DRepresentation('Ball and Stick');
        //transform.specs.set3DRepresentation('Line');
        transform.specs.backgroundColor = 'white';
        transform.specs.proteins_displayRibbon = false;
        transform.specs.proteins_displayBackbone = false;
        transform.specs.nucleics_display = false;
        transform.specs.macro_displayAtoms = true;
        transform.specs.macro_displayBonds = true;
        return transform;
    }
}

var isWebGLavailable = true;
var webGLmessage = null;

function checkWebGL() {

    if (!window.WebGLRenderingContext /*|| isIE*/) {
        webGLmessage = "A browser with WebGL support is required to display the model. Learn more at <a href='//get.webgl.org/' target='_blank'>//get.webgl.org/</a>.";
        isWebGLavailable = false;
        return;
    }

    var canvas = $("<canvas />").get(0);
    var gl;
    try { gl = canvas.getContext("webgl"); }
    catch (x) { gl = null; }
    if (gl == null) {
        try { gl = canvas.getContext("experimental-webgl"); }
        catch (x) { gl = null; }
    }
    if (gl == null) {
        isWebGLavailable = false;
        webGLmessage = "A browser with WebGL support is required to display the model. "
                + "Unable to create a WebGL context. Learn more at <a href='//get.webgl.org/troubleshooting' target='_blank'>//get.webgl.org/troubleshooting</a>.";
    }
}

var isIE = false;
(function () {
    if (!window['jQuery'] || !JSON) {
        alert("You are using an unsupported browser, the results might not display correctly. It is recommended that you use the latest version of Mozilla Firefox or Google Chrome.");
        return;
    }

    var N = navigator.appName, ua = navigator.userAgent, tem;
    var M = ua.match(/(opera|chrome|safari|firefox|msie)\/?\s*(\.?\d+(\.\d+)*)/i);
    if (M && (tem = ua.match(/version\/([\.\d]+)/i)) != null) M[2] = tem[1];
    M = M ? [M[1], M[2]] : [N, navigator.appVersion, '-?'];
    
    isIE = M[0].toLowerCase() === "msie" || M[0].toLowerCase() === "netscape";
    if (M[0].toLowerCase() === "msie" && M[1] < 10.0) {
        alert("You are using an unsupported browser, the results might not display correctly. It is recommended that you use the latest version of Mozilla Firefox or Google Chrome.");
    }
})();

!function ($) {
    $(function () {
        ko.applyBindings(mainViewModel, document.getElementById("mainView"));
        ko.applyBindings(detailsModalModel, document.getElementById("detailsModal"));

        checkWebGL();
        
        if (validatorParams.isFinished) getResult();
        else checkComputing();

        $('#detailsModal').on('hide', function () {
            if (modelVisualizationA != null) {
                modelVisualizationA.dispose();
                modelVisualizationA = null;
            }
            if (modelVisualizationB != null) {
                modelVisualizationB.dispose();
                modelVisualizationB = null;
            }
            cancelVisualization();
        });
    });
}(window.jQuery)