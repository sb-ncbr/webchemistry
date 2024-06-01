namespace WebChemistry.Framework.Core
{
    using System;
    using System.Linq;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// Represents a molecular structure.
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Atoms = {Atoms.Count}, Bonds = {Bonds.Count}")]
    public class Structure : InteractivePropertyObject, IStructure
    {
        public static readonly Structure Empty = new Structure("empty", AtomCollection.Empty);

        private string id;

        /// <summary>
        /// The ID of the structure.
        /// </summary>
        public string Id { get { return id; } }

        /// <summary>
        /// Atoms.
        /// </summary>
        public IAtomCollection Atoms { get; private set; }

        /// <summary>
        /// Bonds.
        /// </summary>
        public IBondCollection Bonds { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="atoms"></param>
        /// <param name="bonds"></param>
        protected Structure(string id, IAtomCollection atoms, IBondCollection bonds)
        {
            Contract.Requires(id != null, "id");
            Contract.Requires(atoms != null, "atoms");
            Contract.Requires(bonds != null, "bonds");

            this.id = id;
            this.Atoms = atoms;
            this.Bonds = bonds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="atoms"></param>
        protected Structure(string id, IAtomCollection atoms)
        {
            Contract.Requires(id != null, "id");
            Contract.Requires(atoms != null, "atoms");

            this.id = id;
            this.Atoms = atoms;
            this.Bonds = BondCollection.Empty;
        }
        
        /// <summary>
        /// Returns a string in the form "Structure (id, atoms: count, bonds: count)"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Structure ({0}, atoms: {1}, bonds: {2})", this.id, this.Atoms.Count, this.Bonds.Count);
        }

        /// <summary>
        /// Clones the struture.
        /// Only creates a shallow copy of the properties.
        /// </summary>
        /// <returns></returns>
        public virtual IStructure Clone()
        {
            var ac = new Dictionary<int, IAtom>(Atoms.Count);
            for (int i = 0; i < Atoms.Count; i++)
            {
                var a = Atoms[i];
                ac.Add(a.Id, a.Clone());
            }
            var newAtoms = AtomCollection.Create(ac.Values);
            var bonds = Bonds.Select(b => Bond.Create(ac[b.A.Id], ac[b.B.Id], b.Type));
            var newBonds = BondCollection.Create(bonds);
            var ret = new Structure(Id, newAtoms, newBonds);
            ret.ClonePropertiesFrom(this);
            return ret;
        }

        /// <summary>
        /// Creates a new generic structure.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="atoms"></param>
        /// <returns></returns>
        public static IStructure Create(string id, IAtomCollection atoms)
        {
            return new Structure(id, atoms);
        }

        /// <summary>
        /// Creates a new generic structure.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="atoms"></param>
        /// <param name="bonds"></param>
        /// <returns></returns>
        public static IStructure Create(string id, IAtomCollection atoms, IBondCollection bonds)
        {
            return new Structure(id, atoms, bonds);
        }

        internal void SetBonds(IBondCollection bonds)
        {
            this.Bonds = bonds;
        }
    }
}