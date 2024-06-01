function CustomAnalysisModel() {
    var self = this;
    
    this.searchModes = [
        {
            setCurrent: function () { updateSearchMode(this); },
            tooltip: "Validation results for molecules in selected PDB entries",
            title: "PDB Entry",
            structures: "fromText",
            models: "includeAll",
            sampleStructures: "3D12, 3D11\n2VWD, 2VSM",
            sampleStructuresShort: "2H6O"
        },
        {
            setCurrent: function () { updateSearchMode(this); },
            tooltip: "Validation results for selected molecules",
            title: "Molecule Identifier",
            structures: "fromText",
            models: "includeAll",
            sampleStructures: "3D11: 1210 A\n3D12: 1361 A; 1362 A; 1363 A; 1364 A; 1365 A; 1366 A; 1367 A",
            sampleStructuresShort: "3D12: 1362 A",
            ligandSearch: true
        },
        {
            setCurrent: function () { updateSearchMode(this); },
            tooltip: "Validation results for molecules with specific annotations",
            title: "Molecule Annotation",
            structures: "includeAll",
            models: "fromText",
            sampleModels: "HEM, HEC, DHE, HAS, HDM",
            sampleModelsShort: "MAN"
        },
        {
            setCurrent: function () { updateSearchMode(this); },
            tooltip: "Validation results for molecules with specific annotations, from selected PDB entries",
            title: "PDB Entry and Molecule Annotation",
            structures: "fromText",
            models: "fromText",
            sampleStructures: "3D12, 3D11\n2VWD, 2VSM",
            sampleStructuresShort: "3D12",
            sampleModels: "MAN\nNAG",
            sampleModelsShort: "NAG"
        }
    ];
    
    function updateSearchMode(mode) {
        self.currentSearchMode(mode);
        self.pdbEntriesSource(mode.structures);
        self.modelsSource(mode.models);
        self.isLigandSearch(!!mode['ligandSearch']);
        self.pdbEntriesText("");
        self.modelsText("");
        self.updateCanCompute();
    }

    this.showExample1 = function () {
        var mode = self.currentSearchMode();
        self.pdbEntriesText(mode['sampleStructuresShort'] || "");
        self.modelsText(mode['sampleModelsShort'] || "");
    };

    this.showExample2 = function () {
        var mode = self.currentSearchMode();
        self.pdbEntriesText(mode['sampleStructures'] || "");
        self.modelsText(mode['sampleModels'] || "");
    };

    var maxSmallSize = 10;
    var specficResSeparator = ":", specificResDelimiter = ";";

    this.currentSearchMode = ko.observable(null);

    this.isLigandSearch = ko.observable(false);

    this.pdbEntriesSource = ko.observable("includeAll");
    this.modelsSource = ko.observable("includeAll");

    this.pdbEntriesText = ko.observable("");
    this.modelsText = ko.observable("");

    this.canCompute = ko.observable(false);
    this.computeLabel = ko.observable("");

    this.specifyPdbEntered = ko.observable("");
    this.specifyModelEntered = ko.observable("");

    this.canEdit = ko.observable(true);
    
    function splitIds(text) {
        return _.map(
            text.replace('\r', '').split(/[\n,]/),
            function (n) {
                //var ret = n.trim();
                //var ci = ret.indexOf(':');
                //if (ci >= 0) {
                //    ret = ret.substr(0, ci);
                //}
                return n.trim();
            }).filter(function (n) { return n.length > 0; });
    }

    function isQuickSearch(pc, mc) {
        return (pc > 0 && pc <= maxSmallSize) || (mc > 0 && mc <= maxSmallSize && pc <= maxSmallSize);
    }
    
    this.updateCanCompute = function () {
        var pes = self.pdbEntriesSource(), mes = self.modelsSource();

        self.specifyPdbEntered("");
        self.specifyModelEntered("");

        if (pes === "includeAll" && mes === "includeAll") {
            self.canCompute(false);
            self.computeLabel("Nothing to search for");
            return;
        }

        var pet = self.pdbEntriesText().trim(), met = self.modelsText().trim();
        if (pes === "fromText" && pet.length === 0) {
            self.canCompute(false);
            self.computeLabel(self.currentSearchMode()['ligandSearch'] ? "Enter some molecule identifiers" : "Enter some PDB identifiers");
            return;
        }

        if (mes === "fromText" && met.length === 0) {
            self.canCompute(false);
            self.computeLabel("Enter some molecules");
            return;
        }

        self.canCompute(true);

        if (pet.length < 500 && met.length < 200) {
            var pdbList = splitIds(pet), modelList = splitIds(met);

            if (pdbList.length === 0 || pes === "includeAll") {
                self.specifyPdbEntered("");
            } else if (pdbList.length <= maxSmallSize) {
                self.specifyPdbEntered("" + pdbList.length.toString() + " entered");
            } else {
                self.specifyPdbEntered("more than " + maxSmallSize + " entered");
            }
            
            if (modelList.length === 0 || mes === "includeAll") {
                self.specifyModelEntered("");
            } else if (modelList.length <= maxSmallSize) {
                self.specifyModelEntered("" + modelList.length.toString() + " entered");
            } else {
                self.specifyModelEntered("more than " + maxSmallSize + " entered");
            }


            var isSpecific = false;
            _.forEach(pdbList, function (e) {
                if (e.indexOf(specficResSeparator) > 0) {
                    isSpecific = true; return false;
                }
                return true;
            });

            if (isSpecific && pdbList.length > maxSmallSize) {
                self.canCompute(false);
                self.computeLabel("Molecule identifier search can only be used when at most " + maxSmallSize.toString() + " entries are specified.");
                return;
            }

            if (isQuickSearch(pdbList.length, modelList.length)) {
                self.computeLabel("<i class='icon-search icon-white'></i> Quick Search");
                return;
            }
        }

        self.computeLabel("<i class='icon-search icon-white'></i> Slow Search");
    };

    var pdbRegex = new RegExp("^[ \\" + specficResSeparator + specificResDelimiter + "a-z0-9]+$", "i"),
        modelRegex = new RegExp("^[a-z0-9]+$", "i");

    var inNewTab = false;

    var computeInternal = function () {
        var i, pes = self.pdbEntriesSource(), mes = self.modelsSource();

        var pdbList = [],
            isSpecificResidue = false;        
        if (pes !== "includeAll") {
            pdbList = splitIds(self.pdbEntriesText());
            for (i in pdbList) {
                if (pdbList[i].length < 4 || !pdbRegex.test(pdbList[i])) {
                    self.canCompute(false);
                    self.computeLabel("'" + pdbList[i] + "' is not a valid PDB ID.");
                    return;
                }
                if (pdbList[i].indexOf(specficResSeparator) > 0) {
                    isSpecificResidue = true;
                }
            }
        }

        var modelList = [];
        if (mes !== "includeAll") {
            modelList = splitIds(self.modelsText());
            for (i in modelList) {
                if (modelList[i].length > 3 || !modelRegex.test(modelList[i])) {
                    self.canCompute(false);
                    self.computeLabel("'" + modelList[i] + "' is not a valid molecule annotation.");
                    return;
                }
            }
        }

        var hint = "";
        if (pes !== "includeAll") {
            hint = _.first(pdbList, 3).join(', ');
            if (pdbList.length > 3) {
                hint += ", and " + (pdbList.length - 3).toString() + " more";
            } 
        } else {
            hint = "All PDB entries";
        }

        hint += "; ";
        if (mes !== "includeAll") {
            hint += _.first(modelList, 3).join(', ');
            if (modelList.length > 3) {
                hint += ", and " + (modelList.length - 3).toString() + " more";
            }
        } else {
            hint += "all molecules";
        }
        
        if (isSpecificResidue && pdbList.length > maxSmallSize) {
            self.canCompute(false);
            self.computeLabel("Molecule identifier search can only be used when at most " + maxSmallSize.toString() + " entries are specified.");
            return;
        }

        if (isQuickSearch(pdbList.length, modelList.length)) {
            var url = ValidatorDbParams.searchAction + "?";
            if (pdbList.length > 0) {
                url += "structures=" + encodeURIComponent(pdbList.join());
            }
            if (modelList.length > 0) {
                if (pdbList.length > 0) {
                    url += "&";
                }
                url += "models=" + encodeURIComponent(modelList.join());
            }
            if (inNewTab) {
                var win = window.open(url, "_blank");
                win.focus();
            } else {
                window.location.href = url;
            }
            return; 
        }
        
        self.canEdit(false);
        self.canCompute(false);
        self.computeLabel("Uploading search criteria...");
        $.ajax({
            type: "POST",
            url: ValidatorDbParams.customAnalysisAction,
            data: JSON.stringify({ 
                'structures': pes !== "includeAll" ? pdbList : null,
                'models': mes !== "includeAll" ? modelList : null
            }),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (result) {
            if (result["ok"]) {
                self.computeLabel("Searching...");
                var href = ValidatorDbParams.customAnalysisResultAction.replace("-id-", result.id);
                RecentlySubmittedComputations.submit("ValidatorDB", result.id, hint);

                if (inNewTab) {
                    var win = window.open(href, "_blank");
                    win.focus();
                } else {
                    window.location.href = href;
                }
            } else {
                self.canEdit(true);
                self.canCompute(true);
                self.computeLabel(result['error']);
                alert(result["message"]);
            }
        }).fail(function (x, y, message) {
            self.canEdit(true);
            self.canCompute(true);
            self.computeLabel(message);
            console.log(message);
            console.log(x);
            console.log(y);
        });
    };

    this.compute = function () {
        inNewTab = false;
        computeInternal();
    };

    this.computeNewTab = function () {
        inNewTab = true;
        computeInternal();
    };

    self.pdbEntriesSource.subscribe(self.updateCanCompute);
    self.modelsSource.subscribe(self.updateCanCompute);
    self.pdbEntriesText.subscribe(self.updateCanCompute);
    self.modelsText.subscribe(self.updateCanCompute);

    this.updateCanCompute();
}