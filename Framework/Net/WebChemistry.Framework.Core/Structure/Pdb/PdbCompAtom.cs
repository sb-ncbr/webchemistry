namespace WebChemistry.Framework.Core.Pdb
{
    using WebChemistry.Framework.Math;
    using System;

    /// <summary>
    /// Determine the atom chirality (R = right handed, S = left handed)
    /// </summary>
    public enum AtomChiralityRS
    {
        None = 0,
        R = 1,
        S = 2
    }

    /// <summary>
    /// A representation of the PDB atom record.
    /// </summary>
    public class PdbCompAtom : PdbAtom
    {
        /// <summary>
        /// The model coordinates.
        /// </summary>
        public Vector3D? ModelPosition { get; private set; }

        /// <summary>
        /// The ideal coordinates.
        /// </summary>
        public Vector3D? IdealPosition { get; private set; }

        /// <summary>
        /// Determines if the atom is chiral or not.
        /// </summary>
        public AtomChiralityRS Chirality { get; private set; }

        /// <summary>
        /// Clone the atom.
        /// </summary>
        /// <returns></returns>
        public override IAtom Clone()
        {
            var ret = new PdbCompAtom(this.Id, this.ElementSymbol, InvariantPosition) { Position = new Vector3D(this.Position.X, this.Position.Y, this.Position.Z) };
            //ret.CopyPropertiesFrom(this);

            ret.Name = this.Name;
            ret.RecordName = this.RecordName;
            ret.SerialNumber = this.SerialNumber;
            ret.EntityId = this.EntityId;
            ret.ResidueName = this.ResidueName;
            ret.ResidueSequenceNumber = this.ResidueSequenceNumber;
            ret.InsertionResidueCode = this.InsertionResidueCode;
            ret.ChainIdentifier = this.ChainIdentifier;
            ret.AlternateLocaltionIdentifier = this.AlternateLocaltionIdentifier;
            ret.Occupancy = this.Occupancy;
            ret.TemperatureFactor = this.TemperatureFactor;
            ret.SegmentIdentifier = this.SegmentIdentifier;
            ret.Charge = this.Charge;
            ret.Chirality = this.Chirality;
            ret.IdealPosition = this.IdealPosition;
            ret.ModelPosition = this.ModelPosition;

            return ret;
        }

        /// <summary>
        /// Clone the atom.
        /// </summary>
        /// <returns></returns>
        public IAtom ToModelAtom()
        {
            var position = ModelPosition.HasValue ? ModelPosition.Value : new Vector3D();
            return CloneWith(this, position: position);
        }

        /// <summary>
        /// Clone the atom.
        /// </summary>
        /// <returns></returns>
        public IAtom ToIdealAtom()
        {
            var position = IdealPosition.HasValue ? IdealPosition.Value : new Vector3D();
            return CloneWith(this, position: position);
        }

        /// <summary>
        /// Clone the atom with alternate PDB properties.
        /// </summary>
        /// <returns></returns>
        public static IAtom CloneWith(
            IAtom atom,
            int? id = null,
            ElementSymbol? elementSymbol = null,
            string name = null,
            string recordName = null,
            int? serialNumber = null,
            int? entityId = null,
            string residueName = null,
            int? residueSequenceNumber = null,
            char? insertionResidueCode = null,
            string chainIdentifier = null,
            char? alternateLocationIdentifier = null,
            double? occupancy = null,
            double? temperatureFactor = null,
            string segmentIdentifier = null,
            string charge = null,
            Vector3D? position = null,
            AtomChiralityRS? chirality = null,
            Vector3D? idealPosition = null,
            Vector3D? modelPosition = null)
        {
            var pdbAtom = atom as PdbCompAtom;
            if (pdbAtom == null) throw new ArgumentException("'atom' must be a PdbCompAtom.");

            PdbCompAtom ret;

            if (position == null) ret = new PdbCompAtom(id.DefaultIfNull(atom.Id), elementSymbol.DefaultIfNull(atom.ElementSymbol), atom.InvariantPosition) { Position = new Vector3D(atom.Position.X, atom.Position.Y, atom.Position.Z) };
            else ret = new PdbCompAtom(id.DefaultIfNull(atom.Id), elementSymbol.DefaultIfNull(atom.ElementSymbol), position.Value) { Position = position.Value };

            //ret.CopyPropertiesFrom(atom);

            ret.Name = name.DefaultIfNull(pdbAtom.Name);
            ret.RecordName = recordName.DefaultIfNull(pdbAtom.RecordName);
            ret.SerialNumber = serialNumber.DefaultIfNull(pdbAtom.SerialNumber);
            ret.EntityId = entityId.DefaultIfNull(pdbAtom.EntityId);
            ret.ResidueName = residueName.DefaultIfNull(pdbAtom.ResidueName);
            ret.ResidueSequenceNumber = residueSequenceNumber.DefaultIfNull(pdbAtom.ResidueSequenceNumber);
            ret.InsertionResidueCode = insertionResidueCode.DefaultIfNull(pdbAtom.InsertionResidueCode);
            ret.ChainIdentifier = string.IsNullOrWhiteSpace(chainIdentifier) ? "" : chainIdentifier;
            ret.AlternateLocaltionIdentifier = alternateLocationIdentifier.DefaultIfNull(pdbAtom.AlternateLocaltionIdentifier);
            ret.Occupancy = occupancy.DefaultIfNull(pdbAtom.Occupancy);
            ret.TemperatureFactor = temperatureFactor.DefaultIfNull(pdbAtom.TemperatureFactor);
            ret.SegmentIdentifier = segmentIdentifier.DefaultIfNull(pdbAtom.SegmentIdentifier);
            ret.Charge = charge.DefaultIfNull(pdbAtom.Charge);
            ret.Chirality = chirality.DefaultIfNull(pdbAtom.Chirality);

            if (!ret.ModelPosition.HasValue && modelPosition.HasValue) 
            {
                ret.ModelPosition = modelPosition;
            }

            if (!ret.IdealPosition.HasValue && idealPosition.HasValue)
            {
                ret.IdealPosition = idealPosition;
            }

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="elementSymbol"></param>
        /// <param name="invariantPosition"></param>
        protected PdbCompAtom(int id, ElementSymbol elementSymbol, Vector3D invariantPosition)
            : base(id, elementSymbol, invariantPosition)
        {

        }

        /// <summary>
        /// Creates an instance of a PDBAtom.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="elementSymbol"></param>
        /// <param name="name"></param>
        /// <param name="entityId"></param>
        /// <param name="recordName"></param>
        /// <param name="serialNumber"></param>
        /// <param name="residueName"></param>
        /// <param name="residueSequenceNumber"></param>
        /// <param name="insertionResidueCode"></param>
        /// <param name="chainIdentifier"></param>
        /// <param name="alternateLocationIdentifier"></param>
        /// <param name="occupancy"></param>
        /// <param name="temperatureFactor"></param>
        /// <param name="segmentIdentifier"></param>
        /// <param name="charge"></param>
        /// <param name="position"></param>
        /// <param name="chirality"></param>
        /// <param name="idealPosition"></param>
        /// <param name="modelPosition"></param>
        /// <returns></returns>
        public static IAtom Create(int id,
                       ElementSymbol elementSymbol,
                       string name = "",
                       string recordName = "HETATM",
                       int serialNumber = 0,
                       int entityId = 1,
                       string residueName = "UNK",
                       int residueSequenceNumber = 0,
                       char insertionResidueCode = ' ',
                       string chainIdentifier = "",
                       char alternateLocationIdentifier = ' ',
                       double occupancy = 1.0,
                       double temperatureFactor = 0.0,
                       string segmentIdentifier = "",
                       string charge = "",
                       Vector3D position = new Vector3D(),
                       AtomChiralityRS chirality = AtomChiralityRS.None,
                       Vector3D? idealPosition = null,
                       Vector3D? modelPosition = null)
        {
            var ret = new PdbCompAtom(id, elementSymbol, new Vector3D(position.X, position.Y, position.Z)) { Position = position };

            ret.Name = name;
            ret.RecordName = recordName;
            ret.SerialNumber = serialNumber;
            ret.EntityId = entityId;
            ret.ResidueName = residueName;
            ret.ResidueSequenceNumber = residueSequenceNumber;
            ret.InsertionResidueCode = insertionResidueCode;
            ret.ChainIdentifier = string.IsNullOrWhiteSpace(chainIdentifier) ? "" : chainIdentifier;
            ret.AlternateLocaltionIdentifier = alternateLocationIdentifier;
            ret.Occupancy = occupancy;
            ret.TemperatureFactor = temperatureFactor;
            ret.SegmentIdentifier = segmentIdentifier;
            ret.Charge = charge;        
            ret.IdealPosition = idealPosition;
            ret.ModelPosition = modelPosition;
            ret.Chirality = chirality;

            return ret;
        }
    }
}
