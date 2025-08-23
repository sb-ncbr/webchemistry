// -----------------------------------------------------------------------
// <copyright file="Benchmark.cs" company="">
// David Sehnal 2012
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Framework.Core
{
    using System;
    using System.Linq;
        
    /// <summary>
    /// Benchmarking helper.
    /// </summary>
    public static class Benchmark
    {
        static TimeSpan RunOnce(Action a)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.GetTotalMemory(true);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            a();
            sw.Stop();
            return sw.Elapsed;
        }

        /// <summary>
        /// Runs a timing/memory benchmark of an Action.
        /// </summary>
        /// <param name="a">The action to time.</param>
        /// <param name="report">Write the result out to the Console?</param>
        /// <param name="timesToRun">Number of times to run the computation.</param>
        /// <param name="measureMemory">Measure the memory used? "Ignores" timesToRun.</param>
        /// <param name="name">Name of the computation.</param>
        /// <param name="runToJIT">Run the computation once before measuring?</param>
        /// <returns>The time of the computation averaged over running the computation timesToRun times and the heap size difference in bytes.</returns>
        public static Tuple<TimeSpan, long> Run(Action a, bool report = true, int timesToRun = 1, bool measureMemory = false, string name = "Computation", bool runToJIT = false)
        {
            long memory = 0L;
            if (measureMemory) memory = GC.GetTotalMemory(true);

            if (runToJIT) a();
            if (timesToRun < 1) timesToRun = 1;

            var ticks = Enumerable.Range(0, timesToRun).Sum(_ => RunOnce(a).Ticks) / timesToRun;
            var result = TimeSpan.FromTicks(ticks);

            if (measureMemory) memory = GC.GetTotalMemory(true) - memory;

            if (report)
            {
                string format = "{0}: {1}ms";

                if (timesToRun > 1) format += ", avg. of {2} runs";

                string memoryUsedStr = null;

                if (measureMemory)
                {
                    if (memory > 1024 * 1024)
                    {
                        format += ", {3}MB";
                        memoryUsedStr = (memory / 1024.0 / 1024.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (memory > 1024)
                    {
                        format += ", {3}KB";
                        memoryUsedStr = (memory / 1024.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        format += ", {3}B";
                        memoryUsedStr = memory.ToString();
                    }
                }

                format += ".";

                Console.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, format, name, result.TotalMilliseconds, timesToRun, memoryUsedStr));
            }

            return Tuple.Create(result, memory);
        }
    }
}
