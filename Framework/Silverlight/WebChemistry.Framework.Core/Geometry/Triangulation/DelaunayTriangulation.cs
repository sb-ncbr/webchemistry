namespace WebChemistry.Framework.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using WebChemistry.Framework.Geometry.Triangulation.DH;
    using WebChemistry.Framework.Math;
    
    public class DelaunayTriangulation3D<TVertex, TCell> : ITriangulation3D<TVertex, TCell>
        where TCell : TriangulationCell3D<TVertex, TCell>, new()
    {
        /// <summary>
        /// Cells of the triangulation.
        /// </summary>
        public ReadOnlyCollection<TCell> Cells { get; private set; }

        public static DelaunayTriangulation3D<TVertex, TCell> Create(IEnumerable<TVertex> data, Func<TVertex, Vector3D> positionSelector)
        {
            if (data == null) throw new ArgumentException("data can't be null.");
            if (!(data is IList<TVertex>)) data = data.ToArray();
            if (!data.Any()) return new DelaunayTriangulation3D<TVertex, TCell> { Cells = new ReadOnlyCollection<TCell>(new List<TCell>()) };

            var tri = DHTriangulation<TVertex>.Create(data, positionSelector);

            // Create the "TCell" representation.
            int cellCount = tri.Simplices.Count;
            var faces = new List<Tetrahedron<TVertex>>();
            var cells = new List<TCell>(cellCount);

            const int dimension = 3;
            var delaunayFaces = tri.Simplices;

            for (int i = 0; i < cellCount; i++)
            {
                var face = delaunayFaces[i];

                if (face.Infinite) continue;

                faces.Add(face);

                var vertices = new TVertex[] { face.V0.Value, face.V1.Value, face.V2.Value, face.V3.Value };
                cells.Add(new TCell
                {
                    Vertices = vertices,
                    Adjacency = new TCell[dimension + 1]
                });
                face.Tag = cells.Count - 1;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                var face = faces[i];
                var cell = cells[i];

                if (!face.N0.Infinite) cell.Adjacency[0] = cells[face.N0.Tag];
                if (!face.N1.Infinite) cell.Adjacency[1] = cells[face.N1.Tag];
                if (!face.N2.Infinite) cell.Adjacency[2] = cells[face.N2.Tag];
                if (!face.N3.Infinite) cell.Adjacency[3] = cells[face.N3.Tag];
            }

            return new DelaunayTriangulation3D<TVertex, TCell> { Cells = new ReadOnlyCollection<TCell>(cells) };
        }

        /// <summary>
        /// Can only be created using a factory method.
        /// </summary>
        private DelaunayTriangulation3D()
        {

        }
    }
}
