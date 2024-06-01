var summaryData = JSON.parse(ValidatorDbParams.summaryJson);

function LookupModel() {
    "use strict";

    function makeListComponent(ids) {
        var names = ids.split(",").map(function (n) { return n.trim().toUpperCase(); }).filter(function (n) { return n.length > 0; });
        if (names.length > 0) {
            return {
                type: names.every(function (n) { return n.length <= 3; }) ? "model" : 
                    (names.every(function (n) { return n.length === 4; }) ? "structure" : "none"),
                uri: names.sort().join(",")
            };
        }
        return null;
    }
    var self = this;

    var regex = /^[a-z0-9]*$/i;

    this.id = ko.observable("");
    this.canLookup = ko.computed(function () {
        var names = self.id().split(",").map(function (n) { return n.trim(); }).filter(function (n) { return n.length > 0; });
        return names.length <= 10 && names.length > 0
            && names.every(function (n) { return regex.test(n); })
            && (names.every(function (n) { return n.length <= 3; }) || names.every(function (n) { return n.length === 4; }));
    });

    this.lookup = function () {
        if (!self.canLookup()) {
            return;
        }

        var t = makeListComponent(self.id());
        if (t.type === "model") {
            //var win = window.open(ValidatorDbParams.perModelAction.replace("-id-", t.uri));
            //win.focus();
            window.location.href = ValidatorDbParams.perModelAction.replace("-id-", t.uri);
        } else if (t.type === "structure") {
            //var win = window.open(ValidatorDbParams.perStructureAction.replace("-id-", t.uri));
            //win.focus();
            window.location.href = ValidatorDbParams.perStructureAction.replace("-id-", t.uri);
        }
    };

    this.lookupButtonLabel = ko.computed(function () {
        if (self.id().trim().length === 0 || self.canLookup()) {
            return "Quick Lookup";
        } else {
            return "Incorrect Input";
        }
    });
}

var lookupModel = new LookupModel();
var wwPdbDictModel;

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
    $("#motifCount").text(summaryData.Analyzed + summaryData.NotAnalyzed);
    //ko.applyBindings(lookupModel, document.getElementById("lookup-form"));
    ko.applyBindings(new CustomAnalysisModel(), document.getElementById("custom-analysis"));
    $("#custom-analysis a[data-do-tooltip]").tooltip();
    //ko.applyBindings(summaryData, document.getElementById("summary"));

    //wwPdbDictModel = new WwPdbDictViewModel();

    var summaryInitialized = false, detailsModelDownloaded = false, detailsStructureDownloaded = false,
        detailsModel, detailsStructure;
    $('a[data-toggle="tab"]').on('shown', function (e) {
        var href = $(e.target).attr('href');
        if (href === "#summary") {
            if (!summaryInitialized) {
                makeOverviewPlot(summaryData, "summary-plot");
                summaryInitialized = true;
            }
        } else if (href === "#model-details") {
            if (!detailsModelDownloaded) {
                detailsModelDownloaded = true;
                detailsModel = downloadByModelDetails();
            } else {
                if (detailsModel.refresh) { detailsModel.refresh(); }
            }
        } else if (href === "#structure-details") {
            if (!detailsStructureDownloaded) {
                detailsStructureDownloaded = true;
                detailsStructure = downloadByStructureDetails();
            } else {
                if (detailsStructure.refresh) { detailsStructure.refresh(); }
            }
        }
        //if ($(e.target).attr('href') === "#wwpdbdict") {
        //    wwPdbDictModel.init();
        //}
    });
});