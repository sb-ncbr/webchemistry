namespace WebChemistry.Charges.Service.Analyzer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WebChemistry.Charges.Core;
    using WebChemistry.Charges.Service.DataModel;
    using WebChemistry.Framework.Core;

    class AnalyzerUtils
    {
        public static bool IsReferenceChargesFilename(string filename)
        {
            var ext = new FileInfo(filename).Extension;
            if (ext.EndsWith(".wprop", StringComparison.OrdinalIgnoreCase)
                || ext.EndsWith("chrg", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public static string GetRefChargesName(string filename)
        {
            var withoutExt = Path.GetFileNameWithoutExtension(filename);
            if (withoutExt.EndsWith("_ref", StringComparison.OrdinalIgnoreCase)) return withoutExt.Substring(0, withoutExt.Length - 4);
            return withoutExt;
        }

        public static ChargeAnalyzerParameterSetEntry ConvertSetToEntry(EemParameterSet set)
        {
            return new ChargeAnalyzerParameterSetEntry
            {
                Name = set.Name,
                Properties = set.Properties.Select(p => new[] { p.Item1, p.Item2 }).ToArray(),
                AvailableAtoms = set.ParameterGroups.SelectMany(g => g.Parameters.Keys).Distinct().Select(e => e.ToString()).OrderBy(e => e).ToArray(),
                Xml = set.ToXml().ToString()
            };
        }

        public static int EstimateTotalCharge(IStructure structure)
        {            
            return 0;
        }

        public static ChargeAnalyzerStructureCategory[] GetStructureCategories(IStructure structure)
        {
            return new ChargeAnalyzerStructureCategory[0];
        }
    }
}
