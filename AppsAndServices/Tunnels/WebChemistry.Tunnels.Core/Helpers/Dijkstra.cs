using System.Collections.Generic;

namespace WebChemistry.Tunnels.Core.Helpers
{
    internal class Dijkstra
    {
        double GetDistance(Tetrahedron t, Dictionary<Tetrahedron, double> distances)
        {
            double d;
            if (distances.TryGetValue(t, out d)) return d;
            return double.MaxValue;
        }

        public static IEnumerable<Tetrahedron> FindPath(Cavity cavity, Tetrahedron from, Tetrahedron to)
        {
            //Dictionary<Tetrahedron, double> distances = new Dictionary<Tetrahedron,double>(cavity.Tetrahedrons.Count);
            //Dictionary<Tetrahedron, Tetrahedron> pred = new Dictionary<Tetrahedron, Tetrahedron>(cavity.Tetrahedrons.Count);
            //FibonacciHeap<double, Tetrahedron> queue = new FibonacciHeap<double,Tetrahedron>();
            //queue.Enqueue(0, from);

            //while (queue.Count > 0)
            //{

            //}

            return null;
        }
    }
}
