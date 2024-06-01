namespace WebChemistry.Framework.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using WebChemistry.Framework.Math;
    
    /// <summary>
    /// Triangulation 3D interface.
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TCell"></typeparam>
    public interface ITriangulation3D<TVertex, TCell>
        where TCell : TriangulationCell3D<TVertex, TCell>, new()
    {
         ReadOnlyCollection<TCell> Cells { get; }
    }

    /// <summary>
    /// Factory class for creating triangulations.
    /// </summary>
    public static class Triangulation3D
    {        
        public static ITriangulation3D<TVertex, TFace> CreateDelaunay<TVertex, TFace>(IEnumerable<TVertex> data, Func<TVertex, Vector3D> positionSelector)
            where TFace : TriangulationCell3D<TVertex, TFace>, new()
        {
            return DelaunayTriangulation3D<TVertex, TFace>.Create(data, positionSelector);
        }
    }
}
