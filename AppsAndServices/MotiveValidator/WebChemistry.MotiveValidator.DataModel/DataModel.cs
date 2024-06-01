namespace WebChemistry.MotiveValidator.DataModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using WebChemistry.Platform.Services;

    [Description("Contains information about a wrong bond.")]
    [HelpDescribe]
    public class WrongBondInfo
    {
        /// <summary>
        /// Model from id.
        /// </summary>
        [Description("Atom identifier of the first atom in the model structure.")]
        public int ModelFrom { get; set; }

        /// <summary>
        /// Model to id.
        /// </summary>
        [Description("Atom identifier of the second atom in the model structure.")]
        public int ModelTo { get; set; }

        /// <summary>
        /// Bond type on the motive.
        /// </summary>
        [Description("Type of the bond.")]
        public string Type { get; set; }

        /// <summary>
        /// atoms on the motive
        /// </summary>
        [Description("Atoms in the specific motif.")]
        public string MotiveAtoms { get; set; }
    }

    [Description("Describes the state of the entry.")]
    [HelpDescribe]
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ValidationResultEntryState
    {
        [Description("The entry was successfully validated.")]
        Validated = 0,
        [Description("The entry was disconnected and thus could not be validated.")]
        Disconnected,
        [Description("The entry was degenerate and thus could not be validated.")]
        Degenerate
    }

    [Description("An entry generated for each motif.")]
    [HelpDescribe]
    public class ValidationResultEntry
    {
        /// <summary>
        /// Name of the model ... 
        /// </summary>
        [Description("Name of the model.")]
        public string ModelName { get; set; }

        /// <summary>
        /// Id of the parent structure.
        /// </summary>
        [Description("Identifier of the parent structure.")]
        public string Id { get; set; }

        /// <summary>
        /// State of the entry.
        /// </summary>
        [Description("Determines whether the entry was validated or if there was some problem in doing so.")]
        public ValidationResultEntryState State { get; set; }

        /// <summary>
        /// Analysis flags.
        /// </summary>
        public string[] Flags { get; set; }

        /// <summary>
        /// Main residue name.
        /// </summary>
        [Description("The main verified residue.")]
        public string MainResidue { get; set; }

        /// <summary>
        /// Number of residues.
        /// </summary>
        [Description("Number of residues.")]
        public int ResidueCount { get; set; }
        
        /// <summary>
        /// Residues in the entry.
        /// </summary>
        [Description("All residues in the motif.")]
        public string[] Residues { get; set; }

        /// <summary>
        /// Number of missing atoms.
        /// </summary>
        [Description("Number of missing atoms.")]
        public int MissingAtomCount { get; set; }

        /// <summary>
        /// Number of missing rings.
        /// </summary>
        [Description("Number of missing rings.")]
        public int MissingRingCount { get; set; }

        /// <summary>
        /// Number of incorrect bonds
        /// </summary>
        [Description("Number of wrong bonds.")]
        public int WrongBondCount { get; set; }

        /////// <summary>
        /////// Number of planar atoms with wrong chirality estimation.
        /////// </summary>
        ////[Description("Number of planar atoms with wrong chirality estimation.")]
        ////public int PlanarAtomsWithWrongChiralityCount { get; set; }

        /// <summary>
        /// Is there a difference between model/matched motif bonds?
        /// </summary>
        [Description("Determines if there was a discrepancy between model and matched motif bonds.")]
        public bool HasBondDiscrepancy { get; set; }

        /// <summary>
        /// Count of name mismatches.
        /// </summary>
        [Description("Number of atoms with mismatched name.")]
        public int NameMismatchCount { get; set; }

        /// <summary>
        /// Number of atoms not on the "main" residue.
        /// </summary>
        [Description("Number of atoms that do not belong to the main residue.")]
        public int ForeignAtomCount { get; set; }

        /// <summary>
        /// Count of name mismatches.
        /// </summary>
        [Description("Number of atoms with wrong chirality.")]
        public int ChiralityMismatchCount { get; set; }

        /// <summary>
        /// Number of substituted atoms.
        /// </summary>
        [Description("Number of substitued atoms.")]
        public int SubstitutionCount { get; set; }

        /// <summary>
        /// Number of unmatched atoms from the input motifs.
        /// </summary>
        [Description("Number atoms that were not matched with the model.")]
        public int UnmatchedMotiveAtomCount { get; set; }
        
        /// <summary>
        /// A list of missing atom Model Ids.
        /// </summary>
        [Description("Identifier of model atoms that are missing in this motif.")]
        public int[] MissingAtoms { get; set; }

        /////// <summary>
        /////// Identifier of model atoms that are planar and have wrong chirality.
        /////// </summary>
        ////[Description("Identifier of model atoms that are planar and have wrong chirality.")]
        ////public int[] PlanarAtomsWithWrongChirality { get; set; }
        
        /// <summary>
        /// Missing rings.
        /// </summary>
        [Description("Missing rings.")]
        public string MissingRings { get; set; }

        /// <summary>
        /// Wrong bonds.
        /// </summary>
        [Description("Information about wrong bonds. Not usable in the current implementation.")]
        [HelpDescribe(typeof(WrongBondInfo))]
        public WrongBondInfo[] WrongBonds { get; set; }

        /// <summary>
        /// Atom name mismatches.
        /// </summary>
        [Description("A map of model atom ids to mismatched atom names on the motif.")]
        public Dictionary<int, string> NameMismatches { get; set; }

        /// <summary>
        /// Atom name mismatch flags.
        /// </summary>
        [Description("A map of model atom ids to mismatched atom flags.")]
        public Dictionary<int, string[]> NameMismatchFlags { get; set; }

        /// <summary>
        /// Foreign atoms.
        /// </summary>
        [Description("A map of model atom ids to atoms on the motif.")]
        [HelpTypeName("AtomId -> Atom")]
        public Dictionary<int, string> ForeignAtoms { get; set; }
                
        /// <summary>
        /// Atoms that have mismatched chirality.
        /// </summary>
        [Description("A map of model atom ids to atoms with different chirality on the motif.")]
        [HelpTypeName("AtomId -> Atom")]
        public Dictionary<int, string> ChiralityMismatches { get; set; }
        
        /// <summary>
        /// Substitued atoms.
        /// </summary>
        [Description("A map of model atom ids to substituted atoms on the motif.")]
        [HelpTypeName("AtomId -> Atom")]
        public Dictionary<int, string> Substitutions { get; set; }

        /// <summary>
        /// Number of alternate locations.
        /// </summary>
        [Description("Number of alternate locations.")]
        public int AlternateLocationCount { get; set; }

        /// <summary>
        /// Duplicate atom names on the 'main' residue.
        /// </summary>
        [Description("Duplicate atom names on the 'main' residue.")]
        public string[][] DuplicateNames { get; set; }

        /// <summary>
        /// Substitution and foreign atoms that are not on the 'boundary' of the fragment.
        /// </summary>
        [Description("Substitution and foreign atoms that are not on the 'boundary' of the fragment.")]
        public string[] NonBoundarySubstitutionAndForeignAtoms { get; set; }

        /// <summary>
        /// Names on model that were not matched (i.e. missing, substituted, or foreign).
        /// </summary>
        [Description("Names on model that were not matched (i.e. missing, substituted, or foreign).")]
        public string[] UnmatchedModelNames { get; set; }

        /// <summary>
        /// Rmsd to the model.
        /// </summary>
        [Description("A map of model atom ids to substituted atoms on the motif.")]
        public double ModelRmsd { get; set; }
    }
        
    [HelpDescribe]
    [Description("Analysis summary for a single motif.")]
    public class ModelValidationEntry
    {
        [Description("Name of the model (3-letter residue code).")]
        public string ModelName { get; set; }

        [Description("Chemical name. Only available for structures in 'ligandexpo.csv'.")]
        public string LongName { get; set; }

        [Description("Formula. Only available for structures in 'ligandexpo.csv'.")]
        public string Formula { get; set; }

        [Description("The source of coordinates. (Default/CifModel/CifIdeal.")]
        public string CoordinateSource { get; set; }
        
        [Description("A map of model atom ids to their respective names.")]
        [HelpTypeName("AtomId -> Name")]
        public Dictionary<int, string> ModelNames { get; set; }

        [Description("A map of model atom ids to their respective types (element symbols).")]
        [HelpTypeName("AtomId -> ElementSymbol")]
        public Dictionary<int, string> ModelAtomTypes { get; set; }

        [Description("A list of nearly planar atom identifiers.")]
        [HelpTypeName("AtomId[]")]
        public int[] PlanarAtoms { get; set; }

        [Description("A list of chiral atoms.")]
        [HelpTypeName("AtomId[]")]
        public int[] ChiralAtoms { get; set; }

        [Description("More detailed info about chiral atoms (carbons, metals, with double bonds, etc.).")]
        [HelpTypeName("InfoType->AtomId[]")]
        public Dictionary<string, int[]> ChiralAtomsInfo { get; set; }

        [Description("Equivalence classes of atom names.")]
        [HelpTypeName("InfoType->(Name->Name)")]
        public Dictionary<string, Dictionary<string, string>> AtomNamingEquivalence { get; set; }

        [Description("A map of model atom ids pairs to their bond type.")]
        [HelpTypeName("AtomId-AtomId -> Type")]
        public Dictionary<string, int> ModelBonds { get; set; }

        [Description("A map of analysis type to the number of affected motifs.")]
        [HelpTypeName("SummaryType -> NumberOfMotifs")]
        public Dictionary<string, int> Summary { get; set; }
        
        [Description("Names of motifs that were analyzed.")]
        public string[] StructureNames { get; set; }

        [Description("Names of structures that were not analyzed (due to errors).")]
        public string[] NotAnalyzedNames { get; set; }

        [Description("An entry for each motif in the analysis.")]
        public ValidationResultEntry[] Entries { get; set; }

        [Description("A map of motif id to a list of warnings.")]
        [HelpTypeName("MotifId -> ListOfWarnings")]
        public Dictionary<string, string[]> Warnings { get; set; }

        [Description("A map of motif id to error.")]
        [HelpTypeName("MotifId -> Error")]
        public Dictionary<string, string> Errors { get; set; }
    }

    [HelpDescribe]
    [Description("Result of the validation.")]
    public class ValidationResult
    {
        /// <summary>
        /// Version of the service.
        /// </summary>
        [Description("Version of the application used to compute this analysis.")]    
        public string Version { get; set; }

        /// <summary>
        /// Errors encountered during the computation.
        /// </summary>
        [Description("General errors in this analysis.")]
        [HelpTypeName("MotifId -> Error")]
        public Dictionary<string, string> Errors { get; set; }

        /// <summary>
        /// Were the motives identified automatically.
        /// </summary>
        [HelpDescribe]
        [Description("Type of the validation.")]
        public MotiveValidationType ValidationType { get; set; }
        
        /// <summary>
        /// Number of motifs analyzed.
        /// </summary>
        [Description("Total count of motifs that were analyzed.")]
        public int MotiveCount { get; set; }

        /// <summary>
        /// A list of models.
        /// </summary>
        [Description("Models that were analyzed.")]
        [HelpDescribe(typeof(ModelValidationEntry))]
        public ModelValidationEntry[] Models { get; set; }

        public ValidationResult()
        {
            Errors = new Dictionary<string, string>(StringComparer.Ordinal);
            Models = new ModelValidationEntry[0];
        }
    }
}
