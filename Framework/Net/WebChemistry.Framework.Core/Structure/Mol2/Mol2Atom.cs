namespace WebChemistry.Framework.Core
{
    using WebChemistry.Framework.Math;
    using System;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// A representation of the Mol2 atom record.
    /// </summary>
    public sealed class Mol2Atom : Atom
    {
        /// <summary>
        /// Mol 2 atom type.
        /// </summary>
        public string AtomType { get; private set; }

        /// <summary>
        /// Atom name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Partail Charge [double]
        /// </summary>
        public double PartialCharge { get; private set; }

        /// <summary>
        /// Residue identifier.
        /// </summary>
        public PdbResidueIdentifier ResidueIdentifier { get; private set; }

        /// <summary>
        /// residue name.
        /// </summary>
        public string ResidueName { get; private set; }
        
        /// <summary>
        /// Clone the atom.
        /// </summary>
        /// <returns></returns>
        public override IAtom Clone()
        {
            var ret = new Mol2Atom(this.Id, this.ElementSymbol, this.AtomType, this.Name, this.InvariantPosition) { Position = new Vector3D(this.Position.X, this.Position.Y, this.Position.Z) };
          
            //ret.CopyPropertiesFrom(this);
            ret.ResidueIdentifier = this.ResidueIdentifier;
            ret.PartialCharge = this.PartialCharge;
            ret.ResidueName = this.ResidueName;
            return ret;
        }

        /// <summary>
        /// Clone the atom with alternate properties.
        /// </summary>
        /// <returns></returns>
        public static IAtom CloneWith(
            IAtom atom,
            int? id = null,
            ElementSymbol? elementSymbol = null,
            string atomType = null,
            string name = null,
            PdbResidueIdentifier? residueIdentifier = null,
            string residueName = null,
            double? partialCharge = null)
        {
            var molAtom = atom as Mol2Atom;
            if (molAtom == null) throw new ArgumentException("'atom' must be a Mol2Atom.");

            var ret = new Mol2Atom(id.DefaultIfNull(atom.Id), 
                elementSymbol.DefaultIfNull(atom.ElementSymbol),
                atomType ?? molAtom.AtomType,
                name ?? molAtom.Name,
                atom.InvariantPosition) { Position = new Vector3D(atom.Position.X, atom.Position.Y, atom.Position.Z) };

            ret.ResidueIdentifier = residueIdentifier.HasValue ? residueIdentifier.Value : molAtom.ResidueIdentifier;
            ret.PartialCharge = partialCharge.HasValue ? partialCharge.Value : molAtom.PartialCharge;
            ret.ResidueName = residueName ?? molAtom.ResidueName;
 
            return ret;
        }

        /// <summary>
        /// Create a new instance of the atom.
        /// </summary>
        /// <param name="id">The unique identifier for the atom.</param>
        /// <param name="elementSymbol">The element symbol of the atom.</param>
        /// <param name="atomType">The MOL2 atom type.</param>
        /// <param name="name">The name of the atom.</param>
        /// <param name="invariantPosition">The invariant position used for structural comparisons.</param>
        private Mol2Atom(int id,
                        ElementSymbol elementSymbol, string atomType,
                        string name,
                        Vector3D invariantPosition)
            : base(id, elementSymbol, invariantPosition)
        {
            this.AtomType = atomType;
            this.Name = name;
        }

        /// <summary>
        /// Creates an instance of a Mol2 atom.
        /// </summary>
        /// <param name="id">The unique identifier for the atom.</param>
        /// <param name="elementSymbol">The element symbol of the atom.</param>
        /// <param name="atomType">The MOL2 atom type.</param>
        /// <param name="name">The name of the atom.</param>
        /// <param name="residueIdenfier">Optional residue identifier.</param>
        /// <param name="residueName">The name of the residue (default: UNK).</param>
        /// <param name="partialCharge">The partial charge of the atom.</param>
        /// <param name="position">The 3D position of the atom.</param>
        /// <returns>A new Mol2Atom instance.</returns>
        public static IAtom Create(int id,
                        ElementSymbol elementSymbol,
                        string atomType,
                        string name,
                        PdbResidueIdentifier? residueIdenfier = null,
                        string residueName = "UNK",
                        double partialCharge = 0.0,
                        Vector3D position = new Vector3D())
        {
            var ret = new Mol2Atom(id, elementSymbol, atomType, name, new Vector3D(position.X, position.Y, position.Z)) { Position = position };
            ret.PartialCharge = partialCharge;
            ret.ResidueName = residueName;
            ret.ResidueIdentifier = residueIdenfier.HasValue ? residueIdenfier.Value : new PdbResidueIdentifier(1, "A", ' ');
            return ret;
        }
    }
}
