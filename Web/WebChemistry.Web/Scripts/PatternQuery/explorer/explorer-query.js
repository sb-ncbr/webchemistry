
function PatternQueryExplorerQueryViewModel(vm, queryTarget) {
    "use strict";
    
    var self = this,
        editor = new PatternQueryQueryEditor({ fontSize: 22 }),
        queryText = ko.observable(""),
        queryInProgress = false;

    editor.init(queryTarget);

    this.canQuery = ko.observable(false);

    this.showExamples = ko.observable(false);
    this.examples = ko.observableArray([]);

    this.insertExample = function insertExample(example) {
        editor.insertValue(example.Query);
        editor.focus();
    };

    this.setExample = function (example) {
        editor.setValue("");
        editor.insertValue(example.Query);
        editor.focus();
    };

    this.updateExamples = function (help) {
        self.showExamples(true);
        self.examples.removeAll();
        _.forEach(help.Examples, function (e) {
            self.examples.push(e);
        });
    };

    editor.on('changed', queryText);
    editor.on('enterPress', function () { self.query(); });
    editor.on('help', self.updateExamples);

    var blurTimeout = null;
    editor.on('focus', function () {
        if (blurTimeout !== null) {
            clearTimeout(blurTimeout);
            blurTimeout = null;
        }
        var wrap = document.getElementById("mq-explorer-query-wrap");
        if (wrap) {
            wrap.style.height = "135px";
        }

        self.showExamples(true);
    });
    editor.on('blur', function () {
        if (blurTimeout !== null) {
            clearTimeout(blurTimeout);
        }
        blurTimeout = setTimeout(function () {
            self.showExamples(false);
            var wrap = document.getElementById("mq-explorer-query-wrap");
            if (wrap) {
                wrap.style.height = "26px";
            }
        }, 250);
    });

    this.setQuery = function (q) {
        editor.setValue(q);
    };

    this.query = function () {        
        var status = vm.status, log = vm.log;
        if (!self.canQuery() || status.isBusy() || queryInProgress) return;

        if (vm.structures.getCount() === 0) {
            log.message("No data to query. Please use the options in the top right corner to add some molecules...");
            return;
        }
        
        queryInProgress = true;
        status.setBusy(true);
        status.message('Executing the query...');
        editor.closeCompleter();
        $.ajax({ url: PatternQueryExplorerActions.queryActionProvider(self.normalizeQuery()), type: 'GET', dataType: 'json' })
        .done(function (result) {
           queryInProgress = false;
           vm.status.setBusy(false);
           if (result['error']) {
               vm.log.error("Query error: " + result.message);
               return;
           }
            //editor.blur();
           vm.fromPdb.hide();
           vm.motifs.applyResult(result, true);
           log.addMotifs(result);
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
           queryInProgress = false;
           console.log(errorThrown);
           vm.log.error("Error executing query. Please try again later. It is possible your query was too complicated and took to long to execute or that the server is just too busy at the moment.");
           vm.status.setBusy(false);
        });
    };

    this.normalizeQuery = function () {
        //var re = /[ \t]*(\r\n|\n\r|\n|\r)[ \t]*/g;
        //return queryText().replace(re, '');
        return "(" + queryText() + ")";
    }

    function updateCanQuery() {
        self.canQuery(queryText().trim().length > 0);
    }

    queryText.subscribe(updateCanQuery);
    vm.structures.countString.subscribe(updateCanQuery);

    this.blur = function () {
        editor.blur();
    };
}