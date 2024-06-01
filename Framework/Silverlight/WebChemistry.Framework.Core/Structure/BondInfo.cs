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
        public static void SetDefault()
        {
            #region dataset1
            //elementInfo.Add(ElementSymbols.H, new ElementInfo(1, 0, 0, 1.0, ElementColor.Parse("#800080"), 1.42));
            //elementInfo.Add(ElementSymbols.O, new ElementInfo(2, 0, 0, 1.45, ElementColor.Parse("#FF0000")));
            //elementInfo.Add(ElementSymbols.S, new ElementInfo(4, 0, 0, 1.77, ElementColor.Parse("#FFFF00"), 2.3));
            //elementInfo.Add(ElementSymbols.N, new ElementInfo(4, 0, 0, 1.55, ElementColor.Parse("#0000FF")));
            //elementInfo.Add(ElementSymbols.C, new ElementInfo(4, 0, 0, 1.61, ElementColor.Parse("#D3D3D3"), 1.9));

            //elementInfo.Add(ElementSymbols.Fe, new ElementInfo(8, 0, 0, 1.7, ElementColor.Parse("#12AA12"), 2.8));
            //elementInfo.Add(ElementSymbols.P, new ElementInfo(5, 0, 0, 1.9, ElementColor.Parse("#98984E"), 2.3));
            //elementInfo.Add(ElementSymbols.Si, new ElementInfo(8, 0, 0, 1.8, ElementColor.Default));
            //elementInfo.Add(ElementSymbols.Al, new ElementInfo(8, 0, 0, 1.84, ElementColor.Default, 2.8));
            //elementInfo.Add(ElementSymbols.Li, new ElementInfo(8, 0, 0, 1.8, ElementColor.Default, 2.8));
            //elementInfo.Add(ElementSymbols.Na, new ElementInfo(8, 0, 0, 1.8, ElementColor.Default, 2.8));
            //elementInfo.Add(ElementSymbols.Cl, new ElementInfo(4, 0, 0, 1.75, ElementColor.Default));
            ////  elementInfo.Add(ElementSymbols.As, new ElementInfo(4, 0, 0, 1.85, ElementColor.Parse("#D3D3D3"), 2.31));

            //Action<ElementSymbol, Threshold[]> addBondInfo = (s, ts) =>
            //{
            //    if (!elementBondInfo.ContainsKey(s)) elementBondInfo.Add(s, ts);
            //};

            //foreach (var m in metalAtoms)
            //{
            //    if (!elementInfo.ContainsKey(m))
            //    {
            //        elementInfo.Add(m, new ElementInfo(8, 0, 0, 1.8, ElementColor.Default, 2.8));
            //    }

            //    addBondInfo(m, new Threshold[] { new Threshold(2.8, BondType.Metallic) });
            //}

            //addBondInfo(ElementSymbols.O, new Threshold[] { new Threshold(1.52, BondType.Single) });
            //addBondInfo(ElementSymbols.C, new Threshold[] { new Threshold(1.65, BondType.Single) });
            //addBondInfo(ElementSymbols.P, new Threshold[] { new Threshold(1.9, BondType.Single) });
            //addBondInfo(ElementSymbols.N, new Threshold[] { new Threshold(1.55, BondType.Single) });
            //addBondInfo(ElementSymbols.H, new Threshold[] { new Threshold(1.42, BondType.Single) });
            //addBondInfo(ElementSymbols.S, new Threshold[] { new Threshold(1.9, BondType.Single) });
            //addBondInfo(ElementSymbols.Si, new Threshold[] { new Threshold(1.8, BondType.Single) });
            ////addBondInfo(ElementSymbols.Na, new Threshold[] { new Threshold(1.8, BondType.Single) });
            ////addBondInfo(ElementSymbols.Al, new Threshold[] { new Threshold(1.8, BondType.Single) });
            ////addBondInfo(ElementSymbols.Li, new Threshold[] { new Threshold(1.8, BondType.Single) });
            //addBondInfo(ElementSymbols.Cl, new Threshold[] { new Threshold(1.8, BondType.Single) });

            //Func<ElementSymbol, ElementSymbol, ElementPair> createEP = (a, b) => new ElementPair(a, b);

            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.C), new Threshold[] { new Threshold(1.4, BondType.Double)/*, new Threshold(2, BondType.Single) */});
            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.O), new Threshold[] { new Threshold(1.26, BondType.Double) });
            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.N), new Threshold[] { new Threshold(1.27, BondType.Double) });
            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.H), new Threshold[] { new Threshold(1.3, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.S), new Threshold[] { new Threshold(1.9, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.F), new Threshold[] { new Threshold(1.45, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.Cl), new Threshold[] { new Threshold(1.8, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.Br), new Threshold[] { new Threshold(2.05, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.I), new Threshold[] { new Threshold(2.25, BondType.Single) });

            //pairsBondInfo.Add(createEP(ElementSymbols.N, ElementSymbols.H), new Threshold[] { new Threshold(1.3, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.N, ElementSymbols.N), new Threshold[] { new Threshold(1.55, BondType.Single) });

            //pairsBondInfo.Add(createEP(ElementSymbols.O, ElementSymbols.H), new Threshold[] { new Threshold(1.05, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.O, ElementSymbols.O), new Threshold[] { new Threshold(1.6, BondType.Single) });

            //pairsBondInfo.Add(createEP(ElementSymbols.F, ElementSymbols.H), new Threshold[] { new Threshold(1.0, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.F, ElementSymbols.F), new Threshold[] { new Threshold(1.55, BondType.Single) });

            //pairsBondInfo.Add(createEP(ElementSymbols.Cl, ElementSymbols.H), new Threshold[] { new Threshold(1.4, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.Cl, ElementSymbols.Cl), new Threshold[] { new Threshold(2.1, BondType.Single) });

            //pairsBondInfo.Add(createEP(ElementSymbols.I, ElementSymbols.H), new Threshold[] { new Threshold(1.7, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.I, ElementSymbols.I), new Threshold[] { new Threshold(2.8, BondType.Single) });

            //pairsBondInfo.Add(createEP(ElementSymbols.Br, ElementSymbols.H), new Threshold[] { new Threshold(1.5, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.Br, ElementSymbols.Br), new Threshold[] { new Threshold(2.4, BondType.Single) });

            //pairsBondInfo.Add(createEP(ElementSymbols.S, ElementSymbols.S), new Threshold[] { new Threshold(2.3, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.S, ElementSymbols.P), new Threshold[] { new Threshold(2.3, BondType.Single) });
            //pairsBondInfo.Add(createEP(ElementSymbols.P, ElementSymbols.P), new Threshold[] { new Threshold(2.3, BondType.Single) });

            //pairsBondInfo.Add(createEP(ElementSymbols.H, ElementSymbols.H), new Threshold[] { new Threshold(0.8, BondType.Single) });

            ////pairsBondInfo.Add(createEP(ElementSymbols.P, ElementSymbols.N), new Threshold[] { new Threshold(1.9, BondType.Single) });

            //SetDefaultColors();
            #endregion

            #region dataset2
            elementInfo.Add(ElementSymbols.H, new ElementInfo(1, 0, 0, 1.0, ElementColor.Parse("#800080"), 1.42));
            elementInfo.Add(ElementSymbols.D, new ElementInfo(1, 0, 0, 1.0, ElementColor.Parse("#800080"), 1.42));
            elementInfo.Add(ElementSymbols.O, new ElementInfo(3, 0, 0, 1.45, ElementColor.Parse("#FF0000"), 1.9));
            elementInfo.Add(ElementSymbols.S, new ElementInfo(4, 0, 0, 1.77, ElementColor.Parse("#FFFF00"), 2.3));
            elementInfo.Add(ElementSymbols.N, new ElementInfo(4, 0, 0, 1.55, ElementColor.Parse("#0000FF"), 1.9));
            elementInfo.Add(ElementSymbols.C, new ElementInfo(4, 0, 0, 1.61, ElementColor.Parse("#D3D3D3"), 1.9));

            elementInfo.Add(ElementSymbols.Fe, new ElementInfo(6, 0, 0, 1.7, ElementColor.Parse("#12AA12"), 2.8));
            elementInfo.Add(ElementSymbols.P, new ElementInfo(5, 0, 0, 1.9, ElementColor.Parse("#98984E"), 2.3));
            elementInfo.Add(ElementSymbols.Si, new ElementInfo(4, 0, 0, 1.8, ElementColor.Default, 2.11));
            elementInfo.Add(ElementSymbols.Al, new ElementInfo(3, 0, 0, 1.84, ElementColor.Default, 2.8));
            elementInfo.Add(ElementSymbols.Cl, new ElementInfo(4, 0, 0, 1.75, ElementColor.Default));

            //new
            elementInfo.Add(ElementSymbols.As, new ElementInfo(5, 0, 0, 1.85, ElementColor.Default, 2.68));
            elementInfo.Add(ElementSymbols.Br, new ElementInfo(5, 0, 0, 1.85, ElementColor.Default, 2.68));
            elementInfo.Add(ElementSymbols.I, new ElementInfo(7, 0, 0, 1.85, ElementColor.Default, 2.81));
            elementInfo.Add(ElementSymbols.Se, new ElementInfo(6, 0, 0, 1.9, ElementColor.Default, 2.34));
            elementInfo.Add(ElementSymbols.B, new ElementInfo(5, 0, 0, 0.9, ElementColor.Default, 2));
            elementInfo.Add(ElementSymbols.Rh, new ElementInfo(6, 0, 0, 1.49, ElementColor.Default, 2.77));

            elementInfo.Add(ElementSymbols.Mn, new ElementInfo(4, 0, 0, 1.0, ElementColor.Default, 2.81));
            elementInfo.Add(ElementSymbols.W, new ElementInfo(6, 0, 0, 1.39, ElementColor.Default, 2.66));
            elementInfo.Add(ElementSymbols.Hf, new ElementInfo(4, 0, 0, 1.59, ElementColor.Default, 2.8));
            elementInfo.Add(ElementSymbols.Ru, new ElementInfo(10, 0, 0, 1.34, ElementColor.Default, 2.5));
            elementInfo.Add(ElementSymbols.Ir, new ElementInfo(8, 0, 0, 1.47, ElementColor.Default, 2.51));


            //  skupina I
            elementInfo.Add(ElementSymbols.Li, new ElementInfo(1, 0, 0, 1.82, ElementColor.Default, 2));
            elementInfo.Add(ElementSymbols.Na, new ElementInfo(1, 0, 0, 2.27, ElementColor.Default, 2)); // should be 0.95
            elementInfo.Add(ElementSymbols.K, new ElementInfo(1, 0, 0, 2.75, ElementColor.Default, 1));

            //  skupina II
            elementInfo.Add(ElementSymbols.Be, new ElementInfo(6, 0, 0, 1.53, ElementColor.Default, 1.76));
            elementInfo.Add(ElementSymbols.Mg, new ElementInfo(6, 0, 0, 1.73, ElementColor.Default, 2.4));//2.29
            elementInfo.Add(ElementSymbols.Ca, new ElementInfo(6, 0, 0, 2.31, ElementColor.Default, 2.65));
            elementInfo.Add(ElementSymbols.Sr, new ElementInfo(6, 0, 0, 2.49, ElementColor.Default, 2.82));

            //#new
            elementInfo.Add(ElementSymbols.Hg, new ElementInfo(6, 0, 0, 1.0, ElementColor.Default, 3.0));
            elementInfo.Add(ElementSymbols.Pt, new ElementInfo(6, 0, 0, 1.0, ElementColor.Default, 3.24));
            elementInfo.Add(ElementSymbols.Te, new ElementInfo(6, 0, 0, 1.0, ElementColor.Default, 2.2));


            Action<ElementSymbol, Threshold[]> addBondInfo = (s, ts) =>
            {
                if (!elementBondInfo.ContainsKey(s)) elementBondInfo.Add(s, ts);
            };

            foreach (var m in metalAtoms)
            {
                if (!elementInfo.ContainsKey(m))
                {
                    elementInfo.Add(m, new ElementInfo(8, 0, 0, 1.8, ElementColor.Default, 2.8));
                }

                addBondInfo(m, new Threshold[] { new Threshold(2.8, BondType.Metallic) });
            }

            addBondInfo(ElementSymbols.O, new Threshold[] { new Threshold(1.52, BondType.Single) });
            addBondInfo(ElementSymbols.C, new Threshold[] { new Threshold(1.75, BondType.Single) });
            addBondInfo(ElementSymbols.P, new Threshold[] { new Threshold(1.9, BondType.Single) });
            addBondInfo(ElementSymbols.N, new Threshold[] { new Threshold(1.6, BondType.Single) });
            addBondInfo(ElementSymbols.H, new Threshold[] { new Threshold(1.42, BondType.Single) });
            addBondInfo(ElementSymbols.D, new Threshold[] { new Threshold(1.42, BondType.Single) });
            addBondInfo(ElementSymbols.T, new Threshold[] { new Threshold(1.42, BondType.Single) });
            addBondInfo(ElementSymbols.S, new Threshold[] { new Threshold(1.9, BondType.Single) });
            addBondInfo(ElementSymbols.Si, new Threshold[] { new Threshold(1.9, BondType.Single) });
            addBondInfo(ElementSymbols.Cl, new Threshold[] { new Threshold(1.8, BondType.Single) });
            addBondInfo(ElementSymbols.As, new Threshold[] { new Threshold(2.68, BondType.Single) });

            Func<ElementSymbol, ElementSymbol, ElementPair> createEP = (a, b) => new ElementPair(a, b);



            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.C), new Threshold[] { new Threshold(1.25, BondType.Triple), new Threshold(1.4, BondType.Double), new Threshold(1.75, BondType.Single) });



            //pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.C), new Threshold[] { new Threshold(1.4, BondType.Double)/*, new Threshold(2, BondType.Single) */});
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.O), new Threshold[] { new Threshold(1.26, BondType.Double), new Threshold(1.59, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.N), new Threshold[] { new Threshold(1.27, BondType.Double), new Threshold(1.6, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.H), new Threshold[] { new Threshold(1.3, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.S), new Threshold[] { new Threshold(2.0, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.F), new Threshold[] { new Threshold(1.45, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.Cl), new Threshold[] { new Threshold(1.9, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.N, ElementSymbols.H), new Threshold[] { new Threshold(1.3, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.N, ElementSymbols.N), new Threshold[] { new Threshold(1.55, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.O, ElementSymbols.H), new Threshold[] { new Threshold(1.05, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.O, ElementSymbols.O), new Threshold[] { new Threshold(1.6, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.F, ElementSymbols.H), new Threshold[] { new Threshold(1.0, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.F, ElementSymbols.F), new Threshold[] { new Threshold(1.55, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.Cl, ElementSymbols.H), new Threshold[] { new Threshold(1.4, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Cl, ElementSymbols.Cl), new Threshold[] { new Threshold(2.1, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.S, ElementSymbols.S), new Threshold[] { new Threshold(2.3, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.S, ElementSymbols.P), new Threshold[] { new Threshold(2.3, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.P, ElementSymbols.P), new Threshold[] { new Threshold(2.3, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.H, ElementSymbols.H), new Threshold[] { new Threshold(0.8, BondType.Single) });

            //new
            pairsBondInfo.Add(createEP(ElementSymbols.As, ElementSymbols.C), new Threshold[] { new Threshold(2.6, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.As, ElementSymbols.S), new Threshold[] { new Threshold(2.68, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.As, ElementSymbols.O), new Threshold[] { new Threshold(1.7, BondType.Single), new Threshold(1.93, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.I, ElementSymbols.O), new Threshold[] { new Threshold(1.68, BondType.Double), new Threshold(1.72, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.H, ElementSymbols.I), new Threshold[] { new Threshold(1, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Hg, ElementSymbols.I), new Threshold[] { new Threshold(2.81, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.I, ElementSymbols.I), new Threshold[] { new Threshold(2.73, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.I), new Threshold[] { new Threshold(2.48, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.Br, ElementSymbols.O), new Threshold[] { new Threshold(1.53, BondType.Double), new Threshold(1.62, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.N, ElementSymbols.Br), new Threshold[] { new Threshold(2.06, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.H, ElementSymbols.Br), new Threshold[] { new Threshold(1, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Br, ElementSymbols.Pd), new Threshold[] { new Threshold(2.44, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Hg, ElementSymbols.Br), new Threshold[] { new Threshold(2.87, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Pt, ElementSymbols.Br), new Threshold[] { new Threshold(2.84, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Ta, ElementSymbols.Br), new Threshold[] { new Threshold(2.63, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.Br), new Threshold[] { new Threshold(2.1, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.Se, ElementSymbols.Se), new Threshold[] { new Threshold(2.34, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Se, ElementSymbols.C), new Threshold[] { new Threshold(1.82, BondType.Double), new Threshold(2.27, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.S, ElementSymbols.Se), new Threshold[] { new Threshold(2.33, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.O, ElementSymbols.Se), new Threshold[] { new Threshold(1.8, BondType.Double), new Threshold(2.05, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Se, ElementSymbols.H), new Threshold[] { new Threshold(1.54, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.B, ElementSymbols.F), new Threshold[] { new Threshold(1.36, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.P, ElementSymbols.B), new Threshold[] { new Threshold(1.49, BondType.Double), new Threshold(1.98, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.B, ElementSymbols.N), new Threshold[] { new Threshold(1.56, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.B, ElementSymbols.H), new Threshold[] { new Threshold(1.31, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.B, ElementSymbols.B), new Threshold[] { new Threshold(1.84, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.B), new Threshold[] { new Threshold(1.88, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.B, ElementSymbols.O), new Threshold[] { new Threshold(1.68, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.F, ElementSymbols.Be), new Threshold[] { new Threshold(1.63, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.O, ElementSymbols.Be), new Threshold[] { new Threshold(1.76, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.Mg, ElementSymbols.N), new Threshold[] { new Threshold(2.4, BondType.Metallic) });//2.29
            pairsBondInfo.Add(createEP(ElementSymbols.Mg, ElementSymbols.O), new Threshold[] { new Threshold(2.24, BondType.Metallic) });
            pairsBondInfo.Add(createEP(ElementSymbols.Mg, ElementSymbols.F), new Threshold[] { new Threshold(2.02, BondType.Metallic) });

            //new II
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.Hg), new Threshold[] { new Threshold(2.36, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Hg, ElementSymbols.S), new Threshold[] { new Threshold(2.75, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.Pt, ElementSymbols.N), new Threshold[] { new Threshold(2.11, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.Pt, ElementSymbols.O), new Threshold[] { new Threshold(2.6, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.O, ElementSymbols.Te), new Threshold[] { new Threshold(2.1, BondType.Single) });
            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.Te), new Threshold[] { new Threshold(2.14, BondType.Single) });

            pairsBondInfo.Add(createEP(ElementSymbols.C, ElementSymbols.Si), new Threshold[] { new Threshold(1.91, BondType.Single) });

            SetDefaultColors();

            #endregion

        }
    }
}
