namespace WebChemistry.Framework.Math
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a double vector
    /// </summary>
    public class Vector : IVector<double>
    {
        double[] data;
        public double[] Data
        {
            get { return data; }
        }

        /// <summary>
        /// Creates an instance of a vector
        /// </summary>
        /// <param name="data"></param>
        /// <param name="copy">determine whether to copy the data or not</param>
        public Vector(double[] data, bool copy = true)
        {
            if (copy)
            {
                this.data = new double[data.Length];
                Array.Copy(data, this.data, data.Length);
            }
            else 
            {
                this.data = data;
            }
        }

        public Vector(IEnumerable<double> data)
            : this(data.ToArray(), false)
        {
        }

        public Vector(int dim)
            : this(new double[dim], false)
        {
        }

        public Vector(params double[] coords)
            : this(coords, false)
        {

        }

        public void Resize(int newDimension)
        {
            Array.Resize(ref data, newDimension);
        }

        public int Dimension
        {
            get { return data.Length; }
        }

        public double Norm
        {
            get
            {
                double acc = 0;
                int dim = data.Length;
                for (int i = 0; i < dim; i++)
                {
                    double t = data[i];
                    acc += t * t;
                }

                return System.Math.Sqrt(acc);
            }
        }
        
        public double this[int i]
        {
            get { return data[i]; }
            set { data[i] = value; }
        }
    }
}
