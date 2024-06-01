using System.Windows;
using System.Windows.Media;

namespace WebChemistry.Framework.Visualization
{
    public class AtomMaterial
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

        RadialGradientBrush _brush;

        public RadialGradientBrush Brush { get { return _brush; } }

        void UpdateColor()
        {
            _brush.GradientStops[0].Color = _color;
        }

        public AtomMaterial(Color color)
        {
            _color = color;

            _brush = new RadialGradientBrush();
            _brush.Center = new Point(0.7, 0.3);
            _brush.GradientOrigin = new Point(0.7, 0.3);
            _brush.RadiusX = 1.8;
            _brush.RadiusY = 1.8;
            _brush.GradientStops.Add(new GradientStop() { Color = _color, Offset = 0 });
            _brush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0xFF, 0x38, 0x38, 0x38), Offset = 1 });
        }
    }
}
