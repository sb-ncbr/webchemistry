namespace WebChemistry.Framework.Core
{
    using System;

    /// <summary>
    /// Thrown if two atoms with the same id appear in the same "context" (collection)
    /// </summary>
    public class NonUniqueAtomIdException : Exception
    {
        IAtom first, second;

        /// <summary>
        /// Atom that has occurred "sooner"
        /// </summary>
        public IAtom First { get { return first; } }

        /// <summary>
        /// Atom that has occurred "later"
        /// </summary>
        public IAtom Second { get { return second; } }

        /// <summary>
        /// Exception message.
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format("AtomCollection cannot contain 2 atoms with the same id ({0}).", first.Id);
            }
        }

        /// <summary>
        /// Create this exception when atoms with the same Id appear in one collection.
        /// </summary>
        /// <param name="first">Atom that occurred sooner.</param>
        /// <param name="second">Atom that occurred later.</param>
        public NonUniqueAtomIdException(IAtom first, IAtom second)
        {
            this.first = first;
            this.second = second;
        }
    }
}
