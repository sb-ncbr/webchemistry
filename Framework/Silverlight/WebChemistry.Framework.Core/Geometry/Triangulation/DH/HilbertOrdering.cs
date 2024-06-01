namespace WebChemistry.Framework.Geometry.Triangulation.DH
{
    using System;
    using System.Collections.Generic;
    using WebChemistry.Framework.Math;

    static partial class HilbertOrdering
    {
        const int VoxelThreshold = 400;
        private static uint[] bitMask = { 0x1, 0x2, 0x4, 0x8, 0x10 };
        private static uint[] E = { 0, 0, 0, 3, 3, 6, 6, 5 };
        private static uint[] D = { 0, 1, 1, 2, 2, 1, 1, 0 };

        public static TriangulationVertex<T>[] OrderPoints<T>(IEnumerable<TriangulationVertex<T>> source)
        {
            // Find bounding box and count the input vertices
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            int count = 0;
            foreach (TriangulationVertex<T> vector in source)
            {
                if (vector.X > maxX) maxX = vector.X;
                if (vector.Y > maxY) maxY = vector.Y;
                if (vector.Z > maxZ) maxZ = vector.Z;

                if (vector.X < minX) minX = vector.X;
                if (vector.Y < minY) minY = vector.Y;
                if (vector.Z < minZ) minZ = vector.Z;

                count++;
            }
            maxX += 0.001 * (maxX - minX);
            maxY += 0.001 * (maxY - minY);
            maxZ += 0.001 * (maxZ - minZ);

            TriangulationVertex<T>[] result = new TriangulationVertex<T>[count];

            // Order vertices
            OrderPointsSegment(source, result, 0, new Vector3D(minX, minY, minZ), new Vector3D(maxX, maxY, maxZ), count);

            return result;
        }

        private static void OrderPointsSegment<T>(IEnumerable<TriangulationVertex<T>> source, TriangulationVertex<T>[] target, int startIndex, Vector3D min, Vector3D max, int count)
        {
            int order = ComputeCurveOrder(count);

            if (order == 0)
            {
                int i = 0;
                foreach (TriangulationVertex<T> vertex in source)
                {
                    target[startIndex + i] = vertex;
                    i++;
                }
                return;
            }

            int[] hilbertIndices = new int[count];
            int[] voxelDensity = new int[1 << (3 * order)];
            int[] voxelCummulativeDensity = new int[1 << (3 * order)];
            IntCoordinates[] LUT = new IntCoordinates[1 << (3 * order)];

            int cells = (1 << order);
            int j = 0;
            foreach (TriangulationVertex<T> vertex in source)
            {
                var coordinates = ConvertCoordinates(vertex, min, max, cells);
                int hilbertIndex = HilbertEncode3D(order, coordinates);
                voxelDensity[hilbertIndex]++;
                LUT[hilbertIndex] = coordinates;
                hilbertIndices[j] = hilbertIndex;
                j++;
            }

            for (int i = 1; i < voxelDensity.Length; i++)
            {
                voxelCummulativeDensity[i] = voxelCummulativeDensity[i - 1] + voxelDensity[i - 1];
            }

            j = 0;
            foreach (TriangulationVertex<T> vertex in source)
            {
                target[startIndex + voxelCummulativeDensity[hilbertIndices[j]]] = vertex;
                voxelCummulativeDensity[hilbertIndices[j]]++;
                j++;
            }

            // Reorder voxels with too many vertices recursively
            Vector3D step = (max - min) / cells;
            for (int i = 1; i < voxelDensity.Length; i++)
            {
                if (voxelDensity[i] >= VoxelThreshold)
                {
                    TriangulationVertex<T>[] newSource = new TriangulationVertex<T>[voxelDensity[i]];
                    Array.Copy(target, startIndex + voxelCummulativeDensity[i] - voxelDensity[i], newSource, 0, voxelDensity[i]);
                    Vector3D localMin = new Vector3D(LUT[i].X * step.X + min.X, LUT[i].Y * step.Y + min.Y, LUT[i].Z * step.Z + min.Z);
                    Vector3D localMax = new Vector3D((LUT[i].X + 1) * step.X + min.X, (LUT[i].Y + 1) * step.Y + min.Y, (LUT[i].Z + 1) * step.Z + min.Z);
                    OrderPointsSegment(newSource, target, startIndex + voxelCummulativeDensity[i] - voxelDensity[i], localMin, localMax, voxelDensity[i]);
                }
            }
        }


        /// <remarks>
        /// Algorithm by Chris Hamilton, Compact hilbert Indices, 2006, p.19
        /// This implementation is 3D specific
        /// </remarks>
        private static int HilbertEncode3D(int order, IntCoordinates coordinates)
        {
            var LUT = LUTManager.GetLUT(order);
            if (LUT != null) return LUT[coordinates.X, coordinates.Y, coordinates.Z];

            // Numbered comments in this method correspond to line numbers in Hamilton's paper.

            // 1:
            uint h = 0, e = 0;
            int d = 1;

            // 2:
            for (int i = order - 1; i >= 0; i--)
            {

                // 3:
                uint l = ((uint)coordinates.X & bitMask[i]) >> i;
                l |= (((uint)coordinates.Y & bitMask[i]) >> i) << 1;
                l |= (((uint)coordinates.Z & bitMask[i]) >> i) << 2;

                // 4:
                l = RotRight3b(l ^ e, d + 1);

                // 5:
                uint w = GrayCodeInverse3b(l);

                // 6:
                e ^= RotLeft3b(E[w], d + 1);

                // 7:
                d = (int)((d + D[w] + 1) % 3);

                // 8:
                h = (h << 3) | w;
            }
            return (int)h;
        }

        /// <summary>
        /// Right bit rotation of a 3-bit number stored in an unsigned integer
        /// </summary>
        private static uint RotRight3b(uint i, int shift)
        {
            return (uint)((i >> shift) | (i << (3 - shift))) & 0x7;
        }

        /// <summary>
        /// Left bit rotation of a 3-bit number stored in an unsigned integer
        /// </summary>
        private static uint RotLeft3b(uint i, int shift)
        {
            return (uint)((i << shift) | (i >> (3 - shift))) & 0x7;
        }

        /// <summary>
        /// Computes the number encoded in a 3-bit gray code value
        /// </summary>
        private static uint GrayCodeInverse3b(uint g)
        {
            uint i = g;
            for (int j = 1; j < 3; j++) i ^= g >> j;
            return i;
        }

        /// <summary>
        /// Converts vertex coordinates to an integer representation
        /// </summary>
        private static IntCoordinates ConvertCoordinates<T>(TriangulationVertex<T> value, Vector3D min, Vector3D max, int cells)
        {
            return new IntCoordinates(
                (int)(((value.X - min.X) / (max.X - min.X)) * cells),
                (int)(((value.Y - min.Y) / (max.Y - min.Y)) * cells),
                (int)(((value.Z - min.Z) / (max.Z - min.Z)) * cells));
        }

        /// <summary>
        /// Computes the order of the Hilbert curve
        /// </summary>
        private static int ComputeCurveOrder(int numOfVertices)
        {
            if (numOfVertices < VoxelThreshold) return 0;
            else if (numOfVertices < VoxelThreshold * 8) return 1;
            else if (numOfVertices < VoxelThreshold * 64) return 2;
            else if (numOfVertices < VoxelThreshold * 512) return 3;
            else return 4;
        }

        private struct IntCoordinates
        {
            public IntCoordinates(int x, int y, int z)
            {
                X = x; Y = y; Z = z;
            }
            public int X, Y, Z;
        }
    }
}
