function startResultIntro(currentTab, id) {
    var intro = introJs();

    var steps = [];

    switch (currentTab) {
        case "#details-tab": details(); break;
        case "#pdbTab": byPDB(); break;
        case "#motifTab": byMotif(); break;
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

        if (!document.getElementById("mq-result-header-wrap")) {
            addStep(null,
                "<p>You query appears to be still running. Please wait once it's finished for the results to be displayed.</p>" +
                "<p>This guide will contain more steps once the query is finished.</p>");

            return;
        }

        addStep(null,
            "This is the <b>PatternQuery Results page</b>.");

        addStep("mq-result-header-wrap",
            "<p>You may return to this page later via the specified URL.</p>" +
            "<p>Download the results of all queries: structural patterns (in .pdb format), information regarding the atom and residue composition of the patterns, metadata summary, and overview of the queried PDB entries.</p>" +
            "<p>Alternatively, the results can be inspected directly in your web browser.<p>");

        addStep("mq-summary-header",
            "<p>The <b>Summary tab</b> has two sections:</p>" +
            "<ul>" +
            "<li><i>Queries</i> - each query is listed with its unique name, formula, number of patterns identified, PDB entries queried, and problems encountered.</li>" +
            "<li><i>Input</i> - list of criteria used in the selection of PDB entries to be queried.</li>" +
            "</ul>");

        addStep("mq-details-header",
            "The <b>Details tab</b> allows to inspect the results of each query thoroughly, and consists of two sub-tabs. Please see the help guides in each sub-tab for more information.");

        addStep("mq-input-header",
            "<p>The tab <b>Queried PDB Entries</b> provides information about all PDB entries that were queried, and has two sections:</p>" +
            "<ul>" +
            "<li><i>PDB entries</i> - PDB entries that were queried are listed with their PDB ID, number of patterns identified in any of the queries (if more were run) and statistics from the query execution.</li>" +
            "<li><i>PDB entry</i> - detailed information about the query execution for a given PDB entry that was queried, including explicit warnings and errors.</li>" +
            "</ul>");
    }

    function byPDB() {

        addStep(null,
            "The <b>By PDB Entry tab</b> allows to analyze the PDB entries that contain the identified patterns.");

        addStep(id + "-structure-entries-wrap",
            "<p>The <i>PDB Entries</i> section consists of a table with PDB entries where at least one structural pattern was found using the given query.</p>" +
            "<p>Each PDB entry is listed with its PDB ID, number of patterns identified, number of atoms and residues, potential issues encountered during the query execution.</p>",
            "top");

        addStep("$#" + id + "-structures-tab .slick-header-column:first",
            "Click on any PDB ID to see full details below.",
            "right");

        addStep("$#" + id + "-structures-tab .slick-header-column:nth-child(2)",
            "Clicking on the number of patterns will switch to the <b>By Pattern tab</b> to inspect each pattern identified in this PDB entry.",
            "right");

        addStep(id + "-structure-filters-wrap",
            "Dynamic filter for the table with PDB entries. The entries can be filtered by their warning/error status, and/or their PDB ID.");

        addStep(id + "-structure-details-wrap",
            "<p>The <i>PDB Entry</i> section provides a variety of information about the selected PDB entry:</p>" +
            "<ul><li>Links to PDBe.org and the validation report for ligands and non-standard residues, statistics about the query execution.</li><li>Detailed metadata information.</li></ul>",
            "top");

        addStep(id + "-metadata-selection-wrap",
            "<p>The <i>Metadata Filter</i> allows you to customize the content of the table with PDB entries. For each metadata category of interest, select one or more of the values available and <b>Apply Filter</b> to update the table.</p>" +
            "<p>The selected values for each category are related by the <b>OR</b> operator, and selected categories by the <b>AND</b> operator.</p>",
            "top");

    }

    function byMotif() {
        addStep(null,
            "The <b>By Pattern tab</b> allows to analyze the identified patterns.");

        addStep(id + "-motif-entries-wrap",
            "<p>The <i>Patterns</i> section consists of a table with patterns found using the given query.</p>" +
            "<p>Each pattern is listed with its unique Id, PDB ID of the parent PDB entry, number of atoms and residues, residue composition (<i>signature</i>).</p>",
            "top");

        addStep("$#" + id + "-motifs-tab .slick-header-column:first",
            "Click on any pattern Id to see full details below.",
            "right");

        addStep("$#" + id + "-motifs-tab .slick-header-column:nth-child(2)",
            "Clicking on the PDB ID will switch to the <b>By PDB Entry tab</b> where you may further inspect that entry.",
            "right");

        addStep(id + "-motif-filters-wrap",
            "Dynamic filter for the table with patterns. The patterns can be filtered by their validation status, and/or their PDB ID.");

        addStep(id + "-motif-details-wrap",
            "The <i>Pattern</i> section provides a variety of information for a given pattern.",
            "top");

        addStep("$#" + id + "-motifs-tab .mq-motif-3d",
            "3D model of the pattern. Visualization options are in the top right corner.",
            "right");

        addStep("$#" + id + "-motifs-tab .mq-motif-details-info",
            "<p>Information about the pattern composition and available validation reports.</p>",
            "right");

        addStep("$#" + id + "-motifs-tab .mq-motif-details-metadata-info",
            "<p>Detailed metadata information from the parent PDB entry.</p>",
            "left");

        addStep(id + "-metadata-selection-wrap",
            "<p>The <i>Metadata Filter</i> allows to customize the content of the table with PDB entries. For each metadata category of interest, select one or more of the values available and <b>Apply Filter</b> to update the table.</p>" +
            "<p>The selected values for each category are related by the <b>OR</b> operator, and selected categories by the <b>AND</b> operator.</p>",
            "top");
    }
}