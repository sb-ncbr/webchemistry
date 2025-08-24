using Newtonsoft.Json;

namespace WebChemistry.Tunnels.DataModel
{
    /// <summary>
    /// Type of the cavity.
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum CavityType
    {
        /// <summary>
        /// Empty space inside the protein separated by a bottleneck.
        /// </summary>
        Pocket,
        
        /// <summary>
        /// Empty space near the molecular surface not separated by a bottleneck.
        /// </summary>
        Depression,

        /// <summary>
        /// Empty space that connects two or more "sides" of the structure. Does not contain any significant bottlenect.
        /// </summary>
        Channel,

        /// <summary>
        /// Empty space inside the protein that has no access to the surface.
        /// </summary>
        Void,
    }

    /// <summary>
    /// A class representing empty space inside a protein.
    /// </summary>
    public class Cavity
    {
        /// <summary>
        /// Cavity Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Type of the cavity.
        /// </summary>
        public CavityType Type { get; set; }

        /// <summary>
        /// Volume of the cavity.
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// Residues that surround the cavity.
        /// </summary>
        public object[] Residues { get; set; }

        /// <summary>
        /// Bottlenecks of the cavity.
        /// </summary>
        public object[] Bottlenecks { get; set; }

        /// <summary>
        /// Medial axis of the cavity.
        /// </summary>
        public object MedialAxis { get; set; }
    }
}
