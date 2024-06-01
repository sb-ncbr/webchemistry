namespace WebChemistry.Charges.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.Queries;

    /// <summary>
    /// The default partitions.
    /// </summary>
    public static class DefaultParititionDescriptors
    {
        static readonly PartitionDescriptor AtomsPartition, ResiduesPartition, AtomTypesPartition;

        public static readonly PartitionDescriptor[] Partitions;

        /// <summary>
        /// Make the default partitions from a structure.
        /// 
        /// For a protein -- Atom and Residues
        /// For other molecules -- Atom
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static AtomPartition[] MakePartitions(IStructure structure)
        {
            if (structure.IsPdbStructure() || structure.IsMol2())
            {
                return new [] { AtomsPartition.Apply(structure), ResiduesPartition.Apply(structure)/*, AtomTypesPartition.Apply(structure)*/ };
            }
            return new[] { AtomsPartition.Apply(structure)/*, AtomTypesPartition.Apply(structure)*/ };
        }

        static DefaultParititionDescriptors()
        {
            var atoms = QueryBuilder.Atoms().ToMetaQuery().Compile() as QueryMotive;
            var residues = QueryBuilder.Residues().ToMetaQuery().Compile() as QueryMotive;
            var atomTypes = QueryBuilder.GroupedAtoms().ToMetaQuery().Compile() as QueryMotive;
            AtomsPartition = new PartitionDescriptor { Name = "Atoms", TopLevel = atoms, BottomLevel = atoms };
            ResiduesPartition = new PartitionDescriptor { Name = "Residues", TopLevel = residues, BottomLevel = residues };
            AtomTypesPartition = new PartitionDescriptor { Name = "AtomTypes", TopLevel = atomTypes, BottomLevel = atomTypes };
        }
    }

    /// <summary>
    /// Specifies a partition using two MQ expressions.
    /// </summary>
    public class PartitionDescriptor
    {
        /// <summary>
        /// Name of the descriptor.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Top level grouping. For example "Residues()".
        /// </summary>
        public QueryMotive TopLevel { get; set; }

        /// <summary>
        /// Bottom level "subset". For example "AtomNames("Ca")".
        /// </summary>
        public QueryMotive BottomLevel { get; set; }

        static IStructure Induce(string name, List<AtomPartition.Group> groups, IStructure s)
        {
            var labels = s.Atoms.ToDictionary(a => a, _ => new HashSet<int>());
            foreach (var g in groups)
            {                
                foreach (var a in g.Data)
                {
                    labels[a].Add(g.Id);
                }
            }

            var atoms = groups.ToDictionary(
                g => g,
                g => Atom.Create(
                    g.Id,
                    g.Data.Count == 1 ? g.Data[0].ElementSymbol : ElementSymbols.Unknown,
                    g.Data.GeometricalCenter()));
                    //residueName: g.Data[0].PdbResidueName(),
                    //residueSequenceNumber: g.Data[0].PdbResidueSequenceNumber(),
                    //chainIdentifier: g.Data[0].PdbChainIdentifier()));

            var bonds = new HashSet<IBond>();

            var queue = new Queue<Tuple<IAtom, int>>();
            var visited = new HashSet<IAtom>();

            queue.Enqueue(Tuple.Create(groups[0].Data[0], groups[0].Id));

            var links = groups.Select(g => new LinkedListNode<AtomPartition.Group>(g)).ToArray();
            var remainingGroups = new LinkedList<AtomPartition.Group>();
            links.ForEach(l => remainingGroups.AddFirst(l));

            while (queue.Count > 0 || remainingGroups.Count > 0)
            {
                if (queue.Count == 0)
                {
                    var g = remainingGroups.First.Value;
                    queue.Enqueue(Tuple.Create(g.Data[0], g.Id));
                    continue;
                }

                var pivot = queue.Dequeue();
                var a = pivot.Item1;
                var label = pivot.Item2;
                visited.Add(a);
                var link = links[label];
                if (link.List != null) remainingGroups.Remove(links[label]);

                var bs = s.Bonds[a];
                for (int i = 0; i < bs.Count; i++)
                {
                    var b = bs[i].B;
                    if (visited.Contains(b)) continue;

                    var ls = labels[b];

                    if (ls.Count == 0 || ls.Contains(label))
                    {
                        queue.Enqueue(Tuple.Create(b, label));
                    }
                    else
                    {
                        var t = atoms[groups[label]];
                        var nl = -1;
                        foreach (var l in ls)
                        {
                            bonds.Add(Bond.Create(t, atoms[groups[l]], BondType.Single));
                            if (nl < 0) nl = l;
                        }
                        queue.Enqueue(Tuple.Create(b, nl));
                    }
                }
            }

            var ret = Structure.Create(s.Id + "_" + name, AtomCollection.Create(atoms.Values), BondCollection.Create(bonds));
            ret.ToCentroidCoordinates();
            return ret;
        }

        /// <summary>
        /// Create partitioning from a structure.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public AtomPartition Apply(IStructure s)
        {
            bool isPdbOrMol2 = s.IsPdbStructure() || s.IsMol2();
            var bonds = s.Bonds;
            var allAtomsSet = s.Atoms.ToHashSet();

            var partitions = TopLevel.Matches(s)
                    .Select(t => BottomLevel.Matches(t.ToStructure("t", true, true))
                        .SelectMany(m => m.Atoms.Select(a => s.Atoms.GetById(a.Id)))
                        .ToList())
                    .Where(xs => xs.Count > 0)
                    .Select((xs, i) => AtomPartition.Group.Create(i, xs, allAtomsSet, bonds, isPdbOrMol2))
                    .ToList();

            if (partitions.Count == 0)
            {
                LogService.Default.Warning("{0} partitioning of '{1}' yielded no groups. Collapsing all atoms into a single group.", Name, s.Id);
                partitions = new List<AtomPartition.Group> { AtomPartition.Group.Create(0, s.Atoms, allAtomsSet, bonds, isPdbOrMol2) };
            }

            var clusters = new List<PartitionClustering>();
            bool isAtoms = Name == "Atoms";
            
            if (isPdbOrMol2)
            {
                clusters.Add(new PartitionClustering
                {
                    Name = "ResidueName",
                    Clusters = new ReadOnlyCollection<PartitionClustering.Cluster>(partitions
                        .GroupBy(p => p.Residues)
                        .Select((g, i) => new PartitionClustering.Cluster { Id = i, Groups = new ReadOnlyCollection<AtomPartition.Group>(g.ToArray()), Key = g.Key })
                        .ToList())
                });

                clusters.Add(new PartitionClustering
                {
                    Name = isAtoms ? "ChemicalElement" : "AtomicComposition",
                    Clusters = new ReadOnlyCollection<PartitionClustering.Cluster>(partitions
                        .GroupBy(p => p.Atoms)
                        .Select((g, i) => new PartitionClustering.Cluster { Id = i, Groups = new ReadOnlyCollection<AtomPartition.Group>(g.ToArray()), Key = g.Key })
                        .ToList())
                });

                if (partitions.All(g => g.ResidueCount == 1))
                {
                    clusters.Add(new PartitionClustering
                    {
                        Name = "ResidueChargeType",
                        Clusters = new ReadOnlyCollection<PartitionClustering.Cluster>(partitions
                            .GroupBy(p => PdbResidue.GetChargeType(p.Data[0].PdbResidueName()))
                            .Select((g, i) => new PartitionClustering.Cluster { Id = i, Groups = new ReadOnlyCollection<AtomPartition.Group>(g.ToArray()), Key = g.Key.ToString() })
                            .ToList())
                    });
                }
            }
            else
            {
                clusters.Add(new PartitionClustering
                {
                    Name = isAtoms ? "ChemicalElement" : "AtomicComposition",
                    Clusters = new ReadOnlyCollection<PartitionClustering.Cluster>(partitions
                        .GroupBy(p => p.Atoms)
                        .Select((g, i) => new PartitionClustering.Cluster { Id = i, Groups = new ReadOnlyCollection<AtomPartition.Group>(g.ToArray()), Key = g.Key })
                        .ToList())
                });
            }

            return new AtomPartition
            {
                Name = Name,
                Structure = s,
                InducedStructure = Induce(Name, partitions, s),
                Groups = new ReadOnlyCollection<AtomPartition.Group>(partitions),
                Clusters = new ReadOnlyCollection<PartitionClustering>(clusters)
            };
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        }

        public override bool Equals(object obj)
        {
            var d = obj as PartitionDescriptor;
            if (d == null) return false;
            return Name.EqualOrdinalIgnoreCase(d.Name);
        }
    }

    /// <summary>
    /// Actual parititoning.
    /// </summary>
    public class AtomPartition
    {
        /// <summary>
        /// Single group.
        /// </summary>
        public class Group
        {
            /// <summary>
            /// Serial number id.
            /// </summary>
            public int Id { get; private set; }

            /// <summary>
            /// The atoms.
            /// </summary>
            public ReadOnlyCollection<IAtom> Data { get; private set; }

            /// <summary>
            /// Atom string.
            /// </summary>
            public string Atoms { get; private set; }

            /// <summary>
            /// Atom identifiers.
            /// </summary>
            public string AtomIdentifiers { get; private set; }

            /// <summary>
            /// Residue string.
            /// </summary>
            public string Residues { get; private set; }

            /// <summary>
            /// Residue identifiers, separated by "-"
            /// </summary>
            public string ResidueIdentifiers { get; private set; }

            /// <summary>
            /// Residue identifiers, separated by "-"
            /// </summary>
            public string[] ResidueNames { get; private set; }

            /// <summary>
            /// Residue count.
            /// </summary>
            public int ResidueCount { get; private set; }

            /// <summary>
            /// Group label.
            /// </summary>
            public string Label { get; private set; }
            
            /// <summary>
            /// Create the group.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="atoms"></param>
            /// <param name="isPdb"></param>
            /// <returns></returns>
            public static Group Create(int id, IEnumerable<IAtom> atoms, HashSet<IAtom> allAtomsSet, IBondCollection bonds, bool isPdb)
            {
                var data = new ReadOnlyCollection<IAtom>(atoms.AsList());
                var ac = PdbResidueCollection.GetCountedResidueString(data.GroupBy(a => a.ElementSymbol).ToDictionary(g => g.Key.ToString(), g => g.Count()));

                var residues = data
                    .GroupBy(a => PdbResidueIdentifier.FromAtom(a))
                    .OrderBy(g => g.Key)
                    .ToArray();

                var rc = residues
                    .GroupBy(g => g.First().PdbResidueName())
                    .ToDictionary(g => g.Key, g => g.Count());

                var residueNames = residues.Select(g => g.First())
                        .OrderBy(r => r.PdbChainIdentifier())
                        .ThenBy(r => r.PdbResidueSequenceNumber())
                        .Select(r => r.ResidueString())
                        .ToArray();

                var residueIdentifiers = string.Join("-", residueNames);

                string label = "";
                var residueCount = rc.Sum(r => r.Value);

                var fstAtom = data[0].ElementSymbol;
                if (data.Count == 1)
                {
                    var atom = data[0];
                    var type = EemSolver.GetBondType(bonds[atom], allAtomsSet);
                    var chainId = atom.PdbChainIdentifier();

                    if (isPdb)
                    {
                        // Symbol:Type Name Serial (ResName ResSerial ResChain)
                        label = string.Format("{1}:{6} {0} {2} ({3} {4}{5})",
                            atom.PdbName(), atom.ElementSymbol.ToString(), atom.PdbSerialNumber(),
                            atom.PdbResidueName(), atom.PdbResidueSequenceNumber(), string.IsNullOrWhiteSpace(chainId) ? "" : " " + chainId,
                            type);
                    }
                    else
                    {
                        // Symbol:Type Id
                        label = string.Format("{0}:{2} {1}", atom.ElementSymbol.ToString(), atom.Id, type);
                    }
                }
                else if (data.All(a => a.ElementSymbol == fstAtom))
                {
                    label = string.Format("[{0}] {1}", fstAtom, residueIdentifiers);
                }
                else if (residueCount == 1)
                {
                    var atom = data[0];
                    label = string.Format("{0} {1} {2} ({3} atoms)", atom.PdbResidueName(), atom.PdbResidueSequenceNumber(), atom.PdbChainIdentifier(), data.Count);
                }
                else
                {
                    label = residueIdentifiers;
                }

                return new Group
                {
                    Id = id,
                    Label = label,
                    Data = data,
                    Atoms = ac,
                    Residues = PdbResidueCollection.GetCountedResidueString(rc),
                    ResidueIdentifiers = residueIdentifiers,
                    AtomIdentifiers = string.Join("-", data.OrderBy(a => a.PdbSerialNumber()).Select(a => string.Format("{0} {1}", a.ElementSymbol, a.PdbSerialNumber()))),
                    ResidueCount = residueCount,
                    ResidueNames = residueNames
                };
            }

            /// <summary>
            /// Compute the atom charge for the group.
            /// </summary>
            /// <param name="atomCharges"></param>
            /// <returns></returns>
            public double? GetCharge(IDictionary<IAtom, ChargeValue> atomCharges)
            {
                var data = Data.Where(a => atomCharges.ContainsKey(a)).ToList();
                if (data.Count == 0) return null;
                return data.Sum(a => atomCharges[a].Charge);
            }

            public override int GetHashCode()
            {
                return Id;
            }

            public override bool Equals(object obj)
            {
                var g = obj as Group;
                if (g == null) return false;
                return g.Id == Id;
            }

            private Group()
            {

            }
        }

        /// <summary>
        /// Name of the partition.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parent.
        /// </summary>
        public IStructure Structure { get; set; }

        /// <summary>
        /// Substructure induced by the groups.
        /// </summary>
        public IStructure InducedStructure { get; set; }

        /// <summary>
        /// The partitions.
        /// </summary>
        public ReadOnlyCollection<Group> Groups { get; set; }

        /// <summary>
        /// Clusters.
        /// </summary>
        public ReadOnlyCollection<PartitionClustering> Clusters { get; set; }

        /// <summary>
        /// Compute the group charges.
        /// </summary>
        /// <param name="atomCharges"></param>
        /// <returns></returns>
        public AtomPartitionCharges GetGroupCharges(ChargeComputationResult atomCharges)
        {
            var charges = atomCharges.Charges;
            var partitionCharges = Groups
                    .Select(g => new { Group = g, Charge = g.GetCharge(charges) })
                    .Where(c => c.Charge.HasValue)
                    .ToDictionary(c => c.Group, c => c.Charge.Value);

            return new AtomPartitionCharges
            {
                Partition = this,
                PartitionCharges = partitionCharges,
                ClusterStats = Clusters.Select(c => c.GetClusterStats(partitionCharges)).ToList(),
                MinCharge = partitionCharges.Values.Min(),
                MaxCharge = partitionCharges.Values.Max(),
                RawCharges = atomCharges
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Charges and stats for atom partitioning.
    /// </summary>
    public class AtomPartitionCharges
    {
        public AtomPartition Partition { get; set; }
        public ChargeComputationResult RawCharges { get; set; }
        public IDictionary<AtomPartition.Group, double> PartitionCharges { get; set; }
        public double MinCharge { get; set; }
        public double MaxCharge { get; set; }
        public IList<PartitionClusteringStats> ClusterStats { get; set; }
    }

    /// <summary>
    /// Clusters of partition groups.
    /// </summary>
    public class PartitionClustering
    {
        /// <summary>
        /// Group cluster.
        /// </summary>
        public class Cluster
        {
            /// <summary>
            /// Key -- for example residue name or charge type.
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// The groups.
            /// </summary>
            public ReadOnlyCollection<AtomPartition.Group> Groups { get; set; }

            /// <summary>
            /// The id.
            /// </summary>
            public int Id { get; set; }

            public override int GetHashCode()
            {
                return Id;
            }

            public override bool Equals(object obj)
            {
                var c = obj as Cluster;
                if (c == null) return false;
                return c.Id == Id;
            }
        }

        /// <summary>
        /// Charge statistics for a cluster.
        /// </summary>
        public class ClusterStats
        {
            public string Key { get; set; }
            public int Count { get; set; }
            public double? MinCharge { get; set; }
            public double? MaxCharge { get; set; }
            public double? Average { get; set; }
            public double? AbsAverage { get; set; }
            public double? Median { get; set; }
            public double? AbsMedian { get; set; }
            public double? Sigma { get; set; }
            public double? AbsSigma { get; set; }

            /// <summary>
            /// Computes the stats for a cluster group.
            /// </summary>
            /// <param name="cluster"></param>
            /// <param name="groupCharges"></param>
            /// <returns></returns>
            public static ClusterStats Create(Cluster cluster, IDictionary<AtomPartition.Group, double> groupCharges)
            {
                var data = cluster.Groups.Where(g => groupCharges.ContainsKey(g)).Select(g => groupCharges[g]).ToArray();

                if (data.Length == 0)
                {
                    return new ClusterStats
                    {
                        Key = cluster.Key,
                        Count = 0
                    };
                }

                var absData = data.Select(x => Math.Abs(x)).ToArray();

                return new ClusterStats
                {
                    Key = cluster.Key,
                    Count = cluster.Groups.Count,
                    MinCharge = Math.Round(data.Min(), 3),
                    MaxCharge = Math.Round(data.Max(), 3),
                    Average = Math.Round(data.Average(), 3),
                    AbsAverage = Math.Round(absData.Average(), 3),
                    Sigma = data.Length == 1 ? 0 : Math.Round(MathNet.Numerics.Statistics.Statistics.StandardDeviation(data), 3),
                    AbsSigma = data.Length == 1 ? 0 : Math.Round(MathNet.Numerics.Statistics.Statistics.StandardDeviation(absData), 3),
                    Median = Math.Round(MathNet.Numerics.Statistics.Statistics.Median(data), 3),
                    AbsMedian = Math.Round(MathNet.Numerics.Statistics.Statistics.Median(absData), 3)
                };
            }

            private ClusterStats()
            {

            }
        }

        /// <summary>
        /// Create an exporter from the data.
        /// </summary>
        /// <param name="separator"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ListExporter GetClusterStatsExporter(string separator, IEnumerable<ClusterStats> data)
        {
            return data.GetExporter(separator)
                .AddExportableColumn(c => c.Key, ColumnType.String, "Key")
                .AddExportableColumn(c => c.Count, ColumnType.String, "Count")
                .AddExportableColumn(c => c.MinCharge, ColumnType.Number, "MinCharge")
                .AddExportableColumn(c => c.MaxCharge, ColumnType.Number, "MaxCharge")
                .AddExportableColumn(c => c.Average, ColumnType.Number, "Average")
                .AddExportableColumn(c => c.AbsAverage, ColumnType.Number, "AbsAverage")
                .AddExportableColumn(c => c.Median, ColumnType.Number, "Median")
                .AddExportableColumn(c => c.AbsMedian, ColumnType.Number, "AbsMedian")
                .AddExportableColumn(c => c.Sigma, ColumnType.Number, "Sigma")
                .AddExportableColumn(c => c.AbsSigma, ColumnType.Number, "AbsSigma");
        }


        /// <summary>
        /// Name of the clustering.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The clusters.
        /// </summary>
        public ReadOnlyCollection<Cluster> Clusters { get; set; }

        /// <summary>
        /// Get the cluster stats.
        /// </summary>
        /// <param name="groupCharges"></param>
        /// <returns></returns>
        public PartitionClusteringStats GetClusterStats(IDictionary<AtomPartition.Group, double> groupCharges)
        {
            return new PartitionClusteringStats
            {
                Clustering = this,
                Stats = Clusters.Select(c => ClusterStats.Create(c, groupCharges)).ToList()
            };
        }
    }

    /// <summary>
    /// Stats for the clusters.
    /// </summary>
    public class PartitionClusteringStats
    {
        public PartitionClustering Clustering { get; set; }
        public IList<PartitionClustering.ClusterStats> Stats { get; set; }

        public override string ToString()
        {
            return Clustering.Name;
        }
    }
}
