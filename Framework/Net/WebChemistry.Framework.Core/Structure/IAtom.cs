namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// A base interface for representing atoms.
    /// </summary>
    public interface IAtom : INotifyPropertyChanged, IInteractive, IEquatable<IAtom>, ICloneable<IAtom>, IComparable<IAtom>, IComparable
    {
        /// <summary>
        /// Returns a unique id of the atom. Read-only.
        /// </summary>
        int Id { get; }

        ///// <summary>
        ///// Linear atom - corresponds to its index in the atom collection.
        ///// </summary>
        //int LinearId { get; }

        /// <summary>
        /// Element symbol of the atom. Read-only.
        /// </summary>
        ElementSymbol ElementSymbol { get; }

        /// <summary>
        /// Immutable Position of the atom. Assigned when the atom was created.
        /// </summary>
        Vector3D InvariantPosition { get; }

        /// <summary>
        /// Position of the atom. Mutable.
        /// </summary>
        Vector3D Position { get; set; }

        ///// <summary>
        ///// Bonds
        ///// </summary>
        //IEnumerable<IBond> Bonds { get; }
    }
}