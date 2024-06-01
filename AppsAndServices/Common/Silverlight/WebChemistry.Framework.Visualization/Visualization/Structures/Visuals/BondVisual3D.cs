namespace WebChemistry.Framework.Visualization
{
    public abstract class BondVisual3D : VisualElement3D
    {
        BondModel3D _model;

        public BondModel3D Model { get { return _model; } }
        
        protected BondVisual3D(BondModel3D model)
        {
            _model = model;
        }
    }
}