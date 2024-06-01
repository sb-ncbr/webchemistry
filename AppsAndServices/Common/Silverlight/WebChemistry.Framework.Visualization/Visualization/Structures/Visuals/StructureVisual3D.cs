using WebChemistry.Framework.Core;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using System;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace WebChemistry.Framework.Visualization
{
    //public abstract class StructureVisual3D : Visual3D
    //{
    //    public Color Color
    //    {
    //        get { return (Color)GetValue(ColorProperty); }
    //        set { SetValue(ColorProperty, value); }
    //    }

    //    public static readonly DependencyProperty ColorProperty =
    //        DependencyProperty.Register("Color", typeof(Color), typeof(StructureVisual3D), new PropertyMetadata(Color.FromArgb(0, 0, 0, 0), OnColorChangedInternal));

    //    static void OnColorChangedInternal(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        //(d as Visual3D).OnOpacityChanged((double)e.NewValue);
    //    }

    //    protected virtual void OnColorChanged(Color newColor)
    //    {

    //    }

    //    StructureRenderMode _renderMode;

    //    public StructureRenderMode RenderMode
    //    {
    //        get { return _renderMode; }
    //        set { _renderMode = value; }
    //    }

    //    StructureModel3D _model;
        
    //    Ellipse[] _atomVisuals;
    //    BondPath3D[] _bondVisuals;

    //    Dictionary<int, Ellipse> _atomVisualsById;

    //    public override bool Equals(object obj)
    //    {
    //        if (obj is StructureVisual3D)
    //        {
    //            return (obj as StructureVisual3D)._model.Structure.Id == _model.Structure.Id;
    //        }
    //        return false;
    //    }

    //    public override int GetHashCode()
    //    {
    //        return _model.Structure.Id.GetHashCode();
    //    }

    //    public void MakeHitTestInvisible()
    //    {
    //        _atomVisuals.Run(a => a.IsHitTestVisible = false);
    //    }


    //    public class StrokeThicknessConverter : IValueConverter
    //    {
    //        #region IValueConverter Members

    //        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //        {
    //            var highlighted = (bool)value;
    //            if (highlighted) return 3.3;
    //            return 1.0;
    //        }

    //        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        #endregion
    //    }

    //    static readonly StrokeThicknessConverter AtomOutlineThicknessConverter = new StrokeThicknessConverter();
               
    //    void Create()
    //    {
    //        var structure = _model.Structure;

    //        _atomVisuals = structure.Atoms.Select(a =>
    //            new Ellipse()
    //            {
    //                Tag = a,
    //                Fill = null
    //            }).ToArray();
        

    //        _atomVisualsById = new Dictionary<int, Ellipse>();

    //      //  _atomOutlines = _structure.Atoms.Select(a => new HighlightableOutlineBrushWrapper(a, _set)).ToArray();

    //        //int i = 0;
    //        //_atomVisuals.Run(a =>
    //        //{
    //        //  //  a.SetBinding(Ellipse.StrokeProperty, new Binding("OutlineBrush") { Source = _atomOutlines[i], Mode = BindingMode.OneWay });
    //        //    a.SetBinding(Ellipse.StrokeThicknessProperty, new Binding("IsHighlighted") { Source = _structure.Atoms[i], Mode = BindingMode.OneWay, Converter = AtomOutlineThicknessConverter });
    //        //    _atomVisualsById[(a.Tag as IAtom).Id] = a;
    //        //    i++;
    //        //});

    //        _bondVisuals = structure.Bonds.Select(b =>
    //            new BondPath3D()
    //            {
    //                IsHitTestVisible = false,
    //                StrokeThickness = 1,
    //                Tag = b,
    //                //   Fill = ChargeBrushes.GetBondBrush(b.A.Property<double>(prop), b.B.Property<double>(prop), chargeRange)
    //            }).ToArray();


    //        //_bondVisuals.Run(b => b.SetBinding(BondPath3D.StrokeProperty, new Binding("OutlineBrush") { Source = _set, Mode = BindingMode.OneWay }));
    //    }

    //    public StructureVisual3D(StructureModel3D model, IStructure structure)
    //    {
    //        _model = model;
    //     //   _structure = structure;

    //        model.Atoms.Run(a =>
    //        {
    //            a.Atom.PropertyChanged += AtomPropertyChanged;
    //        });

    //        Create();
    //    }

    //    void AtomPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    //    {
    //        if (e.PropertyName == "IsHighlighted")
    //        {
    //            IAtom atom = sender as IAtom;

    //            if (atom.IsHighlighted)
    //            {
    //                //atom.Properties["OldZ" + _set.Name] = Canvas.GetZIndex(_atomVisualsById[atom.Id]);
    //                Canvas.SetZIndex(_atomVisualsById[atom.Id], Int16.MaxValue - 150);
    //            }
    //            else
    //            {
    //                //Canvas.SetZIndex(_atomVisualsById[atom.Id], atom.Property<int>("OldZ" + _set.Name));
    //            }
    //        }
    //    }

    //    public void Register(Canvas canvas)
    //    {
    //        _atomVisuals.Run(a => canvas.Children.Add(a));
    //        _bondVisuals.Run(b => canvas.Children.Add(b));
    //    }

    //    public void Render(double offset)
    //    {
    //        //double dX = offset.X;
    //        //double dY = offset.X;

    //        for (int i = 0; i < _atomVisuals.Length; i++)
    //        {
    //            var visual = _atomVisuals[i];
    //            //  var underlay = _atomUnderlays[i];
    //            var model = _model.Atoms[i];
    //           // var direction = _model.AtomDirections[i];


    //            //var boundingBox = model.BoundingBox;

    //            //visual.Width = boundingBox.Width;
    //            //visual.Height = boundingBox.Height;
    //            //visual.SetValue(Canvas.LeftProperty, boundingBox.Left + offset * direction.X);
    //            //visual.SetValue(Canvas.TopProperty, boundingBox.Top + offset * direction.Y);

    //            //int z = model.ZIndex + (int)(offset * direction.dZ);
    //            //if (z > Int16.MaxValue - 1000) z = Int16.MaxValue - 1000;
    //            //visual.SetValue(Canvas.ZIndexProperty, z);

    //            //underlay.Width = boundingBox.Width + 4;
    //            //underlay.Height = boundingBox.Height + 4;
    //            //underlay.SetValue(Canvas.LeftProperty, boundingBox.Left + offset * direction.X - 2);
    //            //underlay.SetValue(Canvas.TopProperty, boundingBox.Top + offset * direction.Y - 2);
    //            //underlay.SetValue(Canvas.ZIndexProperty, model.ZIndex + (int)(offset * direction.dZ) - 1);
    //        }

    //        for (int i = 0; i < _bondVisuals.Length; i++)
    //        {
    //            //var model = _model.Bonds[i];
    //            //var visual = _bondVisuals[i];
    //            //var bondDir = _model.BondDirections[i];
    //            ////var underlay = _bondUnderlays[i];

    //            //if (!model.Visible) return;

    //            //var pA = model.Origin;
    //            //var dA = bondDir.DirectionA;

    //            //var pB = model.Other;
    //            //var dB = bondDir.DirectionB;

    //            //var origin = new Point(pA.X + offset * dA.X, pA.Y + offset * dA.Y);
    //            ////var other = new Point(pB.X + offset * dB.X, pB.Y + offset * dB.Y);
    //            ////double dX = other.X - origin.X;
    //            ////double dY = other.Y - origin.Y;

    //            //double dX = pB.X - pA.X + offset * (dB.X - dA.X);
    //            //double dY = pB.Y - pA.Y + offset * (dB.Y - dA.Y);
    //            //double length = System.Math.Sqrt(dX * dX + dY * dY);
    //            //double angle = MathHelper.RadiansToDegrees(System.Math.Atan2(dY, dX));

    //            //var a = model.A;
    //            //var b = model.B;
    //            ////var radius = model.Radius;

    //            ////double r = radius / b.Radius;
    //            //double width = model.Radius * (a.BoundingBox.Width / a.Radius + b.BoundingBox.Width / b.Radius) / 2;

    //            //int z;

    //            //int zA = a.ZIndex + (int)(offset * dA.dZ);
    //            //int zB = b.ZIndex + (int)(offset * dB.dZ);
    //            //int zpA = zA - dA.dZ;
    //            //int zpB = zB - dB.dZ;

    //            //if (dA.dZ > 0)
    //            //{
    //            //    int bottom = System.Math.Max(zpA, zpB);
    //            //    int top = System.Math.Min(zA, zB);
    //            //    z = (bottom + top) / 2;
    //            //}
    //            //else
    //            //{
    //            //    int bottom = System.Math.Min(zA, zB);
    //            //    int top = System.Math.Min(zpA, zpB);
    //            //    z = (bottom + top) / 2;
    //            //}

    //            //if (z > Int16.MaxValue - 1000) z = Int16.MaxValue - 1000;
    //            //visual.Update(origin, width, length, angle, z);
    //            //  underlay.Update(origin, width + 4, length, angle, z - 1);

    //            //var model = _model.Bonds[i];
    //            //var visual = _bondVisuals[i];
    //            //var bondDir = _model.BondDirections[i];

    //            //var a = model.A;
    //            //var b = model.B;
    //            //var aT = a.TransformedCenter;
    //            //var bT = b.TransformedCenter;

    //            //double tdX = (bT.X - aT.X);
    //            //double tdY = (bT.Y - aT.Y);

    //            //if (tdX * tdX + tdY * tdY < 0.1)
    //            //{
    //            //    continue;
    //            //}

    //            //var atomDistance = model.Length;

    //            //var distanceRatio = 0.9 / atomDistance;

    //            //var aR = a.Radius * distanceRatio;
    //            //var bR = b.Radius * distanceRatio;

    //            //var pA = new Point(aT.X + tdX * aR, aT.Y + tdY * aR);
    //            //var dA = bondDir.DirectionA;
    //            //var pB = new Point(bT.X - tdX * bR, bT.Y - tdY * bR);
    //            //var dB = bondDir.DirectionB;

    //            ////var origin = new Point(pA.X + offset * dA.X, pA.Y + offset * dA.Y);

    //            ////double dX = (bT.X - tdX * bR) - (aT.X + tdX * aR) + offset * (dB.X - dA.X);
    //            ////double dY = (bT.Y - tdY * bR) - (aT.Y + tdY * aR) + offset * (dB.Y - dA.Y);

    //            ////double dX = (bT.X - aT.X) - tdX * (bR + aR) + offset * (dB.X - dA.X);
    //            ////double dY = (bT.Y - aT.Y) - tdY * (bR + aR) + offset * (dB.Y - dA.Y);

    //            //double dX = pB.X - pA.X + offset * (dB.X - dA.X);
    //            //double dY = pB.Y - pA.Y + offset * (dB.Y - dA.Y);

    //            //double length = System.Math.Sqrt(dX * dX + dY * dY);
    //            //double angle = MathHelper.RadiansToDegrees(System.Math.Atan2(dY, dX));
    //            //double width = 0.5 * model.Radius * (a.BoundingBox.Width / a.Radius + b.BoundingBox.Width / b.Radius);

    //            //int z;

    //            //int zA = a.ZIndex + (int)(offset * dA.dZ);
    //            //int zB = b.ZIndex + (int)(offset * dB.dZ);
    //            //int zpA = zA - dA.dZ;
    //            //int zpB = zB - dB.dZ;

    //            //if (dA.dZ > 0)
    //            //{
    //            //    int bottom = System.Math.Max(zpA, zpB);
    //            //    int top = System.Math.Min(zA, zB);
    //            //    z = (bottom + top) / 2;
    //            //}
    //            //else
    //            //{
    //            //    int bottom = System.Math.Min(zA, zB);
    //            //    int top = System.Math.Min(zpA, zpB);
    //            //    z = (bottom + top) / 2;
    //            //}

    //            //if (z > Int16.MaxValue - 1000) z = Int16.MaxValue - 1000;
    //            ////visual.Update(origin, width, length, angle, z);

    //            //visual.Update(width, length, angle);
    //            //visual.SetValue(Canvas.LeftProperty, (pA.X + offset * dA.X) - width / 4);
    //            //visual.SetValue(Canvas.TopProperty, (pA.Y + offset * dA.Y) - width / 2);
    //            //visual.SetValue(Canvas.ZIndexProperty, z);
    //        }
    //    }

    //    public override void Dispose()
    //    {
    //        if (_atomVisuals != null)
    //        {
    //            for (int i = 0; i < _atomVisuals.Length; i++) 
    //            {
    //                _atomVisuals[i].Fill = null;
    //                _atomVisuals[i].Stroke = null; 
    //                _atomVisuals[i] = null; 
    //            } 
    //            _atomVisuals = null;
    //        }
            
    //        if (_bondVisuals != null)
    //        {
    //            for (int i = 0; i < _bondVisuals.Length; i++) 
    //            {
    //                _bondVisuals[i].Fill = null; 
    //                _bondVisuals[i].Stroke = null;
    //                _bondVisuals[i] = null; 
    //            }
    //            _bondVisuals = null;
    //        }

    //        if (_model != null)
    //        {
    //            _model.Atoms.Run(a => a.Atom.PropertyChanged -= AtomPropertyChanged);
    //            _model = null;
    //        }
    //    }
    //}
}