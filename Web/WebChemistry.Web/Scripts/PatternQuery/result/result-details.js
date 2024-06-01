function MotifDetailsViewModel(summary, vm) {
    "use strict";
    var self = this,
        initialized = false,
        initializing = false,
        shown = false;

    this.currentBy = "motif";

    this.structuresDisplayFilters = [
        { type: "All", label: "All", cls: "", count: summary.StructureWithPatternsCount },
        //{ type: "WithMotif", label: "Has Patterns", cls: "", count: summary.StructureWithPatternsCount },
        { type: "ComputationWarning", label: "Has Query Warnings", cls: "mq-computation-warning", count: summary.StructureWithComputationWarningCount },
        { type: "ReaderWarning", label: "Has Input Warnings", cls: "mq-reader-warning", count: summary.StructureWithReaderWarningCount },
        { type: "Error", label: "Has Error", cls: "mq-structure-error", count: summary.StructureWithErrorCount }
    ];

    this.structuresDisplayFilters = _.filter(this.structuresDisplayFilters, function (f) { return f.count > 0; });
    _.forEach(this.structuresDisplayFilters, function (f) {
        f.label += ' (' + f.count + ')';
    });

    this.motifsDisplayFilters = [
        { type: "All", label: "All", cls: "", count: summary.PatternCount },
        { type: "None", label: "No Issue", cls: "", count: 0 },
        { type: "ChiralityMinor", label: "Minor Chirality Issue", cls: "", count: 0 },
        { type: "Chirality", label: "Chirality Issue", cls: "", count: 0 },
        { type: "Incomplete", label: "Incomplete Structure", cls: "", count: 0 }
    ];

    this.isInitialized = ko.observable(false);
    this.failedToDownload = ko.observable(false);
    this.id = 'query-' + summary.Id.replace(/\s/g, '_');
    this.fullId = summary.Id;
    this.summary = summary;
    this.details = undefined;
    this.metadata = undefined;
    this.structureMap = {};
    this.motifMap = {};
    this.validations = {};
    this.motifsFilterText = ko.observable("");
    this.motifsFilterType = ko.observable(this.motifsDisplayFilters[0]);
    this.motifsViewCount = ko.observable({ motifs: summary.PatternCount, structures: summary.StructureWithPatternsCount });
    this.structuresViewCount = ko.observable({ motifs: summary.PatternCount, structures: summary.StructureCount });
    this.structuresFilterText = ko.observable("");
    this.structuresFilterType = ko.observable(this.structuresDisplayFilters[0]);
    this.motifsGrid = null;
    this.structuresGrid = null;
    this.view3d = null;
    
    this.currentMotif = ko.observable();
    this.currentStructure = ko.observable();

    function getValidationCss(flags) {
        if (flags.length === 0) {
            return { problem: "None", text: 'No issues', css: 'mq-motif-ok' };
        }
        if (flags.indexOf("Missing") >= 0) {
            return { problem: "Incomplete", text: 'Incomplete structure', css: 'mq-motif-incomplete' };
        }
        if (flags.indexOf("HasAll_BadChirality") >= 0) {
            if (flags.indexOf("HasAll_BadChirality_Planar") >= 0) {
                return { problem: "ChiralityMinor", text: 'Chirality issues (minor)', css: 'mq-motif-minor-issue' };
            }

            return { problem: "Chirality", text: 'Chirality issues', css: 'mq-motif-chirality-issue' };
        }
        return { problem: "None", text: 'No issues', css: 'mq-motif-ok' };
    }

    function prepare(details) {
        self.details = details;
        self.validations = _.mapValues(details.ValidationFlags, getValidationCss);
        _.forEach(details.Structures, function (s) {
            s.warningCount = s.ComputationWarnings.length + s.ReaderWarnings.length;
            self.structureMap[s.Id] = s;
        });

        var motifsDisplayFiltersIndex = _.indexBy(self.motifsDisplayFilters, 'type');
        _.forEach(details.Patterns, function (m) {
            self.motifMap[m.Id] = m;
            if (m.ValidationFlagsId >= 0) motifsDisplayFiltersIndex[self.validations[m.ValidationFlagsId].problem].count++;
        });
        self.motifsDisplayFilters = _.filter(self.motifsDisplayFilters, function (f) { return f.count > 0; });
        _.forEach(self.motifsDisplayFilters, function (f) { f.label += ' (' + f.count + ')'; });

        self.metadata = new PatternQueryMetadataViewModel(self);
        
        self.view3d = new PatternQuery3DModelViewModel(self);

        if (details.Patterns.length > 0) self.currentMotif(details.Patterns[0]);
        if (details.Structures.length > 0) self.currentStructure(details.Structures[0]);

        self.isInitialized(true);

        self.view3d.init();

        self.motifsGrid = new PatternQueryGridViewModel(false, self);
        self.structuresGrid = new PatternQueryGridViewModel(true, self);

        self.motifsFilterText.subscribe(function (text) { self.motifsGrid.filterText(text); });
        self.motifsFilterType.subscribe(function (type) { self.motifsGrid.filterType(type.type); });
        self.structuresFilterText.subscribe(function (text) { self.structuresGrid.filterText(text); });
        self.structuresFilterType.subscribe(function (type) { self.structuresGrid.filterType(type.type); });

        $('#' + self.id + '-details-data a[data-details-by]').on('shown', function (e) {
            self.currentBy = $(e.target).data('details-by');
            self.showTab();
        });

        initialized = true;
        initializing = false;
        self.showTab();
    }

    function init() {
        var spinner = new Spinner({ hwaccel: true, radius: 25, length: 15, width: 8, lines: 13, color: '#000' }).spin(document.querySelector('#' + self.id + '-tab .spinner-host'));
        initializing = true;
        $.ajax({
            url: PatternQueryActions.detailsAction.replace("-query-", encodeURIComponent(summary.Id)),
            type: 'GET',
            dataType: 'json'
        })
        .done(function (data) { spinner.stop(); prepare(data); })
        .fail(function (jqXHR, textStatus, errorThrown) {
            spinner.stop();
            console.log(errorThrown);
            initializing = false;
            sel.isInitialized(true);
            self.failedToDownload(true);
        });
    }

    function show3d() {
        if (self.view3d) self.view3d.show();
    }

    function dispose3d() {
        if (self.view3d) self.view3d.hide();
    }

    this.showTab = function () {
        shown = true;
        if (initializing) return;
        if (!initialized) {
            init();
        } else {
            show3d();
            if (self.currentBy === 'motif') {
                self.motifsGrid.init();
            } else {
                self.structuresGrid.init();
            }
        }
    };

    this.hideTab = function () {
        shown = false;
        dispose3d();
    };

    this.showMotifList = function () {
        var data = "data:text/plain;charset=UTF-8," + encodeURIComponent(self.motifsGrid.toCsv());
        var w = window.open(data, '_blank');
        w.focus();
    };

    this.showStructureList = function () {
        var data = "data:text/plain;charset=UTF-8," + encodeURIComponent(self.structuresGrid.toCsv());
        var w = window.open(data, '_blank');
        w.focus();
    };


    this.setCurrentFromGrid = function (ctx, event) {
        if (!event) return;
        var type = event.target.getAttribute("data-show-type"), id;
        if (!type) return;
        id = event.target.getAttribute("data-id");
        switch (type) {
            case 'current-motif': if (self.motifMap[id]) self.currentMotif(self.motifMap[id]); break;
            case 'current-structure': if (self.structureMap[id]) self.currentStructure(self.structureMap[id]); break;
            case 'show-structure':
                if (self.structureMap[id]) {
                    $('#' + self.id + '-details-data a[data-details-by=structure]').click();
                    self.currentStructure(self.structureMap[id]);
                }
                break;
            case 'show-motifs':
                if (self.structureMap[id]) {
                    $('#' + self.id + '-details-data a[data-details-by=motif]').click();
                    self.motifsFilterText('^' + id + '_');
                }
                break;
            default: return;
        }
        event.preventDefault();
    };
}