
namespace WebChemistry.Framework.Math
{
    /// <summary>
    /// A representation of a 3D plane.
    /// </summary>
    public struct Plane3D
    {   
        public readonly double A;
        public readonly double B;
        public readonly double C;
        public readonly double D;

        /// <summary>
        /// Represents the plane as Ax + By + Cz + D = 0.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}x + {1}y + {2}z + {3} = 0", A, B, C, D);
        }

        /// <summary>
        /// Normal of the plane.
        /// </summary>
        public Vector3D Normal { get { return new Vector3D(A, B, C); } }
        
        /// <summary>
        /// Normal = (b - a) x (c - b)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Plane3D FromPoints(Vector3D a, Vector3D b, Vector3D c)
        {
            var n = Vector3D.CrossProduct(b - a, c - b).Normalize();
            var d = c.X * n.X + c.Y * n.Y + c.Z * n.Z;

            return new Plane3D(n.X, n.Y, n.Z, -d);
        }

        /// <summary>
        /// Calculates the absolute distance of a point to the plane.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double DistanceTo(Vector3D point)
        {
            return System.Math.Abs(A * point.X + B * point.Y + C * point.Z + D);
        }

        /// <summary>
        /// Calculates the "signed" distance of a point to the plane.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double DistanceToSigned(Vector3D point)
        {
            return A * point.X + B * point.Y + C * point.Z + D;
        }

        /// <summary>
        /// Projects the supplied point to the plane.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Vector3D ProjectToPlane(Vector3D p)
        {
            return p - DistanceToSigned(p) * Normal;
        }

        /// <summary>
        /// Returns angle in radians between the plane and the line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public double GetAngleInRadians(Line3D line)
        {
            var n = new Vector3D(A, B, C);
            var d = line.Direction;
            return System.Math.Asin(System.Math.Abs(Vector3D.DotProduct(n, d)) / System.Math.Sqrt(n.LengthSquared * d.LengthSquared));
        }
        
        /// <summary>
        /// Creates a representation of a plane a x + b y + c z + d = 0.
        /// Parameters get normalized.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public Plane3D(double a, double b, double c, double d)
        {
            var norm = System.Math.Sqrt(a * a + b * b + c * c);
            this.A = a / norm;
            this.B = b / norm;
            this.C = c / norm;
            this.D = d / norm;
        }
    }
}
