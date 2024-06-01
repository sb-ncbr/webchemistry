using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using WebChemistry.Queries.Core.MetaQueries;
using WebChemistry.Queries.Core.Queries;
using WebChemistry.Queries.Core.Symbols;

namespace WebChemistry.Queries.Core
{
    static class QueryBuilderHelper
    {
        public static QueryBuilderElement Apply(string head, params QueryBuilderElement[] args)
        {
            return new QueryBuilderApply { Head = head, Args = args };
        }

        public static QueryBuilderElement Apply(string head, QueryBuilderElement[] args, Tuple<string, object>[] opts)
        {
            return new QueryBuilderApply { Head = head, Args = args, Options = opts };
        }

        public static QueryBuilderElement Value(object val)
        {
            if (!(val is string) && val is IEnumerable)
            {
                List<QueryBuilderElement> args = new List<QueryBuilderElement>();
                var xs = val as IEnumerable;
                foreach (var x in xs) args.Add(Value(x));
                return Apply(SymbolTable.ListSymbol.Name, args.ToArray());
            }
            return new QueryBuilderValue { Value = val };
        }

        public static QueryBuilderElement Matrix(object val)
        {
            if (!(val is IEnumerable)) throw new ArgumentException("invalid matrix");
            List<List<double>> matrix = new List<List<double>>();
            foreach (var row in (val as IEnumerable))
            {
                if (!(row is IEnumerable)) throw new ArgumentException("invalid matrix");
                List<double> rr = new List<double>();
                foreach (var x in (row as IEnumerable))
                {
                    try
                    {
                        dynamic xx = x;
                        rr.Add((double)xx);
                    }
                    catch
                    {
                        throw new ArgumentException("invalid matrix");
                    }
                }
                matrix.Add(rr);
            }
            return new QueryBuilderValue { Value = matrix.Select(r => r.ToArray()).ToArray() };
        }

        public static Tuple<string, object> NoWatersOption(bool value)
        {
            return Tuple.Create("NoWaters", (object)value);
        }

        public static Tuple<string, object> Option(string name, object value)
        {
            return Tuple.Create(name, value);
        }

        public static Tuple<string, object>[] Options(params Tuple<string, object>[] options)
        {
            return options;
        }

        public static QueryBuilderElement[] Args(params QueryBuilderElement[] args)
        {
            return args;
        }
    }

    public class QueryBuilderContext
    {
        public int LambdaVariableCounter { get; set; }
    }

    public abstract class QueryBuilderElement : DynamicObject
    {
        public abstract MetaQuery ToMetaQueryInternal(QueryBuilderContext context);

        public override string ToString()
        {
            return "<Query>";
        }

        public MetaQuery ToMetaQuery()
        {
            return ToMetaQueryInternal(new QueryBuilderContext());
        }

        public static implicit operator bool(QueryBuilderElement x)
        {
            return true;
        }

        public static implicit operator MetaQuery(QueryBuilderElement x)
        {
            return x.ToMetaQuery();
        }

        public static implicit operator Query(QueryBuilderElement x)
        {
            return x.ToMetaQuery().Compile();
        }

        public static implicit operator QueryBuilderElement(string x)
        {
            return new QueryBuilderValue { Value = x };
        }

        public static implicit operator QueryBuilderElement(int x)
        {
            return new QueryBuilderValue { Value = x };
        }

        public static implicit operator QueryBuilderElement(double x)
        {
            return new QueryBuilderValue { Value = x };
        }

        public static implicit operator QueryBuilderElement(bool x)
        {
            return new QueryBuilderValue { Value = x };
        }

        static QueryBuilderElement Operator(string name, QueryBuilderElement l, QueryBuilderElement r)
        {
            return new QueryBuilderApply { Head = name, Args = new QueryBuilderElement[] { l, r } };
        }

        static QueryBuilderElement Operator(string name, QueryBuilderElement l, object r)
        {
            return new QueryBuilderApply { Head = name, Args = new QueryBuilderElement[] { l, new QueryBuilderValue { Value = r } } };
        }

        static QueryBuilderElement Operator(string name, object l, QueryBuilderElement r)
        {
            return new QueryBuilderApply { Head = name, Args = new QueryBuilderElement[] { new QueryBuilderValue { Value = l }, r } };
        }
        
        // relations
        public static QueryBuilderElement operator >=(QueryBuilderElement l, QueryBuilderElement r) { return Operator(RelationOp.GreaterEqual.ToString(), l, r); }
        public static QueryBuilderElement operator >=(QueryBuilderElement l, object r) { return Operator(RelationOp.GreaterEqual.ToString(), l, r ); }
        public static QueryBuilderElement operator <=(QueryBuilderElement l, QueryBuilderElement r) { return Operator(RelationOp.LessEqual.ToString(), l, r); }
        public static QueryBuilderElement operator <=(QueryBuilderElement l, object r) { return Operator(RelationOp.LessEqual.ToString(), l, r ); }
        public static QueryBuilderElement operator >(QueryBuilderElement l, QueryBuilderElement r) { return Operator(RelationOp.Greater.ToString(), l, r); }
        public static QueryBuilderElement operator >(QueryBuilderElement l, object r) { return Operator(RelationOp.Greater.ToString(), l, r ); }
        public static QueryBuilderElement operator <(QueryBuilderElement l, QueryBuilderElement r) { return Operator(RelationOp.Less.ToString(), l, r); }
        public static QueryBuilderElement operator <(QueryBuilderElement l, object r) { return Operator(RelationOp.Less.ToString(), l, r ); }
        public static QueryBuilderElement operator ==(QueryBuilderElement l, QueryBuilderElement r) { return Operator(RelationOp.Equal.ToString(), l, r); }
        public static QueryBuilderElement operator ==(QueryBuilderElement l, object r) { return Operator(RelationOp.Equal.ToString(), l, r ); }
        public static QueryBuilderElement operator !=(QueryBuilderElement l, QueryBuilderElement r) { return Operator(RelationOp.NotEqual.ToString(), l, r); }
        public static QueryBuilderElement operator !=(QueryBuilderElement l, object r) { return Operator(RelationOp.NotEqual.ToString(), l, r ); }

        // logic      
        public static QueryBuilderElement operator &(QueryBuilderElement l, QueryBuilderElement r)  { return Operator(LogicalOp.LogicalAnd.ToString(), l, r); }
        public static QueryBuilderElement operator &(QueryBuilderElement l, object r) { return Operator(LogicalOp.LogicalAnd.ToString(), l, r); }
        public static QueryBuilderElement operator |(QueryBuilderElement l, QueryBuilderElement r) { return Operator(LogicalOp.LogicalOr.ToString(), l, r); }
        public static QueryBuilderElement operator |(QueryBuilderElement l, object r) { return Operator(LogicalOp.LogicalOr.ToString(), l, r ); }
        public static QueryBuilderElement operator !(QueryBuilderElement l) { return new QueryBuilderApply { Head = LogicalOp.LogicalNot.ToString(), Args = new QueryBuilderElement[] { l } }; }
        
        //public QueryBuilderElement And(QueryBuilderElement other) { return Operator(LogicalOp.LogicalAnd.ToString(), this, other); }
        //public QueryBuilderElement Or(QueryBuilderElement other) { return Operator(LogicalOp.LogicalOr.ToString(), this, other); }
        public QueryBuilderElement LogicalNot() { return new QueryBuilderApply { Head = LogicalOp.LogicalNot.ToString(), Args = new QueryBuilderElement[] { this } }; }
        public QueryBuilderElement Not() { return new QueryBuilderApply { Head = LogicalOp.LogicalNot.ToString(), Args = new QueryBuilderElement[] { this } }; }

        // arithmetic
        public static QueryBuilderElement operator +(QueryBuilderElement l, QueryBuilderElement r) { return Operator("Plus", l, r); }
        public static QueryBuilderElement operator +(QueryBuilderElement l, object r) { return Operator("Plus", l, r ); }
        public static QueryBuilderElement operator -(QueryBuilderElement l, QueryBuilderElement r) { return Operator("Subtract", l, r); }
        public static QueryBuilderElement operator -(QueryBuilderElement l, object r) { return Operator("Subtract", l, r); }
        public static QueryBuilderElement operator -(object l, QueryBuilderElement r) { return Operator("Subtract", l, r); }
        public static QueryBuilderElement operator *(QueryBuilderElement l, QueryBuilderElement r) { return Operator("Times", l, r); }
        public static QueryBuilderElement operator *(QueryBuilderElement l, object r) { return Operator("Times", l, r); }
        public static QueryBuilderElement operator /(QueryBuilderElement l, QueryBuilderElement r) { return Operator("Divide", l, r); }
        public static QueryBuilderElement operator /(QueryBuilderElement l, object r) { return Operator("Divide", l, r); }
        public static QueryBuilderElement operator /(object l, QueryBuilderElement r) { return Operator("Divide", l, r); }
        public static QueryBuilderElement operator ^(QueryBuilderElement l, QueryBuilderElement r) { return Operator("Power", l, r); }
        public static QueryBuilderElement operator ^(QueryBuilderElement l, object r) { return Operator("Power", l, r); }
        public static QueryBuilderElement operator ^(object l, QueryBuilderElement r) { return Operator("Power", l, r); }
        public static QueryBuilderElement operator -(QueryBuilderElement l) { return new QueryBuilderApply { Head = "Minus", Args = new QueryBuilderElement[] { l } }; }

        public QueryBuilderElement Many(int n) { return QueryBuilderHelper.Apply("Repeat", this, QueryBuilderHelper.Value(n)); }
        public QueryBuilderElement ToPattern() { return QueryBuilderHelper.Apply("ToPattern", this); }
        public QueryBuilderElement ToAtoms() { return QueryBuilderHelper.Apply("ToAtoms", this); }
        public QueryBuilderElement ToResidues() { return QueryBuilderHelper.Apply("ToResidues", this); }
        public QueryBuilderElement Named() { return QueryBuilderHelper.Apply("Named", this); }
        public QueryBuilderElement Union() { return QueryBuilderHelper.Apply("Union", this); }
        public QueryBuilderElement Count(QueryBuilderElement what) { return QueryBuilderHelper.Apply("Count", this, what); }
        public QueryBuilderElement SeqCount() { return QueryBuilderHelper.Apply("SeqCount", this); }
        public QueryBuilderElement Contains(QueryBuilderElement what) { return QueryBuilderHelper.Apply("Contains", this, what); }
        public QueryBuilderElement Filter(Func<dynamic, dynamic> filter) { return QueryBuilderHelper.Apply("Filter", this, new QueryBuilderLambda { Lambda = filter }); }
        public QueryBuilderElement ExecuteIf(Func<dynamic, dynamic> condition) { return QueryBuilderHelper.Apply("ExecuteIf", this, new QueryBuilderLambda { Lambda = condition }); }
        public QueryBuilderElement Flatten(Func<dynamic, dynamic> selector) { return QueryBuilderHelper.Apply("Flatten", this, new QueryBuilderLambda { Lambda = selector }); }
        public QueryBuilderElement Inside(QueryBuilderElement where) { return QueryBuilderHelper.Apply("Inside", this, where); }
        public QueryBuilderElement IsConnectedTo(QueryBuilderElement to) { return QueryBuilderHelper.Apply("IsConnectedTo", this, to); }
        public QueryBuilderElement IsConnected() { return QueryBuilderHelper.Apply("IsConnected", this); }
        public QueryBuilderElement IsNotConnectedTo(QueryBuilderElement to) { return QueryBuilderHelper.Apply("IsNotConnectedTo", this, to); }
        public QueryBuilderElement Star(params QueryBuilderElement[] xs) { return QueryBuilderHelper.Apply("Star", new[] { this }.Concat(xs).ToArray()); }
        public QueryBuilderElement ConnectedAtoms(int n, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("ConnectedAtoms", QueryBuilderHelper.Args(this, QueryBuilderHelper.Value(n)), new[] { QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public QueryBuilderElement ConnectedResidues(int n, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("ConnectedResidues", QueryBuilderHelper.Args(this, QueryBuilderHelper.Value(n)), new[] { QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public QueryBuilderElement AmbientAtoms(double r, bool NoWaters = true, bool ExcludeBase = false, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("AmbientAtoms", QueryBuilderHelper.Args(this, QueryBuilderHelper.Value(r)), new[] { QueryBuilderHelper.NoWatersOption(NoWaters), QueryBuilderHelper.Option(SymbolTable.ExcludeBaseOption.Name, ExcludeBase), QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public QueryBuilderElement AmbientResidues(double r, bool NoWaters = true, bool ExcludeBase = false, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("AmbientResidues", QueryBuilderHelper.Args(this, QueryBuilderHelper.Value(r)), new[] { QueryBuilderHelper.NoWatersOption(NoWaters), QueryBuilderHelper.Option(SymbolTable.ExcludeBaseOption.Name, ExcludeBase), QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public QueryBuilderElement Spherify(double r, bool NoWaters = true, bool ExcludeBase = false, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("Spherify", QueryBuilderHelper.Args(this, QueryBuilderHelper.Value(r)), new[] { QueryBuilderHelper.NoWatersOption(NoWaters), QueryBuilderHelper.Option(SymbolTable.ExcludeBaseOption.Name, ExcludeBase), QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public QueryBuilderElement Filled(double RadiusFactor = 0.75, bool NoWaters = true) { return QueryBuilderHelper.Apply("Filled", QueryBuilderHelper.Args(this), new [] { QueryBuilderHelper.Option("RadiusFactor", RadiusFactor), QueryBuilderHelper.NoWatersOption(NoWaters) }); }
        public QueryBuilderElement NearestDistanceTo(QueryBuilderElement to) { return QueryBuilderHelper.Apply("NearestDistanceTo", this, to); }
        public QueryBuilderElement Find(QueryBuilderElement what) { return QueryBuilderHelper.Apply("Find", this, what); }
        ////public QueryBuilderElement Tunnels(QueryBuilderElement start, double ProbeRadius = 3.0, double InteriorThreshold = 1.25, double BottleneckRadius = 1.25)
        ////{
        ////    return QueryBuilderHelper.Apply("Tunnels", QueryBuilderHelper.Args(this, start),
        ////        new[] 
        ////        { 
        ////            QueryBuilderHelper.Option(SymbolTable.ProbeRadiusOption.Name, ProbeRadius), 
        ////            QueryBuilderHelper.Option(SymbolTable.InteriorThresholdOption.Name, InteriorThreshold), 
        ////            QueryBuilderHelper.Option(SymbolTable.BottleneckRadiusOption.Name, BottleneckRadius) 
        ////        });
        ////}
        public QueryBuilderElement EmptySpace(double ProbeRadius = 3.0, double InteriorThreshold = 1.25)
        {
            return QueryBuilderHelper.Apply("EmptySpace", QueryBuilderHelper.Args(this),
                new[] 
                { 
                    QueryBuilderHelper.Option(SymbolTable.ProbeRadiusOption.Name, ProbeRadius), 
                    QueryBuilderHelper.Option(SymbolTable.InteriorThresholdOption.Name, InteriorThreshold)
                });
        }

        //public QueryBuilderElement Metadata(string name) { return QueryBuilderHelper.Apply("Metadata", this, QueryBuilderHelper.Value(name)); }
        public QueryBuilderElement AtomProperty(string name) { return QueryBuilderHelper.Apply("AtomProperty", this, QueryBuilderHelper.Value(name)); }
        public QueryBuilderElement Descriptor(string name) { return QueryBuilderHelper.Apply("Descriptor", this, QueryBuilderHelper.Value(name)); }
        public QueryBuilderElement AminoSequenceString() { return QueryBuilderHelper.Apply("AminoSequenceString", this); }
        
        // Metadata
        static QueryBuilderElement HasAllHelper(string name, QueryBuilderElement e, string[] props)
        {
            return QueryBuilderHelper.Apply("HasAll" + name, new QueryBuilderElement[] { e }.Concat(props.Select(n => QueryBuilderHelper.Value(n))).ToArray());
        }

        static QueryBuilderElement HasAnyHelper(string name, QueryBuilderElement e, string[] props)
        {
            return QueryBuilderHelper.Apply("HasAny" + name, new QueryBuilderElement[] { e }.Concat(props.Select(n => QueryBuilderHelper.Value(n))).ToArray());
        }

        public QueryBuilderElement ReleaseDate() { return QueryBuilderHelper.Apply("ReleaseDate", this); }
        public QueryBuilderElement ReleaseYear() { return QueryBuilderHelper.Apply("ReleaseYear", this); }
        public QueryBuilderElement LatestRevisionDate() { return QueryBuilderHelper.Apply("LatestRevisionDate", this); }
        public QueryBuilderElement LatestRevisionYear() { return QueryBuilderHelper.Apply("LatestRevisionYear", this); }

        public QueryBuilderElement Resolution() { return QueryBuilderHelper.Apply("Resolution", this); }
        public QueryBuilderElement Weight() { return QueryBuilderHelper.Apply("Weight", this); }
        public QueryBuilderElement PolymerType() { return QueryBuilderHelper.Apply("PolymerType", this); }
        public QueryBuilderElement ExperimentMethod() { return QueryBuilderHelper.Apply("ExperimentMethod", this); }
        public QueryBuilderElement ProteinStoichiometry() { return QueryBuilderHelper.Apply("ProteinStoichiometry", this); }
        public QueryBuilderElement ProteinStoichiometryString() { return QueryBuilderHelper.Apply("ProteinStoichiometryString", this); }
        
        public QueryBuilderElement Authors() { return QueryBuilderHelper.Apply("Authors", this); }
        public QueryBuilderElement HasAllAuthors(params string[] props) { return HasAllHelper("Authors", this, props); }
        public QueryBuilderElement HasAnyAuthor(params string[] props) { return HasAnyHelper("Author", this, props); }

        public QueryBuilderElement Keywords() { return QueryBuilderHelper.Apply("Keywords", this); }
        public QueryBuilderElement HasAllKeywords(params string[] props) { return HasAllHelper("Keywords", this, props); }
        public QueryBuilderElement HasAnyKeyword(params string[] props) { return HasAnyHelper("Keyword", this, props); }

        public QueryBuilderElement EntityTypes() { return QueryBuilderHelper.Apply("EntityTypes", this); }
        public QueryBuilderElement HasAllEntityTypes(params string[] props) { return HasAllHelper("EntityTypes", this, props); }
        public QueryBuilderElement HasAnyEntityType(params string[] props) { return HasAnyHelper("EntityType", this, props); }

        public QueryBuilderElement ECNumbers() { return QueryBuilderHelper.Apply("ECNumbers", this); }
        public QueryBuilderElement HasAllECNumbers(params string[] props) { return HasAllHelper("ECNumbers", this, props); }
        public QueryBuilderElement HasAnyECNumber(params string[] props) { return HasAnyHelper("ECNumber", this, props); }

        public QueryBuilderElement OriginOrganisms() { return QueryBuilderHelper.Apply("OriginOrganisms", this); }
        public QueryBuilderElement HasAllOriginOrganisms(params string[] props) { return HasAllHelper("OriginOrganisms", this, props); }
        public QueryBuilderElement HasAnyOriginOrganism(params string[] props) { return HasAnyHelper("OriginOrganism", this, props); }

        public QueryBuilderElement OriginOrganismIds() { return QueryBuilderHelper.Apply("OriginOrganismIds", this); }
        public QueryBuilderElement HasAllOriginOrganismIds(params string[] props) { return HasAllHelper("OriginOrganismIds", this, props); }
        public QueryBuilderElement HasAnyOriginOrganismId(params string[] props) { return HasAnyHelper("OriginOrganismId", this, props); }

        public QueryBuilderElement OriginOrganismGenus() { return QueryBuilderHelper.Apply("OriginOrganismGenus", this); }
        public QueryBuilderElement HasAllOriginOrganismGenus(params string[] props) { return HasAllHelper("OriginOrganismGenus", this, props); }
        public QueryBuilderElement HasAnyOriginOrganismGenus(params string[] props) { return HasAnyHelper("OriginOrganismGenus", this, props); }

        public QueryBuilderElement HostOrganisms() { return QueryBuilderHelper.Apply("HostOrganisms", this); }
        public QueryBuilderElement HasAllHostOrganisms(params string[] props) { return HasAllHelper("HostOrganisms", this, props); }
        public QueryBuilderElement HasAnyHostOrganism(params string[] props) { return HasAnyHelper("HostOrganism", this, props); }

        public QueryBuilderElement HostOrganismIds() { return QueryBuilderHelper.Apply("HostOrganismIds", this); }
        public QueryBuilderElement HasAllHostOrganismIds(params string[] props) { return HasAllHelper("HostOrganismIds", this, props); }
        public QueryBuilderElement HasAnyHostOrganismId(params string[] props) { return HasAnyHelper("HostOrganismId", this, props); }

        public QueryBuilderElement HostOrganismGenus() { return QueryBuilderHelper.Apply("HostOrganismGenus", this); }
        public QueryBuilderElement HasAllHostOrganismGenus(params string[] props) { return HasAllHelper("HostOrganismGenus", this, props); }
        public QueryBuilderElement HasAnyHostOrganismGenus(params string[] props) { return HasAnyHelper("HostOrganismGenus", this, props); }

        //public override bool TryGetMember(GetMemberBinder binder, out object result)
        //{
        //    result = new QueryBuilderDynamicInvoke
        //    {
        //        Inner = this,
        //        Name = binder.Name,
        //        IsProperty = true
        //    };
        //    return true;
        //}
    }

    //public class QueryBuilderDynamicInvoke : QueryBuilderElement
    //{
    //    public QueryBuilderElement Inner { get; set; }
    //    public string Name { get; set; }
    //    public bool IsProperty { get; set; }
    //    public object[] Args { get; set; }

    //    public override string ToString()
    //    {
    //        if (IsProperty) return string.Format("InvokeProperty[{0},{1}]", Inner, Name);
    //        return string.Format("InvokeMember[{0},{1},{{{2}}}]", Inner, Name, Args == null ? "" : string.Join(",", Args.Select(a => a == null ? "null" : a.ToString())));
    //    }


    //    public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
    //    {
    //        result = new QueryBuilderDynamicInvoke
    //        {
    //            Inner = this,
    //            Name = Name,
    //            IsProperty = false,
    //            Args = args
    //        };
    //        return true;
    //    }

    //    public override bool TryGetMember(GetMemberBinder binder, out object result)
    //    {
    //        result = new QueryBuilderDynamicInvoke
    //        {
    //            Inner = this,
    //            Name = binder.Name,
    //            IsProperty = true
    //        };
    //        return true;
    //    }

    //    internal override MetaQuery ToMetaQueryInternal(QueryBuilderContext context)
    //    {
    //        return MetaQuery.CreateApply(
    //            MetaQuery.CreateSymbol("DynamicInvoke"),
    //            MetaQuery.CreateTuple(
    //                Inner.ToMetaQueryInternal(context),
    //                MetaQuery.CreateValue(Name),
    //                MetaQuery.CreateValue(IsProperty),
    //                MetaQuery.CreateObjectValue(Args)
    //            ));
    //        throw new NotImplementedException();
    //    }
    //}

    public class QueryBuilderSymbol : QueryBuilderElement
    {
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public override MetaQuery ToMetaQueryInternal(QueryBuilderContext context)
        {
            return MetaQuery.CreateSymbol(Name);
        }
    }

    public class QueryBuilderValue : QueryBuilderElement
    {
        public object Value { get; set; }

        public override string ToString()
        {
            if (Value is string) return "\"" + Value.ToString() + "\"";
            return Value.ToString();            
        }

        public override MetaQuery ToMetaQueryInternal(QueryBuilderContext context)
        {
            if (Value is QueryBuilderElement) return (Value as QueryBuilderElement).ToMetaQueryInternal(context);
            if (Value is string) return MetaQuery.CreateValue(Value as string);
            if (Value is int) return MetaQuery.CreateValue((int)Value);
            if (Value is double) return MetaQuery.CreateValue((double)Value);
            if (Value is bool) return MetaQuery.CreateValue((bool)Value);
            if (Value is double[][]) return MetaQuery.CreateValue((double[][])Value);
            //return MetaQuery.CreateObjectValue(Value);

            throw new NotSupportedException(string.Format("Object '{0}' of type '{1}' cannot be translated to a query.",
                Value, Value.GetType()));
        }
    }

    public class QueryBuilderApply : QueryBuilderElement
    {
        public string Head { get; set; }
        public IEnumerable<QueryBuilderElement> Args { get; set; }
        public IEnumerable<Tuple<string, object>> Options { get; set; }

        public override string ToString()
        {
            if (Options.Count() > 0)
            {
                return string.Format("{0}({1})[{2}]",
                    Head, 
                    string.Join(", ", Options.Select(a => a.Item1 + "=" + a.Item2.ToString())), 
                    string.Join(", ", Args.Select(a => a.ToString())));
            }
            return string.Format("{0}[{1}]", 
                Head, 
                string.Join(", ", Args.Select(a => a.ToString())));
            //return string.Format("<Apply(\"{0}\", ({1}))>", Head, string.Join(", ", Args.Select(a => a.ToString())));
        }

        public override MetaQuery ToMetaQueryInternal(QueryBuilderContext context)
        {
            var args = Args.Select(a => a.ToMetaQueryInternal(context));
            var opts = Options.Select(o => MetaAssign.Create(MetaQuery.CreateSymbol(o.Item1), new QueryBuilderValue { Value = o.Item2 }.ToMetaQueryInternal(context)));

            return MetaQuery.CreateApply(
                MetaQuery.CreateSymbol(Head),
                MetaQuery.CreateTuple(args.Concat(opts).ToArray()));
        }

        ////public override string ToString()
        ////{
        ////    return string.Format("Apply(\"{0}\", {1})", Head, string.Join(", ", Args.Select(a => a.ToString()).ToArray()));
        ////}

        public QueryBuilderApply()
        {
            Options = new Tuple<string, object>[0];
        }
    }

    public class QueryBuilderLambda : QueryBuilderElement
    {
        public Func<dynamic, dynamic> Lambda { get; set; }

        public override string ToString()
        {
            return string.Format("$x => {0}", Lambda(new QueryBuilderSymbol { Name = "$x" }));
        }

        public override MetaQuery ToMetaQueryInternal(QueryBuilderContext context)
        {
            var symbol = new QueryBuilderSymbol { Name = "$x" + (context.LambdaVariableCounter++) };
            var result = Lambda(symbol);
            return MetaQuery.CreateLambda(symbol, new QueryBuilderValue { Value = result }.ToMetaQueryInternal(context));
        } 
    }

    public class QueryBuilderLambda2 : QueryBuilderElement
    {
        public Func<dynamic, dynamic, dynamic> Lambda { get; set; }

        public override string ToString()
        {
            return string.Format("($x, $y) => {0}", Lambda(new QueryBuilderSymbol { Name = "$x" }, new QueryBuilderSymbol { Name = "$y" }));
        }

        public override MetaQuery ToMetaQueryInternal(QueryBuilderContext context)
        {
            var symbol0 = new QueryBuilderSymbol { Name = "$x" + (context.LambdaVariableCounter++) };
            var symbol1 = new QueryBuilderSymbol { Name = "$x" + (context.LambdaVariableCounter++) };
            var result = Lambda(symbol0, symbol1);
            return MetaQuery.CreateLambda(MetaQuery.CreateTuple(symbol0, symbol1), new QueryBuilderValue { Value = result }.ToMetaQueryInternal(context));
        }
    }
    
    public static class QueryBuilder
    {   
        public static QueryBuilderElement Pattern(string id) { return QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(id)); }

        public static QueryBuilderElement Atoms(params string[] names) { return QueryBuilderHelper.Apply("Atoms", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement Atoms(IEnumerable<string> names) { return QueryBuilderHelper.Apply("Atoms", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }        
        public static QueryBuilderElement NotAtoms(params string[] names) { return QueryBuilderHelper.Apply("NotAtoms", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement NotAtoms(IEnumerable<string> names) { return QueryBuilderHelper.Apply("NotAtoms", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement AtomNames(params string[] names) { return QueryBuilderHelper.Apply("AtomNames", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement AtomNames(IEnumerable<string> names) { return QueryBuilderHelper.Apply("AtomNames", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement NotAtomNames(params string[] names) { return QueryBuilderHelper.Apply("NotAtomNames", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement NotAtomNames(IEnumerable<string> names) { return QueryBuilderHelper.Apply("NotAtomNames", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement AtomIds(params int[] ids) { return QueryBuilderHelper.Apply("AtomIds", ids.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement AtomIds(IEnumerable<int> ids) { return QueryBuilderHelper.Apply("AtomIds", ids.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement NotAtomIds(params int[] ids) { return QueryBuilderHelper.Apply("NotAtomIds", ids.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement NotAtomIds(IEnumerable<int> ids) { return QueryBuilderHelper.Apply("NotAtomIds", ids.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement AtomIdRange(int id) { return QueryBuilderHelper.Apply("AtomIdRange", Value(id), Value(id)); }
        public static QueryBuilderElement AtomIdRange(int minId, int maxId) { return QueryBuilderHelper.Apply("AtomIdRange", Value(minId), Value(maxId)); }
        public static QueryBuilderElement Residues(params string[] names) { return QueryBuilderHelper.Apply("Residues", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement Residues(IEnumerable<string> names) { return QueryBuilderHelper.Apply("Residues", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement NotResidues(params string[] names) { return QueryBuilderHelper.Apply("NotResidues", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement NotResidues(IEnumerable<string> names) { return QueryBuilderHelper.Apply("NotResidues", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement ModifiedResidues(params string[] names) { return QueryBuilderHelper.Apply("ModifiedResidues", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement ModifiedResidues(IEnumerable<string> names) { return QueryBuilderHelper.Apply("ModifiedResidues", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement ResidueIds(params string[] ids) { return QueryBuilderHelper.Apply("ResidueIds", ids.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement ResidueIds(IEnumerable<string> ids) { return QueryBuilderHelper.Apply("ResidueIds", ids.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement ResidueIdRange(string chain, int id) { return QueryBuilderHelper.Apply("ResidueIdRange", Value(chain), Value(id), Value(id)); }
        public static QueryBuilderElement ResidueIdRange(string chain, int min, int max) { return QueryBuilderHelper.Apply("ResidueIdRange", Value(chain), Value(min), Value(max)); }
        ////public static QueryBuilderElement ResidueCategory(params string[] categories) { return QueryBuilderHelper.Apply("ResidueCategory", categories.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        ////public static QueryBuilderElement ResidueCategory(IEnumerable<string> categories) { return QueryBuilderHelper.Apply("ResidueCategory", categories.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        ////public static QueryBuilderElement NotResidueCategory(params string[] categories) { return QueryBuilderHelper.Apply("NotResidueCategory", categories.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        ////public static QueryBuilderElement NotResidueCategory(IEnumerable<string> categories) { return QueryBuilderHelper.Apply("NotResidueCategory", categories.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }

        public static QueryBuilderElement AminoAcids() { return QueryBuilderHelper.Apply("AminoAcids"); }
        public static QueryBuilderElement AminoAcids(string ChargeType) { return QueryBuilderHelper.Apply("AminoAcids", QueryBuilderHelper.Args(), new[] { QueryBuilderHelper.Option(SymbolTable.ChargeTypeOption.Name, ChargeType) }); }
        public static QueryBuilderElement NotAminoAcids(bool NoWaters = true) { return QueryBuilderHelper.Apply("NotAminoAcids", QueryBuilderHelper.Args(), new [] { QueryBuilderHelper.NoWatersOption(NoWaters) }); }
        public static QueryBuilderElement HetResidues(bool NoWaters = true) { return QueryBuilderHelper.Apply("HetResidues", new QueryBuilderElement[0], new[] { QueryBuilderHelper.NoWatersOption(NoWaters) }); }

        public static QueryBuilderElement Rings(params string[] names) { return QueryBuilderHelper.Apply("Rings", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement Rings(IEnumerable<string> elements) { return QueryBuilderHelper.Apply("Rings", elements.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement RingAtoms(QueryBuilderElement atom) { return QueryBuilderHelper.Apply("RingAtoms", atom); }
        public static QueryBuilderElement RingAtoms(QueryBuilderElement atom, QueryBuilderElement ring) { return QueryBuilderHelper.Apply("RingAtoms", atom, ring); }
        public static QueryBuilderElement RegularMotifs(string regex, string Type = "Amino") { return QueryBuilderHelper.Apply("RegularMotifs", new[] { QueryBuilderHelper.Value(regex) }, new[] { QueryBuilderHelper.Option(SymbolTable.RegularMotifTypeOption.Name, Type) }); }
        public static QueryBuilderElement RegularMotifs(QueryBuilderElement regex, string Type = "Amino") { return QueryBuilderHelper.Apply("RegularMotifs", new[] { regex }, new[] { QueryBuilderHelper.Option(SymbolTable.RegularMotifTypeOption.Name, Type) }); }
        public static QueryBuilderElement Chains(params string[] identifiers) { return QueryBuilderHelper.Apply("Chains", identifiers.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement Sheets() { return QueryBuilderHelper.Apply("Sheets"); }
        public static QueryBuilderElement Helices() { return QueryBuilderHelper.Apply("Helices"); }
        public static QueryBuilderElement Named(QueryBuilderElement inner) { return QueryBuilderHelper.Apply("Named", inner); }

        public static QueryBuilderElement GroupedAtoms(params string[] names) { return QueryBuilderHelper.Apply("GroupedAtoms", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }
        public static QueryBuilderElement GroupedAtoms(IEnumerable<string> names) { return QueryBuilderHelper.Apply("GroupedAtoms", names.Select(n => QueryBuilderHelper.Value(n)).ToArray()); }

        public static QueryBuilderElement InputAsPattern() { return QueryBuilderHelper.Apply("InputAsPattern"); }
        public static QueryBuilderElement Current() { return QueryBuilderHelper.Apply("Current"); }
        public static QueryBuilderElement CommonAtoms(string id) { return QueryBuilderHelper.Apply("CommonAtoms", QueryBuilderHelper.Value(id)); }

        public static QueryBuilderElement Count(QueryBuilderElement where, QueryBuilderElement what) { return QueryBuilderHelper.Apply("Count", where, what); }
        public static QueryBuilderElement SeqCount(QueryBuilderElement what) { return QueryBuilderHelper.Apply("SeqCount", what); }
        public static QueryBuilderElement Contains(QueryBuilderElement where, QueryBuilderElement what) { return QueryBuilderHelper.Apply("Contains", where, what); }
        public static QueryBuilderElement Filter(QueryBuilderElement where, Func<dynamic, dynamic> filter) { return QueryBuilderHelper.Apply("Filter", where, new QueryBuilderLambda { Lambda = filter }); }
        public static QueryBuilderElement ExecuteIf(QueryBuilderElement query, Func<dynamic, dynamic> condition) { return QueryBuilderHelper.Apply("ExecuteIf", query, new QueryBuilderLambda { Lambda = condition }); }
        public static QueryBuilderElement Flatten(QueryBuilderElement patterns, Func<dynamic, dynamic> selector) { return QueryBuilderHelper.Apply("Flatten", patterns, new QueryBuilderLambda { Lambda = selector }); }
        public static QueryBuilderElement Inside(QueryBuilderElement patterns, QueryBuilderElement where) { return QueryBuilderHelper.Apply("Inside", patterns, where); }
        public static QueryBuilderElement Or(params QueryBuilderElement[] xs) { return QueryBuilderHelper.Apply("Or", xs); }
        public static QueryBuilderElement Or(IEnumerable<QueryBuilderElement> xs) { return QueryBuilderHelper.Apply("Or", xs.ToArray()); }
        public static QueryBuilderElement Union(QueryBuilderElement inner) { return QueryBuilderHelper.Apply("Union", inner); }
        public static QueryBuilderElement ToAtoms(QueryBuilderElement inner) { return QueryBuilderHelper.Apply("ToAtoms", inner); }
        public static QueryBuilderElement ToResidues(QueryBuilderElement inner) { return QueryBuilderHelper.Apply("ToResidues", inner); }
        public static QueryBuilderElement ToPattern(QueryBuilderElement inner) { return QueryBuilderHelper.Apply("ToPattern", inner); }

        public static QueryBuilderElement ConnectedAtoms(QueryBuilderElement inner, int n, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("ConnectedAtoms", QueryBuilderHelper.Args(inner, QueryBuilderHelper.Value(n)), new[] { QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public static QueryBuilderElement ConnectedResidues(QueryBuilderElement inner, int n, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("ConnectedResidues", QueryBuilderHelper.Args(inner, QueryBuilderHelper.Value(n)), new[] { QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public static QueryBuilderElement Path(params QueryBuilderElement[] xs ) { return QueryBuilderHelper.Apply("Path", xs); }
        public static QueryBuilderElement Star(QueryBuilderElement center, params QueryBuilderElement[] xs) { return QueryBuilderHelper.Apply("Star", new[] { center }.Concat(xs).ToArray()); }
        public static QueryBuilderElement IsConnectedTo(QueryBuilderElement what, QueryBuilderElement to) { return QueryBuilderHelper.Apply("IsConnectedTo", what, to); }
        public static QueryBuilderElement IsNotConnectedTo(QueryBuilderElement what, QueryBuilderElement to) { return QueryBuilderHelper.Apply("IsNotConnectedTo", what, to); }
        public static QueryBuilderElement IsConnected(QueryBuilderElement what) { return QueryBuilderHelper.Apply("IsConnected", what); }

        public static QueryBuilderElement Cluster(double r, params QueryBuilderElement[] patterns) { return QueryBuilderHelper.Apply("Cluster", QueryBuilderHelper.Args(Value(r)).Concat(patterns).ToArray()); }
        public static QueryBuilderElement Cluster(double r, IEnumerable<QueryBuilderElement> patterns) { return QueryBuilderHelper.Apply("Cluster", QueryBuilderHelper.Args(Value(r)).Concat(patterns).ToArray()); }
        public static QueryBuilderElement Near(double r, params QueryBuilderElement[] patterns) { return QueryBuilderHelper.Apply("Near", QueryBuilderHelper.Args(Value(r)).Concat(patterns).ToArray()); }
        public static QueryBuilderElement Near(double r, IEnumerable<QueryBuilderElement> patterns) { return QueryBuilderHelper.Apply("Near", QueryBuilderHelper.Args(Value(r)).Concat(patterns).ToArray()); }
        public static QueryBuilderElement AmbientAtoms(QueryBuilderElement inner, double r, bool NoWaters = true, bool ExcludeBase = false, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("AmbientAtoms", QueryBuilderHelper.Args(inner, QueryBuilderHelper.Value(r)), new[] { QueryBuilderHelper.NoWatersOption(NoWaters), QueryBuilderHelper.Option("ExcludeBase", ExcludeBase), QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public static QueryBuilderElement AmbientResidues(QueryBuilderElement inner, double r, bool NoWaters = true, bool ExcludeBase = false, bool YieldNamedDuplicates = false) { return QueryBuilderHelper.Apply("AmbientResidues", QueryBuilderHelper.Args(inner, QueryBuilderHelper.Value(r)), new[] { QueryBuilderHelper.NoWatersOption(NoWaters), QueryBuilderHelper.Option("ExcludeBase", ExcludeBase), QueryBuilderHelper.Option(SymbolTable.YieldNamedDuplicatesOption.Name, YieldNamedDuplicates) }); }
        public static QueryBuilderElement Filled(QueryBuilderElement inner, double RadiusFactor = 0.75, bool NoWaters = true) { return QueryBuilderHelper.Apply("Filled", QueryBuilderHelper.Args(inner), new[] { QueryBuilderHelper.Option("RadiusFactor", RadiusFactor), QueryBuilderHelper.NoWatersOption(NoWaters) }); }
        public static QueryBuilderElement NearestDistanceTo(QueryBuilderElement what, QueryBuilderElement to) { return QueryBuilderHelper.Apply("NearestDistanceTo", what, to); }
        public static QueryBuilderElement DistanceCluster(IEnumerable<QueryBuilderElement> patterns, object DistanceMin, object DistanceMax) { return QueryBuilderHelper.Apply("DistanceCluster", patterns.ToArray(), new[] { QueryBuilderHelper.Option(SymbolTable.DistanceMatrixMinOption.Name, QueryBuilderHelper.Matrix(DistanceMin)), QueryBuilderHelper.Option(SymbolTable.DistanceMatrixMaxOption.Name, QueryBuilderHelper.Matrix(DistanceMax)) }); }
        public static QueryBuilderElement Stack2(double minDist, double maxDist, double minProjDist, double maxProjDist, double minAngleDeg, double maxAngleDeg, QueryBuilderElement pattern1, QueryBuilderElement pattern2) { return QueryBuilderHelper.Apply("Stack2", QueryBuilderHelper.Args(Value(minDist), Value(maxDist), Value(minProjDist), Value(maxProjDist), Value(minAngleDeg), Value(maxAngleDeg), pattern1, pattern2)); }
        ////public static QueryBuilderElement Tunnels(QueryBuilderElement inner, QueryBuilderElement start, double ProbeRadius = 3.0, double InteriorThreshold = 1.25, double BottleneckRadius = 1.25) 
        ////{ 
        ////    return QueryBuilderHelper.Apply("Tunnels", QueryBuilderHelper.Args(inner, start), 
        ////        new[] 
        ////        { 
        ////            QueryBuilderHelper.Option(SymbolTable.ProbeRadiusOption.Name, ProbeRadius), 
        ////            QueryBuilderHelper.Option(SymbolTable.InteriorThresholdOption.Name, InteriorThreshold), 
        ////            QueryBuilderHelper.Option(SymbolTable.BottleneckRadiusOption.Name, BottleneckRadius) 
        ////        });
        ////}
        public static QueryBuilderElement EmptySpace(QueryBuilderElement inner, double ProbeRadius = 3.0, double InteriorThreshold = 1.25)
        {
            return QueryBuilderHelper.Apply("EmptySpace", QueryBuilderHelper.Args(inner),
                new[] 
                { 
                    QueryBuilderHelper.Option(SymbolTable.ProbeRadiusOption.Name, ProbeRadius), 
                    QueryBuilderHelper.Option(SymbolTable.InteriorThresholdOption.Name, InteriorThreshold)
                });
        }

        public static QueryBuilderElement CSA() { return QueryBuilderHelper.Apply("CSA"); }

        public static QueryBuilderElement AtomSimilarity(QueryBuilderElement a, QueryBuilderElement b) { return QueryBuilderHelper.Apply("AtomSimilarity", a, b); }
        public static QueryBuilderElement AtomSimilarity(string a, string b) { return QueryBuilderHelper.Apply("AtomSimilarity", QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(a)), QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(b))); }
        public static QueryBuilderElement AtomSimilarity(QueryBuilderElement a, string b) { return QueryBuilderHelper.Apply("AtomSimilarity", a, QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(b))); }
        public static QueryBuilderElement AtomSimilarity(string a, QueryBuilderElement b) { return QueryBuilderHelper.Apply("AtomSimilarity", QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(a)), b); }
        public static QueryBuilderElement ResidueSimilarity(QueryBuilderElement a, QueryBuilderElement b) { return QueryBuilderHelper.Apply("ResidueSimilarity", a, b); }
        public static QueryBuilderElement ResidueSimilarity(string a, string b) { return QueryBuilderHelper.Apply("ResidueSimilarity", QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(a)), QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(b))); }
        public static QueryBuilderElement ResidueSimilarity(QueryBuilderElement a, string b) { return QueryBuilderHelper.Apply("ResidueSimilarity", a, QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(b))); }
        public static QueryBuilderElement ResidueSimilarity(string a, QueryBuilderElement b) { return QueryBuilderHelper.Apply("ResidueSimilarity", QueryBuilderHelper.Apply("Pattern", QueryBuilderHelper.Value(a)), b); }
        public static QueryBuilderElement AtomProperty(QueryBuilderElement a, string name) { return QueryBuilderHelper.Apply("AtomProperty", a, QueryBuilderHelper.Value(name)); }
        //public static QueryBuilderElement Metadata(QueryBuilderElement m, string name) { return QueryBuilderHelper.Apply("Metadata", m, QueryBuilderHelper.Value(name)); }
        public static QueryBuilderElement Descriptor(QueryBuilderElement a, string name) { return QueryBuilderHelper.Apply("Descriptor", a, QueryBuilderHelper.Value(name)); }
        public static QueryBuilderElement AminoSequenceString(QueryBuilderElement m) { return QueryBuilderHelper.Apply("AminoSequenceString", m); }

        public static QueryBuilderElement Value(object val) { return QueryBuilderHelper.Value(val); }
        public static QueryBuilderElement Function(Func<dynamic, dynamic> f) { return new QueryBuilderLambda { Lambda = f }; }
        public static QueryBuilderElement Abs(QueryBuilderElement a) { return QueryBuilderHelper.Apply("Abs", a); }
        public static QueryBuilderElement LogicalXor(QueryBuilderElement l, QueryBuilderElement r) { return QueryBuilderHelper.Apply(LogicalOp.LogicalXor.ToString(), l, r); }
        public static QueryBuilderElement LogicalXor(QueryBuilderElement l, object r) { return QueryBuilderHelper.Apply(LogicalOp.LogicalXor.ToString(), l, QueryBuilderHelper.Value(r)); }
        public static QueryBuilderElement Xor(QueryBuilderElement l, QueryBuilderElement r) { return QueryBuilderHelper.Apply(LogicalOp.LogicalXor.ToString(), l, r); }
        public static QueryBuilderElement Xor(QueryBuilderElement l, object r) { return QueryBuilderHelper.Apply(LogicalOp.LogicalXor.ToString(), l, QueryBuilderHelper.Value(r)); }

        public static QueryBuilderElement Find(QueryBuilderElement where, QueryBuilderElement what) { return QueryBuilderHelper.Apply("Find", where, what); }

        public static QueryBuilderElement Many(QueryBuilderElement a, int n) { return QueryBuilderHelper.Apply("Repeat", a, QueryBuilderHelper.Value(n)); }
        public static QueryBuilderElement Many(object a, int n) { return QueryBuilderHelper.Apply("Repeat", QueryBuilderHelper.Value(a), QueryBuilderHelper.Value(n)); }

        public static QueryBuilderElement LogicalNot(QueryBuilderElement e) { return new QueryBuilderApply { Head = LogicalOp.LogicalNot.ToString(), Args = new QueryBuilderElement[] { e } }; }
        public static QueryBuilderElement Not(QueryBuilderElement e) { return new QueryBuilderApply { Head = LogicalOp.LogicalNot.ToString(), Args = new QueryBuilderElement[] { e } }; }

        // Metadata
        static QueryBuilderElement HasAllHelper(string name, QueryBuilderElement e, string[] props)
        {
            return QueryBuilderHelper.Apply("HasAll" + name, new QueryBuilderElement[] { e }.Concat(props.Select(n => QueryBuilderHelper.Value(n))).ToArray());
        }

        static QueryBuilderElement HasAnyHelper(string name, QueryBuilderElement e, string[] props)
        {
            return QueryBuilderHelper.Apply("HasAny" + name, new QueryBuilderElement[] { e }.Concat(props.Select(n => QueryBuilderHelper.Value(n))).ToArray());
        }
        
        public static QueryBuilderElement ReleaseDate(QueryBuilderElement e) { return QueryBuilderHelper.Apply("ReleaseDate", e); }
        public static QueryBuilderElement ReleaseYear(QueryBuilderElement e) { return QueryBuilderHelper.Apply("ReleaseYear", e); }
        public static QueryBuilderElement LatestRevisionDate(QueryBuilderElement e) { return QueryBuilderHelper.Apply("LatestRevisionDate", e); }
        public static QueryBuilderElement LatestRevisionYear(QueryBuilderElement e) { return QueryBuilderHelper.Apply("LatestRevisionYear", e); }

        public static QueryBuilderElement Resolution(QueryBuilderElement e) { return QueryBuilderHelper.Apply("Resolution", e); }
        public static QueryBuilderElement Weight(QueryBuilderElement e) { return QueryBuilderHelper.Apply("Weight", e); }
        public static QueryBuilderElement PolymerType(QueryBuilderElement e) { return QueryBuilderHelper.Apply("PolymerType", e); }
        public static QueryBuilderElement ExperimentMethod(QueryBuilderElement e) { return QueryBuilderHelper.Apply("ExperimentMethod", e); }
        public static QueryBuilderElement ProteinStoichiometry(QueryBuilderElement e) { return QueryBuilderHelper.Apply("ProteinStoichiometry", e); }
        public static QueryBuilderElement ProteinStoichiometryString(QueryBuilderElement e) { return QueryBuilderHelper.Apply("ProteinStoichiometryString", e); }

        public static QueryBuilderElement Authors(QueryBuilderElement e) { return QueryBuilderHelper.Apply("Authors", e); }
        public static QueryBuilderElement HasAllAuthors(QueryBuilderElement e, params string[] props) { return HasAllHelper("Authors", e, props); }
        public static QueryBuilderElement HasAnyAuthor(QueryBuilderElement e, params string[] props) { return HasAnyHelper("Author", e, props); }

        public static QueryBuilderElement Keywords(QueryBuilderElement e) { return QueryBuilderHelper.Apply("Keywords", e); }
        public static QueryBuilderElement HasAllKeywords(QueryBuilderElement e, params string[] props) { return HasAllHelper("Keywords", e, props); }
        public static QueryBuilderElement HasAnyKeyword(QueryBuilderElement e, params string[] props) { return HasAnyHelper("Keyword", e, props); }

        public static QueryBuilderElement EntityTypes(QueryBuilderElement e) { return QueryBuilderHelper.Apply("EntityTypes", e); }
        public static QueryBuilderElement HasAllEntityTypes(QueryBuilderElement e, params string[] props) { return HasAllHelper("EntityTypes", e, props); }
        public static QueryBuilderElement HasAnyEntityType(QueryBuilderElement e, params string[] props) { return HasAnyHelper("EntityType", e, props); }

        public static QueryBuilderElement ECNumbers(QueryBuilderElement e) { return QueryBuilderHelper.Apply("ECNumbers", e); }
        public static QueryBuilderElement HasAllECNumbers(QueryBuilderElement e, params string[] props) { return HasAllHelper("ECNumbers", e, props); }
        public static QueryBuilderElement HasAnyECNumber(QueryBuilderElement e, params string[] props) { return HasAnyHelper("ECNumber", e, props); }

        public static QueryBuilderElement OriginOrganisms(QueryBuilderElement e) { return QueryBuilderHelper.Apply("OriginOrganisms", e); }
        public static QueryBuilderElement HasAllOriginOrganisms(QueryBuilderElement e, params string[] props) { return HasAllHelper("OriginOrganisms", e, props); }
        public static QueryBuilderElement HasAnyOriginOrganism(QueryBuilderElement e, params string[] props) { return HasAnyHelper("OriginOrganism", e, props); }

        public static QueryBuilderElement OriginOrganismIds(QueryBuilderElement e) { return QueryBuilderHelper.Apply("OriginOrganismIds", e); }
        public static QueryBuilderElement HasAllOriginOrganismIds(QueryBuilderElement e, params string[] props) { return HasAllHelper("OriginOrganismIds", e, props); }
        public static QueryBuilderElement HasAnyOriginOrganismId(QueryBuilderElement e, params string[] props) { return HasAnyHelper("OriginOrganismId", e, props); }

        public static QueryBuilderElement OriginOrganismGenus(QueryBuilderElement e) { return QueryBuilderHelper.Apply("OriginOrganismGenus", e); }
        public static QueryBuilderElement HasAllOriginOrganismGenus(QueryBuilderElement e, params string[] props) { return HasAllHelper("OriginOrganismGenus", e, props); }
        public static QueryBuilderElement HasAnyOriginOrganismGenus(QueryBuilderElement e, params string[] props) { return HasAnyHelper("OriginOrganismGenus", e, props); }

        public static QueryBuilderElement HostOrganisms(QueryBuilderElement e) { return QueryBuilderHelper.Apply("HostOrganisms", e); }
        public static QueryBuilderElement HasAllHostOrganisms(QueryBuilderElement e, params string[] props) { return HasAllHelper("HostOrganisms", e, props); }
        public static QueryBuilderElement HasAnyHostOrganism(QueryBuilderElement e, params string[] props) { return HasAnyHelper("HostOrganism", e, props); }

        public static QueryBuilderElement HostOrganismIds(QueryBuilderElement e) { return QueryBuilderHelper.Apply("HostOrganismIds", e); }
        public static QueryBuilderElement HasAllHostOrganismIds(QueryBuilderElement e, params string[] props) { return HasAllHelper("HostOrganismIds", e, props); }
        public static QueryBuilderElement HasAnyHostOrganismId(QueryBuilderElement e, params string[] props) { return HasAnyHelper("HostOrganismId", e, props); }

        public static QueryBuilderElement HostOrganismGenus(QueryBuilderElement e) { return QueryBuilderHelper.Apply("HostOrganismGenus", e); }
        public static QueryBuilderElement HasAllHostOrganismGenus(QueryBuilderElement e, params string[] props) { return HasAllHelper("HostOrganismGenus", e, props); }
        public static QueryBuilderElement HasAnyHostOrganismGenus(QueryBuilderElement e, params string[] props) { return HasAnyHelper("HostOrganismGenus", e, props); }
    }
}
