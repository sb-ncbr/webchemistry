namespace WebChemistry.Queries.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// A motive represented as a set of atoms.
    /// </summary>
    public class Motive : IEquatable<Motive>, IComparable<Motive>, IComparable
    {
        /// <summary>
        /// Context of the motive.
        /// </summary>
        public MotiveContext Context { get; private set; }

        /// <summary>
        /// Atoms of the motive stored in a hash-trie.
        /// Each atom must have unique hash code!!
        /// </summary>
        public HashTrie<IAtom> Atoms { get; private set; }

        Vector3D? center;
        /// <summary>
        /// Geometrical center of the motive.
        /// Cached.
        /// </summary>
        public Vector3D Center
        {
            get
            {
                if (center == null)
                {
                    var tc = new Vector3D();
                    int count = 0;
                    foreach (var atom in Atoms)
                    {
                        tc += atom.InvariantPosition;
                        count++;
                    }
                    center = (1.0 / count) * tc;
                }
                return center.Value;
            }
        }

        double? radius;
        /// <summary>
        /// Bounding sphere radius.
        /// Cached.
        /// </summary>
        public double Radius
        {
            get
            {
                if (radius == null)
                {
                    var c = Center;
                    radius = Atoms.Max(a => a.InvariantPosition.DistanceTo(c));
                }
                return radius.Value;
            }
        }

        string signature;
        /// <summary>
        /// Signature of the motive (ordered residue names with counts)
        /// </summary>
        public string Signature
        {
            get
            {
                signature = signature ?? string.Join("-", Atoms.Flatten()
                    .GroupBy(a => a.ResidueIdentifier())
                    .Select(g => g.First().PdbResidueName())
                    .GroupBy(n => n)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderBy(r => r.Name)
                    .Select(r => r.Count > 1 ? r.Count + r.Name : r.Name));                
                return signature;
            }
        }

        int? name;
        /// <summary>
        /// The name (atom id of some atom in the motive). can be null.
        /// </summary>
        public int? Name
        {
            get { return name; }
        }

        /// <summary>
        /// Get the name.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int? GetName(int? a, int? b)
        {
            if (a.HasValue && b.HasValue) return Math.Min(a.Value, b.Value);
            else if (a.HasValue) return a;
            else return b;
        }

        /// <summary>
        /// Converts the motive to a structure.
        /// Structure id is "parent.Id_firstMotiveAtomId".
        /// </summary>
        /// <param name="id">Can be empty -- then motive name is used.</param>
        /// <param name="parent"></param>
        /// <param name="addBonds"></param>
        /// <returns></returns>
        public IStructure ToStructure(string id, bool addBonds, bool asPdb)
        {
            return Context.ToStructure(this, id, addBonds, asPdb);
        }

        /// <summary>
        /// Creates a motive from a single atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static Motive FromAtom(IAtom atom, MotiveContext context)
        {
            return new Motive(HashTrie.Singleton(atom), null, context);
        }

        /// <summary>
        /// Creates a motive from a single residue.
        /// </summary>
        /// <param name="residue"></param>
        /// <returns></returns>
        public static Motive FromResidue(ResidueWrapper residue, MotiveContext context)
        {
            return new Motive(residue.AtomSet, null, context);
        }

        /// <summary>
        /// Creates a motive from a sequence of atoms.
        /// </summary>
        /// <param name="atoms"></param>
        /// <returns></returns>
        public static Motive FromAtoms(IEnumerable<IAtom> atoms, int? name, MotiveContext context)
        {
            return new Motive(HashTrie.Create(atoms), name, context);
        }

        /// <summary>
        /// Creates a motive from a sequence of residues.
        /// </summary>
        /// <param name="residues"></param>
        /// <returns></returns>
        public static Motive FromResidues(IEnumerable<ResidueWrapper> residues, int? name, MotiveContext context)
        {
            //var set = HashTrie.Empty<IAtom>();
            var atomList = new List<IAtom>();
            foreach (var r in residues) //set = HashTrie.Union(set, r.AtomSet);
            {
                atomList.AddRange(r.Residue.Atoms);
            }
            return new Motive(HashTrie.Create(atomList), name, context);
        }

        /// <summary>
        /// Merges two motives.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Motive Merge(Motive a, Motive b, MotiveContext context)
        {
            //return new Motive(HashTrie.Union(a.Atoms, b.Atoms), GetName(a.name, b.name), context);
            var list = new List<IAtom>(a.Atoms.Count + b.Atoms.Count);
            a.Atoms.VisitLeaves(list.Add);
            b.Atoms.VisitLeaves(list.Add);
            return new Motive(HashTrie.Create(list), GetName(a.name, b.name), context);
        }

        /// <summary>
        /// Create a named motive.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Motive Named(Motive m)
        {
            var id = int.MaxValue;
            m.Atoms.VisitLeaves(atom =>
            {
                if (atom.Id < id) id = atom.Id;
            });
            return new Motive(m.Atoms, id, m.Context);
        }

        /// <summary>
        /// Computes the distance of the closest atoms from both motives.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Distance(Motive a, Motive b)
        {            
            double min = double.MaxValue;

            var xs = a.Atoms.Flatten();
            var ys = b.Atoms.Flatten();

            for (int i = 0; i < xs.Count; i++)
            {
                var x = xs[i];
                for (int j = 0; j < ys.Count; j++)
                {
                    var y = ys[j];
                    var d = x.InvariantPosition.DistanceTo(y.InvariantPosition);
                    if (d < min) min = d;
                }
            }

            return min;
        }

        /// <summary>
        /// Check if an atom is a member of a motive.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public bool IsMember(IAtom atom)
        {
            return Atoms.Contains(atom);
        }

        /// <summary>
        /// Updates the context, used by the "Find" query.
        /// </summary>
        /// <param name="newCtx"></param>
        public void UpdateContext(MotiveContext newCtx)
        {
            this.Context = newCtx;
        }

        /// <summary>
        /// Check if a motive is connected to an atom. The atom musn't be a member of the motive.
        /// </summary>
        /// <param name="atom"></param>
        /// <param name="motive"></param>
        /// <param name="structure"></param>
        /// <param name="bondType"></param>
        /// <returns></returns>
        public static bool AreConnected(IAtom atom, Motive motive, IStructure structure, BondType bondType = BondType.Unknown)
        {
            var bonds = structure.Bonds[atom];
            for (int i = 0; i < bonds.Count; i++)
            {
                var bond = bonds[i];
                if (bond.B.Id == atom.Id) continue;
                if ((bondType == BondType.Unknown || bond.Type == bondType) && motive.Atoms.Contains(bond.B)) return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if there is a bond that connects atoms in both motives.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="structure"></param>
        /// <param name="exclusive"></param>
        /// <returns></returns>
        public static bool AreConnected(Motive a, Motive b, BondType bondType = BondType.Unknown, bool exclusive = false)
        {
            var intersected = HashTrie.AreIntersected(a.Atoms, b.Atoms);
            if (intersected) return !exclusive;

            var structure = a.Context.Structure;

            Motive pivot, other;
            if (a.Atoms.Count < b.Atoms.Count)
            {
                pivot = a;
                other = b;
            }
            else
            {
                pivot = b;
                other = a;
            }

            var bonds = structure.Bonds;
            var pivotAtomList = pivot.Atoms.Flatten();

            //var pivotAtoms = pivot.Atoms;
            var otherAtoms = other.Atoms;


            if (bondType == BondType.Unknown)
            {
                for (int j = 0; j < pivotAtomList.Count; j++)
                {
                    var atom = pivotAtomList[j];
                    var atomBonds = structure.Bonds[atom];
                    for (int i = 0; i < atomBonds.Count; i++)
                    {
                        var bond = atomBonds[i];
                        if (otherAtoms.Contains(bond.B) /*&& !pivot.Atoms.Contains(bond.B)*/) return true;
                    }
                }
            }
            else
            {
                for (int j = 0; j < pivotAtomList.Count; j++)
                {
                    var atom = pivotAtomList[j];
                    var atomBonds = structure.Bonds[atom];
                    for (int i = 0; i < atomBonds.Count; i++)
                    {
                        var bond = atomBonds[i];
                        var btom = bond.B;
                        if (bond.Type == bondType && otherAtoms.Contains(btom)/* && !pivotAtoms.Contains(btom)*/) return true;
                    }
                }
            }

            return false;
        }


        /////// <summary>
        /////// Determines if two motives share at least 1 atom.
        /////// </summary>
        /////// <param name="a"></param>
        /////// <param name="b"></param>
        /////// <returns></returns>
        ////public static bool AreIntersected(Motive a, Motive b)
        ////{
        ////    return HashTrie.AreIntersected(a.Atoms, b.Atoms);
        ////}

        /// <summary>
        /// Checks whether two motives are near.
        /// </summary>
        /// <param name="maxDistance"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool AreNear(double maxDistance, Motive a, Motive b)
        {
            var cd = a.Center.DistanceTo(b.Center);
            if (cd - a.Radius - b.Radius <= maxDistance) return Distance(a, b) < maxDistance;
            return false;
        }


        /// <summary>
        /// Two motives are equal if they share the same residues and atoms.
        /// Do equality based on atoms because of the residue/atom overlap?
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Motive other)
        {
            return this.Atoms.Equals(other.Atoms);
        }

        /// <summary>
        /// Hash code composed from the residue and atom set hashes.
        /// Cached.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Atoms.GetHashCode();
        }

        /// <summary>
        /// Two motives are equal if they share the same residues and atoms.
        /// Do equality based on atoms because of the residue/atom overlap?
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as Motive;
            if (other != null) return Equals(other);
            return false;
        }

        /// <summary>
        /// Compares the motives based on their hashcode.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            var other = obj as Motive;
            if (other != null) return CompareTo(other);
            return 1;
        }

        /// <summary>
        /// Compares the motives based on their hashcode.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Motive other)
        {
            return this.GetHashCode().CompareTo(other.GetHashCode());
        }

        /// <summary>
        /// Create a new motive from a hash trie set.
        /// </summary>
        /// <param name="atoms"></param>
        /// <param name="name"></param>
        /// <param name="context"></param>
        public Motive(HashTrie<IAtom> atoms, int? name, MotiveContext context)
        {
            this.Atoms = atoms;
            this.Context = context;
            this.name = name;
        }
    }
}
