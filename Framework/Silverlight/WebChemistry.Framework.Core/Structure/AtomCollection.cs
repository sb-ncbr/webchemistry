namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// A read-only collection of atoms.
    /// </summary>
    public sealed class AtomCollection : ReadOnlyCollection<IAtom>, IAtomCollection
    {
        /// <summary>
        /// An empty collection of atoms.
        /// </summary>
        public static readonly IAtomCollection Empty = new AtomCollection(new IAtom[0]);

        Dictionary<int, IAtom> atomDict;
        
        /// <summary>
        /// Get atoms by IAtom.Id
        /// </summary>
        /// <param name="id">IAtom.Id of the atom</param>
        /// <returns>Atom with IAtom.Id <paramref name="id"/>.</returns>
        public IAtom GetById(int id)
        {
            IAtom atom;
            if (atomDict.TryGetValue(id, out atom)) return atom;

            throw new ArgumentException(string.Format("No atom with id = {0} exists.", id));
        }

        /// <summary>
        /// Try to retrieve an atom from the collection.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="atom"></param>
        /// <returns></returns>
        public bool TryGetAtom(int id, out IAtom atom)
        {
            return atomDict.TryGetValue(id, out atom);
        }

        /// <summary>
        /// Checks if there is an atom with the same Id in the collection.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public new bool Contains(IAtom atom)
        {
            return atomDict.ContainsKey(atom.Id);
        }

        /// <summary>
        /// Creates a collection of atoms. Only adds atoms with unique Id.
        /// Mostly useful for motive finder.
        /// </summary>
        /// <param name="atoms"></param>
        public static AtomCollection FromUniqueAtoms(IEnumerable<IAtom> atoms)
        {
            if (atoms == null) throw new NullReferenceException("atoms");

            List<IAtom> uniqueAtoms = new List<IAtom>();
            var atomDict = new Dictionary<int, IAtom>();

            foreach (var atom in atoms)
            {
                if (atomDict.ContainsKey(atom.Id)) continue;
                else
                {
                    uniqueAtoms.Add(atom);
                    atomDict.Add(atom.Id, atom);
                }
            }

            return new AtomCollection(uniqueAtoms, atomDict);
        }

        private AtomCollection(List<IAtom> atoms, Dictionary<int, IAtom> atomDict)
            : base(atoms)
        {
            this.atomDict = atomDict;
        }

        /// <summary>
        /// Creates a collection of atoms.
        /// </summary>
        /// <exception cref="WebChemistry.Framework.Core.NonUniqueAtomIdException">Thrown if trying to add 2 atoms with the same id.</exception>
        /// <param name="atoms"></param>
        private AtomCollection(IEnumerable<IAtom> atoms) 
            : base(atoms.AsList())
        {
            atomDict = new Dictionary<int, IAtom>(this.Count);

            foreach (var atom in this)
            {
                if (this.atomDict.ContainsKey(atom.Id))
                {
                    throw new NonUniqueAtomIdException(this.atomDict[atom.Id], atom);
                }
                else
                {
                    this.atomDict.Add(atom.Id, atom);
                }
            }
        }

        /// <summary>
        /// Creates a new atom collection.
        /// </summary>
        /// <param name="atoms"></param>
        /// <returns></returns>
        public static IAtomCollection Create(IEnumerable<IAtom> atoms)
        {
            Contract.Requires(atoms != null);
            return new AtomCollection(atoms);
        }
    }
}