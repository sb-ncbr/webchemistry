namespace WebChemistry.Framework.Geometry
{
    /// <summary>
    /// A class representing an (undirected) edge of the Voronoi graph.
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TCell"></typeparam>
    public class VoronoiEdge3D<TVertex, TCell>
        where TCell : TriangulationCell3D<TVertex, TCell>
    {
        /// <summary>
        /// Source of the edge.
        /// </summary>
        public TCell Source
        {
            get;
            internal set;
        }

        /// <summary>
        /// Target of the edge.
        /// </summary>
        public TCell Target
        {
            get;
            internal set;
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as VoronoiEdge3D<TVertex, TCell>;
            if (other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            return (Source == other.Source && Target == other.Target)
                || (Source == other.Target && Target == other.Source);
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            //int hash = 23;
            //hash = hash * 31 + Source.GetHashCode();
            //return hash * 31 + Target.GetHashCode();

            long i = Source.GetHashCode();
            long j = Target.GetHashCode();

            long key = i > j ? (j << 32) | i : (i << 32) | j;
            key = (~key) + (key << 18); // key = (key << 18) - key - 1;
            key = key ^ (key >> 31);
            key = key * 21; // key = (key + (key << 2)) + (key << 4);
            key = key ^ (key >> 11);
            key = key + (key << 6);
            key = key ^ (key >> 22);
            return (int)key;
        }

        /// <summary>
        /// Create an instance of the edge.
        /// </summary>
        public VoronoiEdge3D()
        {

        }

        /// <summary>
        /// Create an instance of the edge.
        /// </summary>
        public VoronoiEdge3D(TCell source, TCell target)
        {
            this.Source = source;
            this.Target = target;
        }
    }
}
