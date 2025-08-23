namespace WebChemistry.Framework.Core.PdbQt
{
    using WebChemistry.Framework.Math;
    using System;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// A representation of the PDBQt atom record.
    /// </summary>
    public class PdbQtAtom : PdbAtom
    {
        /// <summary>
        /// Partial atom charge.
        /// </summary>
        public double PartialCharge { get; private set; }

        /// <summary>
        /// Clone the atom.
        /// </summary>
        /// <returns></returns>
        public override IAtom Clone()
        {
            var ret = new PdbQtAtom(this.Id, this.ElementSymbol, this.InvariantPosition) { Position = new Vector3D(this.Position.X, this.Position.Y, this.Position.Z) };
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
            ret.PartialCharge = this.PartialCharge;

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
            double? partialCharge = null)
        {
            var pdbAtom = atom as PdbQtAtom;
            if (pdbAtom == null) throw new ArgumentException("'atom' must be a PdbQtAtom.");

            var ret = new PdbQtAtom(id.DefaultIfNull(atom.Id), elementSymbol.DefaultIfNull(atom.ElementSymbol), atom.InvariantPosition) { Position = new Vector3D(atom.Position.X, atom.Position.Y, atom.Position.Z) };

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
            ret.PartialCharge = partialCharge.DefaultIfNull(pdbAtom.PartialCharge);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="elementSymbol"></param>
        private PdbQtAtom(int id, ElementSymbol elementSymbol, Vector3D invariantPosition)
            : base(id, elementSymbol, invariantPosition)
        {

        }

        /// <summary>
        /// Creates an instance of a PDBAtom.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="elementSymbol"></param>
        /// <param name="name"></param>
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
        /// <param name="partialCharge"></param>
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
                       double occupancy = 0.0,
                       double temperatureFactor = 0.0,
                       string segmentIdentifier = "",
                       string charge = "",
                       double partialCharge = 0.0,
                       Vector3D position = new Vector3D())
        {
            var ret = new PdbQtAtom(id, elementSymbol, new Vector3D(position.X, position.Y, position.Z)) { Position = position };

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
            ret.PartialCharge = partialCharge;

            return ret;
        }
    }
}
