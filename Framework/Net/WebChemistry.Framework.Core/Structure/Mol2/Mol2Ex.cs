
namespace WebChemistry.Framework.Core
{
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Extensions for Mol2 format.
    /// </summary>
    public static class Mol2Ex
    {
        const string Mol2Category = "Mol2Structure";

        public static readonly PropertyDescriptor<bool> Mol2ContainsChargesProperty
           = PropertyHelper.OfType<bool>("Mol2ContainsCharges", category: Mol2Category);

        public static readonly PropertyDescriptor<bool> IsMol2Property
           = PropertyHelper.OfType<bool>("IsMol2", category: Mol2Category);
        
        /// <summary>
        /// Checks if the stucture contains Mol2 charges.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool Mol2ContainsCharges(this IStructure structure)
        {
            return structure.GetProperty(Mol2ContainsChargesProperty, false);
        }

        /// <summary>
        /// Determines if this is a mol2 structure.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool IsMol2(this IStructure structure)
        {
            return structure.GetProperty(IsMol2Property, false);
        }


        /// <summary>
        /// MOL2 partial atom charge.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static double Mol2PartialCharge(this IAtom atom)
        {
            var a = atom as Mol2Atom;
            if (a != null) return a.PartialCharge;
            return 0.0;
        }
    }
}