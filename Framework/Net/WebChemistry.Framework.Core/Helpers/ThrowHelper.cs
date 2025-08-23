using System;

namespace WebChemistry.Framework.Core
{
    static class ThrowHelper
    {
        public static void ThrowArgumentException(string argName)
        {
            throw new ArgumentException(argName);
        }
    }
}
