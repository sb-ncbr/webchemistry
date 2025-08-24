////namespace WebChemistry.MotiveValidator.Service
////{
////    using System;
////    using System.Collections.Concurrent;
////    using System.Collections.Generic;
////    using System.Diagnostics;
////    using System.IO;
////    using System.Linq;
////    using System.Text;
////    using System.Threading;
////    using WebChemistry.Framework.Core;

////    class StructureConverter : IDisposable
////    {
////        class ConverterWrapper
////        {
////            public bool IsAvailable { get; set; }

////            Process ConverterProcess;

////            public ConverterWrapper(string executablePath)
////            {
////                var parent = Process.GetCurrentProcess();
////                var pId = parent.Id;
////                var started = parent.StartTime.ToUniversalTime().ToString(System.Globalization.CultureInfo.InvariantCulture);
////                ConverterProcess = Process.Start(new ProcessStartInfo(executablePath, string.Format("{0} \"{1}\"", pId, started))
////                {
////                    RedirectStandardInput = true,
////                    RedirectStandardOutput = true,
////                    CreateNoWindow = true,
////                    WindowStyle = ProcessWindowStyle.Hidden,
////                    UseShellExecute = false
////                });
////                ConverterProcess.StandardInput.AutoFlush = false;
////                IsAvailable = true;
////            }

////            public string ConvertPdbToMol(string source, bool customBonds, string bondsString)
////            {
////                var input = ConverterProcess.StandardInput;
////                var output = ConverterProcess.StandardOutput;

////                if (customBonds) input.WriteLine("CUSTOMCONVERT");
////                else input.WriteLine("CONVERT");

////                input.WriteLine(bondsString);
////                input.WriteLine(source);
////                input.WriteLine("ENDINPUT");
////                input.Flush();

////                var retBuilder = new StringBuilder();
////                string line;
////                while (!(line = output.ReadLine()).Equals("ENDCONVERT", StringComparison.Ordinal))
////                {
////                    retBuilder.AppendLine(line);
////                }

////                var ret = retBuilder.ToString();

////                if (ret.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
////                {
////                    throw new InvalidOperationException("Error converting structure to MOL format: " + ret.Substring("Error:".Length));
////                }

////                return ret;
////            }

////            public void Kill()
////            {
////                ConverterProcess.StandardInput.WriteLine("KILL");
////                ConverterProcess.StandardInput.Flush();
////            }
////        }

////        int NumConverters = 8;

////        ConverterWrapper[] Converters;
////        Semaphore Pool;

////        public void Dispose()
////        {
////            foreach (var c in Converters)
////            {
////                try
////                {
////                    c.Kill();
////                }
////                catch { }
////            }

////            if (Pool != null)
////            {
////                Pool.Dispose();
////                Pool = null;
////            }
////        }

////        private void InitConverters()
////        {
////            var executablePath = Path.Combine((new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).Directory.FullName, "WebChemistry.MotiveValidator.Converter.exe");
////            Converters = Enumerable.Range(0, NumConverters).Select(_ => new ConverterWrapper(executablePath)).ToArray();
////            Pool = new Semaphore(NumConverters, NumConverters);
////        }

////        public StructureConverter(int numConverters)
////        {
////            NumConverters = numConverters;
////            InitConverters();
////        }

////        public string ConvertPdbToMol(string source, bool customBonds, string bondsString)
////        {
////            Pool.WaitOne();

////            try
////            {
////                ConverterWrapper converter = null;
////                lock (Converters)
////                {
////                    for (int i = 0; i < NumConverters; i++)
////                    {
////                        if (Converters[i].IsAvailable)
////                        {
////                            Converters[i].IsAvailable = false;
////                            converter = Converters[i];
////                            break;
////                        }
////                    }
////                }

////                var ret = converter.ConvertPdbToMol(source, customBonds, bondsString);

////                lock (Converters)
////                {
////                    converter.IsAvailable = true;
////                }

////                return ret;
////            }
////            finally
////            {
////                Pool.Release();
////            }
////        }

////        static BondType DefaultType(IBond b)
////        {
////            return b.Type;
////        }

////        public static string MakeBondsString(IStructure structure, Func<IBond, bool> filter, Func<IBond, BondType> type = null)
////        {
////            if (type == null) type = DefaultType;

////            var atomIndices = structure.Atoms.Select((a, i) => new { A = a, I = i }).ToDictionary(a => a.A, a => a.I);
////            StringBuilder bonds = new StringBuilder();
////            foreach (var b in structure.Bonds)
////            {
////                if (!filter(b)) continue;
////                bonds.AppendFormat("{0} {1} {2},", atomIndices[b.A] + 1, atomIndices[b.B] + 1, (int)type(b));
////            }
////            return bonds.ToString();
////        }
////    }

////    //class StructureConverter__
////    //{
////    //    string ExecutablePath;

////    //    static BondType DefaultType(IBond b)
////    //    {
////    //        return b.Type;
////    //    }

////    //    public static string MakeBondsString(IStructure structure, Func<IBond, bool> filter, Func<IBond, BondType> type = null)
////    //    {
////    //        if (type == null) type = DefaultType;

////    //        var atomIndices = structure.Atoms.Select((a, i) => new { A = a, I = i }).ToDictionary(a => a.A, a => a.I);
////    //        StringBuilder bonds = new StringBuilder();
////    //        foreach (var b in structure.Bonds)
////    //        {
////    //            if (!filter(b)) continue;
////    //            bonds.AppendFormat("{0} {1} {2},", atomIndices[b.A] + 1, atomIndices[b.B] + 1, (int)type(b));
////    //        }
////    //        return bonds.ToString();
////    //    }

////    //    public string ConvertPdbToMol(string source, bool customBonds, string bondsString)
////    //    {
////    //        var p = Process.Start(new ProcessStartInfo(ExecutablePath, customBonds ? "-custombonds" : "")
////    //        {
////    //            RedirectStandardInput = true,
////    //            RedirectStandardOutput = true,
////    //            CreateNoWindow = true,
////    //            WindowStyle = ProcessWindowStyle.Hidden,
////    //            UseShellExecute = false
////    //        });
////    //        p.StandardInput.WriteLine(bondsString);
////    //        p.StandardInput.Write(source);
////    //        p.StandardInput.Close();
////    //        var ret = p.StandardOutput.ReadToEnd();
////    //        p.WaitForExit();

////    //        if (ret.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
////    //        {
////    //            throw new InvalidOperationException("Error converting structure to MOL format: " + ret.Substring("Error:".Length));
////    //        }

////    //        return ret;
////    //    }

////    //    public StructureConverter__()
////    //    {
////    //        ExecutablePath = Path.Combine((new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).Directory.FullName, "WebChemistry.MotiveValidator.Converter.exe");
////    //    }
////    //}
////}
