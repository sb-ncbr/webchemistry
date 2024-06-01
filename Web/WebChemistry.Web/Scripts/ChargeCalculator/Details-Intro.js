function startDetailsIntro(currentTab) {
    var intro = introJs();

    var steps = [];

    switch (currentTab) {
        case "#summary": summaryIntro(); break;
        case "#data": dataIntro(); break;
        case "#aggregates": analyzeIntro(); break;
        case "#correlations": compareIntro(); break;
        case "#model3d": view3dIntro(); break;
        default: globalIntro(); break;
    }

    function addStep(id, msg, pos) {
        if (id && id[0] === '$') {
            var item = $(id.substr(1));
            if (item[0]) {
                steps.push({
                    element: item[0],
                    intro: msg,
                    position: id ? pos || "bottom-middle-aligned" : undefined
                });
                return;
            }
        }

        if (!id || document.getElementById(id)) {
            steps.push({
                element: id ? "#" + id : undefined,
                intro: msg,
                position: id ? pos || "bottom-middle-aligned" : undefined
            });
        }
    }

    intro.setOptions({ steps: steps });
    intro.start();

    function globalIntro() {
        addStep("charges-header-highlight",
            "The <b>ACC Specifics page</b> contains the results of all atomic charge calculation jobs pertaining to <b>a single molecule</b>, identified according to the name of the original input file. Functional tabs allow to view and analyze various parts of the data directly in your browser.");

        addStep("summaryTabLink",
            "Overview of the calculation, including warnings.");

        addStep("dataTabLink",
            "View all charges.");

        addStep("aggregatesTabLink",
            "Statistics of charges");

        addStep("correlationsTabLink",
            "Compare sets of charges");

        addStep("view3dTabLink",
            "View charges on 3D structures.");

        addStep("downloadBtn",
            "Download files with charges and statistics for post-processing.");
    }

    function summaryIntro() {
        addStep("basic-info",
            "Basic information about the input molecule and the setup of the calculation.",
            "right");

        addStep("charges-view-options", //View and download buttons
            "The <b>molecule</b> comprises all structural elements present in the <b>input file</b>, regardless of annotations.",
            "bottom");

        addStep("charges-view-atoms", //Atoms and value
            "Molecular formula, describing the atomic composition.",
            "bottom");

        addStep("charges-view-tc", //Total charge and value
            "Total charge of the molecule, distributed across the consitutent atoms based on the Electronegativity Equalization Method (EEM).",
            "bottom");

        addStep("charges-view-timing", //Timing and value
            "Total duration of all jobs for this molecule. Individual job timing available for download with the rest of the results.",
            "right");

        addStep("charges-view-comp-charges", //Computed charges and list
            "Multiple <b>jobs</b> may have been run for this molecule. A job is defined based on the molecule and its total charge, along with the EEM parameter set and computation method used. Each job produces a distinct <b>set of charges</b> with a unique ID encoding the EEM parameter set, method and total molecular charge. If the original input file contained values of atomic charges, these are also stored in a charge set named according to the input file, and marked by <i>ref</i>. All charge sets are available for analysis in the remaining tabs.",
            "top");
        
        addStep("comp-warnings",
            "<p>Warnings and errors generated while processing the input files and running the calculation. Each job will have its own list of errors and warnings. Jobs with errors generally do not produce any charge set, while jobs with warnings do.</p>" +
            "<p>Warnings inform about some action taken under unexpected circumstances (e.g, defaulting to different parameters for certain atoms), or draw attention to aspects which may represent issues (e.g., insufficient number of H to satisfy all valences).</p>",
            "left");
    }

    function dataIntro() {
        addStep(null,
            "The <b>raw data</b> is available in tabular form, and refers to the actual values of atomic charges resulted from the EEM calculation, or read in from the input file. Residue charges may also be available. No additiona statistics included here.");

        addStep("charges-raw-grouping", //atom grouping and menu
            "Raw data available at more levels of resolution, termed <b>atom grouping</b>. Pick <i>Atoms</i> to list atomic charges, or <i>Residues</i> to list residue charges (sum of atomic charges for each residue). Residue charges available only if residue annotations are present in the input file.",
            "bottom");
        //floating

        addStep("$#details-data-view .slick-header-columns > div:first-child", //Label header + column if possible, or 2-3 cells in column 1
            "Unique entry based on <b>atom grouping</b>. Labels for <i>Atoms</i> contain the atom type, atom name, atom serial number and residue of origin. Labels for <i>Residues</i> contain the residue name, residue serial number, chain identifier and number of atoms in the residue. Entries can be sorted according to atom or residue serial number.",
            "right");

        addStep("$#details-data-view .slick-header-columns > div:nth-child(2)", //Sets header (all or just 2-3) + 2-3 cells for each 
            "Each job produced one set of charges, identified by the EEM parameter set, method and total molecular charge. Each column contains atom or residue charges resulted from a single job, or read from the input file (identified by the file name and marked by <i>ref</i>).");

        addStep("model-details-view-filter", //filter and counter 
            "Filter table entries according to label. Regular expressions work. Counter records filter results.",
            "bottom");
    }

    function analyzeIntro() {
        addStep(null,
            "<b>ACC</b> provides several statistical descriptors for the results at more levels of resolution, in graphical and tabular form.");

        addStep("details-aggregate-plot", //graph
            "<p>Displays the <b>value</b> of a statistical descriptor for charges on <b>groups of atoms</b>, according to a certain <b>property</b>. For example, the <i>minimum charge</i> of <i>atoms</i> according to their <i>chemical element</i>. Content of the graph dictated by the menus on the left.</p>" +
            "<p>Each column represents the results from a single job (defined by EEM parameter set, computation method, and total molecular charge), or read in from the input file (identified by the file name and marked by <i>ref</i>).</p>",
            "left");

        addStep("charges-aggregate-content", //AG,Property and Value and menus
            "Dictate contents of the graph to the right, and of the <b>Raw Data</b> table below.",
            "right");

        addStep("charges-aggregate-ag", //atom grouping and menu
            "<b>Atom Grouping</b> gives the level of resolution. Pick <i>Atoms</i> to display atomic charges, or <i>Residues</i> to display residue charges (sum of atomic charges for each residue). Residue-based statistics available only if residue annotations present in input file.",
            "right");

        addStep("charges-aggregate-group", //group property and menu
            "<b>Group Property</b> sets the specific property based on which the statistics are calculated for a certain <Atom Grouping<b>. For example, statistics on all <i>atoms with a certain chemical element</i>, or statistics on all <i>polar residues</i>.",
            "right");

        addStep("charges-aggregate-pv", //plot value and menu 
            "<b>Plot Value</b> refers to the specific statistical descriptor displayed in the graph. For example, the <i>standard deviation</i> of the charge on atoms with a certain chemical element.",
            "right");

        addStep("charges-aggregate-raw", //Raw data and menu and table
            "Tabular representation of all statistical descriptors for each set of charges (<i>raw data</i>) resulted from a single job, identified by EEM parameter set, computation method, and total molecular charge. Contents depend on the <b>grouping</b> and <b>propery</b> menus above.",
            "right");
    }

    function compareIntro() {
        addStep(null,
            "<b>ACC</b> allows to compare sets of charges at more levels of resolution, in graphical and tabular form. These sets of charges may have resulted from different jobs, or were given directly by the input file.");

        addStep("details-correlation-plot", //graph
            "<p>Statistical comparison between two sets of charges (X and Y axes) on <b>groups of atoms</b>. For example, comparison between formal residue charges given in the input file, and residue charges as computed by EEM. Content of the graph dictated by the menus on the left.</p>",
            "left");

        addStep("charges-corr-controls", //AG, XY menus and outlier warning
            "Dictate contents of the graph to the right, and of the <b>Raw Data</b> table below.",
            "right");

        addStep("charges-corr-ag", //atom grouping and menu
            "<b>Atom Grouping</b> gives the level of resolution. Pick <i>Atoms</i> to compare atomic charges, or <i>Residues</i> to compare residue charges (sum of atomic charges for each residue). Residue-based statistics available only if residue annotations present in input file.",
            "right");

        addStep("charges-corr-xy", //X,Y and menus (not the ranges)
            "Pick the sets of charges to compare. Each set of charges was produced by a single job (identified by EEM parameter set, computation method and total molecular charge), or was present in the input file (identified by the input file name and marked by <i>ref</i>).",
            "right");

        addStep("charges-corr-range", //X,Y ranges (entire lines)
            "If you find the default XY ranges unsuitable for a certain dataset, you may adjust them manually.",
            "right");

        addStep("charges-corr-warn", //outliers warning
            "<p>The number of data points depends on <b>Atom Grouping</b>. Comparison statistics computed using the entire datasets, but only 2500 data points display by default. Generally doesn't cause problems even when displaying 10000 data points. If in trouble, just reload the page.</p>" +
            "<p>Since outliers are displayed first, the graph might suggest the two charge sets are more dissimilar than the comparison statistics say. </p>",
            "right");

        addStep("charges-corr-raw", //Raw data and menu and table
            "Tabular representation of the statistical comparison between a specific set of charges (<i>raw data</i>) and all other charge sets available for this molecule. Contents depend on the <b>grouping</b> menu above.",
            "right");
    }

    function view3dIntro() {
        addStep(null,
            "<b>ACC</b> allows a few basic charge visualization options to aid in interpreting the results of the atomic charge calculation. You may need to update your browser and driver version.");

        addStep("charges-3d-all-controls", //everything on the left side, but not the Update button
            "Dictate contents of the visualization window on the right. 3D visualization is generally memory consuming, so you should consider the settings before attempting to visualize the 3D model of your molecule.",
            "right");

        addStep("details-3d-big-warning", //size warning
            "<p>For large biomolecular complexes, start with <i>lower levels of display detail</i>, and work your way up if you feel unsatisfied with the quality of the display. If in trouble, just reload the page.</p>" +
            "<p>To reduce the complexity you may also choose to display the model based on <i>residue positions</i> (rather than atom positions). Only relevant if input file contains residue annotations.</p>",
            "right");

        addStep(null/*"charges-3d-update"*/, //Update button
            "Make sure your visualization settings are suitable, and click <b>Update</b> to display the 3D model of your molecule colored according to charges. Use <b>Update</b> whenever you modify the visualization options.",
            "top");

        addStep("details-3d-model", //visualizer window
            "<b>Visualization window</b>, displaying a 3D model of your molecule, colored according to charges. Content depends on the visualization settings in the panel on the left. Mouse controls in the right bottom corner.",
            "left");

        addStep("charges-3d-ag", //atom grouping and menu
            "<b>Atom Grouping</b> gives the level of resolution. Pick <i>Atoms</i> to build the 3D model based on <i>atomic positions</i> and color it by <i>atomic charges</i>. Pick <i>Residues</i> to build the 3D model based on <i>residue positions</i> and color it by <i>residue charges</i> (sum of atomic charges for each residue). Residue positions and charges available only if residue annotations present in input file. Requires <b>Update</b> to take effect.",
            "right");

        addStep("charges-3d-display", //display charges and menu
            "Pick the sets of charges used in coloring the 3D model of your molecule. Each set of charges was produced by a single job (identified by EEM parameter set, computation method and total molecular charge), or was read from the input file (identified by the input file name and marked by <i>ref</i>).",
            "right");

        addStep("charges-3d-diff-wrap", //show differences and menu
            "Instead of coloring by charges, it is sometimes useful to color by differences between two sets of charges. For example, to see how the presence of water changes the charge profile of the molecular surface.",
            "right");

        addStep("charges-3d-mode", //display mode and dependent menus
            "<p>Several basic modes available for displaying the 3D model, depending on the <b>Atom Grouping</b>. Requires <b>Update</b> to take effect.</p>" +
            "<p>Each display mode has additional options you may wish to tweak. For example, display the protein using <i>Cartoons</i>, and the ligands using <i>Balls and Sticks</i>, but hide water molecules.</p>",
            "right");

        addStep("charges-3d-detail", //display detail and menu
            "For large biomolecular complexes, start with <i>lower</i> detail. Requires <b>Update</b> to take effect.",
            "right");

        addStep("charges-3d-colors", //Color scale menus
            "If you find the default coloring scheme unsuitable, you may adjust it manually.",
            "right");

        addStep("charges-3d-gap", //Value gap and menu
            "Further adjustment to the coloring scheme allows to emphasize the visual difference in charge distribution in different areas of the molecule. Emphasis increases with <b>Value Gap</b>.",
            "right");

        addStep("charges-3d-default", //Set default parameters
            "Reset to default visualization options. Might require <b>Update</b> to take effect.",
            "right");
    }
}
