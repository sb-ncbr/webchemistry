// -----------------------------------------------------------------------
// <copyright file="Matching.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.SiteBinder.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Core;

    public enum PivotType
    {
        Average,
        SpecificStructure
    }

    public enum MatchMethod
    {
        Subgraph,
        Combinatorial
    }

    public class MatchParameters
    {
        public static readonly MatchParameters Default = new MatchParameters();

        public PivotType PivotType { get; set; }
        public int PivotIndex { get; set; }        
        public MatchMethod Method { get; set; }

        public MatchParameters()
        {
            PivotIndex = -1;
            Method = MatchMethod.Subgraph; 
            PivotType = Core.PivotType.Average;
        }
    }

    public class Matching
    {
        public static MultipleMatching<IAtom> MatchStructures(IList<IStructure> structures, MatchParameters parameters = null)
        {
            if (parameters == null) parameters = MatchParameters.Default;

            var graphs = structures.Select(s => MatchGraph.Create(s, true)).ToArray();
            
            var match = MultipleMatching<IAtom>.Find(graphs, pivotType: parameters.PivotType, pivotIndex: parameters.PivotIndex, pairMethod: parameters.Method);
            return match;
        }
    }

    public static class SiteBinderVersion
    {
        public static readonly AppVersion Version = new AppVersion(2, 0, 21, 12, 14, 'a');
    }
}
