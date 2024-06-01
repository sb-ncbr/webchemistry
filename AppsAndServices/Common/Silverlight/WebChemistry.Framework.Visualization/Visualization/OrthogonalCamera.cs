using WebChemistry.Framework.Math;
using System.Windows;

namespace WebChemistry.Framework.Visualization
{
    public class OrthogonalCamera : DependencyObject
    {
        Viewport3DBase _viewport;
        private PerspectiveCamera _perspectiveCamera = null;

        public double AspectRatio
        {
            get { return _perspectiveCamera.AspectRatio; }
            set { _perspectiveCamera.AspectRatio = value; }
        }

        public double FieldOfView
        {
            get { return _perspectiveCamera.FieldOfView; }
            set { _perspectiveCamera.FieldOfView = value; }
        }

        public Vector3D UpDirection
        {
            get { return new Vector3D(0, 1, 0); }
        }

        public Vector3D Position
        {
            get { return _perspectiveCamera.Position; }
        }
                
        double _yaw;
        double _pitch;
        Vector3D _lookAt;

        bool _settingYawPitch = false;

        Matrix3D _worldTransform;
        
        private void SetYaw(double yaw)
        {
            if (_settingYawPitch) return;

            double dY = yaw - _yaw;
            _yaw = yaw;
            _worldTransform.Append(Rotation3D.CreateRotationMatrix(_lookAt, new Vector3D(0, 1, 0), dY));

            _viewport.Render();
        }

        private void SetPitch(double pitch)
        {
            if (_settingYawPitch) return;

            double dP = pitch - _pitch;
            _pitch = pitch;
            _worldTransform.Append(Rotation3D.CreateRotationMatrix(_lookAt, new Vector3D(1, 0, 0), dP));

            _viewport.Render();
        }

        private void SetRadius(double radius)
        {
            _perspectiveCamera.Position = new Vector3D(0, 0, radius);

            _viewport.Render();
        }

        public void SetYawPitch(double yaw, double pitch)
        {
            double dY = yaw - _yaw;
            _yaw = yaw;
            double dP = pitch - _pitch;
            _pitch = pitch;


            if (dY > dP)
            {
                _worldTransform.Append(Rotation3D.CreateRotationMatrix(_lookAt, new Vector3D(0, 1, 0), dY));
                _worldTransform.Append(Rotation3D.CreateRotationMatrix(_lookAt, new Vector3D(1, 0, 0), dP));
            }
            else
            {
                _worldTransform.Append(Rotation3D.CreateRotationMatrix(_lookAt, new Vector3D(1, 0, 0), dP));
                _worldTransform.Append(Rotation3D.CreateRotationMatrix(_lookAt, new Vector3D(0, 1, 0), dY));
            }

            _settingYawPitch = true;

            Yaw = yaw;
            Pitch = pitch;

            _settingYawPitch = false;

            _viewport.Render();
        }
                
        public double Yaw
        {
            get { return (double)GetValue(YawProperty); }
            set { SetValue(YawProperty, value); }
        }

        public static readonly DependencyProperty YawProperty =
            DependencyProperty.Register("Yaw", typeof(double), typeof(OrthogonalCamera), new PropertyMetadata(0.0, (o, a) => (o as OrthogonalCamera).SetYaw((double)a.NewValue)));

        public double Pitch
        {
            get { return (double)GetValue(PitchProperty); }
            set { SetValue(PitchProperty, value); }
        }

        public static readonly DependencyProperty PitchProperty =
            DependencyProperty.Register("Pitch", typeof(double), typeof(OrthogonalCamera), new PropertyMetadata(0.0, (o, a) => (o as OrthogonalCamera).SetPitch((double)a.NewValue)));

        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(double), typeof(OrthogonalCamera), new PropertyMetadata(0.0, (o, a) => (o as OrthogonalCamera).SetRadius((double)a.NewValue)));
        
        internal Matrix3D Value
        {
            get 
            {

                return _perspectiveCamera.Value;
            }
        }

        internal Matrix3D WorldTransform
        {
            get
            {
                return _worldTransform;
            }
        }

        public OrthogonalCamera(Viewport3DBase viewport)
        {
            _viewport = viewport;

            _perspectiveCamera = new PerspectiveCamera();

            _perspectiveCamera.Position = new Vector3D(0, 0, 1);
            _perspectiveCamera.LookDirection = new Vector3D(0, 0, -1);
            _perspectiveCamera.UpDirection = new Vector3D(0, 1, 0);

            _worldTransform = Matrix3D.Identity;
        }
    }
}
