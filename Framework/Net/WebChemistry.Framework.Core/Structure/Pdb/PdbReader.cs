namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using WebChemistry.Framework.Math;

    internal partial class PdbReader
    {
        class SecondaryElementInfo
        {
            public SecondaryStructureType Type;
            public PdbResidueIdentifier Start, End;
        }

        class ModifiedResidueInfo
        {
            public PdbResidueIdentifier Id;
            public string ModifiedFrom;
        }

        readonly string Filename;
        readonly PdbReaderParams Parameters;
        readonly TextReader Reader;

        //int CurrentModelIndex = 1;

        List<PdbAtom> CompAtoms;
        List<PdbAtom> Atoms;
        HashSet<int> UsedAtomIds;
        Dictionary<Vector3D, IAtom> OccupiedPositions;

        List<string> ConectRecords;
        List<SecondaryElementInfo> SecondaryElements;

        List<ModifiedResidueInfo> ModifiedResidues;
        
        int HugeId;
        bool IsHuge;        
        int CurrentLine;

        List<StructureReaderWarning> Warnings;
        HashSet<PdbResidueIdentifier> ExtraResidueIds;
        
        PdbMetadata Metadata;

        public StructureReaderResult Read()
        {
            this.Warnings = new List<StructureReaderWarning>();
            this.ExtraResidueIds = new HashSet<PdbResidueIdentifier>();

            this.HugeId = 100000;
            this.IsHuge = false;

            this.CurrentLine = 0;
            this.CompAtoms = new List<PdbAtom>();
            this.Atoms = new List<PdbAtom>();
            this.UsedAtomIds = new HashSet<int>();
            this.OccupiedPositions = new Dictionary<Vector3D, IAtom>();

            this.ConectRecords = new List<string>();
            this.SecondaryElements = new List<SecondaryElementInfo>();

            this.ModifiedResidues = new List<ModifiedResidueInfo>();

            this.Metadata = new PdbMetadata();

            if (Parameters.Type == PdbReaderParams.StructureType.PdbX) return ReadPdbX();
            return ReadPdbOrPqr();
        }

        StructureReaderResult ReadPdbOrPqr()
        {
            bool isNMR = false;                                    
            var keywords = new List<string>();            
            int modelCount = -1;
            bool skipNonConnect = false;

            try
            {
                bool warnedAboutMultipleModels = false;
                string line;
                while ((line = Reader.ReadLine()) != null)
                {
                    CurrentLine++;
                    if (line.Length == 0) continue;
                    if (skipNonConnect)
                    {
                        if (!warnedAboutMultipleModels && line.StartsWith("MODEL", StringComparison.OrdinalIgnoreCase))
                        {
                            warnedAboutMultipleModels = true;
                            Warnings.Add(new OnlyFirstModelLoadedReaderWarning(CurrentLine));
                        }

                        if (line.StartsWith("CONECT", StringComparison.Ordinal))
                        {
                            ConectRecords.Add(line);
                        }

                        continue;
                    }

                    switch (line[0])
                    {
                        case 'A':
                            if (line.StartsWith("ATOM", StringComparison.Ordinal))
                            {
                                if (Parameters.Type == PdbReaderParams.StructureType.Pqr) ParsePqrAtom(line, Math.Max(modelCount, 0));
                                else ParseAtom(line, Math.Max(modelCount, 0));
                            }
                            break;

                        case 'C':
                            if (line.StartsWith("CONECT", StringComparison.Ordinal))
                            {
                                ConectRecords.Add(line);
                            }
                            break;

                        case 'E':
                            if (line.StartsWith("ENDMDL", StringComparison.Ordinal))
                            {
                                if (!IsHuge && (isNMR || Parameters.LoadFirstModelOnly))
                                {
                                    skipNonConnect = true;
                                }

                                ////if (!IsHugeStructure)
                                ////{
                                ////    models.Add(CurrentAtoms);
                                ////    this.CurrentAtoms = new List<PdbAtom>(CurrentAtoms.Count);
                                ////    this.UsedAtomIds.Clear();
                                ////    CurrentModelIndex++;
                                ////}
                            }
                            else if (line.StartsWith("END", StringComparison.Ordinal))
                            {
                                if (line.Trim().Length == 3) break;
                            }
                            else if (line.StartsWith("EXPDTA", StringComparison.Ordinal) && line.IndexOf("nmr", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                isNMR = true;
                            }

                            break;

                        case 'H':
                            if (line.StartsWith("HETATM", StringComparison.Ordinal))
                            {
                                if (Parameters.Type == PdbReaderParams.StructureType.Pqr) ParsePqrAtom(line, Math.Max(modelCount, 0));
                                else ParseAtom(line, Math.Max(modelCount, 0));
                            }
                            else if (line.StartsWith("HELIX", StringComparison.Ordinal))
                            {
                                var h = line;
                                string chain = h[19].ToString();
                                int number = int.Parse(h.Substring(21, 4));
                                char insertionCode = h[25];
                                var start = PdbResidueIdentifier.Create(number, chain, insertionCode);

                                chain = h[31].ToString();
                                number = int.Parse(h.Substring(33, 4));
                                insertionCode = h[37];
                                var end = PdbResidueIdentifier.Create(number, chain, insertionCode);

                                SecondaryElements.Add(new SecondaryElementInfo { Type = SecondaryStructureType.Helix, Start = start, End = end });
                            }
                            else if (line.StartsWith("HEADER", StringComparison.Ordinal))
                            {
                                var match = new Regex("(?<day>[0-9]+)-(?<month>...)-(?<year>[0-9]+)").Match(line);
                                if (match.Success)
                                {
                                    var day = match.Groups["day"].Value.ToInt();
                                    var year = match.Groups["year"].Value.ToInt();
                                    var months = new[] { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
                                    var month = Maybe.Just(Array.IndexOf(months, match.Groups["month"].Value.ToLowerInvariant()));

                                    try
                                    {
                                        var date = from m in month
                                                   where m >= 0
                                                   from d in day
                                                   from y in year
                                                   select new DateTime(y > 50 ? 1900 + y : 2000 + y, m + 1, d);
                                        if (date.IsSomething()) Metadata.Released = date.GetValue();
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                            break;

                        ////case 'L':
                        ////    if (line.StartsWith("LINKS", StringComparison.Ordinal))
                        ////    { // nestandardni vazby
                        ////        links.Add(line);
                        ////    }
                        ////    break;

                        case 'M':
                            if (line.StartsWith("MODRES", StringComparison.Ordinal))
                            {
                                HandleModRes(line);
                            }
                            else if (line.StartsWith("MODEL", StringComparison.Ordinal))
                            {
                               modelCount++;
                            }
                            break;
                        case 'R':
                        // REMARK   2 RESOLUTION.    1.55 ANGSTROMS.    
                            if (line.StartsWith("REMARK   2 RESOLUTION.", StringComparison.Ordinal))
                            {
                                var match = new Regex(@"[0-9]+\.[0-9]+").Match(line);
                                if (match.Success)
                                {
                                    Metadata.Resolution = double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture);
                                }
                            }
                            break;

                        case 'S':
                            if (line.StartsWith("SHEET", StringComparison.Ordinal))
                            {
                                var s = line;
                                string chain = s[21].ToString();
                                int number = int.Parse(s.Substring(22, 4));
                                char insertionCode = s[26];
                                var start = PdbResidueIdentifier.Create(number, chain, insertionCode);

                                chain = s[32].ToString();
                                number = int.Parse(s.Substring(33, 4));
                                insertionCode = s[37];
                                var end = PdbResidueIdentifier.Create(number, chain, insertionCode);

                                SecondaryElements.Add(new SecondaryElementInfo { Type = SecondaryStructureType.Sheet, Start = start, End = end });
                            }
                            ////if (line.StartsWith("SSBOND", StringComparison.Ordinal)) ssbonds.Add(line); // disulfidicky mustek

                            break;

                        case 'K':
                            if (line.StartsWith("KEYWDS", StringComparison.Ordinal))
                            {
                                line.Substring(10)
                                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .ToList()
                                    .ForEach(a =>
                                    {
                                        var kw = a.Trim();
                                        if (!string.IsNullOrEmpty(kw)) keywords.Add(kw);
                                    });
                            }
                            break;

                        default:
                            break;

                    }
                }
                
                if (Atoms.Count == 0)
                {
                    throw new IOException("The file contains no atoms.");
                }
            }
            catch (Exception e)
            {
                throw new IOException(string.Format("Invalid PDB format. First error at line {0}: {1}", CurrentLine, e.Message));
            }

            Metadata.Keywords = keywords.ToArray();

            return CreateStructure(FilterAtoms(Atoms));
        }

        void HandleModRes(string line)
        {
            line = line.PadRight(28);

            ModifiedResidues.Add(new ModifiedResidueInfo
            {
                Id = PdbResidueIdentifier.Create(NumberParser.ParseIntFast(line, 18, 4), line[16].ToString().Trim(), line[22]),
                ModifiedFrom = line.Substring(24, 3).Trim()
            });
        }

        static string GuessElementSymbol(string name, bool isHetAtom, char fallback)
        {
            if (isHetAtom)
            {
                if (name.StartsWith("H", StringComparison.OrdinalIgnoreCase)) return "H";
                return name;
            }

            if (name.StartsWith("H", StringComparison.OrdinalIgnoreCase)) return "H";
            return fallback.ToString();
        }

        bool HandleDuplicates(IAtom atom)
        {

            // Skip the duplicate ids.
            if (!UsedAtomIds.Add(atom.Id))
            {
                Warnings.Add(new AtomStructureReaderWarning(atom, "Duplicate id, atom ignored.", CurrentLine, AtomStructureReaderWarningType.DuplicateId));
                return false;
            }

            // skip the duplicate atom positions.
            if (OccupiedPositions.ContainsKey(atom.Position))
            {
                var other = OccupiedPositions[atom.Position];

                if (other.ResidueIdentifier() == atom.ResidueIdentifier())
                {
                    if (other.PdbAlternateLocationIdentifier() != atom.PdbAlternateLocationIdentifier())
                    {
                        return true;
                    }

                    if (other.PdbName() != atom.PdbName())
                    {
                        string message = string.Format("Position [{0}] already occupied by atom '{1}'.", atom.Position, other.Id);
                        Warnings.Add(new AtomStructureReaderWarning(atom, message, CurrentLine, AtomStructureReaderWarningType.PositionOccupied));
                        return false;
                    }
                }
                else
                {
                    IAtom max =
                        other.PdbChainIdentifier().CompareTo(atom.PdbChainIdentifier()) < 0
                        || (other.PdbChainIdentifier().EqualOrdinal(atom.PdbChainIdentifier())
                        && other.PdbResidueSequenceNumber() < atom.PdbResidueSequenceNumber())
                        ? atom : other;
                    string message = string.Format("'{0}' and '{1}' are too close to each other (0.000 ang). '{2}' ignored.",
                        atom.ResidueString(), other.ResidueString(), max.ResidueString());
                    if (ExtraResidueIds.Add(max.ResidueIdentifier()))
                    {
                        Warnings.Add(new ResidueStructureReaderWarning(max.PdbResidueName(), max.ResidueIdentifier(), message, CurrentLine, ResidueStructureReaderWarningType.ResiduesTooClose));
                    }
                    return false;
                }
            }
            else
            {
                OccupiedPositions.Add(atom.Position, atom);
            }

            return true;
        }

        int GetId(int serialNumber, int modelIndex)
        {
            if (serialNumber == 10000 && Atoms.Count >= 95000)
            {
                IsHuge = true;
            }
            if (IsHuge)
            {
                HugeId++;
                return HugeId;
            }
            var idOffset = modelIndex * 100000;
            return serialNumber + idOffset;
        }

        void ParseAtom(string line, int modelIndex)
        {
            line = line.PadRight(80, ' ');

            int serialNumber = NumberParser.ParseIntFast(line, 6, 5);

            int id = GetId(serialNumber, modelIndex);

            double x, y, z;

            x = NumberParser.ParseDoubleFast(line, 30, 8);
            y = NumberParser.ParseDoubleFast(line, 38, 8);
            z = NumberParser.ParseDoubleFast(line, 46, 8);

            var position = new Vector3D(x, y, z);
                        
            string residueName = line.Substring(17, 3).Trim();
            string recordName = line.StartsWith("HETATM", StringComparison.Ordinal) ? "HETATM" : "ATOM";  //line.StartsWith("HETATM", StringComparison.Ordinal) ? (Modres.Contains(residueName) ? "ATOM" : "HETATM") : "ATOM";            
            string name = line.Substring(12, 4).Trim();
            char alternateLocationIdent = line[16];            
            string chainIdentifier = line[21].ToString();
            int residueSequenceNumber = NumberParser.ParseIntFast(line, 22, 4);
            char insertionResidueCode = line[26];
                        
            double occupancy = NumberParser.ParseDoubleFast(line, 54, 6);
            double temperatureFactor = NumberParser.ParseDoubleFast(line, 60, 6);

            string segmentIdentifier = line.Substring(72, 4).Trim();
            string elementSymbol = line.Substring(76, 2).Trim();

            if (string.IsNullOrEmpty(elementSymbol))
            {
                elementSymbol = GuessElementSymbol(name, recordName.Equals("HETATM", StringComparison.Ordinal), line[13]);
            }

            string charge = line.Substring(78, 2).Trim();

            var atom = (PdbAtom)PdbAtom.Create(
                id: id,
                elementSymbol: ElementSymbol.Create(elementSymbol),
                residueName: residueName,
                serialNumber: serialNumber,
                residueSequenceNumber: residueSequenceNumber + modelIndex * 10000,
                chainIdentifier: chainIdentifier,
                alternateLocationIdentifier: alternateLocationIdent,
                name: name,
                recordName: recordName,
                insertionResidueCode: insertionResidueCode,
                occupancy: occupancy,
                temperatureFactor: temperatureFactor,
                segmentIdentifier: segmentIdentifier,
                charge: charge,
                position: position);

            if (HandleDuplicates(atom)) Atoms.Add(atom);
        }

        static char[] spaceSplit = new char[] { ' ' };
        private void ParsePqrAtom(string line, int modelIndex)
        {
            ////var fields = line.Split(spaceSplit, StringSplitOptions.RemoveEmptyEntries);

            ////if (fields.Length != 10 && fields.Length != 11) throw new IOException("The input is not in a valid PQR file format.");

            ////bool containsChain = fields.Length == 11;

            ////// Field_name
            ////int fieldIndex = 0;
            ////string recordName = fields[fieldIndex].ToUpper();

            ////// Atom_number
            ////fieldIndex++;
            ////int serialNumber = NumberParser.ParseIntFast(fields[fieldIndex], 0, fields[fieldIndex].Length);

            ////int id = GetId(serialNumber, modelIndex);
            
            ////// Atom_name
            ////fieldIndex++;
            ////string name = fields[fieldIndex];

            ////// Residue_name
            ////fieldIndex++;
            ////string residueName = fields[fieldIndex];

            ////string chainIdentifier = "";

            ////if (containsChain)
            ////{
            ////    // Chain_ID
            ////    fieldIndex++;
            ////    chainIdentifier = fields[fieldIndex];
            ////}

            ////// Residue_number
            ////fieldIndex++;
            ////int residueSequenceNumber = NumberParser.ParseIntFast(fields[fieldIndex], 0, fields[fieldIndex].Length);

            ////double x, y, z;

            ////fieldIndex++;
            ////x = NumberParser.ParseDoubleFast(fields[fieldIndex], 0, fields[fieldIndex].Length);
            ////fieldIndex++;
            ////y = NumberParser.ParseDoubleFast(fields[fieldIndex], 0, fields[fieldIndex].Length);
            ////fieldIndex++;
            ////z = NumberParser.ParseDoubleFast(fields[fieldIndex], 0, fields[fieldIndex].Length);

            ////var position = new Vector3D(x, y, z);
                        
            ////fieldIndex++;
            ////double charge = NumberParser.ParseDoubleFast(fields[fieldIndex], 0, fields[fieldIndex].Length); // as occupancy

            ////fieldIndex++;
            ////double radius = NumberParser.ParseDoubleFast(fields[fieldIndex], 0, fields[fieldIndex].Length); // as tempFactor
            
            ////string elementSymbol = GuessElementSymbol(name, recordName.Equals("HETATM", StringComparison.Ordinal), name[0]);
            
            ////var atom = (PdbAtom)PdbAtom.Create(
            ////    id: id,
            ////    elementSymbol: ElementSymbol.Create(elementSymbol),
            ////    residueName: residueName,
            ////    serialNumber: serialNumber,
            ////    residueSequenceNumber: residueSequenceNumber + modelIndex * 10000,
            ////    chainIdentifier: chainIdentifier,
            ////    name: name,
            ////    recordName: recordName,
            ////    occupancy: charge,
            ////    temperatureFactor: radius,
            ////    position: position);

            ////if (HandleDuplicates(atom)) Atoms.Add(atom);

            line = line.PadRight(80, ' ');

            int serialNumber = NumberParser.ParseIntFast(line, 6, 5);

            int id = GetId(serialNumber, modelIndex);

            double x, y, z;

            x = NumberParser.ParseDoubleFast(line, 30, 8);
            y = NumberParser.ParseDoubleFast(line, 38, 8);
            z = NumberParser.ParseDoubleFast(line, 46, 8);

            var position = new Vector3D(x, y, z);

            string residueName = line.Substring(17, 3).Trim();
            string recordName = line.StartsWith("HETATM", StringComparison.Ordinal) ? "HETATM" : "ATOM"; // line.StartsWith("HETATM", StringComparison.Ordinal) ? (Modres.Contains(residueName) ? "ATOM" : "HETATM") : "ATOM";
            string name = line.Substring(12, 4).Trim();
            char alternateLocationIdent = line[16];
            string chainIdentifier = line[21].ToString();
            int residueSequenceNumber = NumberParser.ParseIntFast(line, 22, 4);
            char insertionResidueCode = line[26];

            var fields = line.Substring(55).Split(spaceSplit, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length < 2) throw new InvalidOperationException("Invalid PQR record. The fields must be aligned the same way as in PDB format, with charge, occupancy, and optionally element symbol, being the last 3 columns and separated by at least a single space. For example: 'ATOM      1  N   MET A   1      39.914   3.935  -2.319  0.15920000    1.550 N'.");
            
            double charge = NumberParser.ParseDoubleFast(fields[0], 0, fields[0].Length);
            double radius = NumberParser.ParseDoubleFast(fields[1], 0, fields[1].Length);
            string elementSymbol = null;
            if (fields.Length >= 3) elementSymbol = fields[2];

            if (string.IsNullOrEmpty(elementSymbol))
            {
                elementSymbol = GuessElementSymbol(name, recordName.Equals("HETATM", StringComparison.Ordinal), line[13]);
            }
            
            var atom = (PdbAtom)PdbAtom.Create(
                id: id,
                elementSymbol: ElementSymbol.Create(elementSymbol),
                residueName: residueName,
                serialNumber: serialNumber,
                residueSequenceNumber: residueSequenceNumber + modelIndex * 10000,
                chainIdentifier: chainIdentifier,
                alternateLocationIdentifier: alternateLocationIdent,
                name: name,
                recordName: recordName,
                insertionResidueCode: insertionResidueCode,
                occupancy: charge,
                temperatureFactor: radius,
                position: position);

            if (HandleDuplicates(atom)) Atoms.Add(atom);
        }

        List<IBond> GetKnownBonds(IAtomCollection atoms, List<string> connect)
        {
            var added = new HashSet<BondIdentifier>();
            List<IBond> ret = new List<IBond>();
            var ids = new List<int>(5);

            Action<string, int> process = (l, offset) =>
                {
                    if (l.Length > offset)
                    {
                        var t = l.Substring(offset - 1, 5);
                        if (!string.IsNullOrWhiteSpace(t)) ids.Add(int.Parse(t));
                    }
                };

            foreach (var c in connect)
            {
                IAtom pivot;
                if (!atoms.TryGetAtom(NumberParser.ParseIntFast(c, 6, 5), out pivot)) continue;
                
                ids.Clear();
                process(c, 12);
                process(c, 17);
                process(c, 22);
                process(c, 27);

                for (int i = 0; i < ids.Count; i++)
                {
                    IAtom other;
                    if (!atoms.TryGetAtom(ids[i], out other) || pivot == other) continue;

                    IBond bond;
                    if (ElementAndBondInfo.IsMetalSymbol(pivot.ElementSymbol)
                        || ElementAndBondInfo.IsMetalSymbol(other.ElementSymbol))
                    {
                        bond = Bond.Create(pivot, other, BondType.Metallic);
                    }
                    else
                    {
                        bond = Bond.Create(pivot, other, BondType.Single);
                    }

                    if (added.Add(bond.Id)) ret.Add(bond);
                }
            }
            return ret;
        }

        List<PdbAtom> FilterAtoms(List<PdbAtom> atoms)
        {
            return atoms.Where(a => !ExtraResidueIds.Contains(a.ResidueIdentifier())).ToList();
        }
                
        /// <summary>
        /// Creates a structure from the provided atoms.
        /// </summary>
        /// <param name="atoms"></param>
        /// <param name="removeCloseResidues"></param>
        /// <returns></returns>
        StructureReaderResult CreateStructure(
            List<PdbAtom> atoms,
            bool removeCloseResidues = true)
        {
            var altLocWarnings = new List<StructureReaderWarning>();
            var multipleNameWarnings = new List<StructureReaderWarning>();

            List<PdbChain> chains = new List<PdbChain>();
            //Dictionary<string, PdbAtom> atomsByName = new Dictionary<string, PdbAtom>(StringComparer.Ordinal);
            //Dictionary<Vector3D, PdbAtom> hydrogensByPosition = new Dictionary<Vector3D, PdbAtom>(20);            
            List<PdbAtom> atomList = new List<PdbAtom>();
            HashSet<int> addedAtoms = new HashSet<int>();
            HashSet<string> usedResidueNames = new HashSet<string>(StringComparer.Ordinal);
            List<string> skippedAlternateLocations = new List<string>();
            foreach (var chain in atoms.GroupBy(a => a.ChainIdentifier))
            {
                List<PdbResidue> residues = new List<PdbResidue>();
                foreach (var residue in chain.GroupBy(a => a.ResidueSequenceNumber.ToString() + a.InsertionResidueCode))
                {                    
                    //atomsByName.Clear();
                    //hydrogensByPosition.Clear();
                    atomList.Clear();
                    char firstNonBlank = ' ';
                    bool ignored = false;
                    foreach (var alternateLocationGroup in residue.GroupBy(a => a.AlternateLocaltionIdentifier).OrderBy(g => g.Key == ' ' ? -1 : g.Key))
                    {
                        var key = alternateLocationGroup.Key;
                        if (ignored)
                        {
                            skippedAlternateLocations.Add(key.ToString());
                            continue;
                        }
                        if (key != firstNonBlank && firstNonBlank != ' ')
                        {
                            skippedAlternateLocations.Clear();
                            skippedAlternateLocations.Add(key.ToString());
                            ignored = true;
                            break;
                        }
                        firstNonBlank = key;

                        foreach (var atom in alternateLocationGroup)
                        {
                            atomList.Add(atom);
                            addedAtoms.Add(atom.Id);
                        }

                        ////foreach (var atom in alternateLocationGroup)
                        ////{
                        ////    if (atom.ElementSymbol == ElementSymbols.H)
                        ////    {
                        ////        PdbAtom duplicate;
                        ////        if (hydrogensByPosition.TryGetValue(atom.InvariantPosition, out duplicate))
                        ////        {
                        ////            if (atom.Occupancy > duplicate.Occupancy)
                        ////            {
                        ////                addedAtoms.Remove(duplicate.Id);
                        ////                addedAtoms.Add(atom.Id);
                        ////                hydrogensByPosition[atom.InvariantPosition] = atom;
                        ////            }
                        ////        }
                        ////        else
                        ////        {
                        ////            addedAtoms.Add(atom.Id);
                        ////            hydrogensByPosition.Add(atom.InvariantPosition, atom);
                        ////        }
                        ////    }
                        ////    else
                        ////    {
                        ////        PdbAtom duplicate;

                        ////        // well this does not work on hydrogens all the time (guess the ones computed by Babel).
                        ////        if (atomsByName.TryGetValue(atom.Name, out duplicate))
                        ////        {
                        ////            if (atom.Occupancy == 1.0 && atom.InvariantPosition != duplicate.InvariantPosition)
                        ////            {
                        ////                addedAtoms.Add(atom.Id);
                        ////            }
                        ////            else if (atom.Occupancy > duplicate.Occupancy)
                        ////            {
                        ////                addedAtoms.Remove(duplicate.Id);
                        ////                addedAtoms.Add(atom.Id);
                        ////                atomsByName[atom.Name] = atom;
                        ////            }
                        ////        }
                        ////        else
                        ////        {
                        ////            atomsByName.Add(atom.Name, atom);
                        ////            addedAtoms.Add(atom.Id);
                        ////        }
                        ////    }
                        ////}
                    }

                    //var atomList = hydrogensByPosition.Count > 0 
                    //    ? atomsByName.Values.Concat(hydrogensByPosition.Values).OrderBy(a => a.Id).ToList()
                    //    : atomsByName.Values.OrderBy(a => a.Id).ToList();
                    
                    var newResidue = PdbResidue.Create(atomList);
                    residues.Add(newResidue);

                    // validate that all atoms on the residues have the same name.
                    usedResidueNames.Clear();
                    for (int i = 0; i < atomList.Count; i++)
                    {
                        usedResidueNames.Add(atomList[i].ResidueName);
                    }

                    if (usedResidueNames.Count > 1)
                    {
                        multipleNameWarnings.Add(new ResidueStructureReaderWarning(newResidue.Name, newResidue.Identifier,
                            string.Format("Atoms in the residue contain multiple names ({0}). Check chain ID.", usedResidueNames.OrderBy(n => n, StringComparer.Ordinal).JoinBy()),
                            type: ResidueStructureReaderWarningType.MultipleNames));
                    }

                    if (ignored)
                    {
                        altLocWarnings.Add(new ResidueStructureReaderWarning(
                            newResidue.Name, 
                            newResidue.Identifier,
                            string.Format("Skipped alternate location(s) '{0}'.", string.Join(", ", skippedAlternateLocations)),
                            type: ResidueStructureReaderWarningType.IgnoredAlternateLocation));
                    }
                }
                chains.Add(new PdbChain(chain.Key, residues.OrderBy(r => r.Number).ThenBy(r => r.InsertionResidueCode)));
            }

            var orderedChains = new ReadOnlyCollection<PdbChain>(chains.OrderBy(c => c.Identifier).ToArray());
            var orderedResidues = PdbResidueCollection.Create(orderedChains.SelectMany(c => c.Residues));
            //var orderedAtoms = AtomCollection.Create(orderedResidues.SelectMany(r => r.Atoms));
            var orderedAtoms = AtomCollection.Create(atoms.Where(a => addedAtoms.Contains(a.Id)));

            var structure = Structure.Create(Parameters.Id, orderedAtoms);
            var known = GetKnownBonds(orderedAtoms, ConectRecords.ToList());
            var bondComputationResult = ElementAndBondInfo.ComputePdbBonds(structure, known);
            if (removeCloseResidues)
            {
                if (bondComputationResult.CloseResidueIdentifiers.Count > 0)
                {
                    ExtraResidueIds.UnionWith(bondComputationResult.CloseResidueIdentifiers);
                    Warnings.AddRange(bondComputationResult.CloseResidueWarnings);
                    return CreateStructure(FilterAtoms(atoms), false);
                }
                else
                {
                    Warnings.AddRange(multipleNameWarnings);
                    Warnings.AddRange(bondComputationResult.ConectWarnings);
                    Warnings.AddRange(altLocWarnings);
                }
            }
            else
            {
                Warnings.AddRange(multipleNameWarnings);
                Warnings.AddRange(bondComputationResult.ConectWarnings);
                Warnings.AddRange(altLocWarnings);
            }

            structure.SetProperty(PdbStructure.ResiduesProperty, orderedResidues);
            structure.SetProperty(PdbStructure.IsPdbStructureProperty, true);
            if (Parameters.Type == PdbReaderParams.StructureType.Pqr)
            {
                structure.SetProperty(PdbStructure.PqrContainsChargesProperty, true);
            }

            structure.SetProperty(PdbStructure.MetadataProperty, Metadata);            

            structure.SetProperty(PdbStructure.ChainsProperty, orderedChains.ToDictionary(c => c.Identifier));

            CreateSecondaryElements(structure, orderedResidues);

            foreach (var mr in ModifiedResidues)
            {
                var r = orderedResidues.FromIdentifier(mr.Id);
                if (r != null)
                {
                    r.ModifiedFrom = mr.ModifiedFrom;
                }
            }

            return StructureReaderResult.Success(Filename, structure, Warnings);
        }

        /// <summary>
        /// Creates secondary elements from the SecondaryElements list.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="residues"></param>
        void CreateSecondaryElements(IStructure structure, PdbResidueCollection residues)
        {
            if (SecondaryElements.Count == 0) return;

            var helicesList = new List<PdbHelix>();
            var sheetsList = new List<PdbSheet>();

            var orderedElements = SecondaryElements.OrderBy(e => e.Start).ToArray();
            int currentElementIndex = 0;
            var currentElement = orderedElements[currentElementIndex];

            int count = residues.Count;
            for (int i = 0; i < residues.Count; )
            {
                var currentResidue = residues[i];
                // Check if we are inside the current secondary element.
                if (currentResidue.Identifier.CompareTo(currentElement.Start) >= 0)
                {
                    // Accumulate residues until the end of the currentElement.
                    var currentList = new List<PdbResidue>(Math.Max(6, currentElement.End.Number - currentElement.Start.Number + 1));
                    do
                    {
                        currentList.Add(currentResidue);
                        i++;
                        if (i >= count) break;
                        currentResidue = residues[i];
                    } while (currentResidue.Identifier.CompareTo(currentElement.End) <= 0);

                    // Create the element if it's not empty.
                    if (currentList.Count > 0)
                    {
                        if (currentElement.Type == SecondaryStructureType.Helix) helicesList.Add(PdbHelix.FromOrderedResidues(currentList));
                        else sheetsList.Add(PdbSheet.FromOrderedResidues(currentList));
                    }

                    // Move to the next element.
                    if (currentElementIndex + 1 < orderedElements.Length)
                    {
                        currentElementIndex++;
                        currentElement = orderedElements[currentElementIndex];
                    }
                    else
                    {
                        break;
                    }
                }
                // check if we are "ahead" (in case of some degenerate data)
                else if (currentResidue.Identifier.CompareTo(currentElement.End) > 0)
                {
                    if (currentElementIndex + 1 < orderedElements.Length)
                    {
                        currentElementIndex++;
                        currentElement = orderedElements[currentElementIndex];
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    // move to the next residue.
                    i++;
                }
            }

            structure.SetProperty(PdbStructure.HelicesProperty, new ReadOnlyCollection<PdbHelix>(helicesList));
            structure.SetProperty(PdbStructure.SheetsProperty, new ReadOnlyCollection<PdbSheet>(sheetsList));
        }

        public PdbReader(string filename, TextReader reader, PdbReaderParams parameters)
        {
            this.Filename = filename;
            this.Reader = reader;
            this.Parameters = parameters;
        }
    }
}
