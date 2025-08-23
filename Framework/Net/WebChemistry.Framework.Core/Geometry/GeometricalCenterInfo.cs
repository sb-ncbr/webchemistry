namespace WebChemistry.Framework.Geometry
{
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Represents a geometrical center and the bounding sphere radius of a structure.
    /// </summary>
    public struct GeometricalCenterInfo
    {
        Vector3D center;
        /// <summary>
        /// Gets the center.
        /// </summary>
        public Vector3D Center { get { return this.center; } }

        double radius;
        /// <summary>
        /// Gets the radius.
        /// </summary>
        public double Radius { get { return this.radius; } }
        
        /// <summary>
        /// Creates the struct.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        public GeometricalCenterInfo(Vector3D center, double radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
}