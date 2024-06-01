using System.Windows;
using System.Windows.Media;

namespace WebChemistry.Framework.Visualization
{
    public class StructureMaterial 
    {
        Color _color;
        public Color Color
        {
            get { return _color; }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    UpdateColor();
                }
            }
        }

        RadialGradientBrush _upBrush, _bottomBrush;

        public RadialGradientBrush UpBrush { get { return _upBrush; } }
        public RadialGradientBrush BottomBrush { get { return _bottomBrush; } }
        public SolidColorBrush Brush { get; private set; }

        public AtomMaterial AtomMaterial { get; private set; }

        void UpdateColor()
        {
            _upBrush.GradientStops[1].Color = _color;
            _bottomBrush.GradientStops[1].Color = _color;
            AtomMaterial.Color = _color;
            Brush.Color = _color;
        }

        public StructureMaterial(Color color)
        {
            AtomMaterial = new AtomMaterial(color);

            _color = color;

            Brush = new SolidColorBrush(color);

            _upBrush = new RadialGradientBrush();
            _upBrush.Center = new Point(0.5, 0.35);
            _upBrush.GradientOrigin = new Point(0.5, 0.25);
            _upBrush.RadiusX = 4;
            _upBrush.RadiusY = 0.25;
            _upBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0xFF, 0xCA, 0xCA, 0xCA), Offset = 0 });
            _upBrush.GradientStops.Add(new GradientStop() { Color = _color, Offset = 1 });

            _bottomBrush = new RadialGradientBrush();
            _bottomBrush.Center = new Point(0.5, 0.65);
            _bottomBrush.GradientOrigin = new Point(0.5, 0.75);
            _bottomBrush.RadiusX = 4;
            _bottomBrush.RadiusY = 0.25;
            _bottomBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0xFF, 0xCA, 0xCA, 0xCA), Offset = 0 });
            _bottomBrush.GradientStops.Add(new GradientStop() { Color = _color, Offset = 1 });           
        }
    }
}
