function PatternQueryConfigViewModel() {
    "use strict";

    var self = this,
        emailCheck = /^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/i;

    this.filtersModel = new DatabaseFiltersViewModel();
    this.listModel = new MoleculeListModel();
    this.queryModel = new QueriesViewModel();
    this.doValidation = ko.observable(true);
    this.examplesModel = new PatternQueryExamplesModel(this);
    this.doUserNotify = ko.observable(false);
    this.userEmail = ko.observable("");
    
    this.dataSource = ko.observable("Db"); // Db/List/Filters

    this.canSubmit = ko.observable(true);
    var defaultSubmitLabel = '<i class="icon-white icon-cog"></i> Submit';
    this.submitLabel = ko.observable("");
    this.isSubmitting = ko.observable(false);
    this.isRedirecting = ko.observable(false);
    this.resultLink = ko.observable("#");
    this.errors = ko.observable([]);
    this.showErrors = ko.observable(false);

    this.hideElementErrors = function () {
        self.queryModel.hidePopups();
        self.filtersModel.hideError();
        self.listModel.hideError();
    };

    this.dataSource.subscribe(function (value) {
        switch (value)
        {
            case "Db": $("a[href='#pdb-tab']").click(); break;
            case "List": $("a[href='#list-tab']").click(); break;
            case "Filtered": $("a[href='#filter-tab']").click(); break;
        }

        if (value === "List") {
            self.listModel.updateChanged(self.listModel.listText());
        } else {
            self.listModel.hideError();
        }
        updateCanSubmit();
    });
    this.filtersModel.changed.subscribe(updateCanSubmit);
    this.queryModel.changed.subscribe(updateCanSubmit);
    this.listModel.changed.subscribe(updateCanSubmit);
    this.userEmail.subscribe(updateCanSubmit);
    this.doUserNotify.subscribe(updateCanSubmit);

    function updateCanSubmit() {
        if (self.queryModel.queryCount() === 0) {
            self.submitLabel("Add at least one query");
            self.canSubmit(false);
            return;
        }

        var src = self.dataSource();
        if (src === "Filtered") {
            if (self.filtersModel.filterCount() === 0) {
                self.submitLabel("Add at least one filter");
                self.canSubmit(false);
                return;
            }

            if (self.filtersModel.hasZeroMatches()) {
                self.submitLabel("Select different filters");
                self.canSubmit(false);
                return;
            }
        }

        if (src === "List") {
            if (!self.listModel.isValid) {
                self.submitLabel("Enter a valid list of PDB identifiers");
                self.canSubmit(false);
                return;
            } 
            if (self.listModel.isEmpty()) {
                self.submitLabel("Enter at least one PDB identifier");
                self.canSubmit(false);
                return;
            }
            if (self.listModel.hasMissingEntries()) {
                self.submitLabel("Remove missing entries");
                self.canSubmit(false);
                return;
            }
        }

        if (self.doUserNotify()) {
            var email = self.userEmail();
            if (email.length > 255 || !emailCheck.test(email)) {
                self.submitLabel("Invalid e-mail address");
                self.canSubmit(false);
                return;
            }
        }
        
        self.submitLabel(defaultSubmitLabel);
        self.canSubmit(true);
    }

    this.hideErrors = function () {
        self.showErrors(false);
    };

    function showErrors(errors) {
        self.errors(errors);
        self.showErrors(true);
        self.canSubmit(true);
        self.submitLabel(defaultSubmitLabel);
    }
    
    function pluralize(count, sing, plural) {
        if (count === 1) return "1 " + sing;
        return count.toString() + " " + plural;
    }

    function ellipsis(xs, len) {
        if (xs.length <= len) return xs.join(", ");
        return _.first(xs, len).join(", ") + ", ...";
    }

    function addRecentComputation(id, config) {
        var hint = pluralize(config.Queries.length, "query", "queries") + " (" + ellipsis(_.map(config.Queries, 'Id'), 3) + "), ";
        switch (config.DataSource)
        {
            case "Db": hint += "entire database"; break;
            case "List": hint += pluralize((config.EntryList || []).length, "PDB entry", "PDB entries") + " (" + ellipsis(config.EntryList || [], 3) + ")"; break;
            case "Filtered": hint += "filtered database (" + pluralize((config.Filters || []).length, "filter", "filters") + "; "
                + ellipsis(_.map(config.Filters || [], function (f) { return mqFilterData.filterMapByProperty[f.PropertyName].name; }), 3) + ")"; break;
        }
        if (config.DoValidation) {
            hint += "; with validation";
        }

        RecentlySubmittedComputations.submit("PatternQuery", id, hint);
    }
            
    function handleSubmitResult(result, config) {
        switch (result['state'])
        {
            case "missing_entries":
                self.isSubmitting(false);
                self.listModel.setMissingEntries(result.entries);
                updateCanSubmit();
                return;

            case "empty_input":
                self.isSubmitting(false);
                self.filtersModel.hasZeroMatches(true);
                updateCanSubmit();
                return;

            case "error":
                self.isSubmitting(false);
                showErrors(result.errors);
                return;

            case "ok":
                self.isSubmitting(true);
                self.canSubmit(false);
                self.submitLabel('Redirecting to the result...');
                var resultLink = PatternQueryActions.resultAction.replace("-id-", result.id);
                self.resultLink(resultLink);
                self.isRedirecting(true);
                addRecentComputation(result.id, config);
                window.location.href = resultLink;
                return;

            default:
                showErrors(['Ooops, something went terribly wrong. Please try again later.']);
        }
    }
    
    this.submit = function () {
        var src = self.dataSource();
        var config = {
            Queries: self.queryModel.getQueries(),
            DataSource: src,
            DoValidation: self.doValidation(),
            Filters: src === 'Filtered' ? self.filtersModel.getFilters() : null,
            EntryList: src === 'List' ? self.listModel.getList() : null,
            NotifyUser: self.doUserNotify(),
            UserEmail: self.doUserNotify() ? self.userEmail() : ""
        };

        //console.log(JSON.stringify(config, null, 2));
        //return;
        //handleSubmitResult({ state: "ok", id: "x" }, config);
        //return;

        var action = PatternQueryActions.submitAction;// + "?config=" + encodeURIComponent(btoa(JSON.stringify(config))),

        
        self.errors([]);
        self.showErrors(false);
        self.isSubmitting(true);
        self.canSubmit(false);
        self.submitLabel("Submitting Query...");

        $.ajax(action, {
            //dataType: 'json',
            data: { config: btoa(JSON.stringify(config)) },
            type: 'POST',
            success: function (result) {
                handleSubmitResult(result, config);
            },
            error: function (x, y, z) {
                handleSubmitResult({ state: 'error', errors: ['Ooops, something went terribly wrong. Please try again later.'] });                
                console.log(z);
            }
        });
    };
    
    updateCanSubmit();
}

function PatternQueryExplorerSession() {
    "use strict";

    var self = this,
        startLabel = "<b>Create PatternQuery Explorer Session</b>";

    this.buttonLabel = ko.observable(startLabel);
    this.canCreate = ko.observable(true);
    this.sessionName = ko.observable("");
    this.sessionCreated = ko.observable(false);
    this.sessionUrl = ko.observable("");
    
    this.createSession = function () {
        if (!self.canCreate()) return;

        var name = self.sessionName().trim().replace(/[^\sa-zA-Z0-9-_]/g, "");
        if (name.length > 25) name = name.substr(0, 25);
        if (name.length === 0) name = 'Unnamed Session';

        var action = PatternQueryActions.createSessionAction.replace('-name-', encodeURIComponent(name));
        self.canCreate(false);
        self.buttonLabel('Creating Session...');
        $.ajax({ url: action, type: 'GET', dataType: 'json' })
        .done(function (result) {
            if (result['error']) {
                alert("A MotiveQeury Explorer session could not be created. Please try again later.");
                self.buttonLabel('Creating Session...');
                self.canCreate(true);
                return;
            }
            RecentlySubmittedComputations.submit("PatternQueryExplorer", result.id, name);
            self.sessionCreated(true);
            self.sessionUrl(PatternQueryActions.explorerSessionAction.replace('-id-', result.id));
            self.buttonLabel('Redirecting...');
            location.href = self.sessionUrl();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            console.log(errorThrown);            
            alert("A PatternQuery Explorer session could not be created. Please try again later.");
            self.buttonLabel(startLabel);
            self.canCreate(true);
        });
    };
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

$(function () {
    "use strict";

    ko.applyBindings({ versionList: appVersions, currentVersion: ko.observable(appVersions[0]) }, document.getElementById('versionsDownload'));
    var vm = new PatternQueryConfigViewModel();
    ko.applyBindings(vm, document.getElementById('submitComputation'));
    ko.applyBindings(vm, document.getElementById('samples'));
    vm.queryModel.initFirstEditor();

    ko.applyBindings(new PatternQueryExplorerSession(), document.getElementById('explorer-tab'));

    ko.applyBindings(new PatternQuerySupportSubmitViewModel(), document.getElementById('mq-support'));

    $('#submitComputation .btn').tooltip();

    $('#mq-main-tab-headers a[data-toggle="tab"]').on('shown', function (e) {
        vm.hideElementErrors();
    });

    $('#submitComputation a[data-toggle="tab"]').on('shown', function (e) {
        var href = $(e.target).attr('href');
        if (href === "#pdb-tab") {
            vm.dataSource("Db");
        } else if (href === "#list-tab") {
            vm.dataSource("List");
        } else {
            vm.dataSource("Filtered");
        }
    });

    $("*[data-dotooltip]").tooltip();
});
