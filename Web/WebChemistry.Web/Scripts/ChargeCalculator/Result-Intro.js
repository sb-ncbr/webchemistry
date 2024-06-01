function startResultIntro() {
    var intro = introJs();
    var steps = [];

    function addStep(id, msg, pos) {
        if (!id || document.getElementById(id)) {
            steps.push({
                element: id ? "#" + id : undefined,
                intro: msg,
                position: id ? pos || "bottom" : undefined
            });
        }
    }

    addStep(null,
        "The <b>ACC Synopsis page</b> contains an overview of your computation, which might have consisted of multiple jobs for multiple molecules.");

    addStep("result-address-and-download",
        "Download all the results now (molecular structure files containing atomic charges, statistics of the results, information about all jobs), or return to this page later via this URL. You may also inspect the results directly in your web browser.");

    addStep("results-view",
        "<p>Each <b>molecule</b> is identified according to the name of the input file, and comprises all structural elements in the input file, from simple compounds to biomacromolecular complexes made up of proteins, nucleic acids, ligands, ions, water, etc.</p>" +
        "<p>For each molecule, your computation may have consisted of several jobs, in which case all the results will be available for comparison. Click on a molecule Id to visualize and analyze the results for this molecule in detail.</p>",
        "top");

    intro.setOptions({ steps: steps });
    intro.start();
}