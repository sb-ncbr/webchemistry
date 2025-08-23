namespace WebChemistry.Framework.Core
{
    using System.Diagnostics;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Represents an atom.
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Element = {ElementSymbol}")]
    public class Atom : InteractiveObject, IAtom
    {
        Vector3D position;
        int id;
        ElementSymbol elementSymbol;
       
        //List<IBond> bonds;

        //internal void AddBond(IBond bond)
        //{
        //    this.bonds.Add(bond);
        //}

        public Vector3D InvariantPosition
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the atom position.
        /// </summary>
        public Vector3D Position
        {
            get { return position; }
            set 
            {
                if (position != value)
                {
                    position = value;
                    NotifyPropertyChanged("Position");
                }
            }
        }
        
        /// <summary>
        /// Gets the element symbol of the atom.
        /// </summary>
        public ElementSymbol ElementSymbol
        {
            get { return elementSymbol; }
        }
        
        /// <summary>
        /// Gets the id of the atom.
        /// </summary>
        public int Id { get { return id; } }

        //public int LinearId { get; internal set; }
        
        ///// <summary>
        ///// Return the bonds for this atom. It always holds that Bond.A = this.
        ///// </summary>
        //public IEnumerable<IBond> Bonds { get { throw new NotImplementedException();/* return bonds;*/ } }
      
        /// <summary>
        /// Creates an instance of atom with a given id and element symbol.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="elementSymbol"></param>
        protected Atom(int id, ElementSymbol elementSymbol, Vector3D invariantPosition)
        {
            this.elementSymbol = elementSymbol;
            this.id = id;
            this.InvariantPosition = invariantPosition;
           // this.bonds = new List<IBond>();
        }
        
        /// <summary>
        /// Compares the Id's of both atoms.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IAtom other)
        {
            return Id == other.Id;
        }
        
        /// <summary>
        /// Calls Equals(IAtom)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is IAtom) return Equals(obj as IAtom);
            return false;
        }

        /// <summary>
        /// Return the id of the atom.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return id;
        }

        /// <summary>
        /// Compares atom Ids.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(IAtom other)
        {
            return this.id.CompareTo(other.Id);
        }

        /// <summary>
        /// Compares atom Ids.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            var other = obj as IAtom;
            if (other != null) return this.Id.CompareTo(other.Id);
            return 1;
        }
       
        /// <summary>
        /// Creates a deep copy of the atoms. 
        /// Dynamic properties are shallow copies.
        /// </summary>
        /// <returns>A deep copy of the atom (dynamic properties are a shallow copy).</returns>
        public virtual IAtom Clone()
        {
            Atom ret = new Atom(this.Id, this.ElementSymbol, this.InvariantPosition) { Position = new Vector3D(this.Position.X, this.Position.Y, this.Position.Z) };
            return ret;
        }

        /// <summary>
        /// Creates new instance of an atom.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="symbol"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static IAtom Create(int id, ElementSymbol symbol, Vector3D position = new Vector3D())
        {
            return new Atom(id, symbol, new Vector3D(position.X, position.Y, position.Z)) { Position = position };
        }
    }
}