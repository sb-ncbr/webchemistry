namespace WebChemistry.Framework.Math
{
    using System;
    using System.Collections.Generic;

    public static partial class MathEx
    {
        public static Vector3D Clone(this Vector3D v)
        {
            return new Vector3D(v.X, v.Y, v.Z);
        }

        public static Quaternion Clone(this Quaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static Vector3D PerspectiveTransform(this Vector3D point, ref Matrix3D worldToNdc, ref Matrix3D ndcToScreen)
        {
            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            Vector3D p = new Vector3D(x * worldToNdc.M11 + y * worldToNdc.M21 + z * worldToNdc.M31 + worldToNdc.OffsetX,
                                      x * worldToNdc.M12 + y * worldToNdc.M22 + z * worldToNdc.M32 + worldToNdc.OffsetY,
                                      x * worldToNdc.M13 + y * worldToNdc.M23 + z * worldToNdc.M33 + worldToNdc.OffsetZ);

            double w = x * worldToNdc.M14 + y * worldToNdc.M24 + z * worldToNdc.M34 + worldToNdc.M44;
            double wInverse = 1.0 / w;
            p = ndcToScreen.Transform(new Vector3D(p.X * wInverse, p.Y * wInverse, p.Z * wInverse));
            return p;
        }

        public static double DistanceTo(this Vector3D point, Vector3D other)
        {
            double x = point.X - other.X;
            double y = point.Y - other.Y;
            double z = point.Z - other.Z;

            return System.Math.Sqrt(x * x + y * y + z * z);
        }

        public static double DistanceToSquared(this Vector3D point, Vector3D other)
        {
            double x = point.X - other.X;
            double y = point.Y - other.Y;
            double z = point.Z - other.Z;

            return x * x + y * y + z * z;
        }

        public static double DistanceTo(this Quaternion point, Quaternion other)
        {
            double x = point.X - other.X;
            double y = point.Y - other.Y;
            double z = point.Z - other.Z;
            double w = point.W - other.W;

            return System.Math.Sqrt(x * x + y * y + z * z + w * w);
        }

        public static double DistanceToSquared(this Quaternion point, Quaternion other)
        {
            double x = point.X - other.X;
            double y = point.Y - other.Y;
            double z = point.Z - other.Z;
            double w = point.W - other.W;

            return x * x + y * y + z * z + w * w;
        }

        public static double DistanceTo(this Line3D line, Vector3D point)
        {
            return Vector3D.CrossProduct(line.Direction, line.Origin - point).Length / line.Direction.Length; 
        }
        
        public static Vector3D ToVector3D(this Vector v)
        {
            return new Vector3D(v[0], v[1], v[2]);
        }

        public static Vector3D GetCenter(this IEnumerable<Vector3D> points)
        {
            return MathHelper.GetCenter(points);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Angle in radians</returns>
        public static double Angle(Vector3D a, Vector3D b)
        {
            return System.Math.Acos(Vector3D.DotProduct(a, b) / System.Math.Sqrt(a.LengthSquared * b.LengthSquared));
        }

        /// <summary>
        /// Adds 2 vectors. Checks dimensions.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Vector Add(this Vector x, Vector y)
        {
            int dim = x.Dimension;
            if (dim != y.Dimension)
            {
                throw new ArgumentException("Vectors need to have equal dimension!");
            }

            double[] ret = new double[dim];
            for (int i = 0; i < dim; i++)
            {
                ret[i] = x[i] + y[i];
            }

            return new Vector(ret, false);
        }

        /// <summary>
        /// Does not check for dimensions (slightly faster than Add)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Vector AddFast(this Vector x, Vector y, int dim)
        {
            double[] ret = new double[dim];
            for (int i = 0; i < dim; i++)
            {
                ret[i] = x[i] + y[i];
            }

            return new Vector(ret, false);
        }

        /// <summary>
        /// Subtracts 2 vectors. Checks dimensions.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Vector Subtract(this Vector x, Vector y)
        {
            int dim = x.Dimension;
            if (dim != y.Dimension)
            {
                throw new ArgumentException("Vectors need to have equal dimension!");
            }

            double[] ret = new double[dim];
            for (int i = 0; i < dim; i++)
            {
                ret[i] = x[i] - y[i];
            }

            return new Vector(ret, false);
        }

        /// <summary>
        /// Does not check for dimensions (slightly faster than Subtract)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Vector SubtractFast(this Vector x, Vector y, int dim)
        {
            double[] ret = new double[dim];
            for (int i = 0; i < dim; i++)
            {
                ret[i] = x[i] - y[i];
            }

            return new Vector(ret, false);
        }

        /// <summary>
        /// Dot product. Checks dimensions.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static double Dot(this Vector x, Vector y)
        {
            int dim = x.Dimension;
            if (dim != y.Dimension)
            {
                throw new ArgumentException("Vectors need to have equal dimension!");
            }

            double acc = 0;
            for (int i = 0; i < dim; i++)
            {
                acc += x[i] * y[i];
            }

            return acc;
        }

        public static void NormalizeInPlace(this Vector x)
        {
            double len = 1.0 / x.Norm;
            for (int i = 0; i < x.Dimension; i++) x[i] *= len;
        }

        /// <summary>
        /// Does not check for dimensions (slightly faster than Dot)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static double DotFast(this Vector x, Vector y, int dim)
        {
            double acc = 0;
            for (int i = 0; i < dim; i++)
            {
                acc += x[i] * y[i];
            }

            return acc;
        }

        public static void ModifyElements<T>(this IVector<T> v, Func<T, T> f)
        {
            for (int i = 0; i < v.Dimension; i++)
            {
                v[i] = f(v[i]);
            }
        }

        public static void ModifyElements<T>(this IVector<T> v, Func<T, int, T> f)
        {
            for (int i = 0; i < v.Dimension; i++)
            {
                v[i] = f(v[i], i);
            }
        }

        public static void SetRow(this Matrix m, int row, Vector v)
        {
            for (int i = 0; i < v.Dimension; i++) m[row, i] = v[i];
        }

        public static void SetColumn(this Matrix m, int column, Vector v)
        {
            for (int i = 0; i < v.Dimension; i++) m[i, column] = v[i];
        }

        public static double DistanceTo(this Vector x, Vector y)
        {
            double d = 0;

            for (int i = 0; i < x.Dimension; i++)
            {
                double t = x[i] - y[i];
                d += t * t;
            }

            return System.Math.Sqrt(d);
        }

        public static Vector GeometricalCenter(this IList<Vector> vectors)
        {
            if (vectors.Count == 0) throw new ArgumentException("vectors must be nonempty");
            int dim = vectors[0].Dimension;
            int count = 0;
            Vector center = new Vector(dim);

            for (int k = 0; k < vectors.Count; k++)
            {
                var v = vectors[k];
                count++;
                for (int i = 0; i < dim; i++)
                {
                    center[i] += v[i];
                }
            }
            var f = 1.0 / (double)count;
            for (int i = 0; i < dim; i++)
            {
                center[i] *= f;
            }
            return center;
        }

        public static Matrix Multiply(this Matrix x, Matrix y)
        {
            if (x.Cols != y.Rows)
                throw new Exception("Column count in first matrix must be equal to row count in second matrix");
            // this is B dot term_i multiplication
            var retRows = x.Rows;
            var retCols = y.Cols;


            var ret = new double[retRows, retCols];

            for (var i = 0; i != retRows; i++)
                for (var j = 0; j != retCols; j++)
                {
                    double acc = 0.0;
                    for (var k = 0; k != x.Cols; k++)
                        acc += x[i, k] * y[k, j];
                    ret[i, j] = acc;
                }
            return new Matrix(ret, false);
        }

        public static Vector Multiply(this Matrix x, Vector y)
        {
            if (x.Cols != y.Dimension)
                throw new Exception("Column count in first matrix must be equal to row count in second matrix");
            // this is B dot term_i multiplication
            var dim = x.Rows;


            var ret = new double[dim];

            for (var i = 0; i != dim; i++)
            {
                double acc = 0;
                for (var j = 0; j != y.Dimension; j++)
                {
                    acc += x[i, j] * y[j];
                }

                ret[i] = acc;
            }
            return new Vector(ret, false);
        }

        /// <summary>
        ///   LU decomposition of A.
        /// </summary>
        /// <param name = "m">The matrix to invert. This matrix is unchanged by this function.</param>
        /// <returns>A matrix of equal size to A that combines the L and U. Here the diagonals belongs to L and the U's diagonal elements are all 1.</returns>
        public static Matrix LUDecomposition(this Matrix m)
        {
            var size = m.Rows;
            if (size != m.Cols)
                throw new Exception("LU Decomposition can only be determined for square matrices.");
            var lu = m.Clone();
            // normalize row 0
            for (var i = 1; i < size; i++) lu[0, i] /= lu[0, 0];

            for (var i = 1; i < size; i++)
            {
                for (var j = i; j < size; j++)
                {
                    // do a column of L
                    var sum = 0.0;
                    for (var k = 0; k < i; k++)
                        sum += lu[j, k] * lu[k, i];
                    lu[j, i] -= sum;
                }
                if (i == size - 1) continue;
                for (var j = i + 1; j < size; j++)
                {
                    // do a row of U
                    var sum = 0.0;
                    for (var k = 0; k < i; k++)
                        sum += lu[i, k] * lu[k, j];
                    lu[i, j] =
                        (lu[i, j] - sum) / lu[i, i];
                }
            }
            return lu;
        }


        /// <summary>
        /// Computes LU decomposition of m "in place".
        /// </summary>
        /// <param name="m"></param>
        /// <returns>LU Decomposition of m stored in m.</returns>
        public static Matrix LUDecompositionInPlace(this Matrix m, int dimension = -1)
        {
            if (dimension == -1) dimension = m.Rows;
            var size = dimension;
            if (size > m.Cols) throw new Exception("LU Decomposition can only be determined for square matrices.");
            var lu = m;
            // normalize row 0
            for (var i = 1; i < size; i++) lu[0, i] /= lu[0, 0];

            for (var i = 1; i < size; i++)
            {
                for (var j = i; j < size; j++)
                {
                    // do a column of L
                    var sum = 0.0;
                    for (var k = 0; k < i; k++)
                        sum += lu[j, k] * lu[k, i];
                    lu[j, i] -= sum;
                }
                if (i == size - 1) continue;
                for (var j = i + 1; j < size; j++)
                {
                    // do a row of U
                    var sum = 0.0;
                    for (var k = 0; k < i; k++)
                        sum += lu[i, k] * lu[k, j];
                    lu[i, j] =
                        (lu[i, j] - sum) / lu[i, i];
                }
            }
            return lu;
        }

        public static Matrix Inverse(this Matrix x)
        {
            // this code is adapted from http://users.erols.com/mdinolfo/matrix.htm
            // one constraint/caveat in this function is that the diagonal elts. cannot
            // be zero.
            // if the matrix is not square or is less than B 2x2, 
            // then this function won't work
            if (x.Rows != x.Cols)
                throw new Exception("Matrix cannot be inverted. Can only invert square matrices.");
            var size = x.Rows;
            if (size == 1) return new Matrix(new[,] { { 1 / x[0, 0] } }, false);

            var lu = LUDecomposition(x);

            #region invert L

            for (var i = 0; i < size; i++)
                for (var j = i; j < size; j++)
                {
                    var t = 1.0;
                    if (i != j)
                    {
                        t = 0.0;
                        for (var k = i; k < j; k++)
                            t -= lu[j, k] * lu[k, i];
                    }
                    lu[j, i] = t / lu[j, j];
                }

            #endregion

            #region invert U

            for (var i = 0; i < size; i++)
                for (var j = i; j < size; j++)
                {
                    if (i == j) continue;
                    var sum = 0.0;
                    for (var k = i; k < j; k++)
                        sum += lu[k, j] * ((i == k) ? 1.0 : lu[i, k]);
                    lu[i, j] = -sum;
                }

            #endregion

            #region final inversion

            for (var i = 0; i < size; i++)
                for (var j = 0; j < size; j++)
                {
                    var sum = 0.0;
                    for (var k = ((i > j) ? i : j); k < size; k++)
                        sum += ((j == k) ? 1.0 : lu[j, k]) * lu[k, i];
                    lu[j, i] = sum;
                }

            #endregion

            return lu;
        }

        /// <summary>
        /// !!!! This does not work when there are zero diagonal elements, use at own risk.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector Solve(this Matrix m, Vector b)
        {
            return m.Inverse().Multiply(b);
        }

        static double Det2(double[,] m)
        {
            return m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0];
        }

        static double Det3(double[,] m)
        {
            return
               m[0, 0] * m[1, 1] * m[2, 2] + m[0, 1] * m[1, 2] * m[2, 0] + m[1, 0] * m[2, 1] * m[0, 2]
               - m[2, 0] * m[1, 1] * m[0, 2] - m[0, 1] * m[1, 0] * m[2, 2] - m[0, 0] * m[2, 1] * m[1, 2];
        }

        static double Det4(double[,] m)
        {
            return
               m[0, 3] * m[1, 2] * m[2, 1] * m[3, 0] - m[0, 2] * m[1, 3] * m[2, 1] * m[3, 0] -
               m[0, 3] * m[1, 1] * m[2, 2] * m[3, 0] + m[0, 1] * m[1, 3] * m[2, 2] * m[3, 0] +
               m[0, 2] * m[1, 1] * m[2, 3] * m[3, 0] - m[0, 1] * m[1, 2] * m[2, 3] * m[3, 0] -
               m[0, 3] * m[1, 2] * m[2, 0] * m[3, 1] + m[0, 2] * m[1, 3] * m[2, 0] * m[3, 1] +
               m[0, 3] * m[1, 0] * m[2, 2] * m[3, 1] - m[0, 0] * m[1, 3] * m[2, 2] * m[3, 1] -
               m[0, 2] * m[1, 0] * m[2, 3] * m[3, 1] + m[0, 0] * m[1, 2] * m[2, 3] * m[3, 1] +
               m[0, 3] * m[1, 1] * m[2, 0] * m[3, 2] - m[0, 1] * m[1, 3] * m[2, 0] * m[3, 2] -
               m[0, 3] * m[1, 0] * m[2, 1] * m[3, 2] + m[0, 0] * m[1, 3] * m[2, 1] * m[3, 2] +
               m[0, 1] * m[1, 0] * m[2, 3] * m[3, 2] - m[0, 0] * m[1, 1] * m[2, 3] * m[3, 2] -
               m[0, 2] * m[1, 1] * m[2, 0] * m[3, 3] + m[0, 1] * m[1, 2] * m[2, 0] * m[3, 3] +
               m[0, 2] * m[1, 0] * m[2, 1] * m[3, 3] - m[0, 0] * m[1, 2] * m[2, 1] * m[3, 3] -
               m[0, 1] * m[1, 0] * m[2, 2] * m[3, 3] + m[0, 0] * m[1, 1] * m[2, 2] * m[3, 3];
        }

        public static double Determinant(this Matrix m)
        {
            if (m.Rows != m.Cols) throw new ArgumentException("can't compute a determinant of a non-square matrix");

            if (m.Rows == 1) return m[0, 0];
            if (m.Rows == 2) return Det2(m.Data);
            if (m.Rows == 3) return Det3(m.Data);
            if (m.Rows == 4) return Det4(m.Data);

            Matrix lu = m.LUDecomposition();
            double det = 1;
            for (int i = 0; i < m.Rows; i++)
            {
                det *= lu[i, i];
            }

            return double.IsNaN(det) ? 0 : det;
        }

        public static double DestructiveDeterminant(this Matrix m, int dimension = -1)
        {
            if (dimension == -1) dimension = m.Rows;
            if (dimension > m.Cols) throw new ArgumentException("can't compute a determinant of a non-square matrix");

            if (dimension == 1) return m[0, 0];
            if (dimension == 2) return Det2(m.Data);
            if (dimension == 3) return Det3(m.Data);
            if (dimension == 4) return Det4(m.Data);

            Matrix lu = m.LUDecompositionInPlace(dimension);
            double det = 1;
            for (int i = 0; i < dimension; i++)
            {
                det *= lu[i, i];
            }

            return double.IsNaN(det) ? 0 : det;
        }

        public static Vector3D PointOnPlane(this Plane3D plane)
        {
            double x = 1, y = 1, z;
            z = -(plane.D + x * plane.A + y * plane.B) / plane.C;
            return new Vector3D(x, y, z);
        }

        public static Vector3D Intersect(this Plane3D plane, Line3D line)
        {
            var n = plane.Normal;
            var t = Vector3D.DotProduct(n, plane.PointOnPlane() - line.Origin) / Vector3D.DotProduct(n, line.Direction);
            return line.Interpolate(-t);
        }
        
        public static Vector3D ToVector(this Vector3D p)
        {
            return new Vector3D(p.X, p.Y, p.Z);
        }
    }
}