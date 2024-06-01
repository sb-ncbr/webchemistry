using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Queries.Silverlight.Visuals;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Geometry;

namespace WebChemistry.Queries.Silverlight.DataModel
{
    /// <summary>
    /// Visual...
    /// </summary>
    public interface IVisual : IInteractive
    {
        MotiveVisual3D Visual { get; }
        void ClearVisual();
        GeometricalCenterInfo CenterInfo { get; }
    }
}
