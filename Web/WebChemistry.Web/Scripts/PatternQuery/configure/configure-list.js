function MoleculeListModel(opts) {
    var self = this;

    var timer = null,
        entryList = [],
        version = 0;

    opts = opts || {};

    this.changed = ko.observable();
    this.listText = ko.observable("");
    this.enteredCountText = ko.observable("0");
    this.isValid = true;
    this.isEmpty = function () {
        return entryList.length === 0;
    };
    this.getList = function () {
        return entryList;
    };

    this.missingEntries = [];
    this.hasMissingEntries = ko.observable(false);
    this.missingEntryCountText = ko.observable("0 entries");
    this.missingEntryLink = ko.observable("#")

    this.setMissingEntries = function (xs) {
        self.missingEntries = xs;
        self.missingEntryCountText(xs.length + " entries");
        self.missingEntryLink("data:text/plain;charset=UTF-8," + encodeURIComponent(xs.join('\n')));
        self.hasMissingEntries(true);
    };

    this.removeMissingEntries = function () {
        var ys = {};
        _.forEach(self.missingEntries, function (x) { ys[x.toLowerCase()] = true; });
        var newList = _.filter(entryList, function (e) { return !ys[e]; });
        self.listText(newList.join(','));
        self.hasMissingEntries(false);
    }

    function getUniqueList(value) {
        var list = _.filter(_.map(value.replace('\r', '').split(/[\n,]/), function (n) { return n.trim(); }), function (n) { return n.length > 0; }),
            uniq = {};
        _.forEach(list, function (e) { uniq[e.toLowerCase()] = true; });
        return _.keys(uniq);
    }

    function updateChanged(value) {
        if (timer !== null) {
            clearTimeout(timer);
            timer = null;
        }

        var all = getUniqueList(value);
        var regex = /^[a-z0-9]{4}$/i;
        var invalid = all.filter(function (n) { return !regex.test(n); });

        self.enteredCountText((all.length - invalid.length).toString());
        if (invalid.length > 0) {
            if (invalid.length === 1) {
                showError("'" + invalid[0] + "' is not a valid PDB identifier.");
            } else {
                showError("'" + invalid[0] + "' and " + (invalid.length - 1).toString() + " more are not a valid PDB identifier.");
            }
            isValid = false;
            return;
        }

        if (window['MqDatabaseInfo'] && all.length > MqDatabaseInfo.structureCount) {
            showError("The database only contains " + MqDatabaseInfo.structureCount + " entries. Entering " + all.length + " seems like an overkill.");
            isValid = false;
            return;
        }

        showError("");
        entryList = all;
        self.isValid = true;
    }
    this.updateChanged = updateChanged;
    this.hideError = function () { showError(""); }

    var errorMessage = "";

    this.errorMessage = ko.observable("");

    function showError(message) {
        self.errorMessage(message);

        if (errorMessage === message) return;
        var $wrap = $("#list-tab .control-group");
        if (!message) {
            $wrap.removeClass("error");
            $wrap.tooltip('destroy');
            errorMessage = "";
            return;
        }
        $wrap.addClass("error");
        
        if (!opts.hideErrorTooltip) {
            $wrap
                .tooltip('destroy')
                .tooltip({
                    animation: false,
                    title: message,
                    placement: 'bottom',
                    trigger: 'manual'
                })
                .tooltip('show');
        }
        errorMessage = message;
    }

    this.listText.subscribe(function (value) {
        self.isValid = false;
        self.changed(++version);
        if (timer !== null) {
            clearTimeout(timer);
            timer = null;
        }
        timer = setTimeout(function () { updateChanged(value); self.changed(++version); }, 250);
    });
}