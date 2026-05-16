namespace WebChemistry.Framework.Geometry
{
    using System;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// A specialized 3D kD tree to store atom. Inherits from K3DTree of IAtom.
    /// </summary>
    public class KDAtomTree : K3DTree<IAtom>
    {
        /// <summary>
        /// Create an atom tree with specific key selector.
        /// </summary>
        /// <param name="atoms">The collection of atoms to index.</param>
        /// <param name="keySelector">A function to extract the position from an atom. If null, uses the atom's Position property.</param>
        /// <param name="leafCapacity">The maximum number of elements in a leaf node. Defaults to 5.</param>
        /// <param name="method">The pivot selection method to use. Defaults to Average.</param>
        public KDAtomTree(IEnumerable<IAtom> atoms, Func<IAtom, Vector3D> keySelector = null, int leafCapacity = 5, K3DPivotSelectionMethod method = K3DPivotSelectionMethod.Average)
            : base(atoms, keySelector ?? (a => a.Position), leafCapacity, method)
        {

        }
    }

    ///// <summary>
    ///// A specialized 3D kD tree to store atom. Inherits from K3DTree of IAtom.
    ///// </summary>
    //public class KDAtomTreeOld : K3DTreeOld<IAtom>
    //{
    //    /// <summary>
    //    /// Create an atom tree with specific key selector.
    //    /// </summary>
    //    /// <param name="atoms"></param>
    //    /// <param name="keySelector"></param>
    //    public KDAtomTreeOld(IEnumerable<IAtom> atoms, Func<IAtom, Vector3D> keySelector = null)
    //        : base(atoms, keySelector ?? (a => a.Position))
    //    {

    //    }
    //}
}
