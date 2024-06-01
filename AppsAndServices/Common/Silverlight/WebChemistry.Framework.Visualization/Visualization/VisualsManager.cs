using System.Collections.Generic;
using System.Windows.Shapes;

namespace WebChemistry.Framework.Visualization
{
    public static class VisualsManager<T> where T : Shape, new()
    {
        static int maxSize = 10000;
        public static int MaxSize
        {
            get { return maxSize; }
            set
            {
                if (maxSize == value) return;

                visuals.Clear();
                maxSize = value;
            }
        }

        static Stack<T> visuals = new Stack<T>();
        
        public static T Withdraw()
        {
            if (visuals.Count == 0) return new T();
            return visuals.Pop();
        }

        public static void Deposit(T visual)
        {
            if (visual != null)
            {
                visual.Fill = null;
                visual.Stroke = null;
                if (visuals.Count < MaxSize) visuals.Push(visual);
            }
        }

        public static void Free()
        {
            visuals.Clear();
        }
    }
}