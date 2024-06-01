namespace PlatformTests
{
    class Program
    {
        public static void Main(string[] args)
        {
        }

        //class Obj1
        //{
        //    public string X { get; set; }
        //}

        //class Obj2
        //{
        //    public string Y { get; set; }
        //}

        //class IndexNode
        //{
        //    public string X0 { get; set; }
        //    public int    X1 { get; set; }
        //    public string X2 { get; set; }
        //    public string X3 { get; set; }
        //    public string X4 { get; set; }
        //    public string X5 { get; set; }
        //    public string X6 { get; set; }

        //    public HashSet<string> Atoms { get; set; }
        //}

        //static void TestXml()
        //{
        //    Benchmark.Run(() =>
        //        {
        //            var root = new XElement("Index");
        //            for (int i = 0; i < 80000; i++)
        //            {
        //                root.Add(new XElement("Node",
        //                    new XAttribute("X0", "st" + i),
        //                    new XAttribute("X1", i),
        //                    new XAttribute("X2", "st" + i),
        //                    new XAttribute("X3", "st" + i),
        //                    new XAttribute("X4", String.Join(" ", Enumerable.Range('a', 'z' - 'a'))),
        //                    new XAttribute("X5", String.Join(" ", Enumerable.Range('a', 'z' - 'a'))),
        //                    new XAttribute("X6", String.Join(" ", Enumerable.Range('a', 'z' - 'a')))));
        //            }
        //            File.WriteAllText(@"i:/test/index.xml", root.ToString());
        //        }, timesToRun: 1, runToJIT: false, measureMemory: true, name: "Write");

        //    string text = "";
        //    Benchmark.Run(() =>
        //    {
        //        text = File.ReadAllText(@"i:/test/index.xml");
        //    }, timesToRun: 2, runToJIT: false, measureMemory: true, name: "ReadText");
        //    Console.WriteLine(text.Length);

        //    IndexNode[] index = null;
        //    Benchmark.Run(() =>
        //        {
        //            index = XElement.Load(@"i:/test/index.xml", LoadOptions.None)
        //                .Elements()
        //                .Select(e => new IndexNode 
        //                { 
        //                    X0 = e.Attribute("X0").Value,
        //                    X1 = int.Parse(e.Attribute("X1").Value),
        //                    X2 = e.Attribute("X2").Value,
        //                    X3 = e.Attribute("X3").Value,
        //                    X4 = e.Attribute("X4").Value,
        //                    X5 = e.Attribute("X5").Value,
        //                    X6 = e.Attribute("X6").Value,
        //                    //Atoms = new HashSet<string>(e.Attribute("Id").Value.Split(' '), StringComparer.OrdinalIgnoreCase)
        //                })
        //                .ToArray();
        //        }, timesToRun: 2, runToJIT: true, measureMemory: true, name: "Read");

        //    Console.WriteLine("The index has {0} elements.", index.Length);
        //}

        //internal class CustomObjectCreationConverter : JsonConverter
        //{
        //    //public override ISet<string> Create(Type objectType)
        //    //{
        //    //    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //    //}

        //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        //    {
        //        return new HashSet<string>((reader.Value as string).Split(' '), StringComparer.OrdinalIgnoreCase);
        //    }

        //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        //    {
        //        var set = value as ISet<string>;
        //        writer.WriteValue(string.Join(" ", set));
        //    }

        //    public override bool CanConvert(Type objectType)
        //    {
        //        return true;
        //    }
        //}


        ////static void TestJson()
        ////{
        ////    var root = new List<IndexNode>();
        ////    for (int i = 0; i < 80000; i++)
        ////    {
        ////        root.Add(new IndexNode
        ////            {
        ////                Id = "st" + i,
        ////                Atoms = new HashSet<string>(Enumerable.Range('a', 'z' - 'a').Select(x => x.ToString()), StringComparer.OrdinalIgnoreCase)
        ////            });
        ////    }
        ////    File.WriteAllText(@"i:/test/index.json", JsonConvert.SerializeObject(root, Formatting.Indented));

        ////    List<IndexNode> index = null;
        ////    Benchmark.Run(() =>
        ////    {
        ////        index = JsonConvert.DeserializeObject<List<IndexNode>>(File.ReadAllText(@"i:/test/index.json"));                    
        ////    }, timesToRun: 2, runToJIT: true, measureMemory: true);

        ////    Console.WriteLine("The index has {0} elements.", index.Count);
        ////}

        //static void TestStringFilter()
        //{
        //    var s = "Ca|Zn|Fe";
        //    var filter = new StringFilter(s);

        //    //var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "X", "D", "C", "B" };

        //    var set = new HashSet<string>("N;C;O;S;Fe".Split(';'), StringComparer.OrdinalIgnoreCase);

        //    Console.WriteLine(filter.Passes(set));
        //}


        //static void RunConsumer(object bco)
        //{
        //    BlockingCollection<IStructure> bc = bco as BlockingCollection<IStructure>;

        //    using (var fs = File.Create("c:/test/xs.zip"))
        //    using (var zip = new ZipOutputStream(fs))
        //    using (var writer = new StreamWriter(zip))
        //    {
        //        try
        //        {
        //            while (true)
        //            {
        //                var s = bc.Take();
        //                var e = new ZipEntry(s.Id + ".pdb");
        //                zip.PutNextEntry(e);
        //                //byte[ ] buffer = new byte[4096];
        //                //using (var ss = new StringReader(s.ToPdbString()))
        //                //    StreamUtils.Copy(ss, zip, buffer);
        //                writer.Write(s.ToPdbString());
        //                writer.Flush();
        //                //zip.Write(
                       
        //                zip.CloseEntry();
        //            }
        //        }
        //        catch (InvalidOperationException)
        //        {
                    
        //        }
        //    }

        //    Console.WriteLine("Zip created");
        //}

        //static void TestProdConsumer()
        //{
        //    using (var collection = new BlockingCollection<IStructure>(5000))
        //    using (var zipper = Task.Factory.StartNew(RunConsumer, collection))
        //    {
        //        Parallel.For(0, 100000, i =>
        //        {
        //            collection.Add(CreateDummy(i.ToString(), 50));
        //        });

        //        collection.CompleteAdding();
        //        Console.WriteLine("Done adding.");

        //        Task.WaitAll(zipper);
        //        Console.WriteLine("Done.");
        //    }
        //}
        
        //static void TestRx()
        //{
        //    Subject<int> subj = new Subject<int>();

        //    Console.WriteLine("Current thread: " + Thread.CurrentThread.ManagedThreadId);
        //    var wh = new EventWaitHandle(false, EventResetMode.ManualReset);

        //    var obs = subj.ObserveOn(NewThreadScheduler.Default);
                
        //    obs.Subscribe(i =>
        //    {
        //        Thread.Sleep(1000);
        //        Console.WriteLine("Observed {0} on thread {1} at {2}", i, Thread.CurrentThread.ManagedThreadId, DateTime.Now);
        //    },
        //    () =>
        //    {
        //        Console.WriteLine("Everything observed on thread {0}.", Thread.CurrentThread.ManagedThreadId);
        //        wh.Set();
        //    });

        //    for (int i = 0; i < 5; i++)
        //    {
        //        subj.OnNext(i);
        //        Console.WriteLine("Sent " + i);
        //    }
        //    subj.OnCompleted();
        //    Console.WriteLine("Sent OnCompleted.");

        //    wh.WaitOne();
        //    Console.WriteLine("Completed.");
        //}

        //static Random rnd = new Random();
        //static Func<Vector3D> rndVec = () => new Vector3D(rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble());
        //static IStructure CreateDummy(string id, int numatoms)
        //{
        //    var atoms = Enumerable.Range(0, numatoms).Select(i => PdbAtom.Create(i, i % 2 == 0 ? ElementSymbols.C : ElementSymbols.H, position: rndVec(), residueName: "R" + i)).ToArray();
        //    return Structure.Create(
        //        id, 
        //        AtomCollection.Create(atoms),
        //        BondCollection.Create(Enumerable.Range(0, numatoms).Select(i => Bond.Create(atoms[i], atoms[(i+1)%numatoms]))))
        //        .AsPdbStructure();
        //}

        //static List<IStructure> TestStructureMemory()
        //{
        //    List<IStructure> xs = new List<IStructure>();
        //    Benchmark.Run(() =>
        //    {
        //        for (int i = 0; i < 50000; i++)
        //        {
        //            xs.Add(CreateDummy(i.ToString(), 50));
        //        }
        //    }, timesToRun: 1, runToJIT: false, measureMemory: true);
        //    Console.WriteLine(xs.Count);
        //    return xs;
        //}

        //class CustomState
        //{
        //    public int Value { get; set; }
        //}

        //static void Zipppp()
        //{
        //    //using (var file = File.Create("i:/merged.zip")) { }
        //    using (var merged = ZipFile.Create("i:/merged.zip"))
        //    {
        //        merged.BeginUpdate();
        //        using (var zip = new ZipFile("i:/1.zip")) foreach (ZipEntry e in zip) merged.Add(e);
        //        using (var zip = new ZipFile("i:/2.zip")) foreach (ZipEntry e in zip) merged.Add(e);
        //        merged.CommitUpdate();
        //    }
        //}

        //class JsonTest
        //{
        //    public EntityId Id { get; set; }
        //}

        //class TestObj : PersistentObjectBase<TestObj>
        //{
        //    public int Value { get; set; }

        //    public static TestObj Create(EntityId id, int val)
        //    {
        //        return CreateAndSave(id, o => o.Value = val);
        //    }
        //}
        
        //static void Main(string[] args)
        //{
        //    //var xs = TestStructureMemory();
        //    //Benchmark.Run(() => TestProdConsumer(), name: "Zipper");
        //    //TestRx();
        //    //return;
        //    //Console.WriteLine(Path.GetFullPath(Path.Combine("ahoj//////", "./test")));
        //    //return;

        //    //PlatformEnvironment.PlatformRootPath = "i:/test/WebChemPlatform";
        //    //PlatformEnvironment.ComputationHostExecutable = @"C:\Projects\WebChemistry\Plaftorm\bin\ComputationHost\ComputationHost.exe";

        //    var config = new ServerManagerInfo
        //    {
        //        Master = new ServerInfo { Name = "Master", Root = @"i:\test\WebChemPlatform\MasterServer" },
        //        Servers = new ServerInfo[]
        //        {
        //            new ServerInfo { Name = "Default", Root = @"i:\test\WebChemPlatform\Servers\Default" },
        //        }
        //    };

        //    var serversConfig = @"i:\test\WebChemPlatform\servers.json";
        //    JsonHelper.WriteJsonFile(serversConfig, config);
        //    ServerManager.Init(serversConfig);


        //    ////for (int i = 0; i < 1000; i++)
        //    ////{
        //    ////    var ent = new EntityId("Master", "test/" + i);
        //    ////    var obj = TestObj.Create(ent, i);
        //    ////}

        //    ////int sum = 0;
        //    ////Benchmark.Run(() =>
        //    ////    {
        //    ////        for (int i = 0; i < 1000; i++)
        //    ////        {
        //    ////            var obj = TestObj.Load(new EntityId("Master", "test/" + i));
        //    ////            sum += obj.Value;
        //    ////        }
        //    ////    }, timesToRun: 10, runToJIT: true);
        //    ////Console.WriteLine(sum);
        //    //return;

        //    var master = ServerManager.MasterServer;
        //    var server = ServerManager.GetServer("Default");

        //    var db = master.PublicDatabases.GetDatabaseByName("pdb");
        //        //? master.PublicDatabases.GetDatabaseByName("testdb") 
        //        //: master.PublicDatabases.CreateDatabase("testdb", description: "100 structures", customId: "pdbtest");

        //    //int added = db.UpdateDatabase(@"I:\test\Queries\Databases\PDBDataSample100", visitedCallback: n => { });
        //    //added += db.UpdateDatabase(@"I:\test\Queries\Databases\PDBDataSample250", visitedCallback: n => { });            
        //    //Console.WriteLine("DB Updated. {0} new structures.", added);

        //    var view = db.DefaultView;

        //    Console.WriteLine(view.Name);


        //   //// var user = server.Users.GetOrCreateUserByName("david.sehnal@gmail.com");

        //   //// var mq = master.Services.RegisterOrUpdateService("Queries", "WebChemistry.Queries.Service.exe", ComputationPriority.Default, @"C:\Projects\WebChemistry\AppsAndServices\Queries\bin\Service");
            

        //   //////// db = PlatformServer.PublicDatabases.Exists("testdbBigger") ? PlatformServer.PublicDatabases.GetDatabaseByName("testdbBigger") : PlatformServer.PublicDatabases.CreateDatabase("testdbBigger");
        //   ////// //added = db.Update(@"I:\test\Queries\Databases\PDBDataSample100", visitedCallback: n => { });
        //   ////// //added = db.Update(@"I:\test\Queries\Databases\PDBDataSample250", visitedCallback: n => { });
        //   ////// //added = db.Update(@"I:\test\Queries\Databases\PDBDataSample500", visitedCallback: n => { });
        //   ////// //Console.WriteLine("DB Updated.");

        //   //// user.Computations.RemoveAll();

        //   //// //return;

        //   //// var view = user.DatabaseViews.Exists("testView")
        //   ////     ? user.DatabaseViews.GetViewByName("testView")
        //   ////     : user.DatabaseViews.CreateView(db, "testView",
        //   ////             description: "yolo lol",
        //   ////             filters: EntryFilter.Create(FilterType.Atom, "Zn | Ca").ToSingletonArray());

        //   //// user.DatabaseViews.Update(view, new DatabaseView.Update { Description = "Zns and Cas", Filters = view.Filters, Name = view.Name, IncludeObsolete = view.IncludeObsolete });

        //   ////// Console.WriteLine(db.DefaultView.Snapshot().Sum(s => s.ReadStructure().Atoms.Count));

        //   //// var snapshot = DatabaseSnapshot.Create(user.Repository.GetNewEntityId(), view.ToSingletonArray());
            
        //   //// //var comp = ComputationInfo.Load(user.Computations.Id.GetChildId("ba61da71-7e1f-453b-8c91-5013ebc5f6a3"));
        //   //// //var status = comp.GetStatus();
        //   //// //var settings = comp.GetSettings<QueriesServiceSettings>();

        //   //// //var summary = JsonHelper.ReadJsonFile<QueriesResultSummary>(Path.Combine(comp.GetResultFolderId().GetEntityPath(), "summary.json"));

        //   //// //Console.WriteLine(comp.GetStatus());

        //   //// var comp = user.Computations.CreateComputation(
        //   ////     user,
        //   ////     mq,
        //   ////     "Queries",
        //   ////     new QueriesServiceSettings
        //   ////     {
        //   ////         DatabaseSnapshotId = snapshot.Id,
        //   ////         Queries = new QueryInfo[]
        //   ////         {
        //   ////             new QueryInfo { Name = "ZnSurroundings", QueryString = "ConnectedResidues[1, Named[@Zn]]" },
        //   ////             new QueryInfo { Name = "CaSurroundings", QueryString = "ConnectedResidues[1, Named[@Ca]]" }
        //   ////         }
        //   ////     },
        //   ////     dependentObjects: snapshot.Id.ToSingletonArray());
        //   //// Console.WriteLine("Computation: {0}", comp.Id);
        //   //// comp.Schedule();

        //    //var vm = user.DatabaseViews;
        //    //var views = vm.GetAll();
        //    //var db = PlatformEnvironment.PublicDatabases.GetAll().First();

        //    //return;

        //    //if (!PlatformEnvironment.PublicDatabases.Exists("testdb")) PlatformEnvironment.PublicDatabases.CreateDatabase("testdb");
        //    //var db = PlatformEnvironment.PublicDatabases.GetDatabaseByName("testdb");
        //    //int added = db.Update(@"I:\test\Queries\Databases\PDBDataSample100", visitedCallback: n => { });
        //    //Console.WriteLine("DB Updated.");

        //    //PlatformEnvironment.Services.RegisterOrUpdateService("TestSvc", "TestService.exe", ComputationType.Default, @"C:\Projects\WebChemistry\Plaftorm\bin\TestService");
        //    //PlatformEnvironment.Services.RegisterOrUpdateService("Queries", "WebChemistry.Queries.Service.exe", ComputationType.Default, @"C:\Projects\WebChemistry\AppsAndServices\Queries\bin\Service");

        //    //var user = PlatformEnvironment.Users.Exists("dave") ? PlatformEnvironment.Users.GetUserByName("dave") : PlatformEnvironment.Users.CreateUser("dave");
        //    //user.Computations.RemoveAll();
            
        //    //var view = PlatformEnvironment.PublicDatabaseViews.GetAll().First();
        //    //var snapshot = view.Snapshot();
        //    //Console.WriteLine(view.Snapshot().Count());
            
        //    return;

        //    //var um = UserManager.Load("users");
        //    ////um.CreateUser("dave");
        //    //var user = um.CreateUser("dave");
        //    //var dbm = user.GetDatabaseManager();
        //    //var ddb = dbm.CreateDatabase("default");
        //    //user.GetDatabaseViewManager();
        //    //user.GetRepositoryManager();
        //    //var props = user.GetPropertyManager();
        //    //props.CreateProperty(ddb, "test", "Charges", "jolly");
        //    //return;

        //    //var manager = MoleculeDatabaseManager.Load(@"moldbtest/");
        //    //var db = manager.Exists("test") ? manager.GetDatabaseByName("test") : manager.CreateDatabase("test");

        //    //if (manager.Exists("test2")) manager.Delete(manager.GetDatabaseByName("test2").Id);

        //    //var vm = MoleculeDatabaseViewManager.Load("views/", "moldbtest/");

        //    //var viewId = MoleculeDatabaseView.Create(db, @"i:/test/moldbtest/", "testview", atomFilter: "Ca|Zn|Fe");
        //    //var view = MoleculeDatabaseView.Load(@"i:/test/moldbtest/" + viewId);
        //    //Console.WriteLine(view.Snapshot().Count());


        //    //var started = DateTime.Now;
        //    //int added = db.Update(@"I:\test\Queries\Databases\PDBDataSample100", n => Console.WriteLine("Visiting {0}.", n));
        //    //var added = db.Update(@"I:\test\Queries\Databases\PDBDataSample250", n => {}/* Console.WriteLine("Visiting {0}.", n)*/);
        //    ////added += db.Update(@"I:\test\Queries\Databases\PDBDataSample500", n => Console.WriteLine("Visiting {0}.", n));
        //    //Console.WriteLine("Updated: {0} new entries in {1}.", added, DateTime.Now - started);

        //    //Console.WriteLine(view.Snapshot().Count());
        //    return;

        //    //var cm = ComputationManager.Load(@"i:/test/comptest/");
        //    //var cmp = cm.CreateComputation(new CreateComputationInfo
        //    //{
        //    //    ApplicationId = "DbUpdate",
        //    //    UserId = "dave",
        //    //    ExecutableFilename = @"C:\Projects\WebChemistry\Plaftorm\bin\MoleculeDatabaseUpdater\MoleculeDatabaseUpdater.exe"
        //    //    //ExecutableFilename = "notepad.exe"
        //    //}, new
        //    //{
        //    //    DatabasePath = db.GetRootPath(),
        //    //    SourcePath = "I:/test/Queries/Databases/PDBDataSample100"
        //    //});
        //    //cmp.Start();
        //    //Console.WriteLine(cmp.Id);

        //    //Console.WriteLine("Enter to terminate...");
        //    //Console.ReadLine();
        //    //try
        //    //{
        //    //    cmp.Terminate();
        //    //}
        //    //catch { }

        //    return;

        //    //var manager = MoleculeDatabaseManager.Load(@"i:/test/moldbtest/");
        //    //var db = manager.CreateDatabase("test");

        //    //var started = DateTime.Now;
        //    //int added = db.Update(@"I:\test\Queries\Databases\PDBDataSample100", n => Console.WriteLine("Visiting {0}.", n));
        //    ////added += db.Update(@"I:\test\Queries\Databases\PDBDataSample250", n => Console.WriteLine("Visiting {0}.", n));
        //    ////added += db.Update(@"I:\test\Queries\Databases\PDBDataSample500", n => Console.WriteLine("Visiting {0}.", n));
        //    //Console.WriteLine("Updated: {0} new entries in {1}.", added, DateTime.Now - started);
        //    //return;

        //    //var db = manager.GetDatabaseByName("test");
        //    //manager.Delete(db.Id);
        //    //return;

        //    //var index = db.GetIndex();

        //    //IStructure s = null;

        //    //var entry = index.MaxBy(e => e.AtomCount)[0];
            
        //    //Benchmark.Run(() =>
        //    //{
        //    //    s =  entry.ReadStructure();
        //    //}, timesToRun: 3, runToJIT: true, measureMemory: false);


        //    //Console.WriteLine("{0}, {1}, {2}, {3}", s.Id, s.Atoms.Count, s.Bonds.Count, s.Rings().Count);

        //    //int rc = 0;

        //    //Benchmark.Run(() =>
        //    //{
        //    //    s = StructureReader.Read(Path.Combine(db.GetRootPath(), "data", entry.Filename), computePdbBonds: true);
        //    //    rc = s.Rings().Count;
        //    //}, timesToRun: 3, runToJIT: true, measureMemory: true);

        //    //Console.WriteLine("{0}, {1}, {2}, {3}", s.Id, s.Atoms.Count, s.Bonds.Count, rc);

        //    ////System.Collections.Concurrent.ConcurrentQueue<>
        //    //await Task.Run(() => Parallel.For(0, 100, (i, s) =>
        //    //    {
        //    //        Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
        //    //    }));
            
        //    //TestXml();
        //    //TestJson();
            
        //    //var t = JsonConvert.SerializeObject(null);
        //    //var s = JsonConvert.SerializeObject(new object());

        //    //return;
        //    ////var o1 = JsonConvert.SerializeObject(new Obj1 { X = "10" });
        //    ////var o2 = JsonConvert.DeserializeObject<Obj2>(" ");
        //    ////Console.WriteLine(o2.Y);

        //    ////var p = Process.GetProcessById(100);

        //    //var manager = SimpleComputationManager.Load(@"i:/test/cpmngr");
        //    //var computation = manager.CreateComputation(
        //    //    new CreateComputationInfo { ApplicationId = "text", UserId = "anon", ExecutableFilename = "notepad.exe" },
        //    //    new { Text = "XXX" });
        //    //computation.Start();

        //    //Console.WriteLine("Press enter to kill...");
        //    //Console.ReadLine();

        //    //var cc = manager.GetComputation(computation.Id);
        //    //cc.Terminate();

        //    //var manager = FileRepositoryManager.Load(@"i:/test/repository");
        //    //var ri = manager.CreateRepository("testtag", new { Param = 1337 });

        //  //  var reps = manager.GetRepositoriesByUser("dave");
        //    //foreach (var r in reps) Console.WriteLine(r.Tag);
        //}
    }
}
