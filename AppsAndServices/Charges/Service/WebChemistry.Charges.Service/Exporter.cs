namespace WebChemistry.Charges.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using WebChemistry.Charges.Core;
    using WebChemistry.Charges.Service.DataModel;
    using WebChemistry.Framework.Core;
    using WebChemistry.Platform;

    class ChargeComputationResultWrapper
    {
        public TimeSpan Timing { get; set; }
        public ChargeComputationResult Result { get; set; }

        public ChargeComputationResultWrapper(TimeSpan timing, ChargeComputationResult result)
        {
            this.Timing = timing;
            this.Result = result;
        }
    }

    class AtomPartitionResultWrapper
    {
        public AtomPartition Partition { get; set; }
        public IList<AtomPartitionCharges> PartitionCharges { get; set; }
        public Dictionary<string, Dictionary<string, ChargesCorrelationEntry>> Correlations { get; set; }
    }
    
    class ExporterStructureEntry
    {
        public IStructure Structure { get; set; }
        public IList<ChargeComputationResultWrapper> Charges { get; set; }
    }

    class ExporterPartitionEntry
    {
        public IStructure Structure { get; set; }
        
        public AtomPartition Partition { get; set; }
        public IList<AtomPartitionCharges> PartitionCharges { get; set; }
        public Dictionary<string, Dictionary<string, ChargesCorrelationEntry>> Correlations { get; set; }
    }

    class ExporterStructureDataEntry
    {
        public IStructure Structure { get; set; }
        public ChargesServiceComputationEntrySummary Summary { get; set; }
        public IList<ChargeComputationResultWrapper> Charges { get; set; }
        public IList<AtomPartitionResultWrapper> Partitions { get; set; }
    }

    class ChargeServiceExporter : IDisposable
    {
        static readonly string ExporterSeparator = ",";
        static readonly string ExporterTableExtension = ".csv";

        #region Read-me
        static readonly string ReadMeText = new string[] {
            "The structure of the output is as follows:",
            "------------------------------------------",
            "charges/ ",
            "  <source1>/",
            "    mol2/  [folder with structures in MOL2 format]",
            "    pqr/   [folder with structures in PQR format]",
            "    wprop/ [folder with results in WPROP format - a list of pairs of atom serial numbers and values of atomic charges <Atom Id> <Charge>]",
            "    computation_setup.json  [file describing the setup of the calculation]",
            "  <source2>/",
            "    ...",
            "  <molecule>_allcharges.csv [CSV file for each molecule with all computed charges]",
            "statistics/",
            "  Atoms/ [statistics data for each molecule, calculated at atomic level resolution]",
            "    mol2/ [folder with structures in MOL2 format]",
            "    csv/  [folder - for each molecule, a CSV file with all computed atomic charges]",
            "    correlations/ [folder - for each molecule, a CSV file with correlation statistics between all computed atomic charges]",
            "    properties/ [folder with statistics based on several specific properties of atoms]",
            "  Residues/ [statistics data for each molecule, calculated at the residue level resolution - only present if the input file contained relevant residue information]",
            "    mol2/ [folder with mock structures in MOL2 format, where each atom represents a single residue]",
            "    csv/  [folder - for each molecule, a CSV file with all computed residue charges]",
            "    correlations/ [folder - for each molecule, a CSV file with correlation statistics between all computed residue charges]",
            "    properties/ [folder with statistics based on several specific properties of residues]",
            "json/ [all data for each molecule in JSON format]",
            "  <molecule>.json [entry for each molecule]",
            "logs/",
            "  <molecule>_log.csv [a log entry for each molecule that includes execution time, warnings, etc]",
            "Sets.xml [parameter sets used for the computation]",
            "Summary_<date:year-month-day>_<time:hour-minute>.json [summary information about the entire computation, date/time is in universal time]"

        }.JoinBy(Environment.NewLine);
        #endregion

        #region CSV Exporter
        static string GetCorrelationsCsv(IEnumerable<ChargesCorrelationEntry> xs)
        {
            return xs.GetExporter(ExporterSeparator)
                .AddExportableColumn(x => x.IndependentId, ColumnType.String, "Pivot")
                .AddExportableColumn(x => x.DependentId, ColumnType.String, "Other")
                .AddExportableColumn(x => x.A.ToStringInvariant("0.0000"), ColumnType.Number, "A (y = Ax + B)")
                .AddExportableColumn(x => x.B.ToStringInvariant("0.0000"), ColumnType.Number, "B (y = Ax + B)")
                .AddExportableColumn(x => x.PearsonCoefficient.ToStringInvariant("0.0000"), ColumnType.Number, "Pearson")
                .AddExportableColumn(x => x.SpearmanCoefficient.ToStringInvariant("0.0000"), ColumnType.Number, "Spearman")
                .AddExportableColumn(x => x.Rmsd.ToStringInvariant("0.0000"), ColumnType.Number, "RMSD")
                .AddExportableColumn(x => x.AbsoluteDifferenceSum.ToStringInvariant("0.0000"), ColumnType.Number, "DiffSum")
                .AddExportableColumn(x => x.DataPointCount, ColumnType.Number, "DataPointCount")
                .ToCsvString();
        }

        static string GetCorrelationsCsv(Dictionary<string, Dictionary<string, ChargesCorrelationEntry>> correlations)
        {
            return GetCorrelationsCsv(correlations
                .SelectMany(a => a.Value.SelectMany(b => new[] { b.Value, b.Value.Invert() }))
                .OrderBy(c => c.IndependentId)
                .ThenBy(c => c.DependentId));
        }

        static string GetRadiusString(EemChargeComputationParameters prms)
        {
            if (prms.Method == ChargeComputationMethod.Eem || prms.Method == ChargeComputationMethod.Reference) return "-";
            return prms.CutoffRadius.ToStringInvariant();
        }

        static string GetTotalChargeString(EemChargeComputationParameters prms)
        {
            if (prms.Method == ChargeComputationMethod.Reference) return "-";
            return prms.TotalCharge.ToStringInvariant();
        }

        static string GetSummaryCsv(ExporterStructureEntry entry)
        {
            return entry.Charges.GetExporter(ExporterSeparator)
                .AddStringColumn(r => r.Result.Parameters.Id, "Id")
                .AddStringColumn(r => r.Result.Parameters.Method, "Method")
                .AddStringColumn(r => GetRadiusString(r.Result.Parameters), "Radius")
                .AddNumericColumn(r => GetTotalChargeString(r.Result.Parameters), "TotalCharge")
                .AddStringColumn(r => r.Result.State, "State")
                .AddNumericColumn(r => r.Timing, "Timing")
                .AddStringColumn(r => string.Join("; ", r.Result.Messages), "Messages")
                .ToCsvString();
        }

        static string GetRawChargesCsv(IStructure structure, IList<ChargeComputationResult> charges)
        {
            var exp = structure.Atoms.GetExporter(ExporterSeparator)
                .AddNumericColumn(a => a.Id, "Id")
                .AddStringColumn(a => a.ElementSymbol, "Element");

            if (structure.IsPdbStructure() || structure.IsMol2())
            {
                exp.AddStringColumn(a => a.PdbName(), "AtomName");
            }

            var pivot = charges.FirstOrDefault(c => c.Parameters.Method != ChargeComputationMethod.Reference);

            if (pivot != null)
            {
                var types = pivot.Multiplicities;
                exp.AddNumericColumn(a => types.ContainsKey(a) ? types[a].ToString() : "-", "BondType");
            }

            foreach (var _cs in charges)
            {
                var cs = _cs.Charges;
                exp.AddNumericColumn(a =>
                {
                    ChargeValue value;
                    if (cs.TryGetValue(a, out value)) return value.Charge.ToStringInvariant("0.00000");
                    return "-";
                }, _cs.Parameters.Id);
            }

            return exp.ToCsvString();
        }

        static string GetPartitionChargesCsv(AtomPartition partition, IList<AtomPartitionCharges> charges)
        {
            var exp = partition.Groups.GetExporter(ExporterSeparator)
                .AddExportableColumn(r => r.Id, ColumnType.Number, "Id")
                .AddExportableColumn(r => r.Residues, ColumnType.String, "ResidueSignature")
                .AddExportableColumn(r => r.ResidueCount, ColumnType.Number, "ResidueCount")
                .AddExportableColumn(r => r.ResidueIdentifiers, ColumnType.String, "Residues")
                .AddExportableColumn(r => r.Atoms, ColumnType.String, "AtomSignature")
                .AddExportableColumn(r => r.Data.Count, ColumnType.Number, "AtomCount")
                .AddExportableColumn(r => r.AtomIdentifiers, ColumnType.String, "Atoms");
                                    
            foreach (var _cs in charges)
            {                
                var cs = _cs.PartitionCharges;
                exp.AddNumericColumn(g =>
                {
                    double value;
                    if (cs.TryGetValue(g, out value)) return value.ToStringInvariant("0.00000");
                    return "-";
                }, _cs.RawCharges.Parameters.Id);
            }

            return exp.ToCsvString();
        }        

        static string GetAggregatesCsv(AtomPartition partition, IList<AtomPartitionCharges> charges, int clusterIndex)
        {
            var clusters = charges.SelectMany(c => c.ClusterStats[clusterIndex].Stats.Select(a => new { Id = c.RawCharges.Parameters.Id, Cluster = a }));

            var exp = clusters.GetExporter(ExporterSeparator)
                .AddStringColumn(c => c.Id, "Id")
                .AddExportableColumn(c => c.Cluster.Key, ColumnType.String, "Key")
                .AddExportableColumn(c => c.Cluster.Count, ColumnType.String, "Count")
                .AddExportableColumn(c => c.Cluster.MinCharge, ColumnType.Number, "MinCharge")
                .AddExportableColumn(c => c.Cluster.MaxCharge, ColumnType.Number, "MaxCharge")
                .AddExportableColumn(c => c.Cluster.Average, ColumnType.Number, "Average")
                .AddExportableColumn(c => c.Cluster.AbsAverage, ColumnType.Number, "AbsAverage")
                .AddExportableColumn(c => c.Cluster.Median, ColumnType.Number, "Median")
                .AddExportableColumn(c => c.Cluster.AbsMedian, ColumnType.Number, "AbsMedian")
                .AddExportableColumn(c => c.Cluster.Sigma, ColumnType.Number, "Sigma")
                .AddExportableColumn(c => c.Cluster.AbsSigma, ColumnType.Number, "AbsSigma");

            return exp.ToCsvString();
        }
        #endregion

        #region Result Data Exporter

        static StructureAtomPartitionClusterElementStats ConvertClusterStats(PartitionClustering.ClusterStats stats)
        {
            return new StructureAtomPartitionClusterElementStats
            {
                Key = stats.Key,
                Count = stats.Count,
                MinCharge = stats.MinCharge,
                MaxCharge = stats.MaxCharge,
                Average = stats.Average,
                AbsAverage = stats.AbsAverage,
                Median = stats.Median,
                AbsMedian = stats.AbsMedian,
                Sigma = stats.Sigma,
                AbsSigma = stats.AbsSigma
            };
        }

        static StructureAtomCharges ConvertChargeComputationResult(ChargeComputationResult result)
        {
            return new StructureAtomCharges
            {
                Id = result.Id,
                Messages = result.Messages,
                State = result.State,
                TimingMs = (long)result.Timing.TotalMilliseconds,
                Values = result.Charges.ToDictionary(c => c.Key.Id, c => c.Value.Charge)
            };
        }

        static StructureAtomPartition ConvertAtomPartition(AtomPartitionResultWrapper entry)
        {
            return new StructureAtomPartition
            {
                Name = entry.Partition.Name,
                Groups = entry.Partition.Groups.ToDictionary(
                    g => g.Id,
                    g => new StructureAtomPartitionGroup
                    {
                        Id = g.Id,
                        Label = g.Label,
                        Residues = g.ResidueNames,
                        AtomIds = g.Data.Select(a => a.Id).ToArray()
                    }),
                Clusters = entry.Partition.Clusters.ToDictionary(
                    c => c.Name,
                    c => new StructureAtomPartitionClustering
                    {
                        Name = c.Name,
                        Clusters = c.Clusters.Select(pc => new StructureAtomPartitionCluster
                        {
                            Id = pc.Id,
                            Key = pc.Key,
                            GroupIds = pc.Groups.Select(g => g.Id).ToArray()
                        }).ToArray()
                    }),
                Correlations = entry.Correlations,
                Charges = entry.PartitionCharges.ToDictionary(
                    c => c.RawCharges.Id,
                    c => new StructureAtomPartitionCharges
                    {
                        RawChargesId = c.RawCharges.Id,
                        PartitionName = c.Partition.Name,
                        GroupCharges = c.PartitionCharges.ToDictionary(x => x.Key.Id, x => x.Value),
                        ClusterStats = c.ClusterStats.ToDictionary(
                            s => s.Clustering.Name,
                            s => new StructureAtomPartitionClusterStats
                            {
                                ClusterName = s.Clustering.Name,
                                Stats = s.Stats.Select(st => ConvertClusterStats(st)).ToArray()
                            })
                    })
            };
        }

        #endregion

        ZipUtils.ZipWrapper Zip, JsonDataZip;
        string OutputFolder;
        bool ExportUncompressed;
        ChargesService Svc;
        DateTime Created;
        
        void WriteEntry(string directory, string filename, string content)
        {
            var path = string.IsNullOrEmpty(directory) ? filename : Path.Combine(directory, filename);
            Zip.AddEntry(path, content);
            if (ExportUncompressed)
            {                
                var dir = string.IsNullOrEmpty(directory) ? OutputFolder : Path.Combine(OutputFolder, directory);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, filename), content);
            }
        }
        
        static double GetChargeValue(IDictionary<IAtom, ChargeValue> charges, IAtom atom)
        {
            ChargeValue v;
            if (charges.TryGetValue(atom, out v)) return v.Charge;
            return 0.0;
        }

        static double? GetChargeValueNullable(IDictionary<IAtom, ChargeValue> charges, IAtom atom)
        {
            ChargeValue v;
            if (charges.TryGetValue(atom, out v)) return v.Charge;
            return null;
        }

        RealAtomProperties MakeProperties(IStructure structure, ChargeComputationResult result)
        {
            var charges = result.Charges;
            var props = RealAtomProperties.Create(structure, result.Parameters.Id, "Charges", a => GetChargeValueNullable(charges, a));
            structure.AttachAtomProperties(props);
            return props;
        }
        
        public void ExportStructure(ExporterStructureEntry entry)
        {
            var basePrefix = "charges";
                  
            var toExport = entry.Charges.Where(r => r.Result.State != ChargeResultState.Error).ToArray();

            // Info
            foreach (var c in toExport)
            {
                var prms = c.Result.Parameters;
                WriteEntry(Path.Combine(basePrefix, c.Result.Parameters.Id), "computation_setup.json", new
                {
                    Method = prms.Method.ToString(),
                    Precision = prms.Precision.ToString(),
                    CutoffRadius = prms.CutoffRadius,
                    CorrectCutoffTotalCharge = prms.CorrectCutoffTotalCharge,
                    IgnoreWaters = prms.IgnoreWaters,
                    TotalCharge = prms.TotalCharge,
                    SetName = prms.Set.Name
                }.ToJsonString());
            }

            // CSV
            WriteEntry("logs", entry.Structure.Id + "_log" + ExporterTableExtension, GetSummaryCsv(entry));
            WriteEntry(basePrefix, entry.Structure.Id + "_allcharges" + ExporterTableExtension, 
                GetRawChargesCsv(entry.Structure, toExport.Select(r => r.Result).ToArray()));
                                    
            // WPROP
            foreach (var c in toExport)
            {
                var props = MakeProperties(entry.Structure, c.Result);
                var src = props.WriteToString();
                WriteEntry(Path.Combine(basePrefix, c.Result.Parameters.Id, "wprop"), entry.Structure.Id + "_" + c.Result.Parameters.Id + ".wprop", src);
            }

            // MOL2
            foreach (var c in toExport)
            {
                var charges = c.Result.Charges;
                var src = entry.Structure.ToMol2String(chargeSelector: a => GetChargeValue(charges, a));
                WriteEntry(Path.Combine(basePrefix, c.Result.Parameters.Id, "mol2"), entry.Structure.Id + "_" + c.Result.Parameters.Id + ".mol2", src);
            }

            // PQR
            foreach (var c in toExport)
            {
                var charges = c.Result.Charges;
                var src = entry.Structure.ToPqrString(chargeSelector: a => GetChargeValue(charges, a));
                WriteEntry(Path.Combine(basePrefix, c.Result.Parameters.Id, "pqr"), entry.Structure.Id + "_" + c.Result.Parameters.Id + ".pqr", src);
            }
        }

        public void ExportPartition(ExporterPartitionEntry entry)
        {
            var basePrefix = Path.Combine("statistics", entry.Partition.Name);

            // CSV
            var pathPrefix = basePrefix;
            WriteEntry(Path.Combine(pathPrefix, "csv"), entry.Structure.Id + "_allcharges_" + entry.Partition.Name + ExporterTableExtension, GetPartitionChargesCsv(entry.Partition, entry.PartitionCharges));
            WriteEntry(Path.Combine(pathPrefix, "correlations"), entry.Structure.Id + "_correlations_" + entry.Partition.Name + ExporterTableExtension, GetCorrelationsCsv(entry.Correlations));
            for (int i = 0; i < entry.Partition.Clusters.Count; i++)
            {
                var c = entry.Partition.Clusters[i];
                WriteEntry(Path.Combine(pathPrefix, "properties", c.Name), entry.Structure.Id + "_property_" + c.Name + ExporterTableExtension, GetAggregatesCsv(entry.Partition, entry.PartitionCharges, i));
            }

            //// JSON -- induced structure
            //pathPrefix = Path.Combine(basePrefix, "json");
            //foreach (var c in entry.PartitionCharges)
            //{
            //    var structure = c.Partition.InducedStructure;
            //    var src = JsonHelper.ToJsonString(structure);
            //    WriteEntry(pathPrefix, "structure.json", src);
            //}
            
            // MOL2
            pathPrefix = Path.Combine(basePrefix, "mol2");
            foreach (var c in entry.PartitionCharges)
            {
                var structure = c.Partition.InducedStructure;
                var chargesById = c.PartitionCharges.ToDictionary(g => g.Key.Id, g => g.Value);
                var src = structure.ToMol2String(chargeSelector: a =>
                {
                    double v;
                    if (chargesById.TryGetValue(a.Id, out v)) return v;
                    return 0.0;
                });
                WriteEntry(pathPrefix, entry.Structure.Id + "_" + c.RawCharges.Parameters.Id + "_" + entry.Partition.Name + ".mol2", src);
            }
        }

        public void ExportStructureData(ExporterStructureDataEntry entry)
        {
            // check if the entry is valid (in this case Structure == null)
            if (!entry.Summary.IsValid)
            {
                var invalidData = new ChargesServiceStructureData
                {
                    Summary = entry.Summary
                };
                var invalidJson = invalidData.ToJsonString();
                WriteEntry("json", entry.Summary.Id + ".json", invalidJson);
                if (JsonDataZip != null) JsonDataZip.AddEntry(entry.Summary.Id + ".json", invalidJson);
                return;
            }

            var bondTypes = entry.Charges.FirstOrDefault(c => c.Result.Parameters.Method != ChargeComputationMethod.Reference);

            var data = new ChargesServiceStructureData
            {
                Summary = entry.Summary,
                Charges = entry.Charges.ToDictionary(c => c.Result.Id, c => ConvertChargeComputationResult(c.Result)),
                Partitions = entry.Partitions.ToDictionary(p => p.Partition.Name, p => ConvertAtomPartition(p)),
                StructureJson = entry.Structure.ToJsonString(prettyPrint: false),
                BondTypes = bondTypes == null 
                    ? new Dictionary<int, int>()
                    : bondTypes.Result.Multiplicities.ToDictionary(t => t.Key.Id, t => t.Value)
            };

            var json = data.ToJsonString();
            WriteEntry("json", entry.Summary.Id + ".json", json);
            if (JsonDataZip != null) JsonDataZip.AddEntry(entry.Summary.Id + ".json", json);
        }

        public void ExportSummary(ChargesServiceComputationSummary result)
        {
            var json = result.ToJsonString();
            Zip.AddEntry(string.Format("summary_{0}.json", Created.ToString("yyyy-M-dd_HH-mm", System.Globalization.CultureInfo.InvariantCulture)), json);
            Zip.AddEntry("readme.txt", ReadMeText);
            File.WriteAllText(Path.Combine(OutputFolder, "summary.json"), json);
        }

        public void ExportParameterSets(IList<EemParameterSet> sets)
        {
            using (var s = new StringWriter())
            using (var w = XmlWriter.Create(s, new XmlWriterSettings() { Indent = true }))
            {
                new XElement("Sets", sets.Select(set => set.ToXml())).WriteTo(w);
                w.Flush();
                WriteEntry(null, "Sets.xml", s.ToString());
            }
        }

        public ChargeServiceExporter(ChargesService svc, string outputFolder, bool exportUncompressed, DateTime created)
        {
            this.Created = created;
            this.Svc = svc;
            this.ExportUncompressed = exportUncompressed;
            this.OutputFolder = outputFolder;
            Zip = new ZipUtils.ZipWrapper(Path.Combine(outputFolder, "result.zip"));
            if (!svc.IsStandalone) JsonDataZip = new ZipUtils.ZipWrapper(Path.Combine(outputFolder, "json_data.zip"));
        }

        public void Dispose()
        {
            if (Zip != null)
            {
                Zip.Dispose();
            }
            if (JsonDataZip != null)
            {
                JsonDataZip.Dispose();
            }
        }
    }
}
