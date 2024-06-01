namespace WebChemistry.Framework.Core
{
    using System.ComponentModel;

    /// <summary>
    /// A base interface for representing structures (molecules).
    /// </summary>
    [Newtonsoft.Json.JsonConverter(typeof(WebChemistry.Framework.Core.Json.WriterStructureJsonConverter))]
    public interface IStructure : INotifyPropertyChanged, IInteractive, IPropertyObject, ICloneable<IStructure>
    {
        /// <summary>
        /// Gets the structure's is.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets atoms.
        /// </summary>
        IAtomCollection Atoms { get; }

        /// <summary>
        /// Gets bonds.
        /// </summary>
        IBondCollection Bonds { get; }
    }
}