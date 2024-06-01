function __RecentlySubmittedComputations() {
    function empty() {
        return { submitted: [] };
    }

    function get(appName) {
        if (typeof (Storage) === "undefined") {
            return empty();
        }

        var data = localStorage.getItem(appName);
        try {
            var ret = JSON.parse(data);
            if (ret.submitted === undefined) {
                return empty();
            }
            return ret;
        } catch (e) {
            return empty();
        }
    }

    function store(appName, data) {
        if (typeof (Storage) === "undefined") { return; }
        if (data.submitted.length > 10) {
            data.submitted = data.submitted.slice(data.submitted.length - 10);
        }
        localStorage.setItem(appName, JSON.stringify(data));
    }

    this.submit = function (appName, id, hint) {
        try
        {
            if (typeof (Storage) === "undefined") { return; }
            var data = get(appName);
            data.submitted.push({ 'id': id, 'timestamp': new Date().getTime(), 'hint': hint });
            store(appName, data);
        } catch (e) {
            console.log("Error updating recent computation list: " + e);
        }
    };

    this.getSubmitted = function (appName) {
        var ret = get(appName);
        ret.submitted.reverse();
        return ret;
    };

    this.clear = function (appName) {
        if (typeof (Storage) === "undefined") {
            return;
        }
        localStorage.removeItem(appName);
    };
}

var RecentlySubmittedComputations = RecentlySubmittedComputations || new __RecentlySubmittedComputations();