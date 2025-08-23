
namespace WebChemistry.Framework.Core
{
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core.MdlMol;

    /// <summary>
    /// Extensions for Mol format.
    /// </summary>
    public static class MolEx
    {
        /// <summary>
        /// Get the stereo parity.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static MdlMolStereoParity MolStereoParity(this IAtom atom)
        {
            var a = atom as MdlMolAtom;
            if (a != null) return a.StereoParity;
            return MdlMolStereoParity.EitherOrUnmarked;
        }
    }
}