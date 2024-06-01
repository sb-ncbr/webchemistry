var mainViewModel = {
    template: ko.observable({ name: "empty-template", data: null })
};

function CustomAnalysisResultModel(result) {
    "use strict";

    this.stats = JSON.parse(result.statsJson);
    this.summary = JSON.parse(result.summaryJson);
    this.missing = JSON.parse(result.missingJson);

    this.hasMissingStructures = this.stats.MissingStructureCount > 0;
    this.hasMissingModels = this.stats.MissingModelCount > 0;

    this.missingStructuresString = _.first(this.missing.Structures, 5).join(", ");
    if (this.stats.MissingStructureCount > 5) this.missingStructuresString += ", ...";
    this.missingStructuresHref = "data:text/plain;charset=UTF-8," + encodeURIComponent(this.missing.Structures.join('\n'));

    this.missingModelsString = _.first(this.missing.Models, 5).join(", ");
    if (this.stats.MissingModelCount > 5) this.missingModelsString += ", ...";
    this.missingModelsHref = "data:text/plain;charset=UTF-8," + encodeURIComponent(this.missing.Models.join('\n'));
}

function showResult(result) {
    "use strict";

    var model = new CustomAnalysisResultModel(result);
    
    mainViewModel.template({ name: "result-template", data: model });

    var summaryData = model.summary;
    makeOverviewPlot(summaryData, "summary-plot");

    var detailsModelDownloaded = false, detailsStructureDownloaded = false,
        detailsModel, detailsStructure;
    $('a[data-toggle="tab"]').on('shown', function (e) {
        if ($(e.target).attr('href') === "#model-details") {
            if (!detailsModelDownloaded) {
                detailsModelDownloaded = true;
                detailsModel = downloadByModelDetails();
            } else {
                if (detailsModel.refresh) { detailsModel.refresh(); }
            }
        }
        if ($(e.target).attr('href') === "#structure-details") {
            if (!detailsStructureDownloaded) {
                detailsStructureDownloaded = true;
                detailsStructure = downloadByStructureDetails();
            } else {
                if (detailsStructure.refresh) { detailsStructure.refresh(); }
            }
        }
    });
}

ko.bindingHandlers.stopBinding = {
    init: function () {
        return { controlsDescendantBindings: true };
    }
};

ko.virtualElements.allowedBindings.stopBinding = true;

$(function () {
    "use strict";

    function messageFormatter(status) {
        if (status.IsIndeterminate) {
            return status.Message;
        } else {
            var percent = (100 * status.CurrentProgress / status.MaxProgress).toFixed(0);
            return status.Message + " " + percent + "%";
        };
    }


    var computationWrapper = StatusAndResultWrapper({
        mainViewModel: mainViewModel,
        isFinished: ValidatorDbParams.isFinished,
        statusAction: ValidatorDbParams.statusAction,
        resultAction: ValidatorDbParams.resultAction,
        onResult: showResult,
        messageFormatter: messageFormatter
    });
    ko.applyBindings(mainViewModel, document.getElementById("mainView"));
    computationWrapper.run();
});