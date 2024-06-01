namespace WebChemistry.Charges.Service.DataModel
{
    using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebChemistry.Platform;
using WebChemistry.Platform.Computation;
using WebChemistry.Platform.Server;
using WebChemistry.Platform.Users;

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ChangeComputationStates
    {
        New,
        Analyzed
    }

    public class ChangeComputationState
    {
        public ChangeComputationStates State { get; set; }
    }

    public class ChargeAnalysisState
    {
        public EntityId ChargeComputationId { get; set; }
    }

    public class ChargeComputationWrapper
    {
        public ComputationInfo AnalyzerComputation { get; set; }
        public string AnalyzerInputDirectory { get; set; }
        public ComputationInfo ChargeComputation { get; set; }
    }

    public static class ChargeCalculatorApp
    {
        /// <summary>
        /// Creates the comptutation.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ChargeComputationWrapper CreateComputation(UserInfo user, string source)
        {
            var analyzerService = ServerManager.MasterServer.Services.GetService("Charges.Analyzer");
            var chargesService = ServerManager.MasterServer.Services.GetService("Charges");

            var analysis = user.Computations.CreateComputation(user, analyzerService, "ChargesAnalyzerComputation", new ChargesAnalyzerConfig(), source /*, dependentObjects: new[] { repository } */);
            var charges = user.Computations.CreateComputation(user, chargesService, "ChargesComputation", new ChargesServiceConfig(), source,
                dependentObjects: new[] { analysis.Id }, customState: new ChangeComputationState { State = ChangeComputationStates.New });
            charges.MakeInputDirectory();
            analysis.SetCustomStateAndSave(new ChargeAnalysisState { ChargeComputationId = charges.Id });

            return new ChargeComputationWrapper
            {
                AnalyzerComputation = analysis,
                AnalyzerInputDirectory = analysis.MakeInputDirectory(),
                ChargeComputation = charges,
            };
        }

        /// <summary>
        /// Schedule the computation of the charges with updated config from the analyzer.
        /// </summary>
        /// <param name="chargesComputation"></param>
        /// <param name="config"></param>
        public static void UpdateConfig(ComputationInfo chargesComputation, ChargesServiceConfig config)
        {
            chargesComputation.UpdateSettings(config);
        }

        /// <summary>
        /// Updates the computation state.
        /// </summary>
        /// <param name="chargesComputation"></param>
        /// <param name="newState"></param>
        public static void UpdateState(ComputationInfo chargesComputation, ChangeComputationStates newState)
        {
            chargesComputation.SetCustomStateAndSave(new ChangeComputationState { State = newState });
        }

        /// <summary>
        /// Gets the input file
        /// </summary>
        /// <param name="chargesComputation"></param>
        /// <returns></returns>
        public static string GetInputZipFilename(ComputationInfo chargesComputation)
        {
            return Path.Combine(chargesComputation.GetInputDirectory(), "input.zip");
        }

        /// <summary>
        /// Returns the filename of the config file.
        /// </summary>
        /// <param name="chargesComputation"></param>
        /// <returns></returns>
        public static string GetConfigFilename(ComputationInfo chargesComputation)
        {
            return chargesComputation.GetSettingsPath();
        }

        /// <summary>
        /// Reads the analysis result.
        /// </summary>
        /// <param name="analysisComputation"></param>
        /// <returns></returns>
        public static string GetAnalysisDataString(ComputationInfo analysisComputation)
        {
            return File.ReadAllText(Path.Combine(analysisComputation.GetResultFolderId().GetEntityPath(), "result.json"));
        }

        public static string GetSummaryString(ComputationInfo chargeComputation)
        {
            return File.ReadAllText(Path.Combine(chargeComputation.GetResultFolderId().GetEntityPath(), "summary.json"));
        }
    }
}
