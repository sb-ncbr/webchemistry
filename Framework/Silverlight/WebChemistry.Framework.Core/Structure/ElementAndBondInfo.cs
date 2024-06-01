namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;

    public static partial class ElementAndBondInfo
    {
        public struct ElementPair : IEquatable<ElementPair>
        {
            public readonly ElementSymbol a, b;
            int hash;

            public bool Equals(ElementPair other)
            {
                return (a.Equals(other.a) && b.Equals(other.b)) || (a.Equals(other.b) && b.Equals(other.a));
            }

            public override int GetHashCode()
            {
                return hash;
            }

            public override string ToString()
            {
                return a.ToString() + " " + b.ToString();
            }

            public ElementPair(ElementSymbol a, ElementSymbol b)
            {
                this.a = a;
                this.b = b;

                long ah = a.GetHashCode();
                long bh = b.GetHashCode();

                long key = ah < bh ? (ah << 32) | bh : (bh << 32) | ah;
                key = (~key) + (key << 18); // key = (key << 18) - key - 1;
                key = key ^ (key >> 31);
                key = key * 21; // key = (key + (key << 2)) + (key << 4);
                key = key ^ (key >> 11);
                key = key + (key << 6);
                key = key ^ (key >> 22);

                this.hash = (int)key;


                //if (ah != bh) hash = ah ^ bh;
                //else if (ah < bh) hash = (ah << 16) | (bh & 0xFFFF);
                //else hash = (bh << 16) | (ah & 0xFFFF);
            }
        }

        private static double BondThresholdTolerance = 0.0;

        public class Threshold
        {
            public double Value { get; private set; }
            public BondType BondType { get; private set; }

            public Threshold(double value, BondType type)
            {
                this.Value = value;
                this.BondType = type;
            }
        }

        public class ElementInfo
        {
            public static readonly ElementInfo Default = new ElementInfo(8, 0, 0, 1.75, ElementColor.Default);

            public int Valency { get; private set; }
            public double CovalentRadius { get; private set; }
            public double AtomicRadius { get; private set; }
            public double VdwRadius { get; private set; }
            public ElementColor Color { get; internal set; }

            public double MaxBondingRadius { get; private set; }

            public ElementInfo(int valency, double covalentRadis, double atomicRadius, double vdwRadius, ElementColor color, double? maxBondingRadius = null)
            {
                this.Valency = valency;
                this.CovalentRadius = covalentRadis;
                this.AtomicRadius = atomicRadius;
                this.VdwRadius = vdwRadius;
                this.Color = color;
                this.MaxBondingRadius = maxBondingRadius ?? vdwRadius;
            }
        }

        static Dictionary<ElementPair, Threshold[]> pairsBondInfo = new Dictionary<ElementPair, Threshold[]>();
        static Dictionary<ElementSymbol, Threshold[]> elementBondInfo = new Dictionary<ElementSymbol, Threshold[]>();
        static internal Dictionary<ElementSymbol, ElementInfo> elementInfo = new Dictionary<ElementSymbol, ElementInfo>();

        public static Dictionary<ElementPair, Threshold[]> ElementPairInfo { get { return pairsBondInfo; } }
        public static Dictionary<ElementSymbol, Threshold[]> ElementThresholdInfos { get { return elementBondInfo; } }

        public static Tuple<int, double> GetValencyAndRadius(IAtom atom)
        {
            ElementInfo info;
            if (elementInfo.TryGetValue(atom.ElementSymbol, out info)) return Tuple.Create(info.Valency, info.MaxBondingRadius);
            return Tuple.Create(8, ElementInfo.Default.VdwRadius);
        }

        public static double GetVdwRadius(IAtom atom)
        {
            ElementInfo info;
            if (elementInfo.TryGetValue(atom.ElementSymbol, out info)) return info.VdwRadius;
            return ElementInfo.Default.VdwRadius;
        }

        public static ElementInfo GetElementInfo(ElementSymbol e)
        {
            ElementInfo info;
            if (elementInfo.TryGetValue(e, out info)) return info;
            return ElementInfo.Default;
        }

        public static ElementColor GetColor(ElementSymbol e)
        {
            ElementInfo info;
            if (elementInfo.TryGetValue(e, out info)) return info.Color;
            return ElementColor.Default;
        }

        static BondType? GetBondTypePair(double distance, ElementSymbol a, ElementSymbol b)
        {
            Threshold[] ts;
            if (pairsBondInfo.TryGetValue(new ElementPair(a, b), out ts))
            {
                for (int i = 0; i < ts.Length; i++)
                {
                    var t = ts[i];
                    if (distance / (1 + BondThresholdTolerance) < t.Value) return t.BondType;
                }
            }

            return null;
        }

        static BondType? GetBondTypeSingle(double distance, ElementSymbol a)
        {
            Threshold[] ts;
            if (elementBondInfo.TryGetValue(a, out ts))
            {
                for (int i = 0; i < ts.Length; i++)
                {
                    var t = ts[i];
                    if (distance / (1 + BondThresholdTolerance) < t.Value) return t.BondType;
                }
            }

            return null;
        }

        //static string PdbPrefix(IAtom a)
        //{
        //    return "Pdb:" + a.PdbName();
        //}

        static HashSet<ElementSymbol> metalAtoms = new HashSet<ElementSymbol>
        { 
            ElementSymbols.Li, ElementSymbols.Na, ElementSymbols.K, ElementSymbols.Rb, ElementSymbols.Cs, ElementSymbols.Fr, 
            ElementSymbols.Be, ElementSymbols.Mg, ElementSymbols.Ca, ElementSymbols.Sr, ElementSymbols.Ba, ElementSymbols.Ra, 
            ElementSymbols.Al, ElementSymbols.Ga, ElementSymbols.In, ElementSymbols.Sn, ElementSymbols.Tl, ElementSymbols.Pb, 
            ElementSymbols.Bi, ElementSymbols.Sc, ElementSymbols.Ti, ElementSymbols.V, ElementSymbols.Cr, ElementSymbols.Mn, 
            ElementSymbols.Fe, ElementSymbols.Co, ElementSymbols.Ni, ElementSymbols.Cu, ElementSymbols.Zn, ElementSymbols.Y, 
            ElementSymbols.Zr, ElementSymbols.Nb, ElementSymbols.Mo, ElementSymbols.Tc, ElementSymbols.Ru, ElementSymbols.Rh, 
            ElementSymbols.Pd, ElementSymbols.Ag, ElementSymbols.Cd, ElementSymbols.La, ElementSymbols.Hf, ElementSymbols.Ta, 
            ElementSymbols.W, ElementSymbols.Re, ElementSymbols.Os, ElementSymbols.Ir, ElementSymbols.Pt, ElementSymbols.Au, 
            ElementSymbols.Hg, ElementSymbols.Ac, ElementSymbols.Rf, ElementSymbols.Db, ElementSymbols.Sg, ElementSymbols.Bh, 
            ElementSymbols.Hs, ElementSymbols.Mt, ElementSymbols.Ce, ElementSymbols.Pr, ElementSymbols.Nd, ElementSymbols.Pm, 
            ElementSymbols.Sm, ElementSymbols.Eu, ElementSymbols.Gd, ElementSymbols.Tb, ElementSymbols.Dy, ElementSymbols.Ho, 
            ElementSymbols.Er, ElementSymbols.Tm, ElementSymbols.Yb, ElementSymbols.Lu, ElementSymbols.Th, ElementSymbols.Pa, 
            ElementSymbols.U, ElementSymbols.Np, ElementSymbols.Pu, ElementSymbols.Am, ElementSymbols.Cm, ElementSymbols.Bk, 
            ElementSymbols.Cf, ElementSymbols.Es, ElementSymbols.Fm, ElementSymbols.Md, ElementSymbols.No, ElementSymbols.Lr
        };

        static ElementSymbol[] metalAtomList = new[]
        { 
            ElementSymbols.Li, ElementSymbols.Na, ElementSymbols.K, ElementSymbols.Rb, ElementSymbols.Cs, ElementSymbols.Fr, 
            ElementSymbols.Be, ElementSymbols.Mg, ElementSymbols.Ca, ElementSymbols.Sr, ElementSymbols.Ba, ElementSymbols.Ra, 
            ElementSymbols.Al, ElementSymbols.Ga, ElementSymbols.In, ElementSymbols.Sn, ElementSymbols.Tl, ElementSymbols.Pb, 
            ElementSymbols.Bi, ElementSymbols.Sc, ElementSymbols.Ti, ElementSymbols.V, ElementSymbols.Cr, ElementSymbols.Mn, 
            ElementSymbols.Fe, ElementSymbols.Co, ElementSymbols.Ni, ElementSymbols.Cu, ElementSymbols.Zn, ElementSymbols.Y, 
            ElementSymbols.Zr, ElementSymbols.Nb, ElementSymbols.Mo, ElementSymbols.Tc, ElementSymbols.Ru, ElementSymbols.Rh, 
            ElementSymbols.Pd, ElementSymbols.Ag, ElementSymbols.Cd, ElementSymbols.La, ElementSymbols.Hf, ElementSymbols.Ta, 
            ElementSymbols.W, ElementSymbols.Re, ElementSymbols.Os, ElementSymbols.Ir, ElementSymbols.Pt, ElementSymbols.Au, 
            ElementSymbols.Hg, ElementSymbols.Ac, ElementSymbols.Rf, ElementSymbols.Db, ElementSymbols.Sg, ElementSymbols.Bh, 
            ElementSymbols.Hs, ElementSymbols.Mt, ElementSymbols.Ce, ElementSymbols.Pr, ElementSymbols.Nd, ElementSymbols.Pm, 
            ElementSymbols.Sm, ElementSymbols.Eu, ElementSymbols.Gd, ElementSymbols.Tb, ElementSymbols.Dy, ElementSymbols.Ho, 
            ElementSymbols.Er, ElementSymbols.Tm, ElementSymbols.Yb, ElementSymbols.Lu, ElementSymbols.Th, ElementSymbols.Pa, 
            ElementSymbols.U, ElementSymbols.Np, ElementSymbols.Pu, ElementSymbols.Am, ElementSymbols.Cm, ElementSymbols.Bk, 
            ElementSymbols.Cf, ElementSymbols.Es, ElementSymbols.Fm, ElementSymbols.Md, ElementSymbols.No, ElementSymbols.Lr
        };

        /// <summary>
        /// A list of metal atoms.
        /// </summary>
        public static IEnumerable<ElementSymbol> MetalAtomsList { get { return metalAtomList; } }

        /// <summary>
        /// determines if the element is a metal.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsMetalSymbol(ElementSymbol symbol)
        {
            return metalAtoms.Contains(symbol);
        }

        public static BondType? GetBondType(double distance, IAtom a, IAtom b)
        {
            BondType? type;

            //type = GetBondTypePair(distance, PdbPrefix(a), PdbPrefix(b));
            //if (type != null) return type;

            //var elA = a.ElementSymbol.ToString();

            //type = GetBondTypePair(distance, elA, PdbPrefix(b));
            //if (type != null) return type;

            //var elB = b.ElementSymbol.ToString();

            //type = GetBondTypePair(distance, PdbPrefix(a), elB);
            //if (type != null) return type;

            type = GetBondTypePair(distance, a.ElementSymbol, b.ElementSymbol);
            if (type != null) return type;

            //type = GetBondTypeSingle(distance, PdbPrefix(a));
            //if (type != null) return type;

            //type = GetBondTypeSingle(distance, PdbPrefix(b));
            //if (type != null) return type;

            type = GetBondTypeSingle(distance, a.ElementSymbol);
            if (type != null) return type;

            type = GetBondTypeSingle(distance, b.ElementSymbol);
            if (type != null) return type;

            return null;
        }

        static double GetDouble(XElement e, string attribute, double defaultValue)
        {
            var att = e.Attribute(attribute);

            if (att != null) return double.Parse(att.Value, System.Globalization.CultureInfo.InvariantCulture);
            return defaultValue;
        }

        static int GetInt(XElement e, string attribute, int defaultValue)
        {
            var att = e.Attribute(attribute);

            if (att != null) return int.Parse(att.Value, System.Globalization.CultureInfo.InvariantCulture);
            return defaultValue;
        }

        static string GetString(XElement e, string attribute, string defaultValue)
        {
            var att = e.Attribute(attribute);

            if (att != null) return att.Value;
            return defaultValue;
        }

        static BondType GetBondType(XElement e, string attribute, BondType defaultValue)
        {
            var att = e.Attribute(attribute);

            if (att != null) return (BondType)Enum.Parse(typeof(BondType), att.Value, true);
            return defaultValue;
        }

        static Threshold[] GetThresholds(XElement root)
        {
            return root.Elements()
                .Select(e => new Threshold(GetDouble(e, "Value", 0), GetBondType(e, "Type", BondType.Unknown)))
                .OrderBy(t => t.Value)
                .ToArray();
        }

        //public static void FromXml(XElement root)
        //{
        //    pairsBondInfo.Clear();
        //    elementBondInfo.Clear();
        //    elementInfo.Clear();

        //    foreach (var e in root.Element("ElementInfo").Elements())
        //    {
        //        elementInfo[new ElementSymbol(e.Attribute("Name").Value)] = 
        //            new ElementInfo(
        //                GetInt(e, "Valency", 8), 
        //                GetDouble(e, "CovalentRadius", 1),
        //                GetDouble(e, "AtomicRadius", 1),
        //                GetDouble(e, "VDWRadius", 1.5),
        //                ElementColor.Parse(GetString(e, "Color", "#FFFFFF")));
        //    }

        //    foreach (var e in root.Elements("BondInfo").Elements())
        //    {
        //        var from = GetString(e, "From", "*");
        //        var to = GetString(e, "To", "*");

        //        if (!from.EqualIgnoreCase("*") && !to.EqualIgnoreCase("*"))
        //        {
        //            pairsBondInfo[new ElementPair(new ElementSymbol(from), new ElementSymbol(to))] = GetThresholds(e);
        //        }
        //        else if (!from.EqualIgnoreCase("*"))
        //        {
        //            elementBondInfo[new ElementSymbol(from)] = GetThresholds(e);
        //        }
        //        else
        //        {
        //            elementBondInfo[new ElementSymbol(to)] = GetThresholds(e);
        //        }
        //    }
        //}


        static double GetMaxBondingRadius(IAtom atom)
        {
            ElementInfo info;
            if (elementInfo.TryGetValue(atom.ElementSymbol, out info)) return info.MaxBondingRadius;
            return 3; // dummy value
        }
        
        public class ComputePdbBondsResult
        {
            public HashSet<PdbResidueIdentifier> CloseResidueIdentifiers { get; set; }
            public List<StructureReaderWarning> CloseResidueWarnings { get; set; }
            public List<StructureReaderWarning> ConectWarnings { get; set; }
        }

        /// <summary>
        /// Computes bonds for a given structure.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="knownBonds"></param>
        public static ComputePdbBondsResult ComputePdbBonds(IStructure structure, IList<IBond> knownBonds = null)
        {
            const double minBondLength = 0.6;

            List<StructureReaderWarning> 
                closeResidueWarnings = new List<StructureReaderWarning>(),
                conectWarnings = new List<StructureReaderWarning>();

            KDAtomTree kdTree = structure.InvariantKdAtomTree();
            var atoms = structure.Atoms;

            HashSet<BondIdentifier> included = new HashSet<BondIdentifier>();
            HashSet<IBond> suspiciousBonds = new HashSet<IBond>();
            List<IBond> bonds = new List<IBond>();
            
            if (knownBonds != null)
            {
                foreach (var bond in knownBonds)
                {
                    if (included.Add(BondIdentifier.Get(bond.A, bond.B)))
                    {
                        bonds.Add(bond);
                        var len = bond.A.InvariantPosition.DistanceTo(bond.B.InvariantPosition);
                        if (len < minBondLength)
                        {
                            suspiciousBonds.Add(bond);
                        }

                        if (len < minBondLength || !ElementAndBondInfo.GetBondType(len, bond.A, bond.B).HasValue)
                        {
                            conectWarnings.Add(new ConectBondLengthReaderWarning(bond.A, bond.B));
                        }
                    }
                }
            }

            foreach (var atom in atoms.OrderBy(a => GetMaxBondingRadius(a)))
            {
                var info = ElementAndBondInfo.GetValencyAndRadius(atom);
                var atomPosition = atom.Position;

                // Handle hydrogens.
                if (atom.ElementSymbol == ElementSymbols.H || atom.ElementSymbol == ElementSymbols.D)
                {
                    var cand = kdTree.Nearest(atomPosition, 4, info.Item2);
                    for (int i = 1; i < cand.Count; i++)
                    {
                        var other = cand[i].Value;
                        if (other == atom || other.ElementSymbol == ElementSymbols.H || other.ElementSymbol == ElementSymbols.D) continue;
                        if (included.Add(BondIdentifier.Get(atom, other)))
                        {
                            bonds.Add(Bond.Create(atom, other, BondType.Single));
                        }
                        break;
                    }                        
                    continue;
                }

                // neighbors are ordered in ascending order, neighbors[0] = atom
                var neighbors = kdTree.Nearest(atomPosition, info.Item1 + 1, info.Item2);

                //int skip = -1;
                // try to add bonds from the closest atom, ignore the 0th neighbor, because it is the atom we did the query for.
                for (int i = neighbors.Count - 1; i > 0 ; i--)
                {
                    //if (i == skip) continue;

                    var neighbor = neighbors[i];
                    var neighborAtom = neighbor.Value;

                    if (neighborAtom.ElementSymbol == ElementSymbols.H || neighborAtom.ElementSymbol == ElementSymbols.D) continue;

                    bool ok = true;
                    // if there is already a bond with some of the neighboring atoms (and the atom will eventually form a bond with the "center"), ignore the atom
                    for (int j = i - 1; j > 0; j--)
                    {
                        var other = neighbors[j];

                        if (other.Priority < minBondLength * minBondLength)
                        {
                            suspiciousBonds.Add(Bond.Create(atom, other.Value));
                        }

                        if (included.Contains(BondIdentifier.Get(other.Value, neighborAtom)))
                        {
                            if (ElementAndBondInfo.GetBondType(System.Math.Sqrt(other.Priority), atom, other.Value).HasValue)
                            {
                                //skip = j;
                                ok = false;
                                break;
                            }
                        }
                    }
                    if (!ok)
                    {
                        continue;
                    }
                    //else
                    //{
                    //    skip = -1;
                    //}

                    double dist = System.Math.Sqrt(neighbor.Priority);
                    
                    var type = ElementAndBondInfo.GetBondType(dist, atom, neighborAtom);

                    ////if (!type.HasValue)
                    ////{
                    ////    // this was added because of the inconsistency in atom radii and thresholds...
                    ////    // should be removed in the final version
                    ////    if (info.Item2 >= dist) type = BondType.Single;
                    ////}

                    if (type.HasValue && neighborAtom != atom && included.Add(BondIdentifier.Get(atom, neighborAtom)))
                    {
                        var bond = Bond.Create(atom, neighborAtom, type.Value);
                        bonds.Add(bond);
                        if (dist < minBondLength)
                        {
                            suspiciousBonds.Add(bond);
                        }
                    }
                }
            }


            var ret = BondCollection.Create(bonds);

            HashSet<PdbResidueIdentifier> closeResidues = new HashSet<PdbResidueIdentifier>();
            
            foreach (var bond in suspiciousBonds)
            {
                var a = bond.A;
                var b = bond.B;

                var ari = a.ResidueIdentifier();
                var bri = b.ResidueIdentifier();
                if (ari == bri) continue;
                if (closeResidues.Contains(ari) || closeResidues.Contains(bri)) continue;

                foreach (var s in ret[a])
                {
                    if (s.Id == bond.Id) continue;

                    var broke = false;
                    foreach (var u in ret[s.B])
                    {
                        if (u.A.InvariantPosition.DistanceTo(u.B.InvariantPosition) > minBondLength) continue;
                        if (u.Id == bond.Id) continue;

                        IAtom max =
                            a.PdbChainIdentifier().CompareTo(b.PdbChainIdentifier()) < 0
                            || (a.PdbChainIdentifier() == b.PdbChainIdentifier()
                            && a.PdbResidueSequenceNumber() < b.PdbResidueSequenceNumber())
                            ? b : a;
                        string message = string.Format("'{0}' and '{1}' are too close to each other ({2} ang). '{3}' ignored.",
                            a.ResidueString(), b.ResidueString(),
                            a.InvariantPosition.DistanceTo(b.InvariantPosition).ToStringInvariant("0.000"),
                            max.ResidueString());
                        if (closeResidues.Add(max.ResidueIdentifier()))
                        {
                            closeResidueWarnings.Add(new ResidueStructureReaderWarning(max.PdbResidueName(), max.ResidueIdentifier(), message, type: ResidueStructureReaderWarningType.ResiduesTooClose));
                        }
                        broke = true;
                        break;
                    }
                    if (broke) break;
                }
            }

            structure.MustBe<Structure>().SetBonds(ret);
            return new ComputePdbBondsResult
            {
                CloseResidueWarnings = closeResidueWarnings,
                CloseResidueIdentifiers = closeResidues,
                ConectWarnings = conectWarnings
            };
        }

        /// <summary>
        /// Serializes bonds.
        /// Format:
        /// A.id
        /// B.id
        /// Type
        /// </summary>
        /// <param name="s"></param>
        /// <param name="writer"></param>
        public static void SerializeBonds(IStructure s, TextWriter writer)
        {
            writer.WriteLine(s.Bonds.Count);
            foreach (var b in s.Bonds)
            {
                //writer.WriteLine("{0} {1} {2}", b.A.Id, b.B.Id, (int)b.Type);
                writer.WriteLine(b.A.Id);
                writer.WriteLine(b.B.Id);
                writer.WriteLine((int)b.Type);
            }
            //writer.WriteLine(s.Atoms.Count);
            //foreach (var a in s.Atoms)
            //{
            //    writer.WriteLine("{0} {1}", a.Id, string.Join(" ", s.Bonds[a].Select(b => b.B.Id.ToString() + " " + (int)b.Type)));
            //}
        }

        /// <summary>
        /// Reads bonds from a stream.
        /// Format:
        /// A.id
        /// B.id
        /// Type
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="reader"></param>
        public static void ReadBonds(IStructure structure, TextReader reader)
        {
            var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;
            var numberStyle = NumberStyles.Integer;
            
            var atoms = structure.Atoms;
            List<IBond> bonds = new List<IBond>();
            var toRead = int.Parse(reader.ReadLine(), numberStyle, invariantCulture);

            //for (int i = 0; i < toRead; i++)
            //{
            //    string l = reader.ReadLine();
            //    int a = l.IndexOf(' '), b = l.LastIndexOf(' ');
            //    int aId = StructureReader.ParseIntFast(l, 0, a);
            //    int bId = StructureReader.ParseIntFast(l, a + 1, b - a - 1);
            //    BondType t = (BondType)StructureReader.ParseIntFast(l, b + 1, l.Length - b - 1);

            //    bonds.Add(Bond.Create(atoms.GetById(aId), atoms.GetById(bId), t));
            //}

            //int numRead = 0;
            //while (numRead < toRead)
            //{
            //    string l = reader.ReadLine();
            //    int aId = int.Parse(l, numberStyle, invariantCulture);

            //    l = reader.ReadLine();
            //    int bId = int.Parse(l, numberStyle, invariantCulture);

            //    l = reader.ReadLine();
            //    BondType t = (BondType)int.Parse(l, numberStyle, invariantCulture);

            //    bonds.Add(Bond.Create(atoms.GetById(aId), atoms.GetById(bId), t));

            //    numRead++;
            //}

            int numRead = 0;
            while (numRead < toRead)
            {
                string l = reader.ReadLine();
                int aId = NumberParser.ParseIntFast(l, 0, l.Length);

                l = reader.ReadLine();
                int bId = NumberParser.ParseIntFast(l, 0, l.Length);

                l = reader.ReadLine();
                BondType t = (BondType)NumberParser.ParseIntFast(l, 0, l.Length);

                bonds.Add(Bond.Create(atoms.GetById(aId), atoms.GetById(bId), t));

                numRead++;
            }

            //toRead = int.Parse(reader.ReadLine(), numberStyle, invariantCulture);
            //numRead = 0;

            //while (numRead < toRead)
            //{
            //}

            structure.MustBe<Structure>().SetBonds(BondCollection.Create(bonds));
        }

        private static void SetDefaultColors()
        {
            Action<ElementSymbol, ElementColor> setColor = (e, c) =>
            {
                if (elementInfo.ContainsKey(e)) elementInfo[e].Color = c;
                else
                {
                    elementInfo[e] = ElementInfo.Default;
                    elementInfo[e].Color = c;
                }
            };

            setColor(ElementSymbols.H, ElementColor.Parse("#FFFFFF"));
            setColor(ElementSymbols.He, ElementColor.Parse("#D9FFFF"));
            setColor(ElementSymbols.Li, ElementColor.Parse("#CC80FF"));
            setColor(ElementSymbols.Be, ElementColor.Parse("#C2FF00"));
            setColor(ElementSymbols.B, ElementColor.Parse("#FFB5B5"));
            setColor(ElementSymbols.C, ElementColor.Parse("#909090"));
            setColor(ElementSymbols.N, ElementColor.Parse("#3050F8"));
            setColor(ElementSymbols.O, ElementColor.Parse("#FF0D0D"));
            setColor(ElementSymbols.F, ElementColor.Parse("#90E050"));
            setColor(ElementSymbols.Ne, ElementColor.Parse("#B3E3F5"));
            setColor(ElementSymbols.Na, ElementColor.Parse("#AB5CF2"));
            setColor(ElementSymbols.Mg, ElementColor.Parse("#8AFF00"));
            setColor(ElementSymbols.Al, ElementColor.Parse("#BFA6A6"));
            setColor(ElementSymbols.Si, ElementColor.Parse("#F0C8A0"));
            setColor(ElementSymbols.P, ElementColor.Parse("#FF8000"));
            setColor(ElementSymbols.S, ElementColor.Parse("#FFFF30"));
            setColor(ElementSymbols.Cl, ElementColor.Parse("#1FF01F"));
            setColor(ElementSymbols.Ar, ElementColor.Parse("#80D1E3"));
            setColor(ElementSymbols.K, ElementColor.Parse("#8F40D4"));
            setColor(ElementSymbols.Ca, ElementColor.Parse("#3DFF00"));
            setColor(ElementSymbols.Sc, ElementColor.Parse("#E6E6E6"));
            setColor(ElementSymbols.Ti, ElementColor.Parse("#BFC2C7"));
            setColor(ElementSymbols.V, ElementColor.Parse("#A6A6AB"));
            setColor(ElementSymbols.Cr, ElementColor.Parse("#8A99C7"));
            setColor(ElementSymbols.Mn, ElementColor.Parse("#9C7AC7"));
            setColor(ElementSymbols.Fe, ElementColor.Parse("#E06633"));
            setColor(ElementSymbols.Co, ElementColor.Parse("#F090A0"));
            setColor(ElementSymbols.Ni, ElementColor.Parse("#50D050"));
            setColor(ElementSymbols.Cu, ElementColor.Parse("#C88033"));
            setColor(ElementSymbols.Zn, ElementColor.Parse("#7D80B0"));
            setColor(ElementSymbols.Ga, ElementColor.Parse("#C28F8F"));
            setColor(ElementSymbols.Ge, ElementColor.Parse("#668F8F"));
            setColor(ElementSymbols.As, ElementColor.Parse("#BD80E3"));
            setColor(ElementSymbols.Se, ElementColor.Parse("#FFA100"));
            setColor(ElementSymbols.Br, ElementColor.Parse("#A62929"));
            setColor(ElementSymbols.Kr, ElementColor.Parse("#5CB8D1"));
            setColor(ElementSymbols.Rb, ElementColor.Parse("#702EB0"));
            setColor(ElementSymbols.Sr, ElementColor.Parse("#00FF00"));
            setColor(ElementSymbols.Y, ElementColor.Parse("#94FFFF"));
            setColor(ElementSymbols.Zr, ElementColor.Parse("#94E0E0"));
            setColor(ElementSymbols.Nb, ElementColor.Parse("#73C2C9"));
            setColor(ElementSymbols.Mo, ElementColor.Parse("#54B5B5"));
            setColor(ElementSymbols.Tc, ElementColor.Parse("#3B9E9E"));
            setColor(ElementSymbols.Ru, ElementColor.Parse("#248F8F"));
            setColor(ElementSymbols.Rh, ElementColor.Parse("#0A7D8C"));
            setColor(ElementSymbols.Pd, ElementColor.Parse("#006985"));
            setColor(ElementSymbols.Ag, ElementColor.Parse("#C0C0C0"));
            setColor(ElementSymbols.Cd, ElementColor.Parse("#FFD98F"));
            setColor(ElementSymbols.In, ElementColor.Parse("#A67573"));
            setColor(ElementSymbols.Sn, ElementColor.Parse("#668080"));
            setColor(ElementSymbols.Sb, ElementColor.Parse("#9E63B5"));
            setColor(ElementSymbols.Te, ElementColor.Parse("#D47A00"));
            setColor(ElementSymbols.I, ElementColor.Parse("#940094"));
            setColor(ElementSymbols.Xe, ElementColor.Parse("#429EB0"));
            setColor(ElementSymbols.Cs, ElementColor.Parse("#57178F"));
            setColor(ElementSymbols.Ba, ElementColor.Parse("#00C900"));
            setColor(ElementSymbols.La, ElementColor.Parse("#70D4FF"));
            setColor(ElementSymbols.Ce, ElementColor.Parse("#FFFFC7"));
            setColor(ElementSymbols.Pr, ElementColor.Parse("#D9FFC7"));
            setColor(ElementSymbols.Nd, ElementColor.Parse("#C7FFC7"));
            setColor(ElementSymbols.Pm, ElementColor.Parse("#A3FFC7"));
            setColor(ElementSymbols.Sm, ElementColor.Parse("#8FFFC7"));
            setColor(ElementSymbols.Eu, ElementColor.Parse("#61FFC7"));
            setColor(ElementSymbols.Gd, ElementColor.Parse("#45FFC7"));
            setColor(ElementSymbols.Tb, ElementColor.Parse("#30FFC7"));
            setColor(ElementSymbols.Dy, ElementColor.Parse("#1FFFC7"));
            setColor(ElementSymbols.Ho, ElementColor.Parse("#00FF9C"));
            setColor(ElementSymbols.Er, ElementColor.Parse("#00E675"));
            setColor(ElementSymbols.Tm, ElementColor.Parse("#00D452"));
            setColor(ElementSymbols.Yb, ElementColor.Parse("#00BF38"));
            setColor(ElementSymbols.Lu, ElementColor.Parse("#00AB24"));
            setColor(ElementSymbols.Hf, ElementColor.Parse("#4DC2FF"));
            setColor(ElementSymbols.Ta, ElementColor.Parse("#4DA6FF"));
            setColor(ElementSymbols.W, ElementColor.Parse("#2194D6"));
            setColor(ElementSymbols.Re, ElementColor.Parse("#267DAB"));
            setColor(ElementSymbols.Os, ElementColor.Parse("#266696"));
            setColor(ElementSymbols.Ir, ElementColor.Parse("#175487"));
            setColor(ElementSymbols.Pt, ElementColor.Parse("#D0D0E0"));
            setColor(ElementSymbols.Au, ElementColor.Parse("#FFD123"));
            setColor(ElementSymbols.Hg, ElementColor.Parse("#B8B8D0"));
            setColor(ElementSymbols.Tl, ElementColor.Parse("#A6544D"));
            setColor(ElementSymbols.Pb, ElementColor.Parse("#575961"));
            setColor(ElementSymbols.Bi, ElementColor.Parse("#9E4FB5"));
            setColor(ElementSymbols.Po, ElementColor.Parse("#AB5C00"));
            setColor(ElementSymbols.At, ElementColor.Parse("#754F45"));
            setColor(ElementSymbols.Rn, ElementColor.Parse("#428296"));
            setColor(ElementSymbols.Fr, ElementColor.Parse("#420066"));
            setColor(ElementSymbols.Ra, ElementColor.Parse("#007D00"));
            setColor(ElementSymbols.Ac, ElementColor.Parse("#70ABFA"));
            setColor(ElementSymbols.Th, ElementColor.Parse("#00BAFF"));
            setColor(ElementSymbols.Pa, ElementColor.Parse("#00A1FF"));
            setColor(ElementSymbols.U, ElementColor.Parse("#008FFF"));
            setColor(ElementSymbols.Np, ElementColor.Parse("#0080FF"));
            setColor(ElementSymbols.Pu, ElementColor.Parse("#006BFF"));
            setColor(ElementSymbols.Am, ElementColor.Parse("#545CF2"));
            setColor(ElementSymbols.Cm, ElementColor.Parse("#785CE3"));
            setColor(ElementSymbols.Bk, ElementColor.Parse("#8A4FE3"));
            setColor(ElementSymbols.Cf, ElementColor.Parse("#A136D4"));
            setColor(ElementSymbols.Es, ElementColor.Parse("#B31FD4"));
            setColor(ElementSymbols.Fm, ElementColor.Parse("#B31FBA"));
            setColor(ElementSymbols.Md, ElementColor.Parse("#B30DA6"));
            setColor(ElementSymbols.No, ElementColor.Parse("#BD0D87"));
            setColor(ElementSymbols.Lr, ElementColor.Parse("#C70066"));
            setColor(ElementSymbols.Rf, ElementColor.Parse("#CC0059"));
            setColor(ElementSymbols.Db, ElementColor.Parse("#D1004F"));
            setColor(ElementSymbols.Sg, ElementColor.Parse("#D90045"));
            setColor(ElementSymbols.Bh, ElementColor.Parse("#E00038"));
            setColor(ElementSymbols.Hs, ElementColor.Parse("#E6002E"));
            setColor(ElementSymbols.Mt, ElementColor.Parse("#EB0026"));
        }

        static ElementAndBondInfo()
        {
            SetDefault();
        }
    }
}