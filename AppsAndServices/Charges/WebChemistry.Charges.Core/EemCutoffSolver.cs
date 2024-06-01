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

    public static partial class EemSolver
    {
        static ChargeComputationResult SolveCutoff(List<IAtom> atoms, Dictionary<IAtom, int> bondTypes,
            List<AtomPatition> partitions, EemChargeComputationParameters parameters, ComputationProgress progress)
        {
            var kd = K3DTree.Create(atoms, a => a.Position, pivotSelectionMethod: K3DPivotSelectionMethod.Average);

            progress.Update(isIndeterminate: false, currentProgress: 0, maxProgress: atoms.Count);

            var solver = parameters.Precision == ChargeComputationPrecision.Single
                ? (MatrixSolver)new WebChemistry.Charges.Core.Solvers.SingleSolver()
                : (MatrixSolver)new WebChemistry.Charges.Core.Solvers.DoubleSolver();

            var charges = new Dictionary<IAtom, ChargeValue>(atoms.Count);
            HashSet<string> warnings = new HashSet<string>();
            var sync = new object();

            var radius = parameters.CutoffRadius;
            //var minAtoms = (int)(radius * radius * 1.61);

            int visited = 0;

            var pivots = partitions.SelectMany(p => p.Atoms.Select(a => Tuple.Create(p, a))).ToList();
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
                    //if (neighbors.Count < minAtoms) neighbors = kd.NearestRadius(atom.Position, radius + 1);
                    HashSet<string> localWarnings;
                    var totalCharge = (double)neighbors.Count / (double)atoms.Count * parameters.TotalCharge;
                    var tmp = solver.Solve(neighbors, n => n.Value, parameters.Set, pivot.Item1.Parameters, bondTypes, totalCharge, out localWarnings);

                    if (progress.CancelRequested) return;

                    lock (sync)
                    {
                        progress.UpdateProgress(++visited);
                        warnings.UnionWith(localWarnings);
                        charges.Add(atom, new ChargeValue
                        {
                            Charge = tmp[atom],
                            Parameters = pivot.Item1.Parameters,
                            Multiplicity = bondTypes[atom]
                        });
                    }
                }
            });

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

        static ChargeComputationResult SolveCutoffCoalesceH(IStructure structure, List<IAtom> atoms, Dictionary<IAtom, int> bondTypes,
            List<AtomPatition> partitions, EemChargeComputationParameters parameters, ComputationProgress progress)
        {
            var kd = K3DTree.Create(atoms, a => a.Position, pivotSelectionMethod: K3DPivotSelectionMethod.Average);
            
            var charges = new Dictionary<IAtom, ChargeValue>(atoms.Count);
            HashSet<string> warnings = new HashSet<string>();
            var sync = new object();

            var radius = parameters.CutoffRadius;
            //var minAtoms = (int)(radius * radius * 1.61);

            int visited = 0;

            var allPivots = partitions.SelectMany(p => p.Atoms.Select(a => Tuple.Create(p, a))).ToList();
            var bonds = structure.Bonds;

            var pivots = allPivots.Where(p => 
                {
                    var a = p.Item2;
                    return a.ElementSymbol != ElementSymbols.H || bonds[a].Count == 0;
                })
                .ToArray();

            progress.Update(isIndeterminate: false, currentProgress: 0, maxProgress: pivots.Length);

            var solver = parameters.Precision == ChargeComputationPrecision.Single
                ? (MatrixSolver)new WebChemistry.Charges.Core.Solvers.SingleSolver()
                : (MatrixSolver)new WebChemistry.Charges.Core.Solvers.DoubleSolver();

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
                    //if (neighbors.Count < minAtoms) neighbors = kd.NearestRadius(atom.Position, radius + 1);
                    HashSet<string> localWarnings;
                    var totalCharge = (double)neighbors.Count / (double)atoms.Count * parameters.TotalCharge;
                    var tmp = solver.Solve(neighbors, n => n.Value, parameters.Set, pivot.Item1.Parameters, bondTypes, totalCharge, out localWarnings);
                    if (progress.CancelRequested) return;

                    var values = new List<Tuple<IAtom, ChargeValue>>(4);

                    values.Add(Tuple.Create(atom, new ChargeValue
                    {
                        Charge = tmp[atom],
                        Parameters = pivot.Item1.Parameters,
                        Multiplicity = bondTypes[atom]
                    }));


                    var hs = bonds[atom];
                    for (int i = 0; i < hs.Count; i++)
                    {
                        var b = hs[i].B;
                        double chrg;
                        if (b.ElementSymbol == ElementSymbols.H && tmp.TryGetValue(b, out chrg))
                        {
                            values.Add(Tuple.Create(b, new ChargeValue
                            {
                                Charge = tmp[b],
                                Parameters = pivot.Item1.Parameters,
                                Multiplicity = bondTypes[b]
                            }));
                        }
                    }
                    

                    lock (sync)
                    {
                        progress.UpdateProgress(++visited);
                        warnings.UnionWith(localWarnings);
                        foreach (var c in values)
                        {
                            charges.Add(c.Item1, c.Item2);
                        }
                    }
                }
            });

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
