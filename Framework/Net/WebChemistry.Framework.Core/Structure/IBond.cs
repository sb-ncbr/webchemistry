namespace WebChemistry.Framework.Core
{
    using System.ComponentModel;
    using System;

    /// <summary>
    /// A base interface for representing bonds.
    /// </summary>
    public interface IBond : INotifyPropertyChanged, IInteractive, IEquatable<IBond>
    {
        /// <summary>
        /// Calculated from IDs of A and B as (i * 2^32) | j   // (i + j) * (i + j + 1) / 2 + j where i is the smaller id and j is the larger one.
        /// </summary>
        BondIdentifier Id { get; }

        /// <summary>
        /// The first atom.
        /// </summary>
        IAtom A { get; }

        /// <summary>
        /// The second atom.
        /// </summary>
        IAtom B { get; }

        /// <summary>
        /// Bond type.
        /// </summary>
        BondType Type { get; }
    }
}