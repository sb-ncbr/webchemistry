namespace WebChemistry.Framework.Geometry
{
    /// <summary>
    /// A  triangulation cell.
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TCell"></typeparam>
    public abstract class TriangulationCell3D<TVertex, TCell>
        where TCell : TriangulationCell3D<TVertex, TCell>
    {
        /// <summary>
        /// Adjacency. Array of length "dimension".
        /// If F = Adjacency[i] then the vertices shared with F are Vertices[j] where j != i.
        /// In the context of triangulation, can be null (indicates the cell is at boundary).
        /// </summary>
        public TCell[] Adjacency { get; set; }

        /// <summary>
        /// Tetrahedron vertices.
        /// </summary>
        public TVertex[] Vertices { get; set; }
    }

    /// <summary>
    /// Default triangulation cell.
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    public class DefaultTriangulationCell3D<TVertex> : TriangulationCell3D<TVertex, DefaultTriangulationCell3D<TVertex>>
    {
    }
}
