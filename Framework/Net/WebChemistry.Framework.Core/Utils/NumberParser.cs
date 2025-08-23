namespace WebChemistry.Framework.Core
{
    using System;

    /// <summary>
    /// Faster number parsing methods.
    /// </summary>
    public static class NumberParser
    {
        /// <summary>
        /// Integers.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static int ParseIntFast(string str, int start, int count)
        {
            int val = 0;
            bool neg = false;
            bool trailing = true;
            int end = start + count;
            for (int i = start; i < end; i++)
            {
                var c = str[i];
                if (char.IsDigit(c))
                {
                    val = val * 10 + (c - '0');
                    trailing = false;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (trailing) continue;
                    break;
                }
                else if (c == '-') neg = true;                
                else break;
            }
            return neg ? -val : val;
        }

        static double ParseDoubleScientificFast(double main, string str, int start, int count)
        {
            var exp = ParseIntFast(str, start, count);
            return main * Math.Pow(10, exp);
        }

        /// <summary>
        /// Doubles.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static double ParseDoubleFast(string str, int start, int count)
        {
            double main = 0;
            double point = 0;
            bool neg = false;
            bool trailing = true;
            int i;
            var end = start + count;
            for (i = start; i < end; i++)
            {
                var c = str[i];
                if (char.IsDigit(c))
                {
                    main = main * 10 + (c - '0');
                    trailing = false;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (trailing) continue;
                    return (neg ? -main : main);
                }
                else if (c == '-') neg = true;
                else if (c == '.') break;
                else if (c == 'e' || c == 'E') return ParseDoubleScientificFast(neg ? -main : main, str, i + 1, start + count - i - 1);                
                else return (neg ? -main : main);
            }
            double div = 1;
            for (i = i + 1; i < end; i++)
            {
                var c = str[i];
                if (char.IsDigit(c))
                {
                    div *= 10;
                    point = point * 10 + (c - '0');
                }
                else if (c == 'e' || c == 'E') return ParseDoubleScientificFast(neg ? -(main + point / div) : (main + point / div), str, i + 1, start + count - i - 1);
                else break;
            }
            return neg ? -(main + point / div) : (main + point / div);
        }
    }
}
