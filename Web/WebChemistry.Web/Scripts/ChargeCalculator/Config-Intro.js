function startConfigIntro(elementName) {
    var intro = introJs();

    var steps = [],
        startStep = 0;

    function addStep(id, msg, pos) {
        if (!id || document.getElementById(id)) {
            steps.push({
                element: id ? "#" + id : undefined,
                intro: msg,
                position: id ? pos || "bottom" : undefined
            });

            if (elementName && id === elementName) startStep = steps.length;
        }
    }

    addStep(false,
        "You have successfully uploaded your molecule(s), and are about to set up your calculation.");

    addStep("result-address",
        "Come back to this URL for the latest state of your calculation.");

    addStep("quickComputeWrapper",
        "ACC has attempted to fill the setup form with suitable options based on the molecule(s) you uploaded. You may start the calculation immediately, or inspect and adjust the setup as needed.",
        "bottom");

    addStep("structuresWrapper",
        "These are the molecules you loaded. A <b>molecule</b> comprises of the entire contents of a single structure file." +
        "<ul><li>The highlight marks potential issues in the input file, such as improper formatting or missing H atoms. Always check the messages.</li>" +
        "<li>Make sure the total charge is correct for each molecule. The default total charge assigned is 0.</li>" +
        "<li>Click on the molecule Id for full info and to visualize the structure file.</li></ul>",
        "top");

    addStep("totalChargeWrapper",
        "Useful way to set non-zero total charge for a large set of molecules.",
        "top");

    addStep("setsWrapper",
        "<p>The Electronegativity Equalization Method (EEM) is the procedure by which atomic charges are calculated. EEM employs special parameters for each type of atom. Many EEM parameter sets have been published in literature, and are available here as built-in parameter sets. <b>ACC</b> tries to recommend a set suitable for the molecules you uploaded. Click <i>More</i> and <i>Show sets</i> for the full list. Orange highlight marks missing parameters for certain atom types.</p>" +
        "<p>You may also <i>add</i> your own set of EEM parameters, or a modified version of some built-in set. Click on the built-in set of interest, then <i>View XML</i> on the panel to the right. Copy/paste the content into the <i>Add</i> window, make your modifications, and save this EEM parameter set under a unique name.</p>" +
        "<p>Select one or more EEM parameter sets for your computation.</p>",
        "top");

    addStep("methodsWrapper",
        "<p>The default computation includes all atoms in the system and solves the entire EEM matrix in double precision. For very large systems you may need to resort to one of the time and memory efficient EEM implementations specifically tailored for such systems.</p>" +
        "<p>A biomolecular complex with 100 000 atoms makes good use of the method <i>EEM Cutoff Cover</i> with a <i>Cutoff Radius</i> of 12 and single <i>Precision</i>.</p>" +
    "<p>If your system is solvated or includes a few key water molecules, you may want to run parallel calculations with and without the water molecules, to see how water can affect the charge distribution in the biomolecule.</p>",
        "top");

    addStep("summaryWrapper",
        "Check the computation setup once more before you start. One computation may actualy consist of several jobs. A single job is defined by the molecule, its total charge, the set of EEM parameters, and the method employed.",
        "top");

    addStep("computeWrapper",
        "Start the calculation once you have reviewed all input and settings (molecules, total charge, EEM parameter sets, computation method). The calculation will be initiated, and you will be able to follow the progress on the screen. Alternatively, use the same URL to check the state of the calculation and access the results.",
        "top");

    intro.setOptions({ steps: steps });
    intro.start();
    if (elementName && startStep > 0) {
        intro.goToStep(startStep);
    }
}