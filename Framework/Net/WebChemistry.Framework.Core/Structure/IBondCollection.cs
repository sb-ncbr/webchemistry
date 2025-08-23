namespace WebChemistry.Framework.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// A collection of bonds.
    /// </summary>
    public interface IBondCollection : ICollection<IBond>
    {
        /// <summary>
        /// Return the i-th bond.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        IBond this[int i] { get; }

        /// <summary>
        /// Returns all bonds corresponding to a given atom.
        /// It always holds that Bond.A = a and Bond.B = other atom.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        IList<IBond> this[IAtom a] { get; }

        /// <summary>
        /// Returns a bond between bond atoms.
        /// Null if the bond does not exist.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        IBond this[IAtom a, IAtom b] { get; }

        /// <summary>
        /// Check whether the collection contains a bond between two atoms.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool Contains(IAtom a, IAtom b);
    }
}
