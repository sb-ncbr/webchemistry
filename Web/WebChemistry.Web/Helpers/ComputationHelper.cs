using System;
using System.Collections.Generic;
using System.Linq;
using WebChemistry.Platform.Computation;
using WebChemistry.Web.Models;
using WebChemistry.Framework.Core;

namespace WebChemistry.Web.Helpers
{
    public static class ComputationHelper
    {
        public static ComputationModel[] GetModels(IEnumerable<ComputationInfo> cmps)
        {
            return cmps.Select(c => new ComputationModel { Info = c, Status = c.GetStatus(), IsRunning = c.IsRunning() }).ToArray();
        }

        public static string GetElapsedTimeString(ComputationStatus status)
        {
            var elapsed = status.FinishedTime - status.StartedTime;
            return GetTimeString(elapsed);
        }

        public static string GetTimeString(TimeSpan elapsed)
        {
            return 
                elapsed.Days > 0 
                ? elapsed.ToString(@"d\.hh\hmm\mss\s") 
                : elapsed.Hours > 0 
                ? elapsed.ToString(@"h\hmm\mss\s")
                : elapsed.Minutes > 0
                ? elapsed.ToString(@"m\mss\s")
                : elapsed.ToString(@"s\s");
        }

        public static string GetTimeString(TimeSpan? elapsed)
        {
            if (elapsed.HasValue) return GetTimeString(elapsed.Value);
            return "n/a";
        }

        public static string GetFileSizeString(long size)
        {
            return (size / (1024.0 * 1024)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " MB";
        }

        public static string GetResultSizeString(ComputationStatus status)
        {
            return GetFileSizeString(status.ResultSizeInBytes);
        }

        private static readonly long UnixEpochTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

        public static long ToJsonTicks(DateTime value)
        {
            return (value.ToUniversalTime().Ticks - UnixEpochTicks) / 10000;
        }

        public static long ToJsonTicks(DateTime? value)
        {
            if (value.HasValue) return (value.Value.ToUniversalTime().Ticks - UnixEpochTicks) / 10000;
            return 0;
        }

        public static object GetStatus(ComputationInfo comp)
        {
            var status = comp.GetStatus();

            string remaining = "n/a";

            if (!status.IsIndeterminate)
            {
                var done = (double)status.MaxProgress / status.CurrentProgress - 1.0;
                if (!double.IsNaN(done) && !double.IsInfinity(done))
                {
                    remaining = (from started in status.StartedTime.AsMaybe()
                                 select "approx. " + ComputationHelper.GetTimeString(TimeSpan.FromMilliseconds((DateTime.UtcNow - started).TotalMilliseconds * done)))
                                .ToString();

                }
            }

            return new
            {
                Exists = true,
                IsRunning = comp.IsRunning(),
                State = status.State.ToString(),
                Message = string.IsNullOrWhiteSpace(status.Message) ? "n/a" : status.Message,
                CreatedTicks = ComputationHelper.ToJsonTicks(comp.DateCreated),   //comp.DateCreated.Ticks .ToString() + " UTC",
                StartedTicks = ComputationHelper.ToJsonTicks(status.StartedTime), //status.StartedTime.ToString() + " UTC",
                Elapsed = ComputationHelper.GetElapsedTimeString(status),
                Remaining = remaining,
                IsIndeterminate = status.IsIndeterminate,
                CurrentProgress = status.CurrentProgress,
                MaxProgress = status.MaxProgress,
                CustomState = status.CustomState ?? "",
                ResultSize = ComputationHelper.GetResultSizeString(status)
            };
        }
    }
}