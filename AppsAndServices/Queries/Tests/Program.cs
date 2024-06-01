using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Queries.Core;
using WebChemistry.Framework.Core;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using WebChemistry.Queries.Core.MetaQueries;
using WebChemistry.Queries.Core.Symbols;
using WebChemistry.Framework.TypeSystem;

namespace Tests
{
    class Program
    {
        //static void TestHashTrie()
        //{
        //    var rnd = new Random();
        //    var numbers = Enumerable.Range(0, 200000).Select(_ => rnd.Next(int.MaxValue)).Distinct().ToArray();


        //    int len = numbers.Length;

        //    var fullcollisions = 0;
        //    var partialcollisions = 0;
        //    var eqcount = 0;

        //    for (int i = 0; i < 1000; i++)
        //    {
        //        var atoms0 = numbers.Skip(rnd.Next(len - 50)).Take(rnd.Next(5) + 5).ToArray();
        //        var atoms1 = numbers.Skip(rnd.Next(len - 50)).Take(rnd.Next(5) + 5).ToArray();


        //        //var atoms0 = new int[] { 500028991, 1196855863, 1626566359, 818389878, 1273284749 };
        //        //var atoms1 = new int[] { 2096451179, 1262057130, 2056518129, 1645284461, 897439849, 1551362955 };


        //        var m0 = HashTrie.Create(atoms0.ToRandomlyOrderedArray());
        //        var m1 = HashTrie.Create(atoms1.ToRandomlyOrderedArray());

        //        var m2 = HashTrie.Union(m0, m1);
        //        var m3 = HashTrie.Create(atoms0.Concat(atoms1));

        //        if (m2.Count == atoms0.Concat(atoms1).Distinct().Count()) eqcount++;

        //        //atoms0.ForEach(x => Console.Write(x.ToString() + " "));
        //        //Console.Write("\n");
        //        //atoms1.ForEach(x => Console.Write(x.ToString() + " "));
        //        //Console.Write("\n");

        //        //Console.WriteLine("M0: {0} ({1}), M1: {2} ({3}) | M2: {4}, M3: {5}, Uniq: {6}", m0.Count, atoms0.Length, m1.Count, atoms1.Length, m2.Count, m3.Count, atoms0.Concat(atoms1).Distinct().Count());

        //        //if (m2.Equals(m3)) eqcount++;


        //        //bool equal = m0.Equals(m1);

        //        //if (equal) eqcount++;

        //        //if (!equal && m0.GetHashCode() == m1.GetHashCode()) fullcollisions++;
        //        //if (!equal && m0.CalcHashCode(1) == m1.CalcHashCode(1)) partialcollisions++;
        //    }

        //    Console.WriteLine("Equals: {0}", eqcount);
        //    Console.WriteLine("FUll: {0}", fullcollisions);
        //    Console.WriteLine("Partial: {0}", partialcollisions);

        //   // numbers.ToRandomlyOrderedArray().ForEach(c => Console.WriteLine(c));

        //    //var trie1 = HashTrie.Create(numbers.ToRandomlyOrderedArray());
        //    //var trie2 = HashTrie.Create(numbers.ToRandomlyOrderedArray());

        //    //trie1.Flatten().Zip(trie2.Flatten(), (x,y) => x + " " + y).ForEach(x => Console.WriteLine(x));

        //    //Console.WriteLine(Enumerable.SequenceEqual(trie1.Flatten(), trie2.Flatten()));

        //}

        //public static int CommonNeighborCount(string[] a, string[] b)
        //{
        //    int count = 0, indexB = 0;

        //    var distLabels = a.Distinct().OrderBy(x => x).ToArray();

        //    int lenA = distLabels.Length;
        //    int lenB = b.Length;

        //    var bNl = b;

        //    for (int i = 0; i < lenA; i++)
        //    {
        //        string pivot = distLabels[i];
        //        while (indexB < lenB && bNl[indexB].Equals(pivot, StringComparison.Ordinal))
        //        {
        //            count++;
        //            indexB++;
        //        }
        //        if (indexB >= lenB) return count;
        //    }
        //    return count;
        //}


        //static void GenerateInput()
        //{
        //    var rs = new string[] { "NAG","MAN","GLC","BMA","BGC","NDG","GAL","FUC","BOG","XYP","SIA","GLA","LMU","MAL","SUC","LMT","BCD","DMU","XYS","SGN" };

        //    var c = JsonConvert.SerializeObject(new { Id = "Test", Name = "zZ", QueryString = "ConnectedAtoms(1,{NAG})" });


        //    Func<string, string, object> cq = (id, q) => new { Id = id, Name = id, QueryString = q };

        //    //var pattern = "{{\\\"Id\\\":\\\"{0}\\\",\\\"Name\\\":\\\"{0}\\\",\\\"QueryString:\\\":\\\"{1}\\\"}}";
        //    var qs = rs
        //        .Select(r => new { q = string.Format("Residues(\"{0}\")", r), n = r })
        //        .SelectMany(r => new object[]
        //        {
        //           cq(r.n + "_res", r.q),
        //           cq(r.n + "_conn_atoms_1", string.Format("ConnectedAtoms(1,{0})", r.q)),
        //           cq(r.n + "_conn_atoms_2", string.Format("ConnectedAtoms(2,{0})", r.q)),
        //           cq(r.n + "_conn_res_1", string.Format("ConnectedResidues(1,{0})", r.q)),
        //           cq(r.n + "_amb_atoms_3", string.Format("AmbientAtoms(3,{0})", r.q)),
        //           cq(r.n + "_amb_atoms_5", string.Format("AmbientAtoms(5,{0})", r.q)),
        //           cq(r.n + "_amb_res_3", string.Format("AmbientResidues(3,{0})", r.q)),
        //           cq(r.n + "_amb_res_5", string.Format("AmbientResidues(5,{0})", r.q)),
        //        })
        //        .ToArray();

        //    //var input = "[" + string.Join(",\n", qs) + "]";
        //    File.WriteAllText("i:/test/motivequery/sgrinput.txt", JsonConvert.SerializeObject(qs));           
        //}

        //static void TestFilter()
        //{
        //    var files = Directory.GetFiles(@"I:\test\SiteBinder\Motives\NAG_conn_atoms_2").Take(100).ToArray();

        //    var qs = "Count(Filter([N], Count(AminoAcids, Current) == 1 && IsConnectedTo({NAG}, Current) && IsConnectedTo(Rings, Current)), Current) == 1";
        //    //qs = "IsConnectedTo({NAG}, Current))";

        //    var query = Query.Parse(qs, QueryTypes.Boolean);
        //    Console.WriteLine(query.ToString());

        //    foreach (var f in files)
        //    {
        //        var s = StructureReader.Read(f);
        //        var context = ExecutionContext.Create(s);
        //        context.CurrentMotive = context.CurrentContext.StructureMotive;
        //        //var result = query.ExecuteAs<bool>(context);

        //        //Console.WriteLine("{0}: {1}", s.Id, result);
        //    }
        //}

        static void FormatDescriptor(TextWriter text, SymbolDescriptor symbol)
        {
            text.WriteLine("=== {0}{1} ===", symbol.Name, 
                symbol.Description.OperatorForm != null
                ? " (" + symbol.Description.OperatorForm.Replace(">", "&gt;").Replace("<", "&lt;").Replace(":", "&#58;") + ")"
                : "");

            if (symbol.Description.Category != SymbolCategories.ElementaryTypes)
            {
                text.WriteLine("<code>{0}{1} -&gt; {2}</code><br/>", symbol.Name, symbol.Arguments.Length > 0
                    ? "(" + string.Join(", ", symbol.Arguments.Select(a => a.Name + "&#58; " + a.Type.ToString())) + ")"
                    : "()", symbol.Type);
                text.WriteLine("''{0}{1}''<br/>", symbol.Description.Description, symbol.Description.IsInternal ? " (internal)" : "");

                if (symbol.Description.Category == SymbolCategories.MiscFunctions)
                {
                    text.WriteLine("<small>''Note:'' This function cannot be used directly to query motifs from MotiveExplorer or Queries service.</small><br/>");
                }

                if (symbol.Arguments.Length > 0) text.WriteLine(";Arguments");
                foreach (var arg in symbol.Arguments)
                {
                    text.WriteLine(": {0}&#58; {1} - ''{2}'' ", arg.Name, arg.Type, arg.Description);
                    //text.WriteLine(":: ''{0}''", arg.Description);
                }
                if (symbol.Options.Count > 0) text.WriteLine(";Options");
                foreach (var opt in symbol.Options.Values.OrderBy(o => o.Name))
                {
                    text.WriteLine(": {0}&#58; {1} = {2} - ''{3}'' ", opt.Name, opt.Type, opt.DefaultValue, opt.Description);
                    //text.WriteLine(":: ''{0}''", arg.Description);
                }
            }
            //text.WriteLine(";Return type");
            //text.WriteLine(": {0}", symbol.Type);
            if (symbol.Description.Examples.Length > 0)
            {
                text.WriteLine(";Examples");
                foreach (var ex in symbol.Description.Examples)
                {
                    text.WriteLine(": <code>{0}</code>", ex.ExampleCode);
                    text.WriteLine(":: ''{0}''", ex.ExampleDescription);
                }
            }
        }

        static void FormatSymbols()
        {
            var text = new StringWriter();

            foreach (var group in SymbolTable.AllSymbols.GroupBy(s => s.Description.Category).Where(g => !g.Key.ExcludeFromHelp).OrderBy(g => g.Key.Index))
            {
                text.WriteLine("== {0} ==", group.Key.Name);
                text.WriteLine("''{0}''", group.Key.Description);
                
                var ordered = group.OrderBy(s => s.Name).ToArray();
                var index = 0;
                foreach (var symbol in ordered)
                {                    
                    FormatDescriptor(text, symbol);
                    index++;
                    if (index != ordered.Length) text.WriteLine("----");
                }
                text.WriteLine("<br/>", group.Key.Name);
            }

            File.WriteAllText("i:/test/Queries/spec.txt", File.ReadAllText("i:/test/Queries/intro.txt") + text.ToString());
        }

        //static void TestJaccard()
        //{
            
        //    var s1 = StructureReader.Read(@"I:\test\SiteBinder\Motives\FUC_conn_atoms_2\1lsl_5.pdb");
        //    var s2 = StructureReader.Read(@"I:\test\SiteBinder\Motives\FUC_conn_atoms_2\1dan_13.pdb");

        //    var sq = "AtomSimilarity[Motive['1lsl_5'],Motive['1dan_13']]";
        //    var ctx = ExecutionContext.Create(new IStructure[] { s1, s2 });
        //    var q = Query.ParseMeta(sq).Compile();
        //   // Console.WriteLine(q.ExecuteAs<dynamic>(ctx));
        //}

        //static void TestQueryBuilder()
        //{
        //    Query mq = QueryBuilder.Filter(
        //        QueryBuilder.Residues("HIS"), r => 
        //            true 
        //            | QueryBuilder.Count(QueryBuilder.Atoms(), r) >= 3
        //            & QueryBuilder.Count(QueryBuilder.Residues(), r) == 1);

        //    mq = QueryBuilder.Atoms().Filter(a => QueryBuilder.Atoms("C").Count(a) >= 3);
        //    Console.WriteLine(mq.ToString());
        //}

        static void Main(string[] args)
        {

           // TestQueryBuilder();

            //return;

            //TestJaccard();
            //return;
            //GenerateInput();
            FormatSymbols();
            return;
            //TestFilter();
            //return;

            //Console.WriteLine(new ParallelOptions { }.MaxDegreeOfParallelism);
            //return;

            //var xxx = CommonNeighborCount(
            //    new string[] { "A", "B", "B", "B", "C", "C", "X" },
            //    new string[] { "B", "B", "C", "C", "C", "D", "X" });
            //Console.WriteLine(xxx);
            //return;
            ////GenerateInput();
            ////return;

            //TestHashTrie();
            //return;

            //var query = Query.Parse("Cluster(3, {HIS}, {HIS}, {CYS})");

            //var query = Query.Parse("Filter(Cluster(3, {HIS}, {HIS}, {CYS}), Greater(Count([Ca], \"Test\"), 3))");

            //string qs;

            ////qs = "LogicalAnd(Less(Count([Ca]),3),LogicalAnd(Greater(Count([C]),6), Less(Count([Ca]),3)))";
            //qs = "Filter(NotAminoAcid(IgnoreWaters=true), Count([C]) >= 3 && Count([N]) >= 2)";

            //qs = "Near(4,[Zn],2-{HIS},2-{CYS})";
            ////qs = "\"10\" = 12";

            //var test = SymbolTable.TryGetDescriptor("Atoms");


            
            var symb = new MetaSymbol("test");

            ////qs = "OnRing([C], Ring(5-C,O))";

            //var tx = TypeTuple.Create(new TypeExpression[] { TypeMany.Create(BasicTypes.Value) });
            //var ty = TypeTuple.Create(new TypeExpression[] { TypeMany.Create(TypeWildcard.Instance) });
            //var ur = TypeExpression.Unify(tx, ty);
            //if (ur.Success)
            //{
            //    Console.WriteLine(ur.InferedExpression);
            //}

            //try
            {
                MetaQuery query = null;

                //Benchmark.Run(() =>
                    {
                        //var qs = "AmbientAtoms(4,[Zn])";
                        var qs = "Filter[Atoms[Zn], m => Count[Atoms[C], m] >= 3]"; //"Filter(Residues, Count([Zn], Current) > 0)";
                        //qs = "m => Count[Atoms[C], m]"; //"Filter(Residues, Count([Zn], Current) > 0)";
                        qs = "x => Filter[Atoms["+ string.Join(",", Enumerable.Range(0, 5).Select(x => "X" + x)) +  "], m => Count[Atoms,m] >= x]";
                        //qs = "Count[Atoms,x]";
                        //qs = "(x, y) => Count[Atoms,x] >= 2";
                        qs = "let x = Ca in \r\n let y = Zn in Atoms[y, x]";
                        //qs = "1,'2',x";
                        qs = "NotAminoAcids[NoWaters=true]";
                        qs = "Abs['t']";
                        qs = "2 |> x => x * 3 |> x => x + 1";
                        qs = "ringatoms[@C, rings[5~C,O]]";
                        qs = "1-1-1";
                        qs = "filter[residues, r => count[@c, r] > 4]";
                        //qs = "count[@c, r]";
                        query = Query.ParseMeta(qs);
                    }//, runToJIT: true, timesToRun: 10);
                ////query = Query.Parse("LogicalOr(Cluster(3, {HIS}, {HIS}, {CYS}), Greater(Count([Ca], \"Test\"), 3))");

                var compiled = query.Compile();

                Console.WriteLine(query.ToString());
                Console.WriteLine(compiled.ToString());
               // Console.WriteLine(compiled.ExecuteDynamic(null).ToString());
             //   Console.WriteLine(query.Type);
            }
          //  catch (Exception e)
            {
            //    Console.WriteLine(e.Message);
            }

            //Console.WriteLine(query.Compile().ToString());
            //return;

            //var s = StructureReader.ReadPdb("i:/1A1G.pdb");

            ////var tree = s.KdAtomTree();
            ////var near = tree.NearestRadius(s.Atoms.GetById(1144).Position, 4.0);

            //var matches = query.Matches(s);
            //matches.ForEach(x => Console.WriteLine(x.Signature));
            
            return;
            
            Motive m;

            var rnd = new Random();

            int count = 0;
            
            //Benchmark.Run(() =>
            //{
            //    for (int i = 0; i < 1000000; i++)
            //    {
            //        var atoms0 = s.Atoms.Skip(rnd.Next(1000)).Take(rnd.Next(35) + 15).ToArray();
            //        var atoms1 = s.Atoms.Skip(rnd.Next(1000)).Take(rnd.Next(35) + 15).ToArray();
            //        var m0 = Motive.FromAtoms(atoms0);
            //        var m1 = Motive.FromAtoms(atoms1);
            //        m = Motive.Merge(m0, m1);
            //        count += m.Atoms.Flatten().Count;
            //    }
            //}, runToJIT: false, timesToRun: 1);

            //Console.WriteLine(count);

            //IList<Motive> matches = null;

            //Benchmark.Run(() =>
            //    {
            //        var query = Query.Parse(qs);
            //        matches = query.Matches(s);
            //    }, runToJIT: true, timesToRun: 1);

            //foreach (var m in matches)
            //{
            //   Console.WriteLine(m.Signature);
            //}
        }
    }
}
