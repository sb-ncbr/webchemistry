namespace WebChemistry.Charges.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Core;

    /// <summary>
    /// EEM parameter set.
    /// </summary>
    public class EemParameterSet : InteractiveObject
    {
        public class Value
        {
            public int Multiplicity { get; set; }
            public double A { get; set; }
            public double B { get; set; }

            public Value(int multiplicity, double a, double b)
            {
                Multiplicity = multiplicity;
                A = a;
                B = b;
            }

            public Value()
            {
                    
            }
        }
        
        public class ParameterGroup
        {
            public string TargetQueryString { get; set; }
            public Query TargetQuery { get; set; }
            public int Priority { get; set; }
            public double Kappa { get; set; }
            public IDictionary<ElementSymbol, Value[]> Parameters { get; set; }

            public Value GetParam(IAtom atom, int multiplicity)
            {
                Value[] prms;
                if (!Parameters.TryGetValue(atom.ElementSymbol, out prms) || prms.Length == 0)
                {
                    return null;
                }

                Value prev = null;
                for (int i = 0; i < prms.Length; i++)
                {
                    var param = prms[i];
                    if (param.Multiplicity == multiplicity) return param;
                    prev = param;
                }
                return multiplicity == 0 ? prms[0] : prev;
            }

            public static ParameterGroup FromXml(XElement xml)
            {
                var ret = new ParameterGroup();

                ret.Kappa = double.Parse(xml.Attribute("Kappa").Value, System.Globalization.CultureInfo.InvariantCulture);
                var targetAttribute = xml.Attribute("Target");
                var priorityAttribute = xml.Attribute("Priority");

                if (targetAttribute != null) ret.TargetQueryString = targetAttribute.Value;
                else ret.TargetQueryString = "Atoms";

                if (priorityAttribute != null) ret.Priority = int.Parse(priorityAttribute.Value);
                else ret.Priority = 0;

                try
                {
                    ret.TargetQuery = Query.Parse(ret.TargetQueryString);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(string.Format("Error parsing taget query '{0}': {1}", ret.TargetQueryString, e.Message));
                }

                xml.Elements().ForEach(symbol =>
                {
                    ret.Parameters[ElementSymbol.Create(symbol.Attribute("Name").Value)] = symbol.Elements().Select(bond =>
                        new Value 
                        {
                            Multiplicity = int.Parse(bond.Attribute("Type").Value),
                            A = double.Parse(bond.Attribute("A").Value, System.Globalization.CultureInfo.InvariantCulture),
                            B = double.Parse(bond.Attribute("B").Value, System.Globalization.CultureInfo.InvariantCulture)
                        })
                        .OrderBy(v => v.Multiplicity)
                        .ToArray();
                });

                return ret;
            }
                        
            static string FormatCharge(double charge)
            {
                return charge.ToString("0.000000000000", System.Globalization.CultureInfo.InvariantCulture);
            }

            public XElement ToXml()
            {
                XElement parameters = new XElement("Parameters", 
                    new XAttribute("Target", TargetQueryString),
                    new XAttribute("Priority", Priority),
                    new XAttribute("Kappa", FormatCharge(Kappa)));
                
                Parameters
                    .OrderBy(p => p.Key.ToString())
                    .ForEach(p => parameters.Add(
                           new XElement("Element", new XAttribute("Name", p.Key),
                                p.Value.Select(b => new XElement( "Bond",
                                       new XAttribute("Type", b.Multiplicity),
                                       new XAttribute("A", FormatCharge(b.A)),
                                       new XAttribute("B", FormatCharge(b.B))
                                       )))));

                return parameters;
            }
            
            public ParameterGroup()
            {
                Parameters = new Dictionary<ElementSymbol, Value[]>();
            }
        }

        public ParameterGroup[] ParameterGroups { get; set; }

        public string Name { get; private set; }

        public List<Tuple<string, string>> Properties { get; private set; }

        public double KappaFactor { get; private set; }
        public double ABFactor { get; private set; }

        string target;
        public string Target
        {
            get 
            { 
                if (target != null) return target;
                var t = Properties.FirstOrDefault(p => p.Item1.Equals("Target", StringComparison.OrdinalIgnoreCase));
                if (t == null) target = "Unknown";
                else target = t.Item2;
                return target;
            }
        }

        string basisSet;
        public string BasisSet
        {
            get
            {
                if (basisSet != null) return basisSet;
                var t = Properties.FirstOrDefault(p => p.Item1.Equals("Basis Set", StringComparison.OrdinalIgnoreCase));
                if (t == null) basisSet = "Unknown";
                else basisSet = t.Item2;
                return basisSet;
            }
        }

        public static bool ParameterEqual(EemParameterSet a, EemParameterSet b)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            var other = obj as EemParameterSet;
            if (other == null) return false;
            return other.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        
        /// <summary>
        /// Create an empty set.
        /// </summary>
        public EemParameterSet()
        {
            Properties = new List<Tuple<string, string>>();
            ParameterGroups = new ParameterGroup[0];
            Name = "Unknown";
            ABFactor = 1.0;
            KappaFactor = 1.0;
        }

        /// <summary>
        /// Empty set with some dummy values.
        /// </summary>
        /// <returns></returns>
        public static EemParameterSet NewSet()
        {
            return new EemParameterSet
            {
                Name = "No Name",
                Properties = new List<Tuple<string, string>>
                {                
                    Tuple.Create("Author", "Not Specified"),
                    Tuple.Create("Publication", "Not Specified"),
                    Tuple.Create("Journal", "Not Specified"),
                    Tuple.Create("Year", "Not Specified"),
                    Tuple.Create("Url", "Not Specified"),
                    Tuple.Create("Target", "Not Specified"),
                    Tuple.Create("Publication", "Not Specified"),
                    Tuple.Create("Basis Set", "Not Specified"),
                    Tuple.Create("Population Analysis", "Not Specified"),
                    Tuple.Create("QM Method", "Not Specified"),
                    Tuple.Create("Original Units", "Not Specified"),
                    Tuple.Create("Training Set Size", "Not Specified"),
                    Tuple.Create("Data Source", "Not Specified"),
                    Tuple.Create("Priority", "Not Specified")
                },
                ParameterGroups = new ParameterGroup[]
                {
                    new ParameterGroup
                    {
                        TargetQueryString = "Atoms",
                        TargetQuery = Query.Parse("Atoms"),
                        Kappa = 0.123,
                        Parameters = new Dictionary<ElementSymbol, Value[]>
                        {
                            { ElementSymbols.H, new Value[] { new Value { Multiplicity = 1, A = 2.5, B = 0.2 } } }, 
                            { ElementSymbols.N, new Value[] { new Value { Multiplicity = 1, A = 2.5, B = 0.2 }, new Value { Multiplicity = 2, A = 2.5, B = 0.2 } } },
                            { ElementSymbols.C, new Value[] { new Value { Multiplicity = 1, A = 2.5, B = 0.2 }, new Value { Multiplicity = 2, A = 2.5, B = 0.2 } } },
                            { ElementSymbols.O, new Value[] { new Value { Multiplicity = 1, A = 2.5, B = 0.2 }, new Value { Multiplicity = 2, A = 2.5, B = 0.2 } } }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Empty set with some dummy values.
        /// </summary>
        /// <returns></returns>
        public static EemParameterSet ReferenceSet(string name)
        {
            var ret = new EemParameterSet();
            ret.Name = name;
            return ret;
        }

        /// <summary>
        /// Parse XML.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static EemParameterSet FromXml(XElement xml)
        {
            EemParameterSet set = new EemParameterSet();

            set.Name = xml.Attribute("Name").Value;
            xml.Element("Properties").Elements().ForEach(e => set.Properties.Add(Tuple.Create(e.Attribute("Name").Value, e.Value)));
            set.ParameterGroups = xml.Elements("Parameters").Select(x => ParameterGroup.FromXml(x)).OrderBy(x => x.Priority).ToArray();

            var units = xml.Element("UnitConversion");
            if (units != null)
            {
                set.ABFactor = double.Parse(units.Attribute("ABFactor").Value, System.Globalization.CultureInfo.InvariantCulture);
                set.KappaFactor = double.Parse(units.Attribute("KappaFactor").Value, System.Globalization.CultureInfo.InvariantCulture);
            }

            return set;
        }

        /// <summary>
        /// Convert the set to XML representation.
        /// </summary>
        /// <returns></returns>
        public XElement ToXml()
        {
            XElement root = new XElement("ParameterSet", new XAttribute("Name", Name));
            XElement properties = new XElement("Properties");

            Properties.ForEach(p => properties.Add(new XElement("Property", new XAttribute("Name", p.Item1), p.Item2)));

            root.Add(properties);
            root.Add(new XElement("UnitConversion",
                new XAttribute("KappaFactor", KappaFactor.ToString("0.000000000000", System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("ABFactor", ABFactor.ToString("0.000000000000", System.Globalization.CultureInfo.InvariantCulture))));
            ParameterGroups.ForEach(g => root.Add(g.ToXml()));

            return root;
        }
    }
}
