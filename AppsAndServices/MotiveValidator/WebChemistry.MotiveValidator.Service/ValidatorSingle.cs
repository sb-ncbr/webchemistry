namespace WebChemistry.MotiveValidator.Service
{
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.Services;

    partial class ValidatorService : ServiceBase<ValidatorService, MotiveValidatorConfig, MotiveValidatorStandaloneConfig, object>
    {
        void RunSingle()
        {
            UpdateProgress("Loading models...");
            Log("Loading models...");
            ReadModels();
            Log("Found {0} models.", Models.Count);

            if (Models.Count == 0)
            {
                LogError("General", "Could not load any models.");
                var result = new ValidationResult
                {
                    Errors = Errors
                };
                JsonHelper.WriteJsonFile(Path.Combine(ResultFolder, "result.json"), result);
                return;
            }

            UpdateProgress("Computing...");

            ExecuteSingle();
        }
        
        void ExecuteSingle()
        {
            UpdateProgress("Reading/identifying motifs...");
            Log("Reading/identifying motifs...");
            var entries = ReadMotives();
            Log("Found {0} motifs.", entries.Sum(e => e.Value.Length));

            var groups = entries.Select(e =>
                new GroupAnalysis
                {
                    Model = Models[e.Key],
                    Service = this,
                    Entries = e.Value
                })
                .ToArray();

            groups.ForEach(g => g.Run(maxParallelism: MaxDegreeOfParallelism));

            var analyzedGroups = groups.Where(g => g.IsAnalyzed).ToArray();
            var allEntries = analyzedGroups.SelectMany(g => g.AnalyzedEntries).ToArray();
            var results = allEntries.Select(r => r.Result).ToArray();

            UpdateProgress("Exporting results...");
            Log("Exporting results...");

            var result = MakeResult(analyzedGroups, allEntries);               
            var resultJson = result.ToJsonString();

            if (SummaryOnly)
            {
                File.WriteAllText(Path.Combine(ResultFolder, "index.json"), resultJson);
                return;
            }

            if (!IsStandalone)
            {
                JsonHelper.WriteJsonFile(Path.Combine(ResultFolder, "result.json"), result);

                foreach (var g in analyzedGroups)
                {
                    var folder = Path.Combine(ResultFolder, g.Model.Name);
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    ZipUtils.CreateZip(Path.Combine(ResultFolder, g.Model.Name, "notanalyzed.zip"), ctx =>
                    {
                        foreach (var entry in g.Entries.Where(e => !e.IsAnalyzed && e.Motive != null))
                        {
                            ctx.AddEntry(entry.Motive.Id + ".pdb", entry.Motive.ToPdbString());
                            ctx.AddEntry(entry.Motive.Id + ".json", entry.Motive.ToJsonString(prettyPrint: false));
                            //ctx.AddEntry(entry.Motive.Id + ".mol", entry.Motive.ToMolString());
                        }
                    });

                    ZipUtils.CreateZip(Path.Combine(ResultFolder, g.Model.Name, "notvalidated.zip"), ctx =>
                    {
                        foreach (var entry in g.AnalyzedEntries.Where(e => e.Result.State != ValidationResultEntryState.Validated))
                        {
                            ctx.AddEntry(entry.Motive.Id + ".pdb", entry.Motive.ToPdbString());
                            //ctx.AddEntry(entry.Motive.Id + ".mol", entry.Motive.ToMolString());
                            ctx.AddEntry(entry.Motive.Id + ".json", entry.Motive.ToJsonString(prettyPrint: false));
                        }
                    });

                    var validated = g.AnalyzedEntries.Where(e => e.Result.State == ValidationResultEntryState.Validated).ToList();


                    ZipUtils.CreateZip(Path.Combine(ResultFolder, g.Model.Name, "json.zip"), ctx =>
                    {
                        foreach (var entry in validated)
                        {
                            ctx.BeginEntry(Path.Combine(entry.Motive.Id + ".json"));
                            ctx.TextWriter.Write(CreateEntryJson(entry));
                            ctx.EndEntry();
                        }
                    });


                    //ZipUtils.CreateZip(Path.Combine(ResultFolder, g.Model.Name, "mols.zip"), ctx =>
                    //{
                    //    foreach (var entry in validated)
                    //    {
                    //        ctx.BeginEntry(Path.Combine(entry.Motive.Id + ".mol"));
                    //        ctx.TextWriter.Write(entry.Analysis.MolString);
                    //        ctx.EndEntry();
                    //    }
                    //});

                    ZipUtils.CreateZip(Path.Combine(ResultFolder, g.Model.Name, "matched.zip"), ctx =>
                    {
                        foreach (var entry in validated)
                        {
                            ctx.BeginEntry(Path.Combine(entry.Motive.Id + ".pdb"));
                            ctx.TextWriter.Write(entry.MatchedPdbString);
                            ctx.EndEntry();
                        }
                    });

                    ZipUtils.CreateZip(Path.Combine(ResultFolder, g.Model.Name, "motives.zip"), ctx =>
                    {
                        foreach (var entry in validated)
                        {
                            ctx.BeginEntry(Path.Combine(entry.Motive.Id + ".pdb"));
                            ctx.TextWriter.Write(entry.Analysis.MotivePdbString);
                            ctx.EndEntry();
                        }
                    });

                    //ZipUtils.CreateZip(Path.Combine(ResultFolder, g.Model.Name, "motivesmol.zip"), ctx =>
                    //{
                    //    foreach (var entry in validated)
                    //    {
                    //        ctx.BeginEntry(Path.Combine(entry.Motive.Id + ".mol"));
                    //        ctx.TextWriter.Write(entry.Analysis.MotiveMolString);
                    //        ctx.EndEntry();
                    //    }
                    //});
                }
            }

            using (var zip = ZipUtils.CreateZip(Path.Combine(ResultFolder, "result.zip")))
            {
                ExportSingleResult(new ExportInfo
                {
                    ResultZip = zip,
                    ResultFolder = ResultFolder,
                    ExportModelSeparately = !IsStandalone,
                    AllEntries = allEntries,
                    AnalyzedGroups = analyzedGroups,
                    Results = results,
                    ResultJson = resultJson,
                    GeneralErrors = Errors
                });
            }
        }
    }
}
