namespace WebChemistry.Framework.Core
{
    using WebChemistry.Framework.Core.PdbQt;

    /// <summary>
    /// PdbQt structure extensions.
    /// </summary>
    public static class PdbQtEx
    {
        /// <summary>
        /// PDBQt atom charge.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static double PdbQtCharge(this IAtom atom)
        {
            var a = atom as PdbQtAtom;
            if (a != null) return a.PartialCharge;
            return 0.0;
        }
    }
}