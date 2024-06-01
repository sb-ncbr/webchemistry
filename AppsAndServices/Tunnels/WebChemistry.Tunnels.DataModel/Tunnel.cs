
namespace WebChemistry.Tunnels.DataModel
{
    using Newtonsoft.Json;

    /// <summary>
    /// Type of the tunnel.
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum TunnelType
    {
        /// <summary>
        /// A tunnel from a specific starting point inside the molecule.
        /// </summary>
        Tunnel,

        /// <summary>
        /// The active site for this tunnel is on the surface of the molecule and can be easily accessed.
        /// </summary>
        Surface,

        /// <summary>
        /// A path between two points on the molecular surface.
        /// </summary>
        Pore,

        /// <summary>
        /// A path between two arbitrary points within the protein.
        /// </summary>
        Path
    }
    
    /// <summary>
    /// Tunnel building block.
    /// </summary>
    public class TunnelNode
    {
        /// <summary>
        /// Node center.
        /// </summary>
        public object Center { get; set; }

        /// <summary>
        /// Radius of the tunnel.
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// Distance from the start.
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Residues around this node.
        /// </summary>
        public object[] SurroundingResidues { get; set; }
    }

    /// <summary>
    /// Layer representation.
    /// </summary>
    public class TunnelLayer
    {

    }

    /// <summary>
    /// Represents a tunnel.
    /// </summary>
    public class Tunnel
    {
        /// <summary>
        /// Tunnel Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Which cavity the tunnel is in.
        /// </summary>
        public int CavityId { get; set; }

        /// <summary>
        /// Name of the sctructure the tunnel is in.
        /// </summary>
        public string StructureName { get; set; }

        /// <summary>
        /// Type of the tunnel.
        /// </summary>
        public TunnelType Type { get; set; }

        /// <summary>
        /// Where the tunnel starts.
        /// </summary>
        public StructurePoint StartPoint { get; set; }

        /// <summary>
        /// Where it ends.
        /// </summary>
        public StructurePoint EndPoint { get; set; }

        /// <summary>
        /// Nodes of the tunnel.
        /// </summary>
        public TunnelNode[] Nodes { get; set; }

        /// <summary>
        /// Layers of the tunnel.
        /// </summary>
        public TunnelLayer[] Layers { get; set; }

        /// <summary>
        /// Tunnel properties.
        /// </summary>
        public object Properties { get; set; }
    }
}
