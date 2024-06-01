using WebChemistry.Framework.Core;
using System;

namespace WebChemistry.Framework.Visualization
{
    public class RenderingParameters
    {
        public bool ShowAnnotations { get; set; }
        public Func<IAtom, string> AtomAnnotation { get; set; }
    }
}