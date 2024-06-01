using OpenBabel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace WebChemistry.MotiveValidator.Converter
{
    struct BondIdentifier : IEquatable<BondIdentifier>
    {
        private readonly long Id;

        /// <summary>
        /// Returns the hash code computed as a hash of a 64bit id.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            long key = Id;
            key = (~key) + (key << 18); // key = (key << 18) - key - 1;
            key = key ^ (key >> 31);
            key = key * 21; // key = (key + (key << 2)) + (key << 4);
            key = key ^ (key >> 11);
            key = key + (key << 6);
            key = key ^ (key >> 22);
            return (int)key;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is BondIdentifier)
            {
                return this.Id == ((BondIdentifier)obj).Id;
            } 
            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(BondIdentifier other)
        {
            return this.Id == other.Id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(BondIdentifier a, BondIdentifier b)
        {
            return a.Id == b.Id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(BondIdentifier a, BondIdentifier b)
        {
            return a.Id != b.Id;
        }

        /// <summary>
        /// Creates the identifier.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public BondIdentifier(int a, int b)
        {
            long i = a;
            long j = b;

            if (i > j) Id = (j << 32) | i;
            else Id = (i << 32) | j;
        }
    }

    class Program
    {
        static int ParseInt(string str, int start, int count)
        {
            int val = 0;
            bool neg = false;
            bool trailing = true;
            int end = start + count;
            for (int i = start; i < end; i++)
            {
                var c = str[i];
                if (char.IsDigit(c))
                {
                    val = val * 10 + (c - '0');
                    trailing = false;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (trailing) continue;
                    break;
                }
                else if (c == '-') neg = true;
                else break;
            }
            return neg ? -val : val;
        }

        static void SetOptions(bool customBonds)
        {
            if (customBonds)
            {
                BabelConverter.AddOption("a", OBConversion.Option_type.INOPTIONS);
                BabelConverter.AddOption("b", OBConversion.Option_type.INOPTIONS);
                BabelConverter.AddOption("c", OBConversion.Option_type.INOPTIONS);
                BabelConverter.AddOption("b", OBConversion.Option_type.OUTOPTIONS);                
            }
            else
            {
                BabelConverter.AddOption("c", OBConversion.Option_type.INOPTIONS);
                BabelConverter.RemoveOption("a", OBConversion.Option_type.INOPTIONS);
                BabelConverter.RemoveOption("b", OBConversion.Option_type.INOPTIONS);
                BabelConverter.RemoveOption("b", OBConversion.Option_type.OUTOPTIONS);
            }
        }

        static void AddBond(string bondsString, int start, int end, OBMol molecule, HashSet<BondIdentifier> knownBonds)
        {
            var fst = bondsString.IndexOf(' ', start);
            var snd = bondsString.LastIndexOf(' ', end);

            var atom1 = ParseInt(bondsString, start, fst - start);
            var atom2 = ParseInt(bondsString, fst + 1, snd - fst - 1);
            var type = ParseInt(bondsString, snd + 1, end - snd);
            
            if (knownBonds.Add(new BondIdentifier(atom1, atom2))) molecule.AddBond(atom1, atom2, type);
            //Console.WriteLine("{0} {1} {2}", atom1, atom2, type);
        }

        static void AddBonds(string bondsString, OBMol molecule)
        {
            if (string.IsNullOrWhiteSpace(bondsString)) return;

            HashSet<BondIdentifier> knownBonds = new HashSet<BondIdentifier>(molecule.Bonds().Select(b => new BondIdentifier((int)b.GetBeginAtomIdx() - 1, (int)b.GetEndAtomIdx() - 1)));

            int start = 0;
            int index = 0;
            int maxIndex = bondsString.Length - 1;
            while ((index = bondsString.IndexOf(',', Math.Min(index + 1, maxIndex))) >= 0)
            {
                AddBond(bondsString, start, index - 1, molecule, knownBonds);
                if (index == maxIndex) break;
                start = index + 1;
            }
        }

        class ParentInfo
        {
            public int Id { get; set; }
            public string StartedUtc { get; set; }
        }

        static ParentInfo PInfo;
        static OBConversion BabelConverter = new OBConversion();

        static void CheckParentRunning(object state)
        {
            bool running = false;
            try
            {
                var p = Process.GetProcessById(PInfo.Id);
                if (p.StartTime.ToUniversalTime().ToString(System.Globalization.CultureInfo.InvariantCulture).Equals(PInfo.StartedUtc, StringComparison.Ordinal))
                {
                    running = true;
                }
            }
            catch
            {
            }

            if (!running)
            {
                Environment.Exit(0);
            }
        }

        static void Init(string[] args)
        {
            BabelConverter = new OBConversion();
            BabelConverter.SetInFormat("pdb");
            BabelConverter.SetOutFormat("mol");
            
            PInfo = new ParentInfo
            {
                Id = int.Parse(args[0]),
                StartedUtc = args[1]
            };

            new System.Threading.Timer(CheckParentRunning, null, 2500, 2500);
        }

        static StreamWriter Output;
        static StreamReader Input;

        static void HandleCommand()
        {
            while (true)
            {
                var command = Input.ReadLine();
                if (command == null)
                {
                    CheckParentRunning(null);
                    continue;
                }
                if (command.StartsWith("CONVERT", StringComparison.OrdinalIgnoreCase))
                {
                    Convert(false);
                }
                else if (command.StartsWith("CUSTOMCONVERT", StringComparison.OrdinalIgnoreCase))
                {
                    Convert(true);
                }
                else if (command.StartsWith("KILL", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }

        static void Convert(bool customBonds)
        {
            try
            {
                SetOptions(customBonds);

                var bondsString = Input.ReadLine();
                var src = new StringBuilder();
                string line;
                while (!(line = Input.ReadLine()).Equals("ENDINPUT", StringComparison.Ordinal))
                {
                    src.AppendLine(line);
                }

                var mol = new OBMol();
                BabelConverter.ReadString(mol, src.ToString());
                AddBonds(bondsString, mol);
                Output.WriteLine(BabelConverter.WriteString(mol));
                Output.WriteLine("ENDCONVERT");
            }
            catch (Exception e)
            {
                Output.WriteLine("Error:" + e.Message);
                Output.WriteLine("ENDCONVERT");
            }
            finally
            {
                Output.Flush();
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Input = new StreamReader(Console.OpenStandardInput());
                Output = new StreamWriter(Console.OpenStandardOutput(32 * 1024));
                Output.AutoFlush = false;

                Init(args);
                HandleCommand();
            }
            catch (Exception e)
            {
                //Log("Error: " + e.Message);
            }
            finally
            {
                //if (Output != null) Output.Dispose();
                //if (Input != null) Input.Dispose();
            }
        }

        //static void Log(string msg)
        //{
        //    File.AppendAllText("i:/converter.txt", string.Format("[{0}] {1}", DateTime.Now, msg + Environment.NewLine));
        //}
    }
}
