namespace WebChemistry.Framework.Geometry.Helpers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a view of the first N elements of an array.
    /// </summary>
    /// <typeparam name="TPriority"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class PriorityArrayView<TPriority, TValue> : IList<PriorityValuePair<TPriority, TValue>>
    {
        int count;
        PriorityValuePair<TPriority, TValue>[] inner;

        public int IndexOf(PriorityValuePair<TPriority, TValue> item)
        {
            for (int i = 0; i < count; i++)
            {
                if (this[i].Equals(item)) return i;
            }
            return -1;
        }

        public void Insert(int index, PriorityValuePair<TPriority, TValue> item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public PriorityValuePair<TPriority, TValue> this[int index]
        {
            get
            {
                return inner[index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void Add(PriorityValuePair<TPriority, TValue> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(PriorityValuePair<TPriority, TValue> item)
        {
            for (int i = 0; i < count; i++)
            {
                if (this[i].Equals(item)) return true;
            }
            return false;
        }

        public void CopyTo(PriorityValuePair<TPriority, TValue>[] array, int arrayIndex)
        {
            for (int i = 0; i < count; i++)
            {
                array[i + arrayIndex] = this[i];
            }
        }

        public int Count
        {
            get { return count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(PriorityValuePair<TPriority, TValue> item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<PriorityValuePair<TPriority, TValue>> GetEnumerator()
        {
            return inner.Take(count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return inner.Take(count).GetEnumerator();
        }

        public PriorityArrayView(PriorityValuePair<TPriority, TValue>[] inner, int count)
        {
            this.inner = inner;
            this.count = count;
        }
    }

    /// <summary>
    /// The elements are accessed in reverse order.
    /// </summary>
    /// <typeparam name="TPriority"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ReversePriorityArrayView<TPriority, TValue> : IList<PriorityValuePair<TPriority, TValue>>
    {
        int count;
        PriorityValuePair<TPriority, TValue>[] inner;

        public int IndexOf(PriorityValuePair<TPriority, TValue> item)
        {
            for (int i = 0; i < count; i++)
            {
                if (this[i].Equals(item)) return i;
            }
            return -1;
        }

        public void Insert(int index, PriorityValuePair<TPriority, TValue> item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public PriorityValuePair<TPriority, TValue> this[int index]
        {
            get
            {
                return inner[count - index - 1];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void Add(PriorityValuePair<TPriority, TValue> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(PriorityValuePair<TPriority, TValue> item)
        {
            for (int i = 0; i < count; i++)
            {
                if (this[i].Equals(item)) return true;
            }
            return false;
        }

        public void CopyTo(PriorityValuePair<TPriority, TValue>[] array, int arrayIndex)
        {
            for (int i = 0; i < count; i++)
            {
                array[i + arrayIndex] = this[i];
            }
        }

        public int Count
        {
            get { return count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(PriorityValuePair<TPriority, TValue> item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<PriorityValuePair<TPriority, TValue>> GetEnumerator()
        {
            return inner.Take(count).Reverse().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return inner.Take(count).Reverse().GetEnumerator();
        }

        public ReversePriorityArrayView(PriorityValuePair<TPriority, TValue>[] inner, int count)
        {
            this.inner = inner;
            this.count = count;
        }
    }
}
