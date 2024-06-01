using WebChemistry.Framework.Math;

namespace WebChemistry.Framework.Visualization
{
    public class UpdateContext
    {
        public Matrix3D WorldTransform;
        public Matrix3D WorldToNdc;
        public Matrix3D NdcToScreen;
        public Vector3D CameraPosition;
        public Vector3D CameraUpVector;
    }
}