using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Math;
using WebChemistry.Queries;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Core.Queries;

namespace MiscTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var files = new System.IO.DirectoryInfo(args[0]).GetFiles().Where(f => f.FullName.EndsWith(".cif")).ToList();
            var i = 0;
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 16 }, (f) =>
            {
                i++;
                if (i % 10000 == 0)
                {
                    Console.WriteLine("Progress: {0}/{1}", i, files.Count);
                }
                try
                {
                    var s = StructureReader.Read(f.FullName).Structure;
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0}: {1}", f.Name, e.Message);
                }
                // Console.WriteLine(s.Atoms.Count);
            });

            //    var s = StructureReader.Read("C:/Projects/TestData/3dep.cif").Structure;
            //Console.WriteLine(s.Atoms.Count);
            //var qq = QueryBuilder.Cluster(4, QueryBuilder.Residues("NAD"), QueryBuilder.Residues("SO4")).ToMetaQuery().Compile();
            //var xs = qq.Matches(s);

            //Console.WriteLine("{0} {1}", s.Atoms.Count, xs.Count);
            //foreach (var x in xs)
            //{
            //    Console.WriteLine(x.Atoms.Count);
            //}



            //var p = new Plane3D(2, Math.Sqrt(17), -2, 5);
            //Console.WriteLine("{0} {1} {2} {3}", p.A, p.B, p.C, p.D);

            //p = new Plane3D(0, 0, 1, 0);
            //Console.WriteLine(p.ProjectToPlane(new Vector3D(2, 3, 15)));
            //return;

            //var qq = QueryBuilder.Stack2(1, 10, 0, 5, 0, 5, QueryBuilder.Rings(), QueryBuilder.Rings()).ToMetaQuery().Compile();
            //var s = StructureReader.Read("d:/testdata/quick/1a00.cif").Structure;

            //var xs = qq.Matches(s);



            //return;

            //var q = QueryBuilder.DistanceCluster(
            //     new[] {
            //    //QueryBuilder.Residues("ALA"),
            //    //QueryBuilder.Residues("ALA"),
            //    //QueryBuilder.Residues("HIS")
            //    QueryBuilder.Near(4.0, QueryBuilder.Atoms("Ca"), QueryBuilder.Atoms("Ca")).ConnectedResidues(1),
            //    QueryBuilder.Near(4.0, QueryBuilder.Atoms("Ca"), QueryBuilder.Atoms("Ca")).ConnectedResidues(1),
            //    QueryBuilder.Near(4.0, QueryBuilder.Atoms("Ca"), QueryBuilder.Atoms("Ca")).ConnectedResidues(1),
            //    QueryBuilder.Near(4.0, QueryBuilder.Atoms("Ca"), QueryBuilder.Atoms("Ca"))
            //},
            //    new[] { new List<double> { 3.0 }, new List<double> { 3.0, 3.0 }, new List<double> { 3.0, 3.0, 3.0 } },
            //    new[] { new List<double> { 100.0 }, new List<double> { 100.0, 100.0 }, new List<double> { 100.0, 100.0, 100.0 } }

            //).ToMetaQuery().Compile();

            //var q = new DistanceClusterQuery(new[]
            //{
            //    QueryBuilder.Residues("ALA").ToMetaQuery().Compile(),
            //    QueryBuilder.Residues("ALA").ToMetaQuery().Compile(),
            //    QueryBuilder.Residues("HIS").ToMetaQuery().Compile()
            //},
            //new[] { new[] { 0.0 }, new[] { 0.0, 0.0 } },
            //new[] { new[] { 9.0 }, new[] { 9.0, 9.0 } });

            //var s = StructureReader.Read(@"E:\Test\Quick\1gzt.pdb").Structure;
            //var r = q.Matches(s);
            //Console.WriteLine(r.Count);
        }
    }
}
