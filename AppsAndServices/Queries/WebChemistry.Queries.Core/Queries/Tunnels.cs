namespace WebChemistry.Queries.Core.Queries
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Core.Utils;
    using System.Reflection;
    using WebChemistry.Tunnels.Core;

    /// <summary>
    /// Find tunnels using MOLE
    /// </summary>
    class TunnelsQuery : QueryMotive
    {
        QueryMotive Where;
        QueryMotive Start;
        double ProbeRadius, InteriorThreshold, BottleneckRadius;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {

            List<Tunnel> tunnels = new List<Tunnel>();

            var ctx = context.RequestCurrentContext();
            foreach (var parent in Where.ExecuteMotive(context))
            {

                var startMotives = Start.ExecuteMotive(context);
                if (startMotives.Count() == 0) continue;

                var complex = Complex.Create(parent.ToStructure(ctx.Structure.Id, true, true), new ComplexParameters { ProbeRadius = ProbeRadius, InteriorThreshold = InteriorThreshold, BottleneckRadius = BottleneckRadius });
                int originCount = 0;
                foreach (var m in startMotives)
                {
                    var center = m.Atoms.GeometricalCenter();
                    originCount += complex.TunnelOrigins.AddFromPoint(center).Count;
                }

                if (originCount == 0) continue;
                foreach (var o in complex.TunnelOrigins.OfType(TunnelOriginType.User))
                {
                    complex.ComputeTunnels(o);
                }

                tunnels.AddRange(complex.Tunnels);
            }

            return tunnels.Select(t => Motive.FromAtoms(t.Lining.SelectMany(r => r.Atoms), null, ctx)).ToArray();
        }

        protected override string ToStringInternal()
        {
            return NameHelper("Tunnels",
                new[] { NameOption("ProbeRadius", ProbeRadius), NameOption("InteriorThreshold", InteriorThreshold), NameOption("BottleneckRadius", BottleneckRadius) },
                new[] { Where.ToString(), Start.ToString() });
        }

        public TunnelsQuery(QueryMotive where, QueryMotive start, double probeRadius, double interiorThreshold, double bottleneckRadius)
        {
            this.Where = where;
            this.Start = start;
            this.ProbeRadius = probeRadius;
            this.InteriorThreshold = interiorThreshold;
            this.BottleneckRadius = bottleneckRadius;
        }
    }

    /// <summary>
    /// Find empty space using MOLE
    /// </summary>
    class EmptySpaceQuery : QueryMotive
    {
        QueryMotive Where;
        double ProbeRadius, InteriorThreshold;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {

            List<Motive> spaces = new List<Motive>();

            var ctx = context.RequestCurrentContext();
            foreach (var parent in Where.ExecuteMotive(context))
            {
                var complex = Complex.Create(parent.ToStructure(ctx.Structure.Id, true, true), new ComplexParameters { ProbeRadius = ProbeRadius, InteriorThreshold = InteriorThreshold });

                spaces.AddRange(
                    complex.Cavities.Concat(complex.Voids)
                    .Select(s => Motive.FromAtoms(s.InnerResidues.Concat(s.BoundaryResidues).SelectMany(r => r.Atoms), null, ctx)));
            }

            return spaces;
        }

        protected override string ToStringInternal()
        {
            return NameHelper("EmptySpace",
                new[] { NameOption("ProbeRadius", ProbeRadius), NameOption("InteriorThreshold", InteriorThreshold) },
                new[] { Where.ToString() });
        }

        public EmptySpaceQuery(QueryMotive where, double probeRadius, double interiorThreshold)
        {
            this.Where = where;
            this.ProbeRadius = probeRadius;
            this.InteriorThreshold = interiorThreshold;
        }
    }
}
