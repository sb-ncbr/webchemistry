namespace WebChemistry.Framework.Core.MdlMol
{
    using WebChemistry.Framework.Math;
    using System;

    /// <summary>
    /// Mol stereo parity.
    /// </summary>
    public enum MdlMolStereoParity
    {
        NotStereo = 0,
        Odd = 1,
        Even = 2,
        EitherOrUnmarked = 3
    }

    /// <summary>
    /// A representation of the Mol2 atom record.
    /// </summary>
    public sealed class MdlMolAtom : Atom
    {
        /// <summary>
        /// Stereo parity
        /// 0 = not stereo
        /// 1 = odd
        /// 2 = even
        /// 3 = both or unmarked
        /// </summary>
        public MdlMolStereoParity StereoParity { get; private set; }

        /// <summary>
        /// Clone the atom.
        /// </summary>
        /// <returns></returns>
        public override IAtom Clone()
        {
            var ret = new MdlMolAtom(this.Id, this.ElementSymbol, this.InvariantPosition) { Position = new Vector3D(this.Position.X, this.Position.Y, this.Position.Z) };

            ret.StereoParity = this.StereoParity;
            return ret;
        }

        /// <summary>
        /// Clone the atom with alternate PDB properties.
        /// </summary>
        /// <returns></returns>
        public static IAtom CloneWith(
            IAtom atom,
            int? id = null,
            ElementSymbol? elementSymbol = null,
            MdlMolStereoParity stereoParity = MdlMolStereoParity.EitherOrUnmarked)
        {
            var molAtom = atom as Mol2Atom;
            if (molAtom == null) throw new ArgumentException("'atom' must be a Mol2Atom.");

            var ret = new MdlMolAtom(id.DefaultIfNull(atom.Id), elementSymbol.DefaultIfNull(atom.ElementSymbol), atom.InvariantPosition) { Position = new Vector3D(atom.Position.X, atom.Position.Y, atom.Position.Z) };

            //   ret.CopyPropertiesFrom(atom);

            ret.StereoParity = stereoParity;

            return ret;
        }

        /// <summary>
        /// Create a new instance of the atom.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="elementSymbol"></param>
        /// <param name="partialCharge"></param>
        private MdlMolAtom(int id,
                        ElementSymbol elementSymbol, Vector3D invariantPosition)
            : base(id, elementSymbol, invariantPosition)
        {
        }

        /// <summary>
        /// Creates an instance of a Mol2 atom.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="elementSymbol"></param>
        /// <param name="partialCharge"></param>
        /// <returns></returns>
        public static IAtom Create(int id,
                        ElementSymbol elementSymbol,
                        MdlMolStereoParity stereoParity,
                        Vector3D position = new Vector3D())
        {
            var ret = new MdlMolAtom(id, elementSymbol, new Vector3D(position.X, position.Y, position.Z)) { Position = position };
            ret.StereoParity = stereoParity;
            return ret;
        }
    }
}
