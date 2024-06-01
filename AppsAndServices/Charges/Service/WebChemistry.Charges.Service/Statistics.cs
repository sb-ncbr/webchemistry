namespace WebChemistry.Charges.Service
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WebChemistry.Charges.Core;
    using WebChemistry.Charges.Service.DataModel;
    using WebChemistry.Framework.Core;

    static class ChargeStatistics
    {
        struct DataPoint
        {
            public readonly double X, Y;

            public DataPoint(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public static ChargesCorrelationEntry FitData(string aId, AtomPartitionCharges a, string bId, AtomPartitionCharges b)
        {
            var dataPoints = new List<DataPoint>(Math.Min(a.PartitionCharges.Count, b.PartitionCharges.Count));

            double rmsd = 0.0;

            int count = 0;
            foreach (var group in a.Partition.Groups)
            {
                double ca, cb;
                if (a.PartitionCharges.TryGetValue(group, out ca) && b.PartitionCharges.TryGetValue(group, out cb))
                {
                    count++;
                    dataPoints.Add(new DataPoint(ca, cb));
                    var d = ca - cb;
                    rmsd += d * d;
                }
            }

            rmsd = Math.Sqrt(rmsd / (double)count);
            
            if (count < 2)
            {
                return new ChargesCorrelationEntry
                {
                    IndependentId = aId,
                    DependentId = bId
                };
            }

            double[,] fmatrix = new double[count, 2];

            double absoluteDifferenceSum = 0;

            var x = dataPoints.Select(p => p.X).ToArray();
            var y = dataPoints.Select(p => p.Y).ToArray();

            for (int i = 0; i < count; i++)
            {
                absoluteDifferenceSum += Math.Abs(x[i] - y[i]);
                fmatrix[i, 0] = 1;
                fmatrix[i, 1] = x[i];
            }

            int info = 0;
            double[] c = new double[0];
            alglib.lsfitreport rep = new alglib.lsfitreport();
            alglib.lsfitlinear(y, fmatrix, count, 2, out info, out c, out rep);

            double fitA = c[1];
            double fitB = c[0];

            var pearsonCoefficient = MathNet.Numerics.Statistics.Correlation.Pearson(x, y);
            pearsonCoefficient *= pearsonCoefficient;

            // save this for last - manipulates with x and y and chages the orders completely.
            var spearmanCoefficient = Spearman(x, y);

            return new ChargesCorrelationEntry
            {
                IndependentId = aId,
                DependentId = bId,

                A = fitA,
                B = fitB,
                DataPointCount = count,
                Rmsd = rmsd,
                AbsoluteDifferenceSum = absoluteDifferenceSum,
                PearsonCoefficient = pearsonCoefficient,
                SpearmanCoefficient = spearmanCoefficient
            };
        }

        #region Helpers

        static double Spearman(double[] x, double[] y)
        {
            Rank(x);
            Rank(y);

            return MathNet.Numerics.Statistics.Correlation.Pearson(x, y);
        }

        static void Rank(double[] x)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int t = 0;
            double tmp = 0;
            int tmpi = 0;

            int n = x.Length;

            //
            // Prepare
            //
            if (n < 1)
            {
                return;
            }
            if (n == 1)
            {
                x[0] = 1;
                return;
            }
            var ra1 = new double[n];
            var ia1 = new int[n];
            for (i = 0; i <= n - 1; i++)
            {
                ra1[i] = x[i];
                ia1[i] = i;
            }

            //
            // sort {R, C}
            //
            if (n != 1)
            {
                i = 2;
                do
                {
                    t = i;
                    while (t != 1)
                    {
                        k = t / 2;
                        if ((double)(ra1[k - 1]) >= (double)(ra1[t - 1]))
                        {
                            t = 1;
                        }
                        else
                        {
                            tmp = ra1[k - 1];
                            ra1[k - 1] = ra1[t - 1];
                            ra1[t - 1] = tmp;
                            tmpi = ia1[k - 1];
                            ia1[k - 1] = ia1[t - 1];
                            ia1[t - 1] = tmpi;
                            t = k;
                        }
                    }
                    i = i + 1;
                }
                while (i <= n);
                i = n - 1;
                do
                {
                    tmp = ra1[i];
                    ra1[i] = ra1[0];
                    ra1[0] = tmp;
                    tmpi = ia1[i];
                    ia1[i] = ia1[0];
                    ia1[0] = tmpi;
                    t = 1;
                    while (t != 0)
                    {
                        k = 2 * t;
                        if (k > i)
                        {
                            t = 0;
                        }
                        else
                        {
                            if (k < i)
                            {
                                if ((double)(ra1[k]) > (double)(ra1[k - 1]))
                                {
                                    k = k + 1;
                                }
                            }
                            if ((double)(ra1[t - 1]) >= (double)(ra1[k - 1]))
                            {
                                t = 0;
                            }
                            else
                            {
                                tmp = ra1[k - 1];
                                ra1[k - 1] = ra1[t - 1];
                                ra1[t - 1] = tmp;
                                tmpi = ia1[k - 1];
                                ia1[k - 1] = ia1[t - 1];
                                ia1[t - 1] = tmpi;
                                t = k;
                            }
                        }
                    }
                    i = i - 1;
                }
                while (i >= 1);
            }

            //
            // compute tied ranks
            //
            i = 0;
            while (i <= n - 1)
            {
                j = i + 1;
                while (j <= n - 1)
                {
                    if ((double)(ra1[j]) != (double)(ra1[i]))
                    {
                        break;
                    }
                    j = j + 1;
                }
                for (k = i; k <= j - 1; k++)
                {
                    ra1[k] = 1 + (double)(i + j - 1) / (double)2;
                }
                i = j;
            }

            //
            // back to x
            //
            for (i = 0; i <= n - 1; i++)
            {
                x[ia1[i]] = ra1[i];
            }
        }
        #endregion
    }
}
