using System.Collections.Generic;
using System.Windows.Markup;
using System.Collections;

namespace WebChemistry.Framework.Visualization
{    
    public class Visual3DCollection : ICollection<Visual3D>, IList<Visual3D>, ICollection, IList, IEnumerable<Visual3D>, IEnumerable
    {
        List<Visual3D> _visuals = new List<Visual3D>();

        #region ICollection<Visual3D> Members

        public void Add(Visual3D visual)
        {
            _visuals.Add(visual);
        }

        public void Clear()
        {
            _visuals.Clear();
        }

        public bool Contains(Visual3D visual)
        {
            return _visuals.Contains(visual);
        }

        public void CopyTo(Visual3D[] array, int arrayIndex)
        {
            _visuals.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _visuals.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Visual3D item)
        {
            return _visuals.Remove(item);
        }

        #endregion

        #region IEnumerable<Visual3D> Members

        public IEnumerator<Visual3D> GetEnumerator()
        {
            return _visuals.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _visuals.GetEnumerator();
        }

        #endregion

        #region IList<Visual3D> Members


        #endregion

        #region IList<Visual3D> Members

        public int IndexOf(Visual3D item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, Visual3D item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        public Visual3D this[int index]
        {
            get
            {
                return _visuals[index];
            }
            set
            {
                _visuals[index] = value;
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(System.Array array, int index)
        {
            CopyTo((Visual3D[])array, index);
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return _visuals; }
        }

        #endregion

        #region IList Members

        public int Add(object value)
        {
            _visuals.Add(value as Visual3D);
            return _visuals.Count - 1;
        }

        public bool Contains(object value)
        {
            return _visuals.Contains(value as Visual3D);
        }

        public int IndexOf(object value)
        {
            return _visuals.IndexOf(value as Visual3D);
        }

        public void Insert(int index, object value)
        {
            _visuals.Insert(index, value as Visual3D);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            _visuals.Remove(value as Visual3D);
        }

        object IList.this[int index]
        {
            get
            {
                return _visuals[index];
            }
            set
            {
                _visuals[index] = value as Visual3D;
            }
        }

        #endregion
    }
}