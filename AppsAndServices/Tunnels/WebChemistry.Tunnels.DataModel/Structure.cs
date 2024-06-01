using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebChemistry.Tunnels.DataModel
{
    public class StructurePoint
    {
        public object[] Residues { get; set; }
        public object ActualPoint { get; set; }
    }

    class Structure
    {
        public string Id { get; set; }

        /// <summary>
        /// Settings used to compute these tunnels.
        /// </summary>
        public TunnelComputationSettings Settings { get; set; }

        /// <summary>
        /// Cavities inside the structure.
        /// </summary>
        public Cavity[] Cavities { get; set; }

        /// <summary>
        /// Specific tunnels.
        /// </summary>
        public Tunnel[] Tunnels { get; set; }
    }
}
