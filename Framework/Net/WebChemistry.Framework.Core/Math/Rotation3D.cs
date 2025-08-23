namespace WebChemistry.Framework.Math
{

    public static class Rotation3D
    {   
        /// <summary>
        /// Creates a rotation matrix
        /// </summary>
        /// <param name="center">Center</param>
        /// <param name="axis">Axis</param>
        /// <param name="angle">Angle in degrees</param>
        /// <returns>Rotation matrix</returns>
        public static Matrix3D CreateRotationMatrix(Vector3D center, Vector3D axis, double angle)
        {
            Vector3D a = axis.Normalize();

            double ang = MathHelper.DegreesToRadians(angle / 2);
            double cos = System.Math.Cos(ang);
            double sin = System.Math.Sin(ang);

            double qW = cos;
            double qX = sin * a.X;
            double qY = sin * a.Y;
            double qZ = sin * a.Z;

            double n1 = 2 * qY * qY;
            double n2 = 2 * qZ * qZ;
            double n3 = 2 * qX * qX;
            double n4 = 2 * qX * qY;
            double n5 = 2 * qW * qZ;
            double n6 = 2 * qX * qZ;
            double n7 = 2 * qW * qY;
            double n8 = 2 * qY * qZ;
            double n9 = 2 * qW * qX;

            Matrix3D result = Matrix3D.Identity;
            result.M11 = 1 - n1 - n2;
            result.M12 = n4 + n5;
            result.M13 = n6 - n7;
            result.M21 = n4 - n5;
            result.M22 = 1 - n3 - n2;
            result.M23 = n8 + n9;
            result.M31 = n6 + n7;
            result.M32 = n8 - n9;
            result.M33 = 1 - n3 - n1;
            result.M44 = 1;

            //If this is not Center=(0,0,0) then have to take that into consideration
            if ((center.X != 0) || (center.Y != 0) || (center.Z != 0))
            {
                result.OffsetX = (((-center.X * result.M11) - 
                                   (center.Y * result.M21)) - 
                                   (center.Z * result.M31)) + center.X;

                result.OffsetY = (((-center.X * result.M12) - 
                                   (center.Y * result.M22)) - 
                                   (center.Z * result.M32)) + center.Y;

                result.OffsetZ = (((-center.X * result.M13) - 
                                   (center.Y * result.M23)) - 
                                   (center.Z * result.M33)) + center.Z;
            }

            return result;
        }
    }
}
