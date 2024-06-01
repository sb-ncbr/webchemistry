using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using WebChemistry.Framework.Math;
using WebChemistry.Framework.Geometry;
using System.IO;
using System.Diagnostics;
using WebChemistry.Framework.Core.Utils;
using System.Globalization;

namespace MiscTests
{
    class Program
    {
        class Vertex : IVertex
        {
            public Vector Position
            {
                get;
                set;
            }
        }

        class Vertex3D : IVertex3D
        {
            public Vector3D Position
            {
                get;
                set;
            }
        }

        static TimeSpan Test2D()
        {
            const int NumberOfVertices = 1000000;
            const double size = 100;

            var r = new Random();
           // Console.WriteLine("Ready? Push Return/Enter to start.");
           // Console.ReadLine();

            Console.WriteLine("Making " + NumberOfVertices + " random vertices.");
            //var vertices = new List<IVertexConvHull>();
            //for (int i = 0; i < NumberOfVertices; i++)
            //    vertices.Add(new vertex(size * r.NextDouble(), size * r.NextDouble()));

            var vertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < NumberOfVertices; i++) vertices[i] = new Vertex { Position = new Vector(size * r.NextDouble(), size * r.NextDouble()) };
            Console.WriteLine("Running...");
            var now = DateTime.Now;
            var convexHull = ConvexHull.Create(vertices).Points;
            var interval = DateTime.Now - now;
            Console.WriteLine("Out of the " + NumberOfVertices + " " + convexHull.First().Position.Dimension + "D vertices, there are " +
                convexHull.Count() + " on the convex hull.");
            Console.WriteLine("time = " + interval);

            GC.Collect();

            return interval;
        }

        static Tuple<TimeSpan, int> TestND(int dim, int numVert, double size = 100)
        {
            var r = new Random();

            var vertices = new Vertex[numVert];
            for (var i = 0; i < numVert; i++)
            {
                double[] v = new double[dim];
                for (int j = 0; j < dim; j++) v[j] = size * r.NextDouble();
                vertices[i] = new Vertex { Position = new Vector(v, false) };
            }

            var now = DateTime.Now;
            var convexHull = ConvexHull.Create(vertices).Points;
            var interval = DateTime.Now - now;
            //Console.WriteLine("{0} {1}D vertices: {2} on the convex hull. Time = {3:0.000}s", numVert, dim, convexHull.Count(), interval.TotalSeconds);
            GC.Collect();
            return Tuple.Create(interval, convexHull.Count());
        }

        static void Test6D()
        {
            const int NumberOfVertices = 5000;
            const double size = 100;
            const int dimension = 4;

            var r = new Random();
            Console.WriteLine("Ready? Push Return/Enter to start.");
            Console.ReadLine();

            Console.WriteLine("Making " + NumberOfVertices + " random 6D vertices.");
            var vertices = new List<Vertex>();
            for (var i = 0; i < NumberOfVertices; i++)
            {
                var location = new double[dimension];
                for (var j = 0; j < dimension; j++)
                    location[j] = size * r.NextDouble();
                vertices.Add(new Vertex { Position = new Vector(location, false) });
            }
            Console.WriteLine("Running...");
            var now = DateTime.Now;
            var voronoi = VoronoiMesh.Create(vertices);
            //var hull = ConvexHull.Create(vertices);
            var interval = DateTime.Now - now;
            //Console.WriteLine("Out of the " + NumberOfVertices + " vertices, there are " +
            //    hull.Hull.Count() + " on the convex hull.");
            Console.WriteLine("Out of the " + NumberOfVertices + " vertices, there are " +
                voronoi.Vertices.Count() + " voronoi points and " + voronoi.Edges.Count() + " voronoi edges.");
            Console.WriteLine("time = " + interval);
        }

        static void Test6DConvex()
        {
            const int NumberOfVertices = 5000;
            const double size = 1000;
            const int dimension = 4;

            IObservable<int> o;

            var r = new Random();
            Console.WriteLine("Ready? Push Return/Enter to start.");
            Console.ReadLine();

            Console.WriteLine("Making " + NumberOfVertices + " random 6D vertices.");
            var vertices = new List<Vertex>();
            for (var i = 0; i < NumberOfVertices; i++)
            {
                var location = new double[dimension];
                for (var j = 0; j < dimension; j++)
                    location[j] = size * r.NextDouble();
                vertices.Add(new Vertex { Position = new Vector(location, false) });
            }
            Console.WriteLine("Running...");
            var now = DateTime.Now;
            var hull = ConvexHull.Create(vertices);
            //var hull = ConvexHull.Create(vertices);
            var interval = DateTime.Now - now;
            //Console.WriteLine("Out of the " + NumberOfVertices + " vertices, there are " +
            //    hull.Hull.Count() + " on the convex hull.");
            Console.WriteLine("Out of the " + NumberOfVertices + " 6D vertices, there are " +
                hull.Points.Count() + " points on the convex hull");
            Console.WriteLine("time = " + interval);
        }

        public static long GetBondId(int a, int b)
        {
            long i = a;
            long j = b;

            if (i > j)
            {
                return (j << 32) | i;
            }
            return (i << 32) | j;
        }

        public static int GetHash(long key)
        {
            //int hash = 23;
            //hash = hash * 31 + (int)(obj & 0xFFFFFFFF);
            //return hash * 31 + (int)(obj >> 32);
            key = (~key) + (key << 18); // key = (key << 18) - key - 1;
              key = key ^ (key >> 31);
              key = key * 21; // key = (key + (key << 2)) + (key << 4);
              key = key ^ (key >> 11);
              key = key + (key << 6);
              key = key ^ (key >> 22);
              return (int) key;
        }

        class V3D
        {
            public double X, Y, Z;
        }

        static void TestDots()
        {
            List<double[]> arrays = new List<double[]>();
            Vector3D[] vecs = new Vector3D[100000];

            for (int i = 0; i < 100000; i++)
            {
                arrays.Add(new double[] { i, i + 1, i + 2 });
                vecs[i] = new Vector3D(i, i + 1, i + 2);
            }

            var start = DateTime.Now;

            double[] p = new double[] { 1,2, 3};
            double sum = 0;
            for (int i = 0; i < 3000; i++)
            {
                double acc = 0.0;
                var v = arrays[i % 100000];
                for (int j = 0; j < 3; j++) acc += p[j] * v[j];
                sum += acc;
            }
            
            Console.WriteLine("Arrays: The sum {0} computed in {1}", sum, (DateTime.Now - start).TotalMilliseconds);

            start = DateTime.Now;
            var vv = new V3D { X = 1, Y = 2, Z = 3 };
            var zz = new V3D { X = 3, Y = 4, Z = 5 };
            sum = 0;
            for (int i = 0; i < 300000000; i++)
            {
                //var v = vv;//vecs[i % 100000];
                sum += zz.X * vv.X + zz.Y * vv.Y + zz.Z * vv.Z;
                //sum += zz.X * vv.X;
                //sum += zz.Y * vv.Y;
                //sum += vv.Z * zz.Z;
            }
            Console.WriteLine("Vecs3D: The sum {0} computed in {1}ms", sum, (DateTime.Now - start).TotalMilliseconds);
        }

        //static void TestKD(IStructure s)
        //{
        //    K3DTree<IAtom> kd = new K3DTree<IAtom>(s.Atoms, a => a.Position);

        //    var fe = s.Atoms.First(a => a.ElementSymbol == ElementSymbols.Fe);

        //    var near = kd.Nearest(fe.Position, 10, 60);

        //    Console.WriteLine(near.All(a => a.Position.DistanceTo(fe.Position) <= 60));

        //    foreach (var a in near) Console.WriteLine(a.Id);
        //}

        static void TestAlloc()
        {
            var start = DateTime.Now;
            int len = 0;
            for (int i = 0; i < 1000000; i++)
            {
                var p = new double[3];
                len += p.Length;
                Array.Resize(ref p, 4);
                len += p.Length;
            }
            Console.WriteLine("L: {0} in {1} ms", len, (DateTime.Now - start).TotalMilliseconds);
        }

        static void TestNDArray()
        {
            var membefore = GC.GetTotalMemory(false);
            int nX = 350, nY = 350, nZ = 350;
            double density = 0.75;

            var start = DateTime.Now;
            var scalarField = new double[nX][][];
            for (int i = 0; i < nX; i++)
            {
                scalarField[i] = new double[nY][];
                for (int j = 0; j < nY; j++)
                {
                    scalarField[i][j] = new double[nZ];
                    for (int k = 0; k < nZ; k++) scalarField[i][j][k] = 1.0 / density;
                }
            }

            Console.WriteLine("L: {0} in {1} ms, with {2} MB", scalarField[0][0][0], (DateTime.Now - start).TotalMilliseconds, (GC.GetTotalMemory(false) - membefore) / 1024.0 / 1024.0);
        }

        static void TestLongHash()
        {
            List<long> numbers = new List<long>();
            List<int> xorHash = new List<int>();

            var rand = new Random();

            for (long i = 0; i < 100000; i++)
            {
                long u = rand.Next();
                long v = rand.Next();
                numbers.Add((u << 32) | v);

                //for (long j = 0; j < 300; j++)
                //{
                //    numbers.Add(i < j ? (i << 32) | j : (j << 32) | i);
                //    xorHash.Add((int)(i ^ j));
                //}
            }

            Console.WriteLine("Distinct numbers: {0}", numbers.Distinct().Count());
            Console.WriteLine("Distinct hashes: {0}", numbers.Select(n => n.GetHashCode()).Distinct().Count());
            Console.WriteLine("Distinct XOR hashes: {0}", xorHash.Distinct().Count());
        }

        static void TestRings()
        {
            //var s = StructureReader.ReadPdb("i:/test/motives/ca/1gzt.pdb", "t", true);
            //var s = StructureReader.ReadPdb("i:/test/ferings.pdb", "t", true);
            var s = StructureReader.Read("i:/1jj2.pdb", "t", true);
            //var s = CycleTestStructure();
            var start = DateTime.Now;
            var rings = s.Rings();//.GetRingsByFingerprint("CCCCCC");
            Console.WriteLine("Rings took {0}", (DateTime.Now - start));
            Console.WriteLine(rings.Count());
            //foreach (var r in rings) Console.WriteLine("{0} - {{{1}}}", r, string.Join("-", r.Atoms.Select(a => a.Id.ToString())));
        }

        static IStructure CycleTestStructure()
        {
            var atoms = AtomCollection.Create(Enumerable.Range(0, 9).Select(i => Atom.Create(i, ElementSymbols.C)));
            Func<int, int, IBond> bond = (i, j) => Bond.Create(atoms[i], atoms[j], BondType.Single);

            var bonds = BondCollection.Create(new IBond[] {
                bond(0, 1),
                bond(0, 3),
                bond(1, 2),
                bond(1, 4),
                bond(2, 5),
                bond(3, 4),
                bond(3, 6),
                bond(4, 5),
                bond(4, 7),
                bond(5, 8),
                bond(6, 7),
                bond(7, 8),
            });

            return Structure.Create("X", atoms, bonds);
        }

        static void TestFirst()
        {
            var hashset = Enumerable.Range(0, 10000).ToHashSet();
            var dict = Enumerable.Range(0, 10000).ToDictionary(i => i);

            var start = DateTime.Now;
            int sum = 0;
            for (int i = 0; i < 100000; i++) sum += hashset.First();
            Console.WriteLine("Hashset took {0} ({1})", (DateTime.Now - start), sum);

            start = DateTime.Now;
            sum = 0;
            for (int i = 0; i < 100000; i++) sum += dict.Values.First();
            Console.WriteLine("Dict took {0} ({1})", (DateTime.Now - start), sum);
        }

        static Tuple<Vertex[], Vertex3D[]> RandomPoints3D(int numVert, double size = 100)
        {
            int dim = 3;
            var r = new Random();

            var verticesND = new Vertex[numVert];
            var vertices3D = new Vertex3D[numVert];
            for (var i = 0; i < numVert; i++)
            {
                double[] v = new double[dim];
                for (int j = 0; j < dim; j++) v[j] = size * r.NextDouble();
                verticesND[i] = new Vertex { Position = new Vector(v, false) };
                vertices3D[i] = new Vertex3D { Position = new Vector3D(v[0], v[1], v[2]) };
            }

            return Tuple.Create(verticesND, vertices3D);
        }

        //static void TestSplit()
        //{
        //    WebChemistry.Framework.Geometry.Vector3DValuePair<double>[] e = new WebChemistry.Framework.Geometry.Vector3DValuePair<double>[]
        //    {
        //        new WebChemistry.Framework.Geometry.Vector3DValuePair<double>(new Vector3D(0,0,0), 1),
        //        new WebChemistry.Framework.Geometry.Vector3DValuePair<double>(new Vector3D(1,0,0), 1),
        //        new WebChemistry.Framework.Geometry.Vector3DValuePair<double>(new Vector3D(4,0,0), 1),
        //        new WebChemistry.Framework.Geometry.Vector3DValuePair<double>(new Vector3D(0.5,0,0), 1),
        //        new WebChemistry.Framework.Geometry.Vector3DValuePair<double>(new Vector3D(2,0,0), 1)
        //    };

        //    K3DTree<double>.Split(e, 0, 4, 1, 0);

        //    foreach (var t in e) Console.WriteLine(t.Position);
        //}

        static void TestK3DTreeNearest(int count)
        {
            //var tree = K3DTree.Create(new Vector3D[] { new Vector3D(0, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), new Vector3D(1, 1, 0), new Vector3D(1, 0, 1),
            //    new Vector3D(0, 1, 1), new Vector3D(1, 1, 1)}, v => v, 1);

            var tree = K3DTree.Create(StructureReader.Read("i:\\1JJ2.pdb", "t", false).Atoms, a => a.Position, 3, K3DPivotSelectionMethod.Average);

            var t = new Vector3D(0.1, 0, 0);
            var xx = 0;

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {                
                var r = tree.NearestCount(t, 3);
                //foreach (var x in r) Console.WriteLine(x.Value);
                xx += r.Count;
            }

            Console.WriteLine("Near new took {0}ms ({1})", sw.ElapsedMilliseconds, xx);

            sw.Stop();

            //foreach (var x in r) Console.WriteLine(x.Value);
        }

        //static void TestK3DTreeNearestOld(int count)
        //{
        //    //var tree = K3DTree.Create(new Vector3D[] { new Vector3D(0, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), new Vector3D(1, 1, 0), new Vector3D(1, 0, 1),
        //    //    new Vector3D(0, 1, 1), new Vector3D(1, 1, 1)}, v => v, 1);

        //    var tree = new K3DTreeOld<IAtom>(StructureReader.ReadPdb("i:\\1JJ2.pdb", "t", false).Atoms, a => a.Position);

        //    var t = new Vector3D(0.1, 0, 0);
        //    var xx = 0;

        //    Stopwatch sw = Stopwatch.StartNew();
        //    for (int i = 0; i < count; i++)
        //    {
        //        var r = tree.Nearest(t, 3);
        //        xx += r.AsList().Count;
        //    }

        //    Console.WriteLine("Near new took {0}ms ({1})", sw.ElapsedMilliseconds, xx);

        //    sw.Stop();

        //    //foreach (var x in r) Console.WriteLine(x.Value);
        //}

        //static void TestK3DTreeNearestM(int count)
        //{
        //    var tree = K3DTree.Create(StructureReader.ReadPdb("i:\\1JJ2.pdb", "t", false).Atoms, a => a.Position, 3, K3DPivotSelectionMethod.Average);
        //    var tree1 = new K3DTreeOld<IAtom>(StructureReader.ReadPdb("i:\\1JJ2.pdb", "t", false).Atoms, a => a.Position);

        //    var t = new Vector3D(78, 55, 62);
        //    var xx = 0;

        //    for (int i = 0; i < count; i++)
        //    {
        //        var r = tree.Nearest(t, 5);
        //        foreach (var x in r) Console.WriteLine(x.Value.Id);

        //        Console.WriteLine("-------------");

        //        var r1 = tree1.Nearest(t, 5);
        //        foreach (var x in r1) Console.WriteLine(x.Id);
        //    }
        //}

        static void TestTree()
        {
            var atoms = StructureReader.Read("i:\\1JJ2.pdb", computeBonds: false).Atoms.ToArray();

            var sw = Stopwatch.StartNew();

            var t = K3DTree.Create(atoms, a => a.Position, pivotSelectionMethod: K3DPivotSelectionMethod.Average);

            var r = 0;

            for (int i = 0; i < atoms.Length; i++)
            {
                var ret = t.Nearest(atoms[i].Position, 5, 2);
                r += ret.Count;
            }

            sw.Stop();

            Console.WriteLine("Timing: {0}ms ({1})", sw.ElapsedMilliseconds, r);
        }


        static void TestTree1()
        {
            var atoms = StructureReader.Read("i:\\1JJ2.pdb", computeBonds: false).Atoms.ToArray();

            var sw = Stopwatch.StartNew();

            var t = K3DTree.Create(atoms, a => a.Position, pivotSelectionMethod: K3DPivotSelectionMethod.Average);

            var r = 0;

            for (int i = 0; i < atoms.Length; i++)
            {
                var ret = t.Nearest(atoms[i].Position, 5, 2);
                r += ret.Count;
            }

            sw.Stop();

            Console.WriteLine("Timing1: {0}ms ({1})", sw.ElapsedMilliseconds, r);
        }

        class OCSplitInfo
        {
            public int[] regCounts = new int[8];
            public int[] cs = new int[8];
        }
        class OCSIFactory
        {
            Stack<OCSplitInfo> stack;

            public OCSplitInfo Get()
            {
                if (stack.Count == 0) return new OCSplitInfo();
                return stack.Pop();
            }

            public void Deposit(OCSplitInfo i)
            {
                // recycle
                for (int j = 0; j < i.regCounts.Length; j++) i.regCounts[j] = 0;
                stack.Push(i);
            }
        }

        abstract class OC3DNode<T> {}

        class OC3DSplitNode<T> : OC3DNode<T> 
        {
            OC3DNode<T>[] sub = new OC3DNode<T>[8];
            int start, end;
            Octree<T> parent;
        }
        class OC3DLeafNode<T> : OC3DNode<T> 
        {
            int start, end;
            Octree<T> parent;
        }

        class Octree<T>
        {
            T[] data;


            void Build()
            {
            }
        }

        class OCBuffers<T>
        {
            public KeyValuePair<Vector3D, T>[] main, secondary;
            public OCSIFactory infos;

            Func<double, int>[] splitters;

            int Region(Vector3D value)
            {
                return splitters[0](value.X) |  splitters[1](value.Y) << 1 | splitters[2](value.Z) << 2;
            }

            OC3DNode<T> BuildR(int start, int end)
            {
                if (end - start < 5)
                {
                    return new OC3DLeafNode<T>();
                }

                var info = infos.Get();

                for (int i = start; i < end; i++)
                {
                    info.regCounts[Region(main[i].Key)]++;
                }

                var ro = info.regCounts.Scan(start, (s, c) => s + c).ToArray();
                
                // copy to secondary
                for (int i = start; i < end; i++)
                {
                    var r = Region(main[i].Key);
                    secondary[ro[r] + info.cs[r]] = main[r];
                    info.cs[r]++;
                }

                var t = main;
                main = secondary;
                secondary = main;

                OC3DNode<T>[] nodes = new OC3DNode<T>[8];
                for (int i = 0; i < 8; i++)
                {
                    nodes[i] = BuildR(info.cs[i], info.cs[i] + info.regCounts[i]);
                }
                
                infos.Deposit(info);

                return new OC3DSplitNode<T>();
            }
        }

        int GetIntervalPosition(double x, double low, double high)
        {
            if (x < low) return 1; // "under" the inteval, binary 01
            if (x > high) return 2; // "above" the interval, binary 10
            return 3; // inside the interval, binary 11
        }

        void Visit(Vector3D pivot, double radius, Vector3D boxCenter)
        {
            int ipX = GetIntervalPosition(boxCenter.X, pivot.X - radius, pivot.X + radius);
            int ipY = GetIntervalPosition(boxCenter.Y, pivot.Y - radius, pivot.Y + radius);
            int ipZ = GetIntervalPosition(boxCenter.Z, pivot.Z - radius, pivot.Z + radius);

            for (int i = 0; i < 8; i++)
            {
                // i is a 3-bit number that determines the segment
                // say i = ZYX (ie. 0th bit from SplitX, 1st bit for SplitY, 2nd bit for SplitZ), then
                // X = i & 1, Y = (i >> 1) & 1, Z = i >> 2
                //
                // then to determine if "x part" of the segment should be visited can be computed by
                // visitX = (ipX & (X + 1)) > 0
                // because if X = 0, then it's the lower X segment and the lower bit if ipX must be set to 1
                // because if X = 1, then it's the upper X segment and the higher bit if ipX must be set to 1
                //
                // and similarly for visitX and visitY

                // together ...
                bool visit = (ipX & ((i & 1) + 1)) > 0
                            && (ipY & (((i >> 1) & 1) + 1)) > 0
                            && (ipZ & ((i >> 2) + 1)) > 0;
                if (visit) 
                {
                    // visit cell i
                }
            }
        }
        
        public class QueryGrammar : SharedGrammar
        {
            public static Rule InnerSimpleElement = Node(OneOrMore(MatchChar(c => Char.IsLetterOrDigit(c))));
            public static Rule InnerCountedElement = Node(Integer + WS + CharToken('-') + WS + OneOrMore(MatchChar(c => Char.IsLetterOrDigit(c))));

            public static Rule InnerElement = InnerCountedElement | InnerSimpleElement;

            public static Rule SimpleElement = Node(MatchChar(c => Char.IsLetter(c)) + ZeroOrMore(MatchChar(c => Char.IsLetterOrDigit(c))));//  Node(Identifier);
            public static Rule CountedElement = Node(Integer + WS + CharToken('-') + WS + MatchChar(c => Char.IsLetter(c)) + ZeroOrMore(MatchChar(c => Char.IsLetterOrDigit(c))));

            public static Rule Element = CountedElement | SimpleElement;

            public static Rule AtomSet = Node(CharToken('[') + Not(CharToken('^') | CharToken('#')) + CommaDelimited(WS + InnerSimpleElement) + CharToken(']'));
            public static Rule NegAtomSet = Node(CharToken('[') + CharToken('^') + CommaDelimited(WS + InnerSimpleElement) + CharToken(']'));
            public static Rule ResidueSet = Node(CharToken('{') + Not(CharToken('^') | CharToken('#')) + CommaDelimited(WS + InnerSimpleElement) + CharToken('}'));
            public static Rule NegResidueSet = Node(CharToken('{') + CharToken('^') + CommaDelimited(WS + InnerSimpleElement) + CharToken('}'));

            public static Rule String = Node(DoubleQuotedString);
            public static Rule Numeric = Node(Number);

            public static Rule Ring = Node(CharToken('@') + CharToken('(') + CommaDelimited(Element) + CharToken(')'));

            public static Rule Ref = Node(CharToken('$') + Not(CharToken('(')) + Identifier);

            public static Rule Surroundings = Node(CharToken('$') + CharToken('(') + CommaDelimited(Recursive(() => Argument)) + CharToken(')'));
            public static Rule SurroundedBy = Node(CharToken('$') + CharToken('!') + CharToken('(') + CommaDelimited(Recursive(() => Argument)) + CharToken(')'));

            public static Rule Head = Node(Identifier);
            public static Rule Argument = Recursive(() => Apply) | AtomSet | NegAtomSet | ResidueSet | NegResidueSet | String | Element | Numeric | Ring | Surroundings | SurroundedBy | Ref;
            public static Rule Arguments = Node(Parenthesize(CommaDelimited(Argument)));

            public static Rule Apply = Node(Head + Arguments);

            public static Rule Query = Apply | AtomSet | NegAtomSet | ResidueSet | NegResidueSet | Ring | Surroundings | SurroundedBy;

            static QueryGrammar()
            {
                InitGrammar(typeof(QueryGrammar));
            }
        }

        static void TestCSVExport()
        {
            var data = Enumerable.Range(0, 10).Select(x => new { X = x, S = "str" + x }).ToArray();

            var export = data.GetExporter(xmlRootName: "Test", xmlElementName: "Node")
                .AddExportableColumn(x => x.X, ColumnType.Number, "X")
                .AddExportableColumn(x => x.S, ColumnType.String, "S")
                .AddExportableColumn(x => x.S + "\"" + x.X, ColumnType.String, "T");

            Console.WriteLine(export.ToCsvString());
            Console.WriteLine("---------------------");
            Console.WriteLine(export.ToXml().ToString());
        }

        public static int ParseIntFast(string str)
        {
            int val = 0;
            bool neg = false;
            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (char.IsWhiteSpace(c)) continue;
                else if (char.IsDigit(c))
                {
                    val = val * 10 + (c - '0');
                }
                else if (c == '-') neg = true;
            }
            return neg ? -val : val;
        }

        public static double ParseDoubleFast(string str)
        {
            double main = 0;
            double point = 0;
            bool neg = false;
            int i;
            for (i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (char.IsWhiteSpace(c)) continue;
                else if (char.IsDigit(c))
                {
                    main = main * 10 + (c - '0');
                }
                else if (c == '-') neg = true;
                else if (c == '.') break;
            }
            double div = 1;
            for (i = i + 1; i < str.Length; i++)
            {
                var c = str[i];
                if (char.IsWhiteSpace(c)) break;
                else if (char.IsDigit(c))
                {
                    div *= 10;
                    point = point * 10 + (c - '0');
                }
            }
            return neg ? -(main + point / div) : (main + point / div);
        }

        static dynamic Plus(dynamic a, dynamic b)
        {
            return a + b;
        }

        class MyValue
        {
            public static MyValue operator +(MyValue l, MyValue r)
            {
                var sx = l as MyString;
                var sy = r as MyString;

                if (sx != null && sy != null)
                {
                    return new MyString { Value = sx.Value + sy.Value };
                }

                var x = l as MyDouble;
                var y = r as MyDouble;
                if (x != null && y != null) return new MyDouble { Value = x.Value + y.Value };
                return null;
            }
        }

        class MyDouble : MyValue
        {
            public double Value { get; set; }
        }

        class MyString : MyValue
        {
            public string Value { get; set; }
        }


        static void TestDescriptors()
        {
            var s = Structure.Create("s", AtomCollection.Empty);

            var desc = s.Descriptors();
            desc["test"] = 1;

            Console.WriteLine(desc["test"]);

            desc.RemoveDescriptor("test");


            Console.WriteLine(desc["test"]);

            //desc.
        }
    


        static Func<dynamic, dynamic, dynamic> sf = (x, y) => x + y;

        static void Main(string[] args)
        {

            ////TestDescriptors();
            ////return;

            ////var sump = 0;
            ////var inputFn = @"I:\test\SiteBinder\MultipleIn\ma_2jdm_3367.pdb";
            ////inputFn = @"I:\test\Queries\Databases\PDBDataSample250\4a41.pdb";
            ////inputFn = @"I:\test\charges\nsc\NSC1000004.pdb";
            ////inputFn = "i:/1GTV.pdb";
            ////Benchmark.Run(() =>
            ////    {
            ////        var xsxs = StructureReader.Read(inputFn, computeBonds: false);
            ////        //Console.WriteLine(xsxs.Bonds.Any(b => b.A.Id == b.B.Id));
            ////        //xsxs.ReadBonds("i:/1htq.bnd");
            ////        //xsxs.ReadRings("i:/1htq.rng");
            ////        //xsxs.Rings();
            ////        //xsxs.Rings();
            ////        //Console.WriteLine(xsxs.Rings().Sum(r => r.Atoms.Count()));
            ////        //File.WriteAllLines("i:/restt.txt", xsxs.PdbResidues().Select(r => r.ToString()).ToArray());
            ////        //Console.WriteLine("{0} atoms, {1} bonds", xsxs.Atoms.Count, xsxs.Bonds.Count);
            ////        sump = xsxs.Atoms.Count;
            ////    }, runToJIT: true, timesToRun: 1, name: "Parse");
            ////Console.WriteLine(sump);
            ////return;

            //var sump = 0.0;
            //Benchmark.Run(() =>
            //    {
            //        for (int i = 0; i < 10000000; i++)
            //        {
            //            var inp = i.ToString() + "1.454466";
            //            var dbl = NumberParser.ParseDoubleFast(inp, 0, inp.Length);
            //            sump += dbl;
            //        }
            //    }, runToJIT: true, timesToRun: 2, name: "Parse fast");
            //Console.WriteLine(sump.ToString());
            //sump = 0;
            //var invCulture = CultureInfo.InvariantCulture;
            //var numberStyle = System.Globalization.NumberStyles.Number;

            //Benchmark.Run(() =>
            //{
            //    for (int i = 0; i < 10000000; i++)
            //    {
            //        var inp = i.ToString() + "1.454466";
            //        double dbl;
            //        double.TryParse(inp, numberStyle, invCulture, out dbl);
            //        sump += dbl;
            //    }
            //}, runToJIT: true, timesToRun: 2, name: "Parse slow");

            //Console.WriteLine(sump.ToString());
            //return;

            //double total = 0;
            //double val = 0;
            //var invCulture = CultureInfo.InvariantCulture;
            //var numberStyle = System.Globalization.NumberStyles.Number;

            //var str = "ABCDEFGH";
            //var chars = str.ToCharArray();

            //Benchmark.Run(() =>
            //{
            //    for (int i = 0; i < 10000000; i++)
            //    {
            //        var xx = new string(chars, 0, i % 7 + 1);
            //        total += xx.Length;
            //        total += xx.IndexOf(' ');
            //    }
            //}, timesToRun: 5, measureMemory: true);
            //Console.WriteLine("{0}, {1}", val, total);

            //total = 0;
            //val = 0;
            //Benchmark.Run(() =>
            //{
            //    for (int i = 0; i < 10000000; i++)
            //    {
            //        total = str.Substring(0, i % 7 + 1).Trim().Length;
            //    }
            //}, timesToRun: 5, measureMemory: true);
            //Console.WriteLine("{0}, {1}", val, total);

            //return;


            //IStructure tqn = StructureReader.ReadPdb("i:/1tqn.pdb");
            //AtomPropertiesBase props;
            //using (var f = File.OpenText("i:/test/testprops.wprop")) props = AtomPropertiesBase.Read(f);
            //tqn.AttachAtomProperties(props);


            ////dynamic xxx = null;
            ////if (xxx) Console.WriteLine("...");

            ////double sum = 0.0;
            ////Benchmark.Run(() =>
            ////    {
            ////        //foreach (var a in tqn.Atoms)
            ////        //{
            ////        //    var p = props.GetValue(a);
            ////        //    if (p.IsSomething()) sum += p.GetValue();
            ////        //}

            ////        Func<dynamic, dynamic, dynamic> f = (x, y) => (x >= y) ? x + y : x - y;
            ////        for (int i = 0; i < 10000000; i++)
            ////        {
            ////            //var a = Maybe.Just<dynamic>((double)i);
            ////            //var b = Maybe.Just<dynamic>((double)(i + 1));

            ////            dynamic a = (double)i;
            ////            dynamic b = (double)i + 1;
            ////            dynamic ret = f(a, b);
                        
            ////            ret = f(ret, b);
            ////            ret = f(ret, b);
            ////            ret = f(ret, b);

            ////            //var ret = a.SelectMany(b, (x, y) => Program.Plus(x, y)); // from x in a from y in b select x + y;
            ////            //var db = ret as MyDouble;
            ////            if (ret != null) sum += ret;
            ////        }
            ////    }, runToJIT: true, timesToRun: 3);
            ////Console.WriteLine(sum);


            ////var v1 = Maybe.Just<dynamic>(true);
            ////var v2 = Maybe.Nothing<dynamic>();

            ////var v3 = from x in v1 from y in v2 select x && y;
            ////Console.WriteLine(v3);

            //////sum = 0;
            //Benchmark.Run(() =>
            //{
            //    for (int i = 0; i < 100000000; i++)
            //    {
            //        var a = (double)i;
            //        var b = (double)(i + 1);
            //        sum += a + b;
            //////    }
            //////}, runToJIT: true, timesToRun: 1);
            //////Console.WriteLine(sum);

            ////return;

            //////int sum = 0;
            ////IStructure sx = StructureReader.Read("i:/1tqn.pdb");


            //var props = RealAtomProperties.Create(sx, "test", "hola", a => a.Position.LengthSquared);
            //using (var f = File.OpenWrite("i:/test/testprops.wprop"))
            //using (var w = new StreamWriter(f)) props.Write(w);

            //Benchmark.Run(() =>
            //    {
            //        AtomPropertiesBase props;
            //        using (var f = File.OpenText("i:/test/testprops.wprop")) props = AtomPropertiesBase.Read(f);
            //        sx.AttachProperties(props);
            //        props = sx.GetProperties("test");
            //        using (var f = File.OpenWrite("i:/test/testprops1.wprop"))
            //        using (var w = new StreamWriter(f)) props.Write(w);
            //        sum += props.Name.Length;
            //        sx.RemoveProperties("test");
            //    }, timesToRun: 10, measureMemory: false, runToJIT: true);
            //Console.WriteLine(sum);
          ////  return;

          //// // sx.WriteBonds("i:/testbonds.bnd");
          //////  return;
          ////  Benchmark.Run(() =>
          ////      {
          ////          sx = StructureReader.Read("i:/2xtg.pdb", computeBonds: false);
          ////          sx.ReadBonds("i:/testbonds.bnd");
          ////          var tree = "";// sx.InvariantKdAtomTree();
          ////          sum += sx.Atoms.Count + (tree == null ? 1 : 0);
          ////      }, timesToRun: 6, measureMemory: false, runToJIT: true);
          ////  Console.WriteLine(sum);
          ////  Console.WriteLine(sx.Id);

          ////  return;
          ////  //

          ////  TestCSVExport();
          ////  return;

            //var ssss = StructureReader.ReadPdb("i:\\1TQN.pdb", "t", true);

            ////var kd = K3DTree.Create(new Vector3D[] { new Vector3D(), new Vector3D(), new Vector3D(), new Vector3D(1, 1, 1) }, v => v);
            ////var near = kd.Nearest(new Vector3D());
            ////Console.WriteLine(near.Priority);

            //////Console.WriteLine(StructureReader.GetStructureIdFromFilename("/thisisatest.pdb"));

            ////return;

            ////var node = Parser.Parse(QueryGrammar.Query, "F(4.0)");
            ////Console.WriteLine(node.Text);
            ////return;
            

            //int[] xs = new int[] { 1, 2, 3 };
            //xs.SelectMany(x => new int[] { x, x });

            //var xx = Maybe.Just(1);
            //var yy = Maybe.Just(2);
            //var zz = Maybe.Just(3);

            //var sum = from x in xx
            //          from y in yy
            //          let t = x + y
            //          where t > 1
            //          from z in zz
            //          select t + z;

            //Console.WriteLine(sum);
            //return;

            //TestK3DTreeNearest(1000);
            //TestK3DTreeNearest(1000);

            //TestTree();
            //TestTree();

            //TestTree1();
            //TestTree1();
            //return;
           //return;

            //TestSplit();
            //return;
            //TestFirst();
            //TestFirst();
          //  TestRings();
       //     GC.Collect();
        //    TestRings();
       //     return;
            //Console.WriteLine(StructureReader.TrimmedSubstring("123  ", 0, 5));
            //TestLongHash();
            //return;

            //TestNDArray();
            //TestNDArray();
            //TestNDArray();
            //Console.ReadLine();
            //return;
            //Test6DConvex();
            //Test6DConvex();
            //return;

            /*TestND(3, 100, 100);

            var counts = new int[] { 100, 1000, 10000, 100000, 1000000 };
            int nDims = 4;
            int nRuns = 3;
            var times =
                counts
                .Select(nV =>
                    new
                    {
                        NumVertices = nV,
                        Times = Enumerable.Range(2, nDims).Select(dim =>
                                new
                                {
                                    Dimension = dim,
                                    AvgTime =
                                        Enumerable.Range(0, nRuns)
                                        .Select(_ => TestND(dim, nV, 1000))
                                        .Average(t => t.Item1.TotalSeconds)
                                })
                    });

            foreach (var r in times)
            {
                Console.Write(r.NumVertices.ToString() + "\t");
                foreach (var t in r.Times) Console.Write("{0:0.000}s\t", t.AvgTime);
                Console.Write(Environment.NewLine);
            }
            
            //Console.WriteLine("The average time was {0:0.000}s", avgTime);
            return;*/
            
            //TestDots();
            //TestDots();
            //TestDots();
            //TestDots();
            //Console.ReadLine();
            //return;

            //TestAlloc();
            //TestAlloc();
            //TestAlloc();
            //return;

            //File.WriteAllLines("i:\\residues_all.xml", File.ReadAllLines("i:\\residues_all.txt")
            //    .Where(l => !string.IsNullOrWhiteSpace(l))
            //    .Select(l => l.Trim())
            //    .Select(l => string.Format("    <Query Name=\"{0}\" Pattern=\"{{{0}}}\" />", l)));
            //return;
        //    Console.WriteLine(ElementSymbols.All.Count());

            //var start = DateTime.Now;
            
            
        //    var s = StructureReader.ReadPdb("d:\\2J0D.pdb", "t", true);            


        //    //var memstart = GC.GetTotalMemory(true);
        //    //var s = StructureReader.ReadPdb("i:\\KscA.pdb", "t", true);

        //  //  var memstart = GC.GetTotalMemory(true);
            Stopwatch sw = Stopwatch.StartNew();
            var s = StructureReader.Read("i:\\1TQN.pdb", "t", true);
            sw.Stop();
            Console.WriteLine("Load took {0}ms", sw.ElapsedMilliseconds);
        // //   Console.WriteLine("Memory stress: {0} MB", (double)(GC.GetTotalMemory(true) - memstart) / 1024.0 / 1024.0);
            Console.Write(s.Bonds.Count);
        //    return;
            
        //    //using (var f = File.OpenText("i:/bonds.bns")) ElementAndBondInfo.ReadBonds(s, f);

        //    var q = from a in s.Atoms
        //            from b in s.Atoms
        //            select a.Equals(b) ? -1 : a.Position.DistanceTo(b.Position) into x
        //            where x > 0
        //            select x;

        //    Console.WriteLine(q.Min());
                    


        //    return;

        //    //s.ReadRings("i:/rings.rng");
        //    //Console.WriteLine("Memory stress: {0} MB", (double)(GC.GetTotalMemory(true) - memstart) / 1024.0 / 1024.0);

        //    //var clone = s.Clone();
        //    //Console.WriteLine(clone.Atoms.Count);
        //    //TestKD(s);
        //    //return;
            
        ////    Console.WriteLine("Loading took {0}", (DateTime.Now - start));
        ////    Console.WriteLine("Atoms: {0}; Bonds: {1}; bs: {2}", s.Atoms.Count, s.Bonds.Count, /*s.Bonds.Aggregate(0L, (i, b) => (i % mod) + (b.Id.Id % mod))*/ 0);

        //    GC.Collect();

        //    start = DateTime.Now;

            ////var s = StructureReader.ReadPdb("d:\\2J0D.pdb", "t", true);            
            ////var memstart = GC.GetTotalMemory(true);
            //s = StructureReader.ReadPdb("i:\\1JJ2.pdb", "t", false);
            //using (var f = File.OpenText("i:/bonds.bns")) ElementAndBondInfo.ReadBonds(s, f);
            ////Console.WriteLine("Memory stress: {0} MB", (double)(GC.GetTotalMemory(true) - memstart) / 1024.0 / 1024.0);

            ////var clone = s.Clone();
            ////Console.WriteLine(clone.Atoms.Count);
            ////TestKD(s);
            ////return;


            //Console.WriteLine("Atoms: {0}; Bonds: {1}; bs: {2}", s.Atoms.Count, s.Bonds.Count, /*s.Bonds.Aggregate(0L, (i, b) => (i % mod) + (b.Id.Id % mod))*/ 0);
            //Console.WriteLine("Loading took {0}", (DateTime.Now - start));


        //    var tttt = ElementSymbol.Create("ZN") == ElementSymbols.Zn;
       //     Console.WriteLine(tttt);

            //using (var f = File.CreateText("i:/bonds.bns")) s.SerializeBonds(f);
            
        //    int maxFaces = 100000;

        //   // var cc = s.Atoms[0].CompareTo((object)s.Atoms[1]);
        //  //  Console.WriteLine(cc);


        var atoms = s.Atoms.Select(a => new Vertex { Position = new Vector(a.Position.X, a.Position.Y, a.Position.Z) }).ToArray();


        //    var start = DateTime.Now;
        //    //var distMatrix = new double[cc][];
        //    //for (int i = 0; i < cc; i++) distMatrix[i] = new double[cc];

        //    //var rnd = s.Atoms.ToRandomlyOrderedArray();
        //    //Console.WriteLine("Rnd took {0} with {1} elems", (DateTime.Now - start), rnd.Length);

        //    //var tri = Triangulation.CreateDelaunay(atoms);
        //    //Console.WriteLine("Tri took {0} with {1} faces", (DateTime.Now - start), tri.Cells.Count());


        var start = DateTime.Now;
        var tri3D = Triangulation.CreateDelaunay3D(atoms);
        Console.WriteLine("Tri3D took {0} with {1} faces", (DateTime.Now - start), tri3D.Cells.Count());


        //    var pts = RandomPoints3D(1000000);

        //   // start = DateTime.Now;
        //   // var tri = Triangulation.CreateDelaunay(pts);
        //   // Console.WriteLine("Tri took {0} with {1} faces", (DateTime.Now - start), tri.Cells.Count());

        //  //  var mem = GC.GetTotalMemory(true);
        //    var mem = 0;

        //    start = DateTime.Now;
        //    var tri3D = Triangulation.CreateDelaunay3D(pts.Item1);
        //    Console.WriteLine("Tri3D took {0} with {1} faces", (DateTime.Now - start), tri3D.Cells.Count());

        //    //mem = GC.GetTotalMemory(true) - mem;
        //    Console.WriteLine("Memory used: {0} MB, per TETRA: {1}", mem / (1024.0 * 1024.0), mem / tri3D.Cells.Count());


        //    //mem = GC.GetTotalMemory(true);

            //start = DateTime.Now;
            //var triT3D = DelaunayTriangulation3D<Vertex3D, DefaultTriangulationCell3D<Vertex3D>>.Create(pts.Item2);
            //Console.WriteLine("TriT3D took {0} with {1} faces", (DateTime.Now - start), triT3D.Cells.Count());

            ////mem = GC.GetTotalMemory(true) - mem;
            //Console.WriteLine("Memory used: {0} MB, per TETRA: {1}", mem / (1024.0 * 1024.0), mem / triT3D.Cells.Count());
            
         //  mem = GC.GetTotalMemory(true) - mem;

        //   Console.WriteLine("Memory used: {0} MB, per TETRA: {1}", mem / (1024.0 * 1024.0), mem / tri3D.Cells.Count());

       //     Console.ReadLine();

            //var start2 = DateTime.Now;
            //var tri2 = Triangulation.CreateDelaunay<Vertex>(s.Atoms.Take(maxFaces).Select(a => new Vertex { Position = new Vector(a.Position.X, a.Position.Y, a.Position.Z) }).ToArray());
            //Console.WriteLine("Tri took {0} with {1} faces", (DateTime.Now - start2), tri2.Cells.Count());

            //Console.ReadLine();
            //AtomEx.TOS
        }
    }
}
