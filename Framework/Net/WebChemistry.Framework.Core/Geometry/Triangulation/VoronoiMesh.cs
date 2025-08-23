namespace WebChemistry.Framework.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// A factory class for creating a Voronoi mesh.
    /// </summary>
    public static class VoronoiMesh3D
    {
        /// <summary>
        /// Create the voronoi mesh.
        /// </summary>
        /// <typeparam name="TVertex"></typeparam>
        /// <typeparam name="TCell"></typeparam>
        /// <typeparam name="TEdge"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static VoronoiMesh3D<TVertex, TCell, TEdge> Create<TVertex, TCell, TEdge>(IEnumerable<TVertex> data, Func<TVertex, Vector3D> positionSelector)
            where TCell : TriangulationCell3D<TVertex, TCell>, new()
            where TEdge : VoronoiEdge3D<TVertex, TCell>, new()
        {
            return VoronoiMesh3D<TVertex, TCell, TEdge>.Create(data, positionSelector);
        }

        /// <summary>
        /// Create the voronoi mesh.
        /// </summary>
        /// <typeparam name="TVertex"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static VoronoiMesh3D<TVertex, DefaultTriangulationCell3D<TVertex>, VoronoiEdge3D<TVertex, DefaultTriangulationCell3D<TVertex>>> Create<TVertex>(IEnumerable<TVertex> data, Func<TVertex, Vector3D> positionSelector)
        {
            return VoronoiMesh3D<TVertex, DefaultTriangulationCell3D<TVertex>, VoronoiEdge3D<TVertex, DefaultTriangulationCell3D<TVertex>>>.Create(data, positionSelector);
        }

        /// <summary>
        /// Create the voronoi mesh.
        /// </summary>
        /// <typeparam name="TVertex"></typeparam>
        /// <typeparam name="TCell"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static VoronoiMesh3D<TVertex, TCell, VoronoiEdge3D<TVertex, TCell>> Create<TVertex, TCell>(IEnumerable<TVertex> data, Func<TVertex, Vector3D> positionSelector)
            where TCell : TriangulationCell3D<TVertex, TCell>, new()
        {
            return VoronoiMesh3D<TVertex, TCell, VoronoiEdge3D<TVertex, TCell>>.Create(data, positionSelector);
        }
    }

    /// <summary>
    /// A representation of a voronoi mesh.
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TCell"></typeparam>
    /// <typeparam name="TEdge"></typeparam>
    public class VoronoiMesh3D<TVertex, TCell, TEdge>
        where TCell : TriangulationCell3D<TVertex, TCell>, new()
        where TEdge : VoronoiEdge3D<TVertex, TCell>, new()
    {
        /// <summary>
        /// This is probably not needed, but might make things a tiny bit faster.
        /// </summary>
        class EdgeComparer : IEqualityComparer<TEdge>
        {
            public bool Equals(TEdge x, TEdge y)
            {
                return (x.Source == y.Source && x.Target == y.Target) || (x.Source == y.Target && x.Target == y.Source);
            }

            public int GetHashCode(TEdge obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Cells of the diagram.
        /// </summary>
        public ReadOnlyCollection<TCell> Vertices { get; private set; }

        /// <summary>
        /// Edges connecting the cells. 
        /// The same information can be retrieved Cells' Adjacency.
        /// </summary>
        public ReadOnlyCollection<TEdge> Edges { get; private set; }
        
        /// <summary>
        /// Create a voronoi diagram of the input data.
        /// </summary>
        /// <param name="data"></param>
        public static VoronoiMesh3D<TVertex, TCell, TEdge> Create(IEnumerable<TVertex> data, Func<TVertex, Vector3D> positionSelector)
        {
            if (data == null) throw new ArgumentNullException("data can't be null");
            
            var t = DelaunayTriangulation3D<TVertex, TCell>.Create(data, positionSelector); 
            var vertices = t.Cells;
            var edges = new HashSet<TEdge>(new EdgeComparer());
            
            foreach (var f in vertices)
            {
                for (int i = 0; i < f.Adjacency.Length; i++)
                {
                    var af = f.Adjacency[i];
                    if (af != null) edges.Add(new TEdge { Source = f, Target = af });
                }
            }

            return new VoronoiMesh3D<TVertex, TCell, TEdge> 
            {
                Vertices = vertices, 
                Edges = new ReadOnlyCollection<TEdge>(edges.ToList())
            };
        }
        
        /// <summary>
        /// Can only be created using a factory method.
        /// </summary>
        private VoronoiMesh3D()
        {

        }
    }
}
