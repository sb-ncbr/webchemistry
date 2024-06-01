namespace WebChemistry.Charges.Core
{
    using System;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;

    abstract class MatrixSolver
    {
        public abstract Dictionary<IAtom, double> Solve<T>(
            IList<T> atoms,
            Func<T, IAtom> selector,
            EemParameterSet parentSet,
            EemParameterSet.ParameterGroup parameters,
            Dictionary<IAtom, int> atomTypes,
            double totalCharge,
            out HashSet<string> warnings);
    }
}
