"use strict";

function FilterModel(vm, filter, value, comparisonType) {
    var self = this;

    this.filter = filter;
    this.value = value;
    this.comparisonType = comparisonType;
    this.remove = function () {
        vm.removeFilter(self)
    };
    this.makeFilter = function () {
        return {
            PropertyName: filter.property,
            PropertyType: filter.propertyType,
            ComparisonType: comparisonType.value,
            Value: value
        };
    };
}

var filterTypeNumeric = "numeric",
    filterTypeString = "string",
    filterTypeDate = "date";

var numericComparisonTypes = [
    { value: "NumberEqual", text: "=" },
    { value: "NumberLess", text: "<" },
    { value: "NumberLessEqual", text: "<=" },
    { value: "NumberGreater", text: ">" },
    { value: "NumberGreaterEqual", text: ">=" }
];

var stringComparisonTypes = [
    { value: "StringEqual", text: "Exact" },
    { value: "StringContainsWord", text: "Contains Word" },
    { value: "StringRegex", text: "Regex" }
];

function intFilter(name, prop, desc) {
    return {
        name: name,
        property: prop,
        description: desc,
        propertyType: "Int",
        type: filterTypeNumeric,
        comparisonTypes: numericComparisonTypes
    };
}

function doubleFilter(name, prop, desc) {
    return {
        name: name,
        property: prop,
        description: desc,
        propertyType: "Double",
        type: filterTypeNumeric,
        comparisonTypes: numericComparisonTypes
    };
}

function dateFilter(name, prop, desc) {
    return {
        name: name,
        property: prop,
        description: desc,
        propertyType: "Date",
        type: filterTypeDate,
        comparisonTypes: numericComparisonTypes
    };
}

function stringFilter(name, prop, desc) {
    return {
        name: name,
        property: prop,
        description: desc,
        propertyType: "StringArray",
        type: filterTypeString,
        comparisonTypes: stringComparisonTypes
    };
}

var filterModelList = [
    intFilter("Atom Count", "AtomCount", "Atom count."),
    intFilter("Residue Count", "ResidueCount", "Residue count."),
    doubleFilter("Resolution", "Resolution", "Resolution in angstroms."),
    doubleFilter("Weight", "Weight", "Weight in kDa."),

    dateFilter("Release Date", "ReleasedDate", "Release date."),
    dateFilter("Latest Revision Date", "LatestRevisionDate", "Last revision date."),

    stringFilter("Keywords", "Keywords", "Keywords."),
    stringFilter("EC Numbers", "EcNumbers", "Enzymatic Commission numbers. When using Contains Word comparison it is possible to enter just a number prefix without the '.', for example \"3.2\"."),
    stringFilter("Authors", "Authors", "Authors."),
    stringFilter("Atoms", "AtomTypes", "Atom types."),
    stringFilter("Residues", "ResidueTypes", "Residue types."),
    stringFilter("Rings", "RingFingerprints", "Ring fingerprints. The elements must be lexicographically ordered. For example \"CCCCC\" or \"CCCCN\". It is possible to use special ring syntax @(E1, E2, ...) that will do the ordering for you. For example @(N, C, N, C, C) is equivalent to entering CCCNN."),

    stringFilter("Polymer type", "PolymerType", "Polymer type. Possible values are: Protein, DNA, RNA, ProteinDNA, ProteinRNA, NucleicAcids, Mixture, Sugar, Other, NotAssigned."),
    stringFilter("Experiment method", "ExperimentMethod", "Experiment method."),
    stringFilter("Protein stoichiometry", "ProteinStoichiometry", "Protein stoichiometry. Possible values are: Monomer, Homomer, Heteromer, NotAssigned."),

    stringFilter("Host organisms", "HostOrganisms", "Host organisms."),
    stringFilter("Host organism IDs", "HostOrganismsId", "Host organism identifiers."),
    stringFilter("Host organisms genus", "HostOrganismsGenus", "Host organism's genus."),

    stringFilter("Origin organisms", "OriginOrganisms", "Origin organisms."),
    stringFilter("Origin organism IDs", "OriginOrganismsId", "Origin organism identifiers."),
    stringFilter("Origin organisms genus", "OriginOrganismsGenus", "Origin organism's genus."),
];

function filterComparer(a, b) {
    return a.name == b.name ? 0 : (a.name < b.name ? -1 : 1);
}

function showFilterError(error) {
    if (!error) {
        $('#filterValue')
            .removeClass('error')
            .tooltip('destroy');

        return;
    }

    $('#filterValue')
        .addClass('error')
        .tooltip('destroy')
        .tooltip({
            animation: false,
            title: error,
            placement: 'top',
            trigger: 'manual'
        })
        .tooltip('show');
}

function CreateViewModel(databases) {

    var self = this;

    this.availableDatabases = [{ name: "Select a database...", id: ""}].concat(databases);

    this.addedFilters = ko.observableArray([]);
    
    this.removeFilter = function (item) {
        self.addedFilters.remove(item);
        self.filtersModel.filters.push(item.filter);
        self.filtersModel.filters.sort(filterComparer);
    };

    this.name = ko.observable("");
    this.description = ko.observable("");
    this.database = ko.observable();

    var sortedFilters = filterModelList.sort(filterComparer);

    this.filtersModel = {
        filters: ko.observableArray(sortedFilters),
        currentFilter: ko.observable(sortedFilters[0]),
    
        value: ko.observable(""),
        comparisonType: ko.observable(""),
    };

    this.canAddFilter = function () {
        var value = self.filtersModel.value().trim();
        if (value.length === 0) {
            showFilterError("The value cannot be empty.");
            return false;
        }

        var filter = self.filtersModel.currentFilter();
        if (filter.type == filterTypeNumeric) {
            if (isNaN(value)) {
                if (filter.propertyType === "Int") showFilterError("The value must be an integer.");
                else showFilterError("The value must be a floating point number.");
                return false;
            }            
            if (filter.propertyType === "Int" && parseFloat(value) % 1 !== 0) {
                showFilterError("The value must be an integer.");
                return false;
            }
        }
        if (filter.type == filterTypeDate && value.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/) === null) {
            showFilterError("Invalid date format. Expected yyyy-d-m, for example 2007-10-27.");
            return false;
        }

        showFilterError();
        return true;
    };

    this.addFilter = function () {
        if (!self.canAddFilter()) return;

        var filter = self.filtersModel.currentFilter();
        var value = self.filtersModel.value().trim();
        if (filter.type == filterTypeNumeric) {
            value = value.replace(",", ".");
            if (value[value.length - 1] === ".") {
                value = value.substring(0, value.length - 1);
            }
        }

        self.addedFilters.push(new FilterModel(self, filter, value, self.filtersModel.comparisonType()));
        self.filtersModel.filters.remove(self.filtersModel.currentFilter());
        self.filtersModel.value("");
        self.filtersModel.comparisonType("");
        showFilterError();
    };

    function makeFilters() {
        return {
            ViewName: self.name(),
            Description: self.description(),
            DatabaseId: self.database().id,
            Filters: self.addedFilters().map(function (f) { return f.makeFilter(); })
        };
    };

    function updateCreateButton() {
        self.showCreateError(false);
        self.createErrorText("");

        if (self.name().trim().length == 0) {
            self.createLabel("Please enter the filter name");
            self.canCreate(false);
            return;
        }
        if (!self.database().id) {
            self.createLabel("Please select a database to filter");
            self.canCreate(false);
            return;
        }
        if (self.addedFilters().length == 0) {
            self.createLabel("Please add at least one filter");
            self.canCreate(false);
            return;
        }
        self.createLabel("Create View");
        self.canCreate(true);
    }

    this.name.subscribe(updateCreateButton);
    this.database.subscribe(updateCreateButton);
    this.addedFilters.subscribe(updateCreateButton);

    this.createLabel = ko.observable("Create View");
    this.canCreate = ko.observable(false);

    this.showCreateError = ko.observable(false);
    this.createErrorText = ko.observable("");

    this.create = function () {
        var filters = makeFilters();
        self.canCreate(false);
        self.createLabel("Creating View... Please do not refresh the page.");

        $.ajax({
            type: "POST",
            url: CreateViewParams.createAction,
            data: JSON.stringify({ 'filters': JSON.stringify(filters) }),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (result) {
            if (result["ok"]) {
                self.createLabel("Redirecting to the view list...");
                location.href = CreateViewParams.listAction;
            } else {
                updateCreateButton();
                self.showCreateError(true);
                self.createErrorText(result["message"]);
            }
        }).fail(function (x, y, message) {
            updateCreateButton();
            self.showCreateError(true);
            self.createErrorText(message);
        });
    }

    updateCreateButton();
}

ko.bindingHandlers.enter = {
    init: function (element, valueAccessor, allBindingsAccessor, data) {
        //wrap the handler with a check for the enter key
        var wrappedHandler = function (data, event) {
            if (event.keyCode === 13) {
                valueAccessor().call(this, data, event);
            }
        };
        //call the real event binding for 'keyup' with our wrapped handler
        ko.bindingHandlers.event.init(element, function () { return { keyup: wrappedHandler }; }, allBindingsAccessor, data);
    }
};

$(function () {
    ko.applyBindings(new CreateViewModel(CreateViewParams.availableDatabases), document.getElementById("mainView"))
});