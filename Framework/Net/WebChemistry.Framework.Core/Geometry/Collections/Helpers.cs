// -----------------------------------------------------------------------
// <copyright file="_3DHelperClasses.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Framework.Geometry
{
    using System;
    using System.Collections.Generic;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Represents a priority/value pair.
    /// </summary>
    class Vector3DValuePair<TValue>
    {
        /// <summary>
        /// Position.
        /// </summary>
        public readonly Vector3D Position;

        /// <summary>
        /// Value.
        /// </summary>
        public readonly TValue Value;

        /// <summary>
        /// Creates the pair.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public Vector3DValuePair(Vector3D position, TValue value)
        {
            this.Position = position;
            this.Value = value;
        }
    }

    /// <summary>
    /// Compares the given coordinate...
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    class CoordinateComparer3D<TValue> : IComparer<Vector3DValuePair<TValue>>
    {
        Func<Vector3DValuePair<TValue>, double> selector;

        public int Compare(Vector3DValuePair<TValue> a, Vector3DValuePair<TValue> b)
        {
            return selector(a).CompareTo(selector(b));
        }

        public CoordinateComparer3D(Func<Vector3DValuePair<TValue>, double> selector)
        {
            this.selector = selector;
        }
    }

}
