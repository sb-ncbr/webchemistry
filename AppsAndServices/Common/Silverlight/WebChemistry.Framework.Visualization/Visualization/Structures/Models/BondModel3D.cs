using System.Windows.Media;
using WebChemistry.Framework.Math;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows;
using WebChemistry.Framework.Core;

namespace WebChemistry.Framework.Visualization
{
    public class BondModel3D : Model3D
    {
        IBond _bond;

        public IBond Bond { get { return _bond; } }

        public AtomModel3D A { get; private set; }
        public AtomModel3D B { get; private set; }

        double _radius = 0.095;

        public double Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        double _length;

        public double Length
        {
            get { return _length; }
        }

        Point _origin;

        public Point Origin
        {
            get { return _origin; }

         //   get { return A.TransformedCenter; }
        }

        Point _other;

        public Point Other
        {
            get { return _other; }
          //  get { return B.TransformedCenter; }
        }

        double _angle;

        public double Angle
        {
            get { return _angle; }
        }

        bool _visible;

        public bool Visible
        {
            get { return _visible; }
        }
        
        public override void Update(UpdateContext context)
        {
            _length = A.Center.DistanceTo(B.Center);

            //var cA = context.WorldTransform.Transform(A.Center);
            //var cB = context.WorldTransform.Transform(B.Center);

            //Vector3D dir = cB - cA;

            //if (dir.LengthSquared < 0.01)
            //{
            //    _visible = false;
            //    return;
            //}

            //_visible = true;

            ////var center = cA + 0.5 * dir;
            //dir.Normalize();

            //double rA = 0.9 * A.Radius;
            //double rB = 0.9 * B.Radius;

            //Point3D iA = new Point3D(cA.X + rA * dir.X, cA.Y + rA * dir.Y, cA.Z + rA * dir.Z);
            //Point3D iB = new Point3D(cB.X - rB * dir.X, cB.Y - rB * dir.Y, cB.Z - rB * dir.Z);

            //Point3D tA = iA.PerspectiveTransform(context);
            //Point3D tB = iB.PerspectiveTransform(context);

            //double dX = tB.X - tA.X;
            //double dY = tB.Y - tA.Y;
            //_length = System.Math.Sqrt(dX * dX + dY * dY);
            //_angle = MathHelper.RadiansToDegrees(System.Math.Atan2(dY, dX));
            //_origin.X = tA.X;
            //_origin.Y = tA.Y;
            //_other.X = tB.X;
            //_other.Y = tB.Y;

            //switch (MathHelper.AngleQuadrant(_angle + 45))
            //{
            //    case 1:
            //    case 2:
            //        _origin = pA;
            //        _other = pB;
            //        break;
            //    case 3:
            //    default: // 4
            //        _origin = pA; // pB
            //        _other = pB;
            //        //_angle += 180;
            //        break;
            //}
        }

        public override void UpdateAsync(UpdateAsyncArgs args)
        {
            Update(args.Context);
        }


        public override void Dispose()
        {
        }

        public BondModel3D(AtomModel3D a, AtomModel3D b, IBond bond)
        {
            A = a;
            B = b;
            _bond = bond;
        }
    }
}