using System.Windows;
using System;

namespace WebChemistry.Framework.Visualization
{
    public abstract class VisualElement3D : IDisposable
    {       
        public abstract void Render(RenderContext context);
        public abstract void Register(Viewport3DBase viewport);
        
        public abstract void Dispose();
    }
}
