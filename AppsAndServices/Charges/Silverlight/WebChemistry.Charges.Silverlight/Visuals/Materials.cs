using System;
using System.Windows.Media;

namespace WebChemistry.Charges.Silverlight.Visuals
{
    public static class Materials
    {
        public static readonly Brush HighlightBrush = new SolidColorBrush(Colors.Yellow);
        public static readonly Brush DefaultAtomBrush = new SolidColorBrush(Colors.DarkGray);
        public static readonly Brush DefaultBondBrush = new SolidColorBrush(Colors.LightGray);
        public static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);
        //public static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xAA, 0x00));
        
        // Must be an odd number
        static readonly int Granuality = 61;
        static readonly int GranualityHalf = (Granuality - 1) / 2;
        static readonly double GranualityHalfDelta = 1.0 / GranualityHalf;

        static SolidColorBrush[] _atomBrushes = new SolidColorBrush[Granuality];
        static LinearGradientBrush[,] _bondBrushes = new LinearGradientBrush[Granuality, Granuality];

        static Materials()
        {
            _atomBrushes[GranualityHalf] = new SolidColorBrush(Colors.White);

            Color minColor = Color.FromArgb(0xFF, 0x2B, 0x5D, 0x92);
            Color maxColor = Color.FromArgb(0xFF, 0xE3, 0x00, 0x16);

            double dMinR = (255.0 - minColor.R) / GranualityHalf;
            double dMinG = (255.0 - minColor.G) / GranualityHalf;
            double dMinB = (255.0 - minColor.B) / GranualityHalf;

            double dMaxR = (255.0 - maxColor.R) / GranualityHalf;
            double dMaxG = (255.0 - maxColor.G) / GranualityHalf;
            double dMaxB = (255.0 - maxColor.B) / GranualityHalf;

            for (int i = 0; i < GranualityHalf; i++)
            {
                _atomBrushes[i] = new SolidColorBrush(Color.FromArgb(255, (byte)(minColor.R + i * dMinR), (byte)(minColor.G + i * dMinG), (byte)(minColor.B + i * dMinB)));
                _atomBrushes[Granuality - i - 1] = new SolidColorBrush(Color.FromArgb(255, (byte)(maxColor.R + i * dMaxR), (byte)(maxColor.G + i * dMaxG), (byte)(maxColor.B + i * dMaxB)));
            }

            for (int i = 0; i < Granuality; i++)
            {
                for (int j = 0; j < Granuality; j++)
                {
                    GradientStopCollection stops = new GradientStopCollection();
                    stops.Add(new GradientStop() { Color = _atomBrushes[i].Color, Offset = 0 });
                    stops.Add(new GradientStop() { Color = _atomBrushes[j].Color, Offset = 1 });
                    _bondBrushes[i, j] = new LinearGradientBrush(stops, 0);
                }
            }
        }

        static int GetIndex(double charge, Tuple<double, double> chargeRange)
        {
            double minRatio = chargeRange.Item1 * GranualityHalfDelta;
            double maxRatio = chargeRange.Item2 * GranualityHalfDelta;

            if (charge <= maxRatio && charge >= minRatio) return GranualityHalf;

            int index;

            if (charge < 0)
            {
                index = GranualityHalf - 1 - (int)(charge / minRatio);
            }
            else
            {
                index = GranualityHalf + 1 + (int)(charge / maxRatio);
            }
            
            if (index < 0) return 0;
            if (index >= Granuality) return Granuality - 1;
            return index;
        }


        public static SolidColorBrush GetAtomBrush(double charge, Tuple<double, double> chargeRange)
        {
            return _atomBrushes[GetIndex(charge, chargeRange)];
        }

        public static LinearGradientBrush GetBondBrush(double chargeA, double chargeB, Tuple<double, double> chargeRange)
        {
            return _bondBrushes[GetIndex(chargeA, chargeRange), GetIndex(chargeB, chargeRange)];
        }
    }
}
