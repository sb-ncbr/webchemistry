using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls.DataVisualization;
using System.Windows.Input;
using WebChemistry.Charges.Core;
using WebChemistry.Charges.Silverlight.ViewModel;
using WebChemistry.Framework.Core;

namespace WebChemistry.Charges.Silverlight.DataModel
{
    public class Correlation
    {
        public class AtomDataPoint
        {

            public double X
            {
                get;
                set;
            }

            public double Y
            {
                get;
                set;
            }

            public double Delta
            {
                get { return Math.Abs(X - Y); }
            }

            public string Label
            {
                get;
                set;
            }
        }

        private ICommand _setCurrentCommand;
        public ICommand SetCurrentCommand
        {
            get
            {
                _setCurrentCommand = _setCurrentCommand ?? new RelayCommand(() => ServiceLocator.Current.GetInstance<CorrelationViewModel>().CurrentCorrelation = this);
                return _setCurrentCommand;
            }
        }
        
        public StructureWrap Structure { get; set; }
        public string PartitionName { get; set; }

        public string IndependentName { get; set; }
        public string IndependentMethodId { get; set; }
        public StructureCharges IndependentCharges { get; set; }
        public Range<double> IndependentRange { get; set; }

        public string DependentName { get; set; }
        public string DependentMethodId { get; set; }
        public StructureCharges DependentCharges { get; set; }
        public Range<double> DependentRange { get; set; }
        
        public string FormattedSlope { get; set; }
        public string FormattedSpearmanCoefficient { get; set; }
        public string FormattedPearsonCoefficient { get; set; }
        public string FormattedAbsoluteDifferenceSum { get; set; }
        public string FormattedRmsd { get; set; }

        public double A { get; set; }
        public double B { get; set; }
        public double SpearmanCoefficient { get; set; }
        public double PearsonCoefficient { get; set; }
        public double AbsoluteDifferenceSum { get; set; }
        public double Rmsd { get; set; }

        public IList<AtomDataPoint> DataPoints { get; set; }

        void Format()
        {
            string format = "0.000";
            if (Math.Abs(B) < 0.0005) FormattedSlope = string.Format("y = {0}x", A.ToString(format, CultureInfo.InvariantCulture));
            else if (B >= 0) FormattedSlope = string.Format("y = {0}x + {1}", A.ToString(format, CultureInfo.InvariantCulture), B.ToString(format, CultureInfo.InvariantCulture));
            else FormattedSlope = string.Format("y = {0}x - {1}", A.ToString(format, CultureInfo.InvariantCulture), (-B).ToString(format, CultureInfo.InvariantCulture));

            FormattedPearsonCoefficient = PearsonCoefficient.ToString("0.000", CultureInfo.InvariantCulture);
            FormattedSpearmanCoefficient = SpearmanCoefficient.ToString("0.000", CultureInfo.InvariantCulture);
            FormattedAbsoluteDifferenceSum = AbsoluteDifferenceSum.ToString("0.000", CultureInfo.InvariantCulture);
            FormattedRmsd = Rmsd.ToString("0.000", CultureInfo.InvariantCulture);
        }

        public Correlation Invert()
        {
            double a = 1 / (A - B);
            double b = B / (B - A);

            var ret = new Correlation
            {
                Structure = Structure,
                PartitionName = PartitionName,

                IndependentName = DependentName,
                IndependentCharges = DependentCharges,
                IndependentRange = DependentRange,
                IndependentMethodId = DependentMethodId,

                DependentName = IndependentName,
                DependentCharges = IndependentCharges,
                DependentRange = IndependentRange,
                DependentMethodId = IndependentMethodId,

                A = a,
                B = b,
                SpearmanCoefficient = SpearmanCoefficient,
                PearsonCoefficient = PearsonCoefficient,
                AbsoluteDifferenceSum = AbsoluteDifferenceSum,
                Rmsd = Rmsd,

                DataPoints = DataPoints.Select(p => new AtomDataPoint { X = p.Y, Y = p.X, Label = p.Label }).ToList()
            };

            ret.Format();

            return ret;
        }

        void FitData(bool selectionOnly, string partition, int setI, int setJ)
        {
            var atomCount = Structure.Structure.Atoms.Count;
            DataPoints = new List<AtomDataPoint>(atomCount);
            
            IndependentName = Structure.Charges[setI].Name;
            IndependentCharges = Structure.Charges[setI];
            IndependentMethodId = IndependentCharges.Result.Parameters.MethodId;

            DependentName = Structure.Charges[setJ].Name;
            DependentCharges = Structure.Charges[setJ];
            DependentMethodId = DependentCharges.Result.Parameters.MethodId;
            Rmsd = 0;

            int count = 0;

            var a = Structure.Charges[setI].PartitionCharges[partition];
            var b = Structure.Charges[setJ].PartitionCharges[partition];
                        
            foreach (var group in a.Partition.Groups)
            {
                if (selectionOnly && !group.Data.Any(t => t.IsSelected)) continue;

                double ca, cb;
                if (a.PartitionCharges.TryGetValue(group, out ca) && b.PartitionCharges.TryGetValue(group, out cb))
                {
                    count++;
                    DataPoints.Add(new AtomDataPoint
                    {
                        X = ca,
                        Y = cb,
                        Label = group.Label
                    });
                    var d = ca - cb;
                    Rmsd += d * d;
                }
            }

            ////else
            ////{
            ////    var a = Structure.Charges[setI].Result.Charges;
            ////    var b = Structure.Charges[setJ].Result.Charges;

            ////    foreach (var atom in Structure.Structure.Atoms)
            ////    {
            ////        if (selectionOnly && !atom.IsSelected) continue;

            ////        ChargeValue ca, cb;
            ////        if (a.TryGetValue(atom, out ca) && b.TryGetValue(atom, out cb))
            ////        {
            ////            count++;
            ////            DataPoints.Add(new AtomDataPoint { X = ca.Charge, Y = cb.Charge, Label = atom.ElementSymbol.ToString() + " " + atom.Id });
            ////            var d = ca.Charge - cb.Charge;
            ////            Rmsd += d * d;
            ////            //string.Format(CultureInfo.InvariantCulture,
            ////            //    "{0} ({1})\n{3}: {2:0.00000}\n{5}: {4:0.00000}",
            ////            //        atom.ElementSymbol.ToString(), atom.Id,
            ////            //        ca, IndependentName,
            ////            //        cb, DependentName)));
            ////        }
            ////    }
            ////}

            Rmsd = Math.Sqrt(Rmsd / (double)count);

            //var rnd = new Random();
            //for (int i = 0; i < 100000; i++)
            //{
            //    DataPoints.Add(new DataPoint { X = -1 + 2 * rnd.NextDouble(), Y = -1 + 2 * rnd.NextDouble() });
            //}
            
            if (count < 2)
            {
                A = B = PearsonCoefficient = SpearmanCoefficient = AbsoluteDifferenceSum = 0.0;
                IndependentRange = new Range<double>(-1, 1);
                DependentRange = new Range<double>(-1, 1);
                Format();
                return;
            }

            double[,] fmatrix = new double[count, 2];

            AbsoluteDifferenceSum = 0;

            var x = DataPoints.Select(p => p.X).ToArray();
            var y = DataPoints.Select(p => p.Y).ToArray();


            IndependentRange = new Range<double>(x.Min(), x.Max());
            DependentRange = new Range<double>(y.Min(), y.Max());

            for (int i = 0; i < count; i++)
            {
                AbsoluteDifferenceSum += Math.Abs(x[i] - y[i]);
                fmatrix[i, 0] = 1;
                fmatrix[i, 1] = x[i];
            }

            int info = 0;
            double[] c = new double[0];
            alglib.lsfitreport rep = new alglib.lsfitreport();
            alglib.lsfitlinear(y, fmatrix, count, 2, out info, out c, out rep);

            A = c[1];
            B = c[0];


            //var yData = new DenseVector(y);
            //var M = DenseMatrix.CreateFromColumns(new[] { new DenseVector(count, 1), yData });
            //var Z = yData;
            //var fit = M.QR().Solve(Z);

            //LogService.Default.AddEntryDispatch("Fit done");
            //A = fit[1];
            //B = fit[0];


            PearsonCoefficient = MathNet.Numerics.Statistics.Correlation.Pearson(x, y);   //alglib.pearsoncorrelation(x, y, count);
            PearsonCoefficient *= PearsonCoefficient;
            
            // save this for last - manipulates with x and y and chages the orders completely.
            SpearmanCoefficient = Spearman(x, y); //alglib.spearmanrankcorrelation(x, y, count);
            

            Format();
        }

        public static Correlation Create(StructureWrap structure, bool selectionOnly, string partition, int i, int j)
        {
            var ret = new Correlation
            {
                Structure = structure,
                PartitionName = partition
            };

            ret.FitData(selectionOnly, partition, i, j);

            return ret;
        }
        
        private Correlation()
        {

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
