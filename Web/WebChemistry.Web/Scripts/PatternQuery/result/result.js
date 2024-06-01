var mainViewModel = {
    template: ko.observable({ name: "empty-template", data: null })
};

function PatternQueryResultViewModel(data, resultSize) {
    "use strict";

    var self = this;

    var summary = data.summary;
    
    this.resultSize = resultSize;
    this.summary = summary;
    this.totalMotifs = 0;    
    this.totalTime = mqResultUtils.formatMsTime(summary.TotalTimeMs);
    
    this.inputModel = new PatternQueryResultInputModel(data.input, this);
    this.isLoading = ko.observable(false);

    var spinner = null;
    function updateBusy() {
        if (!self.isLoading() && spinner != null) {
            spinner.stop();
            return;
        }

        spinner = spinner || new Spinner({ hwaccel: true, radius: 25, length: 15, width: 8, lines: 13, color: '#fff' });
        spinner.spin(document.getElementById('spinner-host'));
        //console.log(spinner);
    }

    self.currentQuery = ko.observable({});

    var queryModelCache = {};
    function updateQueryModel(querySummary) {
        //self.isLoading(!self.isLoading());
        //self.currentQuery(querySummary);
        //updateBusy();
    }

    var detailsMap = {};
    _.forEach(summary.Queries, function (q) {
        self.totalMotifs += q.PatternCount;
        q.detailsModel = new MotifDetailsViewModel(q, self);

        q.showFromSummary = function () {
            $("a[href=#details-tab]").click();
            $("a[href='#" + q.detailsModel.id + "-tab']").click();
        };
        q.show = function () {
            updateQueryModel(q);
        };
        detailsMap[q.detailsModel.id] = q.detailsModel;
    });

    this.detailsMap = detailsMap;
}

var PatternQueryResultModel;

$(function () {
    function showResult(data, resultSize) {
        var minVersion = "1.0.14.11.1",
            summary = data.summary;
        
        if (!mqResultUtils.isVersionOk(summary.ServiceVersion, minVersion)) {
            mainViewModel.template({ name: "old-version-template", data: { current: summary.ServiceVersion, required: minVersion, resultSize: resultSize } });
            $("#resultUrl").click(function () { $(this).select(); });
            return;
        }

        PatternQueryResultModel = new PatternQueryResultViewModel(data, resultSize);
        mainViewModel.template({ name: "result-template", data: PatternQueryResultModel });

        $('a[data-query-tab-header]').on('shown', function (e) {
            var id = $(e.target).data('query-tab-header');
            _.forEach(PatternQueryResultModel.summary.Queries, function (q) {
                if (q.detailsModel.id === id) q.detailsModel.showTab();
                else q.detailsModel.hideTab();
            });
        });

        $('a[data-tabs="main"]').on('shown', function (e) {
            if ($(e.target).attr('href') === "#input-tab") {
                PatternQueryResultModel.inputModel.showTab();
            } else {
                PatternQueryResultModel.inputModel.hideTab();
            }
        });

        $("#resultUrl").click(function () { $(this).select(); });
    }

    function defaultMessageFormatter(status) {
        if (status.IsIndeterminate) {
            return status.Message;
        } else {
            return status.Message + " " + status.CurrentProgress + "/" + status.MaxProgress;
        }
    }

    function statusMessageFormatter(status) {
        try {
            var custom = JSON.parse(status.CustomState);
            if (!custom) {
                return defaultMessageFormatter(status);
            }
            var msg;

            if (status.IsIndeterminate) {
                msg = status.Message;
            } else {
                var percent = ((100 * status.CurrentProgress / status.MaxProgress) || 0).toFixed(1);
                msg = status.Message +
                    "<br/><br/>Queried " + percent + "% of the database <small style='color: #999'>(" + status.CurrentProgress + "/" + status.MaxProgress + ")</small>";// + status.CurrentProgress + "/" + status.MaxProgress;
            }
            msg += "<br /><small>Patterns Found: " + (custom.MotivesFound || 0) + "</small>";
            return msg;
        } catch (e) {
            return defaultMessageFormatter(status);
        }
    }


    var computationWrapper = StatusAndResultWrapper({
        mainViewModel: mainViewModel,
        isFinished: PatternQueryActions.isFinished,
        statusAction: PatternQueryActions.statusAction,
        resultAction: PatternQueryActions.summaryAction,
        resultSize: PatternQueryActions.resultSize,
        onResult: showResult,
        messageFormatter: statusMessageFormatter,

        failedTemplateName: "failed-template",

        canCancel: true,
        canceledTemplateName: "canceled-template",
        onCancel: function () {
            if (confirm("Do you really want to cancel the computation?")) {
                window.location.href = PatternQueryActions.killAction;
            }
        }
    });
    ko.applyBindings(mainViewModel, document.getElementById("mainView"));
    computationWrapper.run();
});