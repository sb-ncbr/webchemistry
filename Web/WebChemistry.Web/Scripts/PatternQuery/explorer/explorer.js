
function PatternQueryExplorerViewModel() {
    var self = this;

    this.log = new PatternQueryExplorerLogViewModel(this, 'mq-explorer-log');
    this.upload = new PatternQueryExplorerUploadViewModel(this);
    this.structures = new PatternQueryExplorerStructuresViewModel(this, 'mq-explorer-structure-grid');
    this.motifs = new PatternQueryExplorerMotifsViewModel(this, 'mq-explorer-motif-grid');
    this.query = new PatternQueryExplorerQueryViewModel(this, 'mq-explorer-query-editor');
    this.status = new PatternQueryExplorerStatusViewModel(this, 'mq-explorer-overlay', 'mq-explorer-overlay-spinner');
    this.view3d = new PatternQueryExplorer3DModelViewModel(this, 'mq-explorer-3d-host', 'mq-explorer-3d-view');
    this.fromPdb = new PatternQueryExplorerFromPdbViewModel(this, 'mq-explorer-from-pdb');
    this.examples = new PatternQueryExplorerExamplesViewModel(this);

    function applyState(state) {
        if (state.QueryHistory.length > 0) {
            var pretty = mqResultUtils.prettifyQuery(state.QueryHistory[state.QueryHistory.length - 1]);
            self.query.setQuery(pretty);
        }
        self.structures.set(state.Structures);
        if (state.LatestResult) {
            self.motifs.applyResult(state.LatestResult, false);
        }
    }

    function init() {
        self.status.setBusy(true);
        self.status.message('Initializing the session...');

        $.ajax({
            url: PatternQueryExplorerActions.stateAction,
            type: 'GET',
            dataType: 'json'
        })
        .done(function (result) {
            applyState(result);
            self.status.setBusy(false);
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            console.log(errorThrown);
            self.log.error("Failed to initialized the session state. Try again later.");
            self.status.setBusy(false);
        });
    }

    this.log.message("Welcome to PatternQuery Explorer " + PatternQueryExplorerActions.version);

    init();
}

ko.bindingHandlers.executeOnEnter = {
    init: function (element, valueAccessor, allBindingsAccessor, viewModel) {
        var allBindings = allBindingsAccessor();
        $(element).keypress(function (event) {
            var keyCode = (event.which ? event.which : event.keyCode);
            if (keyCode === 13) {
                allBindings.executeOnEnter.call(viewModel, element);
                return false;
            }
            return true;
        });
    }
};

var mqExplorerModel;

$(function () {
    mqExplorerModel = new PatternQueryExplorerViewModel();
    ko.applyBindings(mqExplorerModel, document.getElementById('mq-explorer-app'));

    $("#instance-id").click(function () { $(this).select(); });
    $("*[data-dotooltip]").tooltip();
});