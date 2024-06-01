namespace WebChemistry.MotiveValidator.Service
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using WebChemistry.MotiveValidator.DataModel;
    using System.IO;
    using WebChemistry.Platform.MoleculeDatabase;
    using ICSharpCode.SharpZipLib.Zip;
    using System.Threading.Tasks;
    using System.Text.RegularExpressions;
    using System.IO.Compression;
    using System.Text;

    /// <summary>
    /// ModelLoader helper class.
    /// </summary>
    class ModelLoader
    {
        Dictionary<string, Func<StructureReaderResult>> Cifs, Pdbs, Mols;
        ValidatorService validator;

        public static ModelLoader FromHostedSugars(ValidatorService validator, MotiveValidatorConfig config)
        {
            var cifs = DatabaseView.Load(config.SugarModelsView.Value).Snapshot().ToDictionary(s => s.FilenameId, s => new Func<StructureReaderResult>(() => s.ReadStructure()), StringComparer.OrdinalIgnoreCase);

            return new ModelLoader
            {
                Pdbs = new Dictionary<string, Func<StructureReaderResult>>(),
                Mols = new Dictionary<string, Func<StructureReaderResult>>(),
                Cifs = cifs,
                validator = validator
            };
        }
        
        public static ModelLoader FromFolder(ValidatorService validator, string folder)
        {
            var comp = StringComparison.OrdinalIgnoreCase;
            var files = Directory.GetFiles(folder);


            var pdbs = files.Where(f => f.EndsWith(".pdb", comp) || f.EndsWith(".pdb.gz", comp))
                .ToDictionary(f => StructureReader.GetStructureIdFromFilename(f), f => new Func<StructureReaderResult>(() => StructureReader.Read(f)), StringComparer.OrdinalIgnoreCase);
            var cifs = files.Where(f => f.EndsWith(".cif", comp) || f.EndsWith(".mmcif", comp) || f.EndsWith(".cif.gz", comp) || f.EndsWith(".mmcif.gz", comp))
                .ToDictionary(f => StructureReader.GetStructureIdFromFilename(f), f => new Func<StructureReaderResult>(() => StructureReader.Read(f)), StringComparer.OrdinalIgnoreCase);
            var mols = files.Where(f => 
                    f.EndsWith(".sdf", comp) || f.EndsWith(".sdf.gz", comp)
                    || f.EndsWith(".mol", comp) || f.EndsWith(".mol.gz", comp)
                    || f.EndsWith(".sd", comp) || f.EndsWith(".sd.gz", comp))
                .Distinct(f => StructureReader.GetStructureIdFromFilename(f), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(f => StructureReader.GetStructureIdFromFilename(f), f => new Func<StructureReaderResult>(() => StructureReader.Read(f)), StringComparer.OrdinalIgnoreCase);

            return new ModelLoader
            {
                Pdbs = pdbs,
                Mols = mols,
                Cifs = cifs,
                validator = validator
            };
        }

        public static ModelLoader FromZip(ValidatorService validator, string filename)
        {
            var comp = StringComparison.OrdinalIgnoreCase;

            var pdbs = new Dictionary<string, Func<StructureReaderResult>>(StringComparer.OrdinalIgnoreCase);
            var cifs = new Dictionary<string, Func<StructureReaderResult>>(StringComparer.OrdinalIgnoreCase);
            var mols = new Dictionary<string, Func<StructureReaderResult>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var zip = new ZipFile(filename))
                {
                    foreach (ZipEntry entry in zip)
                    {
                        try
                        {
                            if (entry.IsDirectory || !StructureReader.IsStructureFilename(entry.Name)) continue;

                            string id = StructureReader.GetStructureIdFromFilename(entry.Name);
                            var ms = new MemoryStream();
                            using (var stream = zip.GetInputStream(entry))
                            {
                                stream.CopyTo(ms);
                                ms.Flush();
                            }
                            ms.Position = 0;

                            Func<StructureReaderResult> provider;
                            {
                                var _ms = ms;
                                string _name = entry.Name;
                                provider = () => StructureReader.Read(_name, () => ms);
                            }

                            if (entry.Name.EndsWith(".pdb", comp) || entry.Name.EndsWith(".pdb.gz", comp))
                            {
                                pdbs[id] = provider;
                            }
                            if (entry.Name.EndsWith(".cif", comp) || entry.Name.EndsWith(".cif.gz", comp)
                                || entry.Name.EndsWith(".mmcif", comp) || entry.Name.EndsWith(".mmcif.gz", comp))
                            {
                                cifs[id] = provider;
                            }
                            else if (entry.Name.EndsWith(".mol", comp) || entry.Name.EndsWith(".mol.gz", comp)
                                || entry.Name.EndsWith(".sd", comp) || entry.Name.EndsWith(".sd.gz", comp)
                                || entry.Name.EndsWith(".sdf", comp) || entry.Name.EndsWith(".sdf.gz", comp))
                            {
                                mols[id] = provider;
                            }
                        }
                        catch (Exception e)
                        {
                            validator.LogError(entry.Name, e.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                validator.LogError(new FileInfo(filename).Name, "Reading motifs: {0}", ex.Message);
            }

            return new ModelLoader
            {
                Cifs = cifs,
                Pdbs = pdbs,
                Mols = mols,
                validator = validator
            };
        }

        static string ReadToNextDataOrEnd(StreamReader reader, StringBuilder src, bool checkObsolete, out bool isObsolete, ref int lineNumber)
        {
            int lineNum = 0;
            string line;
            bool checkedObsolete = false;

            isObsolete = false;

            while ((line = reader.ReadLine()) != null && !line.StartsWith("data_", StringComparison.Ordinal))
            {
                lineNum++;
                src.AppendLine(line);
                if (checkObsolete && !checkedObsolete)
                {
                    if (line.IndexOf("_chem_comp.pdbx_release_status", StringComparison.Ordinal) >= 0)
                    {
                        isObsolete = line.IndexOf("OBS", StringComparison.Ordinal) > 0;
                        checkedObsolete = true;
                    }
                }
            }
            
            lineNumber += lineNum;
            return line;
        }

        public static ModelLoader FromComponentDictionaryFile(string filename, bool ignoreObsolete, ValidatorService validator)
        {
            //var obsRegex = new Regex(@"_chem_comp\.pdbx_release_status\s+OBS", RegexOptions.Compiled | RegexOptions.Singleline);
            var isGz = filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
            var cifs = new Dictionary<string, Func<StructureReaderResult>>(StringComparer.OrdinalIgnoreCase);
            var fi = new FileInfo(filename);

            try
            {
                using (var file = File.OpenRead(filename))
                using (var reader = new StreamReader(isGz ? (Stream)new GZipStream(file, CompressionMode.Decompress) : file))
                {
                    int lineNumber = 0, ignoredCount = 0;
                    string line;
                    var src = new StringBuilder();

                    // skip to the first data entry;
                    while ((line = reader.ReadLine()) != null && !line.StartsWith("data_", StringComparison.Ordinal)) lineNumber++;
                    if (line == null) validator.Log("{0}: The file does not contain any models.", fi.Name);

                    while (line != null)
                    {
                        string id = line.Substring(5).Trim();

                        src.Clear();
                        bool obsolete;
                        line = ReadToNextDataOrEnd(reader, src, ignoreObsolete, out obsolete, ref lineNumber);

                        if (string.IsNullOrWhiteSpace(id))
                        {
                            validator.Log("{0}: Data entry at line {0} does not specify identifier, skipping entry.", fi.Name, lineNumber);
                        }
                        else if (cifs.ContainsKey(id))
                        {
                            validator.Log("{0}: Data entry at line {0} does not specifies duplicated id '{1}', skipping entry.", fi.Name, lineNumber, id);
                        }
                        else
                        {
                            if (ignoreObsolete && obsolete) ignoredCount++;
                            else
                            {
                                var source = src.ToString();
                                cifs[id] = () => StructureReader.ReadString(id + ".cif", source);
                            }                            
                        }
                    }

                    if (ignoreObsolete)
                    {
                        validator.Log("Ignored {0} obsolete entries.", ignoredCount);
                    }
                }
            }
            catch (Exception ex)
            {
                validator.LogError(fi.Name, "Reading motifs: {0}", ex.Message);
            }

            return new ModelLoader
            {
                Cifs = cifs,
                Pdbs = new Dictionary<string,Func<StructureReaderResult>>(),
                Mols = new Dictionary<string,Func<StructureReaderResult>>(),
                validator = validator
            };
        }

        MotiveModel MakeModel(string filenameId, StructureReaderResult sr, bool isCif = false)
        {
            try
            {
                var structure = sr.Structure;
                var formula = string.Concat(structure.Atoms
                    .GroupBy(a => a.ElementSymbol)
                    .OrderBy(g => g.Key.ToString())
                    .Select(g => string.Format("{0}{{{1}}}", g.Key, g.Count())));
                
                if (structure.PdbResidues().Count != 1)
                {
                    throw new ArgumentException("The model must contain exactly one residue.");
                }
                if (structure.Atoms.Count == 0)
                {
                    throw new ArgumentException("The model contains no atoms.");
                }
                if (structure.Atoms.Count > 999)
                {
                    throw new ArgumentException(string.Format("The model contains too many atoms ({0}, max. 999).", structure.Atoms.Count));
                }
                
                CoordinateSourceTypes sourceType = CoordinateSourceTypes.Default;
                if (isCif)
                {
                    if (structure.IsValidComponentModel())
                    {
                        structure = structure.GetComponentModelStructure();
                        sourceType = CoordinateSourceTypes.CifModel;
                    }
                    else if (structure.IsValidComponentIdeal())
                    {
                        structure = structure.GetComponentIdealStructure();
                        sourceType = CoordinateSourceTypes.CifIdeal;
                    }
                    else
                    {
                        var missingModel = structure.Atoms
                            .Where(a => !a.PdbCompAtomModelPosition().HasValue)
                            .JoinBy(a => a.PdbName());

                        var missingIdeal = structure.Atoms
                            .Where(a => !a.PdbCompAtomIdealPosition().HasValue)
                            .JoinBy(a => a.PdbName());

                        throw new InvalidOperationException(string.Format("The model structure is missing coordinates for some atoms (Experimental: {0}; Model: {1}).",
                            missingModel, missingIdeal));
                    }
                }
                else
                {
                    if (Mols.ContainsKey(filenameId))
                    {
                        var mol = Mols[filenameId]().Structure;
                        if (mol.Atoms.Count == structure.Atoms.Count)
                        {
                            structure = CopyBondsFromMol(structure, mol);
                        }
                        else
                        {
                            throw new InvalidOperationException("The PDB and MOL structures do not contains the same number of atoms.");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("If supplying a model in the PDB format, SDF/MOL file with the same name has to be present in order to correctly determine bonds.");
                    }
                }

                var name = structure.Atoms[0].PdbResidueName();
                var chiralAtoms = structure.ChiralAtoms();
                var filtered = ValidatorService.FilterStructureAtoms(structure);
                chiralAtoms = chiralAtoms.Where(a => filtered.Atoms.Contains(a)).Select(a => filtered.Atoms.GetById(a.Id)).ToArray();

                filtered.ToCentroidCoordinates();

                //!!!! ValidatorService.FilterStructureAtoms(Mols[filenameId]().Structure);

                var model = new MotiveModel(name, formula, filtered, chiralAtoms, sourceType);

                validator.SetLongName(model);

                if (sr.Warnings.Count > 0)
                {
                    model.AddWarnings(name + "_model", sr.Warnings);
                }
                //if (!string.IsNullOrEmpty(molWarning))
                //{
                //    model.AddModelWarnings(string.Format("Error reading MOL bonds: {0}", molWarning));
                //}
                
                return model;
            }
            catch (Exception e)
            {
                validator.LogError(new FileInfo(sr.Filename).Name, e.Message);
                return null;
            }
        }
        
        void ProcessModel(string filenameId, Func<StructureReaderResult> provider, bool isCif, Dictionary<string, MotiveModel> models)
        {
            try
            {
                var m = MakeModel(filenameId, provider(), isCif);
                if (m != null)
                {
                    lock (models)
                    {
                        if (!models.ContainsKey(m.Name)) models.Add(m.Name, m);
                        else
                        {
                            var oldModel = models[m.Name];

                            if (StringComparer.OrdinalIgnoreCase.Compare(m.Structure.Id, oldModel.Structure) < 0)
                            {
                                models[m.Name] = m;
                                validator.LogError(oldModel.Structure.Id, "A model for residue '{0}' is already loaded.", m.Name);
                            }
                            else
                            {
                                validator.LogError(filenameId, "A model for residue '{0}' is already loaded.", m.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                validator.LogError(filenameId, e.Message);
            }
        }

        public Dictionary<string, MotiveModel> ReadModels()
        {
            Dictionary<string, MotiveModel> ret = new Dictionary<string, MotiveModel>(StringComparer.OrdinalIgnoreCase);

            var src = Cifs.Select(p => new { IsCif = true, P = p }).Concat(Pdbs.Select(p => new { IsCif = false, P = p })).ToList();
            Parallel.ForEach(src, new ParallelOptions { MaxDegreeOfParallelism = validator.MaxDegreeOfParallelism }, e =>
            {
                ProcessModel(e.P.Key, e.P.Value, e.IsCif, ret);
            });

            return ret;
        }

        static IStructure CopyBondsFromMol(IStructure pdb, IStructure mol)
        {
            var molIndices = mol.Atoms.Select((a, i) => new { A = a, I = i }).ToDictionary(a => a.A, a => a.I);
            var newBonds = BondCollection.Create(mol.Bonds.Select(b => Bond.Create(pdb.Atoms[molIndices[b.A]], pdb.Atoms[molIndices[b.B]], b.Type)));
            return Structure.Create(pdb.Id, pdb.Atoms, newBonds).AsPdbStructure(null);
        }
    }

}
