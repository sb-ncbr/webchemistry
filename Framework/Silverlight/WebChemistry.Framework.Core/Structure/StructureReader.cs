namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Core.MdlMol;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Math;

#if !SILVERLIGHT
    using System.IO.Compression;
#endif

    /// <summary>
    /// Determines the type of the structure that needs to be parsed.
    /// </summary>
    public enum StructureReaderType
    {
        Auto = 0,
        Pdb,
        PdbQt,
        PdbAssembly,
        PdbX,
        Mol,
        Mol2
    }

    /// <summary>
    /// Result of the read operation.
    /// </summary>
    public class StructureReaderResult
    {   
        /// <summary>
        /// The filename.
        /// </summary>
        public string Filename { get; private set; }
        
        /// <summary>
        /// The structure.
        /// </summary>
        public IStructure Structure { get; private set; }

        /// <summary>
        /// Warnings produced during the loading.
        /// </summary>
        public ReadOnlyCollection<StructureReaderWarning> Warnings { get; private set; }
        
        /// <summary>
        /// Successfully loaded structure.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="structure"></param>
        /// <param name="warnings"></param>
        /// <returns></returns>
        public static StructureReaderResult Success(string filename, IStructure structure, IList<StructureReaderWarning> warnings = null)
        {
            return new StructureReaderResult
            {
                Filename = filename,
                Structure = structure,
                Warnings = warnings == null
                    ? new ReadOnlyCollection<StructureReaderWarning>(new StructureReaderWarning[0])
                    : new ReadOnlyCollection<StructureReaderWarning>(warnings)
            };
        }
        
        private StructureReaderResult()
        {

        }
    }
    
    /// <summary>
    /// PDB reader parameters.
    /// </summary>
    public class PdbReaderParams
    {
        public enum StructureType
        {
            Pdb,
            Pqr,
            PdbX
        }

        /// <summary>
        /// Id of the structure.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Loads only the first model. Default is true.
        /// </summary>
        public bool LoadFirstModelOnly { get; set; }

        /// <summary>
        /// Is this the PQR format?
        /// </summary>
        public StructureType Type { get; set; }
        
        /// <summary>
        /// Default params.
        /// </summary>
        public PdbReaderParams()
        {
            LoadFirstModelOnly = true;
            Type = StructureType.Pdb;
        }
    }

    /// <summary>
    /// The mighty reader.
    /// </summary>
    public static class StructureReader
    {
        /// <summary>
        /// Supported extensions (".ext" format).
        /// </summary>
        public static readonly IEnumerable<string> SupportedExtensions = new string[] { 
            ".pdb", // PDB
            ".pdb0", ".pdb1",".pdb2",".pdb3",".pdb4",".pdb5",".pdb6",".pdb7",".pdb8",".pdb9", // PDB Assembly
            ".pqr", // PQR
            ".mol", ".mdl", ".sdf", ".sd", ".ml2", ".mol2", // MDL
            ".cif", ".mmcif" // PDBx/mmCIF
            #if !SILVERLIGHT
            , ".gz" // GZip compressed stuff
            #endif
        };
        
        /// <summary>
        /// Determines whether the filename is a supported file type.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool IsStructureFilename(string filename)
        {
            #if !SILVERLIGHT
            if (filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)) filename = filename.Substring(0, filename.Length - 3);
            #endif

            var index = filename.LastIndexOf('.');
            if (index < 0) return false;
            string ext = filename.Substring(index).ToLowerInvariant();

            // PDB and assemblies.
            if (!ext.EqualOrdinalIgnoreCase(".pdbqt") && ext.StartsWith(".pdb", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            switch (ext)
            {
                case ".mol":
                case ".mdl":
                case ".sdf":
                case ".sd":
                case ".pdbqt":
                case ".ml2":
                case ".mol2":
                case ".pqr":
                case ".mmcif":
                case ".cif":
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Return a structure type from filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static StructureType GetStructureType(string filename)
        {
            #if !SILVERLIGHT
            if (filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)) filename = filename.Substring(0, filename.Length - 3);
            #endif

            string ext = filename.Substring(filename.LastIndexOf('.')).ToLowerInvariant();
            if (!ext.EqualOrdinalIgnoreCase(".pdbqt") && ext.StartsWith(".pdb", StringComparison.OrdinalIgnoreCase))
            {
                return StructureType.Pdb;
            }
            switch (ext)
            {
                case ".mol":
                case ".mdl":
                case ".sdf":
                case ".sd":
                    return StructureType.Mol;
                case ".pdbqt":
                    return StructureType.PdbQt;
                case ".ml2":
                case ".mol2":
                    return StructureType.Mol2;
                case ".pqr":
                    return StructureType.Pqr;
                case ".mmcif":
                case ".cif":
                    return StructureType.Pdb;
                default:
                    break;
            }

            return StructureType.Unknown;
        }

        /// <summary>
        /// The file name without extension.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetStructureIdFromFilename(string filename)
        {
            #if !SILVERLIGHT
            if (filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)) filename = filename.Substring(0, filename.Length - 3);
            #endif

            var slashIndex = filename.LastIndexOfAny(@"/\".ToCharArray()) + 1;
            var dotIndex = filename.LastIndexOf('.');

            if (slashIndex < dotIndex) return filename.Substring(slashIndex, dotIndex - slashIndex);
            return filename.Substring(slashIndex);
        }


#if !SILVERLIGHT
        /// <summary>
        /// The file name without extension.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static string GetStructureIdFromFileInfo(FileInfo fileInfo)
        {
            return GetStructureIdFromFilename(fileInfo.Name);           
            //return filename.Substring(0, filename.Length - fileInfo.Extension.Length);
        }

        /// <summary>
        /// Read a structure from a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="customId"></param>
        /// <returns></returns>
        public static StructureReaderResult Read(string filename, string customId = null, StructureReaderType customType = StructureReaderType.Auto)
        {
            return Read(filename, () => File.OpenRead(filename), customId, customType);
        }

        /// <summary>
        /// Read a structure from a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="customId"></param>
        /// <returns></returns>
        public static Maybe<StructureReaderResult> TryRead(string filename, string customId = null, StructureReaderType customType = StructureReaderType.Auto)
        {
            return TryRead(filename, () => File.OpenRead(filename), customId, customType);
        }
#endif

        static Func<TextReader> WrapReader(Func<TextReader> readerProvider)
        {
            return new Func<TextReader>(() =>
            {                    
                try
                {
                    var reader = readerProvider();
                    if (reader is StringReader) return reader;
                    string text = null;
                    try
                    {
                        text = reader.ReadToEnd();
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                    return new StringReader(text);
                }
                catch (Exception e)
                {
                    throw new IOException("Error opening input file: " + e.Message);
                }
            });
        }

        static Func<TextReader> GetReader(string filename, Func<Stream> streamProvider)
        {
            return new Func<TextReader>(() =>
            {
                try
                {
                    var stream = streamProvider();
                    #if !SILVERLIGHT
                    if (filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    {
                        return new StreamReader(new GZipStream(stream, CompressionMode.Decompress));
                    }
                    #endif
                    return new StreamReader(stream);                    
                }
                catch (Exception e)
                {
                    throw new IOException("Error opening input file: " + e.Message);
                }
            });
        }

        /// <summary>
        /// Read a structure from the provided stream.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="baseStreamProvider"></param>
        /// <param name="customId"></param>
        /// <param name="customType"></param>
        /// <returns></returns>
        public static StructureReaderResult Read(string filename, Func<Stream> baseStreamProvider, string customId = null, StructureReaderType customType = StructureReaderType.Auto)
        {
            return ReadInternal(filename, GetReader(filename, baseStreamProvider), customId, customType);
        }

        /// <summary>
        /// Reads a structure from string source.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="source"></param>
        /// <param name="customId"></param>
        /// <param name="customType"></param>
        /// <returns></returns>
        public static StructureReaderResult ReadString(string filename, string source, string customId = null, StructureReaderType customType = StructureReaderType.Auto)
        {
            if (filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(".gz files are not supported by StructureReader.ReadString().");
            return ReadInternal(filename, () => new StringReader(source), customId, customType);
        }

        /// <summary>
        /// Read a structure from a stream.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="customId"></param>
        /// <param name="readerProvider"></param>
        /// <returns></returns>
        static StructureReaderResult ReadInternal(string filename, Func<TextReader> readerProvider, string customId = null, StructureReaderType customType = StructureReaderType.Auto)
        {
            StructureReaderResult s = null;
            #if !SILVERLIGHT
            if (filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)) filename = filename.Substring(0, filename.Length - 3);
            #endif
            string ext = filename.Substring(filename.LastIndexOf('.')).ToLowerInvariant();

            readerProvider = WrapReader(readerProvider);

            switch (customType)
            {
                case StructureReaderType.Pdb: ext = ".pdb"; break;
                case StructureReaderType.PdbQt: ext = ".pdbqt"; break;
                case StructureReaderType.PdbAssembly: ext = ".pdb0"; break;
                case StructureReaderType.PdbX: ext = ".cif"; break;
                case StructureReaderType.Mol: ext = ".mol"; break;
                case StructureReaderType.Mol2: ext = ".mol2"; break;
                default: break;
            }

            // PDB and assemblies.
            if (!ext.EqualOrdinalIgnoreCase(".pdbqt") && ext.StartsWith(".pdb", StringComparison.OrdinalIgnoreCase))
            {
                return StructureReader.ReadPdb(filename, readerProvider, new PdbReaderParams { LoadFirstModelOnly = ext.Length == 4, Id = customId ?? StructureReader.GetStructureIdFromFilename(filename) });
            }

            switch (ext)
            {
                case ".mol":
                case ".mdl":
                case ".sdf":
                case ".sd":
                    s = StructureReader.ReadMdlMol(filename, readerProvider, customId);
                    break;
                case ".pdbqt":
                    s = StructureReader.ReadPdbQt(filename, readerProvider, new PdbReaderParams { LoadFirstModelOnly = true, Id = customId ?? StructureReader.GetStructureIdFromFilename(filename) });
                    break;
                case ".pqr":
                    s = StructureReader.ReadPdb(filename, readerProvider, new PdbReaderParams { LoadFirstModelOnly = true, Id = customId ?? StructureReader.GetStructureIdFromFilename(filename), Type = PdbReaderParams.StructureType.Pqr });
                    break;
                case ".cif":
                case ".mmcif":
                    s = StructureReader.ReadPdb(filename, readerProvider, new PdbReaderParams { LoadFirstModelOnly = true, Id = customId ?? StructureReader.GetStructureIdFromFilename(filename), Type = PdbReaderParams.StructureType.PdbX });
                    break;
                case ".ml2":
                case ".mol2":
                    s = StructureReader.ReadMol2(filename, readerProvider, customId);
                    break;
                default:
                    throw new IOException("Unsupported file format.");
            }

            return s;
        }

        /// <summary>
        /// Try read a structure from a stream.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="customId"></param>
        /// <param name="readerProvider"></param>
        /// <returns></returns>
        public static Maybe<StructureReaderResult> TryRead(string filename, Func<Stream> baseStreamProvider, string customId = null, StructureReaderType customType = StructureReaderType.Auto)
        {
            try
            {
                var result = Read(filename, baseStreamProvider, customId, customType);
                return result.AsMaybe();
            }
            catch
            {
                return Maybe.Nothing<StructureReaderResult>();
            }
        }
    
        /// <summary>
        /// Loads structure from PDB format - see  http://deposit.rcsb.org/adit/docs/pdb_atom_format.html for more details about the format.
        /// The current implementation is INCOMPLETE IMPLEMENTATION OF PDB FORMAT (loads only ATOM and HETATM records)!
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="reader">Reader</param>
        /// <param name="parameters"></param>
        /// <returns>Structure constructed from the file. INCOMPLETE IMPLEMENTATION OF PDB FORMAT!</returns>
        static StructureReaderResult ReadPdb(string filename, Func<TextReader> readerProvider, PdbReaderParams parameters)
        {
            using (var r = readerProvider())
            {
                return new WebChemistry.Framework.Core.Pdb.PdbReader(filename, r, parameters).Read();
            }
        }


        /// <summary>
        /// Read PDBQt file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="reader"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        static StructureReaderResult ReadPdbQt(string filename, Func<TextReader> readerProvider, PdbReaderParams parameters)
        {
            using (var r = readerProvider())
            {
                return new WebChemistry.Framework.Core.PdbQt.PdbQtReader(filename, r, parameters).Read();
            }
        }

        static readonly System.Globalization.CultureInfo InvariantCulture = System.Globalization.CultureInfo.InvariantCulture;
               
        /// <summary>
        /// Loads structure from MDL MOL format - see http://www.symyx.com/downloads/public/ctfile/ctfile.pdf for more details about the format.
        /// The current implementation is INCOMPLETE IMPLEMENTATION OF MDL MOL FORMAT!
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="readerProvider">input stream</param>
        /// <param name="customId"></param>
        /// <returns>Structure constructed from the file. INCOMPLETE IMPLEMENTATION OF MDL MOL FORMAT!</returns>
        static StructureReaderResult ReadMdlMol(string filename, Func<TextReader> readerProvider, string customId = null)
        {
            int currentLine = 0;
            try
            {
                using (var reader = readerProvider())
                {
                    currentLine++;
                    reader.ReadLine(); // read the ID line
                    string id = customId ?? GetStructureIdFromFilename(filename);
                    if (!string.IsNullOrEmpty(customId)) id = customId;

                    currentLine++;
                    string molHeaderInfo = reader.ReadLine();
                    currentLine++;
                    string molHeaderComment = reader.ReadLine();
                    currentLine++;
                    string cTabInfo = reader.ReadLine();

                    //if (cTabInfo == null || !cTabInfo.Substring(34, 5).Equals("V2000"))
                    //{
                    //    throw new IOException("Not a valid MOL V2000 format.");
                    //}

                    //var infoArray = cTabInfo.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int numAtoms = int.Parse(cTabInfo.Substring(0, 3));
                    int numBonds = int.Parse(cTabInfo.Substring(3, 3));

                    bool isChiral = int.Parse(cTabInfo.Substring(12, 3)) == 1;

                    List<IAtom> atoms = new List<IAtom>(numAtoms);
                    List<IBond> bonds = new List<IBond>(numBonds);

                    for (int i = 0; i < numAtoms; i++)
                    {
                        currentLine++;
                        string line = reader.ReadLine();
                        atoms.Add(ParseMolAtom(line, i + 1));
                    }

                    for (int i = 0; i < numBonds; i++)
                    {
                        currentLine++;
                        string line = reader.ReadLine();

                        //var bondInfo = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        //int from = int.Parse(bondInfo[0]) - 1;
                        //int to = int.Parse(bondInfo[1]) - 1;
                        //int type = int.Parse(bondInfo[2]);

                        int from = int.Parse(line.Substring(0, 3)) - 1;
                        int to = int.Parse(line.Substring(3, 3)) - 1;
                        int type = int.Parse(line.Substring(6, 3));

                        BondType bondType;

                        IAtom a = atoms[from], b = atoms[to];
                        switch (type)
                        {
                            case 1:
                                bondType = BondType.Single;
                                break;
                            case 2:
                                bondType = BondType.Double;
                                break;
                            case 3:
                                bondType = BondType.Triple;
                                break;
                            case 4:
                                bondType = BondType.Aromatic;
                                break;
                            default:
                                bondType = BondType.Unknown;
                                break;
                        }

                        IBond bond = Bond.Create(a, b, bondType);
                        bonds.Add(bond);
                    }

                    IStructure ret = Structure.Create(id, AtomCollection.Create(atoms), BondCollection.Create(bonds));
                    //ret.SetProperty(StructureProperties.MolHeaderInfo, molHeaderInfo);
                    //ret.SetProperty(StructureProperties.MolHeaderComment, molHeaderComment);
                    //ret.SetProperty(StructureProperties.IsChiral, isChiral);

                    return StructureReaderResult.Success(filename, ret);
                }
            }
            catch (Exception e)
            {
                var error = string.Format("Not a valid MOL/SDF/SD format. Error at line {0}: {1}", currentLine, e.Message);
                throw new IOException(error);
            }
        }

        private static IAtom ParseMolAtom(string line, int index)
        {
            string es = line.Substring(31, 3).Trim();

            double x = NumberParser.ParseDoubleFast(line, 0, 10);
            double y = NumberParser.ParseDoubleFast(line, 10, 10);
            double z = NumberParser.ParseDoubleFast(line, 20, 10);

            MdlMolStereoParity parity;
            string ps = line.Substring(39, 3).Trim();
            switch (ps)
            {
                case "0": parity = MdlMolStereoParity.NotStereo; break;
                case "1": parity = MdlMolStereoParity.Odd; break;
                case "2": parity = MdlMolStereoParity.Even; break;
                default: parity = MdlMolStereoParity.EitherOrUnmarked; break;
            }

            return MdlMolAtom.Create(index, ElementSymbol.Create(es), stereoParity: parity, position: new WebChemistry.Framework.Math.Vector3D(x, y, z));
        }


        /// <summary>
        /// Loads structure from MOL2 format - see http://tripos.com/tripos_resources/fileroot/pdfs/mol2_format.pdf for more details about the format.
        /// The current implementation is INCOMPLETE IMPLEMENTATION OF MOL1 FORMAT!
        /// If the file contains partial charges, saves them to the "PartialCharge" property.
        /// </summary>
        /// <param name="reader">input stream</param>
        /// <returns>Structure constructed from the file. INCOMPLETE IMPLEMENTATION OF MOL2 FORMAT!</returns>
        static StructureReaderResult ReadMol2(string filename, Func<TextReader> readerProvider, string customId = null)
        {
            int currentLine = 0;
            try
            {
                using (var reader = readerProvider())
                {
                    bool loadCharges = false;
                    string line;
                    string id = null;

                    List<Mol2Atom> atoms = null;
                    List<IBond> bonds = null;

                    int numAtoms = 0;
                    int numBonds = 0;

                    while (true) //!reader.EndOfStream
                    {
                        currentLine++;
                        line = reader.ReadLine();
                        if (line == null) break;

                        if (line.StartsWith("#") || line.Trim().Length < 1) continue;

                        if (line.StartsWith("@<TRIPOS>MOLECULE"))
                        {
                            currentLine++;
                            reader.ReadLine(); // read the id line .Trim().Split(new char[] { '.' })[0]; 
                            id = customId ?? GetStructureIdFromFilename(filename);
                            if (!string.IsNullOrEmpty(customId)) id = customId;

                            currentLine++;
                            var info = reader.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                            numAtoms = int.Parse(info[0]);
                            numBonds = int.Parse(info[1]);

                            atoms = new List<Mol2Atom>(numAtoms);
                            bonds = new List<IBond>(numBonds);

                            currentLine++;
                            reader.ReadLine();

                            currentLine++;
                            string charges = reader.ReadLine().Trim();

                            if (!charges.StartsWith("NO_CHARGES")) loadCharges = true;

                            break;
                        }
                    }

                    while (true) // !reader.EndOfStream
                    {
                        currentLine++;
                        line = reader.ReadLine();
                        if (line == null) break;

                        if (line.StartsWith("#") || line.Trim().Length < 1) continue;

                        if (line.StartsWith("@<TRIPOS>ATOM"))
                        {
                            for (int i = 0; i < numAtoms; i++)
                            {
                                currentLine++;
                                line = reader.ReadLine();
                                atoms.Add(ParseMol2Atom(line, i + 1, loadCharges));
                            }

                            break;
                        }
                    }

                    while (true) // !reader.EndOfStream
                    {
                        currentLine++;
                        line = reader.ReadLine();
                        if (line == null) break;

                        if (line.StartsWith("#") || line.Trim().Length < 1) continue;

                        if (line.StartsWith("@<TRIPOS>BOND"))
                        {
                            for (int i = 0; i < numBonds; i++)
                            {
                                currentLine++;
                                var info = reader.ReadLine().Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                                int from = int.Parse(info[1]) - 1;
                                int to = int.Parse(info[2]) - 1;
                                string type = info[3].ToLower();

                                BondType bondType;

                                IAtom a = atoms[from], b = atoms[to];
                                switch (type)
                                {
                                    case "1": bondType = BondType.Single; break;
                                    case "2": bondType = BondType.Double; break;
                                    case "3": bondType = BondType.Triple; break;
                                    case "ar": bondType = BondType.Double; break;
                                    case "am": bondType = BondType.Single; break;
                                    default: bondType = BondType.Unknown; break;
                                }

                                IBond bond = Bond.Create(a, b, bondType);
                                bonds.Add(bond);
                            }

                            break;
                        }
                    }

                    var ret = Structure.Create(id, AtomCollection.Create(atoms), BondCollection.Create(bonds));
                    ret.SetProperty(Mol2Ex.IsMol2Property, true);
                    if (loadCharges)
                    {
                        ret.SetProperty(Mol2Ex.Mol2ContainsChargesProperty, true);
                    }

                    var residues = PdbResidueCollection.Create(atoms.GroupBy(a => a.ResidueIdentifier).Select(g => PdbResidue.Create(g)));
                    ret.SetProperty(PdbStructure.ResiduesProperty, residues);                    

                    return StructureReaderResult.Success(filename, ret);
                }
            }
            catch (Exception e)
            {
                var error = string.Format("Not a valid MOL2 format. Error at line {0}: {1}", currentLine, e.Message);
                throw new IOException(error);
            }
        }

        private static Mol2Atom ParseMol2Atom(string line, int index, bool loadCharges)
        {
            var info = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (loadCharges && info.Length < 9) { throw new InvalidOperationException("Invalid atom format. Required format: Id Name PosX PosY PosZ Type ResId ResName Charge"); }
            if (!loadCharges && info.Length < 8) { throw new InvalidOperationException("Invalid atom format. Required format: Id Name PosX PosY PosZ Type ResId ResName"); }

            string es = info[5].Split(new char[] { '.' })[0];

            double x = NumberParser.ParseDoubleFast(info[2], 0, info[2].Length);
            double y = NumberParser.ParseDoubleFast(info[3], 0, info[3].Length);
            double z = NumberParser.ParseDoubleFast(info[4], 0, info[4].Length);

            var resId = NumberParser.ParseIntFast(info[6], 0, info[6].Length);
            var resName = info[7];

            var position = new WebChemistry.Framework.Math.Vector3D(x, y, z);
            double charge = double.Parse(info[8], System.Globalization.CultureInfo.InvariantCulture);

            return (Mol2Atom)Mol2Atom.Create(index, ElementSymbol.Create(es), info[5], info[1],
                new PdbResidueIdentifier(resId, "A", ' '),
                resName,
                charge, position);
        }
    }
}