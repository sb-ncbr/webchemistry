using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebChemistry.SiteBinder.Core
{
    class ColumnMajorMatrix4x4
    {
        double[] data;

        public double[] Data { get { return data; } }

        public double this[int i, int j]
        {
            get { return data[4 * j + i]; }
            set { data[4 * j + i] = value; }
        }

        public void Reset()
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0.0;
        }

        public ColumnMajorMatrix4x4()
        {
            data = new double[16];
        }
    }

    class EvdCache
    {
        public ColumnMajorMatrix4x4 Matrix;
        public double[] EigenValues, D, E;

        public EvdCache()
        {
            Matrix = new ColumnMajorMatrix4x4();
            EigenValues = new double[4];
            D = new double[4];
            E = new double[4];
        }
    }

    /// <summary>
    /// Borrowed from Math.Net Numerics. Does not create a billion new matrices for each computation and instead reuses the existing one.
    /// </summary>
    class Evd4x4
    {
        public static void Compute(EvdCache cache)
        {
            SymmetricEigenDecomp(4, cache.Matrix.Data, cache.EigenValues, cache.D, cache.E);
        }

        static void SymmetricEigenDecomp(int order, double[] matrixEv, double[] vectorEv, double[] d, double[] e)
        {
            //var d = new double[order];
            //var e = new double[order];
            for (int i = 0; i < order; i++)
            {                
                e[i] = 0.0;
            }

            var om1 = order - 1;
            for (var i = 0; i < order; i++)
            {
                d[i] = matrixEv[i * order + om1];
            }

            SymmetricTridiagonalize(matrixEv, d, e, order);
            SymmetricDiagonalize(matrixEv, d, e, order);

            for (var i = 0; i < order; i++)
            {
                vectorEv[i] = d[i];

                //var io = i * order;
                //matrixD[io + i] = d[i];

                //if (e[i] > 0)
                //{
                //    matrixD[io + order + i] = e[i];
                //    matrixD[(i + 1) * order + i] = e[i];
                //}
                //else if (e[i] < 0)
                //{
                //    matrixD[io - order + i] = e[i];
                //}
            }
        }

        internal static void SymmetricTridiagonalize(double[] a, double[] d, double[] e, int order)
        {
            // Householder reduction to tridiagonal form.
            for (var i = order - 1; i > 0; i--)
            {
                // Scale to avoid under/overflow.
                var scale = 0.0;
                var h = 0.0;

                for (var k = 0; k < i; k++)
                {
                    scale = scale + Math.Abs(d[k]);
                }

                if (scale == 0.0)
                {
                    e[i] = d[i - 1];
                    for (var j = 0; j < i; j++)
                    {
                        d[j] = a[(j * order) + i - 1];
                        a[(j * order) + i] = 0.0;
                        a[(i * order) + j] = 0.0;
                    }
                }
                else
                {
                    // Generate Householder vector.
                    for (var k = 0; k < i; k++)
                    {
                        d[k] /= scale;
                        h += d[k] * d[k];
                    }

                    var f = d[i - 1];
                    var g = Math.Sqrt(h);
                    if (f > 0)
                    {
                        g = -g;
                    }

                    e[i] = scale * g;
                    h = h - (f * g);
                    d[i - 1] = f - g;

                    for (var j = 0; j < i; j++)
                    {
                        e[j] = 0.0;
                    }

                    // Apply similarity transformation to remaining columns.
                    for (var j = 0; j < i; j++)
                    {
                        f = d[j];
                        a[(i * order) + j] = f;
                        g = e[j] + (a[(j * order) + j] * f);

                        for (var k = j + 1; k <= i - 1; k++)
                        {
                            g += a[(j * order) + k] * d[k];
                            e[k] += a[(j * order) + k] * f;
                        }

                        e[j] = g;
                    }

                    f = 0.0;

                    for (var j = 0; j < i; j++)
                    {
                        e[j] /= h;
                        f += e[j] * d[j];
                    }

                    var hh = f / (h + h);

                    for (var j = 0; j < i; j++)
                    {
                        e[j] -= hh * d[j];
                    }

                    for (var j = 0; j < i; j++)
                    {
                        f = d[j];
                        g = e[j];

                        for (var k = j; k <= i - 1; k++)
                        {
                            a[(j * order) + k] -= (f * e[k]) + (g * d[k]);
                        }

                        d[j] = a[(j * order) + i - 1];
                        a[(j * order) + i] = 0.0;
                    }
                }

                d[i] = h;
            }

            // Accumulate transformations.
            for (var i = 0; i < order - 1; i++)
            {
                a[(i * order) + order - 1] = a[(i * order) + i];
                a[(i * order) + i] = 1.0;
                var h = d[i + 1];
                if (h != 0.0)
                {
                    for (var k = 0; k <= i; k++)
                    {
                        d[k] = a[((i + 1) * order) + k] / h;
                    }

                    for (var j = 0; j <= i; j++)
                    {
                        var g = 0.0;
                        for (var k = 0; k <= i; k++)
                        {
                            g += a[((i + 1) * order) + k] * a[(j * order) + k];
                        }

                        for (var k = 0; k <= i; k++)
                        {
                            a[(j * order) + k] -= g * d[k];
                        }
                    }
                }

                for (var k = 0; k <= i; k++)
                {
                    a[((i + 1) * order) + k] = 0.0;
                }
            }

            for (var j = 0; j < order; j++)
            {
                d[j] = a[(j * order) + order - 1];
                a[(j * order) + order - 1] = 0.0;
            }

            a[(order * order) - 1] = 1.0;
            e[0] = 0.0;
        }

        internal static void SymmetricDiagonalize(double[] a, double[] d, double[] e, int order)
        {
            const int maxiter = 1000;

            for (var i = 1; i < order; i++)
            {
                e[i - 1] = e[i];
            }

            e[order - 1] = 0.0;

            var f = 0.0;
            var tst1 = 0.0;
            var eps = Math.Pow(2, -53); // DoubleWidth = 53
            for (var l = 0; l < order; l++)
            {
                // Find small subdiagonal element
                tst1 = Math.Max(tst1, Math.Abs(d[l]) + Math.Abs(e[l]));
                var m = l;
                while (m < order)
                {
                    if (Math.Abs(e[m]) <= eps * tst1)
                    {
                        break;
                    }

                    m++;
                }

                // If m == l, d[l] is an eigenvalue,
                // otherwise, iterate.
                if (m > l)
                {
                    var iter = 0;
                    do
                    {
                        iter = iter + 1; // (Could check iteration count here.)

                        // Compute implicit shift
                        var g = d[l];
                        var p = (d[l + 1] - g) / (2.0 * e[l]);
                        var r = Hypotenuse(p, 1.0);
                        if (p < 0)
                        {
                            r = -r;
                        }

                        d[l] = e[l] / (p + r);
                        d[l + 1] = e[l] * (p + r);

                        var dl1 = d[l + 1];
                        var h = g - d[l];
                        for (var i = l + 2; i < order; i++)
                        {
                            d[i] -= h;
                        }

                        f = f + h;

                        // Implicit QL transformation.
                        p = d[m];
                        var c = 1.0;
                        var c2 = c;
                        var c3 = c;
                        var el1 = e[l + 1];
                        var s = 0.0;
                        var s2 = 0.0;
                        for (var i = m - 1; i >= l; i--)
                        {
                            c3 = c2;
                            c2 = c;
                            s2 = s;
                            g = c * e[i];
                            h = c * p;
                            r = Hypotenuse(p, e[i]);
                            e[i + 1] = s * r;
                            s = e[i] / r;
                            c = p / r;
                            p = (c * d[i]) - (s * g);
                            d[i + 1] = h + (s * ((c * g) + (s * d[i])));

                            // Accumulate transformation.
                            for (var k = 0; k < order; k++)
                            {
                                h = a[((i + 1) * order) + k];
                                a[((i + 1) * order) + k] = (s * a[(i * order) + k]) + (c * h);
                                a[(i * order) + k] = (c * a[(i * order) + k]) - (s * h);
                            }
                        }

                        p = (-s) * s2 * c3 * el1 * e[l] / dl1;
                        e[l] = s * p;
                        d[l] = c * p;

                        // Check for convergence. If too many iterations have been performed, 
                        // throw exception that Convergence Failed
                        if (iter >= maxiter)
                        {
                            throw new InvalidOperationException("Not converging.");
                        }
                    } while (Math.Abs(e[l]) > eps * tst1);
                }

                d[l] = d[l] + f;
                e[l] = 0.0;
            }

            // Sort eigenvalues and corresponding vectors.
            for (var i = 0; i < order - 1; i++)
            {
                var k = i;
                var p = d[i];
                for (var j = i + 1; j < order; j++)
                {
                    if (d[j] < p)
                    {
                        k = j;
                        p = d[j];
                    }
                }

                if (k != i)
                {
                    d[k] = d[i];
                    d[i] = p;
                    for (var j = 0; j < order; j++)
                    {
                        p = a[(i * order) + j];
                        a[(i * order) + j] = a[(k * order) + j];
                        a[(k * order) + j] = p;
                    }
                }
            }
        }

        public static double Hypotenuse(double a, double b)
        {
            if (Math.Abs(a) > Math.Abs(b))
            {
                double r = b / a;
                return Math.Abs(a) * Math.Sqrt(1 + (r * r));
            }

            if (b != 0.0)
            {
                // NOTE (ruegg): not "!b.AlmostZero()" to avoid convergence issues (e.g. in SVD algorithm)
                double r = a / b;
                return Math.Abs(b) * Math.Sqrt(1 + (r * r));
            }

            return 0d;
        }
    }
}
