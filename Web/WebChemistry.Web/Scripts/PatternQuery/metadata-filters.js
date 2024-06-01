function FilterViewModel(vm) {
    "use strict";

    var self = this,
        serial = vm.newSerial();

    this.serial = serial;
    this.parent = vm;

    this.availableFilters = vm.availableFilters;
    this.filter = ko.observable(this.availableFilters()[0]);
    this.comparisonType = ko.observable(this.filter().comparisonTypes[0]);
    this.filterText = ko.observable("");

    this.isAdded = ko.observable(false);
    this.canAdd = ko.observable(false);

    var errorMessage = "";

    self.filterText.subscribe(updateCanAdd);
    self.filter.subscribe(updateCanAdd);

    function updateCanAdd() {
        if (self.isAdded()) return;

        var value = self.filterText().trim(),
            filter = self.filter();

        if (value.length === 0) {
            clearError();
            self.canAdd(false);
            return;
        }

        if (value.length > 5000) {
            showError("The value is too long.");
            self.canAdd(false);
            return;
        }

        try {
            btoa(value);            
        } catch (e) {
            var badApple;
            _.forEach(value, function (c) {
                try {
                    btoa(c);
                } catch (ex) {
                    badApple = c;
                    return false;
                }
            });

            if (badApple !== undefined) {
                showError("The input cannot contain `" + badApple + "` because it is not a basic latin character.");
            } else {
                showError("Error: " + e);
            }
            self.canAdd(false);
            return;
        }
        
        if (filter.type === mqFilterData.filterTypeNumeric) {
            if (isNaN(value)) {
                if (filter.propertyType === "Int") showError("The value must be an integer.");
                else showError("The value must be a floating point number.");
                self.canAdd(false);
                return;
            }
            if (filter.propertyType === "Int" && parseFloat(value) % 1 !== 0) {
                showError("The value must be an integer.");
                self.canAdd(false);
                return;
            }
        }
        if (filter.type === mqFilterData.filterTypeDate) {
            var dateMatch = value.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/);
            if (dateMatch === null) {
                showError("Invalid date format. Expected yyyy-m-d, for example 2007-10-27.");
                self.canAdd(false);
                return;
            }
            var month = parseInt(dateMatch[2]), day = parseInt(dateMatch[3]);
            if (month > 12) {
                showError("Invalid date format. Expected yyyy-m-d, for example 2007-10-27. A year only has 12 months.");
                self.canAdd(false);
                return;
            }
            if (day > 31) {
                showError("Invalid date format. Expected yyyy-m-d, for example 2007-10-27. No month has more than 31 days.");
                self.canAdd(false);
                return;
            }
        }

        clearError();
        self.canAdd(true);
        return;
    }

    function getInputWrap() {
        return $('#filter-' + serial + ' .control-group');
    }

    function clearError() {
        if (!errorMessage) return;
        var $wrap = getInputWrap(name);
        $wrap.removeClass("error");
        $wrap.find("input").tooltip('destroy');
        errorMessage = '';
    }

    this.clearError = clearError;
    
    function showError(message) {
        if (errorMessage === message) return;

        var $wrap = getInputWrap();
        $wrap.addClass("error");
        $wrap.find("input")
            .tooltip('destroy')
            .tooltip({
                animation: false,
                title: message,
                placement: 'bottom',
                trigger: 'manual'
            })
            .tooltip('show');
        errorMessage = message;
    }

    this.add = function () {
        if (self.canAdd()) {
            self.isAdded(true);
            self.filterText().trim();
            vm.add(self);
        }
    };

    this.remove = function () {
        vm.remove(self);
    };

    this.makeFilter = function () {
        var value = self.filterText().trim(),
            filter = self.filter();
        if (filter.type === mqFilterData.filterTypeNumeric) {
            value = value.replace(",", ".");
            if (value[value.length - 1] === ".") {
                value = value.substring(0, value.length - 1);
            }
        }

        return {
            PropertyName: filter.property,
            PropertyType: filter.propertyType,
            ComparisonType: self.comparisonType().value,
            Value: self.filterText()
        };
    };
}

function DatabaseFiltersViewModel() {
    "use strict";

    var self = this,
        filtersSerial = 0,
        version = 0;

    this.newSerial = function () { return ++filtersSerial; };

    this.showHelp = ko.observable(true);
    this.availableFilters = ko.observableArray(mqFilterData.filterModelList.slice());
    this.availableFilters.sort(mqFilterData.filterComparer);
    this.filterCount = ko.observable(0);
    this.filters = ko.observableArray([new FilterViewModel(this)]);
    this.changed = ko.observable();

    this.preview = new DatabaseFiltersPreviewViewModel(this);

    this.hasZeroMatches = ko.observable(false);

    function allowMultipleFilters(filter) {
        return filter.type === mqFilterData.filterTypeNumeric || filter.type === mqFilterData.filterTypeDate;
    }

    this.add = function (model) {
        self.hasZeroMatches(false);
        
        var f = model.filter();
        if (!allowMultipleFilters(f)) {
            self.availableFilters.remove(f);
        }
        self.filterCount(self.filterCount() + 1);        
        if (self.availableFilters().length > 0) {
            var nf = new FilterViewModel(self);
            self.filters.push(nf);
            $('#filter-' + filtersSerial + ' input').focus();
            self.changed(++version);
            self.preview.clear();
            return nf;
        }
        self.changed(++version);
        self.preview.clear();
    };

    this.remove = function (model) {
        var f = model.filter();
        
        if (!allowMultipleFilters(f)) {
            self.availableFilters.push(f);
        }
        self.availableFilters.sort(mqFilterData.filterComparer);

        self.filters.remove(model);
        var xs = self.filters();
                
        if (xs.length === 1) {
            self.preview.clear();
        }

        if (xs.length === 0 || xs[xs.length - 1].isAdded()) {            
            self.filters.push(new FilterViewModel(this));
            $('#filter-' + filtersSerial + ' input').focus();
        }
        self.filterCount(self.filterCount() - 1);
        self.hasZeroMatches(false);
        self.changed(++version);
        self.preview.clear();
    };

    this.clear = function () {
        _.forEach(self.filters(), function (f) { f.clearError(); });
        self.filters.removeAll();
        self.showHelp(true);
        self.availableFilters(mqFilterData.filterModelList.slice());
        self.availableFilters.sort(mqFilterData.filterComparer);
        self.filters.removeAll();
        self.filterCount(0);
        self.changed(++version);
        self.preview.clear();
    };

    this.hideError = function () {
        _.forEach(self.filters(), function (f) { f.clearError(); });
    };

    this.getFilters = function () {
        return _.map(_.filter(self.filters(), function (f) { return f.isAdded(); }), function (f) { return f.makeFilter(); });
    };
}