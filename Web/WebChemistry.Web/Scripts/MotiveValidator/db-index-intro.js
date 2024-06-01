function startDbIndexIntro() {
    var intro = introJs();
    intro.setOptions({
        steps: [
            {
                element: "#validation-totals",
                intro: "At each update, all molecules relevant for validation are extracted from each entry in the Protein Data Bank, and validated against models from the wwPDB Chemical Component Dictionary.",
                position: "bottom-middle-aligned"
            },
            {
                element: "#all-tab-headers",
                intro: "<p>The <i>synopsis page</i> consists of 6 tabs, two providing support, and four enabling access to the database itself.</p><p>The support tabs <i>Quick Help</i> and <i>Samples</i> help you get started, with basic information and examples of database snippets.</p><p>More guides accessible by the <button class='btn btn-success'><i class='icon icon-white icon-question-sign'></i></button> button at the top right corner of some tabs.</p>",
                position: "bottom-middle-aligned"
            },
            {
                element: "#custom-tab-header",
                intro: "Retrieve validation results for specific molecules. The search criteria include PDB entry, molecule identifiers, molecule annotations, or a combination of PDB entry and molecule annotations.",
                position: "bottom-middle-aligned"
            },
            {
                element: "#overview-tab-header",
                intro: "<p>PDB-wide validation statistics. Includes reports about incomplete structures, incorrect chirality and chemical modifications (see the <a href='//webchem.ncbr.muni.cz/Wiki/ValidatorDB:Database_contents' target='_blank'>wiki</a> for details).</p>",
                position: "bottom-middle-aligned"
            },
            {
                element: "#residues-tab-header",
                intro: "PDB-wide validation results for all molecules sharing the same annotation (3-letter code).",
                position: "bottom-middle-aligned"
            },
            {
                element: "#entry-tab-header",
                intro: "Validation results for all relevant molecules in each PDB entry.",
                position: "bottom-middle-aligned"
            }
        ]
    });
    intro.start();
}

function startDbByModelIntro() {
    var intro = introJs();
    intro.setOptions({
        steps: [

            {
                element: "#model-details-view",
                intro: "<p>All validation reports follow the color coding convention: <span class='mvcolor-Missing'>incomplete structures</span>, <span class='mvcolor-HasAll_BadChirality'>incorrect chirality</span>, <span class='mvcolor-HasAll_GoodChirality'>correct structures</span> and <span class='mvcolor-HasAll_Substitutions'>warnings</span>.</p>",
                position: "top"
            },
            {
                element: $("#model-details-view > .slick-header")[0],
                intro: "<p>Click on a category to sort table entries.</p>",
                position: "bottom-middle-aligned"
            },
            {
                element: $("#model-details-view").find(".slick-header-column:first")[0],
                intro: "<p>Click on a specific molecule annotation to access the <i>ValidatorDB specifics page</i>, with a detailed validation report for all molecules sharing that annotation.</p>",
                position: "right"
            },
            {
                element: "#model-details-view-columns",
                intro: "Add/remove table columns.",
                position: "top"
            },
            {
                element: "#model-display-type",
                intro: "Display percentages instead of the default absolute values.",
                position: "bottom-middle-aligned"
            },
            {
                element: "#model-filter",
                intro: "Fetch only molecules with a certain annotation. Regular expressions also work.",
            },
            {
                element: "#model-download-btn",
                intro: "Download table contents in raw form.",
                position: "bottom-middle-aligned"
            },
        ]
    });
    intro.start();
}

function startDbByStructureIntro() {
    var intro = introJs();
    intro.setOptions({
        steps: [


            {
                element: "#structure-details-view",
                intro: "<p>All validation reports follow the color coding convention: <span class='mvcolor-Missing'>incomplete structures</span>, <span class='mvcolor-HasAll_BadChirality'>incorrect chirality</span>, <span class='mvcolor-HasAll_GoodChirality'>correct structures</span> and <span class='mvcolor-HasAll_Substitutions'>warnings</span>.</p>",
                position: "top"
            },
            {
                element: $("#structure-details-view > .slick-header")[0],
                intro: "<p>Click on a category to sort table entries.</p>",
                position: "bottom-middle-aligned"
            },
            {
                element: $("#structure-details-view").find(".slick-header-column:first")[0],
                intro: "<p>Click on a PDB ID to access the <i>ValidatorDB specifics page</i>, with a detailed validation report for all molecules validated in that PDB entry.</p>",
                position: "right"
            },
            {
                element: "#structure-details-view-columns",
                intro: "Add/remove table columns.",
                position: "top"
            },
            {
                element: "#structure-display-type",
                intro: "Display percentages instead of the default absolute values.",
                position: "bottom-middle-aligned"
            },
            {
                element: "#structure-filter",
                intro: "Fetch only certain molecules or PDB entries, according to annotation or PDB ID. Regular expressions also work.",
                position: "bottom-middle-aligned"
            },
            {
                element: "#structure-download-btn",
                intro: "Download table contents in raw form.",
                position: "bottom-middle-aligned"
            },
        ]
    });
    intro.start();
}

function startDbCustomSearchIntro() {
    var intro = introJs();
    intro.setOptions({
        steps: [
            {
                element: "#custom-pdb-entries-header",
                intro: "Click <i>Specify</i> to restrict the search to a limited list of PDB entries. For each entry a specific list of molecule identifier can be given that further narrows the search.",
                position: "bottom-middle-aligned"
            },
            {
                element: "#custom-residue-entries-header",
                intro: "Click <i>Specify</i> to restrict the search to a limited list of molecules, according to their annotation (3-letter code).",
                position: "bottom-middle-aligned"
            },
            {
                element: "#custom-submit-btn",
                intro: "<p>Initiate the database search after specifying the molecules and PDB entries of interest. You will be redirected to a page with your custom view of the database.</p>",
                position: "bottom-middle-aligned"
            },
            {
                element: "#custom-submit-newtab-btn",
                intro: "<p>The result will open in a new tab/window.</p>",
                position: "bottom-middle-aligned"
            }
        ]
    });
    intro.start();
}