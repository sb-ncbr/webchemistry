
namespace WebChemistry.Framework.Visualization
{
    using System.Windows.Media;

    public static class Coloring
    {
        public static Color GetGradientColor(int i, int numSteps, Color minColor, Color maxColor)
        {
            return Interpolate(minColor, maxColor, i, numSteps);
        }

        static Color Interpolate(Color minColor, Color maxColor, int i, int numSteps)
        {
            if (numSteps == 1) return minColor;

            double dR = (maxColor.R - minColor.R) / (numSteps - 1);
            double dG = (maxColor.G - minColor.G) / (numSteps - 1);
            double dB = (maxColor.B - minColor.B) / (numSteps - 1);

            return Color.FromArgb(0xFF, (byte)(minColor.R + i * dR), (byte)(minColor.G + i * dG), (byte)(minColor.B + i * dB));
        }
    }
}
