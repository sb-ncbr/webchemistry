namespace WebChemistry.Framework.Core
{
    public class Bond : InteractiveObject, IBond
    {
        BondIdentifier id;
        IAtom a;
        IAtom b;
        BondType type;

        public BondIdentifier Id { get { return id; } }
        public IAtom A { get { return a; } }
        public IAtom B { get { return b; } }
             
        public BondType Type
        {
            get { return type; }
            private set { type = value; }
        }

        private Bond(IAtom a, IAtom b, BondType type)
        {
            this.a = a;
            this.b = b;
            this.type = type;
            this.id = new BondIdentifier(a, b);
        }

        public static IBond Create(IAtom a, IAtom b, BondType type = BondType.Unknown)
        {
            if (a == b) throw new System.ArgumentException("Cannot create a bond with two identical atoms.");
            return new Bond(a, b, type);
        }
        
        public bool Equals(IBond other)
        {
            return this.id == other.Id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            IBond bond = obj as IBond;

            return bond == null ? false : Equals(bond);
        }
    }
}