using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Math;

namespace WebChemistry.MotiveValidator.Service
{
    static class ChiralityAnalyzer
    {
        static double Det3(double[,] mat)
        {
            return -(mat[0, 2] * mat[1, 1] * mat[2, 0]) + mat[0, 1] * mat[1, 2] * mat[2, 0] +
               mat[0, 2] * mat[1, 0] * mat[2, 1] - mat[0, 0] * mat[1, 2] * mat[2, 1] -
               mat[0, 1] * mat[1, 0] * mat[2, 2] + mat[0, 0] * mat[1, 1] * mat[2, 2];
        }

        static void FillRow(double[,] m, Vector3D pivot, Vector3D a, int row)
        {
            m[row, 0] = pivot.X - a.X;
            m[row, 1] = pivot.Y - a.Y;
            m[row, 2] = pivot.Z - a.Z;
        }
        
        public static List<IAtom> GetAtomsWithDifferentChirality(MotiveEntry motive)
        {
            var model = motive.Model;
            var motif = motive.MatchedWithMovedBonds;
            var pairing = motive.ModelToMotivePairing;

            var modelBonds = model.Structure.Bonds;
            var motifBonds = motif.Bonds;

            IAtom a, b, c;
            IAtom q, x, y, z;

            double[,] P = new double[3, 3], Q = new double[3, 3];

            var ret = new List<IAtom>();
            
            foreach (var p in model.ChiralAtoms)
            {
                if (!pairing.TryGetValue(p, out q)) continue;
                
                if (model.NearPlanarAtoms.Contains(p))
                {
                    if (!MotiveModel.IsPlanar(q, motifBonds)) ret.Add(q);
                    continue;
                }

                var bonds = modelBonds[p];
                var bc = bonds.Count;
                
                for (int i = 0; i < bc - 2; i++)
                {
                    a = bonds[i].B;
                    if (!pairing.TryGetValue(a, out x)) continue;

                    FillRow(P, p.Position, a.Position, 0);
                    FillRow(Q, q.Position, x.Position, 0);

                    for (int j = i + 1; j < bc - 1; j++)
                    {
                        b = bonds[j].B;
                        if (!pairing.TryGetValue(b, out y)) continue;

                        FillRow(P, p.Position, b.Position, 1);
                        FillRow(Q, q.Position, y.Position, 1);

                        for (int k = j + 1; k < bc; k++)
                        {
                            c = bonds[k].B;
                            if (!pairing.TryGetValue(c, out z)) continue;

                            FillRow(P, p.Position, c.Position, 2);
                            FillRow(Q, q.Position, z.Position, 2);

                            double dP = Det3(P), dQ = Det3(Q);
                            //double mV = Math.Max(Math.Abs(dP), Math.Abs(dQ));                            
                            if (Math.Sign(dP) != Math.Sign(dQ) /*&& mV > 0.1 && Math.Abs(dP - dQ) > 0.05 * mV*/)
                            {
                                ret.Add(q);
                                goto I_am_going_to_hell_for_this;
                            }
                        }
                    }
                }

            I_am_going_to_hell_for_this: { }
            }

            return ret;
        }
    }
}
