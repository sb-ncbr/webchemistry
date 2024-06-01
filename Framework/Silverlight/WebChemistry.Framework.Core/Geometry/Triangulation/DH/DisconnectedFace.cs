
namespace WebChemistry.Framework.Geometry.Triangulation.DH
{
    using System;

    /// <summary>
    /// Represents one side of a tetrahedron
    /// </summary>
    class DisconnectedFace<T>
    {
        public DisconnectedFace(Tetrahedron<T> tetrahedron, int face)
        {
            Recycle(tetrahedron, face);
        }

        public DisconnectedFace<T> Recycle(Tetrahedron<T> tetrahedron, int face)
        {
            Tetrahedron = tetrahedron;
            Face = face;

            switch (face)
            {
                case 0: this.V0 = tetrahedron.V1.Index; V1 = tetrahedron.V2.Index; V2 = tetrahedron.V3.Index; break;
                case 1: V0 = tetrahedron.V0.Index; V1 = tetrahedron.V2.Index; V2 = tetrahedron.V3.Index; break;
                case 2: V0 = tetrahedron.V0.Index; V1 = tetrahedron.V1.Index; V2 = tetrahedron.V3.Index; break;
                case 3: V0 = tetrahedron.V0.Index; V1 = tetrahedron.V1.Index; V2 = tetrahedron.V2.Index; break;
                default: throw new ArgumentException("recycle face: invalid face index");
            }

            if (this.V1 < this.V0)
            {
                var t = this.V1;
                this.V1 = this.V0;
                this.V0 = t;
            }
            if (this.V2 < this.V1)
            {
                var t = this.V2;
                this.V2 = this.V1;
                this.V1 = t;
            }
            if (this.V1 < this.V0)
            {
                var t = this.V1;
                this.V1 = this.V0;
                this.V0 = t;
            }

            int hash = 23;
            hash = 31 * hash + V0;
            hash = 31 * hash + V1;
            hash = 31 * hash + V2;

            Hash = hash;

            return this;
        }

        public Tetrahedron<T> Tetrahedron;
        public int V0, V1, V2;
        public int Face;
        public int Hash;

        // Helper field for creation of linked lists
        public DisconnectedFace<T> Previous;
    }
}
