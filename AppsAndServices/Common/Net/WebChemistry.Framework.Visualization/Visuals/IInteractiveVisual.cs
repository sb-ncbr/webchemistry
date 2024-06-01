using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using System.Windows.Media.Media3D;
using System.Windows.Input;

namespace WebChemistry.Framework.Visualization.Visuals
{
    public interface IInteractiveVisual
    {
        bool IsHitTestVisible { get; }
        Key[] ActivationKeys { get; }
        IInteractive GetInteractiveElement(RayMeshGeometry3DHitTestResult ray);
    }
}
