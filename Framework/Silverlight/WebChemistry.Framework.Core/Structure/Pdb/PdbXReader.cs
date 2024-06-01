namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Math;
    
    internal partial class PdbReader
    {
        #region Core
        Dictionary<string, RecordType> RecordTypes;
        Dictionary<RecordType, RecordInfo> RecordsInfo;
        bool IsComp;
        bool HasAtomSite;
        Dictionary<string, int> LoopRecordMap;
        FieldElementInfo[] LoopFields;
        int LoopFieldCount;
        string CurrentLineText;
        List<CompBondElement> CompBonds;

        /// <summary>
        /// Reads the next line in the input stream and stores the value to the CurrentLineText variable.
        /// Increments the CurrentLine counter.
        /// </summary>
        /// <returns>True if the stream did not end, false otherwise.</returns>
        bool NextLine()
        {
            CurrentLine++;
            CurrentLineText = Reader.ReadLine();
            return CurrentLineText != null;
        }

        /// <summary>
        /// Checks if the current line starts with a given value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool StartsWith(char value)
        {
            return CurrentLineText.Length > 0 && CurrentLineText[0] == value;
        }

        /// <summary>
        /// Checks if the current line starts with a given value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool StartsWith(string value)
        {
            return CurrentLineText.StartsWith(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if the line starts with a given value, ignoring case.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool StartWithIgnoreCase(string value)
        {
            return CurrentLineText.StartsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Represents a record info.
        /// </summary>
        class RecordTypeInfo
        {
            public string Key;
            public RecordType Type;
        }

        /// <summary>
        /// Used to determine the record type from the first line of a section (either loop_ or a single record).
        /// 
        /// "Unknown" sections are skipped.
        /// </summary>
        /// <returns></returns>
        RecordTypeInfo GetRecordType()
        {
            var dot = CurrentLineText.IndexOf('.');
            if (dot < 0) return new RecordTypeInfo { Key = CurrentLineText, Type = RecordType.Unknown };
            RecordType type;
            var key = CurrentLineText.Substring(0, dot);
            if (RecordTypes.TryGetValue(key, out type)) return new RecordTypeInfo { Key = key, Type = type };
            return new RecordTypeInfo { Key = key, Type = RecordType.Unknown };
        }

        /// <summary>
        /// Reads the fields contained in a loop.
        /// </summary>
        /// <returns></returns>
        RecordTypeInfo ReadLoopFields()
        {
            bool firstLine = true;
            RecordTypeInfo loopType = null;
            int fieldIndex = 0;
            while (true)
            {
                NextLine();
                if (firstLine)
                {
                    loopType = GetRecordType();
                    if (loopType.Type == RecordType.Unknown) return loopType;
                    firstLine = false;
                    LoopRecordMap = new Dictionary<string, int>(131, StringComparer.Ordinal);
                }
                if (!StartsWith('_')) break;

                LoopRecordMap[CurrentLineText.Trim()] = fieldIndex;
                fieldIndex++;
            }
            LoopFieldCount = fieldIndex;
            return loopType;
        }

        /// <summary>
        /// Wrapper procedure for reading loop fields.
        /// </summary>
        /// <typeparam name="TFields"></typeparam>
        /// <param name="action"></param>
        void ReadLoopElements<TFields>(Action<TFields> action)
            where TFields : FieldsBase, new()
        {
            var fields = FieldsBase.CreateLoop<TFields>(this);

            while (CurrentLineText != null && !StartsWith('#'))
            {
                if (!TokenizeLoopElementOrFail()) break;
                action(fields);
                NextLine();
            }
        }

        /// <summary>
        /// Wrapper procedure for reading loop fields that require state.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <typeparam name="TFields"></typeparam>
        /// <param name="state"></param>
        /// <param name="action"></param>
        void ReadLoopElements<TState, TFields>(TState state, Action<TState, TFields> action)
            where TFields : FieldsBase, new()
        {
            var fields = FieldsBase.CreateLoop<TFields>(this);

            while (CurrentLineText != null && !StartsWith('#'))
            {
                if (!TokenizeLoopElementOrFail()) break;
                action(state, fields);
                NextLine();
            }
        }

        /// <summary>
        /// A loop reader that stored each element to a collection.
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <typeparam name="TFields"></typeparam>
        /// <param name="list"></param>
        /// <param name="fields"></param>
        void CollectionLoop<TElement, TFields>(ICollection<TElement> list, TFields fields)
            where TFields : FieldsBase<TElement, TFields>, new()
        {
            list.Add(fields.GetElement());
        }

        /// <summary>
        /// Reads the loop elements to a list.
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <typeparam name="TFields"></typeparam>
        /// <returns></returns>
        List<TElement> ReadLoopElementsToList<TElement, TFields>()
            where TFields : FieldsBase<TElement, TFields>, new()
        {
            var elements = new List<TElement>();
            ReadLoopElements<List<TElement>, TFields>(elements, CollectionLoop<TElement, TFields>);
            return elements;
        }

        /// <summary>
        /// Skips the current section by finding a # that is not inside a text block.
        /// </summary>
        void SkipSection()
        {
            while (NextLine() && !StartsWith('#'))
            {
            }
        }

        /// <summary>
        /// Handles the loop records.
        /// </summary>
        void HandleLoop()
        {
            // Determine the loop type.
            var type = ReadLoopFields();

            // Check if the loop is useful.
            if (type.Type == RecordType.Unknown)
            {
                SkipSection();
                return;
            }

            // Read the loop.
            var action = RecordsInfo[type.Type].OnLoop;
            if (action != null) action();
            else
            {
                Warnings.Add(new StructureReaderWarning(string.Concat("A loop of '{0}' is not supported.", type.Key)));
            }
        }
                
        /// <summary>
        /// Handles the non-loop records.
        /// </summary>
        void HandleSingleRecord()
        {
            var type = GetRecordType();

            // Skip records that are not useful.
            if (type.Type == RecordType.Unknown)
            {
                SkipSection();
                return;
            }

            var info = RecordsInfo[type.Type];

            // Read the fields
            var fields = FieldsBase.CreateSingle(info.FieldsType, this);
            while (CurrentLineText != null && !StartsWith('#'))
            {
                var spaceIndex = CurrentLineText.IndexOf(' ');
                if (spaceIndex < 0) spaceIndex = CurrentLineText.IndexOf('\t');

                string name, value;

                if (spaceIndex < 0)
                {
                    if (string.IsNullOrWhiteSpace(CurrentLineText))
                    {
                        NextLine();
                        continue;
                    }

                    name = CurrentLineText;
                    value = "";
                }
                else
                {
                    name = CurrentLineText.Substring(0, spaceIndex);
                    value = CurrentLineText.Substring(spaceIndex).Trim();
                }

                // Check if the actual value is on the next line.
                if (string.IsNullOrEmpty(value) && NextLine())
                {              
                    // multi-line string separated by ; at at start of lines.
                    if (StartsWith(';'))
                    {
                        var field = new StringBuilder();
                        field.Append(CurrentLineText.Substring(1));
                        while (NextLine() && !StartsWith(';'))
                        {
                            field.Append(CurrentLineText);
                        }
                        fields.AssignFieldValue(name, field.ToString(), true);
                    }                          
                    else // value at the entire line
                    {
                        fields.AssignFieldValue(name, CurrentLineText.Trim(), false);
                    }          
                }
                else
                {
                    fields.AssignFieldValue(name, value, false);
                }
                NextLine();
            }

            // Execute the fields action
            info.OnSingleBase(fields);
        }
        
        /// <summary>
        /// Reads a PDBx/mmCIF file.
        /// </summary>
        /// <returns></returns>
        StructureReaderResult ReadPdbX()
        {
            InitRecordsInfo();

            this.CompBonds = new List<CompBondElement>();
            this.Metadata = new PdbMetadata();
            
            try
            {
                var numDataEntries = 0;

                while (NextLine())
                {
                    if (CurrentLineText.Length == 0) continue;

                    if (CurrentLineText[0] == '_') HandleSingleRecord();
                    else if (StartWithIgnoreCase("loop_")) HandleLoop();
                    else if (StartWithIgnoreCase("data_"))
                    {
                        numDataEntries++;
                        if (numDataEntries > 1)
                        {
                            Warnings.Add(new StructureReaderWarning("The file contains multiple structures (data_ entries). Only the first structure was loaded. To load all structures, please split them into standalone files."));
                            break;
                        }                                
                    }
                }

                if (Atoms.Count == 0 && CompAtoms.Count == 0)
                {
                    throw new IOException("The file contains no atoms.");
                }

                if (IsComp && !HasAtomSite) return CreateCompStructure();
                return CreateStructure(FilterAtoms(Atoms));
            }
            catch (Exception e)
            {
                throw new IOException(string.Format("Invalid PDBx/mmCIF format. First error at line {0}: {1}", CurrentLine, e.Message));
            }
        }

        static IBond CreateBondFromElement(CompBondElement elem, Dictionary<string, PdbAtom> map)
        {
            PdbAtom a, b;
            if (!map.TryGetValue(elem.AtomA, out a)) throw new InvalidOperationException(string.Format("Bonds - cannot find atom with name '{0}'.", elem.AtomA));
            if (!map.TryGetValue(elem.AtomB, out b)) throw new InvalidOperationException(string.Format("Bonds - cannot find atom with name '{0}'.", elem.AtomB));

            BondType type = BondType.Single;
            if (elem.Type.EqualOrdinalIgnoreCase("sing")) type = BondType.Single;
            else if (elem.Type.EqualOrdinalIgnoreCase("doub") || elem.Type.EqualOrdinalIgnoreCase("delo")) type = BondType.Double;
            else if (elem.Type.EqualOrdinalIgnoreCase("trip")) type = BondType.Triple;

            return Bond.Create(a, b, type);
        }

        /// <summary>
        /// Creates a component structure.
        /// </summary>
        /// <returns></returns>
        StructureReaderResult CreateCompStructure()
        {
            Atoms = CompAtoms;
            var atoms = Atoms;
                        
            var orderedAtoms = AtomCollection.Create(atoms);
            var orderedResidues = PdbResidueCollection.Create(new[] { PdbResidue.Create(atoms) });
            var orderedChains = new ReadOnlyCollection<PdbChain>(new [] { new PdbChain(atoms[0].ChainIdentifier, orderedResidues) });

            var atomsByName = new Dictionary<string, PdbAtom>(atoms.Count * 2 + 1, StringComparer.Ordinal);

            foreach (var a in atoms)
            {
                if (atomsByName.ContainsKey(a.Name))
                {
                    throw new InvalidOperationException(string.Format("Duplicate atom name '{0}'.", a.Name));
                }
                atomsByName.Add(a.Name, a);
            }


            var bonds = BondCollection.Create(CompBonds.Select(e => CreateBondFromElement(e, atomsByName)));
            var structure = Structure.Create(Parameters.Id, orderedAtoms, bonds);

            //var knownBonds = CompBonds.Select(e => CreateBondFromElement(e, atomsByName)).ToList();
            //var bondComputationResult = ElementAndBondInfo.ComputePdbBonds(structure, knownBonds);
            
            structure.SetProperty(PdbStructure.ResiduesProperty, orderedResidues);
            structure.SetProperty(PdbStructure.IsPdbStructureProperty, true);
            structure.SetProperty(PdbStructure.IsPdbComponentStructureProperty, true);
            structure.SetProperty(PdbStructure.MetadataProperty, Metadata);
            structure.SetProperty(PdbStructure.ChainsProperty, orderedChains.ToDictionary(c => c.Identifier));
            
            structure.SetProperty(PdbStructure.HelicesProperty, new ReadOnlyCollection<PdbHelix>(new PdbHelix[0]));
            structure.SetProperty(PdbStructure.SheetsProperty, new ReadOnlyCollection<PdbSheet>(new PdbSheet[0]));

            return StructureReaderResult.Success(Filename, structure, Warnings);
        }
        #endregion

        #region Tokenizer

        /// <summary>
        /// Reads LoopFieldCount number of tokens from the input.
        /// </summary>
        /// <param name="tokenIndex"></param>
        bool TokenizeLoopElementOrFail(int tokenIndex = 0)
        {
            // Check for multiline tokens.
            if (StartsWith(';'))
            {
                var text = new StringBuilder();
                text.Append(CurrentLineText.Substring(1));
                while (NextLine() && !StartsWith(';')) text.Append(CurrentLineText);
                var value = text.ToString();
                tokenIndex = AddFieldTokenToBuffer(tokenIndex, value, 0, value.Length, true);

                if (CurrentLineText.Trim() != ";")
                {
                    tokenIndex = TokenizeCurrentLine(tokenIndex, true);
                }

                if (tokenIndex != LoopFieldCount)
                {
                    // If we don't have enough, read the next line and continue.
                    NextLine();
                    return TokenizeLoopElementOrFail(tokenIndex);
                }
                return true;
            }
            else
            {
                tokenIndex = TokenizeCurrentLine(tokenIndex);
                if (tokenIndex != LoopFieldCount)
                {
                    // If we don't have enough, read the next line and continue.
                    NextLine();

                    // Evil whitespace before #
                    if (tokenIndex == 0 && StartsWith("#")) return false;

                    return TokenizeLoopElementOrFail(tokenIndex);
                }
                return true;
            }
        }

        /// <summary>
        /// Assigns the current token to the LoopFields buffer.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="text"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="escaped"></param>
        /// <returns></returns>
        int AddFieldTokenToBuffer(int index, string text, int start, int count, bool escaped)
        {
            if (index >= LoopFieldCount) throw new InvalidOperationException("The number of records does not match the defined fields.");
            var item = LoopFields[index];

            if (item != null)
            {
                var elem = item.Element;
                elem.Start = start;
                elem.Count = count;
                elem.IsEscaped = escaped;
                elem.Text = text;
            }
            return index + 1;
        }
        
        /// <summary>
        /// Tokenize the current line.
        /// </summary>
        /// <param name="tokenIndex"></param>
        /// <returns></returns>
        int TokenizeCurrentLine(int tokenIndex, bool skipFirst = false)
        {
            var input = skipFirst ? CurrentLineText.Substring(1) : CurrentLineText;

            int currentTokenStart = 0, currentTokenEnd = 0;

            char escapeChar = ' ';
            bool inEscape = false;

            var lastIndex = input.Length - 1;
            for (int i = 0; i < input.Length; i++)
            {
                var current = input[i];
                
                // if in escape, read everything.
                if (inEscape)
                {
                    // Check if the escape ends.
                    if (current == escapeChar && (i == lastIndex || char.IsWhiteSpace(input[i + 1])) && input[i - 1] != '\\')
                    {
                        tokenIndex = AddFieldTokenToBuffer(tokenIndex, input, currentTokenStart, currentTokenEnd - currentTokenStart, true);
                        inEscape = false;
                        currentTokenStart = currentTokenEnd = i + 1;
                    }
                    else
                    {
                        currentTokenEnd = i + 1;
                    }
                }
                // Start of escaped token.
                else if (current == '"' || current == '\'')
                {
                    // Check if the char is preceded by a space to ensure it's a legit escape
                    if (i == 0 || char.IsWhiteSpace(input[i - 1]))
                    {
                        inEscape = true;
                        escapeChar = current;
                        currentTokenStart = currentTokenEnd = i + 1;
                    }
                    else
                    {
                        currentTokenEnd = i + 1;
                    }
                }
                // Move to the next token.
                else if (char.IsWhiteSpace(current))
                {
                    if (currentTokenStart != currentTokenEnd)
                    {
                        tokenIndex = AddFieldTokenToBuffer(tokenIndex, input, currentTokenStart, currentTokenEnd - currentTokenStart, false);
                    }
                    currentTokenStart = currentTokenEnd = i + 1;
                }
                // Increment the end of the current token.
                else
                {
                    currentTokenEnd = i + 1;
                }
            }
            if (currentTokenStart != currentTokenEnd)
            {
                tokenIndex = AddFieldTokenToBuffer(tokenIndex, input, currentTokenStart, currentTokenEnd - currentTokenStart, false);
            }
            return tokenIndex;
        }

        #endregion
    }
}
