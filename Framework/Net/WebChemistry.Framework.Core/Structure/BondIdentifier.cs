namespace WebChemistry.Framework.Core
{
    using System;

    /// <summary>
    /// An unique bond identifier computed from atom identifiers.
    /// </summary>
    public struct BondIdentifier : IEquatable<BondIdentifier>
    {
        private readonly long Id;

        /// <summary>
        /// Returns the hash code computed as a hash of a 64bit id.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashHelper.GetHashFromLong(Id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is BondIdentifier)
            {
                return this.Id == ((BondIdentifier)obj).Id;
            } 
            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(BondIdentifier other)
        {
            return this.Id == other.Id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(BondIdentifier a, BondIdentifier b)
        {
            return a.Id == b.Id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(BondIdentifier a, BondIdentifier b)
        {
            return a.Id != b.Id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static BondIdentifier Get(IAtom a, IAtom b)
        {
            return new BondIdentifier(a, b);
        }

        /// <summary>
        /// Creates the identifier.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public BondIdentifier(IAtom a, IAtom b)
        {
            long i = a.Id;
            long j = b.Id;

            if (i > j) Id = (j << 32) | i;
            else Id = (i << 32) | j;
        }
    }
}
