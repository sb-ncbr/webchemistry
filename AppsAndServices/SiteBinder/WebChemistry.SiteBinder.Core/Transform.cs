// -----------------------------------------------------------------------
// <copyright file="Transform.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.SiteBinder.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Core;

    public static class OptimalTransformationEx
    {
        public static void Apply<T>(this OptimalTransformation t, MatchGraph<T> other)
        {
            t.Apply(other.Vertices, v => v.Position, (v, _, p) => v.Position = p);
        }

        public static void Apply<T>(this OptimalTransformation t, IList<VertexWrap<T>> other)
        {
            t.Apply(other, v => v.Position, (v, _, p) => v.Position = p);
        }
    }

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class OptimalTransformation
    {
        public Vector3D PivotOffset { get; set; }
        
        public Quaternion OtherRotation { get; set; }
        public Vector3D OtherOffset { get; set; }

        public double Rmsd { get; set; }

        Vector3D TransformOther(Vector3D v)
        {
            var q = OtherRotation;
            v = v + OtherOffset;
            return new Vector3D(
                (q.W) * (q.W) * v.X + (q.X) * (q.X) * v.X - ((q.Y) * (q.Y) + (q.Z) * (q.Z)) * v.X + q.W * (-2 * q.Z * v.Y + 2 * q.Y * v.Z) + 2 * q.X * (q.Y * v.Y + q.Z * v.Z),
                2 * q.W * q.Z * v.X + (q.W) * (q.W) * v.Y - (q.X) * (q.X) * v.Y + ((q.Y) * (q.Y) - (q.Z) * (q.Z)) * v.Y + 2 * q.Y * q.Z * v.Z + 2 * q.X * (q.Y * v.X - q.W * v.Z),
                q.W * (-2 * q.Y * v.X + 2 * q.X * v.Y) + 2 * q.Z * (q.X * v.X + q.Y * v.Y) + ((q.W) * (q.W) - (q.X) * (q.X) - (q.Y) * (q.Y) + (q.Z) * (q.Z)) * v.Z);
        }

        public void Apply(IStructure other)
        {
            other.TransformAtomPositions(a => TransformOther(a.Position) - PivotOffset);
        }

        public void Apply<T>(IEnumerable<T> otherSeq, Func<T, Vector3D> positionSelector, Action<T, int, Vector3D> positionAssign)
        {
            int index = 0;
            foreach (var p in otherSeq)
            {
                positionAssign(p, index, TransformOther(positionSelector(p)) - PivotOffset);
                index++;
            }
        }


        static int Solve<T>(EvdCache cache, IList<T> pivots, IList<T> others, Func<T, Vector3D> positions, int count, out Vector3D outOffsetPivot, out Vector3D outOffsetOther, out double outSizeSq)
        {
            if (count < 0) count = pivots.Count;

            var offsetPivot = new Vector3D();
            var offsetOther = new Vector3D();
            var sizeSq = 0.0;

            for (int i = 0; i < count; i++)
            {
                var pp = positions(pivots[i]);
                var po = positions(others[i]);
                offsetPivot += pp;
                offsetOther += po;
            }

            offsetPivot *= -1.0 / count;
            offsetOther *= -1.0 / count;

            var N = cache.Matrix;
            // Reset the matrix.
            N.Reset();
            for (int i = 0; i < count; i++)
            {
                var a = positions(others[i]) + offsetOther;
                var b = positions(pivots[i]) + offsetPivot;

                sizeSq += a.LengthSquared + b.LengthSquared;

                // Generated using Mathematica
                N[0, 0] += a.X * b.X + a.Y * b.Y + a.Z * b.Z; 
                N[0, 1] += -(a.Z * b.Y) + a.Y * b.Z; 
                N[0, 2] += a.Z * b.X - a.X * b.Z; 
                N[0, 3] += -(a.Y * b.X) + a.X * b.Y; 
                N[1, 0] += -(a.Z * b.Y) + a.Y * b.Z; 
                N[1, 1] += a.X * b.X - a.Y * b.Y - a.Z * b.Z; 
                N[1, 2] += a.Y * b.X + a.X * b.Y; 
                N[1, 3] += a.Z * b.X + a.X * b.Z; 
                N[2, 0] += a.Z * b.X - a.X * b.Z;
                N[2, 1] += a.Y * b.X + a.X * b.Y; 
                N[2, 2] += -(a.X * b.X) + a.Y * b.Y - a.Z * b.Z; 
                N[2, 3] += a.Z * b.Y + a.Y * b.Z; 
                N[3, 0] += -(a.Y * b.X) + a.X * b.Y; 
                N[3, 1] += a.Z * b.X + a.X * b.Z; 
                N[3, 2] += a.Z * b.Y + a.Y * b.Z; 
                N[3, 3] += -(a.X * b.X) - a.Y * b.Y + a.Z * b.Z;
            }
            Evd4x4.Compute(cache);
            outOffsetPivot = offsetPivot;
            outOffsetOther = offsetOther;
            outSizeSq = sizeSq;
            return count;
        }

        public static OptimalTransformation Find<T>(IList<T> pivots, IList<T> others, Func<T, Vector3D> positions, int count = -1)
        {
            return FindFast(new EvdCache(), pivots, others, positions, count);
        }

        internal static OptimalTransformation FindFast<T>(EvdCache cache, IList<T> pivots, IList<T> others, Func<T, Vector3D> positions, int count = -1)
        {
            Vector3D offsetPivot, offsetOther;
            double sizeSq;
            count = Solve(cache, pivots, others, positions, count, out offsetPivot, out offsetOther, out sizeSq);

            var t = cache.Matrix;
            //var ev = Math.Abs(cache.EigenValues[0]) > cache.EigenValues[3] ? 0 : 3;
            var ev = 3;
            var rmsd = sizeSq - 2.0 * Math.Abs(cache.EigenValues[ev]);

            return new OptimalTransformation
            {
                PivotOffset = offsetPivot,
                OtherOffset = offsetOther,
                OtherRotation = new Quaternion(t[1, ev], t[2, ev], t[3, ev], t[0, ev]),
                Rmsd = rmsd < 0.0 ? 0.0 : Math.Sqrt(rmsd / count)
            };
        }

        public static double FindRmsd<T>(IList<T> pivots, IList<T> others, Func<T, Vector3D> positions, int count = -1)
        {
            return FindRmsdFast(new EvdCache(), pivots, others, positions, count);            
        }

        internal static double FindRmsdFast<T>(EvdCache cache, IList<T> pivots, IList<T> others, Func<T, Vector3D> positions, int count = -1)
        {
            Vector3D offsetPivot, offsetOther;
            double sizeSq;
            count = Solve(cache, pivots, others, positions, count, out offsetPivot, out offsetOther, out sizeSq);

            //var ev = Math.Abs(cache.EigenValues[0]) > cache.EigenValues[3] ? 0 : 3;
            var ev = 3;
            var rmsd = sizeSq - 2.0 * Math.Abs(cache.EigenValues[ev]);
            return rmsd < 0.0 ? 0.0 : Math.Sqrt(rmsd / count);
        }

        internal static double FindRmsdWithoutTranslating<T>(EvdCache evdCache, T[] pivots, int pivotOffset, T[] others, Func<T, Vector3D> positions, int count)
        {            
            var sizeSq = 0.0;

            EvdCache evd = evdCache;
            var N = evd.Matrix;
            N.Reset();
            for (int i = 0; i < count; i++)
            {
                var a = positions(others[i]);
                var b = positions(pivots[pivotOffset + i]);

                sizeSq += a.LengthSquared + b.LengthSquared;

                // Generated using Mathematica
                N[0, 0] += a.X * b.X + a.Y * b.Y + a.Z * b.Z;
                N[0, 1] += -(a.Z * b.Y) + a.Y * b.Z;
                N[0, 2] += a.Z * b.X - a.X * b.Z;
                N[0, 3] += -(a.Y * b.X) + a.X * b.Y;
                N[1, 0] += -(a.Z * b.Y) + a.Y * b.Z;
                N[1, 1] += a.X * b.X - a.Y * b.Y - a.Z * b.Z;
                N[1, 2] += a.Y * b.X + a.X * b.Y;
                N[1, 3] += a.Z * b.X + a.X * b.Z;
                N[2, 0] += a.Z * b.X - a.X * b.Z;
                N[2, 1] += a.Y * b.X + a.X * b.Y;
                N[2, 2] += -(a.X * b.X) + a.Y * b.Y - a.Z * b.Z;
                N[2, 3] += a.Z * b.Y + a.Y * b.Z;
                N[3, 0] += -(a.Y * b.X) + a.X * b.Y;
                N[3, 1] += a.Z * b.X + a.X * b.Z;
                N[3, 2] += a.Z * b.Y + a.Y * b.Z;
                N[3, 3] += -(a.X * b.X) - a.Y * b.Y + a.Z * b.Z;

                // conjugate instead of transpose.
                //var l = new Quaternion(-a.X, -a.Y, -a.Z, 0).RightMultiplicationToMatrix();
                //l.Transpose();
                //var r = new Quaternion(b.X, b.Y, b.Z, 0).LeftMultiplicationToMatrix();
                //N += l * r;
            }

            Evd4x4.Compute(evd);
            var rmsd = sizeSq - 2.0 * evd.EigenValues[3];

            return rmsd < 0.0 ? 0.0 : Math.Sqrt(rmsd / count);
        }
    }
}
