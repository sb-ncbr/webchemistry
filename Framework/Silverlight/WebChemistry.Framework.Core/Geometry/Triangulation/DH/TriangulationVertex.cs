
namespace WebChemistry.Framework.Geometry.Triangulation.DH
{
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Represents one vertex in the triangulation
    /// </summary>
    class TriangulationVertex<T>
    {
        // 3 coordinates + two 
        public readonly double X, Y, Z, LengthSquared, Unit;

        // Pre-computed hash code
        public readonly int Index;

        // Data associated with this vertex
        public readonly T Value;

        public TriangulationVertex(T value, Vector3D position, int index)
        {
            Value = value;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            Unit = 1;
            LengthSquared = X*X + Y*Y + Z*Z;
            this.Index = index;
        }
        public TriangulationVertex() {
            LengthSquared = 1;
            this.Index = -1;
        }
    }
}
