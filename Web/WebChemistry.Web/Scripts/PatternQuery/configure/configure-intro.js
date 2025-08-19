function startConfigIntro(currentTab) {
    var intro = introJs();

    var steps = [];

    switch (currentTab) {
        case "#submitComputation": submitComputation(); break;
        case "#filteredPDBEntries": filterPDBEntries(); break;
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

    //globalni help pro cely PQ service
    function globalIntro() {
        addStep("mq-main-tab-headers",
            "<ul>" +
            "<li>The <b>PatternQuery (PQ)</b> web page consists of several tabs, either providing support, or enabling access to the PQ service itself.</li>" +
            "<li>The support tabs <b>Quick Help</b> and <b>Samples</b> help you get started, with basic information and examples of interesting queries.</li>" +
            "<li>The tab <b>Support</b> enables you to directly ask us for help with more complex queries or troubleshooting.</li>" +
            "</ul>");

        addStep("samples-header",
            "We provide 3 interesting use cases for PQ. You will learn how to build basic queries and what kind of information can be easily extracted from the results.");

        addStep("explorer-header",
            "Search for and examine structural patterns in small data sets, or just optimize a query you would like to run on the entire PDB.");

        addStep("submit-header",
            "Search for structural patterns inside the entire PDB or its subset.");

        addStep("cmd-header",
            "<p>The PQ web service provides searches over PDB, but the command line version can be used to query any other large structural database.</p>"
            + "<p>A PyMOL plug-in is also available for generating visualization of the detected patterns within the input structure.</p>");
    }

    //help for the PQ Service for PBD queries
    function submitComputation() {
        addStep(null,
            "<p>PQ enables a very efficient way to query very large structural databases such as the Protein Data Bank.</p>" +
            "<p>Before you submit a query for the entire Protein Data Bank, it might be useful to check out the <b>Query Playground</b> and optimize your query. It saves time and you can easily get a good idea if your query will retrieve relevant patterns.</p>");

        addStep("mq-input-query-section",
            "<p>In a single database run you may include up to 10 different queries.</p>" +
            "<p>Start by specifying a unique query name, and then the query itself using the <a target='_blank' href='//webchemwiki.biodata.ceitec.cz/PatternQuery:Language_Reference'>PQ chemical language</a>. Then press <b>Add</b> for including the query.</p>");

        addStep("mq-data-source",
            "<p>Decide where you would like to run the PQ queries defined above:</p>" +
            "<ul>" +
            "<li>On the <i>entire Protein Data Bank<i>.</li>" +
            "<li>On a <i>list of PDB entries selected based on metadata information</i>. Check out the interactive guide in the <b>Filtered by Metadata</b> section for additional support.</li>" +
            "<li>On a <i>list of PDB entries selected based on PDB IDs</i>.</li>" +
            "</ul>");

        addStep("mq-validation-toggle",
            "By default, PQ retrieves validation results for ligands and non-standard residues from <b>Validator<sup>DB</sup></b>.");

        addStep("mq-notify-user",
            "If you wish, enter your e-mail to be notified when the computation finishes.<br/>The running time is about 1 hour per 100000 PDB entries.");

        addStep("submit-btn",
            "<p>When you launch your calculation, a unique URL will be assigned to it. Follow the progress on screen, or return to the URL later to retrieve and analyze the results.</p>" +
            "<p>The results are stored for at least one month and not shared with any 3<sup>rd</sup> party.</p>",
            "top");

        addStep("mq-recently-submitted-queries",
            "Links to the last 10 jobs you submitted, stored locally in your browser.",
            "top");

        addStep("mq-examples-button",
            "<p>If you don't know how to start, load one of the ready-to-use examples.</p>" +
            "<p>Don't forget to check out more <a target='_blank' href='//webchemwiki.biodata.ceitec.cz/PatternQuery:Use_Cases'>use cases</a> and consult the <a target='_blank' href='//webchemwiki.biodata.ceitec.cz/PatternQuery:Language_Reference'>language reference</a>.</p>" +
            "<p>If all else fails, feel free to ask us directly for help via the <a href='#' onclick='javascript: event.preventDefault(); $(\"#supportTabLink\").click()'>Support tab</a>.<p>", //please make it link to the Support tab if possible, and if not just remove the link completely and make Support tab bold.
            "left");
    }

    //help for the section Filtered by Metadata
    function filterPDBEntries() {
        addStep("mq-pdb-filter-table",
            "<p>The <b>Filtered by Metadata</b> section allows you to restrict your PDB search to only those PDB entries which may be relevant for your research. This not only allows you to tremendously speed up the search, but also ensures you invest time and energy analyzing only the data you actually want to work with.</p>" +
            "<p>This guide explains how to select PDB entries based on metadata information. To restrict the search to PDB entries with specific PDB IDs, use the <b>PDB ID List</b> option instead.</p>");

        addStep("mq-filter-col-active",
            "<p>The PQ metadata filter works similarly to the search on the PDB.org website - it applies criteria to metadata categories.</p>" +
            "<p>First, select the category of metadata you are interested in.</p>");

        addStep("mq-filter-comparison-col-active",
            "Then decide how you will evaluate the filter criterion.");

        addStep("mq-filter-text-col-active",
            "Enter the value used in the evaluation of the filter criterion. Please follow the displayed tips, as each metadata category might require a specific syntax.");

        addStep("mq-filter-add-col-active",
            "Click <b>Add</b> or just press Enter to apply the filter. If you add multiple metadata filters, they will all have to hold simultaneously for each PDB entry (using <i>logical and</i>).");
    }
}