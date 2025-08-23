//namespace WebChemistry.Framework.Core
//{
//    using System;
//    using System.Linq;
//    using System.Collections.Generic;

//    static class ElementTable
//    {
//        class Element
//        {
//            public ElementSymbol Name { get; private set; }
//            public int Valency { get; private set; }
//            public Double AtomicRadius { get; set; }
//            public Double VdwRadius { get; set; }
//            public Tuple<BondType, double>[] ThresholdValues { get; set; }

//            public Element(ElementSymbol name, int valency, double atomicRadius, double vdwRadius, Tuple<BondType, double>[] thresholdValues)
//            {
//                this.Name = name;
//                this.Valency = valency;
//                this.AtomicRadius = atomicRadius;
//                this.VdwRadius = vdwRadius;
//                this.ThresholdValues = thresholdValues;
//            }
//        }

//        public enum RadiiDataSource
//        {
//            TunedWebElements = 1,
//            OriginalWebElements,
//            Wiki
//        }

//        static List<Element> ElementData = new List<Element>();
//        public static double TotalMaxBondingRadius;


//        /// <summary>
//        /// Sets the default data needed to compute bonds.
//        /// </summary>
//        /// <param name="source">Which of the three hardcoded sets of data is used. Tuned WebElements data set is recomended.</param>
//        public static void SetDefault(RadiiDataSource source = RadiiDataSource.TunedWebElements)
//        {
//            switch (source)
//            {
//                case RadiiDataSource.TunedWebElements:
//                    LoadTunedWebElementsData();
//                    break;
//                case RadiiDataSource.OriginalWebElements:
//                    LoadOriginalWebElementsData();
//                    break;
//                case RadiiDataSource.Wiki:
//                    LoadWikiData();
//                    break;
//                default:
//                    throw new ArgumentException();
//            }

//            SetTotalMaxBondingRadius();
//            GenerateThresholds();
//            InitColors();

//            HackVdwRadiusToNotBreakWebChemTunnels();
//        }


//        /// <summary>
//        /// Loads the data from http://www.webelements.com/periodicity/covalent_radius/, http://www.webelements.com/periodicity/radii_covalent_double/, 
//        /// http://www.webelements.com/periodicity/radii_covalent_triple/, http://www.webelements.com/periodicity/metallic_radius/,
//        /// http://www.webelements.com/periodicity/radius_ionic_pauling_1/ and http://www.webelements.com/periodicity/van_der_waals_radius/,
//        /// tuned for the best results on the basis of comparison with OpenBabel results.
//        /// </summary>
//        static void LoadTunedWebElementsData()
//        {
//            Action<ElementSymbol, int, double, double, double, double, double, double, double> AddElement
//                = (name, valency, atomicR, vdwR, singleR, doubleR, tripleR, metallicR, ionicR) =>
//                {
//                    ElementData.Add(new Element(name, valency, atomicR, vdwR, new Tuple<BondType, double>[] { 
//                    new Tuple<BondType, double>(BondType.Single, singleR),
//                    new Tuple<BondType, double>(BondType.Double, doubleR),
//                    new Tuple<BondType, double>(BondType.Triple, tripleR),
//                    new Tuple<BondType, double>(BondType.Metallic, metallicR),
//                    new Tuple<BondType, double>(BondType.Ion, ionicR)}));
//                };

//            AddElement(ElementSymbols.H, 1, 0.53, 1.2, 0.37 + 0.085, 0, 0, 0, 0.25 + 0.05);
//            AddElement(ElementSymbols.He, 0, 0.31, 1.4, 0.32, 0, 0, 0, 0.31);
//            AddElement(ElementSymbols.Li, 1, 1.67, 1.82, 1.34, 1.24, 0, 1.56, 1.45);
//            AddElement(ElementSymbols.Be, 2, 1.12, 0, 1, 0.9, 0.85, 1.12, 1.1);
//            AddElement(ElementSymbols.B, 3, 0.87, 0, 0.82 + 0.12, 0.78, 0.73, 0.98, 0.85);
//            AddElement(ElementSymbols.C, 4, 0.67, 1.7, 1 + 0.105, 0.67, 0.6, 0.86, 0.7 + 0.14);
//            AddElement(ElementSymbols.N, 3, 0.56, 1.55, 0.9 + 0.15, 0.6, 0.54, 0.53, 0.95 + 0.08);
//            AddElement(ElementSymbols.O, 2, 0.48, 1.52, 1.02, 0.57, 0.53, 0, 1 + 0.06 - 0.25);
//            AddElement(ElementSymbols.F, 1, 0.42, 1.47, 0.71, 0.59, 0.53, 0, 0.5);
//            AddElement(ElementSymbols.Ne, 0, 0.38, 1.54, 0.69, 0.96, 0, 0, 0.38);
//            AddElement(ElementSymbols.Na, 1, 1.9, 2.27, 1.54 + 0.3, 1.6, 0, 1.91, 1.8 + 0.3);
//            AddElement(ElementSymbols.Mg, 2, 1.45, 1.73, 1.3, 1.32, 1.27, 1.6 + 0.28, 1.5 + 0.28);
//            AddElement(ElementSymbols.Al, 3, 1.18, 0, 1.21 + 0.05, 1.13, 1.11, 1.43, 1.25);
//            AddElement(ElementSymbols.Si, 4, 1.11, 2.1, 1.11, 1.07, 1.02, 1.38, 1.1);
//            AddElement(ElementSymbols.P, 5, 0.98, 1.8, 1.06 + 0.18, 1.02, 0.94, 1.28 + 0.18, 1 + 0.18);
//            AddElement(ElementSymbols.S, 6, 0.88, 1.8, 1.2 + 0.16, 0.94, 0.95, 1.27, 1 + 0.16);
//            AddElement(ElementSymbols.Cl, 5, 0.79, 1.75, 1.10 + 0.08, 0.95, 0.93, 0.91, 1 + 0.085);
//            AddElement(ElementSymbols.Ar, 0, 0.71, 1.88, 0.97, 1.07, 0.96, 0, 0.71);
//            AddElement(ElementSymbols.K, 1, 2.43, 2.75, 1.96, 1.93, 0, 2.35, 2.2 + 0.18);
//            AddElement(ElementSymbols.Ca, 2, 1.94, 0, 1.74 + 0.48, 1.47, 1.33, 1.97 + 0.38, 1.8 + 0.3);
//            AddElement(ElementSymbols.Sc, 3, 1.84, 0, 1.44, 1.16, 1.14, 1.64, 1.6);
//            AddElement(ElementSymbols.Ti, 4, 1.76, 0, 1.36, 1.17, 1.08, 1.47, 1.4);
//            AddElement(ElementSymbols.V, 5, 1.71, 0, 1.25 + 0.55, 1.12, 1.06, 1.35, 1.35 + 0.4);
//            AddElement(ElementSymbols.Cr, 6, 1.66, 0, 1.27 + 0.065, 1.11, 1.03, 1.29, 1.4);
//            AddElement(ElementSymbols.Mn, 4, 1.61, 0, 1.39 + 0.17, 1.05, 1.03, 1.27 + 0.42, 1.55 + 0.20);
//            AddElement(ElementSymbols.Fe, 3, 1.56, 0, 1.25, 1.09, 1.02, 1.55, 1.6 + 0.265);
//            AddElement(ElementSymbols.Co, 3, 1.52, 0, 1.26, 1.03, 0.96, 1.31, 1.41 + 0.17);
//            AddElement(ElementSymbols.Ni, 2, 1.49, 1.63, 1.21 + 0.2, 1.01, 1.01, 1.25 + 0.2, 1.35 + 0.2);
//            AddElement(ElementSymbols.Cu, 2, 1.45, 1.4, 1.38 + 0.25, 1.15, 1.2, 1.28 + 0.25, 1.50 + 0.25);
//            AddElement(ElementSymbols.Zn, 2, 1.42, 1.39, 1.31 + 0.17, 1.2, 0, 1.36 + 0.17, 1.45 + 0.17);
//            AddElement(ElementSymbols.Ga, 3, 1.36, 1.87, 1.26, 1.17, 1.21, 1.4, 1.3);
//            AddElement(ElementSymbols.Ge, 4, 1.25, 0, 1.22, 1.11, 1.14, 1.44, 1.25);
//            AddElement(ElementSymbols.As, 5, 1.14, 1.85, 1.19, 1.14, 1.06, 1.48, 1.15);
//            AddElement(ElementSymbols.Se, 6, 1.03, 1.9, 1.23 + 0.11, 1.07, 1.07, 1.4, 1.15);
//            AddElement(ElementSymbols.Br, 5, 0.94, 1.85, 1.24 + 0.2, 1.09, 1.1, 1.17, 1.15 + 0.2);
//            AddElement(ElementSymbols.Kr, 2, 0.88, 2.02, 1.1, 1.21, 1.08, 0, 0.88);
//            AddElement(ElementSymbols.Rb, 1, 2.65, 0, 2.11 + 0.11, 2.02, 0, 2.48, 2.35 + 0.11);
//            AddElement(ElementSymbols.Sr, 2, 2.19, 0, 1.92, 1.57, 1.39, 2.15, 2);
//            AddElement(ElementSymbols.Y, 3, 2.12, 0, 1.92 + 0.05, 1.3, 1.24, 1.8, 1.58);
//            AddElement(ElementSymbols.Zr, 4, 2.06, 0, 1.48, 1.27, 1.21, 1.6, 1.55);
//            AddElement(ElementSymbols.Nb, 5, 1.98, 0, 1.37, 1.25, 1.16, 1.46, 1.45);
//            AddElement(ElementSymbols.Mo, 6, 1.9, 0, 1.45 + 0.05, 1.21, 1.13, 1.39, 1.45 + 0.075 + 0.084);
//            AddElement(ElementSymbols.Tc, 7, 1.83, 0, 1.56, 1.2, 1.1, 1.36, 1.35);
//            AddElement(ElementSymbols.Ru, 6, 1.78, 0, 1.26, 1.14, 1.03, 1.34, 1.3);
//            AddElement(ElementSymbols.Rh, 6, 1.73, 0, 1.35 + 0.2, 1.1, 1.06, 1.34, 1.35 + 0.2);
//            AddElement(ElementSymbols.Pd, 6, 1.69, 1.63, 1.31 + 0.18, 1.17, 1.12, 1.37, 1.4);
//            AddElement(ElementSymbols.Ag, 3, 1.65, 1.72, 1.53, 1.39, 1.37, 1.44, 1.6);
//            AddElement(ElementSymbols.Cd, 2, 1.61, 1.58, 1.48 + 0.25, 1.44, 0, 1.51, 1.55);
//            AddElement(ElementSymbols.In, 3, 1.56, 1.93, 1.44, 1.36, 1.46, 1.58, 1.55);
//            AddElement(ElementSymbols.Sn, 4, 1.45, 2.17, 1.41, 1.3, 1.32, 1.63, 1.45);
//            AddElement(ElementSymbols.Sb, 5, 1.33, 0, 1.38 + 0.117, 1.33, 1.27, 1.66, 1.45);
//            AddElement(ElementSymbols.Te, 6, 1.23, 2.06, 1.35, 1.28, 1.21, 1.6, 1.4);
//            AddElement(ElementSymbols.I, 7, 1.15, 1.98, 1.33 + 0.335, 1.29, 1.25, 1.39, 1.4);
//            AddElement(ElementSymbols.Xe, 6, 1.08, 2.16, 1.3, 1.35, 1.22, 0, 1.08);
//            AddElement(ElementSymbols.Cs, 1, 2.98, 0, 2.25 + 0.26, 2.09, 0, 2.67, 2.6 + 0.16);
//            AddElement(ElementSymbols.Ba, 2, 2.53, 0, 1.98 + 0.03, 1.61, 1.49, 2.22, 2.15);
//            AddElement(ElementSymbols.La, 3, 1.95, 0, 1.69, 1.39, 1.39, 1.88, 1.95);
//            AddElement(ElementSymbols.Ce, 4, 1.85, 0, 0, 2.37, 1.31, 1.83, 1.85);
//            AddElement(ElementSymbols.Pr, 4, 2.47, 0, 0, 1.38 + 0.8, 1.28, 1.83 + 0.8, 1.85 + 0.8);
//            AddElement(ElementSymbols.Nd, 3, 2.06, 0, 0, 1.37, 0, 1.82, 1.85);
//            AddElement(ElementSymbols.Pm, 3, 2.05, 0, 0, 1.35, 0, 1.81, 1.85);
//            AddElement(ElementSymbols.Sm, 3, 2.38, 0, 0, 1.9 + 0.6, 0, 1.8, 1.85);
//            AddElement(ElementSymbols.Eu, 3, 2.31, 0, 0, 1.34 + 1.18, 0, 2.04, 1.85);
//            AddElement(ElementSymbols.Gd, 3, 2.33, 0, 0, 1.35 + 0.9, 1.32, 1.8, 1.8);
//            AddElement(ElementSymbols.Tb, 4, 2.25, 0, 0, 1.35 + 0.61, 0, 1.78 + 0.61, 1.75 + 0.61);
//            AddElement(ElementSymbols.Dy, 3, 2.28, 0, 0, 1.33, 0, 1.77, 1.75);
//            AddElement(ElementSymbols.Ho, 3, 2.26, 0, 0, 1.33 + 0.6, 0, 1.77, 1.75);
//            AddElement(ElementSymbols.Er, 3, 2.26, 0, 0, 1.33 + 1.11, 0, 1.76, 1.75 + 0.95);
//            AddElement(ElementSymbols.Tm, 3, 2.22, 0, 0, 1.31, 0, 1.75, 1.75);
//            AddElement(ElementSymbols.Yb, 3, 2.22, 0, 0, 1.29 + 1.11, 0, 1.94, 1.75 + 1.11);
//            AddElement(ElementSymbols.Lu, 3, 2.17, 0, 1.87, 1.31, 1.31, 1.73, 1.75);
//            AddElement(ElementSymbols.Hf, 4, 2.08, 0, 1.5, 1.28, 1.22, 1.59, 1.55);
//            AddElement(ElementSymbols.Ta, 5, 2, 0, 1.38 + 0.315, 1.26, 1.19, 1.46, 1.45);
//            AddElement(ElementSymbols.W, 6, 1.93, 0, 1.78 + 0.07, 1.2, 1.15, 1.71, 1.68 + 0.16);
//            AddElement(ElementSymbols.Re, 7, 1.88, 0, 1.59, 1.19, 1.1, 1.37, 1.35);
//            AddElement(ElementSymbols.Os, 6, 1.85, 0, 1.28, 1.16, 1.09, 1.35, 1.3);
//            AddElement(ElementSymbols.Ir, 6, 1.8, 0, 1.37, 1.15, 1.07, 1.36, 1.35);
//            AddElement(ElementSymbols.Pt, 6, 1.77, 1.75, 1.28 + 0.25, 1.12, 1.1, 1.39, 1.35 + 0.25);
//            AddElement(ElementSymbols.Au, 5, 1.74, 1.66, 1.46, 1.21, 1.23, 1.46, 1.37);
//            AddElement(ElementSymbols.Hg, 4, 1.71, 1.55, 1.49 + 0.02, 1.42, 0, 1.51 + 0.02, 1.5 + 0.02);
//            AddElement(ElementSymbols.Tl, 3, 1.56, 1.96, 1.48 + 0.175, 1.42, 1.5, 1.6, 1.9 + 0.17);
//            AddElement(ElementSymbols.Pb, 4, 1.54, 2.02, 1.47 + 0.39, 1.35, 1.37, 1.7 + 0.215, 1.8 + 0.39);
//            AddElement(ElementSymbols.Bi, 5, 1.43, 0, 1.46, 1.41, 1.35, 1.78, 1.6);
//            AddElement(ElementSymbols.Po, 6, 1.35, 0, 0, 1.35, 1.29, 0, 1.9);
//            AddElement(ElementSymbols.At, 1, 1.27, 0, 0, 1.38, 1.38, 0, 1.27);
//            AddElement(ElementSymbols.Rn, 2, 1.2, 0, 1.45, 1.45, 1.33, 0, 1.2);
//            AddElement(ElementSymbols.Fr, 1, 0, 0, 0, 2.18, 0, 0, 0);
//            AddElement(ElementSymbols.Ra, 2, 0, 0, 0, 1.73, 1.59, 0, 2.15);
//            AddElement(ElementSymbols.Ac, 3, 1.95, 0, 0, 1.53, 1.4, 1.9, 1.95);
//            AddElement(ElementSymbols.Th, 4, 1.8, 0, 0, 1.43, 1.36, 1.8, 1.8);
//            AddElement(ElementSymbols.Pa, 5, 1.8, 0, 0, 1.38, 1.29, 1.64, 1.8);
//            AddElement(ElementSymbols.U, 6, 1.75, 1.86, 0, 1.34 + 1.16, 1.18, 1.54, 1.75 + 1.16);
//            AddElement(ElementSymbols.Np, 6, 1.75, 0, 0, 1.36, 1.16, 1.55, 1.75);
//            AddElement(ElementSymbols.Pu, 6, 1.75, 0, 0, 1.35, 0, 1.59, 1.75);
//            AddElement(ElementSymbols.Am, 4, 1.75, 0, 0, 1.35, 0, 1.73, 1.75);
//            AddElement(ElementSymbols.Cm, 4, 0, 0, 0, 1.36, 0, 1.74, 0);


//        }


//        /// <summary>
//        /// Loads the data from http://www.webelements.com/periodicity/covalent_radius/, http://www.webelements.com/periodicity/radii_covalent_double/, 
//        /// http://www.webelements.com/periodicity/radii_covalent_triple/, http://www.webelements.com/periodicity/metallic_radius/,
//        /// http://www.webelements.com/periodicity/radius_ionic_pauling_1/ and http://www.webelements.com/periodicity/van_der_waals_radius/.
//        /// </summary>
//        static void LoadOriginalWebElementsData()
//        {
//            Action<ElementSymbol, int, double, double, double, double, double, double, double> AddElement
//                = (name, valency, atomicR, vdwR, singleR, doubleR, tripleR, metallicR, ionicR) =>
//                {
//                    ElementData.Add(new Element(name, valency, atomicR, vdwR, new Tuple<BondType, double>[] { 
//                    new Tuple<BondType, double>(BondType.Single, singleR),
//                    new Tuple<BondType, double>(BondType.Double, doubleR),
//                    new Tuple<BondType, double>(BondType.Triple, tripleR),
//                    new Tuple<BondType, double>(BondType.Metallic, metallicR),
//                    new Tuple<BondType, double>(BondType.Ion, ionicR)}));
//                };

//            AddElement(ElementSymbols.H, 1, 0.53, 1.2, 0.37, 0, 0, 0, 0.25);
//            AddElement(ElementSymbols.He, 0, 0.31, 1.4, 0.32, 0, 0, 0, 0.31);
//            AddElement(ElementSymbols.Li, 1, 1.67, 1.82, 1.34, 1.24, 0, 1.56, 1.45);
//            AddElement(ElementSymbols.Be, 2, 1.12, 0, 0.9, 0.9, 0.85, 1.12, 1.05);
//            AddElement(ElementSymbols.B, 3, 0.87, 0, 0.82, 0.78, 0.73, 0.98, 0.85);
//            AddElement(ElementSymbols.C, 4, 0.67, 1.7, 0.77, 0.67, 0.6, 0.86, 0.7);
//            AddElement(ElementSymbols.N, 3, 0.56, 1.55, 0.75, 0.6, 0.54, 0.53, 0.65);
//            AddElement(ElementSymbols.O, 2, 0.48, 1.52, 0.73, 0.57, 0.53, 0, 0.6);
//            AddElement(ElementSymbols.F, 1, 0.42, 1.47, 0.71, 0.59, 0.53, 0, 0.5);
//            AddElement(ElementSymbols.Ne, 0, 0.38, 1.54, 0.69, 0.96, 0, 0, 0.38);
//            AddElement(ElementSymbols.Na, 1, 1.9, 2.27, 1.54, 1.6, 0, 1.91, 1.8);
//            AddElement(ElementSymbols.Mg, 2, 1.45, 1.73, 1.3, 1.32, 1.27, 1.6, 1.5);
//            AddElement(ElementSymbols.Al, 3, 1.18, 0, 1.18, 1.13, 1.11, 1.43, 1.25);
//            AddElement(ElementSymbols.Si, 4, 1.11, 2.1, 1.11, 1.07, 1.02, 1.38, 1.1);
//            AddElement(ElementSymbols.P, 5, 0.98, 1.8, 1.06, 1.02, 0.94, 1.28, 1);
//            AddElement(ElementSymbols.S, 6, 0.88, 1.8, 1.02, 0.94, 0.95, 1.27, 1);
//            AddElement(ElementSymbols.Cl, 5, 0.79, 1.75, 0.99, 0.95, 0.93, 0.91, 1);
//            AddElement(ElementSymbols.Ar, 0, 0.71, 1.88, 0.97, 1.07, 0.96, 0, 0.71);
//            AddElement(ElementSymbols.K, 1, 2.43, 2.75, 1.96, 1.93, 0, 2.35, 2.2);
//            AddElement(ElementSymbols.Ca, 2, 1.94, 0, 1.74, 1.47, 1.33, 1.97, 1.8);
//            AddElement(ElementSymbols.Sc, 3, 1.84, 0, 1.44, 1.16, 1.14, 1.64, 1.6);
//            AddElement(ElementSymbols.Ti, 4, 1.76, 0, 1.36, 1.17, 1.08, 1.47, 1.4);
//            AddElement(ElementSymbols.V, 5, 1.71, 0, 1.25, 1.12, 1.06, 1.35, 1.35);
//            AddElement(ElementSymbols.Cr, 6, 1.66, 0, 1.27, 1.11, 1.03, 1.29, 1.4);
//            AddElement(ElementSymbols.Mn, 4, 1.61, 0, 1.39, 1.05, 1.03, 1.27, 1.4);
//            AddElement(ElementSymbols.Fe, 3, 1.56, 0, 1.25, 1.09, 1.02, 1.26, 1.4);
//            AddElement(ElementSymbols.Co, 3, 1.52, 0, 1.26, 1.03, 0.96, 1.25, 1.35);
//            AddElement(ElementSymbols.Ni, 2, 1.49, 1.63, 1.21, 1.01, 1.01, 1.25, 1.35);
//            AddElement(ElementSymbols.Cu, 2, 1.45, 1.4, 1.38, 1.15, 1.2, 1.28, 1.35);
//            AddElement(ElementSymbols.Zn, 2, 1.42, 1.39, 1.31, 1.2, 0, 1.36, 1.35);
//            AddElement(ElementSymbols.Ga, 3, 1.36, 1.87, 1.26, 1.17, 1.21, 1.4, 1.3);
//            AddElement(ElementSymbols.Ge, 4, 1.25, 0, 1.22, 1.11, 1.14, 1.44, 1.25);
//            AddElement(ElementSymbols.As, 5, 1.14, 1.85, 1.19, 1.14, 1.06, 1.48, 1.15);
//            AddElement(ElementSymbols.Se, 6, 1.03, 1.9, 1.16, 1.07, 1.07, 1.4, 1.15);
//            AddElement(ElementSymbols.Br, 5, 0.94, 1.85, 1.14, 1.09, 1.1, 1.17, 1.15);
//            AddElement(ElementSymbols.Kr, 2, 0.88, 2.02, 1.1, 1.21, 1.08, 0, 0.88);
//            AddElement(ElementSymbols.Rb, 1, 2.65, 0, 2.11, 2.02, 0, 2.48, 2.35);
//            AddElement(ElementSymbols.Sr, 2, 2.19, 0, 1.92, 1.57, 1.39, 2.15, 2);
//            AddElement(ElementSymbols.Y, 3, 2.12, 0, 1.62, 1.3, 1.24, 1.8, 1.58);
//            AddElement(ElementSymbols.Zr, 4, 2.06, 0, 1.48, 1.27, 1.21, 1.6, 1.55);
//            AddElement(ElementSymbols.Nb, 5, 1.98, 0, 1.37, 1.25, 1.16, 1.46, 1.45);
//            AddElement(ElementSymbols.Mo, 6, 1.9, 0, 1.45, 1.21, 1.13, 1.39, 1.45);
//            AddElement(ElementSymbols.Tc, 7, 1.83, 0, 1.56, 1.2, 1.1, 1.36, 1.35);
//            AddElement(ElementSymbols.Ru, 6, 1.78, 0, 1.26, 1.14, 1.03, 1.34, 1.3);
//            AddElement(ElementSymbols.Rh, 6, 1.73, 0, 1.35, 1.1, 1.06, 1.34, 1.35);
//            AddElement(ElementSymbols.Pd, 6, 1.69, 1.63, 1.31, 1.17, 1.12, 1.37, 1.4);
//            AddElement(ElementSymbols.Ag, 3, 1.65, 1.72, 1.53, 1.39, 1.37, 1.44, 1.6);
//            AddElement(ElementSymbols.Cd, 2, 1.61, 1.58, 1.48, 1.44, 0, 1.51, 1.55);
//            AddElement(ElementSymbols.In, 3, 1.56, 1.93, 1.44, 1.36, 1.46, 1.58, 1.55);
//            AddElement(ElementSymbols.Sn, 4, 1.45, 2.17, 1.41, 1.3, 1.32, 1.63, 1.45);
//            AddElement(ElementSymbols.Sb, 5, 1.33, 0, 1.38, 1.33, 1.27, 1.66, 1.45);
//            AddElement(ElementSymbols.Te, 6, 1.23, 2.06, 1.35, 1.28, 1.21, 1.6, 1.4);
//            AddElement(ElementSymbols.I, 7, 1.15, 1.98, 1.33, 1.29, 1.25, 1.39, 1.4);
//            AddElement(ElementSymbols.Xe, 6, 1.08, 2.16, 1.3, 1.35, 1.22, 0, 1.08);
//            AddElement(ElementSymbols.Cs, 1, 2.98, 0, 2.25, 2.09, 0, 2.67, 2.6);
//            AddElement(ElementSymbols.Ba, 2, 2.53, 0, 1.98, 1.61, 1.49, 2.22, 2.15);
//            AddElement(ElementSymbols.La, 3, 1.95, 0, 1.69, 1.39, 1.39, 1.88, 1.95);
//            AddElement(ElementSymbols.Ce, 4, 1.85, 0, 0, 1.37, 1.31, 1.83, 1.85);
//            AddElement(ElementSymbols.Pr, 4, 2.47, 0, 0, 1.38, 1.28, 1.83, 1.85);
//            AddElement(ElementSymbols.Nd, 3, 2.06, 0, 0, 1.37, 0, 1.82, 1.85);
//            AddElement(ElementSymbols.Pm, 3, 2.05, 0, 0, 1.35, 0, 1.81, 1.85);
//            AddElement(ElementSymbols.Sm, 3, 2.38, 0, 0, 1.34, 0, 1.8, 1.85);
//            AddElement(ElementSymbols.Eu, 3, 2.31, 0, 0, 1.34, 0, 2.04, 1.85);
//            AddElement(ElementSymbols.Gd, 3, 2.33, 0, 0, 1.35, 1.32, 1.8, 1.8);
//            AddElement(ElementSymbols.Tb, 4, 2.25, 0, 0, 1.35, 0, 1.78, 1.75);
//            AddElement(ElementSymbols.Dy, 3, 2.28, 0, 0, 1.33, 0, 1.77, 1.75);
//            AddElement(ElementSymbols.Ho, 3, 2.26, 0, 0, 1.33, 0, 1.77, 1.75);
//            AddElement(ElementSymbols.Er, 3, 2.26, 0, 0, 1.33, 0, 1.76, 1.75);
//            AddElement(ElementSymbols.Tm, 3, 2.22, 0, 0, 1.31, 0, 1.75, 1.75);
//            AddElement(ElementSymbols.Yb, 3, 2.22, 0, 0, 1.29, 0, 1.94, 1.75);
//            AddElement(ElementSymbols.Lu, 3, 2.17, 0, 1.6, 1.31, 1.31, 1.73, 1.75);
//            AddElement(ElementSymbols.Hf, 4, 2.08, 0, 1.5, 1.28, 1.22, 1.59, 1.55);
//            AddElement(ElementSymbols.Ta, 5, 2, 0, 1.38, 1.26, 1.19, 1.46, 1.45);
//            AddElement(ElementSymbols.W, 6, 1.93, 0, 1.46, 1.2, 1.15, 1.39, 1.35);
//            AddElement(ElementSymbols.Re, 7, 1.88, 0, 1.59, 1.19, 1.1, 1.37, 1.35);
//            AddElement(ElementSymbols.Os, 6, 1.85, 0, 1.28, 1.16, 1.09, 1.35, 1.3);
//            AddElement(ElementSymbols.Ir, 6, 1.8, 0, 1.37, 1.15, 1.07, 1.36, 1.35);
//            AddElement(ElementSymbols.Pt, 6, 1.77, 1.75, 1.28, 1.12, 1.1, 1.39, 1.35);
//            AddElement(ElementSymbols.Au, 5, 1.74, 1.66, 1.44, 1.21, 1.23, 1.44, 1.35);
//            AddElement(ElementSymbols.Hg, 4, 1.71, 1.55, 1.49, 1.42, 0, 1.51, 1.5);
//            AddElement(ElementSymbols.Tl, 3, 1.56, 1.96, 1.48, 1.42, 1.5, 1.6, 1.9);
//            AddElement(ElementSymbols.Pb, 4, 1.54, 2.02, 1.47, 1.35, 1.37, 1.7, 1.8);
//            AddElement(ElementSymbols.Bi, 5, 1.43, 0, 1.46, 1.41, 1.35, 1.78, 1.6);
//            AddElement(ElementSymbols.Po, 6, 1.35, 0, 0, 1.35, 1.29, 0, 1.9);
//            AddElement(ElementSymbols.At, 1, 1.27, 0, 0, 1.38, 1.38, 0, 1.27);
//            AddElement(ElementSymbols.Rn, 2, 1.2, 0, 1.45, 1.45, 1.33, 0, 1.2);
//            AddElement(ElementSymbols.Fr, 1, 0, 0, 0, 2.18, 0, 0, 0);
//            AddElement(ElementSymbols.Ra, 2, 0, 0, 0, 1.73, 1.59, 0, 2.15);
//            AddElement(ElementSymbols.Ac, 3, 1.95, 0, 0, 1.53, 1.4, 1.9, 1.95);
//            AddElement(ElementSymbols.Th, 4, 1.8, 0, 0, 1.43, 1.36, 1.8, 1.8);
//            AddElement(ElementSymbols.Pa, 5, 1.8, 0, 0, 1.38, 1.29, 1.64, 1.8);
//            AddElement(ElementSymbols.U, 6, 1.75, 1.86, 0, 1.34, 1.18, 1.54, 1.75);
//            AddElement(ElementSymbols.Np, 6, 1.75, 0, 0, 1.36, 1.16, 1.55, 1.75);
//            AddElement(ElementSymbols.Pu, 6, 1.75, 0, 0, 1.35, 0, 1.59, 1.75);
//            AddElement(ElementSymbols.Am, 4, 1.75, 0, 0, 1.35, 0, 1.73, 1.75);
//            AddElement(ElementSymbols.Cm, 4, 0, 0, 0, 1.36, 0, 1.74, 0);
//        }


//        /// <summary>
//        /// Loads the covalent radii data from http://en.wikipedia.org/wiki/Covalent_radius#cite_note-Calc1-4 combined with WebElements data.
//        /// </summary>
//        static void LoadWikiData()
//        {
//            Action<ElementSymbol, int, double, double, double, double, double, double, double> AddElement = (name, valency, atomicR, vdwR, singleR, doubleR, tripleR, metallicR, ionicR) =>
//            {
//                ElementData.Add(new Element(name, valency, atomicR, vdwR, new Tuple<BondType, double>[] { 
//                    new Tuple<BondType, double>(BondType.Single, singleR),
//                    new Tuple<BondType, double>(BondType.Double, doubleR),
//                    new Tuple<BondType, double>(BondType.Triple, tripleR),
//                    new Tuple<BondType, double>(BondType.Metallic, metallicR),
//                    new Tuple<BondType, double>(BondType.Ion, ionicR)}));
//            };

//            AddElement(ElementSymbols.H, 1, 0.53, 1.2, 0.32, 0, 0, 0, 0.25);
//            AddElement(ElementSymbols.He, 0, 0.31, 1.4, 0.46, 0, 0, 0, 0.31);
//            AddElement(ElementSymbols.Li, 1, 1.67, 1.82, 1.33, 1.24, 0, 1.56, 1.45);
//            AddElement(ElementSymbols.Be, 2, 1.12, 0, 1.02, 0.9, 0.85, 1.12, 1.05);
//            AddElement(ElementSymbols.B, 3, 0.87, 0, 0.85, 0.78, 0.73, 0.98, 0.85);
//            AddElement(ElementSymbols.C, 4, 0.67, 1.7, 0.75, 0.67, 0.6, 0.86, 0.7);
//            AddElement(ElementSymbols.N, 3, 0.56, 1.55, 0.71, 0.6, 0.54, 0.53, 0.65);
//            AddElement(ElementSymbols.O, 2, 0.48, 1.52, 0.63, 0.57, 0.53, 0, 0.6);
//            AddElement(ElementSymbols.F, 1, 0.42, 1.47, 0.64, 0.59, 0.53, 0, 0.5);
//            AddElement(ElementSymbols.Ne, 0, 0.38, 1.54, 0.67, 0.96, 0, 0, 0.38);
//            AddElement(ElementSymbols.Na, 1, 1.9, 2.27, 1.55, 1.6, 0, 1.91, 1.8);
//            AddElement(ElementSymbols.Mg, 2, 1.45, 1.73, 1.39, 1.32, 1.27, 1.6, 1.5);
//            AddElement(ElementSymbols.Al, 3, 1.18, 0, 1.26, 1.13, 1.11, 1.43, 1.25);
//            AddElement(ElementSymbols.Si, 4, 1.11, 2.1, 1.16, 1.07, 1.02, 1.38, 1.1);
//            AddElement(ElementSymbols.P, 5, 0.98, 1.8, 1.11, 1.02, 0.94, 1.28, 1);
//            AddElement(ElementSymbols.S, 6, 0.88, 1.8, 1.03, 0.94, 0.95, 1.27, 1);
//            AddElement(ElementSymbols.Cl, 5, 0.79, 1.75, 0.99, 0.95, 0.93, 0.91, 1);
//            AddElement(ElementSymbols.Ar, 0, 0.71, 1.88, 0.96, 1.07, 0.96, 0, 0.71);
//            AddElement(ElementSymbols.K, 1, 2.43, 2.75, 1.96, 1.93, 0, 2.35 + 0.06, 2.2 + 0.6);
//            AddElement(ElementSymbols.Ca, 2, 1.94, 0, 1.71, 1.47, 1.33, 1.97, 1.8);
//            AddElement(ElementSymbols.Sc, 3, 1.84, 0, 1.48, 1.16, 1.14, 1.64, 1.6);
//            AddElement(ElementSymbols.Ti, 4, 1.76, 0, 1.36, 1.17, 1.08, 1.47, 1.4);
//            AddElement(ElementSymbols.V, 5, 1.71, 0, 1.34, 1.12, 1.06, 1.35, 1.35);
//            AddElement(ElementSymbols.Cr, 6, 1.66, 0, 1.22, 1.11, 1.03, 1.29, 1.4);
//            AddElement(ElementSymbols.Mn, 4, 1.61, 0, 1.19, 1.05, 1.03, 1.27, 1.4);
//            AddElement(ElementSymbols.Fe, 3, 1.56, 0, 1.16, 1.09, 1.02, 1.26, 1.4);
//            AddElement(ElementSymbols.Co, 3, 1.52, 0, 1.11, 1.03, 0.96, 1.25, 1.35);
//            AddElement(ElementSymbols.Ni, 2, 1.49, 1.63, 1.1, 1.01, 1.01, 1.25, 1.35);
//            AddElement(ElementSymbols.Cu, 2, 1.45, 1.4, 1.12, 1.15, 1.2, 1.28, 1.35);
//            AddElement(ElementSymbols.Zn, 2, 1.42, 1.39, 1.18, 1.2, 0, 1.36, 1.35);
//            AddElement(ElementSymbols.Ga, 3, 1.36, 1.87, 1.24, 1.17, 1.21, 1.4, 1.3);
//            AddElement(ElementSymbols.Ge, 4, 1.25, 0, 1.21, 1.11, 1.14, 1.44, 1.25);
//            AddElement(ElementSymbols.As, 5, 1.14, 1.85, 1.21, 1.14, 1.06, 1.48, 1.15);
//            AddElement(ElementSymbols.Se, 6, 1.03, 1.9, 1.16, 1.07, 1.07, 1.4, 1.15);
//            AddElement(ElementSymbols.Br, 5, 0.94, 1.85, 1.14, 1.09, 1.1, 1.17, 1.15);
//            AddElement(ElementSymbols.Kr, 2, 0.88, 2.02, 1.17, 1.21, 1.08, 0, 0.88);
//            AddElement(ElementSymbols.Rb, 1, 2.65, 0, 2.1, 2.02, 0, 2.48, 2.35);
//            AddElement(ElementSymbols.Sr, 2, 2.19, 0, 1.85, 1.57, 1.39, 2.15, 2);
//            AddElement(ElementSymbols.Y, 3, 2.12, 0, 1.63, 1.3, 1.24, 1.8, 1.58);
//            AddElement(ElementSymbols.Zr, 4, 2.06, 0, 1.54, 1.27, 1.21, 1.6, 1.55);
//            AddElement(ElementSymbols.Nb, 5, 1.98, 0, 1.47, 1.25, 1.16, 1.46, 1.45);
//            AddElement(ElementSymbols.Mo, 6, 1.9, 0, 1.38, 1.21, 1.13, 1.39, 1.45);
//            AddElement(ElementSymbols.Tc, 7, 1.83, 0, 1.28, 1.2, 1.1, 1.36, 1.35);
//            AddElement(ElementSymbols.Ru, 6, 1.78, 0, 1.25, 1.14, 1.03, 1.34, 1.3);
//            AddElement(ElementSymbols.Rh, 6, 1.73, 0, 1.25, 1.1, 1.06, 1.34, 1.35);
//            AddElement(ElementSymbols.Pd, 6, 1.69, 1.63, 1.25, 1.1, 1.06, 1.37, 1.4);
//            AddElement(ElementSymbols.Ag, 3, 1.65, 1.72, 1.28, 1.39, 1.37, 1.44, 1.6);
//            AddElement(ElementSymbols.Cd, 2, 1.61, 1.58, 1.36, 1.44, 0, 1.51, 1.55);
//            AddElement(ElementSymbols.In, 3, 1.56, 1.93, 1.42, 1.36, 1.46, 1.58, 1.55);
//            AddElement(ElementSymbols.Sn, 4, 1.45, 2.17, 1.4, 1.3, 1.32, 1.63, 1.45);
//            AddElement(ElementSymbols.Sb, 5, 1.33, 0, 1.4, 1.33, 1.27, 1.66, 1.45);
//            AddElement(ElementSymbols.Te, 6, 1.23, 2.06, 1.36, 1.28, 1.21, 1.6, 1.4);
//            AddElement(ElementSymbols.I, 7, 1.15, 1.98, 1.33, 1.29, 1.25, 1.39, 1.4);
//            AddElement(ElementSymbols.Xe, 6, 1.08, 2.16, 1.31, 1.35, 1.22, 0, 1.08);
//            AddElement(ElementSymbols.Cs, 1, 2.98, 0, 2.32, 2.09, 0, 2.67, 2.6);
//            AddElement(ElementSymbols.Ba, 2, 2.53, 0, 1.96, 1.61, 1.49, 2.22, 2.15);
//            AddElement(ElementSymbols.La, 3, 1.95, 0, 1.8, 1.39, 1.39, 1.88, 1.95);
//            AddElement(ElementSymbols.Ce, 4, 1.85, 0, 1.63, 1.37, 1.31, 1.83, 1.85);
//            AddElement(ElementSymbols.Pr, 4, 2.47, 0, 1.76, 1.36, 1.28, 1.83, 1.85);
//            AddElement(ElementSymbols.Nd, 3, 2.06, 0, 1.74, 1.37, 0, 1.82, 1.85);
//            AddElement(ElementSymbols.Pm, 3, 2.05, 0, 1.73, 1.35, 0, 1.81, 1.85);
//            AddElement(ElementSymbols.Sm, 3, 2.38, 0, 1.72, 1.34, 0, 1.8, 1.85);
//            AddElement(ElementSymbols.Eu, 3, 2.31, 0, 1.68, 1.34, 0, 2.04, 1.85);
//            AddElement(ElementSymbols.Gd, 3, 2.33, 0, 1.69, 1.35, 1.32, 1.8, 1.8);
//            AddElement(ElementSymbols.Tb, 4, 2.25, 0, 1.68, 1.35, 0, 1.78, 1.75);
//            AddElement(ElementSymbols.Dy, 3, 2.28, 0, 1.67, 1.33, 0, 1.77, 1.75);
//            AddElement(ElementSymbols.Ho, 3, 2.26, 0, 1.66, 1.33, 0, 1.77, 1.75);
//            AddElement(ElementSymbols.Er, 3, 2.26, 0, 1.65, 1.33, 0, 1.76, 1.75);
//            AddElement(ElementSymbols.Tm, 3, 2.22, 0, 1.64, 1.31, 0, 1.75, 1.75);
//            AddElement(ElementSymbols.Yb, 3, 2.22, 0, 1.7, 1.29, 0, 1.94, 1.75);
//            AddElement(ElementSymbols.Lu, 3, 2.17, 0, 1.62, 1.31, 1.31, 1.73, 1.75);
//            AddElement(ElementSymbols.Hf, 4, 2.08, 0, 1.52, 1.28, 1.22, 1.59, 1.55);
//            AddElement(ElementSymbols.Ta, 5, 2, 0, 1.46, 1.26, 1.19, 1.46, 1.45);
//            AddElement(ElementSymbols.W, 6, 1.93, 0, 1.37, 1.2, 1.15, 1.39, 1.35);
//            AddElement(ElementSymbols.Re, 7, 1.88, 0, 1.31, 1.19, 1.1, 1.37, 1.35);
//            AddElement(ElementSymbols.Os, 6, 1.85, 0, 1.29, 1.16, 1.09, 1.35, 1.3);
//            AddElement(ElementSymbols.Ir, 6, 1.8, 0, 1.22, 1.15, 1.07, 1.36, 1.35);
//            AddElement(ElementSymbols.Pt, 6, 1.77, 1.75, 1.23, 1.12, 1.1, 1.39, 1.35);
//            AddElement(ElementSymbols.Au, 5, 1.74, 1.66, 1.24, 1.21, 1.23, 1.44, 1.35);
//            AddElement(ElementSymbols.Hg, 4, 1.71, 1.55, 1.33, 1.42, 0, 1.51, 1.5);
//            AddElement(ElementSymbols.Tl, 3, 1.56, 1.96, 1.44, 1.42, 1.5, 1.6, 1.9);
//            AddElement(ElementSymbols.Pb, 4, 1.54, 2.02, 1.44, 1.35, 1.37, 1.7, 1.8);
//            AddElement(ElementSymbols.Bi, 5, 1.43, 0, 1.51, 1.41, 1.35, 1.78, 1.6);
//            AddElement(ElementSymbols.Po, 6, 1.35, 0, 1.45, 1.35, 1.29, 0, 1.9);
//            AddElement(ElementSymbols.At, 1, 1.27, 0, 1.47, 1.38, 1.38, 0, 1.27);
//            AddElement(ElementSymbols.Rn, 2, 1.2, 0, 1.42, 1.45, 1.33, 0, 1.2);
//            AddElement(ElementSymbols.Fr, 1, 0, 0, 2.23, 2.18, 0, 0, 0);
//            AddElement(ElementSymbols.Ra, 2, 0, 0, 2.01, 1.73, 1.59, 0, 2.15);
//            AddElement(ElementSymbols.Ac, 3, 1.95, 0, 1.86, 1.53, 1.4, 1.9, 1.95);
//            AddElement(ElementSymbols.Th, 4, 1.8, 0, 1.75, 1.43, 1.36, 1.8, 1.8);
//            AddElement(ElementSymbols.Pa, 5, 1.8, 0, 1.69, 1.38, 1.29, 1.64, 1.8);
//            AddElement(ElementSymbols.U, 6, 1.75, 1.86, 1.7, 1.34, 1.18, 1.54, 1.75);
//            AddElement(ElementSymbols.Np, 6, 1.75, 0, 1.71, 1.36, 1.16, 1.55, 1.75);
//            AddElement(ElementSymbols.Pu, 6, 1.75, 0, 1.72, 1.35, 0, 1.59, 1.75);
//            AddElement(ElementSymbols.Am, 4, 1.75, 0, 1.66, 1.35, 0, 1.73, 1.75);
//            AddElement(ElementSymbols.Cm, 4, 0, 0, 1.66, 1.36, 0, 1.74, 0);
//        }

//        /// <summary>
//        /// Assigns a Jmol color to every element. Source: http://jmol.sourceforge.net/jscolors/
//        /// </summary>
//        static void InitColors()
//        {
//            Action<ElementSymbol, ElementColor> setColor = (e, c) =>
//            {
//                if (ElementAndBondInfo.elementInfo.ContainsKey(e)) ElementAndBondInfo.elementInfo[e].Color = c;
//            };

//            setColor(ElementSymbols.H, ElementColor.Parse("#FFFFFF"));
//            setColor(ElementSymbols.He, ElementColor.Parse("#D9FFFF"));
//            setColor(ElementSymbols.Li, ElementColor.Parse("#CC80FF"));
//            setColor(ElementSymbols.Be, ElementColor.Parse("#C2FF00"));
//            setColor(ElementSymbols.B, ElementColor.Parse("#FFB5B5"));
//            setColor(ElementSymbols.C, ElementColor.Parse("#909090"));
//            setColor(ElementSymbols.N, ElementColor.Parse("#3050F8"));
//            setColor(ElementSymbols.O, ElementColor.Parse("#FF0D0D"));
//            setColor(ElementSymbols.F, ElementColor.Parse("#90E050"));
//            setColor(ElementSymbols.Ne, ElementColor.Parse("#B3E3F5"));
//            setColor(ElementSymbols.Na, ElementColor.Parse("#AB5CF2"));
//            setColor(ElementSymbols.Mg, ElementColor.Parse("#8AFF00"));
//            setColor(ElementSymbols.Al, ElementColor.Parse("#BFA6A6"));
//            setColor(ElementSymbols.Si, ElementColor.Parse("#F0C8A0"));
//            setColor(ElementSymbols.P, ElementColor.Parse("#FF8000"));
//            setColor(ElementSymbols.S, ElementColor.Parse("#FFFF30"));
//            setColor(ElementSymbols.Cl, ElementColor.Parse("#1FF01F"));
//            setColor(ElementSymbols.Ar, ElementColor.Parse("#80D1E3"));
//            setColor(ElementSymbols.K, ElementColor.Parse("#8F40D4"));
//            setColor(ElementSymbols.Ca, ElementColor.Parse("#3DFF00"));
//            setColor(ElementSymbols.Sc, ElementColor.Parse("#E6E6E6"));
//            setColor(ElementSymbols.Ti, ElementColor.Parse("#BFC2C7"));
//            setColor(ElementSymbols.V, ElementColor.Parse("#A6A6AB"));
//            setColor(ElementSymbols.Cr, ElementColor.Parse("#8A99C7"));
//            setColor(ElementSymbols.Mn, ElementColor.Parse("#9C7AC7"));
//            setColor(ElementSymbols.Fe, ElementColor.Parse("#E06633"));
//            setColor(ElementSymbols.Co, ElementColor.Parse("#F090A0"));
//            setColor(ElementSymbols.Ni, ElementColor.Parse("#50D050"));
//            setColor(ElementSymbols.Cu, ElementColor.Parse("#C88033"));
//            setColor(ElementSymbols.Zn, ElementColor.Parse("#7D80B0"));
//            setColor(ElementSymbols.Ga, ElementColor.Parse("#C28F8F"));
//            setColor(ElementSymbols.Ge, ElementColor.Parse("#668F8F"));
//            setColor(ElementSymbols.As, ElementColor.Parse("#BD80E3"));
//            setColor(ElementSymbols.Se, ElementColor.Parse("#FFA100"));
//            setColor(ElementSymbols.Br, ElementColor.Parse("#A62929"));
//            setColor(ElementSymbols.Kr, ElementColor.Parse("#5CB8D1"));
//            setColor(ElementSymbols.Rb, ElementColor.Parse("#702EB0"));
//            setColor(ElementSymbols.Sr, ElementColor.Parse("#00FF00"));
//            setColor(ElementSymbols.Y, ElementColor.Parse("#94FFFF"));
//            setColor(ElementSymbols.Zr, ElementColor.Parse("#94E0E0"));
//            setColor(ElementSymbols.Nb, ElementColor.Parse("#73C2C9"));
//            setColor(ElementSymbols.Mo, ElementColor.Parse("#54B5B5"));
//            setColor(ElementSymbols.Tc, ElementColor.Parse("#3B9E9E"));
//            setColor(ElementSymbols.Ru, ElementColor.Parse("#248F8F"));
//            setColor(ElementSymbols.Rh, ElementColor.Parse("#0A7D8C"));
//            setColor(ElementSymbols.Pd, ElementColor.Parse("#006985"));
//            setColor(ElementSymbols.Ag, ElementColor.Parse("#C0C0C0"));
//            setColor(ElementSymbols.Cd, ElementColor.Parse("#FFD98F"));
//            setColor(ElementSymbols.In, ElementColor.Parse("#A67573"));
//            setColor(ElementSymbols.Sn, ElementColor.Parse("#668080"));
//            setColor(ElementSymbols.Sb, ElementColor.Parse("#9E63B5"));
//            setColor(ElementSymbols.Te, ElementColor.Parse("#D47A00"));
//            setColor(ElementSymbols.I, ElementColor.Parse("#940094"));
//            setColor(ElementSymbols.Xe, ElementColor.Parse("#429EB0"));
//            setColor(ElementSymbols.Cs, ElementColor.Parse("#57178F"));
//            setColor(ElementSymbols.Ba, ElementColor.Parse("#00C900"));
//            setColor(ElementSymbols.La, ElementColor.Parse("#70D4FF"));
//            setColor(ElementSymbols.Ce, ElementColor.Parse("#FFFFC7"));
//            setColor(ElementSymbols.Pr, ElementColor.Parse("#D9FFC7"));
//            setColor(ElementSymbols.Nd, ElementColor.Parse("#C7FFC7"));
//            setColor(ElementSymbols.Pm, ElementColor.Parse("#A3FFC7"));
//            setColor(ElementSymbols.Sm, ElementColor.Parse("#8FFFC7"));
//            setColor(ElementSymbols.Eu, ElementColor.Parse("#61FFC7"));
//            setColor(ElementSymbols.Gd, ElementColor.Parse("#45FFC7"));
//            setColor(ElementSymbols.Tb, ElementColor.Parse("#30FFC7"));
//            setColor(ElementSymbols.Dy, ElementColor.Parse("#1FFFC7"));
//            setColor(ElementSymbols.Ho, ElementColor.Parse("#00FF9C"));
//            setColor(ElementSymbols.Er, ElementColor.Parse("#00E675"));
//            setColor(ElementSymbols.Tm, ElementColor.Parse("#00D452"));
//            setColor(ElementSymbols.Yb, ElementColor.Parse("#00BF38"));
//            setColor(ElementSymbols.Lu, ElementColor.Parse("#00AB24"));
//            setColor(ElementSymbols.Hf, ElementColor.Parse("#4DC2FF"));
//            setColor(ElementSymbols.Ta, ElementColor.Parse("#4DA6FF"));
//            setColor(ElementSymbols.W, ElementColor.Parse("#2194D6"));
//            setColor(ElementSymbols.Re, ElementColor.Parse("#267DAB"));
//            setColor(ElementSymbols.Os, ElementColor.Parse("#266696"));
//            setColor(ElementSymbols.Ir, ElementColor.Parse("#175487"));
//            setColor(ElementSymbols.Pt, ElementColor.Parse("#D0D0E0"));
//            setColor(ElementSymbols.Au, ElementColor.Parse("#FFD123"));
//            setColor(ElementSymbols.Hg, ElementColor.Parse("#B8B8D0"));
//            setColor(ElementSymbols.Tl, ElementColor.Parse("#A6544D"));
//            setColor(ElementSymbols.Pb, ElementColor.Parse("#575961"));
//            setColor(ElementSymbols.Bi, ElementColor.Parse("#9E4FB5"));
//            setColor(ElementSymbols.Po, ElementColor.Parse("#AB5C00"));
//            setColor(ElementSymbols.At, ElementColor.Parse("#754F45"));
//            setColor(ElementSymbols.Rn, ElementColor.Parse("#428296"));
//            setColor(ElementSymbols.Fr, ElementColor.Parse("#420066"));
//            setColor(ElementSymbols.Ra, ElementColor.Parse("#007D00"));
//            setColor(ElementSymbols.Ac, ElementColor.Parse("#70ABFA"));
//            setColor(ElementSymbols.Th, ElementColor.Parse("#00BAFF"));
//            setColor(ElementSymbols.Pa, ElementColor.Parse("#00A1FF"));
//            setColor(ElementSymbols.U, ElementColor.Parse("#008FFF"));
//            setColor(ElementSymbols.Np, ElementColor.Parse("#0080FF"));
//            setColor(ElementSymbols.Pu, ElementColor.Parse("#006BFF"));
//            setColor(ElementSymbols.Am, ElementColor.Parse("#545CF2"));
//            setColor(ElementSymbols.Cm, ElementColor.Parse("#785CE3"));
//            setColor(ElementSymbols.Bk, ElementColor.Parse("#8A4FE3"));
//            setColor(ElementSymbols.Cf, ElementColor.Parse("#A136D4"));
//            setColor(ElementSymbols.Es, ElementColor.Parse("#B31FD4"));
//            setColor(ElementSymbols.Fm, ElementColor.Parse("#B31FBA"));
//            setColor(ElementSymbols.Md, ElementColor.Parse("#B30DA6"));
//            setColor(ElementSymbols.No, ElementColor.Parse("#BD0D87"));
//            setColor(ElementSymbols.Lr, ElementColor.Parse("#C70066"));
//            setColor(ElementSymbols.Rf, ElementColor.Parse("#CC0059"));
//            setColor(ElementSymbols.Db, ElementColor.Parse("#D1004F"));
//            setColor(ElementSymbols.Sg, ElementColor.Parse("#D90045"));
//            setColor(ElementSymbols.Bh, ElementColor.Parse("#E00038"));
//            setColor(ElementSymbols.Hs, ElementColor.Parse("#E6002E"));
//            setColor(ElementSymbols.Mt, ElementColor.Parse("#EB0026"));
//        }


//        static void HackVdwRadiusToNotBreakWebChemTunnels()
//        {
//            Action<ElementSymbol, double> setVdw = (e, r) =>
//            {
//                if (ElementAndBondInfo.elementInfo.ContainsKey(e)) ElementAndBondInfo.elementInfo[e].VdwRadius = r;
//            };

//            setVdw(ElementSymbols.H, 1.0);
//            setVdw(ElementSymbols.O, 1.45);
//            setVdw(ElementSymbols.S, 1.77);
//            setVdw(ElementSymbols.N, 1.55);
//            setVdw(ElementSymbols.C, 1.61);

//            setVdw(ElementSymbols.Fe, 1.7);
//            setVdw(ElementSymbols.P, 1.7);
//            setVdw(ElementSymbols.Si, 1.8);
//            setVdw(ElementSymbols.Al, 1.84);
//            setVdw(ElementSymbols.Li, 1.8);
//            setVdw(ElementSymbols.Na, 1.8);
//            setVdw(ElementSymbols.Cl, 1.75);
//        }


//        /// <summary>
//        /// Sets the maximum of all the radii.
//        /// </summary>
//        static void SetTotalMaxBondingRadius()
//        {
//            double currentMax = 0;

//            foreach (var e in ElementData)
//            {
//                foreach (var t in e.ThresholdValues)
//                {
//                    if (currentMax < t.Item2)
//                    {
//                        currentMax = t.Item2;
//                    }
//                }
//            }

//            TotalMaxBondingRadius = currentMax;
//        }


//        /// <summary>
//        /// Generates the thresholds of different bond type lengths for each element.
//        /// </summary>
//        static void GenerateThresholds()
//        {
//            Dictionary<ElementSymbol, ElementAndBondInfo.Threshold[]> thresholds = new Dictionary<ElementSymbol, ElementAndBondInfo.Threshold[]>();
//            List<ElementAndBondInfo.Threshold> thresholdsToElement = new List<ElementAndBondInfo.Threshold>();
//            Tuple<BondType, double> t1, t2;

//            HashSet<ElementSymbol> metals = new HashSet<ElementSymbol> { ElementSymbols.Mg, ElementSymbols.K, ElementSymbols.Ca, ElementSymbols.Mn, ElementSymbols.Fe, ElementSymbols.Co, 
//                ElementSymbols.Ni, ElementSymbols.Cu, ElementSymbols.Zn, ElementSymbols.V, ElementSymbols.Mo, ElementSymbols.W };

//            foreach (var e1 in ElementData)
//            {
//                bool m1 = metals.Contains(e1.Name);

//                foreach (var e2 in ElementData)
//                {
//                    bool m2 = metals.Contains(e2.Name);

//                    if (m1 && m2) // two metals form metallic bond
//                    {
//                        t1 = e1.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Metallic);
//                        t2 = e2.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Metallic);

//                        if (t1.Item2 != 0 && t2.Item2 != 0)
//                        {
//                            thresholdsToElement.Add(new ElementAndBondInfo.Threshold(t1.Item2 + t2.Item2, BondType.Metallic));
//                        }
//                    }
//                    else if ((m1 && !m2) || (!m1 && m2)) // metal and non-metal form ionic bond
//                    {
//                        t1 = e1.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Ion);
//                        t2 = e2.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Ion);

//                        if (t1.Item2 != 0 && t2.Item2 != 0)
//                        {
//                            thresholdsToElement.Add(new ElementAndBondInfo.Threshold(t1.Item2 + t2.Item2, BondType.Ion));
//                        }
//                    }
//                    else if (!m1 && !m2) // two non-metals form covalent bond
//                    {
//                        t1 = e1.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Single);
//                        t2 = e2.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Single);

//                        if (t1.Item2 != 0 && t2.Item2 != 0)
//                        {
//                            thresholdsToElement.Add(new ElementAndBondInfo.Threshold(t1.Item2 + t2.Item2, BondType.Single));
//                        }

//                        t1 = e1.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Double);
//                        t2 = e2.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Double);

//                        if (t1.Item2 != 0 && t2.Item2 != 0)
//                        {
//                            thresholdsToElement.Add(new ElementAndBondInfo.Threshold(t1.Item2 + t2.Item2, BondType.Double));
//                        }

//                        t1 = e1.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Triple);
//                        t2 = e2.ThresholdValues.FirstOrDefault(t => t.Item1 == BondType.Triple);

//                        if (t1.Item2 != 0 && t2.Item2 != 0)
//                        {
//                            thresholdsToElement.Add(new ElementAndBondInfo.Threshold(t1.Item2 + t2.Item2, BondType.Triple));
//                        }
//                    }

//                    thresholds.Add(e2.Name, thresholdsToElement.OrderBy(t => t.Value).ToArray());
//                    thresholdsToElement.Clear();
//                }

//                double maxBondingRadius = thresholds.Max(p => p.Value.Length == 0 ? 0.0 : p.Value.Max(t => t.Value));

//                ElementAndBondInfo.elementInfo.Add(e1.Name, new ElementAndBondInfo.ElementInfo(
//                    e1.Valency, e1.AtomicRadius, e1.VdwRadius, maxBondingRadius, ElementColor.Default, thresholds));

//                thresholds.Clear();
//            }
//        }
//    }
//}