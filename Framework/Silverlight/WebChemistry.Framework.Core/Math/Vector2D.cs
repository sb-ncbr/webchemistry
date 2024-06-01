namespace WebChemistry.Framework.Math
{
    public struct Vector2D
    {
        private double x;
        private double y;

        public Vector2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        #region Properties

        public double X
        {
            get
            {
                return this.x;
            }
            set
            {
                this.x = value;
            }
        }

        public double Y
        {
            get
            {
                return this.y;
            }
            set
            {
                this.y = value;
            }
        }
        
        public double Length
        {
            get
            {
                return MathHelper.SquareRoot(this.x * this.x + this.y * this.y);
            }
        }

        public double LengthSquared
        {
            get
            {
                return this.x * this.x + this.y * this.y;
            }
        }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            int hash = 31;
            hash = unchecked(23 * hash + x.GetHashCode());
            hash = unchecked(23 * hash + y.GetHashCode());
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Vector2D))
            {
                return false;
            }

            return Equals(this, (Vector2D)obj);
        }

        public bool Equals(Vector2D v)
        {
            return Equals(this, v);
        }

        public static bool Equals(Vector2D v1, Vector2D v2)
        {
            //TODO: Use this?
            /*
            return MathHelper.AreEqual(v1.x, v2.x) &&
                   MathHelper.AreEqual(v1.y, v2.y)*/

            return v1.x.Equals(v2.x) &&
                   v1.y.Equals(v2.y);
        }

        public override string ToString()
        {
            return X + ", " + Y;
        }

        public static double DotProduct(Vector2D v1, Vector2D v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }
        
        public void Negate()
        {
            this.x *= -1;
            this.y *= -1;
        }

        public void Normalize()
        {
            double lengthSquared = this.LengthSquared;
            double inverseSquareRoot = MathHelper.InverseSquareRoot(lengthSquared);
            this.x *= inverseSquareRoot;
            this.y *= inverseSquareRoot;
        }
        
        public static Vector2D Add(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.x + v2.x, v1.y + v2.y);
        }
        
        public static Vector2D operator +(Vector2D v1, Vector2D v2)
        {
            return Add(v1, v2);
        }
        
        public static Vector2D Subtract(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.x - v2.x, v1.y - v2.y);
        }
        
        public static Vector2D operator -(Vector2D v1, Vector2D v2)
        {
            return Subtract(v1, v2);
        }

        public static Vector2D Multiply(double scalar, Vector2D v)
        {
            return new Vector2D(v.x * scalar, v.y * scalar);
        }

        public static Vector2D Multiply(Vector2D v, double scalar)
        {
            return new Vector2D(v.x * scalar, v.y * scalar);
        }

        public static Vector2D operator *(double scalar, Vector2D v)
        {
            return Multiply(scalar, v);
        }

        public static Vector2D operator *(Vector2D v, double scalar)
        {
            return Multiply(v, scalar);
        }

        public static Vector2D Divide(Vector2D v, double scalar)
        {
            double div = 1.0 / scalar;
            return new Vector2D(v.x * div, v.y * div);
        }

        public static Vector2D operator /(Vector2D v, double scalar)
        {
            return Divide(v, scalar);
        }

        public static bool operator ==(Vector2D v1, Vector2D v2)
        {
            return Equals(v1, v2);
        }

        public static bool operator !=(Vector2D v1, Vector2D v2)
        {
            return !Equals(v1, v2);
        }

        //public static explicit operator Point(Vector2D v)
        //{
        //    return new Point(v.x, v.y);
        //}

        public static Vector2D operator -(Vector2D v)
        {
            return new Vector2D(-v.x, -v.y);
        }

        #endregion
    }
}