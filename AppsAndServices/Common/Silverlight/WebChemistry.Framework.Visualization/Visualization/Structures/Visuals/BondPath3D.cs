using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using WebChemistry.Framework.Math;
using System;

namespace WebChemistry.Framework.Visualization
{
    public class BondPath3D : Path
    {
        PathFigure _figure;
        LineSegment _top;
        ArcSegment _rightArc;
        LineSegment _bottom;
        ArcSegment _leftArc;

        RotateTransform _rotation;

        public void Update(double width, double length, double angle)
        {
            double halfWidth = width / 2;
            double quarterWidth = halfWidth / 2;

            _figure.StartPoint = new Point(quarterWidth, 0);
            _top.Point = new Point(quarterWidth + length, 0);
            _rightArc.Size = new Size(quarterWidth, halfWidth);
            _rightArc.Point = new Point(quarterWidth + length, width);
            _bottom.Point = new Point(quarterWidth, width);
            _leftArc.Size = new Size(quarterWidth, halfWidth);
            _leftArc.Point = new Point(quarterWidth, 0);

            this.RenderTransformOrigin = new Point(quarterWidth / (length + halfWidth), 0.5);
            _rotation.Angle = angle;
        }

        public BondPath3D()
        {
            this.IsHitTestVisible = false;

            PathGeometry geometry = new PathGeometry();

            _top = new LineSegment();
            _bottom = new LineSegment();
            _rightArc = new ArcSegment();
            _leftArc = new ArcSegment();
            _figure = new PathFigure();

            _figure.Segments.Add(_top);
            _figure.Segments.Add(_rightArc);
            _figure.Segments.Add(_bottom);
            _figure.Segments.Add(_leftArc);

            _leftArc.SweepDirection = SweepDirection.Clockwise;
            _leftArc.IsLargeArc = false;
            _rightArc.SweepDirection = SweepDirection.Clockwise;
            _rightArc.IsLargeArc = false;

            _figure.IsClosed = true;
            _figure.IsFilled = true;

            geometry.Figures.Add(_figure);

            this.Data = geometry;

            this.RenderTransformOrigin = new Point(0, 0.5);
            _rotation = new RotateTransform() { };
            this.RenderTransform = _rotation;
        }
    }
}