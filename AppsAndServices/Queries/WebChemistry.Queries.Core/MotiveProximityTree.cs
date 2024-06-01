namespace WebChemistry.Queries.Core
{
    using System.Linq;
    using WebChemistry.Framework.Core;
    using System.Collections.Generic;
    using WebChemistry.Framework.Geometry;
    
    /// <summary>
    /// Wraps a motive in a kd-tree and recods the maximum radius.
    /// </summary>
    public class MotiveProximityTree
    {
        K3DTree<Motive> tree;
        double maxRadius;

        /// <summary>
        /// Approximate the motives
        /// </summary>
        /// <param name="maxDistance"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public IEnumerable<Motive> GetCloseMotives(double maxDistance, Motive m)
        {
            var center = m.Center;
            var near = tree.NearestRadius(center, maxDistance + m.Radius + maxRadius);
            for (int i = 0; i < near.Count; i++)
            {
                var c = near[i];
                if (Motive.AreNear(maxDistance, m, c.Value)) yield return c.Value;
            }
        }

        public Motive GetNearestMotive(Motive m)
        {
            var center = m.Center;
            var near = tree.Nearest(center);
            return near.Value;
        }

        /// <summary>
        /// Approximate the motives
        /// </summary>
        /// <param name="maxDistance"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public IEnumerable<Motive> GetConenctedMotives(double maxDistance, Motive m, bool exclusive = false)
        {
            var center = m.Center;
            maxDistance = maxDistance + m.Radius + maxRadius;
            var near = tree.NearestRadius(center, maxDistance);
            for (int i = 0; i < near.Count; i++)
            {
                var c = near[i];
                if (Motive.AreConnected(m, c.Value, exclusive: exclusive)) yield return c.Value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="motives"></param>
        public MotiveProximityTree(IEnumerable<Motive> motives)
        {
            this.maxRadius = double.MinValue;

            var ma = motives.AsList();
            tree = new K3DTree<Motive>(motives, m => m.Center, method: K3DPivotSelectionMethod.Average);

            for (int i = 0; i < ma.Count; i++)
            {
                var r = ma[i].Radius;
                if (r > maxRadius) maxRadius = r;
            }
        }
    }
}
