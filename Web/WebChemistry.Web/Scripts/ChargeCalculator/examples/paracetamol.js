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
            "We have loaded a structure which corresponds to the paracetamol molecule (wwPDB CCD ID: TYL, ideal model coordinates)." +
            "<ul><li>The lack of highlight suggests that no issues were encountered while loading the molecule.</li>" +
            "<li>The total formal charge for paracetamol is 0.</li></ul>",
            "top");

        addStep("setsWrapper",
            "<p>The Electronegativity Equalization Method (EEM) is the procedure by which atomic charges are calculated. EEM employs special parameters for each type of atom. Many EEM parameter sets have been published in literature, and are available here as built-in sets. For evaluating the chemical reactivity of paracetamol, we suggest a set specifically developped for drug like molecules.</p>" +
            "<p>The EEM parameter sets selected for this calculation covers all types of atoms in paracetamol, and computes natural (NPA) charges, which generally support qualitative chemical phenomena like induction and conjugation.</p>",
            "top");

        addStep("methodsWrapper",
            "<p>Paracetamol is a small molecule, thus we have selected a computation method which includes all atoms and solves the entire EEM matrix in double precision.</p>",
            "top");

        addStep("computeWrapper",
            "Start the calculation once you have reviewed all input and settings (total charge, EEM parameter sets, computation method). The calculation will be initiated, and you will be able to follow the progress on the screen, or check the state of the calculation and retrieve the results at its current URL.",
            "top");

        intro.setOptions({ steps: steps });
        intro.start();
    },
    totalCharges: {
        "tyl_ideal": [0]
    },
    parameters: [
        "Ouy2009_elem"
    ],
    methods: [
        { method: "Eem" }
    ]
};