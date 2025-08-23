namespace WebChemistry.Framework.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// A collection of atoms.
    /// </summary>
    public interface IAtomCollection : ICollection<IAtom>
    {
        /// <summary>
        /// Returns an atom with a given id.
        /// Throws if the atom does not exist.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IAtom GetById(int id);

        /// <summary>
        /// Try to retrieve an atom by id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="atom"></param>
        /// <returns></returns>
        
        bool TryGetAtom(int id, out IAtom atom);

        /// <summary>
        /// Return the i-th atom.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        IAtom this[int i] { get; }
    }
}
