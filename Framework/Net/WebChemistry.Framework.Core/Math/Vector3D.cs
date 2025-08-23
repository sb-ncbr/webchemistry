// Taken from Kit3D library (kit3d.codeplex.com)

namespace WebChemistry.Framework.Math
{
    using System;

    [Newtonsoft.Json.JsonConverter(typeof(WebChemistry.Framework.Core.Json.Vector3DJsonConverter))]
    public struct Vector3D : IEquatable<Vector3D>
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Vector3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        #region Properties

        public double Length
        {
            get
            {
                return MathHelper.SquareRoot(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
            }
        }

        public double LengthSquared
        {
            get
            {
                return this.X * this.X + this.Y * this.Y + this.Z * this.Z;
            }
        }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            int hash = 31;
            hash = unchecked(23 * hash + X.GetHashCode());
            hash = unchecked(23 * hash + Y.GetHashCode());
            hash = unchecked(23 * hash + Z.GetHashCode());
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Vector3D))
            {
                return false;
            }

            return Equals(this, (Vector3D)obj);
        }

        public bool Equals(Vector3D v)
        {
            return Equals(this, v);
        }

        public static bool Equals(Vector3D v1, Vector3D v2)
        {
            //TODO: Use this?
            /*
            return MathHelper.AreEqual(v1.x, v2.x) &&
                   MathHelper.AreEqual(v1.y, v2.y) &&
                   MathHelper.AreEqual(v1.z, v2.z);*/

            return v1.X.Equals(v2.X) &&
                   v1.Y.Equals(v2.Y) &&
                   v1.Z.Equals(v2.Z);
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}]", X, Y, Z);
        }

        public static double DotProduct(Vector3D v1, Vector3D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static Vector3D CrossProduct(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.Y * v2.Z - v1.Z * v2.Y,
                                v1.Z * v2.X - v1.X * v2.Z,
                                v1.X * v2.Y - v1.Y * v2.X);
        }
                
        public Vector3D Normalize()
        {
            double lengthSquared = this.LengthSquared;
            double inverseSquareRoot = MathHelper.InverseSquareRoot(lengthSquared);
            return new Vector3D(this.X * inverseSquareRoot, this.Y * inverseSquareRoot, this.Z * inverseSquareRoot);
        }

        public Vector3D ScaleTo(double size)
        {
            double inverseSquareRoot = size / Math.Sqrt(LengthSquared);
            return new Vector3D(this.X * inverseSquareRoot, this.Y * inverseSquareRoot, this.Z * inverseSquareRoot);
        }
        
        public static Vector3D Add(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector3D operator +(Vector3D v1, Vector3D v2)
        {
            return Add(v1, v2);
        }
        
        public static Vector3D Subtract(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vector3D operator -(Vector3D v1, Vector3D v2)
        {
            return Subtract(v1, v2);
        }

        public static Vector3D Multiply(double scalar, Vector3D v)
        {
            return new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vector3D Multiply(Vector3D v, double scalar)
        {
            return new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vector3D operator *(double scalar, Vector3D v)
        {
            return Multiply(scalar, v);
        }

        public static Vector3D operator *(Vector3D v, double scalar)
        {
            return Multiply(v, scalar);
        }

        public static Vector3D Divide(Vector3D v, double scalar)
        {
            double div = 1.0 / scalar;
            return new Vector3D(v.X * div, v.Y * div, v.Z * div);
        }

        public static Vector3D operator /(Vector3D v, double scalar)
        {
            return Divide(v, scalar);
        }

        public static bool operator ==(Vector3D v1, Vector3D v2)
        {
            return Equals(v1, v2);
        }

        public static bool operator !=(Vector3D v1, Vector3D v2)
        {
            return !Equals(v1, v2);
        }
        
        public static Vector3D operator -(Vector3D v)
        {
            return new Vector3D(-v.X, -v.Y, -v.Z);
        }

        public Vector3D Negate()
        {
            return new Vector3D(-this.X, -this.Y, -this.Z);
        }

        #endregion
    }
}
