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
    using System.Threading.Tasks;
    
    /// <summary>
    /// Eem charge solver.
    /// </summary>
    public static partial class EemSolver
    {
        public static int MaxDegreeOfParallelism = 8;
        public static int MaxFullEemProblemSize = 6000;

        public static readonly AppVersion Version = new AppVersion(1, 0, 21, 12, 14, 'a');
        
        public static bool IsAtomWater(IAtom atom)
        {
            var name = atom.PdbResidueName();

            if (name.Length == 0) return false;
            //HOH, WAT, H2O
            switch (name[0])
            {
                case 'H': return name.Equals("HOH", StringComparison.OrdinalIgnoreCase) || name.Equals("H2O", StringComparison.OrdinalIgnoreCase);
                case 'W': return name.Equals("WAT", StringComparison.OrdinalIgnoreCase);
                default: return false;
            }
        }
       
        static ChargeComputationResult SolveEem(List<IAtom> atoms, Dictionary<IAtom, int> bondTypes,
            List<AtomPatition> partitions, EemChargeComputationParameters parameters, ComputationProgress progress)
        {
            progress.Update(isIndeterminate: true);

            HashSet<string> warnings = new HashSet<string>();
            Dictionary<IAtom, ChargeValue> charges = new Dictionary<IAtom, ChargeValue>(atoms.Count);

            var solver = parameters.Precision == ChargeComputationPrecision.Single
                ? (MatrixSolver)new WebChemistry.Charges.Core.Solvers.SingleSolver()
                : (MatrixSolver)new WebChemistry.Charges.Core.Solvers.DoubleSolver();

            foreach (var part in partitions)
            {
                progress.ThrowIfCancellationRequested();

                HashSet<string> localWarnings;
                var sol = solver.Solve(atoms, a => a, parameters.Set, part.Parameters, bondTypes, parameters.TotalCharge, out localWarnings);
                warnings.UnionWith(localWarnings);

                foreach (var atom in part.Atoms)
                {
                    double charge;
                    if (sol.TryGetValue(atom, out charge)) charges.Add(atom, new ChargeValue
                    {
                        Charge = charge,
                        Parameters = part.Parameters,
                        Multiplicity = bondTypes[atom]
                    });
                }
            }

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

        static void CorrectCharges(Dictionary<IAtom, ChargeValue> charges, double expectedTotal)
        {
            var delta = (expectedTotal - charges.Sum(c => c.Value.Charge)) / charges.Count;
            foreach (var c in charges) c.Value.CorrectCharge(c.Value.Charge + delta);
        }

        class AtomPatition
        {
            public EemParameterSet.ParameterGroup Parameters;
            public List<IAtom> Atoms;
        }

        static List<AtomPatition> PartitionAtoms(IStructure structure, HashSet<IAtom> atoms, EemParameterSet set)
        {
            HashSet<IAtom> addedAtoms = new HashSet<IAtom>();
            
            var parts = set.ParameterGroups.Select(g => new
            {
                Atoms = g.TargetQuery.Matches(structure).SelectMany(m => m.Atoms).Distinct(a => a.Id).Where(a => atoms.Contains(a)).ToList(),
                Parameters = g
            })
            .ToList();

            List<AtomPatition> ret = new List<AtomPatition>();

            foreach (var part in parts)
            {
                var ap = new AtomPatition
                {
                    Parameters = part.Parameters,
                    Atoms = part.Atoms.Where(a => !addedAtoms.Contains(a)).ToList()
                };

                ap.Atoms.ForEach(a => addedAtoms.Add(a));

                if (ap.Atoms.Count > 0)
                {
                    ret.Add(ap);
                }
            }

            return ret;
        }

        internal static int GetBondType(IList<IBond> bonds, HashSet<IAtom> atoms)
        {
            if (bonds == null || bonds.Count == 0) return 0;

            int ret = 0;
            for (int i = 0; i < bonds.Count; i++)
            {
                var b = bonds[i];
                if (!atoms.Contains(b.B)) continue;

                int val = b.Type == BondType.Metallic ? 1 : (int)b.Type;
                if (val > ret) ret = val;
            }
            return ret;
        }

        public static ChargeComputationResult Compute(EemChargeComputationParameters parameters, ComputationProgress progress = null)
        {
            try
            {
                var started = DateTime.Now;
                if (progress == null) progress = ComputationProgress.DummyInstance;

                progress.Update(isIndeterminate: true);

                var structure = parameters.Structure;
                var pivots = parameters.SelectionOnly ? parameters.AtomSelection.AsEnumerable() : structure.Atoms.AsEnumerable();
                if (parameters.IgnoreWaters)
                {
                    pivots = pivots.Where(a => !EemSolver.IsAtomWater(a));
                }

                var pivotList = pivots.ToList();

                if (parameters.Method == ChargeComputationMethod.Eem && pivotList.Count > MaxFullEemProblemSize)
                {
                    return new ChargeComputationResult(new Dictionary<IAtom,ChargeValue>())
                    {
                        State = ChargeResultState.Error,
                        Messages = new[] { string.Format("The atom count is too large to be solved using the EEM method ({0}, limit is {1}). Use the cutoff method instead.",
                                    pivotList.Count, MaxFullEemProblemSize) },
                        Parameters = parameters
                    };
                }

                if (pivotList.Count == 0)
                {
                    throw new InvalidOperationException("No atoms to compute charges from. Did you select them?");
                }

                var pivotSet = pivotList.ToHashSet();
                var paritions = PartitionAtoms(parameters.Structure, pivotSet, parameters.Set);
                var paramSet = parameters.Set;
                var bondTypes = pivotList.ToDictionary(a => a, a => GetBondType(structure.Bonds[a], pivotSet));

                ChargeComputationResult result;
                if (parameters.Method == ChargeComputationMethod.Eem) result = SolveEem(pivotList, bondTypes, paritions, parameters, progress);
                else if (parameters.Method == ChargeComputationMethod.EemCutoff) result = SolveCutoff(pivotList, bondTypes, paritions, parameters, progress);
                else if (parameters.Method == ChargeComputationMethod.EemCutoffCoalesceH) result = SolveCutoffCoalesceH(parameters.Structure, pivotList, bondTypes, paritions, parameters, progress);
                else result = SolveCover(parameters.Structure, pivotList, bondTypes, paritions, parameters, progress);

                progress.Update(isIndeterminate: true);
                result.Timing = DateTime.Now - started;
                result.Multiplicities = bondTypes;                
                return result;
            }
            catch (ComputationCancelledException)
            {
                throw;
            }
            catch (AggregateException e)
            {
                var msg = "";
                if (e.InnerExceptions != null && e.InnerExceptions.Count > 0) msg = e.InnerExceptions.JoinBy(x => x.Message, "; ");
                else if (e.InnerException != null) msg = e.InnerException.Message;
                else msg = e.Message;
                return new ChargeComputationResult(new Dictionary<IAtom, ChargeValue>())
                {
                    State = ChargeResultState.Error,
                    Messages = new[] { msg },
                    Parameters = parameters
                };
            }
            catch (Exception e)
            {
                return new ChargeComputationResult(new Dictionary<IAtom, ChargeValue>())
                {
                    State = ChargeResultState.Error,
                    Messages = new[] { e.Message },
                    Parameters = parameters
                };
            }
        }
    }
}
