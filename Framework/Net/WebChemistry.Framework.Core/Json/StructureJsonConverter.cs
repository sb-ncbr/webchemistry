namespace WebChemistry.Framework.Core.Json
{
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// ResidueIdentifier converter.
    /// </summary>
    public class PdbResidueIdentifierJsonConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return PdbResidueIdentifier.Parse(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var r = (PdbResidueIdentifier)value;
            writer.WriteValue(r.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }

    class WriterStructureJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.GetInterface("IStructure") != null;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        static Dictionary<BondType, string> bondTypeCache = Enum.GetNames(typeof(BondType)).ToDictionary(n => (BondType)Enum.Parse(typeof(BondType), n, false), n => n);

        static void WriteAtom(IAtom atom, JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Id");
            writer.WriteValue(atom.Id);
            writer.WritePropertyName("Symbol");
            writer.WriteValue(atom.ElementSymbol.ToString());
            writer.WritePropertyName("Position");
            writer.WriteStartArray();
            writer.WriteValue(Math.Round(atom.Position.X, 3));
            writer.WriteValue(Math.Round(atom.Position.Y, 3));
            writer.WriteValue(Math.Round(atom.Position.Z, 3));
            writer.WriteEndArray();
            if (atom is PdbAtom)
            {
                var pdb = atom as PdbAtom;
                writer.WritePropertyName("SerialNumber");
                writer.WriteValue(pdb.SerialNumber);
                writer.WritePropertyName("RecordType");
                writer.WriteValue(pdb.RecordName);
                writer.WritePropertyName("Name");
                writer.WriteValue(pdb.Name);
            }
            else if (atom is Mol2Atom)
            {
                var mol2 = atom as Mol2Atom;
                writer.WritePropertyName("Name");
                writer.WriteValue(mol2.Name);
                writer.WritePropertyName("Mol2Charge");
                writer.WriteValue(Math.Round(mol2.PartialCharge, 5));
            }
            writer.WriteEndObject();
        }

        static void WriteBond(IBond bond, JsonWriter writer)
        {
            writer.WriteStartObject();
            WritePropertyValue(writer, "A", bond.A.Id);
            WritePropertyValue(writer, "B", bond.B.Id);
            WritePropertyValue(writer, "Type", bondTypeCache[bond.Type]);
            writer.WriteEndObject();
        }

        static void WriteResidue(PdbResidue residue, JsonWriter writer)
        {

            writer.WriteStartObject();
            WritePropertyValue(writer, "Name", residue.Name);
            WritePropertyValue(writer, "Chain", residue.ChainIdentifier);
            WritePropertyValue(writer, "SerialNumber", residue.Number);
            WritePropertyValue(writer, "InsertionCode", residue.InsertionResidueCode);
            writer.WritePropertyName("Atoms");
            writer.WriteStartArray();
            foreach (var a in residue.Atoms) writer.WriteValue(a.Id);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        static void WriteCreateResidueId(PdbResidue residue, JsonWriter writer)
        {
            writer.WriteStartObject();
            WritePropertyValue(writer, "Chain", residue.ChainIdentifier);
            WritePropertyValue(writer, "SerialNumber", residue.Number);
            writer.WriteEndObject();
        }

        static void WriteSecondary(PdbSecondaryElement element, JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("StartResidue");
            WriteCreateResidueId(element.StartResidue, writer);
            writer.WritePropertyName("EndResidue");
            WriteCreateResidueId(element.EndResidue, writer);
            writer.WriteEndObject();
        }

        static void WriteArrayProperty<T>(JsonWriter writer, string name, IEnumerable<T> xs, Action<T, JsonWriter> fw)
        {
            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var x in xs) fw(x, writer);
            writer.WriteEndArray();
        }

        static void WritePropertyValue<T>(JsonWriter writer, string name, T value)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var structure = value as IStructure;
            bool isPdb = structure.IsPdbStructure(),
                isMol2 = structure.IsMol2();

            writer.WriteStartObject();

            WritePropertyValue(writer, "Version", "0.2");
            if (isPdb) WritePropertyValue(writer, "Type", "PDB");
            else if (isPdb) WritePropertyValue(writer, "Type", "MOL2");
            else WritePropertyValue(writer, "Type", "Generic");

            // Atoms
            writer.WritePropertyName("Atoms");
            writer.WriteStartObject();
            foreach (var a in structure.Atoms)
            {
                writer.WritePropertyName(a.Id.ToString());
                WriteAtom(a, writer);
            }
            writer.WriteEndObject();

            // Bonds
            WriteArrayProperty(writer, "Bonds", structure.Bonds, WriteBond);

            // Pdb stuff
            if (isPdb)
            {
                WriteArrayProperty(writer, "Residues", structure.PdbResidues(), WriteResidue);
                WriteArrayProperty(writer, "Helices", structure.PdbHelices(), WriteSecondary);
                WriteArrayProperty(writer, "Sheets", structure.PdbSheets(), WriteSecondary);
                writer.WritePropertyName("Metadata");
                serializer.Serialize(writer, structure.PdbMetadata());
            }

            if (isMol2)
            {
                WriteArrayProperty(writer, "Residues", structure.PdbResidues(), WriteResidue);
            }

            writer.WriteEndObject();
        }
    }
}
