function __Filters(includeYear) {
    var filterTypeNumeric = "numeric",
        filterTypeString = "string",
        filterTypeRing = "ring",
        filterTypeDate = "date";

    this.filterTypeNumeric = filterTypeNumeric;
    this.filterTypeString = filterTypeString;
    this.filterTypeRing = filterTypeRing;
    this.filterTypeDate = filterTypeDate;

    var numericComparisonTypes = [
        { value: "NumberLess", text: "<" },
        { value: "NumberLessEqual", text: "≤" },
        { value: "NumberGreater", text: ">" },
        { value: "NumberGreaterEqual", text: "≥" },
        { value: "NumberEqual", text: "=" }
    ];

    var stringComparisonTypes = [
        { value: "StringEqual", text: "Exact Match" },
        { value: "StringContainsWord", text: "Contains Word" },
        { value: "StringRegex", text: "Regular Expr." }
    ];

    var ringComparisonTypes = [
        { value: "StringEqual", text: "Contains" }
    ];

    this.comparisonTypeMap = _.indexBy(numericComparisonTypes.concat(stringComparisonTypes), 'value');

    this.isNumeric = function (filterName) {
        var f = filterMapByProperty[filterName];
        return f ? f.type === filterTypeNumeric : false;
    };

    function intFilter(name, prop, desc, transform, customInfo) {
        return {
            name: name,
            property: prop,
            description: desc,
            propertyType: "Int",
            type: filterTypeNumeric,
            comparisonTypes: numericComparisonTypes,
            priority: 0,
            transform: transform || function (x) { return x; },
            customInfo: customInfo || []
        };
    }

    function doubleFilter(name, prop, desc, transform, customInfo) {
        return {
            name: name,
            property: prop,
            description: desc,
            propertyType: "Double",
            type: filterTypeNumeric,
            comparisonTypes: numericComparisonTypes,
            priority: 0,
            transform: transform || function (x) { return x; },
            customInfo: customInfo || []
        };
    }

    function dateFilter(name, prop, desc, transform, customInfo) {
        return {
            name: name,
            property: prop,
            description: desc,
            propertyType: "Date",
            type: filterTypeDate,
            comparisonTypes: numericComparisonTypes,
            priority: 0,
            transform: transform || function (x) { return x; },
            customInfo: customInfo || []
        };
    }

    function stringFilter(name, prop, desc, transform, customInfo) {
        return {
            name: name,
            property: prop,
            description: desc,
            propertyType: "StringArray",
            type: filterTypeString,
            comparisonTypes: stringComparisonTypes,
            priority: 0,
            transform: transform || function (x) { return x; },
            customInfo: customInfo || []
        };
    }

    function ringFilter(name, prop, desc, transform, customInfo) {
        return {
            name: name,
            property: prop,
            description: desc,
            propertyType: "StringArray",
            type: filterTypeRing,
            comparisonTypes: ringComparisonTypes,
            priority: 0,
            transform: transform || function (x) { return x; },
            customInfo: customInfo || []
        };
    }

    // woRd1 WorD2 -> Word1 Word2
    function capitalize(s) {
        if (!s) return "";
        return s.toLowerCase().replace(/(?:^|\s)\S/g, function (a) { return a.toUpperCase(); });
    }

    // wORd1 woRd2 -> Word1 word2
    function capitalizeFirst(s) {
        if (!s) return "";
        return s.charAt(0).toUpperCase() + s.slice(1);
    }

    function toUpper(s) {
        if (!s) return "";
        return s.toUpperCase();
    }

    function toLower(s) {
        if (!s) return "";
        return s.toLowerCase();
    }

    function formatRingFingerprint(ring) {
        var xs = ring.split('-'), len = xs.length, i, j, acc = [], x;
        for (i = 0; i < len; ) {
            j = i;
            x = xs[i];
            while (j < len && x === xs[j]) { j++; }
            if (j - i > 1) {
                acc.push(x + "<sub>" + (j - i) + "</sub>");
            } else {
                acc.push(x);
            }
            i = j;
        }
        return acc.join('');
    }

    this.ringsRender = function (s, sep) {
        if (!s) return "";
        return _.map(s.split('¦'), formatRingFingerprint).join(sep);
    };

    this.filterModelList = [
        stringFilter("Experiment method", "ExperimentMethod", "Experiment method", toUpper,
            ["For the list of present methods used for structure determination please consult the <a target='_blank' href='//mmcif.wwpdb.org/dictionaries/mmcif_pdbx_v40.dic/Items/_exptl.method.html'>wwPDB Dictionary</a>.",
             "Examples: Regular Expression/Contains Word comparison for <code>X-RAY</code> or <code>NMR</code>."]),
        doubleFilter("Resolution (Å)", "Resolution", "Resolution in Angstroms (Å)"),

        stringFilter("EC Numbers", "EcNumbers", "Enzymatic Commission numbers", undefined,
            ["When using Contains Word comparison it is possible to enter just a number prefix without the '.', for example <code>\"3.2\"</code>.", "When using Regular Expression comparison, use <code>^3\\.2\\..*</code> for the subgroup 3.2."]),

        stringFilter("Origin organisms", "OriginOrganisms", "Origin organisms", capitalize,
            ['Examples: "Escherichia Coli", "Mycobacterium Tuberculosis".']),
        stringFilter("Host organisms", "HostOrganisms", "Host organisms", capitalize,
            ['Examples: "Escherichia Coli", "Mycobacterium Tuberculosis".']),

        stringFilter("Origin organism IDs", "OriginOrganismsId", "Origin organism identifiers"),
        stringFilter("Host organism IDs", "HostOrganismsId", "Host organism identifiers"),

        stringFilter("Polymer type", "PolymerType", "Polymer type", undefined, ["Possible values: Protein, DNA, RNA, ProteinDNA, ProteinRNA, NucleicAcids, Mixture, Sugar, Other, NotAssigned."]),
        stringFilter("Protein stoichiometry", "ProteinStoichiometry", "Protein stoichiometry", undefined, ["Possible values: Monomer, Homomer, Heteromer, NotAssigned."]),
        stringFilter("Protein stoichiometry string", "ProteinStoichiometryString", "Protein stoichiometry string"),

        intFilter("Atom Count", "AtomCount", "Number of atoms in the structure"),
        stringFilter("Atoms", "AtomTypes", "Elements in the structure"),

        intFilter("Residue Count", "ResidueCount", "Number of residues in the structure"),
        stringFilter("Residues", "ResidueTypes", "Types of residues in the structure"),

        doubleFilter("Weight", "Weight", "Molecular weight (in kDa) of all non-water atoms in the asymmetric unit"),
        stringFilter("Host organism's genus", "HostOrganismsGenus", "Host organism's genus", capitalize,
            ["Examples: Homo, Mus, Mycobacterium."]),
        stringFilter("Origin organism's genus", "OriginOrganismsGenus", "Origin organism's genus", capitalize,
            ["Examples: Homo, Mus, Mycobacterium."]),

        stringFilter("Keywords", "Keywords", "Structure keywords", toUpper),
        stringFilter("Authors", "Authors", "Authors of the structure"),

        dateFilter("Release Date", "ReleasedDate", "The date the structure was released"),
        dateFilter("Latest Revision Date", "LatestRevisionDate", "The data the structure was last revised"),

        ringFilter("Rings", "RingFingerprints", "Ring fingerprints.")
    ];

    var defaultColumns = this.filterModelList.slice(0);

    if (includeYear) {
        this.filterModelList.push(intFilter("Year of publication", "YearOfPublication", "Yeah the structure was published"));
    }
    
    _.forEach(this.filterModelList, function (v, i) {
        v.priority = i;
    });

    this.defaultColumns = _.sortBy(defaultColumns, 'priority');

    var filterMapByProperty = {};
    _.forEach(this.filterModelList, function (f) { filterMapByProperty[f.property] = f; });
    this.filterMapByProperty = filterMapByProperty;

    this.filterComparer = function (a, b) {
        return a.name === b.name ? 0 : (a.name < b.name ? -1 : 1);
    };
}

var mqFilterData = new __Filters(window['includeYearToFilters']);