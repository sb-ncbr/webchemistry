namespace WebChemistry.Framework.Core
{

    /// <summary>
    /// Represents RGB color.
    /// </summary>
    public class ElementColor
    {
        /// <summary>
        /// Default color. Set to R = 128, G = 128, B = 128.
        /// </summary>
        public static readonly ElementColor Default = new ElementColor(128, 128, 128);

        /// <summary>
        /// Gets the red component.
        /// </summary>
        public byte R { get; private set; }
        
        /// <summary>
        /// Gets the green component.
        /// </summary>
        public byte G { get; private set; }

        /// <summary>
        /// Gets the blue component.
        /// </summary>
        public byte B { get; private set; }

        /// <summary>
        /// Convers to a hex string: #RRGGBB
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", R, G, B);
        }

        /// <summary>
        /// Create ...
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public ElementColor(byte r, byte g, byte b)
        {
            this.R = r;
            this.B = b;
            this.G = g;
        }

        /// <summary>
        /// Creates an instance of the class from hex string representation of the color.
        /// </summary>
        /// <param name="s">Hex string representation of the color. I.e. "#RRGGBB".</param>
        /// <returns></returns>
        public static ElementColor Parse(string s)
        {
            return new ElementColor(
                r: byte.Parse(s.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                g: byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                b: byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.HexNumber));
        }
    }
}
