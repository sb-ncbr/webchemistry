
namespace WebChemistry.Framework.Geometry
{
    using WebChemistry.Framework.Math;

    class K3DRect
    {
        public double MinX = double.MinValue, MinY = double.MinValue, MinZ = double.MinValue;
        public double MaxX = double.MaxValue, MaxY = double.MaxValue, MaxZ = double.MaxValue;

        public double DistanceSquaredToClosestEdge(Vector3D v)
        {
            double x = 0, y = 0, z = 0;

            if (v.X <= MinX) x = v.X - MinX;
            else if (v.X >= MaxX) x = v.X - MaxX;

            if (v.Y <= MinY) y = v.Y - MinY;
            else if (v.Y >= MaxY) y = v.Y - MaxY;

            if (v.Z <= MinZ) z = v.Z - MinZ;
            else if (v.Z >= MaxZ) z = v.Z - MaxZ;

            return x * x + y * y + z * z;
        }
    }
}
