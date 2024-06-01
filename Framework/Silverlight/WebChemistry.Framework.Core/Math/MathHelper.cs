namespace WebChemistry.Framework.Math
{
    using System.Collections.Generic;
    using System.Linq;
    
    public static class MathHelper
    {
        //TODO: Verify this constant is okay to use, too small?
        public const double Epsilon = 0.000001d;

        public const double OneDivThree = 1.0 / 3;

        public const double PiDiv180 = System.Math.PI / 180.0;

        public static double DegreesToRadians(double angle)
        {
            return angle * PiDiv180;
        }

        public static double RadiansToDegrees(double angle)
        {
            return angle / PiDiv180;
        }

        public static int AngleQuadrant(double degrees)
        {
            if (degrees == -90) return 4;
            if (degrees == 90) return 2;

            if (degrees >= 0 && degrees <= 90) return 1;
            if (degrees >= 0 && degrees < 180) return 2;
            if (degrees < 0 && degrees >= -90) return 3;
            return 4;
        }

        public static bool IsZero(double value)
        {
            return System.Math.Abs(value) < Epsilon;
        }

        public static bool AreEqual(double x, double y)
        {
            return IsZero(x - y);
        }

        public static double SquareRoot(double value)
        {
            return System.Math.Sqrt(value);
        }

        public static double InverseSquareRoot(double value)
        {
            return (1.0 / System.Math.Sqrt(value));
        }

        //public static void SphereFromPoints(Vector3D[] points, out Vector3D center, out double radius)
        //{
        //    Matrix m = new Matrix(4);
            
        //    // x, y, z, 1
        //    for (int i = 0; i < 4; i++)
        //    {
        //        m[i, 0] = points[i].X;
        //        m[i, 1] = points[i].Y;
        //        m[i, 2] = points[i].Z;
        //        m[i, 3] = 1;
        //    }
        //    var a = m.Determinant();

        //    // size, y, z, 1
        //    m[0, 0] = points[0].LengthSquared;
        //    m[1, 0] = points[1].LengthSquared;
        //    m[2, 0] = points[2].LengthSquared;
        //    m[3, 0] = points[3].LengthSquared;
        //    var dx = m.Determinant();

        //    // size, x, z, 1
        //    m[0, 1] = points[0].X;
        //    m[1, 1] = points[1].X;
        //    m[2, 1] = points[2].X;
        //    m[3, 1] = points[3].X;
        //    var dy = -m.Determinant();

        //    // size, x, y, 1
        //    m[0, 2] = points[0].Y;
        //    m[1, 2] = points[1].Y;
        //    m[2, 2] = points[2].Y;
        //    m[3, 2] = points[3].Y;
        //    var dz = m.Determinant();

        //    // size, x, y, z
        //    m[0, 3] = points[0].Z;
        //    m[1, 3] = points[1].Z;
        //    m[2, 3] = points[2].Z;
        //    m[3, 3] = points[3].Z;
        //    var c = m.Determinant();

        //    var s = 1.0 / (2.0 * a);
        //    var r = System.Math.Abs(s) * System.Math.Sqrt(dx * dx + dy * dy + dz * dz - 4 * a * c);
        //    center = new Vector3D(s * dx, s * dy, s * dz);
        //    radius = r;
        //}

        /// <summary>
        /// Computes the center and radius of a sphere determined by 4 points.
        /// Implementation of http://mathworld.wolfram.com/Circumsphere.html.
        /// </summary>
        /// <param name="points">Array of 4 points</param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns>Center and radius of the sphere.</returns>
        public static void SphereFromPoints(Vector3D[] points, out Vector3D center, out double radius)
        {
            Matrix3D m = new Matrix3D(
                points[0].X, points[0].Y, points[0].Z, 1,
                points[1].X, points[1].Y, points[1].Z, 1,
                points[2].X, points[2].Y, points[2].Z, 1, 
                points[3].X, points[3].Y, points[3].Z, 1);

            var a = m.Determinant();

            // size, y, z, 1
            m.M11 = points[0].LengthSquared;
            m.M21 = points[1].LengthSquared;
            m.M31 = points[2].LengthSquared;
            m.OffsetX = points[3].LengthSquared;
            var dx = m.Determinant();

            // size, x, z, 1
            m.M12 = points[0].X;
            m.M22 = points[1].X;
            m.M32 = points[2].X;
            m.OffsetY = points[3].X;
            var dy = -m.Determinant();

            // size, x, y, 1
            m.M13 = points[0].Y;
            m.M23 = points[1].Y;
            m.M33 = points[2].Y;
            m.OffsetZ = points[3].Y;
            var dz = m.Determinant();

            // size, x, y, z
            m.M14 = points[0].Z;
            m.M24 = points[1].Z;
            m.M34 = points[2].Z;
            m.M44 = points[3].Z;
            var c = m.Determinant();

            var s = 1.0 / (2.0 * a);
            var r = System.Math.Abs(s) * System.Math.Sqrt(dx * dx + dy * dy + dz * dz - 4 * a * c);
            center = new Vector3D(s * dx, s * dy, s * dz);
            radius = r;
        }

        public static Vector3D GetCenter(IEnumerable<Vector3D> points)
        {
            double ws = 1.0 / points.Count();
            return (Vector3D)(ws * points.Aggregate<Vector3D, Vector3D>(new Vector3D(), (c, a) => c + a));
        }
    }
}
