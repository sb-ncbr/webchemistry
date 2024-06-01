namespace WebChemistry.Queries.Core.Queries
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;

    public abstract class Query<T> : Query
    {
        internal abstract T Execute(ExecutionContext context);

        public override dynamic ExecuteDynamic(ExecutionContext context)
        {
            return Execute(context);
        }

        public override object ExecuteObject(ExecutionContext context)
        {
            return Execute(context);
        }

        protected static string NameOption(string name, object value)
        {
            return name + "=" + value.ToString();
        }

        protected static string NameHelper(string name, IEnumerable<string> options, IEnumerable<string> arguments)
        {
            return string.Format("{0}({1})[{2}]", name, string.Join(",", options), string.Join(",", arguments));
        }

        protected static string NameHelper(string name, params string[] args)
        {
            return NameHelper(name, new string[0], args);
        }

        protected Query()
        {

        }
    }
    
    public abstract class QueryMotive : Query<IEnumerable<Motive>>
    {
        internal override IEnumerable<Motive> Execute(ExecutionContext context)
        {
            var cache = context.RequestCurrentContext().Cache;
            var name = this.ToString();
            int executionCount = cache.UpdateExecutionCount(name);
            
            if (executionCount == 1) return ExecuteMotive(context);
            if (executionCount == 2)
            {
                var result = ExecuteMotive(context).ToList();
                cache.CacheResult(name, result);
                return result;
            }

            return cache.GetCachedResult(name);
        }

        internal abstract IEnumerable<Motive> ExecuteMotive(ExecutionContext context);
    }
    
    public abstract class QueryUniqueMotive : QueryMotive
    {
        protected bool YieldNamedDuplicates { get; private set; }

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            if (YieldNamedDuplicates)
            {
                HashSet<Motive> yielded = new HashSet<Motive>();
                HashSet<int> yieldedNames = new HashSet<int>();
                foreach (var m in ExecuteMotiveInternal(context))
                {
                    if (yielded.Add(m))
                    {
                        if (m.Name.HasValue) yieldedNames.Add(m.Name.Value);
                        yield return m;
                    }
                    else if (m.Name.HasValue && yieldedNames.Add(m.Name.Value))
                    {
                        yield return m;
                    }
                }
            }
            else
            {
                HashSet<Motive> yielded = new HashSet<Motive>();
                foreach (var m in ExecuteMotiveInternal(context))
                {                    
                    if (yielded.Add(m)) yield return m;
                }
            }
        }

        protected abstract IEnumerable<Motive> ExecuteMotiveInternal(ExecutionContext context);

        protected QueryUniqueMotive(bool yieldNamedDuplicates)
        {
            this.YieldNamedDuplicates = yieldNamedDuplicates;
        }
    }

    public class SymbolQuery : Query<object>
    {
        string name;

        internal override object Execute(ExecutionContext context)
        {
            return context.GetSymbolValue(name);
        }

        protected override string ToStringInternal()
        {
            return string.Format("Symbol[\"{0}\"]", name);
        }

        public SymbolQuery(string name)
        {
            this.name = name.ToLowerInvariant();
        }
    }

    public class LambdaQuery : Query<object>
    {
        readonly object[] emptySymbols;

        string[] symbols;
        Query body;
        
        internal override object Execute(ExecutionContext context)
        {
            return Execute(context, emptySymbols);
        }

        public object Execute(ExecutionContext context, object[] argValues)
        {
            if (symbols.Length != argValues.Length)
            {
                throw new InvalidOperationException();
            }
            try
            {
                for (int i = 0; i < symbols.Length; i++)
                {
                    var arg = argValues[i];
                    if (arg == null) throw new InvalidOperationException(string.Format("The argument '{0}' is not defined.", symbols[i]));
                    context.PushSymbolValue(symbols[i], arg);
                }

                return body.ExecuteObject(context);
            }
            finally
            {
                for (int i = 0; i < symbols.Length; i++)
                {
                    context.PopSymbolValue(symbols[i]);
                }
            }
        }

        protected override string ToStringInternal()
        {
            return string.Format("Lambda[({0}),{1}]", 
                string.Join(",", symbols),
                body.ToString());
        }

        public LambdaQuery(IEnumerable<string> args, Query body)
        {
            this.emptySymbols = new object[0];
            this.symbols = args.Select(a => a.ToLowerInvariant()).ToArray();
            //this.values = args.Select(a => (object)null).ToArray();
            this.body = body;
        }
    }

    public class FindQuery : QueryMotive
    {
        QueryMotive inner;
        Query source;

        IEnumerable<Motive> Exec(ExecutionContext context)
        {
            var oldCurrent = context.RequestCurrentContext();

            context.CurrentContext = source.ExecuteObject(context).MustBe<Motive>().ToStructure("temp", true, oldCurrent.Structure.IsPdbStructure()).MotiveContext();
            var ret = inner.Execute(context).ToList();
            context.CurrentContext = oldCurrent;

            foreach (var m in ret)
            {
                m.UpdateContext(oldCurrent);
            }

            return ret; 
        }

        internal override IEnumerable<Motive> Execute(ExecutionContext context)
        {
            return Exec(context);            
        }

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            return Exec(context);             
        }
        
        protected override string ToStringInternal()
        {
            return "Find[" + inner.ToString() + "," + source.ToString() + "]";
        }
        
        /// <summary>
        /// If where is null, find everywhere.
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="source"></param>
        public FindQuery(QueryMotive inner, Query source)
        {
            this.inner = inner;
            this.source = source;
        }
    }

    public abstract class TestMotive : QueryValueBase
    {
        protected Query Where { get; private set; }

        protected TestMotive(Query where)
        {
            this.Where = where;
        }
    }

    public class ParentQueries : QueryMotive
    {
        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            return new[] { context.RequestCurrentContext().StructureMotive };
        }

        protected override string ToStringInternal()
        {
            return "InputAsPattern[]";
        }
    }

    public class CurrentQueries : Query<Motive>
    {
        internal override Motive Execute(ExecutionContext context)
        {
            return context.CurrentMotive;
        }

        protected override string ToStringInternal()
        {
            return "CurrentMotive[]";
        }
    }

    public class StructureQueries : Query<Motive>
    {
        string id;

        internal override Motive Execute(ExecutionContext context)
        {
            IStructure s;
            if (context.Environment.TryGetValue(id, out s))
            {
                return s.MotiveContext().StructureMotive;
            }

            throw new InvalidOperationException(string.Format("There is no structure with id '{0}' in the current ExecutionContext.", id));
        }

        protected override string ToStringInternal()
        {
            return "StructureMotive[\"" + id +"\"]";
        }

        public StructureQueries(string id)
        {
            this.id = id;
        }
    }


    /////// <summary>
    /////// Caches a Match result.
    /////// </summary>
    ////public class CachedQuery : QueryMotive
    ////{
    ////    QueryMotive inner;

    ////    internal QueryMotive Inner { get { return inner; } }
        
    ////    internal override IEnumerable<Motive> Execute(QueryContext context)
    ////    {
    ////        List<Motive> motives;
    ////        if (context.QueryCache.TryGetValue(inner.ToString(), out motives)) return motives;
    ////        motives = this.inner.Execute(context).ToList();
    ////        context.QueryCache.Add(inner.ToString(), motives);
    ////        return motives;
    ////    }

    ////    protected override string ToStringInternal()
    ////    {
    ////        return "&" + inner.ToString();
    ////    }

    ////    public CachedQuery(QueryMotive inner)
    ////    {
    ////        this.inner = inner;
    ////    }
    ////}
}
