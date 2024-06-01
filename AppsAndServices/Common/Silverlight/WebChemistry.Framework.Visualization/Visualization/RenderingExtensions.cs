using WebChemistry.Framework.Visualization;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Math;
using System;

namespace WebChemistry.Framework.Visualization
{
    public static class RenderingExtensions
    {
        public static Point PerspectiveToScreenTransform(this Vector3D point, UpdateContext context)
        {
            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            //Point3D p = new Point3D(x * context.WorldToNdc.M11 + y * context.WorldToNdc.M21 + z * context.WorldToNdc.M31 + context.WorldToNdc.OffsetX,
            //                        x * context.WorldToNdc.M12 + y * context.WorldToNdc.M22 + z * context.WorldToNdc.M32 + context.WorldToNdc.OffsetY,
            //                        x * context.WorldToNdc.M13 + y * context.WorldToNdc.M23 + z * context.WorldToNdc.M33 + context.WorldToNdc.OffsetZ);

            double tX = x * context.WorldToNdc.M11 + y * context.WorldToNdc.M21 + z * context.WorldToNdc.M31 + context.WorldToNdc.OffsetX;
            double tY = x * context.WorldToNdc.M12 + y * context.WorldToNdc.M22 + z * context.WorldToNdc.M32 + context.WorldToNdc.OffsetY;
            double tZ = x * context.WorldToNdc.M13 + y * context.WorldToNdc.M23 + z * context.WorldToNdc.M33 + context.WorldToNdc.OffsetZ;
            double w = x * context.WorldToNdc.M14 + y * context.WorldToNdc.M24 + z * context.WorldToNdc.M34 + context.WorldToNdc.M44;
            double wInverse = 1.0 / w;
            tX *= wInverse;
            tY *= wInverse;
            tZ *= wInverse;

            //return context.NdcToScreen.Transform(p);
            //_screenToViewTransform.M11 = 2 / _width;
            //_screenToViewTransform.M22 = -2 / (_height * _camera.AspectRatio);
            //_screenToViewTransform.M32 = 1 / _camera.AspectRatio;
            //_screenToViewTransform.M33 = -depth;

            return new Point(
                tX * context.NdcToScreen.M11 + context.NdcToScreen.OffsetX,
                tY * context.NdcToScreen.M22 + tZ * context.NdcToScreen.M32 + context.NdcToScreen.OffsetY);

        }

        public static double DistanceTo(this Point a, Point b)
        {
            double dX = a.X - b.X;
            double dY = a.Y - b.Y;
            return System.Math.Sqrt(dX * dX + dY * dY);
        }         
    }
}