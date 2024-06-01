namespace WebChemistry.Framework.Core.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class FunctionalList<T> : IList<T>
    {
        public class Cons : FunctionalList<T>
        {
            public T Head { get; private set; }
            public FunctionalList<T> Tail { get; internal set; }

            public Cons(T head, FunctionalList<T> tail)
            {
                Head = head;
                Tail = tail;
            }
        }

        public class Empty : FunctionalList<T>
        {
            public static readonly Empty Instance = new Empty();

            private Empty()
            {
            }
        }

        public int IndexOf(T item)
        {
            int i = 0;
            var current = this;
            Cons cons;

            while ((cons = current as Cons) != null)
            {
                if (cons.Head.Equals(item)) return i;
                i++;
                current = cons.Tail;
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            var current = this;
            Cons cons;

            while ((cons = current as Cons) != null)
            {
                if (cons.Head.Equals(item)) return true;
                current = cons.Tail;
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int i = arrayIndex;
            var current = this;
            Cons cons;

            while ((cons = current as Cons) != null)
            {
                array[i++] = cons.Head;
                current = cons.Tail;
            }
        }

        public int Count
        {
            get
            {
                int count = 0;
                var current = this;
                Cons cons;

                while ((cons = current as Cons) != null)
                {
                    count++;
                    current = cons.Tail;
                }

                return count;
            }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            var current = this;
            Cons cons;

            while ((cons = current as Cons) != null)
            {
                yield return cons.Head;
                current = cons.Tail;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private FunctionalList()
        {

        }
    }

    public static class FunctionalList
    {
        public static FunctionalList<T> ToSingletonFunctionalList<T>(this T x)
        {
            return new FunctionalList<T>.Cons(x, FunctionalList<T>.Empty.Instance);
        }

        public static FunctionalList<T> ToFunctionalList<T>(this IEnumerable<T> xs)
        {
            FunctionalList<T> ret = FunctionalList<T>.Empty.Instance;

            foreach (var item in xs.Reverse()) ret = new FunctionalList<T>.Cons(item, ret);

            return ret;
        }

        public static FunctionalList<T> Cons<T>(this FunctionalList<T> xs, T x)
        {
            return new FunctionalList<T>.Cons(x, xs);
        }

        public static bool IsEmpty<T>(this FunctionalList<T> xs)
        {
            return xs is FunctionalList<T>.Empty;
        }
    }
}
