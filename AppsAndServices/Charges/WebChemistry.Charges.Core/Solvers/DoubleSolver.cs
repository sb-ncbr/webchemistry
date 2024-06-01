namespace WebChemistry.Charges.Core.Solvers
{
    using MathNet.Numerics.LinearAlgebra.Double;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;

    class DoubleSolver : MatrixSolver
    {
        public override Dictionary<IAtom, double> Solve<T>(
            IList<T> atoms,
            Func<T, IAtom> selector,
            EemParameterSet parentSet,
            EemParameterSet.ParameterGroup parameters,
            Dictionary<IAtom, int> atomTypes,
            double totalCharge,
            out HashSet<string> warnings)
        {
            int count = atoms.Count;

            warnings = new HashSet<string>();

            var pivots = new List<IAtom>(atoms.Count);
            var diagonal = new List<double>(atoms.Count);
            var resultVector = new List<double>(atoms.Count);

            for (int i = 0; i < count; i++)
            {
                var atom = selector(atoms[i]);
                var mult = atomTypes[atom];
                var value = parameters.GetParam(atom, mult);

                if (value == null)
                {
                    warnings.Add("Missing parameters for symbol '" + atom.ElementSymbol.ToString() + "'. Atom skipped.");
                    continue;
                }
                if (value.Multiplicity != mult)
                {
                    if (EemSolver.IsAtomWater(atom)) warnings.Add("Missing parameters for symbol '" + atom.ElementSymbol.ToString() + "' (Water) and multiplicity " + mult + ". Using value for multiplicity " + value.Multiplicity + " instead.");
                    else warnings.Add("Missing parameters for symbol '" + atom.ElementSymbol.ToString() + "' and multiplicity " + mult + ". Using value for multiplicity " + value.Multiplicity + " instead.");
                }

                pivots.Add(atom);

                double param = parentSet.ABFactor * value.B;
                diagonal.Add(param);

                param = -parentSet.ABFactor * value.A;
                resultVector.Add(param);
            }

            resultVector.Add(totalCharge);

            count = pivots.Count;

            var eemMatrix = new DenseMatrix(count + 1, count + 1);
            var b = resultVector.ToArray();

            for (int i = 0; i < count; i++)
            {
                eemMatrix[i, i] = diagonal[i];
                eemMatrix[count, i] = -1;
                eemMatrix[i, count] = -1;
            }

            var kappa = parentSet.KappaFactor * parameters.Kappa;
            for (int i = 0; i < count - 1; i++)
            {
                var atom1 = pivots[i].Position;
                for (int j = i + 1; j < count; j++)
                {
                    double d = atom1.DistanceTo(pivots[j].Position);
                    double param = kappa / d;
                    eemMatrix[i, j] = param;
                    eemMatrix[j, i] = param;
                }
            }

            b[count] = -totalCharge;


            var provider = MathNet.Numerics.Control.LinearAlgebraProvider;
            var ipvt = new int[count + 1];

#if SILVERLIGHT
            provider.LUFactor(eemMatrix.Data, count + 1, ipvt);
            provider.LUSolveFactored(1, eemMatrix.Data, count + 1, ipvt, b);
#else
            provider.LUFactor(eemMatrix.Values, count + 1, ipvt);
            provider.LUSolveFactored(1, eemMatrix.Values, count + 1, ipvt, b);
#endif

            return pivots.Zip(b, (a, c) => new { A = a, C = c }).ToDictionary(a => a.A, a => a.C);
        }  
    }
}
