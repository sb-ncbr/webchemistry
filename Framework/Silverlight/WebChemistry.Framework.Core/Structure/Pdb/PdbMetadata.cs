namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Entity types.
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum PdbEntityType
    {
        Unknown,
        Polymer,
        NonPolymer,
        Water
    }

    /// <summary>
    /// Pdb entity types.
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum PdbEntitySource
    {
        NotAssigned,
        GMO,
        Natural,
        Synthetic
    }

    /// <summary>
    /// Stoichiometry types for protein chains
    /// Monomer - single chain
    /// Homomer - multiple chains of the same sequence
    /// Heteromer - multiple chains with different sequences
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum PdbStoichiometryType
    {
        NotAssigned,
        Monomer,
        Homomer,
        Heteromer,
    }


    /// <summary>
    /// Enumeration of PDB content as stated in files
    /// 
    /// Protein - polypeptide(L), polypeptide(D)
    /// DNA - polydeoxyribonucleotide
    /// RNA - polyribonucleotide
    /// + their combinations
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum PdbPolymerType
    {
        NotAssigned = 0,
        Protein,
        DNA,
        RNA,
        ProteinDNA,
        ProteinRNA,
        NucleicAcids,
        Mixture,
        Sugar,
        Other
    }

    /// <summary>
    /// Base class for PDB entity info.
    /// </summary>
    public abstract class PdbEntityBase
    {
        /// <summary>
        /// Unique integer identifier of the entity.
        /// </summary>
        public int EntityId { get; set; }
    }

    public class PdbOrganismData : PdbEntityBase
    {
        /// <summary>
        /// Name of the organism.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Ncbi Taxonomy Identifier
        /// </summary>
        public string TaxonomyId { get; set; }

        /// <summary>
        /// Genus.
        /// </summary>
        public string Genus { get; set; }

        /// <summary>
        /// Host name.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Host taxonomy identifier.
        /// </summary>
        public string HostTaxonomyId { get; set; }

        /// <summary>
        /// Genus of the host.
        /// </summary>
        public string HostGenus { get; set; }
    }

    public class PdbPolymerTypeData : PdbEntityBase
    {
        public string[] Chains { get; set; }
        public PdbPolymerType Type { get; set; }
    }

    /// <summary>
    /// Data about entities in a single PDB(x) file.
    /// </summary>
    public class PdbEntityData : PdbEntityBase
    {
        /// <summary>
        /// Ex number.
        /// </summary>
        public string EcNumber { get; set; }

        /// <summary>
        /// Weight of the entity in kDa.
        /// </summary>
        public double? WeightInKda { get; set; }

        /// <summary>
        /// The number of molecules.
        /// </summary>
        public int NumberOfMolecules { get; set; }

        /// <summary>
        /// Source of the entity.
        /// </summary>
        public PdbEntitySource Source { get; set; }

        /// <summary>
        /// Entity type.
        /// </summary>
        public PdbEntityType Type { get; set; }    
    }

    /// <summary>
    /// PDB Metadata
    /// </summary>
    public class PdbMetadata
    {
        /// <summary>
        /// Resolution in angstroms.
        /// </summary>
        public double? Resolution { get; set; }

        /// <summary>
        /// Keywords.
        /// </summary>
        public string[] Keywords { get; set; }

        /// <summary>
        /// Authors.
        /// </summary>
        public string[] Authors { get; set; }

        /// <summary>
        /// Released data.
        /// </summary>
        public DateTime? Released { get; set; }

        /// <summary>
        /// Latest revision of the structure.
        /// </summary>
        public DateTime? LatestRevision { get; set; }


        /// <summary>
        /// Method of structure determination
        /// Current values:
        /// 
        /// X-RAY DIFFRACTION
        /// SOLUTION NMR
        /// ELECTRON MICROSCOPY
        /// NEUTRON DIFFRACTION
        /// ELECTRON CRYSTALLOGRAPHY
        /// SOLUTION SCATTERING
        /// FIBER DIFFRACTION
        /// FLUORESCENCE TRANSFER
        /// SOLID-STATE NMR
        /// POWDER DIFFRACTION
        /// INFRARED SPECTROSCOPY
        /// 
        /// </summary>
        public string ExperimentMethod { get; set; }

        Lazy<double> totalWeight;
        /// <summary>
        /// Weight of all entities (Molecular weight of all non-water atoms in the asymmetric unit)
        /// </summary>
        public double TotalWeightInKda { get { return totalWeight.Value; } }

        Lazy<PdbEntitySource[]> entitySources;
        /// <summary>
        /// Determines whether the structure was isolated from a natural resource, from GMO or 
        /// synthesized Ab initio
        /// </summary>
        public PdbEntitySource[] EntitySources { get { return entitySources.Value; } }

        Lazy<string[]> originOrganisms;
        /// <summary>
        /// Name of the organism the structure come from
        /// </summary>
        public string[] OriginOrganisms { get  { return originOrganisms.Value; } }

        Lazy<string[]> originOrganismId;
        /// <summary>
        /// Taxonomy ID of the organism according to the NCBI http://www.ncbi.nlm.nih.gov/taxonomy
        /// </summary>
        public string[] OriginOrganismsId { get { return originOrganismId.Value; } }

        Lazy<string[]> originOrganismGenus;
        /// <summary>
        /// Taxonomy ID of the organism genus the structure was cultivated in
        /// according to the NCBI http://www.ncbi.nlm.nih.gov/taxonomy
        /// </summary>
        public string[] OriginOrganismsGenus { get { return originOrganismGenus.Value; } }

        Lazy<string[]> hostOrganisms;
        /// <summary>
        /// Name of the organism structure was cultivated in
        /// </summary>
        public string[] HostOrganisms { get { return hostOrganisms.Value; } }

        Lazy<string[]> hostOrganismsId;
        /// <summary>
        /// Taxonomy ID of the organism the structure was cultivated in
        /// according to the NCBI http://www.ncbi.nlm.nih.gov/taxonomy
        /// </summary>
        public string[] HostOrganismsId { get { return hostOrganismsId.Value; } }

        Lazy<string[]> hostOrganismsGenus;
        /// <summary>
        /// Name of the organism genus
        /// </summary>
        public string[] HostOrganismsGenus { get { return hostOrganismsGenus.Value; } }

        Lazy<string[]> ecNumbers;
        /// <summary>
        /// Enzymatic Commission number assigned to this particular structure
        /// </summary>
        public string[] EcNumbers { get { return ecNumbers.Value; } }

        PdbPolymerType? polymerType;
        /// <summary>
        /// Type of a polymers the structure composes of
        /// </summary>
        public PdbPolymerType PolymerType
        {
            get
            {
                if (polymerType.HasValue) return polymerType.Value;

                var types = PolymerTypeDataByEntityId.Values.Select(a => a.Type).Distinct().ToArray();
                if (types.Length == 1) return types[0];
                if (types.Length == 2)
                {
                    switch (types.Sum(a => (int)a))
                    {
                        case 3: return PdbPolymerType.ProteinDNA; 
                        case 4: return PdbPolymerType.ProteinRNA;
                        case 5: return PdbPolymerType.NucleicAcids;
                        default: break;
                    }
                }
                if (types.Length >= 3) polymerType = PdbPolymerType.Mixture;
                else polymerType = PdbPolymerType.Other;
                return polymerType.Value;
            }
        }

        string proteinStoichiometryString;
        /// <summary>
        /// Returns a stoichiometry string specifying the composition of the polypeptides in the structure
        /// A == StoichiometryType.Monomer
        /// A2, A10, etc. == StoichiometryType.Homomer
        /// AB A2B2, A3B2, etc. referes to a StoichiometryType.Heteromer
        /// </summary>
        public string ProteinStoichiometryString
        {
            get
            {
                if (proteinStoichiometryString != null) return proteinStoichiometryString;
                var sb = new StringBuilder();
                var proteinChains = PolymerTypeDataByEntityId
                    .Values
                    .Where(a => a.Type == PdbPolymerType.Protein)
                    .OrderByDescending(a => a.Chains.Count())
                    .ToArray();

                foreach (var item in proteinChains)
                {
                    int i = Array.IndexOf(proteinChains, item);

                    sb.AppendFormat("{0}{1}",
                        ((char)(65 + (i % 26))).ToString(),
                        item.Chains.Count() == 1 ? "" : item.Chains.Count().ToString());
                }
                proteinStoichiometryString = sb.ToString();
                return proteinStoichiometryString;
            }
        }

        PdbStoichiometryType? proteinStoichiometry;
        /// <summary>
        /// Determines a type of structure based on the ProteinStoichiometry string
        /// </summary>
        public PdbStoichiometryType ProteinStoichiometry
        {
            get
            {
                if (proteinStoichiometry.HasValue) return proteinStoichiometry.Value;

                var pss = ProteinStoichiometryString;
                if (String.IsNullOrEmpty(pss)) proteinStoichiometry = PdbStoichiometryType.NotAssigned;
                else if (pss.Length == 1) proteinStoichiometry = PdbStoichiometryType.Monomer;
                else if (Regex.IsMatch(pss, @"^A\d+$")) proteinStoichiometry = PdbStoichiometryType.Homomer;
                else proteinStoichiometry = PdbStoichiometryType.Heteromer;
                return proteinStoichiometry.Value;
            }
        }

        int[] entityIdentifiers;
        /// <summary>
        /// Entity identifiers.
        /// </summary>
        public int[] EntityIdentifiers
        {
            get 
            {
                if (entityIdentifiers != null) return entityIdentifiers;
                entityIdentifiers = EntityDataById.Keys.OrderBy(k => k).ToArray();
                return entityIdentifiers;
            }
        }

        /// <summary>
        /// Data for entities by identifier (keys correspond to EntityIdentifiers)
        /// </summary>
        public Dictionary<int, PdbEntityData> EntityDataById { get; private set; }
        
        /// <summary>
        /// Data about organisms. The data need not be present for each entity.
        /// </summary>
        public Dictionary<int, PdbOrganismData> OrganismDataByEntityId { get; private set; }

        /// <summary>
        /// Polymer types. Need not be present for each entity.
        /// </summary>
        public Dictionary<int, PdbPolymerTypeData> PolymerTypeDataByEntityId { get; private set; }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        public PdbMetadata()
        {
            Keywords = new string[0];
            Authors = new string[0];
            EntityDataById = new Dictionary<int,PdbEntityData>();
            OrganismDataByEntityId = new Dictionary<int,PdbOrganismData>();
            PolymerTypeDataByEntityId = new Dictionary<int,PdbPolymerTypeData>();

            var polymerEntities = new Lazy<HashSet<int>>(() => EntityDataById.Values.Where(e => e.Type == PdbEntityType.Polymer).Select(e => e.EntityId).ToHashSet());

            entitySources = new Lazy<PdbEntitySource[]>(() => EntityDataById.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Select(a => a.Source).Distinct().ToArray());

            originOrganisms = new Lazy<string[]>(() => OrganismDataByEntityId.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Select(a => a.Name).Where(v => v != null).Distinct(StringComparer.Ordinal).ToArray());
            originOrganismGenus = new Lazy<string[]>(() => OrganismDataByEntityId.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Select(a => a.Genus).Where(v => v != null).Distinct(StringComparer.Ordinal).ToArray());
            originOrganismId = new Lazy<string[]>(() => OrganismDataByEntityId.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Select(a => a.TaxonomyId).Where(v => v != null).Distinct(StringComparer.Ordinal).ToArray());

            hostOrganisms = new Lazy<string[]>(() => OrganismDataByEntityId.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Select(a => a.HostName).Where(v => v != null).Distinct(StringComparer.Ordinal).ToArray());
            hostOrganismsGenus = new Lazy<string[]>(() => OrganismDataByEntityId.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Select(a => a.HostGenus).Where(v => v != null).Distinct(StringComparer.Ordinal).ToArray());
            hostOrganismsId = new Lazy<string[]>(() => OrganismDataByEntityId.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Select(a => a.HostTaxonomyId).Where(v => v != null).Distinct(StringComparer.Ordinal).ToArray());

            //totalWeight = new Lazy<double>(() => EntityDataById.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Where(v => v.WeightInKda.HasValue).Sum(e => e.WeightInKda.Value));
            totalWeight = new Lazy<double>(() => EntityDataById.Values.Where(e => e.Type != PdbEntityType.Water).Where(v => v.WeightInKda.HasValue).Sum(e => e.NumberOfMolecules * e.WeightInKda.Value));

            ecNumbers = new Lazy<string[]>(() => EntityDataById.Values.Where(e => polymerEntities.Value.Contains(e.EntityId)).Select(a => a.EcNumber).Where(v => v != null).Distinct(StringComparer.Ordinal).OrderBy(n => n).ToArray());
        }
    }

}
