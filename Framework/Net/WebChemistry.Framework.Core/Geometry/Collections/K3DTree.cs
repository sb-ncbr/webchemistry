namespace WebChemistry.Framework.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Geometry.Helpers;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Methods of pivot selection when constructing the tree.
    /// </summary>
    public enum K3DPivotSelectionMethod
    {
        /// <summary>
        /// The median element is sorted.
        /// This is the slowest method, but ensures the tree is balanced.
        /// </summary>
        Median = 0,
        /// <summary>
        /// The pivot is selected as the element closes to the (min+max)/2 in each particular dimension.
        /// This is a lot faster and almost perfectly balanced for uniformly distributed data.
        /// </summary>
        Average,
        /// <summary>
        /// The pivot is chosen randomly.
        /// </summary>
        Random
    }

    /// <summary>
    /// A helper class for creating K3D-trees
    /// </summary>
    public static class K3DTree
    {
        /// <summary>
        /// Creates n k3d tree.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="data"></param>
        /// <param name="keySelector"></param>
        /// <param name="bucketSize"></param>
        /// <param name="pivotSelectionMethod"></param>
        /// <returns></returns>
        public static K3DTree<TValue> Create<TValue>(IEnumerable<TValue> data, Func<TValue, Vector3D> keySelector, int bucketSize = 5, K3DPivotSelectionMethod pivotSelectionMethod = K3DPivotSelectionMethod.Median)
        {
            return new K3DTree<TValue>(data, keySelector, bucketSize, pivotSelectionMethod);
        }
    }

    /// <summary>
    /// 3D k-D tree implementation.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class K3DTree<TValue>
    {
        #region Helpers
        static Random rnd = new Random(0);

        static double SelectX(Vector3DValuePair<TValue> v)
        {
            return v.Position.X;
        }

        static double SelectY(Vector3DValuePair<TValue> v)
        {
            return v.Position.Y;
        }

        static double SelectZ(Vector3DValuePair<TValue> v)
        {
            return v.Position.Z;
        }

        static K3DSplitNode<TValue> CreateX(double value)
        {
            return new XSplitNode<TValue>(value);
        }

        static K3DSplitNode<TValue> CreateY(double value)
        {
            return new YSplitNode<TValue>(value);
        }

        static K3DSplitNode<TValue> CreateZ(double value)
        {
            return new ZSplitNode<TValue>(value);
        }

        /// <summary>
        /// Selectors of individual dimensions.
        /// </summary>
        static Func<Vector3DValuePair<TValue>, double>[] Selectors = { SelectX, SelectY, SelectZ };

        static Func<double, K3DSplitNode<TValue>>[] Splitters = { CreateX, CreateY, CreateZ };

        static IComparer<Vector3DValuePair<TValue>>[] Comparers = 
        { 
            new CoordinateComparer3D<TValue>(SelectX),
            new CoordinateComparer3D<TValue>(SelectY),
            new CoordinateComparer3D<TValue>(SelectZ)
        };

        static int NearMedian(Vector3DValuePair<TValue>[] elements, int startIndex, int endIndex, int splitDimension)
        {
            var selector = Selectors[splitDimension];

            int count = endIndex - startIndex + 1;
            double min = double.MaxValue, max = double.MinValue;

            for (int i = startIndex; i < endIndex; i++)
            {
                var v = selector(elements[i]);
                if (v < min) min = v;
                if (v > max) max = v;
            }

            if (min == max)
            {
                return -1;
            }

            var med = (max + min) / 2;
            var md = double.MaxValue;
            int ret = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                var d = Math.Abs(selector(elements[i]) - med);
                if (d < md)
                {
                    md = d;
                    ret = i;
                }
            }
            
            return ret;
        }

        static void Swap(Vector3DValuePair<TValue>[] elements, int i, int j)
        {
            var t = elements[i];
            elements[i] = elements[j];
            elements[j] = t;
        }

        static int Split(Vector3DValuePair<TValue>[] elements, int startIndex, int endIndex, int splitIndex, int splitDimension)
        {
            var selector = Selectors[splitDimension];

            int l = startIndex, r = endIndex;

            Swap(elements, startIndex, splitIndex);
            var pivot = elements[startIndex];
            var pivotV = selector(pivot);

            l++;
            for (; ; )
            {
                while (l <= r && selector(elements[l]) <= pivotV) l++;
                while (selector(elements[r]) > pivotV && l < r) r--;

                if (l < r)
                {
                    var t = elements[l];
                    elements[l] = elements[r];
                    elements[r] = t;
                }
                else
                {
                    break;
                }
            }
            l--;
            elements[startIndex] = elements[l];
            elements[l] = pivot;
            return l;
        }
        #endregion

        #region Build

        private static K3DNode<TValue> BuildSubtreeMedian(Vector3DValuePair<TValue>[] elements, int startIndex, int endIndex, int splitDimension, int capacityOfLeaf)
        {
            int count = endIndex - startIndex + 1;

            if (count <= capacityOfLeaf)
            {
                return new K3DLeafNode<TValue>(elements, startIndex, count);
            }

            int medianIndex;
            K3DSplitNode<TValue> node;
            Vector3DValuePair<TValue> medianElement;

            Array.Sort<Vector3DValuePair<TValue>>(elements, startIndex, count, Comparers[splitDimension]);
            medianIndex = startIndex + count / 2; // (endIndex - startIndex - 1) / 2;
            medianElement = elements[medianIndex];
            node = Splitters[splitDimension](Selectors[splitDimension](medianElement));
            
            node.LeftChild = BuildSubtreeMedian(elements, startIndex, medianIndex, (splitDimension + 1) % 3, capacityOfLeaf);
            node.RightChild = BuildSubtreeMedian(elements, medianIndex + 1, endIndex, (splitDimension + 1) % 3, capacityOfLeaf);

            return node;
        }

        private static K3DNode<TValue> BuildSubtreeAverage(Vector3DValuePair<TValue>[] elements, int startIndex, int endIndex, int splitDimension, int capacityOfLeaf)
        {
            int count = endIndex - startIndex + 1;

            if (count <= capacityOfLeaf)
            {
                return new K3DLeafNode<TValue>(elements, startIndex, count);
            }

            int medianIndex;
            K3DSplitNode<TValue> node;
            Vector3DValuePair<TValue> medianElement;

            medianIndex = NearMedian(elements, startIndex, endIndex, splitDimension);

            // only do the split if there is actually something to split.
            if (medianIndex >= 0)
            {
                medianIndex = Split(elements, startIndex, endIndex, medianIndex, splitDimension);
            }
            else
            {
                medianIndex = (startIndex + endIndex) / 2;
            }
            
            medianElement = elements[medianIndex];
            node = Splitters[splitDimension](Selectors[splitDimension](medianElement));

            node.LeftChild = BuildSubtreeAverage(elements, startIndex, medianIndex, (splitDimension + 1) % 3, capacityOfLeaf);
            node.RightChild = BuildSubtreeAverage(elements, medianIndex + 1, endIndex, (splitDimension + 1) % 3, capacityOfLeaf);

            return node;
        }

        private static K3DNode<TValue> BuildSubtreeRandom(Vector3DValuePair<TValue>[] elements, int startIndex, int endIndex, int splitDimension, int capacityOfLeaf)
        {
            int count = endIndex - startIndex + 1;

            if (count <= capacityOfLeaf)
            {
                return new K3DLeafNode<TValue>(elements, startIndex, count);
            }

            int medianIndex;
            K3DSplitNode<TValue> node;
            Vector3DValuePair<TValue> medianElement;

            medianIndex = startIndex + rnd.Next(count - 1) + 1;
            medianElement = elements[medianIndex];
            medianIndex = Split(elements, startIndex, endIndex, medianIndex, splitDimension);
            node = Splitters[splitDimension](Selectors[splitDimension](medianElement));

            node.LeftChild = BuildSubtreeRandom(elements, startIndex, medianIndex, (splitDimension + 1) % 3, capacityOfLeaf);
            node.RightChild = BuildSubtreeRandom(elements, medianIndex + 1, endIndex, (splitDimension + 1) % 3, capacityOfLeaf);

            return node;
        }
        #endregion

        readonly K3DNode<TValue> root;

        /// <summary>
        /// Creates a K3D tree.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keySelector"></param>
        /// <param name="leafCapacity"></param>
        /// <param name="method"></param>
        public K3DTree(IEnumerable<TValue> values, Func<TValue, Vector3D> keySelector, int leafCapacity = 5, K3DPivotSelectionMethod method = K3DPivotSelectionMethod.Median)
        {
            if (leafCapacity < 3) leafCapacity = 3;

            var vals = values.Select(v => new Vector3DValuePair<TValue>(keySelector(v), v)).ToArray();

            switch (method)
            {
                case K3DPivotSelectionMethod.Average: root = BuildSubtreeAverage(vals, 0, vals.Length - 1, 0, leafCapacity); break;
                case K3DPivotSelectionMethod.Median: root = BuildSubtreeMedian(vals, 0, vals.Length - 1, 0, leafCapacity); break;
                case K3DPivotSelectionMethod.Random: root = BuildSubtreeRandom(vals, 0, vals.Length - 1, 0, leafCapacity); break;
                default: throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Finds all elements in a ball with maxDistance radius.
        /// The priority is the squared distance to the pivot.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public IList<PriorityValuePair<double, TValue>> NearestRadius(Vector3D pivot, double maxDistance)
        {
            var buffer = new PriorityBinaryHeap<double, TValue>();
            root.Nearest(pivot, maxDistance, maxDistance * maxDistance, buffer);
            return buffer.Flatten();
        }

        /// <summary>
        /// Finds n closest elements that are no further than maxDistance.
        /// The priority is the squared distance to the pivot.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="n"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public IList<PriorityValuePair<double, TValue>> Nearest(Vector3D pivot, int n, double maxDistance)
        {
            var buffer = new PriorityArray<double, TValue>(n);
            root.Nearest(pivot, maxDistance, maxDistance * maxDistance, buffer);
            return buffer.View();
        }

        /////// <summary>
        /////// Non-recursive version of Nearest.
        /////// </summary>
        /////// <param name="pivot"></param>
        /////// <param name="n"></param>
        /////// <param name="maxDistance"></param>
        /////// <returns></returns>
        ////public IList<PriorityValuePair<double, TValue>> NearestNonRecursive(Vector3D pivot, int n, double maxDistance)
        ////{
        ////    var buffer = new PriorityArray<double, TValue>(n);
        ////    root.NearestStack(pivot, maxDistance, maxDistance * maxDistance, buffer);
        ////    return buffer.View();
        ////}

        //public IList<PriorityValuePair<double, TValue>> Nearest1(Vector3D pivot, int n, double maxDistance)
        //{
        //    var buffer = new PriorityArray<double, TValue>(n);
        //    root.Nearest(pivot, new K3DRect(), maxDistance * maxDistance, buffer);
        //    return buffer.View();
        //}

        /// <summary>
        /// Finds the n nearest elements. Priority is squared distance.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public IList<PriorityValuePair<double, TValue>> NearestCount(Vector3D pivot, int n)
        {
            var buffer = new PriorityArray<double, TValue>(n);
            root.Nearest(pivot, new K3DRect(), n, buffer);
            return buffer.View();
        }

        /// <summary>
        /// Finds the nearest element. Priority is squared distance.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public PriorityValuePair<double, TValue> Nearest(Vector3D pivot)
        {
            var buffer = new PriorityArray<double, TValue>(1);
            root.Nearest(pivot, new K3DRect(), 1, buffer);
            return buffer[0];
        }
    }
}
