namespace WebChemistry.Charges.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;
    using MathNet.Numerics.LinearAlgebra.Double;
    using WebChemistry.Framework.Core.Pdb;
#if SILVERLIGHT
    using PortableTPL;
#else
    using System.Threading.Tasks;
#endif

    class Vector3DComparer : IComparer<Vector3D>
    {
        public int Compare(Vector3D x, Vector3D y)
        {
            var cx = x.X.CompareTo(y.X);
            if (cx == 0)
            {
                var cy = x.Y.CompareTo(y.Y);
                if (cy == 0)
                {
                    return x.Z.CompareTo(y.Z);
                }
                return cy;
            }
            return cx;
        }
    }

    public static partial class EemSolver
    {
        static List<IAtom> MakeCover(IStructure structure, IEnumerable<IAtom> atoms)
        {
            var nodeMap = new Dictionary<IAtom, LinkedListNode<IAtom>>();
            var nodes = new LinkedList<IAtom>();
            var bonds = structure.Bonds;

            foreach (var a in atoms.OrderByDescending(a => bonds[a].Count).ThenBy(a => a.Position, new Vector3DComparer()))
            {
                var node = nodes.AddLast(a);
                nodeMap.Add(a, node);
            }

            var cover = new List<IAtom>();
            while (nodes.Count > 0)
            {
                var pivot = nodes.First;
                nodes.RemoveFirst();
                cover.Add(pivot.Value);
                foreach (var bond in bonds[pivot.Value])
                {
                    var b = bond.B;
                    LinkedListNode<IAtom> node;
                    if (nodeMap.TryGetValue(b, out node) && node.List != null) nodes.Remove(node); 
                }
            }

            return cover;
        }
        
        class CoverCell
        {
            public int Count { get; set; }
            public double ChargeTotal { get; set; }
            public EemParameterSet.ParameterGroup Parameters { get; set; }

            public void Update(double value, EemParameterSet.ParameterGroup prms)
            {
                if (Parameters == null) Parameters = prms;
                Count++;
                ChargeTotal += value;
            }
        }

        static ChargeComputationResult SolveCover(IStructure structure, List<IAtom> atoms, Dictionary<IAtom, int> bondTypes,
            List<AtomPatition> partitions, EemChargeComputationParameters parameters, ComputationProgress progress)
        {
            var kd = K3DTree.Create(atoms, a => a.Position, pivotSelectionMethod: K3DPivotSelectionMethod.Average);

            var solver = parameters.Precision == ChargeComputationPrecision.Single
                ? (MatrixSolver)new WebChemistry.Charges.Core.Solvers.SingleSolver()
                : (MatrixSolver)new WebChemistry.Charges.Core.Solvers.DoubleSolver();

            HashSet<string> warnings = new HashSet<string>();
            var sync = new object();

            var radius = parameters.CutoffRadius;
            //var minAtoms = (int)(radius * radius * 1.61);

            int visited = 0;

            var affectedAtoms = partitions.SelectMany(p => p.Atoms).ToHashSet();

            var bonds = structure.Bonds;
            var pivots = partitions
                .SelectMany(p => MakeCover(structure, p.Atoms).Select(a => Tuple.Create(p, a)))
                .Where(a => a.Item2.ElementSymbol != ElementSymbols.H || bonds[a.Item2].Count == 0)
                .ToList();
            progress.Update(isIndeterminate: false, currentProgress: 0, maxProgress: pivots.Count);

            var cells = affectedAtoms.ToDictionary(a => a, _ => new CoverCell());
            

#if SILVERLIGHT
            ParallelOptions parOptions = new ParallelOptions();
#else
            ParallelOptions parOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism };
#endif
            Parallel.ForEach(pivots, parOptions, pivot =>
            {
                progress.ThrowIfCancellationRequested();

                var atom = pivot.Item2;
                if (pivot.Item1.Parameters.GetParam(atom, bondTypes[atom]) == null)
                {
                    lock (sync)
                    {
                        warnings.Add("Missing parameters for symbol '" + atom.ElementSymbol.ToString() + "'. Atom skipped.");
                        progress.UpdateProgress(++visited);
                    }
                    return;
                }
                else
                {
                    var neighbors = kd.NearestRadius(atom.Position, radius);
                    HashSet<string> localWarnings;
                    var totalCharge = (double)neighbors.Count / (double)atoms.Count * parameters.TotalCharge;
                    var tmp = solver.Solve(neighbors, n => n.Value, parameters.Set, pivot.Item1.Parameters, bondTypes, totalCharge, out localWarnings);

                    if (progress.CancelRequested) return;

                    IAtom[] constituent;

                    if (bonds[atom].Count > 0)
                    {
                        var constituent1 = bonds[atom].Select(b => b.B).ToArray();
                        var constituent2 = constituent1.ToHashSet();
                        constituent1.SelectMany(a => bonds[a].Select(b => b.B)).ForEach(a => constituent2.Add(a));
                        constituent = constituent2.ToArray();
                    }
                    else
                    {
                        constituent = new[] { atom };
                    }
                    
                    lock (sync)
                    {
                        progress.UpdateProgress(++visited);
                        warnings.UnionWith(localWarnings);
                        
                        foreach (var a in constituent)
                        {
                            double val;
                            if (!tmp.TryGetValue(a, out val) || !affectedAtoms.Contains(a)) continue;

                            var cell = cells[a];
                            cell.Update(val, pivot.Item1.Parameters);
                        }

                    }
                }
            });

            var charges = cells
                .Where(c => c.Value.Count > 0)
                .ToDictionary(
                    c => c.Key,
                    c => new ChargeValue
                    {
                        Charge = c.Value.ChargeTotal / c.Value.Count,
                        Parameters = c.Value.Parameters,
                        Multiplicity = bondTypes[c.Key]
                    });

            //charges.Add(atom, new ChargeValue
            //{
            //    Charge = tmp[atom],
            //    Parameters = pivot.Item1.Parameters,
            //    Multiplicity = bondTypes[atom]
            //});

            if (parameters.CorrectCutoffTotalCharge) CorrectCharges(charges, parameters.TotalCharge);

            if (warnings.Count == 0)
            {
                return new ChargeComputationResult(charges)
                {
                    State = ChargeResultState.Ok,
                    Parameters = parameters
                };
            }
            else
            {
                return new ChargeComputationResult(charges)
                {
                    State = ChargeResultState.Warning,
                    Messages = warnings.OrderBy(w => w).ToArray(),
                    Parameters = parameters
                };
            }
        }
    }
}
