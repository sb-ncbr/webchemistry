using System;
using System.Linq;
using WebChemistry.Framework.Core;
using WebChemistry.SiteBinder.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.SiteBinder.Silverlight.ViewModel;
using WebChemistry.Silverlight.Common.DataModel;

namespace WebChemistry.SiteBinder.Silverlight.DataModel
{
    public class Result
    {
        public class Entry
        {
            public StructureWrap Structure { get; set; }
            public string Id { get; set; }
            public double Rmsd { get; set; }
            public int MatchedCount { get; set; }
            public int SelectedCount { get; set; }

            // String because it is used by pagedcollectionview which calls ToString anyway
            public string MatchedCountString { get; set; }
            // String because it is used by pagedcollectionview which calls ToString anyway
            public string SigmaGroupString { get; set; }
            // String because it is used by pagedcollectionview which calls ToString anyway
            public string ClusterGroupString { get; set; }

            public int SigmaGroup { get; set; }
            public int ClusterGroup { get; set; }

            public string FormattedRmsd { get; set; }
        }
        
        public MultipleMatching<IAtom> Matching { get; private set; }
        public StructureWrap Pivot { get; private set; }
        public IAtom[] PivotSelection { get; private set; }
        public TimeSpan Timing { get; private set; }

        public IStructure AverageStructure { get; private set; }
        
        public ObservableCollection<Entry> Structures { get; set; }
        
        public string FormattedRmsd { get; private set; }        
        public string FormattedSigma { get; private set; }
        public string FormattedTiming { get; private set; }

        public string PivotTypeString { get; private set; }
        public string PivotTypeTooltip { get; private set; }
        public string PairwiseMethodString { get; private set; }

        public bool FindPairwiseMatrix { get; private set; }
        public int ClusterCount { get; private set; }

        static string GetSigmaGroupString(int g)
        {
            if (g == MatchingStatistics.InvalidSigmaGroup) return "No sigma group (zero matched atoms)";
            if (g == 0) return "Difference from RMSD < σ ";
            else if (g == 1) return "Difference from RMSD < 2σ";
            else if (g == 2) return "Difference from RMSD < 3σ" ;
            else return "Difference from RMSD > 3σ ";
        }

        static string GetMatchCountString(int count)
        {
            if (count == 1) return "1 atom";
            return count.ToString() + " atoms";
        }

        ListExporter GetExporter(char separator)
        {
            var exporter = Structures.Where(s => s.Structure.Structure.IsSelected).OrderBy(e => e.Rmsd).ThenBy(e => e.SigmaGroup).ThenBy(e => e.ClusterGroup)
                .ToArray()
                .GetExporter(separator: separator.ToString(), xmlRootName: "Structures", xmlElementName: "Entry")
                .AddExportableColumn(e => e.Id, ColumnType.String, "Id")
                .AddExportableColumn(e => e.FormattedRmsd, ColumnType.Number, "RMSD")
                .AddExportableColumn(e => e.SigmaGroup, ColumnType.Number, "SigmaGroup")
                .AddExportableColumn(e => e.ClusterGroup, ColumnType.Number, "ClusterGroup")
                .AddExportableColumn(e => e.MatchedCount, ColumnType.Number, "MatchedCount")
                .AddExportableColumn(e => e.SelectedCount, ColumnType.Number, "SelectedAtomCount")
                .AddExportableColumn(e => e.Structure.Structure.Atoms.Count, ColumnType.Number, "TotalAtomCount")
                .AddExportableColumn(e => e.Structure.ResidueString, ColumnType.String, "Residues");

            exporter = ServiceLocator.Current.GetInstance<Session>().Descriptors.AddColumns(exporter, e => e.Structure);

            return exporter;

        }

        public string ToCsvListString(char separator)
        {
            return GetExporter(separator).ToCsvString();
        }

        public string ToCsvMatrixString(char separator)
        {
            if (Matching.PairwiseMatrix == null) return null;

            string sepString = separator.ToString();

            StringBuilder sb = new StringBuilder();
            
            foreach (var m in Matching.MatchingsList)
            {
                sb.Append(sepString);
                sb.Append(m.Other.Token);
            }
            sb.Append(Environment.NewLine);

            var matrix = Matching.PairwiseMatrix;
            int count = matrix.Length;
            for (int i = 0; i < count; i++)
            {
                sb.Append(Matching.MatchingsList[i].Other.Token);
                var row = matrix[i];
                for (int j = 0; j < count; j++)
                {
                    sb.Append(sepString);
                    sb.Append(row[j].ToStringInvariant("0.00"));
                }
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }
        
        ////public XElement ExportPairing(Session session)
        ////{
        ////    var ret = new XElement("Pairing", 
        ////        new XAttribute("Pivot", Pivot.Structure.Id),
        ////        new XAttribute("PairwiseMethod", Matching.PairwiseType),
        ////        new XAttribute("MultipleMethod", Matching.PivotType));
            
        ////    var structures = Structures.ToDictionary(s => s.Structure.Structure.Id, StringComparer.Ordinal);

        ////    var pivot = Pivot.Structure;
        ////    var pivotCount = pivot.Atoms.Count;
        ////    foreach (var s in Matching.MatchingsList)
        ////    {
        ////        if (s.Other.Token == Pivot.Structure.Id) continue;

        ////        StructureWrap w;
        ////        if (!session.StructureMap.TryGetValue(s.Other.Token, out w)) continue;


                
        ////        var other = structures[s.Other.Token].Structure.Structure;
        ////        var otherCount = other.Atoms.Count;

        ////        List<IAtom> pivotSequence = new List<IAtom>(Math.Max(pivotCount, otherCount)), otherSequence = new List<IAtom>(Math.Max(pivotCount, otherCount));
        ////        var pairing = Enumerable.Zip(s.PivotOrdering, s.OtherOrdering, (l, r) => new { P = l, O = r }).ToDictionary(p => p.P.Vertex, p => p.O.Vertex);

                
        ////        var len = Math.Min(pivotCount, otherCount);

        ////        var pairedSet = new HashSet<IAtom>();

        ////        for (int i = 0; i < pivotCount; i++)
        ////        {
        ////            var a = pivot.Atoms[i];
        ////            IAtom paired;

        ////            pivotSequence.Add(a);
        ////            if (pairing.TryGetValue(a, out paired))
        ////            {
        ////                otherSequence.Add(paired);
        ////                pairedSet.Add(paired);
        ////            }
        ////            else otherSequence.Add(null);
        ////        }

        ////        other.Atoms.Where(a => !pairedSet.Contains(a))
        ////            .ForEach(a =>
        ////            {
        ////                pivotSequence.Add(null);
        ////                otherSequence.Add(a);
        ////            });

        ////        Func<List<IAtom>, string> joined = xs => string.Join(",", xs.Select(x => x == null ? "-" : x.Id.ToString()));

        ////        ret.Add(new XElement("Entry",
        ////            new XAttribute("Id", w.Structure.Id),
        ////            new XElement("PivotSequence", joined(pivotSequence)),
        ////            new XElement("OtherSequence", joined(otherSequence))));
        ////    }

        ////    return ret;
        ////}


        /// <summary>
        /// (Identifiers, Names)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public Tuple<string, string> ExportPairingAsCsv(Session session, char separator)
        {
            //var structures = Structures.ToDictionary(s => s.Structure.Structure.Id, StringComparer.Ordinal);

            StringBuilder identifiers = new StringBuilder();
            StringBuilder names = new StringBuilder();

            //ret.AppendLine( "\"\"" + separator + string.Join(separator.ToString(), Pivot.Structure.Atoms.Select(a => a.PdbName())));
            //ret.AppendLine("\"Pivot (" + Pivot.Structure.Id + ")\"" + separator + string.Join(separator.ToString(), Pivot.Structure.Atoms.Select(a => a.Id.ToString())));

            var header = "\"Pivot (" + Pivot.Structure.Id + ")\"" + separator + string.Join(separator.ToString(), PivotSelection.Select(a => "\"" + a.PdbName() + "\""));
            names.AppendLine(header);
            identifiers.AppendLine(header);
            identifiers.AppendLine("\"Pivot (" + Pivot.Structure.Id + ")\"" + separator + string.Join(separator.ToString(), PivotSelection.Select(a => a.Id.ToString())));

            var matchings = Matching.MatchingsList.ToDictionary(m => m.Other.Token, StringComparer.Ordinal);
            var rvm = ServiceLocator.Current.GetInstance<ResultViewModel>();

            //var pivot = Pivot.Structure;
            var pivotCount = PivotSelection.Length;
            foreach (Entry entry in rvm.ResultView)
            {
                if (entry.Structure.Structure.Id == Pivot.Structure.Id) continue;
                StructureWrap wrap;
                if (!session.StructureMap.TryGetValue(entry.Structure.Structure.Id, out wrap) || !wrap.Structure.IsSelected) continue;

                var other = entry.Structure.Structure;
                var otherCount = other.Atoms.Count;

                var matching = matchings[other.Id];

                var pairing = Enumerable.Zip(matching.PivotOrdering, matching.OtherOrdering, (l, r) => new { P = l, O = r }).ToDictionary(p => p.P.Vertex, p => p.O.Vertex);
                
                var len = Math.Min(pivotCount, otherCount);

                identifiers.Append("\"" + other.Id + "\"");
                names.Append("\"" + other.Id + "\"");

                for (int i = 0; i < pivotCount; i++)
                {
                    var a = PivotSelection[i];
                    IAtom paired;
                    identifiers.Append(separator);
                    names.Append(separator);

                    if (pairing.TryGetValue(a, out paired))
                    {
                        names.Append("\"" + paired.PdbName() + "\"");
                        identifiers.Append(paired.Id.ToString());
                    }
                    else
                    {
                        names.Append("-");
                        identifiers.Append("-");
                    }
                }

                identifiers.Append(Environment.NewLine);
                names.Append(Environment.NewLine);
            }

            return Tuple.Create(identifiers.ToString(), names.ToString());
        }


        public void ExportPairedStructures(Session session, ZipOutputStream zip)
        {
            const string folderName = @"paired\";
            var writer = new StreamWriter(zip);

            var structures = Structures.ToDictionary(s => s.Structure.Structure.Id, StringComparer.Ordinal);
            
            var pivotCount = PivotSelection.Length;

            foreach (var s in Matching.MatchingsList)
            {
                if (s.Other.Token == Pivot.Structure.Id) continue;

                StructureWrap wrap;
                if (!session.StructureMap.TryGetValue(s.Other.Token, out wrap) || !wrap.Structure.IsSelected) continue;

                var other = structures[s.Other.Token].Structure.Structure;

                var orderedPivot = s.PivotOrdering.OrderBy(v => v.Vertex.Id).Select(v => v.Vertex).ToArray();
                var pairing = Enumerable.Zip(s.PivotOrdering, s.OtherOrdering, (l, r) => new { P = l, O = r }).ToDictionary(p => p.P.Vertex, p => p.O.Vertex);

                var current = Pivot.Structure.InducedSubstructure(Pivot.Structure.Id, orderedPivot, cloneAtoms: false);
                zip.PutNextEntry(new ZipEntry(folderName + other.Id + "_pivot.pdb"));
                current.WritePdb(writer, false);
                writer.Flush();
                zip.CloseEntry();

                current = other.InducedSubstructure(other.Id, orderedPivot.Select(v => pairing[v]), cloneAtoms: false);
                zip.PutNextEntry(new ZipEntry(folderName + other.Id + ".pdb"));
                current.WritePdb(writer, false);
                writer.Flush();
                zip.CloseEntry();
            }
        }

        ////public string ExportPairingAsCsv(XElement data, char separator)
        ////{
        ////    return string.Join(Environment.NewLine,
        ////        new string[] { 
        ////            "\"\"" + separator + string.Join(separator.ToString(), Pivot.Structure.Atoms.Select(a => a.PdbName())),
        ////            "\"Pivot\"" + separator + string.Join(separator.ToString(), Pivot.Structure.Atoms.Select(a => a.Id.ToString())) }
        ////        .Concat(
        ////            data.Elements()
        ////            .Select(e => "\"" + e.Attribute("Id").Value + "\"" + separator + e.Element("OtherSequence").Value.Replace(',', separator))));
        ////}

        public static Result Create(MultipleMatching<IAtom> matching, TimeSpan timing, Session session)
        {
            var structures = matching.MatchingsList
                .Select(m => new Entry
                {
                    Structure = session.StructureMap[m.Other.Token],
                    Rmsd = matching.Statistics.RmsdToPivot[m.Other.Token],
                    MatchedCount = m.Size,
                    Id = m.Other.Token,
                    SelectedCount = session.StructureMap[m.Other.Token].SelectedCount,
                    MatchedCountString = GetMatchCountString(m.Size),
                    SigmaGroupString = GetSigmaGroupString(matching.Statistics.SigmaGroups[m.Other.Token]),
                    SigmaGroup = matching.Statistics.SigmaGroups[m.Other.Token],
                    ClusterGroup = matching.Statistics.ClusterGroups[m.Other.Token],
                    ClusterGroupString = "Cluster " + matching.Statistics.ClusterGroups[m.Other.Token],
                    FormattedRmsd = matching.Statistics.RmsdToPivot[m.Other.Token].ToStringInvariant("0.00")
                })
                .ToList();

            var pivot = session.StructureMap[matching.Pivot.Token];
            var average = pivot.Structure.InducedSubstructure("average", matching.AverageVertices, cloneAtoms: true);

            var avgPositions = matching.AverageVertices.Zip(matching.FinalAverage, (a, v) => new { A = a, V = v }).ToDictionary(x => x.A, x => x.V);
            average.TransformAtomPositions(a => avgPositions[a]);
            
            return new Result
                {
                    Structures = new ObservableCollection<Entry>(structures),
                    Matching = matching,
                    Pivot = session.StructureMap[matching.Pivot.Token],
                    PivotSelection = 
                        session.IgnoreHydrogens 
                        ? session.StructureMap[matching.Pivot.Token].Structure.Atoms.Where(a => a.IsSelected && a.ElementSymbol != ElementSymbols.H).ToArray()
                        : session.StructureMap[matching.Pivot.Token].Structure.Atoms.Where(a => a.IsSelected).ToArray(),
                    Timing = timing,
                    AverageStructure = average,
                    FormattedRmsd = matching.Statistics.AverageRmsd.ToStringInvariant("0.00"),
                    FormattedSigma = matching.Statistics.Sigma.ToStringInvariant("0.00"),
                    FormattedTiming = timing.TotalSeconds.ToStringInvariant("0.00"),
                    PivotTypeString = matching.PivotType == PivotType.Average ? "Average (" + matching.FinalAverage.Length + ", " + matching.Pivot.Token + ")" : matching.Pivot.Token,
                    PivotTypeTooltip = matching.PivotType == PivotType.Average ? "Constructed from the given number of atoms and the structure for initial pairing." : "",
                    PairwiseMethodString = matching.PairwiseType.ToString(),
                    FindPairwiseMatrix = matching.PairwiseMatrix != null,
                    ClusterCount = matching.Statistics.NumClusters
                };
        }
    }
}
