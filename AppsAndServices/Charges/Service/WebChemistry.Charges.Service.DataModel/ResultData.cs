namespace WebChemistry.Charges.Service.DataModel
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using WebChemistry.Charges.Core;

    public class ChargesCorrelationEntry
    {
        public string IndependentId { get; set; }
        public string DependentId { get; set; }

        public double A { get; set; }
        public double B { get; set; }

        public double SpearmanCoefficient { get; set; }
        public double PearsonCoefficient { get; set; }
        public double AbsoluteDifferenceSum { get; set; }
        public double Rmsd { get; set; }
        public int DataPointCount { get; set; }

        public ChargesCorrelationEntry Invert()
        {
            return new ChargesCorrelationEntry
            {
                IndependentId = DependentId,
                DependentId = IndependentId,

                A = 1 / (A - B),
                B = B / (B - A),

                SpearmanCoefficient = SpearmanCoefficient,
                PearsonCoefficient = PearsonCoefficient,
                AbsoluteDifferenceSum = AbsoluteDifferenceSum,
                Rmsd = Rmsd,
                DataPointCount = DataPointCount
            };
        }
    }

    public class StructureAtomCharges
    {
        public string Id { get; set; }
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ChargeResultState State { get; set; }
        public long TimingMs { get; set; }
        public string[] Messages { get; set; }
        public Dictionary<int, double> Values { get; set; }
    }

    public class StructureAtomPartitionGroup
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public string[] Residues { get; set; }
        public int[] AtomIds { get; set; }
    }

    public class StructureAtomPartitionCluster
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public int[] GroupIds { get; set; }
    }

    public class StructureAtomPartitionClustering
    {
        public string Name { get; set; }
        public StructureAtomPartitionCluster[] Clusters { get; set; }
    }

    public class StructureAtomPartitionClusterElementStats
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
    }

    public class StructureAtomPartitionClusterStats
    {
        public string ClusterName { get; set; }
        public StructureAtomPartitionClusterElementStats[] Stats { get; set; }
    }

    public class StructureAtomPartitionCharges
    {
        public string RawChargesId { get; set; }
        public string PartitionName { get; set; }
        public Dictionary<int, double> GroupCharges { get; set; }
        public Dictionary<string, StructureAtomPartitionClusterStats> ClusterStats { get; set; }
    }

    public class StructureAtomPartition
    {
        public string Name { get; set; }
        public Dictionary<int, StructureAtomPartitionGroup> Groups { get; set; }
        public Dictionary<string, StructureAtomPartitionClustering> Clusters { get; set; }
        public Dictionary<string, StructureAtomPartitionCharges> Charges { get; set; }
        public Dictionary<string, Dictionary<string, ChargesCorrelationEntry>> Correlations { get; set; }
    }
}
