
namespace WebChemistry.Framework.Geometry.Triangulation.DH
{
    /// <summary>
    /// Takes care of recycling instances of the DisconnectedFace class
    /// </summary>
    class TetrahedronFactory<T>
    {
        private Tetrahedron<T> top;

        public TetrahedronFactory()
        {
        }

        public Tetrahedron<T> Create(TriangulationVertex<T> v0, TriangulationVertex<T> v1, TriangulationVertex<T> v2, TriangulationVertex<T> v3, TriangulationVertex<T> opposite)
        {
            if (top == null)
            {
                return new Tetrahedron<T>(v0, v1, v2, v3, opposite);
            }
            else
            {
                Tetrahedron<T> temp = top;
                top = top.Previous;
                return temp.Recycle(v0, v1, v2, v3, opposite);
            }
        }

        public void Dispose(Tetrahedron<T> t)
        {
            t.Previous = top;
            top = t;
        }
    }
}
