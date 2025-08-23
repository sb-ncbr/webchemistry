
namespace WebChemistry.Framework.Geometry.Triangulation.DH
{
    /// <summary>
    /// Represents one triangulation cell
    /// </summary>
    class Tetrahedron<T>
    {
        // Precomputed values
        private double MinorsUnit, MinorsX, MinorsY, MinorsZ;
        public double MinorsQ;

        // Vertices
        public TriangulationVertex<T> V0, V1, V2, V3, Opposite;

        // Neighbours
        public Tetrahedron<T> N0, N1, N2, N3;

        // Helper fields for creation of linked lists
        public Tetrahedron<T> Previous, Next;

        public double LastDeterminant;
        public bool Precomputed;
        public bool Infinite;
        public bool Marked;
        public bool LocalFlag;

        /// <summary>
        /// Helper.
        /// </summary>
        public int Tag;

        public Tetrahedron(TriangulationVertex<T> v0, TriangulationVertex<T> v1, TriangulationVertex<T> v2, TriangulationVertex<T> v3, TriangulationVertex<T> opposite)
        {
            V0 = v0; V1 = v1; V2 = v2; V3 = v3;
            if (V0.Unit == 0 || V1.Unit == 0 || V2.Unit == 0 || V3.Unit == 0) Infinite = true;
            Opposite = opposite;
        }

        public double SphereDeterminant(TriangulationVertex<T> vertex)
        {
            if (!Precomputed) Precompute();
            return (MinorsUnit - MinorsX * vertex.X + MinorsY * vertex.Y - MinorsZ * vertex.Z + MinorsQ * vertex.LengthSquared);
        }

        public Tetrahedron<T> Recycle(TriangulationVertex<T> v0, TriangulationVertex<T> v1, TriangulationVertex<T> v2, TriangulationVertex<T> v3, TriangulationVertex<T> opposite)
        {
            V0 = v0; V1 = v1; V2 = v2; V3 = v3;
            Infinite = (V0.Unit == 0 || V1.Unit == 0 || V2.Unit == 0 || V3.Unit == 0);
            N0 = N1 = N2 = N3 = null;
            Opposite = opposite;
            Precomputed = Marked = LocalFlag = false;
            return this;
        }

        public void VertexUpdated()
        {
            Infinite = (V0.Unit == 0 || V1.Unit == 0 || V2.Unit == 0 || V3.Unit == 0);
            Precomputed = false;
        }

        public void UpdateLink(Tetrahedron<T> oldLink, Tetrahedron<T> newLink) {
            if (N0 == oldLink) N0 = newLink;
            else if (N1 == oldLink) N1 = newLink;
            else if (N2 == oldLink) N2 = newLink;
            else N3 = newLink;
        }

        private void Precompute()
        {
            Precomputed = true;

            double Minors00 = V0.Unit * V1.X - V0.X * V1.Unit;
            double Minors01 = V0.Unit * V1.Y - V0.Y * V1.Unit;
            double Minors02 = V0.Unit * V1.Z - V0.Z * V1.Unit;
            double Minors03 = V0.Unit * V1.LengthSquared - V0.LengthSquared * V1.Unit;
            double Minors04 = V0.X * V1.Y - V0.Y * V1.X;
            double Minors05 = V0.X * V1.Z - V0.Z * V1.X;
            double Minors06 = V0.X * V1.LengthSquared - V0.LengthSquared * V1.X;
            double Minors07 = V0.Y * V1.Z - V0.Z * V1.Y;
            double Minors08 = V0.Y * V1.LengthSquared - V0.LengthSquared * V1.Y;
            double Minors09 = V0.Z * V1.LengthSquared - V0.LengthSquared * V1.Z;

            double Minors10 = V2.Unit * V3.X - V2.X * V3.Unit;
            double Minors11 = V2.Unit * V3.Y - V2.Y * V3.Unit;
            double Minors12 = V2.Unit * V3.Z - V2.Z * V3.Unit;
            double Minors13 = V2.Unit * V3.LengthSquared - V2.LengthSquared * V3.Unit;
            double Minors14 = V2.X * V3.Y - V2.Y * V3.X;
            double Minors15 = V2.X * V3.Z - V2.Z * V3.X;
            double Minors16 = V2.X * V3.LengthSquared - V2.LengthSquared * V3.X;
            double Minors17 = V2.Y * V3.Z - V2.Z * V3.Y;
            double Minors18 = V2.Y * V3.LengthSquared - V2.LengthSquared * V3.Y;
            double Minors19 = V2.Z * V3.LengthSquared - V2.LengthSquared * V3.Z;

            MinorsUnit = Minors04 * Minors19 - Minors05 * Minors18 + Minors06 * Minors17 + Minors07 * Minors16 - Minors08 * Minors15 + Minors09 * Minors14;
            MinorsX = Minors01 * Minors19 - Minors02 * Minors18 + Minors03 * Minors17 + Minors07 * Minors13 - Minors08 * Minors12 + Minors09 * Minors11;
            MinorsY = Minors00 * Minors19 - Minors02 * Minors16 + Minors03 * Minors15 + Minors05 * Minors13 - Minors06 * Minors12 + Minors09 * Minors10;
            MinorsZ = Minors00 * Minors18 - Minors01 * Minors16 + Minors03 * Minors14 + Minors04 * Minors13 - Minors06 * Minors11 + Minors08 * Minors10;
            MinorsQ = Minors00 * Minors17 - Minors01 * Minors15 + Minors02 * Minors14 + Minors04 * Minors12 - Minors05 * Minors11 + Minors07 * Minors10;

            if (MinorsQ < 0)
            {
                MinorsUnit = -MinorsUnit;
                MinorsX = -MinorsX;
                MinorsY = -MinorsY;
                MinorsZ = -MinorsZ;
                MinorsQ = -MinorsQ;
            }
            else if (Infinite && MinorsUnit - MinorsX * Opposite.X + MinorsY * Opposite.Y - MinorsZ * Opposite.Z < 0)
            {
                MinorsUnit = -MinorsUnit;
                MinorsX = -MinorsX;
                MinorsY = -MinorsY;
                MinorsZ = -MinorsZ;
            }
        }
    }
}
