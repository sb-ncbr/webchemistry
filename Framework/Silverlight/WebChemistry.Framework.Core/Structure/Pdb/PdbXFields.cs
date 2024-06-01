namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using WebChemistry.Framework.Math;

    internal partial class PdbReader
    {
        #region Base

        class FieldElement
        {
            public string Text;
            public int Start, Count;
            public bool IsEscaped;

            public static FieldElement FromString(string str, bool isAlreadyEscaped)
            {
                if (!isAlreadyEscaped && str.Length >= 2)
                {
                    int li = str.Length - 1;
                    if ((str[0] == '\'' && str[li] == '\'') || (str[0] == '"' && str[li] == '"'))
                    {
                        return new FieldElement
                        {
                            Text = str,
                            IsEscaped = true,
                            Start = 1,
                            Count = str.Length - 2
                        };
                    }
                }
                return new FieldElement
                {
                    Text = str,
                    IsEscaped = isAlreadyEscaped,
                    Start = 0,
                    Count = str.Length
                };
            }
        }

        class FieldElementInfo
        {
            public FieldElement Element { get; private set; }
            public int ElementIndex { get; private set; }

            public string Name { get; private set; }
            public bool IsRequired { get; private set; }

            double DefaultValueDouble;
            int DefaultValueInt;
            string DefaultValueString;

            void CheckRequired()
            {
                if (IsRequired)
                {
                    char e;
                    if (Element == null || (!Element.IsEscaped && ((e = Element.Text[Element.Start]) == '?' || e == '.')))
                    {
                        throw new InvalidOperationException(string.Format("The value of '{0}' is required.", Name));
                    }
                }
            }

            bool IsNull()
            {
                char e;
                if (Element == null) return true;
                if (!Element.IsEscaped && ((e = Element.Text[Element.Start]) == '?' || e == '.')) return true;
                return false;
            }

            public void Update(Dictionary<string, int> map)
            {
                var contains = map.ContainsKey(Name);
                if (!contains && IsRequired)
                {
                    throw new InvalidOperationException(string.Format("The field '{0}' is required.", Name));
                }
                if (contains)
                {
                    Element = new FieldElement();
                    ElementIndex = map[Name];
                }
                else
                {
                    Element = null;
                    ElementIndex = -1;
                }
            }

            public void AssignElement(FieldElement element)
            {
                Element = element;
            }
            
            public double GetDoubleValue()
            {
                CheckRequired();
                if (Element == null) return DefaultValueDouble;
                return NumberParser.ParseDoubleFast(Element.Text, Element.Start, Element.Count);
            }

            public double? GetNullableDoubleValue()
            {
                if (IsNull()) return null;
                return NumberParser.ParseDoubleFast(Element.Text, Element.Start, Element.Count);
            }

            public int GetIntValue()
            {
                CheckRequired();
                if (Element == null) return DefaultValueInt;
                return NumberParser.ParseIntFast(Element.Text, Element.Start, Element.Count);
            }

            public int? GetNullableIntValue()
            {
                if (IsNull()) return null;
                return NumberParser.ParseIntFast(Element.Text, Element.Start, Element.Count);
            }

            public string GetStringValue()
            {
                CheckRequired();
                if (Element == null) return DefaultValueString;
                return Element.Text.Substring(Element.Start, Element.Count);
            }

            public string GetNullableStringValue()
            {
                if (IsNull()) return null;
                return Element.Text.Substring(Element.Start, Element.Count);
            }

            public FieldElementInfo(string name, bool isRequired = true)
            {
                this.IsRequired = isRequired;
                this.Name = name;
            }

            public FieldElementInfo(string name, string defaultValue)
            {
                IsRequired = false;
                this.DefaultValueString = defaultValue;
                this.Name = name;
            }

            public FieldElementInfo(string name, int defaultValue)
            {
                IsRequired = false;
                this.DefaultValueInt = defaultValue;
                this.Name = name;
            }

            public FieldElementInfo(string name, double defaultValue)
            {
                IsRequired = false;
                this.DefaultValueDouble = defaultValue;
                this.Name = name;
            }
        }

        abstract class FieldsBase
        {
            protected abstract FieldElementInfo[] AllFields();

            protected Dictionary<string, FieldElementInfo> FieldMap;
            protected PdbReader Reader;

            public void AssignFieldValue(string name, string value, bool isAlreadyEscaped)
            {
                FieldElementInfo info;
                if (FieldMap.TryGetValue(name, out info))
                {
                    info.AssignElement(FieldElement.FromString(value, isAlreadyEscaped));
                }
            }

            void InitLoop(FieldElementInfo[] allFields, PdbReader reader)
            {
                var map = reader.LoopRecordMap;
                reader.LoopFields = new FieldElementInfo[map.Count];

                foreach (var f in allFields)
                {
                    f.Update(map);
                    if (f.ElementIndex >= 0)
                    {
                        reader.LoopFields[f.ElementIndex] = f;
                    }
                }
            }

            void InitSingle(FieldElementInfo[] allFields)
            {
                FieldMap = allFields.ToDictionary(f => f.Name, StringComparer.Ordinal);
            }

            public static T CreateLoop<T>(PdbReader reader)
                where T : FieldsBase, new()
            {
                var ret = new T { Reader = reader };
                ret.InitLoop(ret.AllFields(), reader);
                return ret;
            }

            public static FieldsBase CreateSingle(Type type, PdbReader reader)
            {
                var ret = (FieldsBase)Activator.CreateInstance(type);
                ret.Reader = reader;
                ret.InitSingle(ret.AllFields());
                return ret;
            }

            public static T CreateSingle<T>(PdbReader reader)
                where T : FieldsBase, new()
            {
                var ret = new T { Reader = reader };
                ret.InitSingle(ret.AllFields());
                return ret;
            }
        }

        abstract class FieldsBase<TElement, TFields> : FieldsBase
        {
            static FieldInfo[] allFields;
            static FieldInfo[] AllFieldsInfo()
            {
                if (allFields != null) return allFields;
                var fields = typeof(TFields).GetFields();
                var fieldType = typeof(FieldElementInfo);
                allFields = fields.Where(f => f.FieldType == fieldType).ToArray();
                return allFields;
            }

            protected override FieldElementInfo[] AllFields()
            {
                return AllFieldsInfo().Select(f => (FieldElementInfo)f.GetValue(this)).ToArray();
            }

            public abstract TElement GetElement();
        }

        #endregion
        
        #region Elements

        class AtomSiteElement
        {
            public readonly PdbAtom Atom;
            public readonly string ModelNumber;

            public AtomSiteElement(PdbAtom atom, string modelNum)
            {
                this.Atom = atom;
                this.ModelNumber = modelNum;
            }
        }

        class CompBondElement
        {
            public string AtomA, AtomB, Type;
        }

        class RevisionElement
        {
            public int Num;
            public DateTime? Date, DateOriginal;
        }

        class ReflnsElement
        {
            public double? ResolutionHigh, ResolutionLow;
        }

        class RefineElement
        {
            public double? ResolutionHigh;
        }

        class KeywordsElement
        {
            public string Text, PdbXKeywords;
        }
        
        #endregion
        
        class AtomSiteFields : FieldsBase<AtomSiteElement, AtomSiteFields>
        {
            public readonly FieldElementInfo Id = new FieldElementInfo("_atom_site.id");

            public readonly FieldElementInfo PositionX = new FieldElementInfo("_atom_site.Cartn_x");
            public readonly FieldElementInfo PositionY = new FieldElementInfo("_atom_site.Cartn_y");
            public readonly FieldElementInfo PositionZ = new FieldElementInfo("_atom_site.Cartn_z");

            public readonly FieldElementInfo RecordName = new FieldElementInfo("_atom_site.group_PDB", "HETATM");
            public readonly FieldElementInfo ElementSymbol = new FieldElementInfo("_atom_site.type_symbol");
            public readonly FieldElementInfo Name = new FieldElementInfo("_atom_site.auth_atom_id");

            public readonly FieldElementInfo AltLoc = new FieldElementInfo("_atom_site.label_alt_id", "");

            public readonly FieldElementInfo EntityId = new FieldElementInfo("_atom_site.label_entity_id");

            public readonly FieldElementInfo ResidueName = new FieldElementInfo("_atom_site.auth_comp_id");
            public readonly FieldElementInfo ResidueSeqNumber = new FieldElementInfo("_atom_site.auth_seq_id");
            public readonly FieldElementInfo ChainIdentifier = new FieldElementInfo("_atom_site.auth_asym_id", defaultValue: "");
            public readonly FieldElementInfo InsertionCode = new FieldElementInfo("_atom_site.pdbx_PDB_ins_code", " ");

            public readonly FieldElementInfo Occupancy = new FieldElementInfo("_atom_site.occupancy", 1.0);
            public readonly FieldElementInfo TemperatureFactor = new FieldElementInfo("_atom_site.B_iso_or_equiv", 0.0);

            public readonly FieldElementInfo ModelNumber = new FieldElementInfo("_atom_site.pdbx_PDB_model_num", " ");

            public override AtomSiteElement GetElement()
            {
                int serialNumber = Id.GetIntValue();
                                
                string altLoc = AltLoc.GetStringValue();
                char alternateLocationIdent = string.IsNullOrEmpty(altLoc) || altLoc[0] == '.' ? ' ' : altLoc[0];

                string residueName = ResidueName.GetStringValue();
                int residueSequenceNumber = ResidueSeqNumber.GetIntValue();
                
                string chainIdentifier = ChainIdentifier.GetStringValue();
                if (string.IsNullOrWhiteSpace(chainIdentifier)) chainIdentifier = "";

                string insCode = InsertionCode.GetStringValue();
                char insertionResidueCode = string.IsNullOrWhiteSpace(insCode) || insCode.EqualIgnoreCase("?") || insCode.EqualIgnoreCase(".") ? ' ' : insCode[0];
                
                var atom = (PdbAtom)PdbAtom.Create(
                    id: serialNumber,
                    elementSymbol: Core.ElementSymbol.Create(ElementSymbol.GetStringValue()),
                    residueName: residueName,
                    serialNumber: serialNumber,
                    entityId: EntityId.GetIntValue(),
                    residueSequenceNumber: residueSequenceNumber,
                    chainIdentifier: chainIdentifier,
                    alternateLocationIdentifier: alternateLocationIdent,
                    name: Name.GetStringValue(),
                    recordName: RecordName.GetStringValue(),
                    insertionResidueCode: insertionResidueCode,
                    occupancy: Occupancy.GetDoubleValue(),
                    temperatureFactor: TemperatureFactor.GetDoubleValue(),
                    position: new Vector3D(PositionX.GetDoubleValue(), PositionY.GetDoubleValue(), PositionZ.GetDoubleValue()));

                return new AtomSiteElement(atom, ModelNumber.GetStringValue());
            }
        }

        class CompAtomFields : FieldsBase<PdbCompAtom, CompAtomFields>
        {
            public readonly FieldElementInfo IdealPositionX = new FieldElementInfo("_chem_comp_atom.pdbx_model_Cartn_x_ideal", isRequired: false);
            public readonly FieldElementInfo IdealPositionY = new FieldElementInfo("_chem_comp_atom.pdbx_model_Cartn_y_ideal", isRequired: false);
            public readonly FieldElementInfo IdealPositionZ = new FieldElementInfo("_chem_comp_atom.pdbx_model_Cartn_z_ideal", isRequired: false);

            public readonly FieldElementInfo ModelPositionX = new FieldElementInfo("_chem_comp_atom.model_Cartn_x", isRequired: false);
            public readonly FieldElementInfo ModelPositionY = new FieldElementInfo("_chem_comp_atom.model_Cartn_y", isRequired: false);
            public readonly FieldElementInfo ModelPositionZ = new FieldElementInfo("_chem_comp_atom.model_Cartn_z", isRequired: false);

            public readonly FieldElementInfo ElementSymbol = new FieldElementInfo("_chem_comp_atom.type_symbol");
            public readonly FieldElementInfo Name = new FieldElementInfo("_chem_comp_atom.atom_id");
            public readonly FieldElementInfo ResidueName = new FieldElementInfo("_chem_comp_atom.comp_id");

            public readonly FieldElementInfo StereoConfig = new FieldElementInfo("_chem_comp_atom.pdbx_stereo_config", isRequired: false);

            public override PdbCompAtom GetElement()
            {
                int serialNumber = Reader.CompAtoms.Count + 1;

                var chs = StereoConfig.GetNullableStringValue();
                var chirality = AtomChiralityRS.None;
                if (chs != null)
                {
                    chirality = chs.EqualOrdinalIgnoreCase("R")
                        ? AtomChiralityRS.R
                        : chs.EqualOrdinalIgnoreCase("S")
                        ? AtomChiralityRS.S
                        : AtomChiralityRS.None;
                }

                double? x = ModelPositionX.GetNullableDoubleValue(),
                    y = ModelPositionY.GetNullableDoubleValue(),
                    z = ModelPositionZ.GetNullableDoubleValue();

                var model = x.HasValue && y.HasValue && z.HasValue ? (Vector3D?)new Vector3D(x.Value, y.Value, z.Value) : null;

                double? ix = IdealPositionX.GetNullableDoubleValue(),
                    iy = IdealPositionY.GetNullableDoubleValue(),
                    iz = IdealPositionZ.GetNullableDoubleValue();

                var ideal = ix.HasValue && iy.HasValue && iz.HasValue ? (Vector3D?)new Vector3D(ix.Value, iy.Value, iz.Value) : null;

                return (PdbCompAtom)PdbCompAtom.Create(
                    id: serialNumber,
                    elementSymbol: Core.ElementSymbol.Create(ElementSymbol.GetStringValue()),
                    residueName: ResidueName.GetStringValue(),
                    serialNumber: serialNumber,
                    name: Name.GetStringValue(),
                    recordName: "HETATM",
                    chirality: chirality,
                    modelPosition: model,
                    idealPosition: ideal,
                    position: model.HasValue ? model.Value : new Vector3D());
            }
        }

        class CompBondFields : FieldsBase<CompBondElement, CompBondFields>
        {
            public readonly FieldElementInfo AtomA = new FieldElementInfo("_chem_comp_bond.atom_id_1");
            public readonly FieldElementInfo AtomB = new FieldElementInfo("_chem_comp_bond.atom_id_2");
            public readonly FieldElementInfo Type = new FieldElementInfo("_chem_comp_bond.value_order");

            public override CompBondElement GetElement()
            {
                return new CompBondElement
                {
                    AtomA = AtomA.GetStringValue(),
                    AtomB = AtomB.GetStringValue(),
                    Type = Type.GetStringValue()
                };
            }
        }

        class HelixFields : FieldsBase<SecondaryElementInfo, HelixFields>
        {
            public readonly FieldElementInfo StartResidueSeqNumber = new FieldElementInfo("_struct_conf.beg_auth_seq_id");
            public readonly FieldElementInfo StartChainIdentifier = new FieldElementInfo("_struct_conf.beg_auth_asym_id");

            public readonly FieldElementInfo EndResidueSeqNumber = new FieldElementInfo("_struct_conf.end_auth_seq_id");
            public readonly FieldElementInfo EndChainIdentifier = new FieldElementInfo("_struct_conf.end_auth_asym_id");
            
            public override SecondaryElementInfo GetElement()
            {
                return new SecondaryElementInfo
                {
                    Type = SecondaryStructureType.Helix,
                    Start = PdbResidueIdentifier.Create(StartResidueSeqNumber.GetIntValue(), StartChainIdentifier.GetStringValue(), ' '),
                    End = PdbResidueIdentifier.Create(EndResidueSeqNumber.GetIntValue(), EndChainIdentifier.GetStringValue(), ' ')
                };
            }
        }

        class SheetFields : FieldsBase<SecondaryElementInfo, SheetFields>
        {
            public readonly FieldElementInfo StartResidueSeqNumber = new FieldElementInfo("_struct_sheet_range.beg_auth_seq_id");
            public readonly FieldElementInfo StartChainIdentifier = new FieldElementInfo("_struct_sheet_range.beg_auth_asym_id");

            public readonly FieldElementInfo EndResidueSeqNumber = new FieldElementInfo("_struct_sheet_range.end_auth_seq_id");
            public readonly FieldElementInfo EndChainIdentifier = new FieldElementInfo("_struct_sheet_range.end_auth_asym_id");
            
            public override SecondaryElementInfo GetElement()
            {
                return new SecondaryElementInfo
                {
                    Type = SecondaryStructureType.Sheet,
                    Start = PdbResidueIdentifier.Create(StartResidueSeqNumber.GetIntValue(), StartChainIdentifier.GetStringValue(), ' '),
                    End = PdbResidueIdentifier.Create(EndResidueSeqNumber.GetIntValue(), EndChainIdentifier.GetStringValue(), ' ')
                };
            }
        }

        class AuthorFields : FieldsBase<string, AuthorFields>
        {
            public readonly FieldElementInfo Name = new FieldElementInfo("_audit_author.name");
            
            public override string GetElement()
            {
                return Name.GetStringValue();
            }
        }
        
        class RevisionsFields : FieldsBase<RevisionElement, RevisionsFields>
        {
            public readonly FieldElementInfo Num = new FieldElementInfo("_database_PDB_rev.num");
            public readonly FieldElementInfo Date = new FieldElementInfo("_database_PDB_rev.date");
            public readonly FieldElementInfo DateOriginal = new FieldElementInfo("_database_PDB_rev.date_original", defaultValue: "?");
            
            public override RevisionElement GetElement()
            {
                DateTime date, origDate;

                bool hasDate = DateTime.TryParseExact(Date.GetStringValue(), "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
                bool hasOrigDate = DateTime.TryParseExact(DateOriginal.GetStringValue(), "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out origDate);

                return new RevisionElement
                {
                    Num = Num.GetIntValue(),
                    Date = hasDate ? (DateTime?)date : null,
                    DateOriginal = hasOrigDate ? (DateTime?)origDate : null
                };
            }
        }

        class ReflnsFields : FieldsBase<ReflnsElement, ReflnsFields>
        {
            public readonly FieldElementInfo ResolutionHigh = new FieldElementInfo("_reflns.d_resolution_high", isRequired: false);
            public readonly FieldElementInfo ResolutionLow = new FieldElementInfo("_reflns.d_resolution_low", isRequired: false);
            
            public override ReflnsElement GetElement()
            {
                return new ReflnsElement
                {
                    ResolutionHigh = ResolutionHigh.GetNullableDoubleValue(),
                    ResolutionLow = ResolutionLow.GetNullableDoubleValue()
                };
            }
        }

        class RefineFields : FieldsBase<RefineElement, RefineFields>
        {
            public readonly FieldElementInfo ResolutionHigh = new FieldElementInfo("_refine.ls_d_res_high", isRequired: false);

            public override RefineElement GetElement()
            {
                return new RefineElement
                {
                    ResolutionHigh = ResolutionHigh.GetNullableDoubleValue()
                };
            }
        }

        class KeywordsFields : FieldsBase<KeywordsElement, KeywordsFields>
        {
            public readonly FieldElementInfo Text = new FieldElementInfo("_struct_keywords.text");
            public readonly FieldElementInfo PdbXKeywords = new FieldElementInfo("_struct_keywords.pdbx_keywords", isRequired: false);
            
            public override KeywordsElement GetElement()
            {
                return new KeywordsElement
                {
                    Text = Text.GetStringValue(),
                    PdbXKeywords = PdbXKeywords.GetNullableStringValue()
                };
            }
        }

        class ExpMethodFields : FieldsBase<string, ExpMethodFields>
        {
            public readonly FieldElementInfo ExpMethod = new FieldElementInfo("_exptl.method");

            protected override FieldElementInfo[] AllFields()
            {
                return new[]
                {
                    ExpMethod
                };
            }

            public override string GetElement()
            {
                return ExpMethod.GetStringValue();
            }
        }

        class EntityTypeFields : FieldsBase<PdbEntityData, EntityTypeFields>
        {
            public readonly FieldElementInfo Id = new FieldElementInfo("_entity.id");
            public readonly FieldElementInfo Type = new FieldElementInfo("_entity.type");
            public readonly FieldElementInfo SrcMethod = new FieldElementInfo("_entity.src_method");
            public readonly FieldElementInfo Weight = new FieldElementInfo("_entity.formula_weight", isRequired: false);
            public readonly FieldElementInfo NumberOfMolecules = new FieldElementInfo("_entity.pdbx_number_of_molecules", isRequired: false);
            public readonly FieldElementInfo Ec = new FieldElementInfo("_entity.pdbx_ec", isRequired: false);

            static PdbEntityType GetEntityType(string type)
            {
                if (string.IsNullOrEmpty(type)) return PdbEntityType.Unknown;
                if (type.StartsWith("polymer", StringComparison.OrdinalIgnoreCase)) return PdbEntityType.Polymer;
                if (type.StartsWith("non-polymer", StringComparison.OrdinalIgnoreCase)) return PdbEntityType.NonPolymer;
                if (type.StartsWith("water", StringComparison.OrdinalIgnoreCase)) return PdbEntityType.Water;
                return PdbEntityType.Unknown;
            }

            static PdbEntitySource GetEntitySource(string method)
            {
                switch (method)
                {
                    case "nat":
                        return PdbEntitySource.Natural;
                    case "man":
                        return PdbEntitySource.GMO;
                    case "syn":
                        return PdbEntitySource.Synthetic;
                    default:
                        return PdbEntitySource.NotAssigned;
                }
            }

            public override PdbEntityData GetElement()
            {
                var numberOfMols = NumberOfMolecules.GetNullableIntValue();
                return new PdbEntityData
                {
                    EntityId = Id.GetIntValue(),
                    Type = GetEntityType(Type.GetNullableStringValue()),
                    Source = GetEntitySource(SrcMethod.GetNullableStringValue()),
                    WeightInKda = Weight.GetNullableDoubleValue(),
                    NumberOfMolecules = numberOfMols.HasValue ? numberOfMols.Value : 1,
                    EcNumber = Ec.GetNullableStringValue()
                };
            }
        }

        class PolymerTypeFields : FieldsBase<PdbPolymerTypeData, PolymerTypeFields>
        {
            public readonly FieldElementInfo Id = new FieldElementInfo("_entity_poly.entity_id");
            public readonly FieldElementInfo Type = new FieldElementInfo("_entity_poly.type");
            public readonly FieldElementInfo Chains = new FieldElementInfo("_entity_poly.pdbx_strand_id", defaultValue: "");

            static PdbPolymerType GetPolymerType(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return PdbPolymerType.Other;
                if (s.StartsWith("polypeptide", StringComparison.OrdinalIgnoreCase)) return PdbPolymerType.Protein;
                if (s.StartsWith("polydeoxy", StringComparison.OrdinalIgnoreCase)) return PdbPolymerType.DNA;
                if (s.StartsWith("polyribo", StringComparison.OrdinalIgnoreCase)) return PdbPolymerType.RNA;
                if (s.StartsWith("polysaccharide", StringComparison.OrdinalIgnoreCase)) return PdbPolymerType.Sugar;

                return PdbPolymerType.Other;
            }

            public override PdbPolymerTypeData GetElement()
            {
                return new PdbPolymerTypeData
                {
                    EntityId = Id.GetIntValue(),
                    Type = GetPolymerType(Type.GetNullableStringValue()),
                    Chains = Chains.GetStringValue().Split(new char[] { ',' }).Select(t => t.Trim()).ToArray()
                };
            }
        }
        // ... _src_ ... protein extracted from GMO organism
        // ... _nat_ ... protein extracted form natural tissue
        // there is either src or nat per file if not synthesized ab initio. In that case neither of them is present
        class OrganismFields : FieldsBase<PdbOrganismData, OrganismFields>
        {
            public readonly FieldElementInfo FieldIdSrc = new FieldElementInfo("_entity_src_gen.entity_id", isRequired: false);
            public readonly FieldElementInfo FieldIdNat = new FieldElementInfo("_entity_src_nat.entity_id", isRequired: false);

            public readonly FieldElementInfo OriginOrganismSrc = new FieldElementInfo("_entity_src_gen.pdbx_gene_src_scientific_name", isRequired: false);
            public readonly FieldElementInfo OriginOrganismSrcId = new FieldElementInfo("_entity_src_gen.pdbx_gene_src_ncbi_taxonomy_id", isRequired: false);
            public readonly FieldElementInfo OriginOrganismSrcGenus = new FieldElementInfo("_entity_src_gen.gene_src_genus", isRequired: false);

            public readonly FieldElementInfo OriginOrganismNat = new FieldElementInfo("_entity_src_nat.pdbx_organism_scientific", isRequired: false);
            public readonly FieldElementInfo OriginOrganismNatId = new FieldElementInfo("_entity_src_nat.pdbx_ncbi_taxonomy_id", isRequired: false);
            public readonly FieldElementInfo OriginOrganismNatGenus = new FieldElementInfo("_entity_src_nat.genus", isRequired: false);

            public readonly FieldElementInfo HostOrganism = new FieldElementInfo("_entity_src_gen.pdbx_host_org_scientific_name", isRequired: false);
            public readonly FieldElementInfo HostOrganismId = new FieldElementInfo("_entity_src_gen.pdbx_host_org_ncbi_taxonomy_id", isRequired: false);
            public readonly FieldElementInfo HostOrganismGenus = new FieldElementInfo("_entity_src_gen.host_org_genus", isRequired: false);

            public override PdbOrganismData GetElement()
            {
                return new PdbOrganismData
                {
                    EntityId = (FieldIdSrc.GetNullableIntValue() ?? FieldIdNat.GetNullableIntValue()).Value,

                    TaxonomyId = OriginOrganismNatId.GetNullableStringValue() ?? OriginOrganismSrcId.GetNullableStringValue(),
                    Name = OriginOrganismNat.GetNullableStringValue() ?? OriginOrganismSrc.GetNullableStringValue(),
                    Genus = OriginOrganismNatGenus.GetNullableStringValue() ?? OriginOrganismSrcGenus.GetNullableStringValue(),
                    
                    HostTaxonomyId = HostOrganismId.GetNullableStringValue(),
                    HostName = HostOrganism.GetNullableStringValue(),
                    HostGenus = HostOrganismGenus.GetNullableStringValue()
                };
            }
        }

        class ModifiedResiduesFields : FieldsBase<ModifiedResidueInfo, ModifiedResiduesFields>
        {
            public readonly FieldElementInfo Chain = new FieldElementInfo("_pdbx_struct_mod_residue.auth_asym_id", defaultValue: "");
            public readonly FieldElementInfo Number = new FieldElementInfo("_pdbx_struct_mod_residue.auth_seq_id", defaultValue: "0");
            public readonly FieldElementInfo InsCode = new FieldElementInfo("_pdbx_struct_mod_residue.PDB_ins_code", defaultValue: " ");
            public readonly FieldElementInfo ModifiedFrom = new FieldElementInfo("_pdbx_struct_mod_residue.parent_comp_id", defaultValue: "");

            public override ModifiedResidueInfo GetElement()
            {
                var insCode = InsCode.GetStringValue();
                if (insCode.EqualOrdinal("?")) insCode = " ";
                return new ModifiedResidueInfo
                {
                    Id = PdbResidueIdentifier.Create(Number.GetIntValue(), Chain.GetStringValue(), insCode.Length == 0 ? ' ' : insCode[0]),
                    ModifiedFrom = ModifiedFrom.GetStringValue()
                };
            }
        }

    }
}