namespace WebChemistry.Framework.Math
{
    using System;

    public class Matrix : IMatrix<double>
    {
        double[,] data;

        public double this[int i, int j]
        {
            get { return data[i, j]; }
            set { data[i, j] = value; }
        }

        public double[,] Data
        {
            get { return data; }
        }

        public int Rows
        {
            get { return data.GetLength(0); }
        }

        public int Cols
        {
            get { return data.GetLength(1); }
        }

        public Matrix Clone()
        {
            return new Matrix(data);
        }
        
        public Matrix(double[,] data, bool copy = true)
        {
            if (copy)
            {
                this.data = new double[data.GetLength(0), data.GetLength(1)];
                Array.Copy(data, this.data, data.Length);
            }
            else
            {
                this.data = data;
            }
        }

        public Matrix(double[,] data)
            : this(data, true)
        {

        }

        public Matrix(int rows, int cols)
            : this(new double[rows,cols], false)
        {
        }

        public Matrix(int dim)
            : this(dim, dim)
        {
        }
    }
}
