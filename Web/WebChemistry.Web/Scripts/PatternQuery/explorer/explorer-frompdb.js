function PatternQueryExplorerFromPdbViewModel(vm, host) {
    "use strict";

    var self = this,
        $host = $('#' + host),
        maxAtTheSameTime = 100;

    $host.find("a[data-toggle=tab]").on('shown', function (e) {
        if ($(e.target).attr('href') === '#filter-tab') self.dataSource("Filtered");
        else self.dataSource("List");
    });

    this.filtersModel = new DatabaseFiltersViewModel();
    this.listModel = new MoleculeListModel({ hideErrorTooltip: true });
    this.dataSource = ko.observable("Filtered");
    this.selectedCountString = ko.observable("0 selected");

    var subscriptions = [];

    self.filtersModel.preview.previewChanged.subscribe(function () {
        _.forEach(subscriptions, function (s) { s.dispose(); });
        subscriptions = [];

        _.forEach(self.filtersModel.preview.currentPreview, function (p) {
            if (p.sel) {
                subscriptions.push(p.sel.subscribe(update));
            }
        });

        self.canAddDisplayed(self.filtersModel.preview.currentPreview.length > 0);
    });

    var batchUpdate = false;
    function update() {
        if (batchUpdate) return false;

        var count = 0, isValid = true;
        if (self.dataSource() === "Filtered") {
            _.forEach(self.filtersModel.preview.currentPreview, function (p) {
                if (p.sel && p.sel()) count++;
            });
        } else {
            count = self.listModel.getList().length;
            isValid = self.listModel.isValid;
        }        

        var canAdd = count > 0 && count <= maxAtTheSameTime && isValid;
        self.canAdd(canAdd);
        self.selectedCountString(count + " selected" + (count > maxAtTheSameTime ? " - too many, at most " + maxAtTheSameTime + " can be added" : ""));
        return canAdd;
    }

    this.listModel.changed.subscribe(update);

    this.filtersModel.preview.toggleFilterSelection = function () {
        batchUpdate = true;
        var prev = self.filtersModel.preview.currentPreview,
            sel = _.some(prev, function (p) { return p.sel && p.sel(); });
        _.forEach(prev, function (p) {
            if (p.sel) p.sel(!sel);
        });
        batchUpdate = false;
        update();
    };

    this.canAdd = ko.observable(false);
    this.canAddDisplayed = ko.observable(false);
    
    this.addDisplayed = function () {
        add(_.map(self.filtersModel.preview.currentPreview, 'id'));
    };

    this.addSelected = function () {
        add(_.map(_.filter(self.filtersModel.preview.currentPreview, function (p) { return p.sel(); }), 'id'));
    };

    this.addEntries = function () {
        add(self.listModel.getList());
    }

    function add(list) {
        var submitSource = self.dataSource();
        
        vm.status.setBusy(true);
        vm.status.message('Importing entries...');
        $.ajax({ url: PatternQueryExplorerActions.addFromPdbActionProvider(list), type: 'GET', dataType: 'json' })
        .done(function (result) {
            vm.status.setBusy(false);
            if (result['error']) {
                if (result.errorType === 'generic') {
                    vm.log.error(result.message);
                } else if (result.errorType === 'missing') {
                    if (submitSource === 'Filtered') {
                        vm.log.error("Oops, we've ran into some problems with our database. Please try again later.");
                    } else {
                        vm.log.message("Some of the entries you specify are not present in our database.");
                        self.listModel.setMissingEntries(result.entries);
                    }
                } else if (result.errorType === 'db') {
                    vm.log.errorSet("One or more import error(s).", result.messages);
                } else {
                    vm.log.error("Oops, something went terribly wrong. Please try again later.");
                }
                return;
            }
            vm.structures.set(result.AllStructures);
            vm.log.addStructures(result);
            self.hide();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            console.log(errorThrown);
            vm.log.error("Oops, something went terribly wrong. Please try again later.");
            vm.status.setBusy(false);
        });
    };

    this.show = function () {
        $host.show();
    };

    this.hide = function () {
        $host.hide();
    };

    this.toggle = function () {
        $host.toggle();
    };
}