namespace WebChemistry.Framework.Core
{
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Warning wrapper.
    /// </summary>
    public class StructureReaderWarning
    {
        protected virtual string GetHeaderString()
        {
            return "";
        }

        /// <summary>
        /// Line where the issue occurred.
        /// </summary>
        public int Line { get; private set; }

        /// <summary>
        /// Warning message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Converts the warnings to string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var header = GetHeaderString();
            if (!string.IsNullOrEmpty(header))
            {
                if (Line >= 0) return string.Format("{0}, line {1}: {2}", header, Line, Message);
                return string.Format("{0}: {1}", header, Message);
            }

            if (Line >= 0) return string.Format("Line {0}: {1}", Line, Message);
            return string.Format("{0}", Message);
        }

        /// <summary>
        /// Create a new warning.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="message"></param>
        public StructureReaderWarning(string message, int line = -1)
        {
            this.Line = line;
            this.Message = message;
        }
    }

    /// <summary>
    /// Raised if the file contains multiple models but only the 1st one loaded.
    /// </summary>
    public class OnlyFirstModelLoadedReaderWarning : StructureReaderWarning
    {
        public OnlyFirstModelLoadedReaderWarning(int line)
            : base("Only the first model was loaded (the input contains multiple models).", line)
        {

        }
    }


    /// <summary>
    /// Type of warning for atoms.
    /// </summary>
    public enum AtomStructureReaderWarningType
    {
        Generic = 0,
        DuplicateId,
        PositionOccupied
    }

    /// <summary>
    /// Warning for atoms.
    /// </summary>
    public class AtomStructureReaderWarning : StructureReaderWarning
    {
        /// <summary>
        /// Atom ID.
        /// </summary>
        public IAtom Atom { get; private set; }

        /// <summary>
        /// Type of the warning.
        /// </summary>
        public AtomStructureReaderWarningType WarningType { get; private set; }

        protected override string GetHeaderString()
        {
            if (Atom is PdbAtom) return string.Format("Atom {0} ({1}) {2}", Atom.ElementSymbol, Atom.PdbName(), Atom.Id);
            return string.Format("Atom {0} {1}", Atom.ElementSymbol, Atom.Id);
        }

        /// <summary>
        /// Create the warning.
        /// </summary>
        /// <param name="atom"></param>
        /// <param name="message"></param>
        /// <param name="line"></param>
        /// <param name="type"></param>
        public AtomStructureReaderWarning(IAtom atom, string message, int line = -1, AtomStructureReaderWarningType type = AtomStructureReaderWarningType.Generic)
            : base(message, line)
        {
            WarningType = type;
            Atom = atom;
        }
    }

    /// <summary>
    /// Residue warning type.
    /// </summary>
    public enum ResidueStructureReaderWarningType
    {
        Generic = 0,
        ResiduesTooClose,
        IgnoredAlternateLocation,
        MultipleNames
    }

    /// <summary>
    /// Warning for residues.
    /// </summary>
    public class ResidueStructureReaderWarning : StructureReaderWarning
    {
        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Atom ID.
        /// </summary>
        public PdbResidueIdentifier Id { get; private set; }

        /// <summary>
        /// Type of the warning.
        /// </summary>
        public ResidueStructureReaderWarningType WarningType { get; private set; }

        protected override string GetHeaderString()
        {
            if (WarningType == ResidueStructureReaderWarningType.MultipleNames) return string.Format("Residue {0}", Id);
            return string.Format("Residue {0} {1}", Name, Id);
        }

        /// <summary>
        /// Create the warning.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <param name="line"></param>
        /// <param name="type"></param>
        public ResidueStructureReaderWarning(string name, PdbResidueIdentifier id, string message, int line = -1, ResidueStructureReaderWarningType type = ResidueStructureReaderWarningType.Generic)
            : base(message, line)
        {
            WarningType = type;
            Name = name;
            Id = id;
        }
    }
    
    /// <summary>
    /// Warning for residues.
    /// </summary>
    public class ConectBondLengthReaderWarning : StructureReaderWarning
    {
        /// <summary>
        /// The first atom.
        /// </summary>
        public IAtom Atom1 { get; set; }

        /// <summary>
        /// The second atom.
        /// </summary>
        public IAtom Atom2 { get; set; }
        
        protected override string GetHeaderString()
        {
            return string.Format("Bond ({0} {1}, {2} {3}), {4}ang", Atom1.ElementSymbol, Atom1.Id, Atom2.ElementSymbol, Atom2.Id, Atom1.Position.DistanceTo(Atom2.Position).ToStringInvariant("0.00"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public ConectBondLengthReaderWarning(IAtom a, IAtom b)
            : base("Unusual bond length given by CONECT record.", -1)
        {
            Atom1 = a;
            Atom2 = b;
        }
    }
}
