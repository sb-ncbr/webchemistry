using System;
using WebChemistry.Framework.Math;

namespace WebChemistry.Framework.Visualization
{
    class PerspectiveCamera
    {
        double _fieldOfView;
        double _farPlaneDistance;
        double _nearPlaneDistance;
        double _aspectRatio;
        Vector3D _lookDirection;
        Vector3D _upDirection;
        Vector3D _position;

        Matrix3D _viewMatrix;
        Matrix3D _projectionMatrix;
        Matrix3D _value;
        
        private void UpdateViewMatrix()
        {
            Vector3D cameraZAxis = -_lookDirection.Normalize();

            Vector3D cameraXAxis = Vector3D.CrossProduct(_upDirection, cameraZAxis).Normalize();

            Vector3D cameraYAxis = Vector3D.CrossProduct(cameraZAxis, cameraXAxis);

            Vector3D cameraPosition = (Vector3D)_position;
            double offsetX = -Vector3D.DotProduct(cameraXAxis, cameraPosition);
            double offsetY = -Vector3D.DotProduct(cameraYAxis, cameraPosition);
            double offsetZ = -Vector3D.DotProduct(cameraZAxis, cameraPosition);

            _viewMatrix = new Matrix3D(cameraXAxis.X, cameraYAxis.X, cameraZAxis.X, 0,
                                       cameraXAxis.Y, cameraYAxis.Y, cameraZAxis.Y, 0,
                                       cameraXAxis.Z, cameraYAxis.Z, cameraZAxis.Z, 0,
                                       offsetX, offsetY, offsetZ, 1);
        }

        private void UpdateProjectionMatrix()
        {
            double xScale = 1.0 / System.Math.Tan(System.Math.PI * _fieldOfView / 360);
            double yScale = _aspectRatio * xScale;

            double zScale = (_farPlaneDistance == double.PositiveInfinity) ? -1 : _farPlaneDistance / (_nearPlaneDistance - _farPlaneDistance);
            double zOffset = _nearPlaneDistance * zScale;

            _projectionMatrix = new Matrix3D(xScale, 0, 0, 0,
                                             0, yScale, 0, 0,
                                             0, 0, zScale, -1,
                                             0, 0, zOffset, 0);
        }

        private void UpdateValue()
        {
            _value = _viewMatrix * _projectionMatrix;
        }

        public double FarPlaneDistance
        {
            get { return _farPlaneDistance; }
            set
            {
                if (_farPlaneDistance != value)
                {
                    _farPlaneDistance = value;

                    UpdateProjectionMatrix();
                    UpdateValue();
                }
            }
        }

        public double NearPlaneDistance
        {
            get { return _nearPlaneDistance; }
            set
            {
                if (_nearPlaneDistance != value)
                {
                    _nearPlaneDistance = value;

                    UpdateProjectionMatrix();
                    UpdateValue();
                }
            }
        }

        public Vector3D LookDirection
        {
            get { return _lookDirection; }
            set
            {
                if (_lookDirection != value)
                {
                    _lookDirection = value.Normalize();

                    UpdateViewMatrix();
                    UpdateValue();
                }
            }
        }

        public Vector3D Position
        {
            get { return _position; }
            set
            {
                if (_position != value)
                {
                    _position = value;

                    UpdateViewMatrix();
                    UpdateValue();
                }
            }
        }

        public Vector3D UpDirection
        {
            get { return _upDirection; }
            set
            {
                if (_upDirection != value)
                {
                    _upDirection = value.Normalize();

                    UpdateViewMatrix();
                    UpdateValue();
                }
            }
        }

        /// <summary>
        /// In degrees
        /// </summary>
        public double FieldOfView
        {
            get { return _fieldOfView; }
            set
            {
                if (_fieldOfView != value)
                {
                    _fieldOfView = value;

                    UpdateProjectionMatrix();
                    UpdateViewMatrix();
                    UpdateValue();
                }
            }
        }

        public double AspectRatio
        {
            get { return _aspectRatio; }
            set
            {
                if (_aspectRatio != value)
                {
                    _aspectRatio = value;

                    UpdateProjectionMatrix();
                    UpdateValue();
                }
            }
        }
                
        internal Matrix3D ViewMatrix
        {
            get
            {
                return _viewMatrix;
            }
        }

        internal Matrix3D ProjectionMatrix
        {
            get
            {
                return _projectionMatrix;
            }
        }

        internal Matrix3D Value
        {
            get
            {
                return _value;
            }
        }

        public PerspectiveCamera()
        {
            _fieldOfView = 45;
            _farPlaneDistance = double.PositiveInfinity;
            _nearPlaneDistance = 0.125;
            _aspectRatio = 1.618;
            _lookDirection = new Vector3D(0, 0, -1);
            _upDirection = new Vector3D(0, 1, 0);
            _position = new Vector3D(0, 0, 20);

            UpdateProjectionMatrix();
            UpdateViewMatrix();
            UpdateValue();
        }
    }
}