namespace WebChemistry.Framework.Geometry.Helpers
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// Binary heap
    /// 
    /// Taken from QuickGraph library.
    /// </summary>
    /// <remarks>
    /// Indexing rules:
    /// 
    /// parent index: index ¡ 1)/2
    /// left child: 2 * index + 1
    /// right child: 2 * index + 2
    /// 
    /// Reference:
    /// http://dotnetslackers.com/Community/files/folders/data-structures-and-algorithms/entry28722.aspx
    /// </remarks>
    /// <typeparam name="TValue">type of the value</typeparam>
    /// <typeparam name="TPriority">type of the priority metric</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    internal class PriorityBinaryHeap<TPriority, TValue>
        : IEnumerable<PriorityValuePair<TPriority, TValue>>,
          IPriorityCollection<TPriority, TValue>
        where TPriority : IComparable<TPriority>
    {
        readonly Func<TPriority, TPriority, int> priorityComparsion;
        PriorityValuePair<TPriority, TValue>[] items;
        int count;
        int version;

        const int DefaultCapacity = 16;

        public PriorityBinaryHeap()
            : this(DefaultCapacity, (x, y) => x.CompareTo(y))
        { }

        public PriorityBinaryHeap(Func<TPriority, TPriority, int> priorityComparison)
            : this(DefaultCapacity, priorityComparison)
        { }

        public PriorityBinaryHeap(int capacity, Func<TPriority, TPriority, int> priorityComparison)
        {
            Contract.Requires(capacity >= 0);
            Contract.Requires(priorityComparison != null);

            this.items = new PriorityValuePair<TPriority, TValue>[capacity];
            this.priorityComparsion = priorityComparison;
        }

        public Func<TPriority, TPriority, int> PriorityComparison
        {
            get { return this.priorityComparsion; }
        }

        public int Capacity
        {
            get { return this.items.Length; }
        }

        public int Count
        {
            get { return this.count; }
        }

        //public TPriority GetLargestPriority(TPriority maxIfNotFull)
        //{
        //    return maxIfEmpty;
        //}

        public void Add(TPriority priority, TValue value)
        {
            this.version++;
            this.ResizeArray();
            this.items[this.count++] = new PriorityValuePair<TPriority, TValue>(priority, value);
            this.MinHeapifyDown(this.count - 1);
        }

        private void MinHeapifyDown(int start)
        {
            int i = start;
            int j = (i - 1) / 2;
            while (i > 0 &&
                this.Less(i, j))
            {
                this.Swap(i, j);
                i = j;
                j = (i - 1) / 2;
            }
        }

        public TValue[] ToValueArray()
        {
            var values = new TValue[this.items.Length];
            for (int i = 0; i < values.Length; ++i)
                values[i] = this.items[i].Value;
            return values;
        }

        private void ResizeArray()
        {
            if (this.count == this.items.Length)
            {
                var newItems = new PriorityValuePair<TPriority, TValue>[this.count * 2 + 1];
                Array.Copy(this.items, newItems, this.count);
                this.items = newItems;
            }
        }

        public PriorityValuePair<TPriority, TValue> Minimum()
        {
            if (this.count == 0)
                throw new InvalidOperationException();
            return this.items[0];
        }

        public PriorityValuePair<TPriority, TValue> RemoveMinimum()
        {
            // shortcut for heap with 1 element.
            if (this.count == 1)
            {
                this.version++;
                return this.items[--this.count];
            }
            return this.RemoveAt(0);
        }

        public IList<PriorityValuePair<TPriority, TValue>> Flatten()
        {
            int oc = this.count;

            while (this.count > 0) this.RemoveMinimum();

            return new ReversePriorityArrayView<TPriority, TValue>(items, oc);
        }

        public PriorityValuePair<TPriority, TValue> RemoveAt(int index)
        {
            if (this.count == 0)
                throw new InvalidOperationException("heap is empty");
            if (index < 0 | index >= this.count | index + this.count < this.count)
                throw new ArgumentOutOfRangeException("index");

            this.version++;
            // shortcut for heap with 1 element.
            if (this.count == 1)
                return this.items[--this.count];

            if (index < this.count - 1)
            {
                this.Swap(index, this.count - 1);
                this.MinHeapifyUp(index);
            }

            return this.items[--this.count];
        }

        private void MinHeapifyUp(int index)
        {
            var left = 2 * index + 1;
            var right = 2 * index + 2;
            while (
                    (left < this.count - 1 && !this.Less(index, left)) ||
                    (right < this.count - 1 && !this.Less(index, right))
                   )
            {
                if (right >= this.count - 1 ||
                    this.Less(left, right))
                {
                    this.Swap(left, index);
                    index = left;
                }
                else
                {
                    this.Swap(right, index);
                    index = right;
                }
                left = 2 * index + 1;
                right = 2 * index + 2;
            }
        }

        public int IndexOf(TValue value)
        {
            for (int i = 0; i < this.count; i++)
            {
                if (object.Equals(value, this.items[i].Value))
                    return i;
            }
            return -1;
        }

        public bool MinimumUpdate(TPriority priority, TValue value)
        {
            // find index
            for (int i = 0; i < this.count; i++)
            {
                if (object.Equals(value, this.items[i].Value))
                {
                    if (this.priorityComparsion(priority, this.items[i].Priority) <= 0)
                    {
                        this.RemoveAt(i);
                        this.Add(priority, value);
                        return true;
                    }
                    return false;
                }
            }

            // not in collection
            this.Add(priority, value);
            return true;
        }

        public void Update(TPriority priority, TValue value)
        {
            // find index
            var index = this.IndexOf(value);
            // remove if needed
            if (index > -1)
                this.RemoveAt(index);
            this.Add(priority, value);
        }

        [Pure]
        private bool Less(int i, int j)
        {
            Contract.Requires(
                i >= 0 & i < this.count &
                j >= 0 & j < this.count &
                i != j);

            return this.priorityComparsion(this.items[i].Priority, this.items[j].Priority) <= 0;
        }

        private void Swap(int i, int j)
        {
            Contract.Requires(
                i >= 0 && i < this.count &&
                j >= 0 && j < this.count &&
                i != j);

            var kv = this.items[i];
            this.items[i] = this.items[j];
            this.items[j] = kv;
        }

#if DEEP_INVARIANT
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(this.items != null);
            Contract.Invariant(
                this.count > -1 &
                this.count <= this.items.Length);
            Contract.Invariant(
                EnumerableContract.All(0, this.count, index =>
                {
                    var left = 2 * index + 1;
                    var right = 2 * index + 2;
                    return  (left >= count || this.Less(index, left)) &&
                            (right >= count || this.Less(index, right));
                })
            );
        }
#endif

        #region IEnumerable<PriorityValuePair<TKey,TValue>> Members
        public IEnumerator<PriorityValuePair<TPriority, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        struct Enumerator :
            IEnumerator<PriorityValuePair<TPriority, TValue>>
        {
            PriorityBinaryHeap<TPriority, TValue> owner;
            PriorityValuePair<TPriority, TValue>[] items;
            readonly int count;
            readonly int version;
            int index;

            public Enumerator(PriorityBinaryHeap<TPriority, TValue> owner)
            {
                this.owner = owner;
                this.items = owner.items;
                this.count = owner.count;
                this.version = owner.version;
                this.index = -1;
            }

            public PriorityValuePair<TPriority, TValue> Current
            {
                get
                {
                    if (this.version != this.owner.version)
                        throw new InvalidOperationException();
                    if (this.index < 0 | this.index == this.count)
                        throw new InvalidOperationException();
                    Contract.Assert(this.index <= this.count);
                    return this.items[this.index];
                }
            }

            void IDisposable.Dispose()
            {
                this.owner = null;
                this.items = null;
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                if (this.version != this.owner.version)
                    throw new InvalidOperationException();
                return ++this.index < this.count;
            }

            public void Reset()
            {
                if (this.version != this.owner.version)
                    throw new InvalidOperationException();
                this.index = -1;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion

    }
}
