namespace WebChemistry.Framework.Math
{
    public struct Line3D
    {
        public readonly Vector3D Origin;
        public readonly Vector3D Direction;

        public Vector3D Interpolate(double t)
        {
            return Origin + Direction * t;
        }

        public static Line3D Create(Vector3D a, Vector3D b)
        {
            return new Line3D(a, b);
        }

        public Line3D(Vector3D a, Vector3D b)
        {
            this.Origin = a;
            this.Direction = (b - a).Normalize();
        }
    }
}