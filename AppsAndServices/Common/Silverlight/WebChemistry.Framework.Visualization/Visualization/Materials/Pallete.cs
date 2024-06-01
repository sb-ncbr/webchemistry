using System;
using System.Windows.Media;

namespace WebChemistry.Framework.Visualization
{
    public static class Pallete
    {
        static Random rnd = new Random();

        static Color Previous = RandomMix(Color.FromArgb(255, 166, 0, 100), Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 0, 100, 255), 0.5);

        public static void UpdateSeed(int seed)
        {
            rnd = new Random(seed);
        }

        public static Color GetRandomColor(double amountOfGrey = 0.1)
        {
            //byte[] buffer = new byte[3];
            //rnd.NextBytes(buffer);
            //return Color.FromArgb(255, buffer[0], buffer[1], buffer[2]);

            int counter = 0;
            while (true)
            {
                counter++;
                var c = RandomMix(Color.FromArgb(255, 166, 0, 100), Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 0, 100, 255), amountOfGrey);
                var d = System.Math.Abs(Previous.R - c.R) + System.Math.Abs(Previous.G - c.G) + System.Math.Abs(Previous.B - c.B);
                if (d > 100 || counter == 10) { Previous = c; return c; }
            }

            //return RandomMix(Color.FromArgb(255, 166, 0, 100), Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 0, 100, 255), amountOfGrey);
            //return RandomMix(Colors.Red, Colors.Green, Colors.Blue, amountOfGrey);
        }

        public static Color RandomMix(Color color1, Color color2, Color color3, double greyControl)
        {
            int randomIndex = rnd.Next(3);

            double mixRatio1 =
               (randomIndex == 0) ? rnd.NextDouble() * greyControl : rnd.NextDouble();

            double mixRatio2 =
               (randomIndex == 1) ? rnd.NextDouble() * greyControl : rnd.NextDouble();

            double mixRatio3 =
               (randomIndex == 2) ? rnd.NextDouble() * greyControl : rnd.NextDouble();

            double sum = mixRatio1 + mixRatio2 + mixRatio3;

            mixRatio1 /= sum;
            mixRatio2 /= sum;
            mixRatio3 /= sum;

            return Color.FromArgb(
               255,
               (byte)(mixRatio1 * color1.R + mixRatio2 * color2.R + mixRatio3 * color3.R),
               (byte)(mixRatio1 * color1.G + mixRatio2 * color2.G + mixRatio3 * color3.G),
               (byte)(mixRatio1 * color1.B + mixRatio2 * color2.B + mixRatio3 * color3.B));
        }
    }
}
