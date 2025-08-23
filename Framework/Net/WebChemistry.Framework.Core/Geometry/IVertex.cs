namespace WebChemistry.Framework.Geometry
{
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Represents an object with a position.
    /// </summary>
    public interface IVertex
    {
        /// <summary>
        /// Position of the vertex.
        /// </summary>
        Vector Position { get; }
    }

    /// <summary>
    /// Represents an object with a 3D position.
    /// </summary>
    public interface IVertex3D
    {
        /// <summary>
        /// Position of the vertex.
        /// </summary>
        Vector3D Position { get; }
    }
}