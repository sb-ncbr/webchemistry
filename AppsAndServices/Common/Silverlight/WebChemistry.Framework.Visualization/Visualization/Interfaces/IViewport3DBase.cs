using System;

namespace WebChemistry.Framework.Visualization
{
    interface IViewport3DBase : IDisposable
    {
        ICamera Camera { get; }

        void RegisterVisualElement(IVisualElement visual);

        void CancelRender();
        void Render();

        void Clear();

        bool IsRendering { get; }
        bool SuspendRender { get; set; }
    }
}