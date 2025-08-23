// -----------------------------------------------------------------------
// <copyright file="KdNodes.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Framework.Geometry
{
    using System.Collections.Generic;
    using WebChemistry.Framework.Math;
        
    abstract class K3DNode<TValue>
    {
        public abstract void Nearest(Vector3D pivot, double maxDistance, double maxDistanceSquared, IPriorityCollection<double, TValue> output);
        //public abstract void Nearest(Vector3D pivot, K3DRect rect, double maxDistanceSquared, IPriorityCollection<double, TValue> output);
        public abstract void Nearest(Vector3D pivot, K3DRect rect, int n, IPriorityArray<double, TValue> output);


        ////protected abstract void VisitNearest(Stack<K3DNode<TValue>> stack, Vector3D pivot, double maxDistance, double maxDistanceSquared, IPriorityCollection<double, TValue> output);

        /////// <summary>
        /////// non-recursive version of the nearest function.
        /////// </summary>
        /////// <param name="pivot"></param>
        /////// <param name="maxDistance"></param>
        /////// <param name="maxDistanceSquared"></param>
        /////// <param name="output"></param>
        ////public void NearestStack(Vector3D pivot, double maxDistance, double maxDistanceSquared, IPriorityCollection<double, TValue> output)
        ////{
        ////    var stack = new Stack<K3DNode<TValue>>();
        ////    stack.Push(this);

        ////    while (stack.Count > 0)
        ////    {
        ////        stack.Pop().VisitNearest(stack, pivot, maxDistance, maxDistanceSquared, output);
        ////    }
        ////}

        //struct StackFrame
        //{
        //    public K3DNode<TValue> Value;
        //    public double MinX = double.MinValue, MinY = double.MinValue, MinZ = double.MinValue;
        //    public double MaxX = double.MaxValue, MaxY = double.MaxValue, MaxZ = double.MaxValue;

        //    public double DistanceSquaredToClosestEdge(Vector3D v)
        //    {
        //        double x = 0, y = 0, z = 0;

        //        if (v.X <= MinX) x = v.X - MinX;
        //        else if (v.X >= MaxX) x = v.X - MaxX;

        //        if (v.Y <= MinY) y = v.Y - MinY;
        //        else if (v.Y >= MaxY) y = v.Y - MaxY;

        //        if (v.Z <= MinZ) z = v.Z - MinZ;
        //        else if (v.Z >= MaxZ) z = v.Z - MaxZ;

        //        return x * x + y * y + z * z;
        //    }
        //}
    }

    abstract class K3DSplitNode<TValue> : K3DNode<TValue>
    {
        public K3DNode<TValue> LeftChild { get; internal set; }
        public K3DNode<TValue> RightChild { get; internal set; }
                
        readonly double SplitValue;

        protected abstract double GetCoordinate(Vector3D vector);
        protected abstract void SetRectBounds(K3DRect rect, double min, double max);
        protected abstract void SetRectBoundsNext(K3DRect rect, double min, double max);
        protected abstract double GetRectMin(K3DRect rect);
        protected abstract double GetRectMax(K3DRect rect);
        protected abstract double GetRectMinNext(K3DRect rect);
        protected abstract double GetRectMaxNext(K3DRect rect);

        bool Intersects(double v, double radius)
        {
            if (v <= SplitValue)
            {
                if (v + radius > SplitValue) return true;
                return false;
            }
            else
            {
                if (v - radius < SplitValue) return true;
                return false;
            }
        }

        public override void Nearest(Vector3D pivot, double maxDistance, double maxDistanceSquared, IPriorityCollection<double, TValue> output)
        {
            var coord = GetCoordinate(pivot);

            if (Intersects(coord, maxDistance))
            {
                LeftChild.Nearest(pivot, maxDistance, maxDistanceSquared, output);
                RightChild.Nearest(pivot, maxDistance, maxDistanceSquared, output);
            }
            else
            {
                if (coord <= SplitValue) LeftChild.Nearest(pivot, maxDistance, maxDistanceSquared, output);
                else RightChild.Nearest(pivot, maxDistance, maxDistanceSquared, output);
            }
        }

        ////protected override void VisitNearest(Stack<K3DNode<TValue>> stack, Vector3D pivot, double maxDistance, double maxDistanceSquared, IPriorityCollection<double, TValue> output)
        ////{
        ////    var coord = GetCoordinate(pivot);

        ////    if (Intersects(coord, maxDistance))
        ////    {
        ////        stack.Push(LeftChild);
        ////        stack.Push(RightChild);
        ////    }
        ////    else
        ////    {
        ////        if (coord <= SplitValue) stack.Push(LeftChild);
        ////        else stack.Push(RightChild);
        ////    }
        ////}
        
        public override void Nearest(Vector3D pivot, K3DRect rect, int n, IPriorityArray<double, TValue> output)
        {
            bool onLeft = GetCoordinate(pivot) <= SplitValue;

            // store the next level bounds
            double min = GetRectMinNext(rect), max = GetRectMaxNext(rect);

            if (onLeft)
            {
                var old = GetRectMax(rect);
                SetRectBounds(rect, GetRectMin(rect), SplitValue);
                LeftChild.Nearest(pivot, rect, n, output);
                SetRectBounds(rect, SplitValue, old);
            }
            else
            {
                var old = GetRectMin(rect);
                SetRectBounds(rect, SplitValue, GetRectMax(rect));
                RightChild.Nearest(pivot, rect, n, output);
                SetRectBounds(rect, old, SplitValue);
            }

            // restore the next level bounds
            SetRectBoundsNext(rect, min, max);

            var furthest = n == output.Count 
                ? output.GetLargestPriority(double.MaxValue) 
                : double.MaxValue;
            var closest = rect.DistanceSquaredToClosestEdge(pivot);

            if (closest <= furthest)
            {
                // store the next level bounds
                min = GetRectMinNext(rect);
                max = GetRectMaxNext(rect);

                if (onLeft) RightChild.Nearest(pivot, rect, n, output);
                else LeftChild.Nearest(pivot, rect, n, output);

                // restore the next level bounds
                SetRectBoundsNext(rect, min, max);
            }
        }

        protected K3DSplitNode(double splitValue)
        {
            this.SplitValue = splitValue;
        }
    }

    class XSplitNode<TValue> : K3DSplitNode<TValue>
    {
        protected override double GetCoordinate(Vector3D vector) { return vector.X; }
        protected override void SetRectBounds(K3DRect rect, double min, double max) { rect.MinX = min; rect.MaxX = max; }
        protected override void SetRectBoundsNext(K3DRect rect, double min, double max) { rect.MinY = min; rect.MaxY = max; }
        protected override double GetRectMin(K3DRect rect) { return rect.MinX; }
        protected override double GetRectMax(K3DRect rect) { return rect.MaxX; }
        protected override double GetRectMinNext(K3DRect rect) { return rect.MinY; }
        protected override double GetRectMaxNext(K3DRect rect) { return rect.MaxY; }

        public XSplitNode(double splitValue)
            : base(splitValue)
        {

        }
    }

    class YSplitNode<TValue> : K3DSplitNode<TValue>
    {
        protected override double GetCoordinate(Vector3D vector) { return vector.Y; }
        protected override void SetRectBounds(K3DRect rect, double min, double max) { rect.MinY = min; rect.MaxY = max; }
        protected override void SetRectBoundsNext(K3DRect rect, double min, double max) { rect.MinZ = min; rect.MaxZ = max; }
        protected override double GetRectMin(K3DRect rect) { return rect.MinY; }
        protected override double GetRectMax(K3DRect rect) { return rect.MaxY; }
        protected override double GetRectMinNext(K3DRect rect) { return rect.MinZ; }
        protected override double GetRectMaxNext(K3DRect rect) { return rect.MaxZ; }

        public YSplitNode(double splitValue)
            : base(splitValue)
        {

        }
    }

    class ZSplitNode<TValue> : K3DSplitNode<TValue>
    {
        protected override double GetCoordinate(Vector3D vector) { return vector.Z; }
        protected override void SetRectBounds(K3DRect rect, double min, double max) { rect.MinZ = min; rect.MaxZ = max; }
        protected override void SetRectBoundsNext(K3DRect rect, double min, double max) { rect.MinX = min; rect.MaxX = max; }
        protected override double GetRectMin(K3DRect rect) { return rect.MinZ; }
        protected override double GetRectMax(K3DRect rect) { return rect.MaxZ; }
        protected override double GetRectMinNext(K3DRect rect) { return rect.MinX; }
        protected override double GetRectMaxNext(K3DRect rect) { return rect.MaxX; }

        public ZSplitNode(double splitValue)
            : base(splitValue)
        {

        }
    }

    class K3DLeafNode<TValue> : K3DNode<TValue>
    {
        readonly Vector3DValuePair<TValue>[] values;

        public K3DLeafNode(Vector3DValuePair<TValue>[] values)
        {
            this.values = values;
        }

        public K3DLeafNode(Vector3DValuePair<TValue>[] values, int startIndex, int count)
        {
            this.values = new Vector3DValuePair<TValue>[count];
            for (int i = 0; i < count; i++)
            {
                this.values[i] = values[startIndex + i];
            }
        }

        public override void Nearest(Vector3D pivot, double maxDistance, double maxDistanceSquared, IPriorityCollection<double, TValue> output)
        {
            var vals = values;
            for (int i = 0; i < vals.Length; i++)
            {
                var value = vals[i];
                var dist = value.Position.DistanceToSquared(pivot);
                if (dist <= maxDistanceSquared) output.Add(dist, value.Value);
            }
        }

        ////protected override void VisitNearest(Stack<K3DNode<TValue>> stack, Vector3D pivot, double maxDistance, double maxDistanceSquared, IPriorityCollection<double, TValue> output)
        ////{
        ////    Nearest(pivot, maxDistance, maxDistanceSquared, output);
        ////}

        //public override void Nearest(Vector3D pivot, K3DRect rect, double maxDistanceSquared, IPriorityCollection<double, TValue> output)
        //{
        //    var vals = values;
        //    for (int i = 0; i < vals.Length; i++)
        //    {
        //        var value = vals[i];
        //        var dist = value.Position.DistanceToSquared(pivot);
        //        if (dist <= maxDistanceSquared) output.Add(dist, value.Value);
        //    }
        //}

        public override void Nearest(Vector3D pivot, K3DRect rect, int n, IPriorityArray<double, TValue> output)
        {
            var vals = values;
            var p = output.GetLargestPriority(double.MaxValue);
            for (int i = 0; i < vals.Length; i++)
            {
                var value = vals[i];

                var dist = value.Position.DistanceToSquared(pivot);
                if (output.Count < n || dist < p) output.Add(dist, value.Value);
            }
        }
    }
}
