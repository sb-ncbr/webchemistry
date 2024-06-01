
function PatternQueryExplorerExamplesViewModel(vm) {
    "use strict";

    var self = this;

    this.exampleList = [
        { title: "Residues containing a ruthenium ion", query: 'Atoms("Ru").ConnectedResidues(0)', ids: ["2cfl", "2cg1", "4gqj", "3npl", "4lti"] },
        { title: "Residues including a pyranose ring, excluding nucleotides", query: 'Rings(5*["C"] + ["O"])\n  .ConnectedResidues(0)\n  .Filter(lambda l: l.Count(Atoms("P")) == 0)', ids: ["1ild", "2oyh", "2zml", "3lmy", "4pir"] },
        { title: "Backbone of small proteins", query: 'AtomNames("N", "Ca", "C").Inside(AminoAcids()).Union()', ids: ["1f0g", "1kfm", "1pvb", "3thg"] }
    ];

    this.apply = function (example) {

        vm.status.setBusy(true);
        vm.status.message('Loading example...');
        $.ajax({ url: PatternQueryExplorerActions.setStructuresProvider(example.ids), type: 'GET', dataType: 'json' })
        .done(function (result) {
            vm.status.setBusy(false);
            if (result['error']) {
                if (result.errorType === 'generic') {
                    vm.log.error(result.message);
                } else if (result.errorType === 'missing') {
                    // TODO: show message or ignore?
                } else if (result.errorType === 'db') {
                    vm.log.errorSet("One or more load error(s).", result.messages);
                } else {
                    vm.log.error("Oops, something went terribly wrong. Please try again later.");
                }
                return;
            }
            vm.structures.set(result.AllStructures);
            vm.log.message("Loaded example `" + example.title + "`");
            vm.query.setQuery(example.query);
            vm.motifs.clear(true);

            setTimeout(function () { vm.query.query(); }, 50);
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            console.log(errorThrown);
            vm.log.error("Oops, something went terribly wrong. Please try again later.");
            vm.status.setBusy(false);
        });        
    };

    this.clear = function () {
        if (!confirm('Do you really want to clear the session?')) return;

        vm.query.setQuery("");
        vm.motifs.clear(true);
        vm.structures.removeAll(true);
    };
}