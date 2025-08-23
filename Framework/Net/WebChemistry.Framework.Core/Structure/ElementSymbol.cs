namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    namespace Internal
    {
        static class SymbolTable
        {
            public static Dictionary<string, Tuple<ElementSymbolsInternal, string>> Symbols;

            static IEnumerable<ElementSymbolsInternal> GetSymbols()
            {
                var type = typeof(ElementSymbolsInternal);
                if (!type.IsEnum)
                    throw new ArgumentException("Type '" + type.Name + "' is not an enum");

                return (
                  from field in type.GetFields(BindingFlags.Public | BindingFlags.Static)
                  where field.IsLiteral
                  select (ElementSymbolsInternal)field.GetValue(null)
                );
            }

            static SymbolTable()
            {
                var symb = GetSymbols().ToArray();
                Symbols = new Dictionary<string, Tuple<ElementSymbolsInternal, string>>(symb.Length, StringComparer.OrdinalIgnoreCase);

                foreach (var s in symb)
                {
                    Symbols.Add(s.ToString(), Tuple.Create(s, s.ToString()));
                }
            }
        }
    }

    /// <summary>
    /// Represents an element symbol.
    /// </summary>
    public struct ElementSymbol : IEquatable<ElementSymbol>, IComparable<ElementSymbol>, IComparable
    {
        ElementSymbolsInternal symbol;
        string symbolString;
        int hash;
                
        public override string ToString()
        {
            return symbolString;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ElementSymbol)) return false;
            ElementSymbol other = (ElementSymbol)obj;            
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        static Tuple<ElementSymbolsInternal, string> ParseElementSymbol(string symbolString)
        {
            Tuple<ElementSymbolsInternal, string> value;
            if (Internal.SymbolTable.Symbols.TryGetValue(symbolString, out value)) return value;
            return Tuple.Create(ElementSymbolsInternal.Unknown, symbolString);
        }

        public static bool IsKnownSymbol(string symbolString)
        {
            if (Internal.SymbolTable.Symbols.ContainsKey(symbolString)) return true;
            return false;
        }

        public string GetLongName()
        {
            if (symbol != ElementSymbolsInternal.Unknown) return ((ElementSymbolsLongInternal)symbol).ToString();
            return symbolString;
        }

        public static bool operator ==(ElementSymbol a, ElementSymbol b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ElementSymbol a, ElementSymbol b)
        {
            return !a.Equals(b);
        }

        public static ElementSymbol Create(string elementSymbol)
        {
            return new ElementSymbol(elementSymbol);
        }

        private ElementSymbol(string elementSymbol)
        {
            var parsed = ParseElementSymbol(elementSymbol);
            symbol = parsed.Item1;
            symbolString = parsed.Item2;
            if (symbol == ElementSymbolsInternal.Unknown) hash = symbolString.GetHashCode();
            else hash = (int)symbol;
        }

        public bool Equals(ElementSymbol other)
        {
            return string.Equals(symbolString, other.symbolString, StringComparison.Ordinal);

            //if (other.symbol == ElementSymbolsInternal.Unknown && symbol == ElementSymbolsInternal.Unknown)
            //{
            //    return string.Equals(symbolString, other.symbolString, StringComparison.Ordinal);
            //}
            ////.Compare(other.symbolString, symbolString, StringComparison.InvariantCultureIgnoreCase) == 0;
            //return other.symbol == symbol;
        }

        /// <summary>
        /// Uses SymbolString to compare the two symbols.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ElementSymbol other)
        {
            return StringComparer.Ordinal.Compare(symbolString, other.symbolString);
        }

        public int CompareTo(object obj)
        {
            var s = (ElementSymbol)obj;
            return CompareTo(s);
        }
    }
}
