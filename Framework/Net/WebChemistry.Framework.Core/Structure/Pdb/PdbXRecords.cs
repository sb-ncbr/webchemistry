namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal partial class PdbReader
    {
        #region Helpers
        abstract class RecordInfo
        {
            public Action OnLoop { get; set; }
            public abstract void OnSingleBase(FieldsBase f);
            public Type FieldsType { get; set; }

            public RecordInfo()
            {
                OnLoop = () => { };
            }
        }

        class RecordInfo<T> : RecordInfo
            where T : FieldsBase, new()
        {
            public Action<T> OnSingle { get; set; }

            public override void OnSingleBase(FieldsBase fs)
            {
                OnSingle((T)fs);
            }

            public RecordInfo()
            {
                FieldsType = typeof(T);
                OnSingle = _ => { };
            }
        }
        #endregion

        enum RecordType
        {
            Unknown,
            AtomSite,
            AtomComp,
            BondComp,
            Helices,
            Sheets,
            Revisions,
            Authors,
            Reflns, // Resolution
            Refine, 
            Keywords,
            Organism, // Details about the source or host organism
            ExpMethod, // Method of experiment xray, nmr, ...
            PolymerType, // polymer Type Protein, DNA, RNA, ...
            EntityType, // type of molecule natural, synthesized, GMO + EC and mol. weight,
            ModifiedResidues
        }
        
        void InitRecordsInfo()
        {
            RecordTypes = new Dictionary<string,RecordType>(StringComparer.Ordinal)
            {
                { "_atom_site",          RecordType.AtomSite    },
                { "_chem_comp_atom",     RecordType.AtomComp    },
                { "_chem_comp_bond",     RecordType.BondComp    },
                { "_struct_conf",        RecordType.Helices     },
                { "_struct_sheet_range", RecordType.Sheets      },
                { "_database_PDB_rev",   RecordType.Revisions   },
                { "_audit_author",       RecordType.Authors     },
                { "_reflns",             RecordType.Reflns      },
                { "_refine",             RecordType.Refine      },
                { "_struct_keywords",    RecordType.Keywords    },
                { "_exptl",              RecordType.ExpMethod   },
                { "_entity_src_gen",     RecordType.Organism    },
                { "_entity_src_nat",     RecordType.Organism    },
                { "_entity_poly",        RecordType.PolymerType },
                { "_entity",             RecordType.EntityType  },
                { "_pdbx_struct_mod_residue", RecordType.ModifiedResidues  }
            };

            RecordsInfo = new Dictionary<RecordType, RecordInfo>
            {
                // DEFAULT / EMPTY
                { 
                    RecordType.Unknown, 
                    null
                },

                // ATOM SITE
                { 
                    RecordType.AtomSite, 
                    new RecordInfo<AtomSiteFields>  
                    {
                        OnLoop = () => { HasAtomSite = true; ReadAtomSites(); },
                        OnSingle = fields => { HasAtomSite = true; Atoms.Add(fields.GetElement().Atom); },
                    }
                },
                // COMP ATOM SITE
                { 
                    RecordType.AtomComp, 
                    new RecordInfo<CompAtomFields>  
                    {
                        OnLoop = () => { IsComp = true; ReadLoopElements<CompAtomFields>(CompAtomLoop); },
                        OnSingle = fields => { IsComp = true; CompAtomLoop(fields); }
                    }
                },
                // COMP BOND
                { 
                    RecordType.BondComp, 
                    new RecordInfo<CompBondFields>  
                    {
                        OnLoop = () => ReadLoopElements<List<CompBondElement>, CompBondFields>(CompBonds, CollectionLoop<CompBondElement, CompBondFields>),
                        OnSingle = fields => CompBonds.Add(fields.GetElement())
                    }
                },
                // HELICES
                { 
                    RecordType.Helices, 
                    new RecordInfo<HelixFields>  
                    {
                        OnLoop = () => ReadLoopElements<List<SecondaryElementInfo>, HelixFields>(SecondaryElements, CollectionLoop<SecondaryElementInfo, HelixFields>),
                        OnSingle = fields => SecondaryElements.Add(fields.GetElement())
                    }
                },
                // SHEETS
                { 
                    RecordType.Sheets, 
                    new RecordInfo<SheetFields>
                    {
                        OnLoop = () => ReadLoopElements<List<SecondaryElementInfo>, SheetFields>(SecondaryElements, CollectionLoop<SecondaryElementInfo, SheetFields>),
                        OnSingle = fields => SecondaryElements.Add(fields.GetElement())
                    }
                },
                // AUTHORS
                { 
                    RecordType.Authors, 
                    new RecordInfo<AuthorFields>  
                    {
                        OnLoop = () => Metadata.Authors = ReadLoopElementsToList<string, AuthorFields>().ToArray(),
                        OnSingle = fields => Metadata.Authors = new[] { fields.GetElement() }
                    }
                },
                // REVISIONS
                { 
                    RecordType.Revisions, 
                    new RecordInfo<RevisionsFields>  
                    {
                        OnLoop = () => 
                        {
                            var revisions = ReadLoopElementsToList<RevisionElement, RevisionsFields>();
                            if (revisions.Count > 0)
                            {
                                Metadata.Released = revisions[0].Date;
                                Metadata.LatestRevision = revisions.Last().Date;
                            } 
                        },
                        OnSingle = fields => 
                        {
                            var rev = fields.GetElement();
                            Metadata.Released = rev.Date;
                            Metadata.LatestRevision = rev.Date;
                        }
                    }
                },
                // REFLNS
                { 
                    RecordType.Reflns, 
                    new RecordInfo<ReflnsFields>  
                    {
                        OnSingle = fields => { } // Metadata.Resolution = fields.GetElement().ResolutionHigh
                    }
                },
                // REFINE
                { 
                    RecordType.Refine, 
                    new RecordInfo<RefineFields>  
                    {
                        OnSingle = fields => Metadata.Resolution = fields.GetElement().ResolutionHigh
                    }
                },
                // KEYWORDS
                { 
                    RecordType.Keywords, 
                    new RecordInfo<KeywordsFields>  
                    {
                        OnSingle = fields => 
                        {
                            var keywords = fields.GetElement();
                            if (string.IsNullOrEmpty(keywords.PdbXKeywords))
                            {
                                Metadata.Keywords = keywords.Text.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToArray();
                            }
                            else
                            {
                                Metadata.Keywords = (keywords.Text + "," + keywords.PdbXKeywords)
                                    .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                    .Select(f => f.Trim())
                                    .Distinct(StringComparer.Ordinal)
                                    .ToArray();
                            }
                        }
                    }
                },
                //EXPERIMENT METHOD
                {
                    RecordType.ExpMethod,
                    new RecordInfo<ExpMethodFields>
                    {
                        OnSingle = fields => Metadata.ExperimentMethod = fields.GetElement()
                    }                
                },
                //ENTITY TYPE
                {
                    RecordType.EntityType,
                    new RecordInfo<EntityTypeFields>
                    {
                        OnSingle = fields => 
                        {
                            var element = fields.GetElement();
                            Metadata.EntityDataById[element.EntityId] = element;
                        },
                        OnLoop = () => 
                        {
                            foreach (var e in ReadLoopElementsToList<PdbEntityData, EntityTypeFields>()) Metadata.EntityDataById[e.EntityId] = e;
                        }
                    }
                },
                //ORGANISM IDENTIFICATION
                {
                    RecordType.Organism,
                    new RecordInfo<OrganismFields>
                    {
                        OnSingle = fields => 
                        {
                            var element = fields.GetElement();
                            Metadata.OrganismDataByEntityId[element.EntityId] = element;
                        },
                        OnLoop = () => 
                        {
                            foreach (var e in ReadLoopElementsToList<PdbOrganismData, OrganismFields>()) Metadata.OrganismDataByEntityId[e.EntityId] = e;
                        }
                    }
                },
                //POLYMER TYPE
                {
                    RecordType.PolymerType,
                    new RecordInfo<PolymerTypeFields>
                    {
                        OnSingle = fields => 
                        {
                            var element = fields.GetElement();
                            Metadata.PolymerTypeDataByEntityId[element.EntityId] = element;
                        },
                        OnLoop = () => 
                        {
                            foreach (var e in ReadLoopElementsToList<PdbPolymerTypeData, PolymerTypeFields>()) Metadata.PolymerTypeDataByEntityId[e.EntityId] = e;
                        }
                    }
                },
                //MODRES
                {
                    RecordType.ModifiedResidues,
                    new RecordInfo<ModifiedResiduesFields>
                    {
                        OnSingle = fields => 
                        {
                            var element = fields.GetElement();
                            if (!string.IsNullOrWhiteSpace(element.ModifiedFrom)) ModifiedResidues.Add(element);
                        },
                        OnLoop = () => 
                        {
                            ModifiedResidues.AddRange(ReadLoopElementsToList<ModifiedResidueInfo, ModifiedResiduesFields>().Where(e => !string.IsNullOrWhiteSpace(e.ModifiedFrom)));
                        }
                    }
                },
            };
        }

        void ReadAtomSites()
        {
            var fields = FieldsBase.CreateLoop<AtomSiteFields>(this);
            var modelNumbers = new HashSet<string>(StringComparer.Ordinal);

            while (CurrentLineText != null)
            {
                if (IsCommentOrBlank(CurrentLineText)) { NextLine(); continue; }
                if (IsConstructStart(CurrentLineText)) break;

                // If the first model was already loaded, skip the rest of the atoms.
                if (modelNumbers.Count > 1)
                {
                    NextLine();
                    continue;
                }

                // Tokenize the element.
                if (!TokenizeLoopElementOrFail()) break;

                // Parse the atom and add it to the atom list.
                var atom = fields.GetElement();
                modelNumbers.Add(atom.ModelNumber);
                if (modelNumbers.Count > 1)
                {
                    Warnings.Add(new OnlyFirstModelLoadedReaderWarning(CurrentLine));
                }
                else if (HandleDuplicates(atom.Atom))
                {
                    Atoms.Add(atom.Atom);
                }

                // Move to the next line.
                NextLine();
            }
        }
        
        void CompAtomLoop(CompAtomFields fields)
        {
            var atom = fields.GetElement();
            /*if (HandleDuplicates(atom))*/ CompAtoms.Add(atom);
        }   
    }
}
