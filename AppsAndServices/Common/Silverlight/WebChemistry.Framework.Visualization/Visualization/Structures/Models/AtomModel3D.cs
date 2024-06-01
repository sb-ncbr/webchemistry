using System.Windows.Media;
using WebChemistry.Framework.Math;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows;
using System;
using WebChemistry.Framework.Core;
using System.Windows.Data;

namespace WebChemistry.Framework.Visualization
{
    public class AtomModel3D : Model3D
    {
        IAtom _atom;

        public IAtom Atom { get { return _atom; } }               

        private double _radius = 0.19;

        public double Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        public Vector3D Center 
        {
            get { return _atom.Position; }
        }

        Point _transformedCenter;

        public Point TransformedCenter
        {
            get { return _transformedCenter; }
        }
        
        int _zIndex;

        public int ZIndex { get { return _zIndex; } }
           
        public override void Update(UpdateContext context)
        {
            Vector3D centerWorld = context.WorldTransform.Transform(Center);

            Vector3D cd = centerWorld - context.CameraPosition;
            Vector3D rd = Vector3D.CrossProduct(cd, context.CameraUpVector);
            rd = rd.ScaleTo(_radius);
            Vector3D radiusPoint = new Vector3D(centerWorld.X + rd.X, centerWorld.Y + rd.Y, centerWorld.Z + rd.Z);
            var trRadiusPoint = radiusPoint.PerspectiveToScreenTransform(context);


            Point center = centerWorld.PerspectiveToScreenTransform(context);

            //var w = context.WorldTransform * context.WorldToNdc;
            //Point3D center = Center.PerspectiveTransform(ref w, ref context.NdcToScreen);

            double radius = trRadiusPoint.DistanceTo(center);

            double distanceToCameraSq = centerWorld.DistanceToSquared(context.CameraPosition);

           // double radius1 = _radius / distanceToCamera * 0.125 * context.ViewportHeight;

            //double radius;

            //if (distanceToCamera >= context.CameraDistance)
            //{
            //    //radius = context.UnitRadius * _radius * distanceToCamera / context.CameraDistance;
            //    //radius = context.UnitRadius * _radius * (1 + (context.CameraDistance - distanceToCamera) / context.CameraDistance);
            //}
            //else
            //{
            //    //radius = context.UnitRadius * _radius * (1 + (context.CameraDistance - distanceToCamera) / context.CameraDistance);
            //}

            //radius = context.UnitRadius * _radius;

            _transformedCenter = center;
            _boundingBox.X = center.X - radius;
            _boundingBox.Y = center.Y - radius;
            _boundingBox.Width = 2 * radius;
            _boundingBox.Height = 2 * radius;
            _zIndex = (int)(-100 * distanceToCameraSq);

            //if (_zIndex > Int16.MaxValue - 1000) _zIndex = Int16.MaxValue - 1000;
        }

        public override void UpdateAsync(UpdateAsyncArgs args)
        {
            Update(args.Context);
        }
                        
        public override void Dispose()
        {
        }

        public AtomModel3D(IAtom atom)
        {
            _atom = atom;
        }
    }
}