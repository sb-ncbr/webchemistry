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
            "<p>We have loaded the structure of protegrin-1 (PDB-ID 1PG1).</p>" +
            "<li>ACC warns that only the first model was loaded. Indeed, the input file contained several NMR models. For this analysis, the first model is sufficient.</li>" +
            "<li>The total formal charge for protegrin-1 is +7, due to its high ARG content.</li></ul>",
            "bottom");

        addStep("setsWrapper",
            "<p>The Electronegativity Equalization Method (EEM) is the procedure by which atomic charges are calculated. EEM employs special parameters for each type of atom. Many EEM parameter sets have been published in literature, and are available here as built-in parameter sets.</p>" +
        "<p>Since protegrin-1 is a peptide, we suggest an EEM parameter set specifically developped for proteins. However, the chosen set of EEM parameters does not contain parameters for D (deuterium). We added parameters for D by copying the values of the parameters for H.</p>",
            "top");

        addStep("methodsWrapper",
            "<p>We have selected a computation method which includes all atoms and solves the entire EEM matrix in double precision.</p>",
            "top");

        addStep("computeWrapper",
            "Start the calculation once you have reviewed all input and settings (total charge, EEM parameter sets, computation method). The calculation will be initiated, and you will be able to follow the progress on the screen, or check the state of the calculation and retrieve the results at its current URL.",
            "top");

        intro.setOptions({ steps: steps });
        intro.start();
    },
    totalCharges: {
        "1pg1": [7]
    },
    customSets: [
        {
            "Name": "EX-NPA_6-31Gd_gas_withD",
            "AvailableAtoms": [
              "C",
              "Ca",
              "D",
              "H",
              "N",
              "O",
              "S"
            ],
            "Properties": [
              [
                "Author",
                "Ionescu, C. M., Geidl, S., Svobodova Varekova, R., Koca, J."
              ],
              [
                "Publication",
                "Rapid Calculation of Accurate Atomic Charges for Proteins via the Electronegativity Equalization Method"
              ],
              [
                "Journal",
                "J. Chem Inf. Model., 53"
              ],
              [
                "Year",
                "2013"
              ],
              [
                "Target",
                "Biomacromolecules"
              ],
              [
                "Basis Set",
                "6-31G*"
              ],
              [
                "Population Analysis",
                "NPA"
              ],
              [
                "QM Method",
                "HF"
              ],
              [
                "Training Set Size",
                "41"
              ],
              [
                "Data Source",
                "protein fragments"
              ],
              [
                "Priority",
                "1"
              ]
            ],
            "Xml": "<ParameterSet Name=\"EX-NPA_6-31Gd_gas_withD\">\r\n  <Properties>\r\n    <Property Name=\"Author\">Ionescu, C. M., Geidl, S., Svobodova Varekova, R., Koca, J.</Property>\r\n    <Property Name=\"Publication\">Rapid Calculation of Accurate Atomic Charges for Proteins via the Electronegativity Equalization Method</Property>\r\n    <Property Name=\"Journal\">J. Chem Inf. Model., 53</Property>\r\n    <Property Name=\"Year\">2013</Property>\r\n    <Property Name=\"Target\">Biomacromolecules</Property>\r\n    <Property Name=\"Basis Set\">6-31G*</Property>\r\n    <Property Name=\"Population Analysis\">NPA</Property>\r\n    <Property Name=\"QM Method\">HF</Property>\r\n    <Property Name=\"Training Set Size\">41</Property>\r\n    <Property Name=\"Data Source\">protein fragments</Property>\r\n    <Property Name=\"Priority\">1</Property>\r\n  </Properties>\r\n  <UnitConversion KappaFactor=\"1.000000000000\" ABFactor=\"1.000000000000\" />\r\n  <Parameters Target=\"Atoms\" Priority=\"0\" Kappa=\"0.006000000000\">\r\n    <Element Name=\"C\">\r\n      <Bond Type=\"1\" A=\"2.461479000000\" B=\"0.007989000000\" />\r\n      <Bond Type=\"2\" A=\"2.461306000000\" B=\"0.007458000000\" />\r\n    </Element>\r\n    <Element Name=\"Ca\">\r\n      <Bond Type=\"1\" A=\"2.476347000000\" B=\"-0.005690000000\" />\r\n      <Bond Type=\"2\" A=\"2.476347000000\" B=\"-0.005690000000\" />\r\n    </Element>\r\n    <Element Name=\"D\">\r\n      <Bond Type=\"1\" A=\"2.458610000000\" B=\"0.014507000000\" />\r\n    </Element>\r\n    <Element Name=\"H\">\r\n      <Bond Type=\"1\" A=\"2.458610000000\" B=\"0.014507000000\" />\r\n    </Element>\r\n    <Element Name=\"N\">\r\n      <Bond Type=\"1\" A=\"2.465674000000\" B=\"0.012741000000\" />\r\n      <Bond Type=\"2\" A=\"2.471200000000\" B=\"0.018870000000\" />\r\n    </Element>\r\n    <Element Name=\"O\">\r\n      <Bond Type=\"1\" A=\"2.474167000000\" B=\"0.019116000000\" />\r\n      <Bond Type=\"2\" A=\"2.468066000000\" B=\"0.012398000000\" />\r\n    </Element>\r\n    <Element Name=\"S\">\r\n      <Bond Type=\"1\" A=\"2.474621000000\" B=\"-0.033915000000\" />\r\n    </Element>\r\n  </Parameters>\r\n</ParameterSet>"
        }
    ],
    parameters: [
        "EX-NPA_6-31Gd_gas_withD"
    ],
    methods: [
        { method: "Eem" }
    ]
};