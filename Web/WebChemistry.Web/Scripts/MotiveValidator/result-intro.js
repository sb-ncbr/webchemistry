function startResultIntro(isMotiveValidator) {
    var intro = introJs();

    var steps = [];

    function addIf(id, msg, pos) {
        if (document.getElementById(id)) {
            steps.push({
                element: "#" + id,
                intro: msg,
                position: pos || "bottom-middle-aligned"
            });
        }
    }

    if (!isMotiveValidator) {
        addIf("result-header",
              "You are currently viewing this snippet of the database.");
    }

    addIf("result-link",
          "Your results are stored on our server at this address until the specified date.");

    addIf("result-tab-headers",
          "The <i>specifics</i> or <i>results</i> page is organized in several functional tabs, allowing to view and interpret the validation results at various levels of detail.");

    addIf("result-overview-header",
          "Overall statistics of the validation results (see the <a href='//webchemwiki.biodata.ceitec.cz/ValidatorDB:Database_contents' target='_blank'>wiki</a> for details.");

    addIf("result-summary-header",
          "Validation report listing all issues encountered for each validated molecule.");

    addIf("result-details-header",
          "Detailed validation report for each validated molecule, advanced filtering criteria for browsing specific validation issues.");

    addIf("result-warnings-header",
          "Problems or outstanding circumstances encountered during validation.");

    addIf("result-download",
          "Download the input structures and all validation reports.");

    addIf("result-download",
          "Download all data in raw format.");

    intro.setOptions({
        steps: steps
    });
    intro.start();
}

function startResultSummaryIntro() {
    var intro = introJs();

    var steps = [];

    function addIf(id, msg, pos) {
        if (document.getElementById(id)) {
            steps.push({
                element: "#" + id,
                intro: msg,
                position: pos || "bottom-middle-aligned"
            });
        }
    }

    addIf("result-residue-list",
          "Validation results are available for the molecules listed here. Click on a molecule annotation to access the validation report for all validated molecules sharing that annotation.");

    addIf("result-residue-info",
          "Basic information about the validated molecule, links to the corresponding entries in the PDB and wwPDB CCD.");

    addIf("result-summary-plot",
          "Validation statistics for all molecules with this annotation. Useful when multiple such molecules were available for validation.",
           "right");

    addIf("result-summary-tables",
          "<p>Issues found during validation. Statistics relevant when multiple molecules were available for validation (see the <a href='//webchemwiki.biodata.ceitec.cz/ValidatorDB:Database_contents' target='_blank'>wiki</a> for details).</p>" +
          "<p>Color coding convention: <span class='mvcolor-Missing'>incomplete structures</span>, <span class='mvcolor-HasAll_BadChirality'>incorrect chirality</span>, <span class='mvcolor-HasAll_GoodChirality'>correct structures</span> and <span class='mvcolor-HasAll_Substitutions'>warnings</span>.</p>",
          "top");


    intro.setOptions({
        steps: steps
    });
    intro.start();
}

function startResultDetailsIntro() {
    var intro = introJs();

    var steps = [];

    function addIf(id, msg, pos) {
        if (document.getElementById(id)) {
            steps.push({
                element: "#" + id,
                intro: msg,
                position: pos || "bottom-middle-aligned"
            });
        }
    }

    addIf("detailsGrid",
          "Each entry refers to a single validated molecule, and is identified by a molecule ID which includes the PDB entry of origin. Click on a molecule ID to access a detailed validation report for this validated molecule. Table entries can be sorted and filtered.",
          "top");

    addIf("result-details-residue",
          "Fetch only the molecules with this annotation.");

    addIf("result-details-group",
          "Fetch only the molecules which exhibit this validation-related characteristic.");


    addIf("result-details-filter",
          "Fetch only the validated molecules with a certain parent PDB ID or molecule ID. Regular expressions work.");

    addIf("result-details-list",
          "Generate a list of molecules (identified by their molecule IDs) which satisfy the filtering criteria.");

    intro.setOptions({
        steps: steps
    });
    intro.start();
}

function startResultResidueDetailIntro() {
    var intro = introJs();

    var steps = [];

    function addIf(id, msg, pos) {
        if (document.getElementById(id)) {
            steps.push({
                element: "#" + id,
                intro: msg,
                position: pos || "bottom-middle-aligned"
            });
        }
    }

    if (BrowserDetection.browser === "Chrome") {
        steps.push({
            element: undefined,
            intro: "This guide might not display correctly in Chrome (overlay elements are incorrectly not visible). Sorry for the inconvenience."
        });
    }

    addIf("first-column-model-info",
          "Basic information about the model, the validated molecule, along with any exceptional circumstances encountered during its validation.",
           "top");

    addIf("result-residue-detail-display-mode",
          "Choose which structure to visualize and how.");

    addIf("result-residue-detail-errors",
          "Errors found in the validated molecule: <span class='mvcolor-Missing'>incomplete structures</span>, <span class='mvcolor-HasAll_BadChirality'>incorrect chirality</span> (see the <a href='//webchemwiki.biodata.ceitec.cz/ValidatorDB:Database_contents' target='_blank'>wiki</a> for details).",
           "top");

    addIf("result-residue-detail-warnings",
          "<span class='mvcolor-HasAll_Substitutions'>Warnings</span> (unusual circumstances) encountered during validation.",
           "top");



    intro.setOptions({
        steps: steps
    });
    intro.start();
}