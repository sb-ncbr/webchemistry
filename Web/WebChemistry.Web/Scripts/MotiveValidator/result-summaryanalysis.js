function makeSummaryAnalysisElement(model, title, filterPrefix, summaryElement, suffix, columns, filter, atomsList, condition) {
    var atomNames = model.ModelNames;
    var entries = model.Entries;
    var total = 0;

    var analysisData = {};
    _.forEach(entries, function (e) {
        if (!filter(e)) {
            return;
        }

        var atoms = atomsList(e);
        if (atoms.length > 0) {
            total++;
        }
        _.forEach(atoms, function (a) {
            analysisData[a] = (analysisData[a] || 0) + 1;
        });
    });
    
    if (total > 0) {
        var data = [];
        for (var c in analysisData) {
            var key = parseInt(c);
            var analysis = {
                id: key,
                name: atomNames[c],
                link: atomNames[c] + suffix,
                modelName: model.ModelName,
                prefix: filterPrefix,
                summaryElement: summaryElement,
                count: analysisData[c],
                total: total
            };

            data.push(analysis);

            var closure = (function (name, c, modelName) {
                return {
                    name: name,
                    count: 0,
                    parentCount: 0,
                    title: filterPrefix + analysis.name,
                    columns: columns,
                    filter: function (e) { return e.ModelName === modelName && condition(e, c); }
                };
            })(analysis.link, key, model.ModelName);
            model.dataViews.push(closure);
        }

        data.sort(function (a, b) { return a.id - b.id; });
        var rows = [];
        var analysisRowWidth = 8;
        for (var i = 0; i < data.length; i += analysisRowWidth) {
            rows.push(data.slice(i, i + analysisRowWidth));
        }

        model.summaryAnalyses.push({
            title: title,
            prefix: filterPrefix,
            summaryElement: summaryElement,
            dataRows: rows,
            dataWidth: Math.min(10, data.length),
            total: total
        });
    }
}

function makeResultSummaryAnalysis(model) {
    model.summaryAnalyses = [];

    makeSummaryAnalysisElement(model, "Missing Atoms", "Missing Atom ", "Missing_Atoms", "-missing-atoms", [
            { id: "MissingAtomCount", field: "MissingAtomCount", name: "Count", sortable: true, maxWidth: countWidth },
            { id: "MissingAtoms", field: "MissingAtoms", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomList }
        ],
        function (e) { return e.MissingAtomCount > 0; },
        function (e) { return e.MissingAtoms; }, 
        function (e, c) { return e.MissingAtomCount > 0 && _.contains(e.MissingAtoms, c); });

    makeSummaryAnalysisElement(model, "Chirality Errors", "Chirality Error on ", "HasAll_BadChirality", "-chirality", [
            { id: "ChiralityMismatchCount", field: "ChiralityMismatchCount", name: "Count", sortable: true, maxWidth: countWidth },
            { id: "ChiralityMismatches", field: "ChiralityMismatches", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        function (e) { return hasAllChirality(e); },
        function (e) { return Object.keys(e.ChiralityMismatches); },
        function (e, c) { return hasAllChirality(e) && e.ChiralityMismatches.hasOwnProperty(c); });

    makeSummaryAnalysisElement(model, "Substitutions", "Substitution on Atom ", "HasAll_Substitutions", "-subst", [
            { id: "SubstitutionCount", field: "SubstitutionCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "Substitutions", field: "Substitutions", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        function (e) { return hasAll(e) && e.SubstitutionCount > 0; },
        function (e) { return Object.keys(e.Substitutions); },
        function (e, c) { return hasAll(e) && e.Substitutions.hasOwnProperty(c); });

    makeSummaryAnalysisElement(model, "Foreign Atoms", "Foreign Atom ", "HasAll_Foreign", "-foreign", [
            { id: "ForeignAtomCount", field: "ForeignAtomCount", name: "Issues", sortable: true, maxWidth: countWidth },
            { id: "ForeignAtoms", field: "ForeignAtoms", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
        ],
        function (e) { return hasAll(e) && e.ForeignAtomCount > 0; },
        function (e) { return Object.keys(e.ForeignAtoms); },
        function (e, c) { return hasAll(e) && e.ForeignAtoms.hasOwnProperty(c); });

    //makeSummaryAnalysisElement(model, "Different Naming", "Different Name on Atom ", "HasAll_NameMismatch", "-naming", [
    //        { id: "NameMismatchCount", field: "NameMismatchCount", name: "Issues", sortable: true, maxWidth: countWidth },
    //        { id: "NameMismatches", field: "NameMismatches", name: "Atoms", sortable: false, maxWidth: wideWidth, formatter: renderAtomMap }
    //    ],
    //    function (e) { return hasAll(e) && e.NameMismatchCount > 0; },
    //    function (e) { return Object.keys(e.NameMismatches); },
    //    function (e, c) { return hasAll(e) && e.NameMismatches.hasOwnProperty(c); });
}