namespace WebChemistry.Framework.Geometry.Helpers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A sorted array that can store up to N elements. Throws away elements if the array is full
    /// and an elements with lower priority arrives.
    /// </summary>
    /// <typeparam name="TPriority"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    internal class PriorityArray<TPriority, TValue> : IPriorityArray<TPriority, TValue>
        where TPriority : IComparable<TPriority>
    {
        int capacity;
        int count;
        Func<TPriority, TPriority, int> comparison;
        PriorityValuePair<TPriority, TValue>[] elements;
        Comparer comparer;

        class Comparer : IComparer<PriorityValuePair<TPriority, TValue>>
        {
            Func<TPriority, TPriority, int> comparison;

            public int Compare(PriorityValuePair<TPriority, TValue> x, PriorityValuePair<TPriority, TValue> y)
            {
                return comparison(x.Priority, y.Priority);
            }

            public Comparer(Func<TPriority, TPriority, int> comparison)
            {
                this.comparison = comparison;
            }
        }

        /// <summary>
        /// Determines how much the array is filled.
        /// </summary>
        public int Count { get { return count; } }

        /// <summary>
        /// Get the i-th element.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public PriorityValuePair<TPriority, TValue> this[int i]
        {
            get { return elements[i]; }
        }

        /// <summary>
        /// Returns the largest priority in the array.
        /// </summary>
        /// <param name="maxIfNotFull"></param>
        /// <returns></returns>
        public TPriority GetLargestPriority(TPriority maxIfNotFull)
        {
            if (count < capacity) return maxIfNotFull;
            return elements[count - 1].Priority;
        }

        public void Clear()
        {
            for (int i = 0; i < count; i++)
            {
                elements[i] = default(PriorityValuePair<TPriority, TValue>);
            }
            count = 0;
        }
        
        public IList<PriorityValuePair<TPriority, TValue>> View()
        {
            return new PriorityArrayView<TPriority, TValue>(elements, count);
        }

        /// <summary>
        /// Throws away elements if the array is full
        /// and an elements with lower priority arrives.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="value"></param>
        public void Add(TPriority priority, TValue value)
        {
            var elem = new PriorityValuePair<TPriority, TValue>(priority, value);

            int i = Array.BinarySearch<PriorityValuePair<TPriority, TValue>>(elements, 0, count, elem, comparer);
            if (i < 0) i = ~i;

            if (i == capacity) return;
            
            for (int k = Math.Min(count - 1, capacity - 2); k >= i; k--) elements[k + 1] = elements[k];
            elements[i] = elem;

            if (count < capacity) count++;
        }

        /// <summary>
        /// Creates a priority array.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="comparison"></param>
        public PriorityArray(int capacity, Func<TPriority, TPriority, int> comparison)
        {
            this.comparison = comparison;
            this.capacity = capacity;
            this.count = 0;
            this.elements = new PriorityValuePair<TPriority, TValue>[capacity];
            this.comparer = new Comparer(comparison);
        }

        public PriorityArray(int capacity)
            : this(capacity, (x, y) => x.CompareTo(y))
        {

        }
    }
}
