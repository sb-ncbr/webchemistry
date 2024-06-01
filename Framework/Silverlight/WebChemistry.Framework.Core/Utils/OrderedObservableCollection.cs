
namespace WebChemistry.Framework.Utils
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;

    public class OrderedObservableCollection<T> : ObservableCollection<T>
    {
        IComparer<T> comparer;

        protected override void SetItem(int index, T item)
        {
            throw new NotSupportedException();
        }
        
        protected override void InsertItem(int index, T item)
        {
            int count = this.Count;
            int min = 0, max = count;

            while (min != max)
            {
                int current = (min + max) / 2;
                int c = comparer.Compare(item, this[current]);
                if (c < 0) max = current;
                else if (c == 0) min = max;
                else
                {
                    if (min == current) min = max;
                    else min = current;
                }
            }

            base.InsertItem(min, item);
        }

        public OrderedObservableCollection(IComparer<T> comparer)
        {
            this.comparer = comparer;
        }
    }
}
