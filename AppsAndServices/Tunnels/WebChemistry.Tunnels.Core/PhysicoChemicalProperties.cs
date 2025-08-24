// -----------------------------------------------------------------------
// <copyright file="PhysicoChemicalProperties.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// ...
    /// </summary>
    public class TunnelPhysicoChemicalProperties
    {
        public static readonly int NumLayerProperties = 4;

        public int Charge { get; private set; }
        public int NumPositives { get; private set; }
        public int NumNegatives { get; private set; }
        public double Hydropathy { get; private set; }
        public double Hydrophobicity { get; private set; }
        public double Polarity { get; private set; }
        //public double Hydratation { get; private set; }
        public int Mutability { get; private set; }

        public TunnelPhysicoChemicalProperties(int Charge, double Hydropathy, double Hydrophobicity, double Polarity, int Mutability, int NumPositives = 0, int NumNegatives = 0)
        {
            this.Charge = Charge;
            this.Hydropathy = Hydropathy;
            this.Hydrophobicity = Hydrophobicity;
            this.Polarity = Polarity;
            //this.Hydratation = Hydratation;
            this.Mutability = Mutability;
            this.NumNegatives = NumNegatives;
            this.NumPositives = NumPositives;
        }

        public XElement ToXml()
        {
            return new XElement("Properties",
                new XAttribute("Charge", Charge),
                new XAttribute("NumPositives", NumPositives),
                new XAttribute("NumNegatives", NumNegatives),
                //new XAttribute("Hydratation", props.Hydratation.ToStringInvariant("0.00")),
                new XAttribute("Hydrophobicity", Hydrophobicity.ToStringInvariant("0.00")),
                new XAttribute("Hydropathy", Hydropathy.ToStringInvariant("0.00")),
                new XAttribute("Polarity", Polarity.ToStringInvariant("0.00")),
                new XAttribute("Mutability", Mutability));
        }
    }

    public static class PhysicoChemicalPropertyCalculation
    {
        public static TunnelPhysicoChemicalProperties CalculateResidueProperties(IList<PdbResidue> residues)
        {
            int count = 0;
            int charge = 0;
            double hydropathy = 0.0;
            double hydrophobicity = 0.0;
            double polarity = 0.0;
            //double hydratation = 0.0;
            double mutability = 0.0;
            int positives = 0;
            int negatives = 0;

            // count only side-chain residues
            foreach (var residue in residues)
            {
                var info = TunnelPhysicoChemicalPropertyTable.GetResidueProperties(residue);
                if (info == null) continue;

                count++;
                var pc = info.Charge;
                charge += pc;
                if (pc > 0)
                {
                    positives++;
                }
                else if (pc < 0)
                {
                    negatives++;
                }
                //hydropathy += info.Hydropathy;
                //hydratation += info.Hydratation;
                //hydrophobicity += info.Hydrophobicity;
                //polarity += info.Polarity;
                mutability += info.Mutability;
            }

            //hydropathy /= (double)count;
            //hydrophobicity /= (double)count;
            //polarity /= (double)count;
            if (count == 0) mutability = 0;
            else mutability /= (double)count;

            PhysicoChemicalPropertyCalculation.CalculateHydrophibilicyPolarityHydropathy(residues, out hydrophobicity, out polarity, out hydropathy);
                        
            return new TunnelPhysicoChemicalProperties(
                Charge: charge,
                Polarity: polarity,
                //Hydratation: hydratation,
                Hydrophobicity: hydrophobicity,
                Hydropathy: hydropathy,
                Mutability: (int)mutability,
                NumNegatives: negatives,
                NumPositives: positives
            );
        }

        public static void CalculateHydrophibilicyPolarityHydropathy(IList<PdbResidue> residues, out double hydrophobicity, out double polarity, out double hydropathy)
        {
            hydrophobicity = 0;
            polarity = 0;
            hydropathy = 0;

            int count = 0;

            foreach (var residue in residues)
            {
                var info = TunnelPhysicoChemicalPropertyTable.GetResidueProperties(residue);
                if (info == null) continue;

                count++;
                hydropathy += info.Hydropathy;
                hydrophobicity += info.Hydrophobicity;
                polarity += info.Polarity;
            }

            var infoGLY = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("GLY");
            var infoASN = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("ASN");

            foreach (var residue in residues)
            {
                count++;
                polarity += infoASN.Polarity;
                hydrophobicity += infoGLY.Hydrophobicity;
                hydropathy += infoGLY.Hydropathy;
            }

            if (count == 0)
            {
                hydropathy = hydrophobicity = polarity = 0;
            }
            else
            {
                hydropathy /= (double)count;
                hydrophobicity /= (double)count;
                polarity /= (double)count;
            }
        }

        public static void CalculateHydrophibilicyPolarityHydropathy(IEnumerable<TunnelLayer> layers, out double hydrophobicity, out double polarity, out double hydropathy)
        {
            hydrophobicity = 0;
            polarity = 0;
            hydropathy = 0;

            int count = 0;

            foreach (var residue in layers.SelectMany(l => l.NonBackboneLining))
            {
                var info = TunnelPhysicoChemicalPropertyTable.GetResidueProperties(residue);
                if (info == null) continue;

                count++;
                hydropathy += info.Hydropathy;
                hydrophobicity += info.Hydrophobicity;
                polarity += info.Polarity;
            }

            var infoGLY = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("GLY");
            var infoASN = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("ASN");

            foreach (var residue in layers.SelectMany(l => l.BackboneLining))
            {
                count++;
                polarity += infoASN.Polarity;
                hydrophobicity += infoGLY.Hydrophobicity;
                hydropathy += infoGLY.Hydropathy;
            }

            if (count == 0)
            {
                hydropathy = hydrophobicity = polarity = 0;
            }
            else
            {
                hydropathy /= (double)count;
                hydrophobicity /= (double)count;
                polarity /= (double)count;
            }
        }
    }

    /// <summary>
    /// Information about physico chemical properties of a tunnel
    /// </summary>
    public class TunnelPhysicoChemicalPropertyTable
    {
        public static TunnelPhysicoChemicalProperties GetResidueProperties(PdbResidue residue)
        {
            TunnelPhysicoChemicalProperties ret;
            if (info.TryGetValue(residue.Name, out ret)) return ret;
            return null;
        }

        public static TunnelPhysicoChemicalProperties GetResidueProperties(string name)
        {
            TunnelPhysicoChemicalProperties ret;
            if (info.TryGetValue(name, out ret)) return ret;
            return null;
        }

        private static Dictionary<string, TunnelPhysicoChemicalProperties> info = new Dictionary<string,TunnelPhysicoChemicalProperties>(StringComparer.OrdinalIgnoreCase) {
            {
                "ALA", new TunnelPhysicoChemicalProperties(
                    Charge: 0,
                    Hydropathy:  1.8,
                    Hydrophobicity:  0.02,
                    Polarity:  0,
                    //Hydratation: 1,
                    Mutability: 100)
            },
            {"ARG", new TunnelPhysicoChemicalProperties(
                Charge: 1,
                Hydropathy:  -4.5,
                Hydrophobicity:  -0.42,
                Polarity:  52,
                //Hydratation: 2.3,
                Mutability: 83
            )},
            {"ASN", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -3.5,
                Hydrophobicity:  -0.77,
                Polarity:  3.38,
                //Hydratation: 2.2,
                Mutability: 104
            )},
            {"ASP", new TunnelPhysicoChemicalProperties(
                Charge: -1,
                Hydropathy:  -3.5,
                Hydrophobicity:  -1.04,
                Polarity:  49.7,
                //Hydratation: 6.5,
                Mutability: 86
            )},
            {"CYS", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  2.5,
                Hydrophobicity:  0.77,
                Polarity:  1.48,
                //Hydratation: 0.1,
                Mutability: 44
            )},
            {"GLU", new TunnelPhysicoChemicalProperties(
                Charge: -1,
                Hydropathy:  -3.5,
                Hydrophobicity:  -1.14,
                Polarity:  49.9,
                //Hydratation: 6.2,
                Mutability: 77
            )},
            {"GLN", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -3.5,
                Hydrophobicity:  -1.1,
                Polarity:  3.53,
                //Hydratation: 2.1,
                Mutability: 84
            )},
            {"GLY", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -0.4,
                Hydrophobicity:  -0.8,
                Polarity:  0,
                //Hydratation: 1.1,
                Mutability: 50
            )},
            {"HIS", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -3.2,
                Hydrophobicity:  0.26,
                Polarity:  51.6,
                //Hydratation: 2.8,
                Mutability: 91
            )},
            {"ILE", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  4.5,
                Hydrophobicity:  1.81,
                Polarity:  0.13,
                //Hydratation: 0.8,
                Mutability: 103
            )},
            {"LEU", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  3.8,
                Hydrophobicity:  1.14,
                Polarity:  0.13,
                //Hydratation: 0.8,
                Mutability: 54
            )},
            {"LYS", new TunnelPhysicoChemicalProperties(
                Charge: 1,
                Hydropathy:  -3.9,
                Hydrophobicity:  -0.41,
                Polarity:  49.5,
                //Hydratation: 5.3,
                Mutability: 72
            )},
            {"MET", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  1.9,
                Hydrophobicity:  1,
                Polarity:  1.43,
                //Hydratation: 0.7,
                Mutability: 93
            )},
            {"PHE", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  2.8,
                Hydrophobicity:  1.35,
                Polarity:  0.35,
                //Hydratation: 1.4,
                Mutability: 51
            )},
            {"PRO", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -1.6,
                Hydrophobicity:  -0.09,
                Polarity:  1.58,
                //Hydratation: 0.9,
                Mutability: 58
            )},
            {"SER", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -0.8,
                Hydrophobicity:  -0.97,
                Polarity:  1.67,
                //Hydratation: 1.7,
                Mutability: 117
            )},		
            {"THR", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -0.7,
                Hydrophobicity:  -0.77,
                Polarity:  1.66,
                //Hydratation: 1.5,
                Mutability: 107
            )},
            {"TRP", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -0.9,
                Hydrophobicity:  1.71,
                Polarity:  2.1,
                //Hydratation: 1.9,
                Mutability: 25
            )},
            {"TYR", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  -1.3,
                Hydrophobicity:  1.11,
                Polarity:  1.61,
                //Hydratation: 2.1,
                Mutability: 50
            )},
            {"VAL", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Hydropathy:  4.2,
                Hydrophobicity:  1.13,
                Polarity:  0.13,
                //Hydratation: 0.9,
                Mutability: 98
            )}
        };
    }
}
