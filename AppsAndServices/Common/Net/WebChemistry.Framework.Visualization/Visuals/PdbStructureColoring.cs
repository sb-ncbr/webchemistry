namespace WebChemistry.Framework.Visualization.Visuals
{
    using System.Linq;
    using System.Windows.Media;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;

    static class PdbStructureColoring
    {
        static readonly Color MinChainColor = Color.FromArgb(0xFF, 0x2B, 0x5D, 0x92);
        static readonly Color MaxChainColor = Color.FromArgb(0xFF, 0xE3, 0x00, 0x16);
        
        public static readonly Color DefaultColor = Colors.Gray;
        public static readonly Color HighlightColor = Colors.Yellow;
        public static readonly Color SelectColor = Colors.Red;
        //public static readonly Color BackgroundColor = Colors.DarkGray;

        public static Color GetChainColor(int i, int numSteps)
        {
            return Interpolate(MinChainColor, MaxChainColor, i, numSteps);
        }

        static Color Interpolate(Color minColor, Color maxColor, int i, int numSteps)
        {
            if (numSteps == 1) return minColor;

            double dR = (maxColor.R - minColor.R) / (numSteps - 1);
            double dG = (maxColor.G - minColor.G) / (numSteps - 1);
            double dB = (maxColor.B - minColor.B) / (numSteps - 1);

            return Color.FromArgb(0xFF, (byte)(minColor.R + i * dR), (byte)(minColor.G + i * dG), (byte)(minColor.B + i * dB));
        }

        static Color GetResidueColor(string residueName)
        {
            if (residueName == "HOH") return Colors.Red;
            else if (residueName == "ALA") return Color.FromRgb(199, 199, 199);
            else if (residueName == "ARG") return Color.FromRgb(229, 10, 10);
            else if (residueName == "CYS") return Color.FromRgb(229, 229, 0);
            else if (residueName == "GLN") return Color.FromRgb(0, 229, 229);
            else if (residueName == "GLU") return Color.FromRgb(229, 10, 10);
            else if (residueName == "GLY") return Color.FromRgb(234, 234, 234);
            else if (residueName == "HIS") return Color.FromRgb(130, 130, 209);
            else if (residueName == "ILE") return Color.FromRgb(15, 130, 15);
            else if (residueName == "LEU") return Color.FromRgb(15, 130, 15);
            else if (residueName == "LYS") return Color.FromRgb(20, 90, 255);
            else if (residueName == "MET") return Color.FromRgb(229, 229, 0);
            else if (residueName == "PHE") return Color.FromRgb(50, 50, 169);
            else if (residueName == "PRO") return Color.FromRgb(219, 149, 130);
            else if (residueName == "SER") return Color.FromRgb(249, 149, 0);
            else if (residueName == "THR") return Color.FromRgb(249, 149, 0);
            else if (residueName == "TRP") return Color.FromRgb(179, 90, 179);
            else if (residueName == "TYR") return Color.FromRgb(50, 50, 169);
            else if (residueName == "VAL") return Color.FromRgb(15, 130, 15);
            else if (residueName == "ASN") return Color.FromRgb(0, 229, 229);
            else return Colors.Green;
        }

        public static Color GetResidueColor(PdbStructureVisual visual, PdbResidue residue, PdbStructureColorScheme? scheme = null)
        {
            scheme = scheme ?? visual.ColorScheme;

            switch (scheme)
            {
                case PdbStructureColorScheme.Background:
                    //return BackgroundColor;
                    return visual.BackgroundColor;
                case PdbStructureColorScheme.Structure:
                    if (residue.SecondaryType == SecondaryStructureType.Helix) return Colors.Blue;// Color.FromRgb(0x14, 0x43, 0x64);
                    if (residue.SecondaryType == SecondaryStructureType.Sheet) return Colors.Orange;
                    return DefaultColor;
                case PdbStructureColorScheme.Chain:
                    int chainCount = visual.Structure.PdbChains().Count;
                    return GetChainColor(visual.Structure.PdbChains().Keys.ToList().IndexOf(residue.ChainIdentifier), chainCount);
                case PdbStructureColorScheme.Atom:
                    return DefaultColor;
                case PdbStructureColorScheme.Residue:
                    return GetResidueColor(residue.Name.ToUpper());
                default:
                    return DefaultColor;
            }
        }

        public static Color GetAtomColor(PdbStructureVisual visual, AtomModel atom, PdbStructureColorScheme? scheme = null)
        {
            scheme = scheme ?? visual.ColorScheme;

            switch (scheme)
            {
                case PdbStructureColorScheme.Background:
                    //return BackgroundColor;
                    return visual.BackgroundColor;
                case PdbStructureColorScheme.Structure:
                    if (atom.Atom.IsHetAtom())
                    {
                        var c = atom.Atom.GetElementColor();
                        return Color.FromRgb(c.R, c.G, c.B);
                    }
                    return GetResidueColor(visual, atom.Structure.Structure.PdbResidues().FromAtom(atom.Atom));
                case PdbStructureColorScheme.Chain:
                case PdbStructureColorScheme.Residue:
                    return GetResidueColor(visual, atom.Structure.Structure.PdbResidues().FromAtom(atom.Atom));
                case PdbStructureColorScheme.Atom:
                    {
                        var c = atom.Atom.GetElementColor();
                        return Color.FromRgb(c.R, c.G, c.B);
                    }
                default:
                    return DefaultColor;
            }
        }
    }
}
