function QueryViewModel(vm) {
    "use strict";

    var addLabelText = 'Add <i class="icon-plus icon-white"></i>';

    var self = this, adding = false;

    var serial = vm.newSerial();
    this.serial = serial;
    this.isValidating = ko.observable(false);
    this.canAdd = ko.observable(false);
    this.addLabel = ko.observable(addLabelText);
    this.isAdded = ko.observable(false);
    this.errorMessage = ko.observable("");
    this.queryId = ko.observable("");
    this.queryText = ko.observable("");
    this.editor = new PatternQueryQueryEditor();
    this.showExamples = ko.observable(false);
    this.examples = ko.observableArray([]);
    
    this.initEditor = function () {        
        if (document.getElementById('query-input-' + serial)) self.editor.init('query-input-' + serial);
    };

    this.isAdded.subscribe(function (added) {
        if (!added) {
            self.editor.destroy();
            self.showExamples(false);
        }
    });

    this.isValidating.subscribe(function (validating) {
        if (self.isAdded()) return;
        self.editor.setReadOnly(validating);
        self.showExamples(false);
    });

    this.queryId.subscribe(updateCanAdd);
    this.queryText.subscribe(updateCanAdd);

    function validate(onOk) {
        self.editor.setValue(self.queryText());

        onOk = onOk || function () { };

        self.canAdd(false);
        self.addLabel("Validating");
        self.isValidating(true);
        $.ajax(PatternQueryActions.validateQueryAction, {
            dataType: 'json',
            data: { query: self.normalizeQuery() },
            success: function (data) {
                if (data.isOk) {
                    onOk();
                } else {
                    self.canAdd(false);
                    self.addLabel(addLabelText);
                    showError('query-text', data.error);
                }
            },
            error: function (x, y, z) {
                self.canAdd(true);
                self.addLabel(addLabelText);
                showError('query-text', 'Unexpected error validating the query. Please try again later.');
                console.log(z);
            },
            complete: function () { self.isValidating(false); }
        });
    }

    function updateCanAdd() {
        if (adding || self.isAdded()) return;

        var id = self.queryId().trim(), text = self.queryText().trim();

        if (id.length === 0 && text.length === 0) {
            self.canAdd(false);
            self.addLabel(addLabelText);
            clearErrors();
            return;
        }

        var error = "";
        if (id.length === 0) {
            error = "Enter unique name.";
        } else if (id.length > 25) {
            error = "The name is too long. Max. 25 characters are allowed.";
        } else if (!/^[a-z0-9\s_-]+$/i.test(id)) {
            error = "This is not a legal name. Only letters, numbers, spaces, -, and _ are allowed.";
        } else if (!vm.validateQueryId(self, id)) {
            error = "This is not an unique name.";
        }

        if (error.length > 0) {
            showError('query-id', error);
            self.canAdd(false);
            self.addLabel(addLabelText);
            return;
        } else {
            clearError('query-id');
        }

        if (text.length === 0) {
            error = "Enter query.";
        } else if (text.length > 1500) {
            error = "The query is too long. Max. 1500 are allowed.";
        }

        if (error.length > 0) {
            showError('query-text', error);
            self.canAdd(false);
            self.addLabel(addLabelText);
            return;
        } else {
            clearError('query-text');
        }

        clearErrors();
        self.canAdd(true);
        self.addLabel(addLabelText);
    }

    function getInputWrap(name) {
        return $('#query-' + serial + ' .mq-' + name + '-wrap');
    }

    var shownErrors = {};

    function clearError(name) {
        if (!shownErrors[name]) return;
        var $wrap = getInputWrap(name);
        $wrap.removeClass("error");
        $wrap/*.find("input")*/.tooltip('destroy');
        shownErrors[name] = '';
    }

    function clearErrors() {
        _.forEach(shownErrors, function (e, n) { clearError(n); });
    }

    this.clearErrors = clearErrors;

    function showError(name, message) {
        if (shownErrors[name] === message) return;

        var $wrap = getInputWrap(name);
        $wrap.addClass("error");
        $wrap//.find("input")
            .tooltip('destroy')
            .tooltip({
                animation: false,
                title: message,
                placement: 'bottom',
                trigger: 'manual'
            })
            .tooltip('show');
        shownErrors[name] = message;
    }

    this.add = function (data, event) {
        updateCanAdd();
        if (!self.canAdd()) return;

        self.editor.blur();

        adding = true;
        self.queryId(self.queryId().trim());
        self.queryText(self.queryText().trim());
        adding = false;

        validate(function () {
            clearErrors();
            vm.add();            
            self.isAdded(true);
        });
    };

    this.remove = function () {
        vm.remove(this);
    };
    
    this.setCurrentQuery = function () {
        var qs = vm.queries(),
            box = qs[qs.length - 1];

        box.editor.setValue(self.queryText());
        box.editor.focus();
    }

    this.insertExample = function (example) {
        self.editor.insertValue(example.Query);
        self.editor.focus();
    };

    this.setExample = function (example) {
        self.editor.setValue("");
        self.editor.insertValue(example.Query);
        self.editor.focus();
    };
    
    this.updateExamples = function (help) {
        self.showExamples(true);
        self.examples.removeAll();
        _.forEach(help.Examples, function (e) {
            self.examples.push(e);
        });
    };

    this.editor.on('changed', function (value) {
        if (self.isAdded()) return;
        self.queryText(value);
    });
    this.editor.on('enterPress', this.add);
    this.editor.on('help', self.updateExamples);

    var blurTimeout = null;
    this.editor.on('focus', function () {
        if (blurTimeout !== null) {
            clearTimeout(blurTimeout);
            blurTimeout = null;
        }
        self.showExamples(true);
        var wrap = getInputWrap('query-text');
        if (wrap[0]) {
            wrap[0].style.height = "100px";
        }

    });
    this.editor.on('blur', function () {
        if (blurTimeout !== null) {
            clearTimeout(blurTimeout);
        }
        blurTimeout = setTimeout(function () {
            self.showExamples(false);
            var wrap = getInputWrap('query-text');
            clearErrors();
            if (wrap[0]) {
                wrap[0].style.height = "30px";
            }
        }, 250);
    });

    this.normalizeQuery = function () {
        //var re = /[ \t]*(\r\n|\n\r|\n|\r)[ \t]*/g;
        //return self.queryText().replace(re, '');
        return "(" + self.queryText() + ")";
    }
}

function QueriesViewModel() {
    var self = this;

    var querySerial = 0,
        version = 0,
        maxQueries = 10;

    this.newSerial = function () { return ++querySerial; };

    this.queryCount = ko.observable(0);
    this.queries = ko.observableArray([new QueryViewModel(this)]);
    this.changed = ko.observable();
    
    this.initFirstEditor = function () {
        var xs = self.queries();
        if (xs.length === 1 && !xs[0].isAdded()) xs[0].initEditor();
    };

    this.validateQueryId = function (p, id) {
        id = id.toLowerCase();
        return !_.some(self.queries(), function (q) { return p !== q && q.isAdded() && q.queryId().toLowerCase() === id; });
    };

    this.add = function () {
        self.queryCount(self.queryCount() + 1);
        if (self.queries().length < maxQueries) {
            var nq = new QueryViewModel(self);
            self.queries.push(nq);
            $("#query-" + querySerial + " .mq-query-id").focus();
            $("#query-" + querySerial + " .btn").tooltip();
            self.changed(++version);
            nq.initEditor();
            return nq;
        }
        self.changed(++version);
        return null;
    };

    this.remove = function (model) {
        self.queries.remove(model);
        self.queryCount(self.queryCount() - 1);
        var qs = self.queries();
        if (qs.length === 0 || qs[qs.length - 1].isAdded()) {
            var nq = new QueryViewModel(self);
            self.queries.push(nq);
            $("#query-" + querySerial + " .mq-query-id").focus();
            $("#query-" + querySerial + " .btn").tooltip();
            self.changed(++version);
            nq.initEditor();
            return;
        }
        self.changed(++version);
    };

    this.clear = function () {
        _.forEach(self.queries(), function (q) { q.clearErrors(); q.editor.blur(); });
        self.queries.removeAll();
        self.queryCount(0);
        self.changed(++version);
    };

    this.hidePopups = function () {
        _.forEach(self.queries(), function (q) { q.clearErrors(); q.editor.blur(); });
    };

    this.getQueries = function () {
        return _.map(_.filter(self.queries(), function (q) { return q.isAdded(); }), function (q) { return { Id: q.queryId(), QueryString: q.normalizeQuery() }; });
    };
}