using System.Windows.Shapes;
using WebChemistry.Framework.Math;
using System.Windows.Controls;
using System.Windows;
using System;
using System.Windows.Media;

namespace WebChemistry.Framework.Visualization
{
    public abstract class Model3D : IDisposable
    {               
        protected Rect _boundingBox;
        public Rect BoundingBox { get { return _boundingBox; } }

        /// <summary>
        /// Recalculates the model
        /// </summary>
        /// <param name="context">Render context</param>
        public abstract void Update(UpdateContext context);

        public abstract void UpdateAsync(UpdateAsyncArgs args);

        public virtual void PostProcess()
        {
        }

        #region IDisposable Members

        public abstract void Dispose();

        #endregion
    }
}
