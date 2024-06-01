using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Core.Queries;
using WebChemistry.Platform;

namespace WebChemistry.MotiveAtlas.Analyzer
{
    static class AnalysisDescriptors
    {
        static double AmbientRadius = 5;
        static int ConnectedCount = 2;

        static MotiveAnalysisDescriptor QueryBased(string name, string description, QueryBuilderElement q, Func<IStructure, bool> shouldVisit)
        {
            var m = q.Named();

            return new MotiveAnalysisDescriptor
            {
                Name = name,
                Description = description,
                ShouldVisitStructure = shouldVisit,
                BaseQuery = m.ToMetaQuery().Compile() as QueryMotive,
                QueryGroups = new Dictionary<string, QueryMotive>(StringComparer.Ordinal)
                { 
                    { MotiveAnalysisDescriptor.AmbientQueryGroup, m.AmbientResidues(AmbientRadius, YieldNamedDuplicates: true).ToMetaQuery().Compile() as QueryMotive },
                    { MotiveAnalysisDescriptor.ConnectedQueryGroup, m.ConnectedAtoms(ConnectedCount, YieldNamedDuplicates: true).ToMetaQuery().Compile() as QueryMotive }
                }
            };
        }

        static MotiveAnalysisDescriptor ResidueBased(string name)
        {
            return QueryBased(name, name + " residues", QueryBuilder.Residues(name), s => s.PdbResidues().ContainsResidueName(name));
        }

        static MotiveAnalysisDescriptor AtomBased(ElementSymbol symbol)
        {
            return QueryBased(symbol.ToString(), symbol.GetLongName(), QueryBuilder.Atoms(symbol.ToString()), s => s.Atoms.Any(a => a.ElementSymbol == symbol));
        }

        static MotiveAnalysisDescriptor ElmBased(string[] entry)
        {
            return QueryBased(entry[0].Substring(4), entry[1], QueryBuilder.RegularMotifs(entry[2]), _ => true);
        }

        static List<CategoryAnalysisDescriptor> GetRegularMotiveCategories()
        {
            var splitter = new char[] { '\t' };
            var entries = File
                .ReadAllLines("elm_classes.tsv")
                .Skip(1)
                .Select(l => l.Split(splitter).Select(e => e.Trim(' ', '"')).ToArray())
                .GroupBy(e => e[0].Substring(0, 3))
                .ToDictionary(g => g.Key, g => g.ToArray());

            var category = new CategoryAnalysisDescriptor
            {
                Name = "Regular Motifs",
                Description = "Regular motifs from http://elm.eu.org/.",
                SubCategories = new List<SubCategoryAnalysisDescriptor>
                {
                    new SubCategoryAnalysisDescriptor
                    {
                        Name = "CLV",
                        Description = "Cleavage Sites",
                        Motives = entries["CLV"].Select(e => ElmBased(e)).ToList()
                    },
                    new SubCategoryAnalysisDescriptor
                    {
                        Name = "LIG",
                        Description = "Ligand Binding Sites",
                        Motives = entries["LIG"].Select(e => ElmBased(e)).ToList()
                    },
                    new SubCategoryAnalysisDescriptor
                    {
                        Name = "MOD",
                        Description = "Post-translational Modification Sites",
                        Motives = entries["MOD"].Select(e => ElmBased(e)).ToList()
                    },
                    new SubCategoryAnalysisDescriptor
                    {
                        Name = "TRG",
                        Description = "Targeting Sites",
                        Motives = entries["TRG"].Select(e => ElmBased(e)).ToList()
                    },
                }
            };

            return new List<CategoryAnalysisDescriptor> { category };
        }

        static List<CategoryAnalysisDescriptor> GetActiveSiteCategories()
        {
            var category = new CategoryAnalysisDescriptor
            {
                Name = "Active Sites",
                Description = "Active sites.",
                SubCategories = new List<SubCategoryAnalysisDescriptor>
                {
                    new SubCategoryAnalysisDescriptor
                    {
                        Name = "Database",
                        Description = "Active sites from various databases.",
                        Motives = new List<MotiveAnalysisDescriptor>
                        {
                            QueryBased("CSA", "Entries from Catalytic Site Atlas", QueryBuilder.CSA(), _ => true)
                        }
                    },
                }
            };

            return new List<CategoryAnalysisDescriptor> { category };
        }
        
        static List<CategoryAnalysisDescriptor> GetSugarCategories()
        {
            var c5o = QueryBuilder.Rings("C", "C", "C", "C", "C", "O");
            var c4o = QueryBuilder.Rings("C", "C", "C", "C", "O");

            var category = new CategoryAnalysisDescriptor
            {
                Name = "Sugars",
                Description = "Sugar motifs.",
                SubCategories = new List<SubCategoryAnalysisDescriptor>
                {
                    new SubCategoryAnalysisDescriptor
                    {
                        Name = "Ring Based",
                        Description = "Sugar motifs that contain C5O or C4O ring.",
                        Motives = new List<MotiveAnalysisDescriptor>
                        {
                            QueryBased("C5O Rings", "C5O rings only", c5o.ConnectedResidues(0).Filter(q => (q.Count(c5o) > 0) & (q.Count(c4o) == 0)), s => s.Rings().ContainsFingerprint("CCCCCO")),
                            //QueryBased("C4O Rings", "C4O rings only", c4o.ConnectedResidues(0).Filter(q => (q.Count(c5o) == 0) & (q.Count(c4o) > 0)), s => s.Rings().ContainsFingerprint("CCCCO")),
                            QueryBased("C4O and C5O Rings", "Both C4O and C5O rings", QueryBuilder.Or(c4o, c5o).ConnectedResidues(0).Filter(q => (q.Count(c5o) > 0) & (q.Count(c4o) > 0)), s => s.Rings().ContainsFingerprint("CCCCO") && s.Rings().ContainsFingerprint("CCCCCO")),
                        }
                    },
                    new SubCategoryAnalysisDescriptor
                    {
                        Name = "Residue Based",
                        Description = "Using residue names from LigandExpo.",
                        Motives = //new string[] { "NAG","MAN","GLC","BMA","BGC","NDG","GAL","FUC","BOG","XYP","SIA","GLA","LMU","MAL","SUC","LMT","BCD","DMU","XYS","SGN" }.Select(n => ResidueBased(n)).ToList()
                            new string[] { "NAG","MAN","FAD","ADP","BMA","NAD","GLC","NAP","ATP","GAL","BGC","FUC","GDP","NDG","BOG","ANP","SAH","NDP","AMP","COA","GTP","XYP","SIA","LMT","UDP","SAM","GLA","PSU","GNP","SUC","UMP","ACO","FUL","5CM","DMU","BNG","MAL","BRU","IDS","H2U","DOC","ADN","NAI","CVM","XYS","5MC","A2G","NGA","TTP","SGN","DTP","CMP","MGD","U5P","5GP","5MU","LMG","3DR","DGD","ACP","APC","RAM","MMA","UPG","8OG","AGS","OMG","CTP","5BU","HDD","FRU","FBP","IMP","OMC","B12","THP","TMP","AHR","TYD","DGT","DCP","LMU","A3P","ADA","APR","F6P","5IU","2MG","C5P" }
                            .Select(n => ResidueBased(n)).ToList()
                    }
                }
            };

            return new List<CategoryAnalysisDescriptor> { category };
        }

        static List<CategoryAnalysisDescriptor> GetMetalCategories()
        {
            var category = new CategoryAnalysisDescriptor
            {
                Name = "Atoms",
                Description = "Atom based motifs.",
                SubCategories = new List<SubCategoryAnalysisDescriptor>
                {
                    new SubCategoryAnalysisDescriptor
                    {
                        Name = "Metals",
                        Description = "Metal atoms",
                        Motives = ElementAndBondInfo.MetalAtomsList.Select(e => AtomBased(e)).ToList()
                    }
                }
            };

            return new List<CategoryAnalysisDescriptor> { category };
        }

        static void Normalize(AtlasAnalysisDescriptor atlas)
        {
            atlas.Categories = atlas.Categories
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var c in atlas.Categories)
            {
                c.Atlas = atlas;
                c.Id = new string(c.Name.Replace(' ', '_').ToLowerInvariant().Where(x => EntityId.IsLegalChar(x)).ToArray());
                c.SubCategories = c.SubCategories.OrderBy(s => s.Name).ToList();
                foreach (var s in c.SubCategories)
                {
                    s.Category = c;
                    s.Id = new string(s.Name.Replace(' ', '_').ToLowerInvariant().Where(x => EntityId.IsLegalChar(x)).ToArray());
                    s.Motives = s.Motives.OrderBy(m => m.Name)/*.Take(2)*/.ToList(); // REMOVE TAKE 2 -- testing purposes only.
                    foreach (var m in s.Motives)
                    {
                        m.SubCategory = s;
                        m.Id = new string(m.Name.Replace(' ', '_').ToLowerInvariant().Where(x => EntityId.IsLegalChar(x)).ToArray());
                    }
                }
            }
        }

        public static AtlasAnalysisDescriptor GetAtlasDescriptor(string databaseName)
        {
            var ret = new AtlasAnalysisDescriptor
            {
                DatabaseName = databaseName,
                Categories = new[]
                {
                    //GetRegularMotiveCategories(),
                    GetActiveSiteCategories(),
                    GetSugarCategories(),
                    GetMetalCategories()
                }.SelectMany(c => c).ToList()
            };

            Normalize(ret);

            return ret;
        }
    }
}
