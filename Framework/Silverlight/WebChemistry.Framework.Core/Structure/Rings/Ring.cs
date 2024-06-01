namespace WebChemistry.Framework.Core
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System;
    using System.Text;
    
    public class Ring : IEquatable<Ring>
    {

        static int GetMinimalRotation<T>(T[] elements, Func<T, string> getter)
        {
            // adapted from http://en.wikipedia.org/wiki/Lexicographically_minimal_string_rotation

            var comp = StringComparer.Ordinal;

            var f = new int[elements.Length * 2];
            int k = 0, i, len = elements.Length, cc;
            string u, v;
            for (i = 0; i < f.Length; i++) f[i] = -1;

            for (int j = 1; j < f.Length; j++)
            {
                i = f[j - k - 1];
                while (i != -1)
                {
                    u = getter(elements[j % len]); v = getter(elements[(k + i + 1) % len]);
                    cc = comp.Compare(u, v);
                    if (cc == 0) break;
                    if (cc < 0) k = j - i - 1;
                    i = f[i];
                }

                if (i == -1)
                {
                    u = getter(elements[j % len]); v = getter(elements[(k + i + 1) % len]);
                    cc = comp.Compare(u, v);
                    if (cc != 0)
                    {
                        if (cc < 0) k = j;
                        f[j - k] = -1;
                    }
                    else f[j - k] = i + 1;
                }
                else f[j - k] = i + 1;
            }

            return k;
        }

        static string BuildFinderprint<T>(T[] elements, int offset, Func<T, string> getter)
        {
            int len = elements.Length, i;
            StringBuilder ret = new StringBuilder(3 * len);
            for (i = 0; i < elements.Length - 1; i++)
            {
                ret.Append(getter(elements[(i + offset) % len]));
                ret.Append('-');
            }
            ret.Append(getter(elements[(i + offset) % len]));
            return ret.ToString();
        }

        /// <summary>
        /// Computes a ring fingerprint from a collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public static string GetFingerprint<T>(T[] elements, Func<T, string> getter)
        {
            int len = elements.Length;
            T[] reversed = new T[len];

            for (int i = 0; i < len; i++) reversed[i] = elements[len - i - 1];

            int rotNormal = GetMinimalRotation(elements, getter),
                rotReversed = GetMinimalRotation(reversed, getter);

            var comp = StringComparer.Ordinal;
            bool isNormalSmaller = false;

            for (int i = 0; i < len; i++)
            {
                var cc = comp.Compare(getter(elements[(i + rotNormal) % len]), getter(reversed[(i + rotReversed) % len]));
                if (cc < 0) 
                {
                    isNormalSmaller = true;
                    break;
                }
                if (cc > 0) break;
            }

            if (isNormalSmaller) return BuildFinderprint(elements, rotNormal, getter);
            return BuildFinderprint(reversed, rotReversed, getter);
        }

        static int GetAtomIndex(IAtom[] atoms, IAtom atom, int start)
        {
            for (int i = start; i < atoms.Length; i++)
            {
                if (atoms[i] == atom) return i;
            }
            return -1;
        }

        public static bool OrderRingAtoms(IAtom[] atoms, IBondCollection bonds)
        {
            for (int i = 0; i < atoms.Length - 2; i++)
            {
                IAtom a = atoms[i];
                var bs = bonds[a];
                int nextIndex = -1;
                for (int j = 0; j < bs.Count; j++)
                {
                    nextIndex = GetAtomIndex(atoms, bs[j].B, i + 1);
                    if (nextIndex >= 0) break;
                }
                if (nextIndex < 0) return false; //throw new InvalidOperationException("Invalid ring.");
                var b = atoms[nextIndex];
                // swap the atoms
                atoms[nextIndex] = atoms[i + 1];
                atoms[i + 1] = b;
            }
            return true;
        }

        /// <summary>
        /// Reorders the atoms!!
        /// </summary>
        /// <param name="orderedAtoms"></param>
        /// <param name="bonds"></param>
        /// <returns></returns>
        static string GetFingerprint(IAtom[] orderedAtoms, IBondCollection bonds)
        {
            return GetFingerprint(orderedAtoms, a => a.ElementSymbol.ToString());
        }

        IAtom[] atoms;
        int? hash;

        /// <summary>
        /// return the hash code of this ring.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (this.hash.HasValue) return this.hash.Value;

            int hash = 17; 
            unchecked 
            {
                for (int i = 0; i < atoms.Length; i++) hash = 23 * hash + atoms[i].Id;
            }
            this.hash = hash;
            return hash;
        }

        /// <summary>
        /// Checks if two rings are identical (including atom Ids).
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as Ring;
            if (other == null) return false;
            return Equals(other);
        }


        public bool Equals(Ring other)
        {
            if (other.atoms.Length != this.atoms.Length || GetHashCode() != other.GetHashCode()) return false;
            if (GetHashCode() != other.GetHashCode()) return false;

            for (int i = 0; i < this.atoms.Length; i++)
            {
                if (this.atoms[i].Id != other.atoms[i].Id) return false;
            }

            return true;
        }

        /// <summary>
        /// Fingerprint string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Fingerprint;
        }

        /// <summary>
        /// Fingerprint - concatenated atoms ordered by element symbol string.
        /// </summary>
        public string Fingerprint { get; private set; }

        /// <summary>
        /// Atoms. Ordered by identifier symbol.
        /// </summary>
        public IAtom[] Atoms { get { return atoms; } }

        /////// <summary>
        /////// Optimized version used by "ReadFromFile"
        /////// </summary>
        /////// <param name="atoms"></param>
        /////// <returns></returns>
        ////internal static Ring FromOrderedAtoms(IAtom[] atoms)
        ////{
        ////    var r = new Ring();
        ////    r.atoms = atoms;
        ////    r.Fingerprint = string.Concat(r.atoms.Select(a => a.ElementSymbol.ToString()));
        ////    return r;
        ////}

        private Ring()
        {

        }

        class AtomIdComparer : IComparer<IAtom>
        {
            public int Compare(IAtom x, IAtom y)
            {
                return x.Id.CompareTo(y.Id);
            }

            public static readonly AtomIdComparer Instance = new AtomIdComparer();
        }

        /// <summary>
        /// Create a ring from atoms.
        /// </summary>
        /// <param name="atoms"></param>
        /// <param name="bonds"></param>
        internal static Ring Create(IEnumerable<IAtom> atoms, IBondCollection bonds)
        {
            var ret = new Ring();

            ret.atoms = atoms.ToArray();
            if (ret.atoms.Length < 3) throw new ArgumentException("A ring must contain at least 3 atoms.");

            if (!OrderRingAtoms(ret.atoms, bonds)) return null;

            ret.Fingerprint = GetFingerprint(ret.atoms, bonds); 
            Array.Sort(ret.atoms, AtomIdComparer.Instance);

            return ret;
        }
    }
}
