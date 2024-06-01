namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    /// A read-only collection of bonds.
    /// </summary>
    public sealed class BondCollection : ReadOnlyCollection<IBond>, IBondCollection
    {
        /// <summary>
        /// An empty collection of bonds.
        /// </summary>
        public static readonly IBondCollection Empty = new BondCollection(new IBond[0]);

        static ReadOnlyCollection<IBond> emptyList = new ReadOnlyCollection<IBond>(new IBond[0]);

        //Dictionary<BondIdentifier, IBond> bondsById;        

        Dictionary<int, ReadOnlyCollection<IBond>> bondsByAtom;
        
        /// <summary>
        /// Get bonds with <paramref name="a"/> as a starting point.
        /// For each bond, it always holds that IBond.A = a and IBond.B ~ the other atom.
        /// </summary>
        /// <param name="a"></param>
        /// <returns>All bonds that contain <paramref name="a"/>. For each of the bonds, it hold IBond.A = <paramref name="a"/>.</returns>
        public IList<IBond> this[IAtom a]
        {
            get
            {
                ReadOnlyCollection<IBond> bonds;
                if (bondsByAtom.TryGetValue(a.Id, out bonds)) return bonds;
                return emptyList;
            }
        }
        
        /// <summary>
        /// Gets the bond between atoms <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Bond or null, if there is no bond.</returns>        
        public IBond this[IAtom a, IAtom b]
        {
            get
            {
                ReadOnlyCollection<IBond> bonds;
                if (bondsByAtom.TryGetValue(a.Id, out bonds))
                {
                    for (int i = 0; i < bonds.Count; i++)
                    {
                        if (b.Id == bonds[i].B.Id) return bonds[i];
                    }
                }

                return null;
            }
        }
   
        /// <summary>
        /// Checks if the collection contains a bond.
        /// </summary>
        /// <param name="bond"></param>
        /// <returns></returns>
        new public bool Contains(IBond bond)
        {
            //return bondsById.ContainsKey(bond.Id);
            return this[bond.A, bond.B] != null;
        }

        /// <summary>
        /// Checks if the collection contains a bond between 2 atoms.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool Contains(IAtom a, IAtom b)
        {
            //return bondsById.ContainsKey(new BondIdentifier(a, b));
            return this[a, b] != null;
        }
 
        /// <summary>
        /// Creates a collection of bonds.
        /// </summary>
        /// <param name="bonds"></param>
        private BondCollection(IEnumerable<IBond> bonds) 
            : base(bonds.AsList())
        {
            if (bonds == null) throw new NullReferenceException("bonds");

            int count = this.Count;
            //bondsById = new Dictionary<BondIdentifier, IBond>(count);
            var bondsByAtom = new Dictionary<int, List<IBond>>(count + 1);
            HashSet<int> usedIds = new HashSet<int>();
            foreach (var bond in bonds)
            {
               // bondsById.Add(bond.Id, bond);

                List<IBond> tb;
                int id = bond.A.Id;
                if (bondsByAtom.TryGetValue(id, out tb)) tb.Add(bond);
                else bondsByAtom.Add(id, new List<IBond>() { bond });

                id = bond.B.Id;
                if (bondsByAtom.TryGetValue(id, out tb)) tb.Add(Bond.Create(bond.B, bond.A, bond.Type));
                else bondsByAtom.Add(id, new List<IBond>() { Bond.Create(bond.B, bond.A, bond.Type) });


                //if (usedIds.Add(bond.A.Id)) bondsByAtom[bond.A.Id] = new List<IBond>() { bond };
                //else bondsByAtom[bond.A.Id].Add(bond);

                //if (usedIds.Add(bond.B.Id)) bondsByAtom[bond.B.Id] = new List<IBond>() { Bond.Create(bond.B, bond.A, bond.Type) };
                //else bondsByAtom[bond.B.Id].Add(Bond.Create(bond.B, bond.A, bond.Type));
            }

            this.bondsByAtom = bondsByAtom.ToDictionary(b => b.Key, b => new ReadOnlyCollection<IBond>(b.Value));
        }

        /// <summary>
        /// Create a new bond collection.
        /// </summary>
        /// <param name="bonds"></param>
        /// <returns></returns>
        public static IBondCollection Create(IEnumerable<IBond> bonds)
        {
            Contract.Requires(bonds != null);
            return new BondCollection(bonds);
        }
    }
}