var ChargesExample = {
    startGuide: function () {
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

        addStep("structuresWrapper",
            "<p>We have loaded the structure of 3 conformers of the 26S proteasome (PDB IDs: 4CR2, 4CR3, 4CR4).</p>" +
            "<ul><li>ACC reports a missing hydrogen warning for all structures. Indeed, no H were added, due to the size and bad resolution of these structures.</li>" +
            "<li>The structures are incomplete, so we do not expect realistic values for the atomic charges for each conformer. However, the charge differences between conformations might hold clues about how information travels in this large biomacromolecular complex.</li>" +
            "<li>The total formal charge for each of the 26S proteasome conformers was set to 0. Again, the value is not realistic, but in this case it is only important to have the same value for all conformers.</li></ul>",
            "bottom");

        addStep("setsWrapper",
            "<p>The Electronegativity Equalization Method (EEM) is the procedure by which atomic charges are calculated. EEM employs special parameters for each type of atom. Many EEM parameter sets have been published in literature, and are available here as built-in parameter sets.</p>" +
            "<p>Since the proteasome is a large complex of multiple proteins, we suggest an EEM parameter set specifically developped for proteins.</p>",
            "top");

        addStep("methodsWrapper",
            "<p>The 26S proteasome is a very large complex with over 10000 residues, requiring an EEM implementation which is time and memory efficient. We have selected a computation method which solves the EEM matrix only for a selected set of atoms in double precision, and assigned a reasonable cutoff radius.</p>" +
            "<p>Note that charges will be computed for all conformers at once, thus a total of 3 ACC jobs will be run.</p>",
            "top");

        addStep("computeWrapper",
            "Start the calculation once you have reviewed all input and settings (total charge, EEM parameter sets, computation method). The calculation will be initiated, and you will be able to follow the progress on the screen. Alternatively, you may use the same URL to check the state of the calculation and retrieve the results.",
            "top");

        intro.setOptions({ steps: steps });
        intro.start();
    },
    totalCharges: {
        "4cr2": [0],
        "4cr3": [0],
        "4cr4": [0]
    },
    parameters: [
        "EX-NPA_6-31Gd_gas"
    ],
    methods: [
        { method: "EemCutoffCover", cutoff: 12 }
    ]
};