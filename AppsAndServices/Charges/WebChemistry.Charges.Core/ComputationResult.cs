namespace WebChemistry.Charges.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using System.IO;

    /// <summary>
    /// Computation result state.
    /// </summary>
    public enum ChargeResultState
    {
        Ok,
        Warning,
        Error
    }

    /// <summary>
    /// Charge value with additional info.
    /// </summary>
    public class ChargeValue
    {
        public bool IsReference { get; set; }
        public EemParameterSet.ParameterGroup Parameters { get; set; }
        public int Multiplicity { get; set; }
        public double Charge { get; set; }

        internal void CorrectCharge(double newValue)
        {
            Charge = newValue;
        }

        public ChargeValue()
        {
            IsReference = false;
        }
    }

    /// <summary>
    /// The result of the computation
    /// </summary>
    public class ChargeComputationResult
    {
        /// <summary>
        /// Shortcut for Parameters.Id
        /// </summary>
        public string Id { get { return Parameters.Id; } }

        public TimeSpan Timing { get; set; }
        public DateTime TimeCreatedUtc { get; set; }
        public ChargeResultState State { get; set; }
        public string[] Messages { get; set; }
        public EemChargeComputationParameters Parameters { get; set; }
        public IDictionary<IAtom, ChargeValue> Charges { get; private set; }
        public double ComputedTotalCharge { get; private set; }
        public IDictionary<IAtom, int> Multiplicities { get; set; }

        string _message;
        public string Message 
        { 
            get
            {
                if (_message != null) return Message;
                _message = string.Join(Environment.NewLine, Messages);
                return _message;
            }
        }

        static double ComputeTotalCharge(Dictionary<IAtom, ChargeValue> charges)
        {
            return charges.Sum(c => c.Value.Charge);
        }

        static Dictionary<IAtom, ChargeValue> ReadReferenceCharges(string filename, IStructure structure, Func<Stream> streamProvider)
        {
            var log = LogService.Default;
            if (filename.EndsWith(".pqr", StringComparison.OrdinalIgnoreCase))
            {
                var rcr = StructureReader.Read(filename, streamProvider);
                var rc = rcr.Structure;

                Dictionary<IAtom, ChargeValue> ret = new Dictionary<IAtom, ChargeValue>();

                foreach (var a in rc.Atoms)
                {
                    IAtom b;
                    if (structure.Atoms.TryGetAtom(a.Id, out b))
                    {
                        ret.Add(b, new ChargeValue { IsReference = true, Charge = a.PqrCharge() });
                    }
                }

                return ret;
            }
            else if (filename.EndsWith(".mol2", StringComparison.OrdinalIgnoreCase))
            {
                var rcr = StructureReader.Read(filename, streamProvider);
                var rc = rcr.Structure;
                if (!rc.Mol2ContainsCharges())
                {
                    throw new InvalidOperationException(string.Format("The file '{0}' does not contain information about charges.", filename));
                }

                if (rc.Atoms.Count != structure.Atoms.Count)
                {
                    throw new InvalidOperationException(string.Format("'{0}': Atom counts do not match. Expected {1}, got {2}.", filename, structure.Atoms.Count, rc.Atoms.Count));
                }

                Dictionary<IAtom, ChargeValue> ret = new Dictionary<IAtom, ChargeValue>();

                for (int i = 0; i < rc.Atoms.Count; i++)
                {
                    var a = structure.Atoms[i];
                    var c = rc.Atoms[i];

                    if (a.ElementSymbol != c.ElementSymbol)
                    {
                        throw new InvalidOperationException(string.Format("'{0}': Atom element symbols do not match. Atom Id {1}, expected {1}, got {2}.", filename, a.Id, a.ElementSymbol, c.ElementSymbol));
                    }

                    ret.Add(a, new ChargeValue { Charge = c.Mol2PartialCharge(), IsReference = true });
                }

                return ret;
            }
            else if (filename.EndsWith(".wprop", StringComparison.OrdinalIgnoreCase))
            {
                AtomPropertiesBase props;
                using (var reader = new StreamReader(streamProvider()))
                {
                    props = AtomPropertiesEx.Read(reader);
                }
                var name = props.Name.EndsWith("_ref", StringComparison.OrdinalIgnoreCase) ? props.Name.Substring(0, props.Name.Length - 4) : props.Name;

                Dictionary<IAtom, ChargeValue> charges = new Dictionary<IAtom, ChargeValue>();

                foreach (var a in structure.Atoms)
                {
                    var value = props.TryGetValue(a);
                    if (value != null) charges.Add(a, new ChargeValue { IsReference = true, Charge = (double)value });
                }
                return charges;
            }
            else if (filename.EndsWith("chrg", StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new StreamReader(streamProvider()))
                {
                    reader.ReadLine();
                    int count = int.Parse(reader.ReadLine());

                    if (count != structure.Atoms.Count)
                    {
                        throw new InvalidOperationException(string.Format("Could not add charges '{0}' to structure '{1}': the atom counts do not match (got {2}, expected {3}).", 
                            filename, structure.Id, count, structure.Atoms.Count));
                    }

                    var set = EemParameterSet.ReferenceSet(filename);
                    Dictionary<IAtom, ChargeValue> charges = new Dictionary<IAtom, ChargeValue>(count);

                    List<Tuple<int, ElementSymbol, double>> records = new List<Tuple<int, ElementSymbol, double>>();

                    var sep = " ".ToCharArray();
                    for (int i = 0; i < count; i++)
                    {
                        var line = reader.ReadLine();
                        var fields = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length == 3) records.Add(Tuple.Create(int.Parse(fields[0]), ElementSymbol.Create(fields[1]), fields[2].ToDouble().GetValue()));
                    }

                    bool error = false;
                    int errorIndex = 0;
                    for (int i = 0; i < count; i++)
                    {
                        var entry = records[i];

                        if (structure.Atoms[i].ElementSymbol != entry.Item2)
                        {
                            error = true;
                            errorIndex = i;
                            break;
                        }

                        charges[structure.Atoms[i]] = new ChargeValue
                        {
                            IsReference = true,
                            Charge = entry.Item3
                        };
                    }

                    if (error)
                    {
                        throw new InvalidOperationException(string.Format("Could not add charges '{0}' to structure '{1}': error at line {2}.", filename, structure.Id, errorIndex + 3));
                    }

                    return charges;
                }
            }

            throw new InvalidOperationException(string.Format("'{0}' is not a valid reference charge source.", filename));
        }

        /// <summary>
        /// load reference charges.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="readerProvider"></param>
        /// <returns></returns>
        public static ChargeComputationResult FromReference(string filename, IStructure structure, Func<Stream> streamProvider)
        {
            var set = EemParameterSet.ReferenceSet(filename);
            var charges = ReadReferenceCharges(filename, structure, streamProvider);
            return new ChargeComputationResult(charges)
            {
                State = ChargeResultState.Ok,
                Parameters = new EemChargeComputationParameters
                {
                    Method = ChargeComputationMethod.Reference,
                    Structure = structure,
                    Set = set
                }
            };
        }

        /// <summary>
        /// load reference charges from a property
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="type">For example "pqr"</param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static ChargeComputationResult FromProperty(IStructure structure, string type, Func<IAtom, double?> selector)
        {
            var set = EemParameterSet.ReferenceSet(structure.Id + "_" + type);

            Dictionary<IAtom, ChargeValue> charges = new Dictionary<IAtom, ChargeValue>(structure.Atoms.Count);
            foreach (var a in structure.Atoms)
            {
                var p = selector(a);
                if (p.HasValue) charges.Add(a, new ChargeValue { IsReference = true, Charge = p.Value });
            }

            return new ChargeComputationResult(charges)
            {
                State = ChargeResultState.Ok,
                Parameters = new EemChargeComputationParameters
                {
                    Method = ChargeComputationMethod.Reference,
                    Structure = structure,
                    Set = set
                }
            };
        }
        public ChargeComputationResult(Dictionary<IAtom, ChargeValue> charges)
        {
            Charges = charges;
            ComputedTotalCharge = ComputeTotalCharge(charges);
            Multiplicities = new Dictionary<IAtom, int>();
            TimeCreatedUtc = DateTime.UtcNow;
            Messages = new string[0];
        }
    }
}