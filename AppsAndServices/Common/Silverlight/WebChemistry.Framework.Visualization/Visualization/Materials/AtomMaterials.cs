using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WebChemistry.Framework.Core;

namespace WebChemistry.Framework.Visualization
{
    public static class AtomMaterials
    {
        public static readonly AtomMaterial StandardMaterial = new AtomMaterial(Colors.LightGray);
        public static readonly AtomMaterial SelectedMaterial = new AtomMaterial(Colors.Red);
        public static readonly AtomMaterial HighlightedMaterial = new AtomMaterial(Colors.Yellow);

        public static readonly RadialGradientBrush OverlayBrush;

       // public static readonly LinearGradientBrush HighlightBondBrush;
     //   public static readonly LinearGradientBrush SelectBondBrush;
        public static readonly SolidColorBrush HighlightBondBrush;
        //public static readonly SolidColorBrush SelectBondBrush;

        public static readonly StructureMaterial SelectBondMaterial = new StructureMaterial(Colors.Red);

        static Dictionary<ElementSymbol, AtomMaterial> ElementMaterials = new Dictionary<ElementSymbol, AtomMaterial>();

        public static AtomMaterial GetAtomMaterial(IAtom atom)
        {
            AtomMaterial m;
            if (ElementMaterials.TryGetValue(atom.ElementSymbol, out m)) return m;
            return StandardMaterial;
        }

        static AtomMaterials()
        {
            OverlayBrush = new RadialGradientBrush();
            OverlayBrush.Center = new Point(0.7, 0.3);
            OverlayBrush.GradientOrigin = new Point(0.7, 0.3);
            OverlayBrush.RadiusX = 0.65;
            OverlayBrush.RadiusY = 0.65;
            OverlayBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Offset = 0 });
            OverlayBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0x7F, 0xFF, 0xFF, 0xFF), Offset = 0.15 });
            OverlayBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), Offset = 1 });

            HighlightBondBrush = new SolidColorBrush(Colors.Yellow);

            foreach (var e in ElementSymbols.All)
            {
                var color = ElementAndBondInfo.GetElementInfo(e).Color;
                ElementMaterials.Add(e, new AtomMaterial(Color.FromArgb(255, color.R, color.G, color.B)));
            }
            

         //   SelectBondBrush = new SolidColorBrush(Colors.Red);

            //HighlightBondBrush = new LinearGradientBrush();
            //HighlightBondBrush.GradientStops.Add(new GradientStop() { Color = Colors.Yellow, Offset = 0 });
            //HighlightBondBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0x00, 0xFF, 0xFF, 0x00), Offset = 1 });

            //SelectBondBrush = new LinearGradientBrush();
            //SelectBondBrush.GradientStops.Add(new GradientStop() { Color = Colors.Red, Offset = 0 });
            //SelectBondBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0x00, 0xFF, 0x00, 0x00), Offset = 1 });
        }
    }
}
