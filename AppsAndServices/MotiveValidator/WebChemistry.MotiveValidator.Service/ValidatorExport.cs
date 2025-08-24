namespace WebChemistry.MotiveValidator.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.Platform.Services;

    partial class ValidatorService : ServiceBase<ValidatorService, MotiveValidatorConfig, MotiveValidatorStandaloneConfig, object>
    {
        class ExportInfo
        {
            public WebChemistry.Platform.ZipUtils.ZipWrapper ResultZip;

            /// <summary>
            /// Used in DB mode.
            /// </summary>
            public bool GeneratePerModelData;

            /// <summary>
            /// This has to be null/empty for nondb mode.
            /// </summary>
            public string ZipFolder;

            /// <summary>
            /// Used by svc mode.
            /// </summary>
            public string ResultFolder;

            /// <summary>
            /// used by svc mode.
            /// </summary>
            public bool ExportModelSeparately;

            public MotiveEntry[] AllEntries;
            public GroupAnalysis[] AnalyzedGroups;
            public ValidationResultEntry[] Results;
            public string ResultJson;
            public Dictionary<string, string> GeneralErrors;

            public string GetEntryPath(params string[] xs)
            {
                if (string.IsNullOrEmpty(ZipFolder)) return Path.Combine(xs);
                return Path.Combine(ZipFolder, Path.Combine(xs));
            }
        }
                
        static string CreateEntryJson(MotiveEntry entry)
        {
            var sb = new System.Text.StringBuilder();

            Action<string, IEnumerable<IAtom>, bool> appendArray = (n, xs, t) =>
            {
                sb.Append("  \""); sb.Append(n); sb.Append("\": [");
                sb.Append(xs.Select(a => a.Id).OrderBy(i => i).JoinBy(", "));
                sb.Append("]");
                if (t) sb.Append(",");
                sb.Append(Environment.NewLine);
            };

            sb.AppendLine("{");
            sb.Append("  \"Input\": "); sb.Append(entry.Motive.ToJsonString(prettyPrint: false)); sb.Append(","); sb.Append(Environment.NewLine);
            sb.Append("  \"Validated\": "); sb.Append(entry.Matched.ToJsonString(prettyPrint: false)); sb.Append(","); sb.Append(Environment.NewLine);
            appendArray("ChiralityError", entry.Analysis.DifferentChirality, true);
            appendArray("Foreign", entry.Analysis.ForeignAtoms.Values, true);
            appendArray("Substitutions", entry.Analysis.Substitutions.Values, true);
            appendArray("NamingMismatch", entry.NamingAnalysis.HasNamingIssue, true);
            appendArray("NamingMismatch_Equiv", entry.NamingAnalysis.HasNamingIssueWithEquivalence, true);
            appendArray("NamingMismatch_EquivIgnoreBondType", entry.NamingAnalysis.HasNamingIssueWithEquivalenceIgnoreBonds, true);
            appendArray("NamingMismatch_NonEquiv", entry.NamingAnalysis.HasNamingIssueWith_OUT_Equivalence, true);
            appendArray("NamingMismatch_NonEquivIgnoreBondType", entry.NamingAnalysis.HasNamingIssueWith_OUT_EquivalenceIgnoreBonds, false);
            sb.AppendLine("}");
            return sb.ToString();
        }

        static void ExportSingleResult(ExportInfo info)
        {
            var zip = info.ResultZip;

            var csv = MotiveEntry.GetResultExporter(info.Results, info.AnalyzedGroups.ToDictionary(g => g.Model.Name, g => g.Model));
            zip.Context.BeginEntry(info.GetEntryPath("result.csv"));
            csv.WriteCsvString(zip.Context.TextWriter);
            zip.Context.EndEntry();
            zip.AddEntry(info.GetEntryPath("general_errors.csv"), ErrorsToCsv(info.GeneralErrors));
            zip.AddEntry(info.GetEntryPath("result.json"), info.ResultJson);

            foreach (var g in info.AnalyzedGroups)
            {
                zip.AddEntry(info.GetEntryPath(g.Model.Name, "errors.csv"), ErrorsToCsv(g.Model.MotiveErrors));
                zip.AddEntry(info.GetEntryPath(g.Model.Name, "warnings.csv"), WarningsToCsv(g.Model.MotiveWarnings));

                foreach (var entry in g.Entries.Where(e => !e.IsAnalyzed && e.Motive != null))
                {
                    zip.AddEntry(info.GetEntryPath(g.Model.Name, "notanalyzed", "pdb", entry.Motive.Id + ".pdb"), entry.Motive.ToPdbString());
                    //zip.AddEntry(info.GetEntryPath(g.Model.Name, "notanalyzed", "mol", entry.Motive.Id + ".mol"), entry.Motive.ToMolString());
                }

                string modelFilename = g.Model.Name + "_model";
                //zip.AddEntry(info.GetEntryPath(g.Model.Name, modelFilename + ".mol"), g.Model.MolString);
                //if (info.ExportModelSeparately) File.WriteAllText(Path.Combine(info.ResultFolder, g.Model.Name, modelFilename + ".mol"), g.Model.MolString);

                var json = g.Model.Structure.ToJsonString(prettyPrint: false);
                zip.AddEntry(info.GetEntryPath(g.Model.Name, modelFilename + ".json"), json);
                if (info.ExportModelSeparately) File.WriteAllText(Path.Combine(info.ResultFolder, g.Model.Name, modelFilename + ".json"), json);

                zip.AddEntry(info.GetEntryPath(g.Model.Name, modelFilename + ".pdb"), g.Model.PdbString);
                if (info.ExportModelSeparately) File.WriteAllText(Path.Combine(info.ResultFolder, g.Model.Name, modelFilename + ".pdb"), g.Model.PdbString);
                
                zip.AddEntry(info.GetEntryPath(g.Model.Name, "pairing.csv"), g.PairingMatrix);
                zip.AddEntry(info.GetEntryPath(g.Model.Name, "summary.json"), g.Summary.ToJsonString());
            }

            foreach (var entry in info.AllEntries)
            {
                if (entry.Result.State == ValidationResultEntryState.Validated)
                {
                    zip.AddEntry(info.GetEntryPath(entry.Model.Name, "motifs", entry.Motive.Id + ".pdb"), entry.Analysis.MotivePdbString);
                    zip.AddEntry(info.GetEntryPath(entry.Model.Name, "matched", entry.Motive.Id + ".pdb"), entry.MatchedPdbString);
                    zip.AddEntry(info.GetEntryPath(entry.Model.Name, "matched", entry.Motive.Id + "_model.pdb"), entry.ModelPdbString);
                    //zip.AddEntry(info.GetEntryPath(entry.Model.Name, "mols", entry.Motive.Id + ".mol"), entry.Analysis.MolString);
                    //zip.AddEntry(info.GetEntryPath(entry.Model.Name, "motifmols", entry.Motive.Id + ".mol"), entry.Analysis.MotiveMolString);                   
                    //zip.AddEntry(info.GetEntryPath(entry.Model.Name, "json", entry.Motive.Id + ".json"), CreateEntryJson(entry));
                }
                else
                {
                    zip.AddEntry(info.GetEntryPath(entry.Model.Name, "notvalidated", "pdb", entry.Motive.Id + ".pdb"), entry.Motive.ToPdbString());
                    //zip.AddEntry(info.GetEntryPath(entry.Model.Name, "notvalidated", "mol", entry.Motive.Id + ".mol"), entry.Motive.ToMolString());
                }
            }
        }

        static void ExportDatabaseResult(ExportInfo info)
        {
            var zip = info.ResultZip;

            var csv = MotiveEntry.GetResultExporter(info.Results, info.AnalyzedGroups.ToDictionary(g => g.Model.Name, g => g.Model));
            zip.AddEntry(info.GetEntryPath("result.json"), info.ResultJson);

            foreach (var g in info.AnalyzedGroups)
            {
                foreach (var entry in g.Entries.Where(e => !e.IsAnalyzed && e.Motive != null))
                {
                    zip.AddEntry(Path.Combine("structures", "notanalyzed", "pdb", entry.Motive.Id + ".pdb"), entry.Motive.ToPdbString());
                    zip.AddEntry(Path.Combine("structures", "notanalyzed", "json", entry.Motive.Id + ".json"), entry.Motive.ToJsonString(prettyPrint: false));
                }
            }

            foreach (var entry in info.AllEntries)
            {
                if (entry.Result.State == ValidationResultEntryState.Validated)
                {
                    zip.AddEntry(Path.Combine("structures", "motifs", entry.Motive.Id + ".pdb"), entry.Analysis.MotivePdbString);
                    zip.AddEntry(Path.Combine("structures", "matched", entry.Motive.Id + ".pdb"), entry.MatchedPdbString);
                    //zip.AddEntry(Path.Combine("structures", "matched", entry.Motive.Id + "_model.pdb"), entry.ModelPdbString);
                    //zip.AddEntry(Path.Combine("structures", "mols", entry.Motive.Id + ".mol"), entry.Analysis.MolString);
                    //zip.AddEntry(Path.Combine("structures", "motifmols", entry.Motive.Id + ".mol"), entry.Analysis.MotiveMolString);

                    zip.AddEntry(Path.Combine("structures", "json", entry.Motive.Id + ".json"), CreateEntryJson(entry));
                }
                else
                {
                    zip.AddEntry(Path.Combine("structures", "notvalidated", "pdb", entry.Motive.Id + ".pdb"), entry.Motive.ToPdbString());
                    zip.AddEntry(Path.Combine("structures", "notvalidated", "json", entry.Motive.Id + ".json"), entry.Motive.ToJsonString(prettyPrint: false));
                }
            }
        }

        void ExportAggregateResult(WebChemistry.Platform.ZipUtils.ZipWrapper zip, string aggregateName, DatabaseAggregator.ModelWrapper model)
        {
            var name = model.Model.Name;
            var result = model.MakeResults(GetVersion());
            zip.AddEntry(Path.Combine(aggregateName, name, "result.json"), result.ToJsonString());            
            
            //zip.AddEntry(Path.Combine(aggregateName, name, "summary.json"), model.Summary.ToJsonString());

            string modelFilename = name + "_model";
            //zip.AddEntry(Path.Combine("structures", "models", modelFilename + ".mol"), model.Model.MolString);
            zip.AddEntry(Path.Combine("structures", "models", modelFilename + ".json"), model.Model.Structure.ToJsonString(prettyPrint: false));
            zip.AddEntry(Path.Combine("structures", "models", modelFilename + ".pdb"), model.Model.PdbString);
        }
    }
}
