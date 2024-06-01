
namespace WebChemistry.Framework.Geometry.Triangulation.DH
{
    /// <summary>
    /// Takes care of recycling instances of the DisconnectedFace class
    /// </summary>
    class DisconnectedFaceFactory<T>
    {
        private DisconnectedFace<T> top;

        public DisconnectedFaceFactory() { }

        public DisconnectedFace<T> Create(Tetrahedron<T> tetrahedron, int face)
        {
            if (top == null)
            {
                return new DisconnectedFace<T>(tetrahedron, face);
            }
            else
            {
                DisconnectedFace<T> temp = top;
                top = top.Previous;
                return temp.Recycle(tetrahedron, face);
            }
        }

        public void Dispose(DisconnectedFace<T> t)
        {
            t.Previous = top;
            top = t;
        }
    }
}
