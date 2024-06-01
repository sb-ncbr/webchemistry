namespace WebChemistry.Framework.Core
{
    /// <summary>
    /// Hash-code helpers.
    /// </summary>
    public static class HashHelper
    {
        /// <summary>
        /// Computes a 32-bit hash from a 64-bit value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetHashFromLong(long value)
        {
            long key = value;
            key = (~key) + (key << 18); // key = (key << 18) - key - 1;
            key = key ^ (key >> 31);
            key = key * 21; // key = (key + (key << 2)) + (key << 4);
            key = key ^ (key >> 11);
            key = key + (key << 6);
            key = key ^ (key >> 22);
            return (int)key;
        }
    }
}
