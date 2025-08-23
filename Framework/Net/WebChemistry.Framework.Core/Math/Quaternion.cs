
namespace WebChemistry.Framework.Math
{
    public struct Quaternion
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;
        public readonly double W;

        public Quaternion(double x, double y, double z, double w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        #region Methods

        public override int GetHashCode()
        {
            return this.X.GetHashCode() ^
                   this.Y.GetHashCode() ^
                   this.Z.GetHashCode() ^
                   this.W.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Quaternion))
            {
                return false;
            }

            return Equals(this, (Quaternion)obj);
        }

        public bool Equals(Quaternion q)
        {
            return Equals(this, q);
        }

        public static bool Equals(Quaternion q1, Quaternion q2)
        {
            //TODO: Use this?
            /*
            return MathHelper.AreEqual(v1.x, v2.x) &&
                   MathHelper.AreEqual(v1.y, v2.y) &&
                   MathHelper.AreEqual(v1.z, v2.z);*/

            return q1.X.Equals(q2.X) &&
                   q1.Y.Equals(q2.Y) &&
                   q1.Z.Equals(q2.Z) &&
                   q1.W.Equals(q2.W);
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z + ", " + W;
        }

        public static double DotProduct(Quaternion q1, Quaternion q2)
        {
            return q1.X * q2.X + q1.Y * q2.Y + q1.Z * q2.Z + q1.W * q2.W;
        }

        //public void Negate()
        //{
        //    this.x *= -1;
        //    this.y *= -1;
        //    this.z *= -1;
        //    this.w *= -1;
        //}

        public Quaternion Conjugate()
        {
            return new Quaternion(-X, -Y, -Z, W);
        }

        //public void Normalize()
        //{
        //    double lengthSquared = this.LengthSquared;
        //    double inverseSquareRoot = MathHelper.InverseSquareRoot(lengthSquared);
        //    this.X *= inverseSquareRoot;
        //    this.Y *= inverseSquareRoot;
        //    this.Z *= inverseSquareRoot;
        //    this.W *= inverseSquareRoot;
        //}

        public Matrix3D ToRotationMatrix()
        {
            double qW = this.W;
            double qX = this.X;
            double qY = this.Y;
            double qZ = this.Z;

            double n1 = 2 * qY * qY;
            double n2 = 2 * qZ * qZ;
            double n3 = 2 * qX * qX;
            double n4 = 2 * qX * qY;
            double n5 = 2 * qW * qZ;
            double n6 = 2 * qX * qZ;
            double n7 = 2 * qW * qY;
            double n8 = 2 * qY * qZ;
            double n9 = 2 * qW * qX;

            Matrix3D result = Matrix3D.Identity;
            result.M11 = 1 - n1 - n2;
            result.M12 = n4 + n5;
            result.M13 = n6 - n7;
            result.M21 = n4 - n5;
            result.M22 = 1 - n3 - n2;
            result.M23 = n8 + n9;
            result.M31 = n6 + n7;
            result.M32 = n8 - n9;
            result.M33 = 1 - n3 - n1;
            result.M44 = 1;

            return result;
        }

        public Vector3D Transform(Vector3D p)
        {
            var r = this * new Quaternion(p.X, p.Y, p.Z, 0) * this.Conjugate();
            return new Vector3D(r.X, r.Y, r.Z);
        }
        
        public Matrix3D RightMultiplicationToMatrix()
        {
            return new Matrix3D(W, -X, -Y, -Z, 
                                X,  W,  Z, -Y, 
                                Y, -Z,  W,  X,
                                Z,  Y, -X,  W);
        }

        public Matrix3D LeftMultiplicationToMatrix()
        {
            return new Matrix3D(W, -X, -Y, -Z,
                                X, W, -Z, Y,
                                Y, Z, W, -X,
                                Z, -Y, X, W);
        }

        public static Quaternion Add(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z, q1.W + q2.W);
        }

        public static Quaternion operator +(Quaternion q1, Quaternion q2)
        {
            return Add(q1, q2);
        }

        public static Quaternion Subtract(Quaternion q1, Quaternion q2)
        {
             return new Quaternion(q1.X - q2.X, q1.Y - q2.Y, q1.Z - q2.Z, q1.W - q2.W);
        }

        public static Quaternion operator -(Quaternion q1, Quaternion q2)
        {
            return Subtract(q1, q2);
        }

        public static Quaternion Multiply(double scalar, Quaternion q)
        {
            return new Quaternion(q.X * scalar, q.Y * scalar, q.Z * scalar, q.W * scalar);
        }

        public static Quaternion Multiply(Quaternion q, double scalar)
        {
            return new Quaternion(q.X * scalar, q.Y * scalar, q.Z * scalar, q.W * scalar);
        }

        public static Quaternion operator *(double scalar, Quaternion v)
        {
            return Multiply(scalar, v);
        }

        public static Quaternion Multiply(Quaternion q1, Quaternion q2)
        {
            double x = q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y;
            double y = q1.W * q2.Y - q1.X * q2.Z + q1.Y * q2.W + q1.Z * q2.X;
            double z = q1.W * q2.Z + q1.X * q2.Y - q1.Y * q2.X + q1.Z * q2.W;
            double w = q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z;

            return new Quaternion(x, y, z, w);
        }

        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            return Multiply(q1, q2);
        }

        public static Quaternion operator *(Quaternion v, double scalar)
        {
            return Multiply(v, scalar);
        }

        public static Quaternion Divide(Quaternion q, double scalar)
        {
            double div = 1.0 / scalar;
            return new Quaternion(q.X * div, q.Y * div, q.Z * div, q.W * div);
        }

        public static Quaternion operator /(Quaternion v, double scalar)
        {
            return Divide(v, scalar);
        }

        public static bool operator ==(Quaternion v1, Quaternion v2)
        {
            return Equals(v1, v2);
        }

        public static bool operator !=(Quaternion v1, Quaternion v2)
        {
            return !Equals(v1, v2);
        }

        public static Quaternion operator -(Quaternion q)
        {
            return new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
        }

        #endregion
    }
}
