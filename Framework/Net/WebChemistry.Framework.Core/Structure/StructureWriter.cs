namespace WebChemistry.Framework.Core
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using WebChemistry.Framework.Core.Pdb;

    public static class StructureWriter
    {
        static System.Globalization.CultureInfo InvariantCulture = System.Globalization.CultureInfo.InvariantCulture;

        #region PDB/PQR
        static string PadPdbAtomName(string name)
        {
            if (name.Length == 1) return string.Format(" {0}  ", name);
            if (name.Length == 2) return string.Format(" {0} ", name);
            if (name.Length == 3) return string.Format(" {0}", name);
            return name;
        }

        /// <summary>
        /// Get the PDB atom record for a given atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static string GetPdbAtomRecord(IAtom atom)
        {
            return string.Format(InvariantCulture, 
                "{0}{1,5} {2}{3}{4} {5}{6,4}{7}   {8,8:#0.000}{9,8:#0.000}{10,8:#0.000}{11,6:#0.00}{12,6:#0.00}      {13}{14}{15}", 

                atom.PdbRecordName().PadRight(6),
                atom.PdbSerialNumber() % 100000,

                PadPdbAtomName(atom.PdbName()),
                atom.PdbAlternateLocationIdentifier(),
                atom.PdbResidueName().PadLeft(3),

                string.IsNullOrWhiteSpace(atom.PdbChainIdentifier()) ? ' ' : atom.PdbChainIdentifier()[0],
                atom.PdbResidueSequenceNumber() % 10000,
                atom.PdbInsertionResidueCode(),

                atom.Position.X,
                atom.Position.Y,
                atom.Position.Z,

                atom.PdbOccupancy(),
                atom.PdbTemperatureFactor(),

                atom.PdbSegmentIdentifier().PadRight(4),
                atom.ElementSymbol.ToString().ToUpper().PadLeft(2),
                atom.PdbCharge().PadRight(2));
        }

        static string PqrFormatNumber(double num, string format, int width)
        {
            var ret = num.ToStringInvariant(format);
            if (ret.Length >= width) return ret + " ";
            return ret;
        }

        static string PqrFormatString(string str, int width)
        {
            if (str.Length >= width) return str + ' ';
            return str.PadRight(width);
        }

        /// <summary>
        /// Get the PQR atom record.
        /// </summary>
        /// <param name="atom">The atom to convert to a PQR record.</param>
        /// <param name="charge">The partial charge of the atom.</param>
        /// <param name="emptyChainId">The character to use for atoms without a chain identifier.</param>
        /// <returns>The PQR atom record string.</returns>
        public static string GetPqrAtomRecord(IAtom atom, double charge, char emptyChainId)
        {

            // "%s%5d %-4s %-3s %c%4d    %8.3f%8.3f%8.3f %11.8f%8.3f %2s  \n"
            ////return string.Format(InvariantCulture,
            ////     "{0}{1,5} {2} {3} {4}{5,4}    {6,8:#0.000}{7,8:#0.000}{8,8:#0.000} {9,11:#0.00000000} {10,8:#0.000} {11}",

            ////     atom.PdbRecordName().PadRight(6), // 0
            ////     atom.PdbSerialNumber() % 100000, // 1

            ////     PadPdbAtomName(atom.PdbName()), // 2
                 
            ////     atom.PdbResidueName().PadLeft(3), // 3
            ////     string.IsNullOrWhiteSpace(atom.PdbChainIdentifier()) ? ' ' : atom.PdbChainIdentifier()[0], // 4
            ////     atom.PdbResidueSequenceNumber() % 10000, // 5

            ////     atom.Position.X, // 6
            ////     atom.Position.Y, // 7
            ////     atom.Position.Z, // 8

            ////     charge, // 9
            ////     ElementAndBondInfo.GetVdwRadius(atom), // 10
            ////     atom.ElementSymbol); // 11

            return string.Format(InvariantCulture,
                "{0}{1,5} {2}{3}{4} {5}{6,4}{7}   {8,8:#0.000}{9,8:#0.000}{10,8:#0.000} {11,11:#0.00000000} {12,8:#0.000} {13}",

                atom.PdbRecordName().PadRight(6),
                atom.PdbSerialNumber() % 100000,

                PadPdbAtomName(atom.PdbName()),
                ' ', // alt loc
                atom.PdbResidueName().PadLeft(3),

                string.IsNullOrWhiteSpace(atom.PdbChainIdentifier()) ? emptyChainId : atom.PdbChainIdentifier()[0],
                atom.PdbResidueSequenceNumber() % 10000,
                ' ', // ins code

                atom.Position.X,
                atom.Position.Y,
                atom.Position.Z, // 10

                charge, // 11
                ElementAndBondInfo.GetVdwRadius(atom), // 12
                atom.ElementSymbol); // 13

            ////return string.Format(InvariantCulture,
            ////    // RecordName0 Serial1 AtomName2 ResidueName3 [ChainID]4 ResidueNumber5 X6 Y7 Z8 Charge9 Radius10
            ////    "{0} {1} {2} {3} {4} {5} {6:#0.000} {7:#0.000} {8:#0.000} {9:#0.000} {10:#0.000}",

            ////    atom.PdbRecordName().PadRight(6),
            ////    atom.PdbSerialNumber(),
            ////    atom.PdbName(),
            ////    atom.PdbResidueName(),
            ////    string.IsNullOrWhiteSpace(atom.PdbChainIdentifier()) ? "" : " " + atom.PdbChainIdentifier() + " ",
            ////    atom.PdbResidueSequenceNumber() % 10000,
            ////    atom.Position.X,
            ////    atom.Position.Y,
            ////    atom.Position.Z,
            ////    charge,
            ////    atom.PqrRadius());
        }

        /// <summary>
        /// Writes a PDB representation to a file. Shortcut for StructureWriter.WritePdb.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="writer"></param>
        /// <param name="sortAtoms"></param>
        public static void WritePdb(this IStructure structure, TextWriter writer, bool sortAtoms = true)
        {
            //List<IAtom> connectAtoms = new List<IAtom>();
            var atoms = sortAtoms ? structure.Atoms.OrderBy(a => a.PdbChainIdentifier()).ThenBy(a => a.PdbSerialNumber()) : structure.Atoms.AsEnumerable();
            writer.WriteLine(string.Format(InvariantCulture, "AUTHOR    Generated by WebChemistry Core {0}, {1}", CoreVersion.Version, DateTime.UtcNow));
            //var includeAllConnects = true;
            foreach (var a in atoms)
            {
                writer.WriteLine(GetPdbAtomRecord(a));
                ////if (includeAllConnects/* ||
                ////    (
                ////    !a.IsWater() 
                ////    && a.Id <= 99999 
                ////    && a.PdbRecordName().StartsWith("HETATM", StringComparison.Ordinal)
                ////    && ElementAndBondInfo.IsMetalSymbol(a.ElementSymbol)
                ////    )*/)
                ////{
                ////    connectAtoms.Add(a);
                ////}
            }

            foreach (var a in atoms) //connectAtoms)
            {
                var bonds = structure.Bonds[a];
                if (bonds.Count == 0) continue;

                for (int i = 0; i < bonds.Count / 4; i++)
                {
                    writer.Write("CONECT");
                    writer.Write("{0,5}", a.Id % 100000);
                    writer.Write("{0,5}", bonds[i * 4 + 0].B.Id % 100000);
                    writer.Write("{0,5}", bonds[i * 4 + 1].B.Id % 100000);
                    writer.Write("{0,5}", bonds[i * 4 + 2].B.Id % 100000);
                    writer.Write("{0,5}", bonds[i * 4 + 3].B.Id % 100000);
                    writer.Write(Environment.NewLine);
                }

                if (bonds.Count % 4 > 0)
                {
                    writer.Write("CONECT");
                    writer.Write("{0,5}", a.Id % 100000);
                    for (int i = bonds.Count % 4; i > 0; i--)
                    {
                        writer.Write("{0,5}", bonds[bonds.Count - i].B.Id % 100000);
                    }
                    writer.Write(Environment.NewLine);
                }
            }
        }


        /// <summary>
        /// Writes a PDB representation to a file. Shortcut for StructureWriter.WritePdb.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="filename"></param>
        /// <param name="sortAtoms"></param>
        public static void WritePdb(this IStructure structure, string filename, bool sortAtoms = true)
        {
            using (var file = File.CreateText(filename)) structure.WritePdb(file, sortAtoms);
        }

        /// <summary>
        /// Writes PQR to a file.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="filename"></param>
        /// <param name="chargeSelector"></param>
        /// <param name="sortAtoms"></param>
        public static void WritePqr(this IStructure structure, string filename, Func<IAtom, double> chargeSelector = null, bool sortAtoms = true)
        {
            using (var file = File.CreateText(filename)) structure.WritePqr(file, chargeSelector, sortAtoms);
        }


        /// <summary>
        /// Convert a structure to a PDB string.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="sortAtoms"></param>
        /// <returns></returns>
        public static string ToPdbString(this IStructure structure, bool sortAtoms = true)
        {
            StringBuilder sb = new StringBuilder();
            using (var w = new StringWriter(sb))
            {
                structure.WritePdb(w, sortAtoms);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Writes PQR format.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="chargeSelector"></param>
        /// <param name="sortAtoms"></param>
        /// <returns></returns>
        public static string ToPqrString(this IStructure structure, Func<IAtom, double> chargeSelector = null, bool sortAtoms = true)
        {
            StringBuilder sb = new StringBuilder();
            using (var w = new StringWriter(sb))
            {
                structure.WritePqr(w, chargeSelector, sortAtoms);
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Writes a PQR file.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="writer"></param>
        /// <param name="chargeSelector">if null, select PqrCharge()</param>
        /// <param name="sortAtoms"></param>
        public static void WritePqr(this IStructure structure, TextWriter writer, Func<IAtom, double> chargeSelector = null, bool sortAtoms = true)
        {
            List<IAtom> connectAtoms = new List<IAtom>();
            if (chargeSelector == null) chargeSelector = a => a.PqrCharge();

            var atoms = sortAtoms ? structure.Atoms.OrderBy(a => a.PdbChainIdentifier()).ThenBy(a => a.PdbSerialNumber()) : structure.Atoms.AsEnumerable();

            writer.WriteLine(string.Format(InvariantCulture, "AUTHOR    Generated by WebChemistry Core {0}, {1}", CoreVersion.Version, DateTime.UtcNow));

            char emptyChainId = (char)0;
            foreach (var atom in atoms)
            {
                char cid = string.IsNullOrWhiteSpace(atom.PdbChainIdentifier()) ? ' ' : atom.PdbChainIdentifier()[0];
                emptyChainId = (char)Math.Max(emptyChainId, cid);
            }

            if (emptyChainId == ' ') emptyChainId = 'A';
            else if (emptyChainId == 'z' || emptyChainId == 'Z') emptyChainId = '0';
            else emptyChainId = (char)(emptyChainId + 1);

            foreach (var a in atoms)
            {
                writer.WriteLine(GetPqrAtomRecord(a, chargeSelector(a), emptyChainId));
            }
        }

        #endregion


        #region MOL2
        //static int GetBondType(IList<IBond> bonds)
        //{
        //    if (bonds == null || bonds.Count == 0) return 1;

        //    int ret = int.MinValue;
        //    for (int i = 0; i < bonds.Count; i++)
        //    {
        //        var b = bonds[i];
        //        int val = b.Type == BondType.Metallic ? 1 : (int)b.Type;
        //        if (val > ret) ret = val;
        //    }
        //    return ret;
        //}

        //static string GetSybylAtomType(IStructure structure, IAtom atom)
        //{
        //    var mol2Atom = atom as Mol2Atom;
        //    if (mol2Atom != null) return mol2Atom.AtomType;

        //    if (atom.ElementSymbol == ElementSymbols.C)
        //    {
        //        var bondType = GetBondType(structure.Bonds[atom]);
        //        if (bondType == 1) return "C.1";
        //        if (bondType == 2) return "C.2";
        //        if (bondType == 3) return "C.3";
        //        return "C";
        //    }
        //    else if (atom.ElementSymbol == ElementSymbols.N)
        //    {
        //        var bondType = GetBondType(structure.Bonds[atom]);
        //        if (bondType == 1) return "N.1";
        //        if (bondType == 2) return "N.2";
        //        if (bondType == 3) return "N.3";
        //        return "N";
        //    }
        //    else if (atom.ElementSymbol == ElementSymbols.O)
        //    {
        //        var bondType = GetBondType(structure.Bonds[atom]);
        //        if (bondType == 1) return "O.2";
        //        if (bondType == 2) return "O.3";
        //        return "O";
        //    }
        //    else
        //    {
        //        return atom.ElementSymbol.ToString();
        //    }
        //}

        static string GetMol2BondType(IBond bond)
        {
            int i = (int)bond.Type;
            switch (i)
            {
                case 1: case 2: case 3: return i.ToString();
                case 4: return "ar";
                case 5: return "1";
                default: return "1";
            }
        }

        /// <summary>
        /// Write structure as Mol2.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="writer"></param>
        /// <param name="remark"></param>
        /// <param name="chargeSelector"></param>
        public static void WriteMol2(this IStructure structure, TextWriter writer, string remark = null, Func<IAtom, double> chargeSelector = null)
        {
            writer.WriteLine("#\tName: " + structure.Id);
            writer.WriteLine("#\tGenerated by WebChemistry Core {0}", CoreVersion.Version);
            writer.WriteLine(string.Format(InvariantCulture, "#\tCreated on " + DateTime.UtcNow.ToLongDateString() + ", " + DateTime.UtcNow.ToLongTimeString()));            
            if (remark != null) writer.WriteLine("#\tRemark: " + remark);

            writer.WriteLine("");

            writer.WriteLine("@<TRIPOS>MOLECULE");
            writer.WriteLine(structure.Id);
            writer.WriteLine(string.Format(" {0} {1} 0 0 0", structure.Atoms.Count, structure.Bonds.Count));
            writer.WriteLine("SMALL");

            if (chargeSelector == null) writer.WriteLine("NO_CHARGES");
            else writer.WriteLine("USER_CHARGES");

            if (chargeSelector == null) chargeSelector = _ => 0.0;

            writer.WriteLine("");

            Func<PdbResidueIdentifier, int> getResidueSerial;
            if (structure.PdbResidues().Select(a => a.ChainIdentifier).Distinct(StringComparer.Ordinal).Count() > 1)
            {
                var residueSerials = structure.PdbResidues().Select((r, i) => new { Id = r.Identifier, Serial = i + 1 }).ToDictionary(r => r.Id, r => r.Serial);
                if (residueSerials.Count == 0) getResidueSerial = _ => 1;
                else if (residueSerials.Count == 1)
                {
                    var serial = structure.PdbResidues()[0].Number;
                    getResidueSerial = _ => serial;
                }
                else
                {
                    getResidueSerial = r =>
                    {
                        int ret;
                        if (residueSerials.TryGetValue(r, out ret)) return ret;
                        return 1;
                    };
                }
            }
            else
            {
                getResidueSerial = id => id.Number;
            }
            
            writer.WriteLine("@<TRIPOS>ATOM");           
            structure.Atoms.ForEach((a, i) =>
            {
                writer.WriteLine(string.Format(InvariantCulture, "{0,6} {1,-5} {2} {3,-6} {4} {5} {6,9:0.0000}", 
                    i + 1, // ID 0
                    a.PdbName(), // NAME 1
                    a.PositionToString(), // POSITION 2
                    a.ElementSymbol.ToString(), //GetSybylAtomType(structure, a), // TYPE 3
                    getResidueSerial(a.ResidueIdentifier()), // RESID
                    a.PdbResidueName(),
                    chargeSelector(a)));
            });

            writer.WriteLine("");

            Dictionary<int, int> atomIndices = new Dictionary<int, int>();
            structure.Atoms.ForEach((a, i) => atomIndices[a.Id] = i + 1);
            writer.WriteLine("@<TRIPOS>BOND");
            structure.Bonds.ForEach((b, i) =>
            {
                writer.WriteLine(string.Format("{0,6} {1,6} {2,6} {3,6}", i + 1, atomIndices[b.A.Id], atomIndices[b.B.Id], GetMol2BondType(b)));
            });
        }

        /// <summary>
        /// Write MOL2 to file.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="filename"></param>
        /// <param name="remark"></param>
        /// <param name="chargeSelector"></param>
        public static void WriteMol2(this IStructure structure, string filename, string remark = null, Func<IAtom, double> chargeSelector = null)
        {
            using (var file = File.CreateText(filename)) structure.WriteMol2(file, remark, chargeSelector);
        }

        /// <summary>
        /// Convert to MOL2 string.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="remark"></param>
        /// <param name="chargeSelector"></param>
        /// <returns></returns>
        public static string ToMol2String(this IStructure structure, string remark = null, Func<IAtom, double> chargeSelector = null)
        {
            StringBuilder sb = new StringBuilder();
            using (var w = new StringWriter(sb))
            {
                structure.WriteMol2(w, remark, chargeSelector);
            }
            return sb.ToString();
        }
        #endregion

        #region mmCIF
        /// <summary>
        /// Writes a structure in mmCIF (PDBx) format.
        /// </summary>
        /// <param name="structure">The structure to write.</param>
        /// <param name="writer">The text writer to write to.</param>
        public static void WriteMmcif(this IStructure structure, TextWriter writer)
        {
            var ci = InvariantCulture;
            var atoms = structure.Atoms.OrderBy(a => a.PdbChainIdentifier()).ThenBy(a => a.PdbSerialNumber());

            // Get unique chains and residues for category data blocks
            var chains = atoms.Select(a => a.PdbChainIdentifier()).Distinct().OrderBy(c => c).ToList();

            writer.WriteLine("data_" + structure.Id.Replace(" ", "_"));
            writer.WriteLine();

            // Save file header
            writer.WriteLine("#");
            writer.WriteLine("_entry.id " + structure.Id);
            writer.WriteLine("#");
            writer.WriteLine();

            // atom_site category
            writer.WriteLine("loop_");
            writer.WriteLine("_atom_site.group_PDB");
            writer.WriteLine("_atom_site.id");
            writer.WriteLine("_atom_site.type_symbol");
            writer.WriteLine("_atom_site.label_atom_id");
            writer.WriteLine("_atom_site.label_alt_id");
            writer.WriteLine("_atom_site.label_comp_id");
            writer.WriteLine("_atom_site.label_asym_id");
            writer.WriteLine("_atom_site.label_entity_id");
            writer.WriteLine("_atom_site.label_seq_id");
            writer.WriteLine("_atom_site.pdbx_PDB_ins_code");
            writer.WriteLine("_atom_site.Cartn_x");
            writer.WriteLine("_atom_site.Cartn_y");
            writer.WriteLine("_atom_site.Cartn_z");
            writer.WriteLine("_atom_site.occupancy");
            writer.WriteLine("_atom_site.B_iso_or_equiv");
            writer.WriteLine("_atom_site.pdbx_PDB_model_num");
            writer.WriteLine();

            int atomIndex = 1;
            foreach (var a in atoms)
            {
                string recordName = a.PdbRecordName(); // ATOM or HETATM
                int serial = a.PdbSerialNumber() % 100000;
                string element = a.ElementSymbol.ToString().ToUpperInvariant();
                string atomName = a.PdbName();
                char altLoc = a.PdbAlternateLocationIdentifier();
                string residueName = a.PdbResidueName();
                string chainId = string.IsNullOrWhiteSpace(a.PdbChainIdentifier()) ? "." : a.PdbChainIdentifier();
                int entityId = PdbEx.PdbEntityId(a);
                int seqNum = a.PdbResidueSequenceNumber() % 10000;
                char insCode = a.PdbInsertionResidueCode();
                double x = a.Position.X;
                double y = a.Position.Y;
                double z = a.Position.Z;
                double occupancy = a.PdbOccupancy();
                double bFactor = a.PdbTemperatureFactor();
                int modelNum = 1;

                string altLocStr = altLoc == ' ' ? "." : altLoc.ToString();
                string insCodeStr = insCode == ' ' ? "." : insCode.ToString();

                writer.WriteLine(string.Format(ci, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10:F3} {11:F3} {12:F3} {13:F2} {14:F2} {15}",
                    recordName,
                    atomIndex++,
                    element,
                    atomName,
                    altLocStr,
                    residueName,
                    chainId,
                    entityId,
                    seqNum,
                    insCodeStr,
                    x, y, z,
                    occupancy,
                    bFactor,
                    modelNum));
            }

            writer.WriteLine("#");
            writer.WriteLine();

            // Write bond information if available
            if (structure.Bonds.Count > 0)
            {
                writer.WriteLine("loop_");
                writer.WriteLine("_struct_conn.id");
                writer.WriteLine("_struct_conn.ptnr1_label_atom_id");
                writer.WriteLine("_struct_conn.ptnr1_label_comp_id");
                writer.WriteLine("_struct_conn.ptnr1_label_asym_id");
                writer.WriteLine("_struct_conn.ptnr2_label_atom_id");
                writer.WriteLine("_struct_conn.ptnr2_label_comp_id");
                writer.WriteLine("_struct_conn.ptnr2_label_asym_id");
                writer.WriteLine("_struct_conn.pdbx_value_order");
                writer.WriteLine();

                int connIndex = 1;
                foreach (var b in structure.Bonds)
                {
                    string bondType = GetMmcifBondType(b.Type);
                    writer.WriteLine(string.Format(ci, "{0} {1} {2} {3} {4} {5} {6} {7}",
                        connIndex++,
                        b.A.PdbName(),
                        b.A.PdbResidueName(),
                        string.IsNullOrWhiteSpace(b.A.PdbChainIdentifier()) ? "." : b.A.PdbChainIdentifier(),
                        b.B.PdbName(),
                        b.B.PdbResidueName(),
                        string.IsNullOrWhiteSpace(b.B.PdbChainIdentifier()) ? "." : b.B.PdbChainIdentifier(),
                        bondType));
                }
                writer.WriteLine("#");
            }
            writer.WriteLine("#");
        }
        
        static string GetMmcifBondType(BondType type)
        {
            switch (type)
            {
                case BondType.Single: return "S";
                case BondType.Double: return "D";
                case BondType.Triple: return "T";
                case BondType.Aromatic: return "A";
                default: return "?";
            }
        }

        /// <summary>
        /// Writes a structure in mmCIF format to a file.
        /// </summary>
        /// <param name="structure">The structure to write.</param>
        /// <param name="filename">The output filename.</param>
        public static void WriteMmcif(this IStructure structure, string filename)
        {
            using (var file = File.CreateText(filename))
            {
                structure.WriteMmcif(file);
            }
        }

        /// <summary>
        /// Converts a structure to an mmCIF string.
        /// </summary>
        /// <param name="structure">The structure to convert.</param>
        /// <returns>The mmCIF formatted string.</returns>
        public static string ToMmcifString(this IStructure structure)
        {
            StringBuilder sb = new StringBuilder();
            using (var w = new StringWriter(sb))
            {
                structure.WriteMmcif(w);
            }
            return sb.ToString();
        }
        #endregion

        #region MOL
        /// <summary>
        /// Convert to SDF/MOL format.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="writer"></param>
        public static void WriteMol(this IStructure structure, TextWriter writer)
        {
            if (structure.Atoms.Count > 999) throw new InvalidOperationException("Cannot write structure with more than 999 atoms to MOL format.");
            if (structure.Bonds.Count > 999) throw new InvalidOperationException("Cannot write structure with more than 999 bonds to MOL format.");

            writer.WriteLine("{0}", structure.Id);
            writer.WriteLine(string.Format(InvariantCulture, "  Generated by WebChemistry Core {0}, {1}", CoreVersion.Version, DateTime.UtcNow));
            writer.WriteLine();

            writer.WriteLine("{0,3}{1,3}  0  0  0  0  0  0  0  0999 V2000", structure.Atoms.Count, structure.Bonds.Count);
            structure.Atoms.ForEach((a, i) =>
            {
                writer.WriteLine(string.Format(InvariantCulture, "{0,10:F4}{1,10:F4}{2,10:F4} {3,-3} 0  0  0  0  0  0  0  0  0  0  0  0",
                    a.Position.X, a.Position.Y, a.Position.Z, a.ElementSymbol));
            });

            Dictionary<int, int> atomIndices = new Dictionary<int, int>();
            structure.Atoms.ForEach((a, i) => atomIndices[a.Id] = i + 1);
            structure.Bonds.ForEach((b, i) =>
            {
                var type = (int)b.Type;
                if (!(type > 0 && type <= 4)) type = 1;
                writer.WriteLine("{0,3}{1,3}{2,3}  0  0  0  0", atomIndices[b.A.Id], atomIndices[b.B.Id], type);
            });
            writer.WriteLine("M  END");
        }

        /// <summary>
        /// Write MOL to file.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="filename"></param>
        public static void WriteMol(this IStructure structure, string filename)
        {
            using (var file = File.CreateText(filename)) structure.WriteMol(file);
        }

        /// <summary>
        /// Convert to MOL string.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static string ToMolString(this IStructure structure)
        {
            StringBuilder sb = new StringBuilder();
            using (var w = new StringWriter(sb))
            {
                structure.WriteMol(w);
            }
            return sb.ToString();
        }
        #endregion
    }
}
