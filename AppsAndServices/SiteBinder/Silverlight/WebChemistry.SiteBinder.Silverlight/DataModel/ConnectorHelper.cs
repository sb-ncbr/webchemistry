using System;
using System.Linq;
using System.Collections.Generic;
using WebChemistry.Framework.Core;

namespace WebChemistry.SiteBinder.Silverlight.DataModel
{
    public static class ConnectorHelper
    {
        static Tuple<Dictionary<IAtom, int>, int> GetSelectedComponents(IStructure structure)
        {
            //var nodes = new LinkedList<IAtom>(structure.Atoms.Where(a => a.IsSelected));
            var atoms = structure.Atoms.Where(a => a.IsSelected).ToHashSet();
            var stack = new Stack<IAtom>();
            var atom = atoms.First();
            stack.Push(atom);
            atoms.Remove(atom);

            Dictionary<IAtom, int> components = new Dictionary<IAtom,int>();

            int maxComponent = 0;

            while (atoms.Count > 0 || stack.Count > 0)
            {
                if (stack.Count == 0)
                {
                    maxComponent++;
                    atom = atoms.First();
                    stack.Push(atom);
                    atoms.Remove(atom);
                    continue;
                }

                atom = stack.Pop();
                components.Add(atom, maxComponent);
                atoms.Remove(atom);

                var bonds = structure.Bonds[atom];
                for (int i = 0; i < bonds.Count; i++)
                {
                    var b = bonds[i].B;
                    if (b.IsSelected && atoms.Contains(b)) 
                    {
                        stack.Push(b);
                        atoms.Remove(b);
                    }
                }
            }

            return Tuple.Create(components, maxComponent + 1);
        }

        static bool SelectPath(IAtom start, HashSet<IAtom> target, HashSet<IAtom> selected, IStructure str)
        {
            var visited = new HashSet<IAtom>();
            var queue = new Queue<IAtom>();
            queue.Enqueue(start);

            Dictionary<IAtom, IAtom> pred = new Dictionary<IAtom, IAtom>();
            IAtom end = null;

            pred.Add(start, null);

            while (queue.Count > 0)
            {
                var atom = queue.Dequeue();
                if (target.Contains(atom))
                {
                    end = atom;
                    break;
                }

                var bonds = str.Bonds[atom];
                for (int i = 0; i < bonds.Count; i++)
                {
                    var other = bonds[i].B;
                    if (!pred.ContainsKey(other)) 
                    {
                        queue.Enqueue(other);
                        pred.Add(other, atom);
                    }
                }
            }

            if (end != null)
            {
                var current = end;
                while (current != null)
                {
                    selected.Add(current);
                    current = pred[current];
                }
                return true;
            }

            return false;
        }

        public static HashSet<IAtom> GetConnectedSelection(IStructure s)
        {
            if (s.Atoms.Count == 0) return null;

            var components = GetSelectedComponents(s);

            if (components.Item2 == 0) return null;
            if (components.Item2 == 1) return new HashSet<IAtom>();

            var selection = new HashSet<IAtom>();

            var sets = components.Item1
                .GroupBy(c => c.Value)
                .Select(g => g.Select(v => v.Key).ToHashSet())
                .ToList();

            var pivot = sets.MinBy(t => t.Count)[0];

            sets.Remove(pivot);

            foreach (var atom in pivot)
            {
                foreach (var set in sets)
                {
                    if (!SelectPath(atom, set, selection, s)) return null;
                }
            }

            return selection;
        }
    }
}
