var MotiveValidatorGlobals = {
    "Analyzed": { color: "#5677fc", name: "Analyzed", shortName: "&#10003; Analyzed", tooltip: "Number of molecules which were successfully analyzed." },
    "NotAnalyzed": { color: "#e00032", name: "Not Analyzed", shortName: "&#10007; Analyzed", tooltip: "Number of molecules which were not analyzed." },

    "Missing": { color: "#e51c23", name: "Incomplete Structure", shortName: "&#10007; Structure", tooltip: "Number of molecules that have incomplete structure." },
    "Missing_Atoms": { color: "#dd191d", name: "Missing Only Atoms", shortName: "&#10007; Atoms", tooltip: "Number of molecules that miss one or more atoms, but no rings." },
    "Missing_Rings": { color: "#d01716", name: "Missing Rings", shortName: "&#10007; Rings", tooltip: "Number of molecules that miss one or more rings." },
    //"Missing_Disconnected": { color: "#c41411", name: "Bond Length Issues", shortName: "&#10007; Bonds", tooltip: "Number of molecules have issues with bond length." },
    "Missing_Degenerate": { color: "#c41411", name: "Degenerate", shortName: "&#10007; Degenerate", tooltip: "Number of molecules that have degenerate structure, and were not validated." }, // b0120a

    "HasAll": { color: "#8bc34a", name: "Complete Structure", shortName: "&#10003; Structure", tooltip: "Number of molecules that have complete structure." },

    "HasAll_BadChirality": { color: "#BDA429", name: "Wrong Chirality", shortName: "&#10007; Chirality", tooltip: "Number of molecules that have all atoms and rings, and wrong chirality on at least one atom." },
    "HasAll_BadChirality_Carbon": { color: "#A58E24", name: "Wrong Chirality (Carbon)", shortName: "&#10007; C Chirality", tooltip: "Number of molecules that have all atoms and rings, and wrong chirality on at least one carbon atom." },
    "HasAll_BadChirality_Metal": { color: "#B79D28", name: "Wrong Chirality (Metal)", shortName: "&#10007; Metal Chirality", tooltip: "Number of molecules that have all atoms and rings, and wrong chirality on at least one metal atom." },
    "HasAll_BadChirality_NonSingleBond": { color: "#BFA32A", name: "Wrong Chirality (High Order)", shortName: "&#10007; HO Chirality", tooltip: "Number of molecules that have all atoms and rings, and wrong chirality on at least one non-C atom with bonds of higher order." },
    "HasAll_BadChirality_Planar": { color: "#BFA32A", name: "Wrong Chirality (Planar)", shortName: "&#10007; Planar Chirality", tooltip: "Number of molecules that have all atoms and rings, and wrong chirality on at least one planar atom." },
    "HasAll_BadChirality_Other": { color: "#BDA129", name: "Wrong Chirality (Other)", shortName: "&#10007; Other Chirality", tooltip: "Number of molecules that have all atoms and rings, and wrong chirality on atoms that do not fall to any of the other special categories." },

    "HasAll_WrongBonds": { color: "#a1887f", name: "Uncertain Chirality", shortName: "? Chirality", tooltip: "Number of molecules that that have all atoms and rings, but contain unusual bonds, preventing to correctly determine their chirality." },

    "HasAll_GoodChirality": { color: "#238C21", name: "Correct Chirality", shortName: "&#10003; Chirality", tooltip: "Number of molecules that have all atoms and rings, and correct chirality on all atoms." },
    "HasAll_GoodChirality_IgnorePlanarAndNonSingleBondErrors": { color: "#259b24", name: "Correct Chirality (Tolerant)", shortName: "&#10003;? Chirality", tooltip: "Number of molecules that have all atoms and rings, and correct chirality on all atoms, or wrong chirality on atoms marked as Planar or High Order." },

    "HasAll_Substitutions": { color: "#607d8b", name: "Atom Substitution", shortName: "Substitutions", tooltip: "Number of molecules that have all atoms and rings, and at least one substituted atom (for example N for O)." },
    "HasAll_Foreign": { color: "#607d8b", name: "Foreign Atom", shortName: "Foreign Atoms", tooltip: "Number of molecules that have all atoms and rings, and atoms that do not belong to the residue being validated." },

    "HasAll_NameMismatch": { color: "#78909c", name: "Different Naming", shortName: "Different Naming", tooltip: "Number of molecules that have all atoms and rings, and contain different atom naming compared to the model." },
    "HasAll_NameMismatch_ChargeEquiv": { color: "#62757F", name: "Diff. Naming (Chem. Equiv)", shortName: "Different Naming (E)", tooltip: "Number of molecules that have all atoms and rings, and contain different atom naming compared to the model on atoms that are chemically equivalent." },
    "HasAll_NameMismatch_ChargeEquivIgnoreBondType": { color: "#62757F", name: "Diff. Naming (Chem. Equiv, Ignore Bonds)", shortName: "Different Naming (EB)", tooltip: "Number of molecules that have all atoms and rings, and contain different atom naming compared to the model on atoms that are chemically equivalent (ignoring bond types when computing the equivalence)." },
    "HasAll_NameMismatch_NonChargeEquiv": { color: "#62757F", name: "Diff. Naming (Non-chem. Equiv)", shortName: "Different Naming (NE)", tooltip: "Number of molecules that have all atoms and rings, and contain different atom naming compared to the model on atoms that are NOT chemically equivalent." },
    "HasAll_NameMismatch_NonChargeEquivIgnoreBondType": { color: "#62757F", name: "Diff. Naming (Non-chem. Equiv, Ignore Bonds)", shortName: "Different Naming (NEB)", tooltip: "Number of molecules that have all atoms and rings, and contain different atom naming compared to the model on atoms that are NOT chemically equivalent (ignoring bond types when computing the equivalence)." },

    //"Has_DuplicateNames": { color: "#8E66FF", name: "Duplicate Names", shortName: "Duplicate Names", tooltip: "Number of molecules that have duplicate atom names." },
    "Has_NamingIssue_NonIsomorphic": { color: "#8E66FF", name: "Non-isomorphic Naming", shortName: "Non-isomorphic Naming", tooltip: "Number of molecules whose atom names do not define graph isomorphism with the model." },
    "Has_NamingIssue_NonBoundarySubstitutionOrForeign": { color: "#8E66FF", name: "Non-boundary Substitution or Foreign Atoms", shortName: "Non-boundary Foreign", tooltip: "Number of molecules that have non-boundary substitution or foreign atoms." },

    "HasAll_ZeroRmsd": { color: "#90a4ae", name: "Zero Model RMSD", shortName: "Zero Model RMSD", tooltip: "Number of molecules that have RMSD = 0 (are identical) with the model in wwPDB CCD." },
    "Has_AlternateLocation": { color: "#90a4ae", name: "Alternate Conformation", shortName: "Alternate Conformation", tooltip: "Number of molecules that have at least one atom with alternate coordinates." },
};

var MotiveValidatorGlobalsColumnOrdering = [
    "Analyzed",
    "NotAnalyzed",

    "Missing",
    "Missing_Atoms",
    "Missing_Rings",
    //"Missing_Disconnected",
    "Missing_Degenerate",

    "HasAll",

    "HasAll_BadChirality",
    "HasAll_BadChirality_Carbon",
    "HasAll_BadChirality_Metal",
    "HasAll_BadChirality_NonSingleBond",
    "HasAll_BadChirality_Planar",
    "HasAll_BadChirality_Other",

    "HasAll_WrongBonds",

    "HasAll_GoodChirality",
    "HasAll_GoodChirality_IgnorePlanarAndNonSingleBondErrors",

    "HasAll_Substitutions",
    "HasAll_Foreign",

    "HasAll_NameMismatch",
    "HasAll_NameMismatch_ChargeEquiv",
    "HasAll_NameMismatch_ChargeEquivIgnoreBondType",
    "HasAll_NameMismatch_NonChargeEquiv",
    "HasAll_NameMismatch_NonChargeEquivIgnoreBondType",

    //"Has_DuplicateNames",
    "Has_NamingIssue_NonIsomorphic",
    "Has_NamingIssue_NonBoundarySubstitutionOrForeign",

    "HasAll_ZeroRmsd", 
    "Has_AlternateLocation"
];

$(function () {
    var style = document.createElement('style');
    style.type = 'text/css';
    var styles = "";
    _.forEach(MotiveValidatorGlobals, function (props, name) {
        styles += ".mvcolor-" + name + " { color: " + props.color + " !important; } ";
        styles += ".mvbackground-" + name + " { background: " + props.color + " !important; } ";
    });
    style.innerHTML = styles;
    document.getElementsByTagName('head')[0].appendChild(style);
});