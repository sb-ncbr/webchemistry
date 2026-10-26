namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Linq;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// A path through the whole structure along an axis, as the Pores tool of MOLEonline computed a pore outside
    /// of the membrane region.
    /// </summary>
    public partial class Complex
    {
        /// <summary>
        /// Tetrahedra of the largest cavity considered for each end of the path.
        /// </summary>
        const int AxisPathEndCandidates = 50;

        /// <summary>
        /// A path whose radius is at most NarrowRadius along MaxNarrowFraction of its profile is computed again
        /// with the VoronoiScale weight function.
        /// </summary>
        const double NarrowRadius = 0.5;
        const double MaxNarrowFraction = 0.2;

        /// <summary>
        /// Computes a path along the axis given by two points far from the structure, one on each side, and adds it
        /// to Paths. The path ends where the structure ends along the axis (on the planes perpendicular to the axis
        /// through the atoms nearest to the far points), in the tetrahedra of the largest cavity nearest to the axis
        /// there. A narrow path is computed again with the VoronoiScale weight function, and kept when that finds
        /// none. Returns null when the structure has no cavity or no path connects the ends.
        /// </summary>
        public Tunnel ComputeAxisPath(Vector3D farStart, Vector3D farEnd)
        {
            var cavity = Cavities.OrderByDescending(c => c.Volume).FirstOrDefault();
            if (cavity == null) return null;

            var start = Pivot(farStart, farEnd);
            var end = Pivot(farEnd, farStart);
            var axis = new Line3D(start, end);
            var tree = K3DTree.Create(cavity.Tetrahedrons, t => t.VoronoiCenter);
            var ends = Tuple.Create(NearestToAxis(tree, start, axis), NearestToAxis(tree, end, axis));

            var path = GetPaths(new[] { ends }).FirstOrDefault();
            if (path != null && IsNarrow(path) && Parameters.WeightFunction != TunnelWeightFunction.VoronoiScale)
            {
                var weightFunction = Parameters.WeightFunction;
                Parameters.WeightFunction = TunnelWeightFunction.VoronoiScale;
                try
                {
                    // Pores.exe had no path when this failed; the narrow one is kept instead
                    path = GetPaths(new[] { ends }).FirstOrDefault() ?? path;
                }
                finally
                {
                    Parameters.WeightFunction = weightFunction;
                }
            }

            if (path != null) Paths.Add(path);
            return path;
        }

        /// <summary>
        /// The point of the axis where the structure ends towards the far point.
        /// </summary>
        Vector3D Pivot(Vector3D far, Vector3D other)
        {
            var atom = KdTree.Nearest(far).Value.Position;
            var normal = other - far;
            return new Plane3D(normal.X, normal.Y, normal.Z, -Vector3D.DotProduct(normal, atom)).ProjectToPlane(far);
        }

        static Tetrahedron NearestToAxis(K3DTree<Tetrahedron> tree, Vector3D pivot, Line3D axis)
        {
            return tree.NearestCount(pivot, AxisPathEndCandidates)
                .Select(p => p.Value)
                .OrderBy(t => axis.DistanceTo(t.VoronoiCenter))
                .First();
        }

        static bool IsNarrow(Tunnel path)
        {
            var profile = path.Profile;
            return (double)profile.Count(n => n.Radius <= NarrowRadius) / profile.Count >= MaxNarrowFraction;
        }
    }
}
