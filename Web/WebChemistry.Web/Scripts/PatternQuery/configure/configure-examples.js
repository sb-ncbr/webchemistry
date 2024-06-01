function PatternQueryExamplesModel(vm) {
    var self = this;

    var atomTypes = mqFilterData.filterMapByProperty['AtomTypes'],
        residueTypes = mqFilterData.filterMapByProperty['ResidueTypes'],
        ringTypes = mqFilterData.filterMapByProperty['RingFingerprints'];

    this.examples = [
        {
            name: "Testosterone - Residue Surrounding",
            queries: [{ id: "TES_ambres4", query: 'Residues("TES").AmbientResidues(4)' }],
            dataSource: "Filtered",
            filters: [{ filter: residueTypes, comparison: residueTypes.comparisonTypes[0], value: "TES" }]
        },
        {
            name: "Zinc Finger transcription factor",
            //queries: [{ id: "Zn_finger", query: 'RegularMotifs(".{2}C.{2,4}C.{3}[F|Y].{5}[AILFPGV].{2}H.{3,5}H")' }, { id: "Zn_conatm", query: 'RegularMotifs(".{2}C.{2,4}C.{3}[F|Y].{5}[AILFPGV].{2}H.{3,5}H")\n  .ConnectedAtoms(1)\n  .Filter(lambda l: l.Count(Atoms("Zn")) == 1)' }],
            queries: [{ id: "Zn_conatm", query: 'RegularMotifs(".{2}C.{2,4}C.{3}[F|Y].{5}[AILFPGV].{2}H.{3,5}H")\n  .ConnectedAtoms(1)' }],
            dataSource: "Filtered",
            filters: [{ filter: atomTypes, comparison: atomTypes.comparisonTypes[0], value: "Zn" }],
        },
        {
            name: "LecB - Lectin Binding Sites",
            queries: [{ id: "Lectins", query: 'Near(4, Atoms("Ca"), Atoms("Ca"))\n  .ConnectedResidues(1)\n  .Filter(lambda l:\n    l.Count(Or(Rings(5 * ["C"] + ["O"]), Rings(4 * ["C"] + ["O"]))) > 0)\n  .Filter(lambda l: l.Count(Atoms("P")) == 0)' }],
            dataSource: "Filtered",
            filters: [{ filter: atomTypes, comparison: atomTypes.comparisonTypes[0], value: "Ca" }, { filter: ringTypes, comparison: ringTypes.comparisonTypes[0], value: "@(C,C,C,C,O) | @(C,C,C,C,C,O)" }]
        },
        { 
            name: "Platinum-containing anti-cancer drugs and interacting molecules.",
            queries: [{ id: "CPTs", query: "Residues('CPT').ConnectedResidues(1)" }, { id: "QPTs", query: "Residues('QPT').ConnectedResidues(1)" }, { id: "1PTs", query: "Residues('1PT').ConnectedResidues(1)" }],
            dataSource: "Filtered",
            filters: [{ filter: residueTypes, comparison: residueTypes.comparisonTypes[0], value: "CPT|1PT|QPT" }]
        },
        {
            name: "MAN with surrounding residues",
            queries: [{ id: "MANs", query: "Residues('MAN').ConnectedResidues(1)" }],
            dataSource: "Filtered",
            filters: [{ filter: residueTypes, comparison: residueTypes.comparisonTypes[0], value: "MAN" }]
        }
    ];

    this.doSampleLec = function () {
        $("#submitTabBtn").click();
        self.apply(self.examples[2]);
    };

    this.doSampleTes = function () {
        $("#submitTabBtn").click();
        self.apply(self.examples[0]);
    };

    this.doSampleZn = function () {
        $("#submitTabBtn").click();
        self.apply(self.examples[1]);
    };

    this.apply = function (example) {
        self.clear();

        vm.queryModel.clear();
        _.forEach(example.queries, function (q) {
            var nq = vm.queryModel.add();
            nq.isAdded(true);
            nq.queryId(q.id);
            nq.queryText(q.query);
        });
        vm.queryModel.add();
        vm.queryModel.queryCount(example.queries.length);
        
        if (example.dataSource === "Filtered") {            
            vm.filtersModel.clear();
            var nf = new FilterViewModel(vm.filtersModel), first = true;
            _.forEach(example.filters, function (f) {
                nf.isAdded(true);
                nf.filter(f.filter);
                nf.comparisonType(f.comparison);
                nf.filterText(f.value);
                if (first) {
                    vm.filtersModel.filters.push(nf);
                    first = false;
                }
                nf = vm.filtersModel.add(nf);
            });
        }
                
        if (example.dataSource === "List") {
            vm.listModel.listText(example.listText);
        }

        vm.dataSource(example.dataSource);
    };

    this.clear = function () {
        vm.queryModel.clear();
        vm.queryModel.add();
        //vm.queryModel.queries.push(new QueryViewModel(vm.queryModel));

        vm.filtersModel.clear();
        vm.filtersModel.filters.push(new FilterViewModel(vm.filtersModel));

        vm.listModel.listText("");

        vm.dataSource("Db");
    };
}