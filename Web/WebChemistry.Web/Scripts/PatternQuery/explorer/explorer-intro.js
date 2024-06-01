function startExplorerIntro(currentTab) {
    var intro = introJs();

    var steps = [];

    switch (currentTab) {
        case "#filteredPDBEntries":
        case "#listPDBEntries":
            addPDBOrg();
            break;

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
        addStep(null,
            "<p><b>PQ Explorer</b> allows you to search for structural patterns in smaller datasets, and is ideal for fine tunning PQ queries that you plan to run on large databases.</p>");

        addStep("mq-explorer-add-data", //show the buttons and grid
            "<p>Start by uploading your own dataset of biomolecular structures, or retrieving entries from PDBe.org based on metadata information or PDB ID.</p>",
            "left");

        addStep("mq-explorer-structure-grid", //show the buttons and grid
            "<p>This <i>entry panel</i> lists the entries you add.</p>",
            "left");

        addStep("mq-explorer-input",
            "Build your query using the PQ chemical language, and run it on all the entries listed in the <i>entry table</i>.");

        addStep("mq-explorer-motif-grid", //show also the filter and download
            "<p>The <i>pattern panel</i> lists all the structural patterns identified by the query, along with basic information.</p>" +
            "<p>Click on any pattern Id to display its 3D model in the <i>visualization panel</i>. All patterns can be downloaded in .pdb format, along with all info in .csv format.</p>",
            "left");

        addStep("mq-explorer-3d-view", //visualization panel
            "The <i>visualization panel</i> displays a 3D model of any structural pattern selected via the <i>pattern panel</i>. Pattern info is given in the bottom right corner",
            "right");

        addStep("mq-explorer-log",
            "The <i>log panel</i> keeps a record of all uploaded or retrieved of data, and any warnings or errors generated while running the queries.",
            "top");

        addStep("mq-explorer-session-info",
            "Come back to your PQ Explorer session at this URL.");

        addStep("mq-explorer-examples-button",
            "You can start using the Explorer with one of the ready to use examples.",
            "left");
    }

    function addPDBOrg() {
        addStep("mq-filter-pdb-header",
            "Add PDB entries based on metadata information...",
            "right");

        addStep("mq-list-pdb-header",
            "<p>...or provide a list of PDB IDs separated by a new line or a comma.</p>" +
            "<p>You might use the search functionality at PDB.org to obtain a list of PDB IDs:</p>" +
            '<ol style="margin-bottom: 0"><li>Go to <a style="font-weight: bold" href="//pdb.org/pdb/search/advSearch.do" target="_blank">PDB.org Search</a> and enter your search criteria.</li><li>Run the search and list the results using the option `Reports: List selected IDs`.</li><li>Copy-paste up to 100 PDB IDs from the result into the text area below.</li></ol>',
            "right");

        if (currentTab !== "#filteredPDBEntries") return;

        addStep("mq-filter-col-active",
            "<p>The PQ metadata filter works similarly to the search on the PDB.org website - it applies criteria to metadata categories.</p>" +
            "<p>First, select the category of metadata you are interested in.</p>",
            "right");

        addStep("mq-filter-comparison-col-active",
            "Then decide how you will evaluate the filter criterion.");

        addStep("mq-filter-text-col-active",
            "Finally, give the value used in the evaluation of the filter criterion. Please follow the tips on screen, as each metadata category might require a specific syntax.");

        addStep("mq-filter-add-col-active",
            "Click <b>Add</b> or just press Enter to apply the filter. If you add multiple metadata filters, they will all have to hold simultaneously for each PDB entry (using <i>logical AND</i>).");

        addStep(null,
            "<p>After applying the filter, your selection of PDB entries will probably still be significant (thousands of entries). PQ Explorer is not designed for such datasets.</p>" +
            "<p>Show 15 random entries matching your criteria or all the entries sorted by the PDB ID (up to 15 entries can be viewed at the same time)." +
            "<p>Alternatively, click <b>Show full list</b> to display the PDB IDs of all entries matching your criteria. Pick some PDB IDs of interest (max 100) and switch to the <b>PDB ID List</b> option of retrieving PDB entries.");

        addStep("mq-explorer-add-wrapper",
            "<p>After obtaining the list with up to 15 PDB entries, select those fitting your needs, and click <b>Add Displayed / Selected</b> to include them in the PQ Explorer <i>entry panel</i>.</p>" +
            "<p>You can repeat this process and add up to 100 PDB entries.</p>",
            "top");
    }
}