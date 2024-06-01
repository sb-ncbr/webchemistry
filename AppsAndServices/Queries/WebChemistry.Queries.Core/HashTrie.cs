namespace WebChemistry.Queries.Core
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /** USE THIS????
     */
    ////static int hash(int h) {
    ////    // This function ensures that hashCodes that differ only by
    ////    // constant multiples at each bit position have a bounded
    ////    // number of collisions (approximately 8 at default load factor).
    ////    h ^= (h >>> 20) ^ (h >>> 12);
    ////    return h ^ (h >>> 7) ^ (h >>> 4);
    ////}

    abstract class HashTrieNodeBase<T>
    {
        public abstract HashTrieLeaf<T> Find(int key, int offset);
        public abstract void Flatten(List<T> list);
        public abstract int Count();
        public abstract HashTrieNodeBase<T> Intersect(HashTrieNodeBase<T> node, int offset);


        public abstract bool AreIntersected(HashTrieNodeBase<T> node, int offset);

        public abstract HashTrieNodeBase<T> Insert(T value, int key, int offset);
        public abstract HashTrieNodeBase<T> InsertPersistent(T value, int key, int offset);
        public abstract HashTrieNodeBase<T> Insert(HashTrieLeaf<T> leaf, int offset);
        public abstract HashTrieNodeBase<T> InsertPersistent(HashTrieLeaf<T> leaf, int offset);

        public abstract int CalculateHashCode(int level);
        public abstract bool StructureEquals(HashTrieNodeBase<T> other);
        public abstract void VisitLeaves(Action<HashTrieLeaf<T>> action);
    }

    class HashTrieNode<T> : HashTrieNodeBase<T>
    {
        static int NextPowerOf2(int x)
        {
            x -= 1;
            x |= x >> 16;
            x |= x >> 8;
            x |= x >> 4;
            x |= x >> 2;
            x |= x >> 1;
            return x + 1;
        }

        // From http://graphics.stanford.edu/~seander/bithacks.html
        static uint CountSetBits(uint v)
        {
            v = v - ((v >> 1) & 0x55555555);                    // reuse input as temporary
            v = (v & 0x33333333) + ((v >> 2) & 0x33333333);     // temp
            return ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
        }

        // From http://graphics.stanford.edu/~seander/bithacks.html
        static int CountSetBits(ulong v)
        {
            ulong c;
            c = v - ((v >> 1) & 0x5555555555555555ul);
            c = ((c >> 2) & 0x3333333333333333ul) + (c & 0x3333333333333333ul);
            c = ((c >> 4) + c) & 0x0F0F0F0F0F0F0F0Ful;
            c = ((c >> 8) + c) & 0x00FF00FF00FF00FFul;
            c = ((c >> 16) + c) & 0x0000FFFF0000FFFFul;
            return (int)((c >> 32) + c)/* & 0x00000000FFFFFFFFull*/;
        }

        uint map;
        int count;
        HashTrieNodeBase<T>[] data;

        const int mask = 31;
        const int shift = 5;
        const uint one = 1;

        public HashTrieNode()
        {
            data = new HashTrieNodeBase<T>[2];
        }

        internal HashTrieNode(HashTrieLeaf<T> leaf, int offset)
        {
            data = new HashTrieNodeBase<T>[2];
            data[0] = leaf;
            count = 1;
            map = one << ((leaf.Key >> offset) & mask);
        }

        private HashTrieNode(HashTrieNodeBase<T>[] data, uint map, int count)
        {
            this.data = data;
            this.map = map;
            this.count = count;
        }

        public override HashTrieNodeBase<T> InsertPersistent(T value, int key, int offset)
        {
            var p = (key >> offset) & mask;

            bool isNew = ((map >> p) & one) == 0;

            var bit = one << p;
            var index = CountSetBits((bit - one) & map);

            if (isNew)
            {
                var newMap = map | bit;
                var newCount = count + 1;
                var newSize = data.Length;
                if (newCount > newSize)
                {
                    newSize = newSize * 2;
                }
                HashTrieNodeBase<T>[] newData = new HashTrieNodeBase<T>[newSize];
                for (int i = count; i > index; i--) newData[i] = data[i - 1];
                for (int i = 0; i < index; i++) newData[i] = data[i];
                newData[index] = new HashTrieLeaf<T>(key, value);
                return new HashTrieNode<T>(newData, newMap, newCount);
            }
            else
            {
                var newNode = data[index].InsertPersistent(value, key, offset + shift);

                if (object.ReferenceEquals(newNode, data[index]))
                {
                    return this;
                }
                else
                {
                    var newData = new HashTrieNodeBase<T>[count];
                    for (int i = 0; i < count; i++) newData[i] = data[i];
                    newData[index] = newNode;
                    return new HashTrieNode<T>(newData, map, count);
                }
            }
        }

        public override HashTrieNodeBase<T> InsertPersistent(HashTrieLeaf<T> leaf, int offset)
        {
            var p = (leaf.Key >> offset) & mask;

            bool isNew = ((map >> p) & one) == 0;

            var bit = one << p;
            var index = CountSetBits((bit - one) & map);

            if (isNew)
            {
                var newMap = map | bit;
                var newCount = count + 1;
                var newSize = data.Length;
                if (newCount > newSize)
                {
                    newSize = newSize * 2;
                }
                HashTrieNodeBase<T>[] newData = new HashTrieNodeBase<T>[newSize];
                for (int i = count; i > index; i--) newData[i] = data[i - 1];
                for (int i = 0; i < index; i++) newData[i] = data[i];
                newData[index] = leaf;
                return new HashTrieNode<T>(newData, newMap, newCount);
            }
            else
            {
                var newNode = data[index].InsertPersistent(leaf, offset + shift);

                if (object.ReferenceEquals(newNode, data[index]))
                {
                    return this;
                }
                else
                {
                    var newData = new HashTrieNodeBase<T>[count];
                    for (int i = 0; i < count; i++) newData[i] = data[i];
                    newData[index] = newNode;
                    return new HashTrieNode<T>(newData, map, count);
                }
            }
        }

        public override HashTrieNodeBase<T> Insert(T value, int key, int offset)
        {
            var p = (key >> offset) & mask;

            bool isNew = ((map >> p) & one) == 0;

            var bit = one << p;
            var index = CountSetBits((bit - one) & map);

            if (isNew)
            {
                map |= bit;
                count++;
                if (count > data.Length)
                {
                    Array.Resize(ref data, data.Length * 2);
                }
                for (int i = count - 1; i > index; i--) data[i] = data[i - 1];
                data[index] = new HashTrieLeaf<T>(key, value);
            }
            else
            {
                data[index] = data[index].Insert(value, key, offset + shift);
            }

            return this;
        }

        public override HashTrieNodeBase<T> Insert(HashTrieLeaf<T> leaf, int offset)
        {
            var p = (leaf.Key >> offset) & mask;

            bool isNew = ((map >> p) & one) == 0;

            var bit = one << p;
            var index = CountSetBits((bit - one) & map);

            if (isNew)
            {
                map |= bit;
                count++;
                if (count > data.Length)
                {
                    Array.Resize(ref data, data.Length * 2);
                }
                for (int i = count - 1; i > index; i--) data[i] = data[i - 1];
                data[index] = leaf;
            }
            else
            {
                data[index] = data[index].Insert(leaf, offset + shift);
            }

            return this;
        }

        public override HashTrieLeaf<T> Find(int key, int offset)
        {
            var p = (key >> offset) & mask;

            if (((map >> p) & 1) == 0) return null;

            var index = CountSetBits(((one << p) - 1) & map);
            return data[index].Find(key, offset + shift);
        }

        public override HashTrieNodeBase<T> Intersect(HashTrieNodeBase<T> node, int offset)
        {
            var leaf = node as HashTrieLeaf<T>;
            if (leaf != null) return Intersect(leaf, offset);
            return Intersect(node as HashTrieNode<T>, offset);
        }

        public override bool AreIntersected(HashTrieNodeBase<T> node, int offset)
        {
            var leaf = node as HashTrieLeaf<T>;
            if (leaf != null) return AreIntersected(leaf, offset);
            return AreIntersected(node as HashTrieNode<T>, offset);
        }

        public override void VisitLeaves(Action<HashTrieLeaf<T>> action)
        {
            for (int i = 0; i < count; i++)
            {
                data[i].VisitLeaves(action);
            }
        }

        HashTrieNodeBase<T> Intersect(HashTrieLeaf<T> leaf, int offset)
        {
            var t = Find(leaf.Key, offset);
            if (t == null) return null;
            return t.Key == leaf.Key ? leaf : null;
        }

        HashTrieNodeBase<T> Intersect(HashTrieNode<T> node, int offset)
        {
            var bMap = node.map;

            var mapR = bMap & this.map;

            if (mapR == 0) return null;

            var rSize = (int)CountSetBits(mapR);
            HashTrieNodeBase<T>[] rData;

            rData = new HashTrieNodeBase<T>[NextPowerOf2(rSize)];

            int indexA = 0, indexB = 0, indexR = 0;

            for (int bit = 0; indexR < rSize && bit < 32; bit++)
            {
                var sA = ((map >> bit) & 1) == 1;
                var sB = ((bMap >> bit) & 1) == 1;

                if (sA && sB)
                {
                    var t = data[indexA].Intersect(node.data[indexB], offset + shift);
                    if (t != null) rData[indexR++] = t;
                    else map ^= one << bit;
                }

                if (sA) indexA++;
                if (sB) indexB++;
            }

            if (indexR == 0) return null;
            if (indexR == 1 && rData[0] is HashTrieLeaf<T>) return rData[0];

            return new HashTrieNode<T>(rData, mapR, indexR);
        }

        bool AreIntersected(HashTrieLeaf<T> leaf, int offset)
        {
            return Find(leaf.Key, offset) != null;
        }

        bool AreIntersected(HashTrieNode<T> node, int offset)
        {
            var bMap = node.map;

            var mapR = bMap & this.map;

            if (mapR == 0) return false;

            var rSize = (int)CountSetBits(mapR);
            int indexA = 0, indexB = 0, indexR = 0;

            for (int bit = 0; indexR < rSize && bit < 32; bit++)
            {
                var sA = ((map >> bit) & 1) == 1;
                var sB = ((bMap >> bit) & 1) == 1;

                if (sA && sB)
                {
                    indexR++;
                    if (data[indexA].AreIntersected(node.data[indexB], offset + shift)) return true;
                }

                if (sA) indexA++;
                if (sB) indexB++;
            }

            return false;
        }

        public override void Flatten(List<T> list)
        {
            for (int i = 0; i < count; i++)
            {
                data[i].Flatten(list);
            }
        }

        public override int Count()
        {
            var total = 0;
            for (int i = 0; i < count; i++)
            {
                total += data[i].Count();
            }
            return total;
        }

        public override int CalculateHashCode(int level)
        {
            int hash = 31;
            hash = (hash * 23) + (int)map;
            hash = (hash * 23) + count;

            if (level == 0) return hash;
            
            level--;
            for (int i = 0; i < count; i++)
            {
                hash = (hash * 23) + data[i].CalculateHashCode(level);
            }

            return hash;
        }

        public override bool StructureEquals(HashTrieNodeBase<T> other)
        {
            var node = other as HashTrieNode<T>;
            if (node == null) return false;

            if (this.map != node.map) return false;

            for (int i = 0; i < this.count; i++)
            {
                if (!data[i].StructureEquals(node.data[i])) return false;
            }

            return true;
        }

        public T GetAny()
        {
            if (count == 0) throw new InvalidOperationException("The node contains no elements.");

            var n = data[0];
            var leaf = n as HashTrieLeaf<T>;
            if (leaf != null) return leaf.Value;
            return (n as HashTrieNode<T>).GetAny();            
        }
    }

    class HashTrieLeaf<T> : HashTrieNodeBase<T>
    {
        internal readonly int Key;
        internal readonly T Value;

        public HashTrieLeaf(int key, T value)
        {
            this.Key = key;
            this.Value = value;
        }

        public override HashTrieNodeBase<T> Insert(HashTrieLeaf<T> leaf, int offset)
        {
            if (leaf.Key == Key) return this;
            var ret = new HashTrieNode<T>(this, offset);
            ret.Insert(leaf, offset);
            return ret;
        }

        public override HashTrieNodeBase<T> Insert(T value, int key, int offset)
        {
            if (key == Key) return this;
            var ret = new HashTrieNode<T>(this, offset);
            ret.Insert(value, key, offset);
            return ret;
        }

        public override HashTrieNodeBase<T> InsertPersistent(HashTrieLeaf<T> leaf, int offset)
        {
            if (leaf.Key == Key) return this;
            var ret = new HashTrieNode<T>(this, offset);
            ret.Insert(leaf, offset);
            return ret;
        }

        public override HashTrieNodeBase<T> InsertPersistent(T value, int key, int offset)
        {
            if (key == Key) return this;
            var ret = new HashTrieNode<T>(this, offset);
            ret.Insert(value, key, offset);
            return ret;
        }

        public override void VisitLeaves(Action<HashTrieLeaf<T>> action)
        {
            action(this);
        }
        
        public override HashTrieNodeBase<T> Intersect(HashTrieNodeBase<T> node, int offset)
        {
            var leaf = node as HashTrieLeaf<T>;
            if (leaf != null) return Intersect(leaf, offset);
            return Intersect(node as HashTrieNode<T>, offset);
        }

        public override bool AreIntersected(HashTrieNodeBase<T> node, int offset)
        {
            var leaf = node as HashTrieLeaf<T>;
            if (leaf != null) return AreIntersected(leaf, offset);
            return AreIntersected(node as HashTrieNode<T>, offset);
        }
        
        public HashTrieNodeBase<T> Intersect(HashTrieLeaf<T> leaf, int offset)
        {
            if (leaf.Key == this.Key) return this;
            return null;
        }

        public HashTrieNodeBase<T> Intersect(HashTrieNode<T> node, int offset)
        {
            return node.Intersect(this, offset);
        }

        public bool AreIntersected(HashTrieLeaf<T> leaf, int offset)
        {
            if (leaf.Key == this.Key) return true;
            return false;
        }

        public bool AreIntersected(HashTrieNode<T> node, int offset)
        {
            return node.AreIntersected(this, offset);
        }

        public override HashTrieLeaf<T> Find(int key, int offset)
        {
            if (this.Key == key) return this;
            return null;
        }

        public override void Flatten(List<T> list)
        {
            list.Add(this.Value);
        }

        public override int Count()
        {
            return 1;
        }

        public override int CalculateHashCode(int level)
        {
            // This function ensures that hashCodes that differ only by
            // constant multiples at each bit position have a bounded
            // number of collisions (approximately 8 at default load factor).
            //h ^= (h >>> 20) ^ (h >>> 12);
            //return h ^ (h >>> 7) ^ (h >>> 4);
            return Key;
        }

        public override bool StructureEquals(HashTrieNodeBase<T> other)
        {
            var leaf = other as HashTrieLeaf<T>;
            if (leaf == null) return false;
            return this.Key == leaf.Key;
        }
    }

    public class HashTrie<T> : IEnumerable<T>, IEquatable<HashTrie<T>>
    {
        public static readonly HashTrie<T> Empty = new HashTrie<T>();

        HashTrieNodeBase<T> root;
        ReadOnlyCollection<T> flat;

        public HashTrie()
        {
        }

        public HashTrie(int key, T value)
        {
            Insert(key, value);
        }

        public HashTrie(T value)
        {
            Insert(value.GetHashCode(), value);
        }

        private HashTrie(HashTrieNodeBase<T> root)
        {
            this.root = root;
        }

        public HashTrie(IEnumerable<T> xs)
        {
            foreach (var x in xs)
            {
                Insert(x.GetHashCode(), x);
            }
        }

        private void Insert(int key, T value)
        {
            if (root == null)
            {
                root = new HashTrieLeaf<T>(key, value);
                flat = null;
                return;
            }

            var newRoot = root.Insert(value, key, 0);
            if (!object.ReferenceEquals(newRoot, root)) flat = null;
            root = newRoot;
        }

        private void Insert(HashTrieLeaf<T> leaf)
        {
            if (root == null)
            {
                root = leaf;
                flat = null;
                return;
            }

            var newRoot = root.Insert(leaf, 0);
            if (!object.ReferenceEquals(newRoot, root)) flat = null;
            root = newRoot;
        }

        //public HashTrie<T> Add(IEnumerable<T> xs)
        //{
        //    var ret = this;
        //    bool first = true;
        //    foreach (var x in xs)
        //    {
        //        if (first)
        //        {
        //            ret = Add(x.GetHashCode(), x);
        //            if (!object.ReferenceEquals(ret, this)) first = false;
        //        }
        //        else ret.Insert(x.GetHashCode(), x);
        //    }

        //    return ret;
        //}

        HashTrie<T> Add(HashTrieLeaf<T> leaf)
        {
            if (root == null)
            {
                return new HashTrie<T>(leaf);
            }

            var ret = root.InsertPersistent(leaf, 0);
            if (object.ReferenceEquals(ret, root)) return this;
            return new HashTrie<T>(ret);
        }

        public HashTrie<T> Add(int key, T value)
        {
            if (root == null)
            {
                return new HashTrie<T>(new HashTrieLeaf<T>(key, value));
            }

            var ret = root.InsertPersistent(value, key, 0);
            if (object.ReferenceEquals(ret, root)) return this;
            return new HashTrie<T>(ret);
        }

        public HashTrie<T> Add(T value)
        {
            if (root == null)
            {
                return new HashTrie<T>(new HashTrieLeaf<T>(value.GetHashCode(), value));
            }

            var ret = root.InsertPersistent(value, value.GetHashCode(), 0);
            if (object.ReferenceEquals(ret, root)) return this;
            return new HashTrie<T>(ret);
        }

        public T GetAny()
        {
            if (root == null) throw new InvalidOperationException("The collection is empty.");
            var leaf = root as HashTrieLeaf<T>;
            if (leaf != null) return leaf.Value;
            var node = root as HashTrieNode<T>;
            return node.GetAny();
        }

        //public static HashTrie<T> Union(HashTrie<T> a, HashTrie<T> b)
        //{
        //    if (a.root == null) return b;
        //    if (b.root == null) return a;

        //    HashTrie<T> ret;
        //    HashTrieNodeBase<T> pivot;

        //    if (a.Count < b.Count)
        //    {
        //        ret = b;
        //        pivot = a.root;
        //    }
        //    else
        //    {
        //        ret = a;
        //        pivot = b.root;
        //    }

        //    bool first = true;

        //    pivot.VisitLeaves(leaf =>
        //    {
        //        if (first)
        //        {
        //            var newRet = ret.Add(leaf);
        //            if (!object.ReferenceEquals(ret, newRet))
        //            {
        //                first = false;
        //                ret = newRet;
        //            }
        //        }
        //        else ret.Insert(leaf);
        //    });

        //    return ret;
        //}

        public void VisitLeaves(Action<T> function)
        {
            if (root == null)
            {
                return;
            }

            if (flat != null)
            {
                for (int i = 0; i < flat.Count; i++)
                {
                    function(flat[i]);
                }
                return;
            }

            {
                var func = function;
                root.VisitLeaves(l => function(l.Value));
            }
        }

        ////public static HashTrie<T> Intersect(HashTrie<T> a, HashTrie<T> b)
        ////{
        ////    if (a.root == null) return a;
        ////    if (b.root == null) return b;

        ////    var r = a.root.Intersect(b.root, 0);
        ////    if (r == null) return new HashTrie<T>();
        ////    return new HashTrie<T>(r);
        ////}

        public static bool AreIntersected(HashTrie<T> a, HashTrie<T> b)
        {
            if (a.root == null || b.root == null) return false;

            return a.root.AreIntersected(b.root, 0);
        }

        public bool TryFind(int key, out T value)
        {
            if (root == null)
            {
                value = default(T);
                return false;
            }

            var r = root.Find(key, 0);

            if (r == null)
            {
                value = default(T);
                return false;
            }
            value = r.Value;
            return true;
        }

        public bool ContainsKey(int key)
        {
            if (root == null)
            {
                return false;
            }

            var r = root.Find(key, 0);

            if (r == null)
            {
                return false;
            }
            return true;
        }

        public bool Contains(T value)
        {
            return ContainsKey(value.GetHashCode());
        }

        public ReadOnlyCollection<T> Flatten()
        {
            if (flat != null)
            {
                return flat;
            }

            if (root == null)
            {
                flat = new ReadOnlyCollection<T>(new T[0]);
                return flat;
            }

            var list = new List<T>();
            root.Flatten(list);
            flat = new ReadOnlyCollection<T>(list);
            return flat;
        }

        public bool IsSingleton
        {
            get { return root is HashTrieLeaf<T>; }
        }

        public T SingleElement
        {
            get { return (root as HashTrieLeaf<T>).Value; }
        }

        int? count = null;
        public int Count
        {
            get 
            {
                if (root == null) return 0;
                if (count.HasValue) return count.Value;
                count = root.Count();
                return count.Value;
            }
        }

        int? hashCode = null;
        public override int GetHashCode()
        {
            if (this.hashCode.HasValue) return this.hashCode.Value;

            if (this.root == null) hashCode = 0;
            else hashCode = root.CalculateHashCode(2);
            return hashCode.Value;

            //var flat = Flatten();
            //int hashCode = 23;
            //for (int i = 0; i < flat.Count; i++)
            //    hashCode = unchecked(hashCode * 31 + flat[i].GetHashCode());
            //this.hashCode = hashCode;
            //return hashCode;
        }

        public override bool Equals(object obj)
        {
            var other = obj as HashTrie<T>;
            if (other == null) return false;
            return Equals(other);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Flatten()).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Flatten().GetEnumerator();
        }

        public bool Equals(HashTrie<T> other)
        {
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.root == null && other.root == null) return true;
            if (this.root == null) return false;
            return this.root.StructureEquals(other.root);
        }
    }

    public static class HashTrie
    {
        public static HashTrie<T> Empty<T>()
        {
            return HashTrie<T>.Empty;
        }

        public static HashTrie<T> Singleton<T>(int key, T value)
        {
            return new HashTrie<T>(key, value);
        }

        public static HashTrie<T> Singleton<T>(T value)
        {
            return new HashTrie<T>(value);
        }

        public static HashTrie<T> Create<T>(IEnumerable<T> xs)
        {
            //return HashTrie<T>.Empty.Add(xs);
            return new HashTrie<T>(xs);
        }

        //public static HashTrie<T> Union<T>(this HashTrie<T> a, HashTrie<T> b)
        //{
        //    return HashTrie<T>.Union(a, b);
        //}

        ////public static HashTrie<T> Intersect<T>(this HashTrie<T> a, HashTrie<T> b)
        ////{
        ////    return HashTrie<T>.Intersect(a, b);
        ////}

        public static bool AreIntersected<T>(this HashTrie<T> a, HashTrie<T> b)
        {
            return HashTrie<T>.AreIntersected(a, b);
        }
    }
}
