//namespace WebChemistry.Framework.Geometry
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using WebChemistry.Framework.Math;

//    /// <summary>
//    /// This is an adaptation of the Java KDTree library implemented by Levy 
//    /// and Heckel. This simplified version is written by Marco A. Alvarez
//    /// 
//    /// KDTree is a class supporting KD-tree insertion, deletion, equality
//    /// search, range search, and nearest neighbor(s) using double-precision
//    /// floating-point keys.  Splitting dimension is chosen naively, by
//    /// depth modulo K.  Semantics are as follows:
//    /// <UL>
//    /// <LI> Two different keys containing identical numbers should retrieve the 
//    ///      same value from a given KD-tree.  Therefore keys are cloned when a 
//    ///      node is inserted.
//    /// <BR><BR>
//    /// <LI> As with Hashtables, values inserted into a KD-tree are <I>not</I>
//    ///      cloned.  Modifying a value between insertion and retrieval will
//    ///      therefore modify the value stored in the tree.
//    /// </UL>
//    /// 
//    /// @author Simon Levy, Bjoern Heckel
//    /// Translation by Marco A. Alvarez
//    /// </summary>
//    public class K3DTreeOld<T>
//    {
//        KDTree<T> _tree;

//        /// <summary>
//        /// Insert an element into the tree.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="value"></param>
//        public void Insert(Vector3D key, T value)
//        {
//            _tree.insert(new double[] { key.X, key.Y, key.Z }, value);
//        }


//        /// <summary>
//        /// Insert an element into the tree.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="value"></param>
//        public void Insert(double[] key, T value)
//        {
//            _tree.insert(key, value);
//        }

//        //class XComparer : IComparer<Vector3D>
//        //{
//        //    public int Compare(Vector3D x, Vector3D y) { return x.X.CompareTo(y.X); }
//        //}

//        //class YComparer : IComparer<Vector3D>
//        //{
//        //    public int Compare(Vector3D x, Vector3D y) { return x.Y.CompareTo(y.Y); }
//        //}

//        //class ZComparer : IComparer<Vector3D>
//        //{
//        //    public int Compare(Vector3D x, Vector3D y) { return x.Z.CompareTo(y.Z); }
//        //}

//        //static readonly IComparer<Vector3D>[] comparers = new IComparer<Vector3D>[]
//        //{
//        //    new XComparer(),
//        //    new YComparer(),
//        //    new ZComparer()
//        //};

//        /// <summary>
//        /// Insert several items into the tree.
//        /// </summary>
//        /// <param name="values"></param>
//        /// <param name="keySelector"></param>
//        public void Insert(IEnumerable<T> values, Func<T, Vector3D> keySelector)
//        {
//            double[] kb = new double[3];
//            foreach (var value in values)
//            {
//                var k = keySelector(value);
//                kb[0] = k.X; kb[1] = k.Y; kb[2] = k.Z;
//                Insert(kb, value);
//            }
//        }

//        /// <summary>
//        /// Find an element in the tree.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <returns></returns>
//        public T Search(double[] key)
//        {
//            return _tree.search(key);
//        }

//        /// <summary>
//        /// Find an element in the tree.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <returns></returns>
//        public T Search(Vector3D key)
//        {
//            return _tree.search(new double[] { key.X, key.Y, key.Z });
//        }

//        /// <summary>
//        /// Remove an element from the tree.
//        /// </summary>
//        /// <param name="key"></param>
//        public void Delete(Vector3D key)
//        {
//            _tree.delete(new double[] { key.X, key.Y, key.Z });
//        }

//        /// <summary>
//        /// Find a nearest element to a given point.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <returns></returns>
//        public T Nearest(Vector3D key)
//        {
//            return _tree.nearest(new double[] { key.X, key.Y, key.Z });
//        }

//        /// <summary>
//        /// Find up to n nearest elements to a given key. The result is in ascending order.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="n"></param>
//        /// <returns></returns>
//        public T[] Nearest(Vector3D key, int n)
//        {
//            return _tree.nearest(new double[] { key.X, key.Y, key.Z }, n);
//        }

//        /// <summary>
//        /// Find all elements in a ball
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="radius"></param>
//        /// <returns></returns>
//        public IList<T> NearestInBall(Vector3D key, double radius)
//        {
//            //var rh = radius / 2;
//            //return _tree.range(new double[] { key.X - rh, key.Y - rh, key.Z - rh }, new double[] { key.X + rh, key.Y + rh, key.Z + rh });
//            return _tree.nearestInBall(new double[] { key.X, key.Y, key.Z }, radius);
//        }

//        /// <summary>
//        /// Find all elements in a ball.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="radius"></param>
//        /// <returns></returns>
//        public IList<T> NearestInBall(double[] key, double radius)
//        {
//            return _tree.nearestInBall(key, radius);
//        }

//        /// <summary>
//        /// Find up to n nearest elements to a given key that are closer than maxDistance.
//        /// The result is in ascendion order.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="n"></param>
//        /// <param name="maxDistance"></param>
//        /// <returns></returns>
//        public T[] Nearest(double[] key, int n, double maxDistance)
//        {
//            return _tree.nearest(key, n, maxDistance);
//        }

//        /// <summary>
//        /// Find up to n nearest elements to a given key that are closer than maxDistance.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="n"></param>
//        /// <param name="maxDistance"></param>
//        /// <returns></returns>
//        public IEnumerable<T> Nearest(Vector3D key, int n, double maxDistance)
//        {
//            return _tree.nearest(new double[] { key.X, key.Y, key.Z }, n, maxDistance);
//        }

//        /// <summary>
//        /// Create a new empty K3DTree.
//        /// </summary>
//        public K3DTreeOld()
//        {
//            _tree = new KDTree<T>(3);
//        }

//        /// <summary>
//        /// Create a K3Tree that contains some initial elements.
//        /// </summary>
//        /// <param name="data"></param>
//        /// <param name="keySelector"></param>
//        public K3DTreeOld(IEnumerable<T> data, Func<T, Vector3D> keySelector)
//        {
//            _tree = new KDTree<T>(3);
//            Insert(data, keySelector);
//        }
//    }
//}