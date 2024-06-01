namespace WebChemistry.Charges.Service
{
    using System;
    using System.IO;
    using WebChemistry.Charges.Core;
    using WebChemistry.Charges.Service.DataModel;
    using WebChemistry.Framework.Core;

    static class ChargeServiceUtils
    {
        public static EemChargeComputationParameters ToComputationParameters(this ChargeSetConfig cfg, IStructure structure, double totalCharge, ParameterSetManager setManager)
        {
            return new EemChargeComputationParameters
            {
                Structure = structure,
                Method = cfg.Method,
                Precision = cfg.Precision,
                CutoffRadius = cfg.CutoffRadius,
                IgnoreWaters = cfg.IgnoreWaters,
                TotalCharge = totalCharge,
                CorrectCutoffTotalCharge = cfg.CorrectCutoffTotalCharge,
                Set = setManager.GetByName(cfg.Name)
            };
        }

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
    }
}
