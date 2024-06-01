namespace WebChemistry.Queries.Core.MetaQueries
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Queries.Core.Symbols;

    /// <summary>
    /// Assign.
    /// </summary>
    public class MetaAssign : MetaQuery
    {
        public MetaSymbol Symbol { get; private set; }
        public MetaQuery Value { get; private set; }

        public override MetaQuery Head
        {
            get { return BasicHeads.AssignHead; }
        }

        public override TypeExpression Type
        {
            get { return Value.Type; }
        }

        public static MetaAssign Create(MetaQuery symbol, MetaQuery value)
        {
            if (!(symbol is MetaSymbol))
            {
                throw new ArgumentException(string.Format("Cannot assign to expression '{0}'. Only symbols can be assigned to.", symbol));
            }


            return new MetaAssign { Symbol = symbol as MetaSymbol, Value = value };
        }

        internal override string ToStringInternal()
        {
            return string.Format("{0} = {1}", Symbol, Value);
        }

        internal override void InferTypes(Dictionary<string, TypeExpression> types)
        {
            TypeExpression val;
            if (!types.TryGetValue(Symbol.Name, out val))
            {
                Value.InferTypes(types);
            }

            if (types.Count == 1) return;

            types.Remove(Symbol.Name);
            Value.InferTypes(types);
            types.Add(Symbol.Name, val);
        }

        internal override void ReplaceTypes(Dictionary<string, TypeExpression> types)
        {
            TypeExpression val;
            if (!types.TryGetValue(Symbol.Name, out val))
            {
                Value.ReplaceTypes(types);
            }

            if (types.Count == 1) return;

            types.Remove(Symbol.Name);
            Value.ReplaceTypes(types);
            types.Add(Symbol.Name, val);
        }

        public override MetaQuery SubstituteSymbols(Dictionary<string, MetaQuery> values)
        {
            MetaQuery val;
            if (!values.TryGetValue(Symbol.Name, out val))
            {
                return MetaAssign.Create(Symbol, Value.SubstituteSymbols(values));
            }

            if (values.Count == 1) return this;

            values.Remove(Symbol.Name);
            var ret = MetaAssign.Create(Symbol, Value.SubstituteSymbols(values));
            values.Add(Symbol.Name, val);
            return ret;
        }

        private MetaAssign()
        {
        }
    }

    /// <summary>
    /// Tuples.
    /// </summary>
    public class MetaTuple : MetaQuery
    {
        TypeExpression innerType;

        public MetaQuery[] Elements { get; private set; }
        
        public override MetaQuery Head
        {
            get { return BasicHeads.TupleHead; }
        }

        public override TypeExpression Type
        {
            get { return innerType; }
        }

        public static MetaTuple Create(MetaQuery[] elements)
        {
            List<MetaQuery> flat = new List<MetaQuery>();

            var seqName = SymbolTable.SequenceSymbol.Name;
            foreach (var e in elements)
            {
                if (e.HeadEquals(seqName)) flat.AddRange((e as MetaApply).Arguments);
                else flat.Add(e);
            }

            return new MetaTuple(flat.ToArray());
        }

        internal override string ToStringInternal()
        {
            return "[" + string.Join(",", Elements.Select(e => e.ToString()).ToArray()) + "]";
        }
        
        internal override void InferTypes(Dictionary<string, TypeExpression> types)
        {
            Elements.ForEach(e => e.InferTypes(types));
        }

        internal override void ReplaceTypes(Dictionary<string, TypeExpression> types)
        {
            Elements.ForEach(e => e.ReplaceTypes(types));
        }

        public override MetaQuery SubstituteSymbols(Dictionary<string, MetaQuery> values)
        {
            return new MetaTuple(Elements.Select(e => e.SubstituteSymbols(values)).ToArray());
        }

        protected MetaTuple(MetaQuery[] elements)
        {
            this.Elements = elements;
            this.innerType = TypeTuple.Create(elements.Select(e => e.Type));
        }
    }

    /// <summary>
    /// Lambdas.
    /// </summary>
    public class MetaLambda : MetaQuery
    {
        HashSet<string> argNames;
        public MetaSymbol[] Arguments { get; private set; }
        public MetaQuery Body { get; private set; }
        
        TypeExpression type;
        public override TypeExpression Type
        {
            get { return type; }
        }

        public override MetaQuery Head
        {
            get { return BasicHeads.LambdaHead; }
        }

        internal override Query Compile(Dictionary<string, QueryMotive> compileCache)
        {
            return new LambdaQuery(Arguments.Select(a => a.Name), Body.Compile(compileCache));
        }

        public static MetaLambda Create(MetaQuery argsQuery, MetaQuery body)
        {
            MetaQuery[] tempArgs;
            if (argsQuery is MetaTuple) tempArgs = (argsQuery as MetaTuple).Elements;
            else if (argsQuery.HeadEquals(SymbolTable.SequenceSymbol.Name)) tempArgs = (argsQuery as MetaApply).Arguments;
            else tempArgs = new MetaQuery[] { argsQuery };

            var args = new List<MetaSymbol>();
            foreach (var arg in tempArgs)
            {
                var symbol = arg as MetaSymbol;
                if (symbol == null) throw new ArgumentException(string.Format("Expression '{0}' cannot be used as an lambda argument name.", arg));
                if (SymbolTable.TryGetDescriptor(symbol.Name) != null) string.Format("Symbol '{0}' cannot be used as an lambda argument because it's a built-in function.", arg);
                args.Add(symbol);
            }

            var types = args.ToDictionary(a => a.Name, a => a.Type, StringComparer.OrdinalIgnoreCase);

            try
            {
                body.InferTypes(types);
                args.ForEach(t => t.ReplaceTypes(types));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(string.Format("Type inference error in expression '{0}': {1}", body.ToString(), e.Message));
            }

            return new MetaLambda
            {
                argNames = new HashSet<string>(args.Select(a => a.Name), StringComparer.OrdinalIgnoreCase),
                Arguments = args.ToArray(),
                Body = body,
                type = TypeArrow.Create(TypeTuple.Create(args.Select(a => a.InferredType)), body.Type)
            };
        }
                
        internal override void InferTypes(Dictionary<string, TypeExpression> types)
        {
            Dictionary<string, TypeExpression> innerTypes = new Dictionary<string, TypeExpression>(types.Comparer);
            foreach (var t in types)
            {
                if (!argNames.Contains(t.Key)) innerTypes.Add(t.Key, t.Value);
            }
            if (innerTypes.Count > 0)
            {
                Body.InferTypes(innerTypes);
                foreach (var t in innerTypes)
                {
                    types[t.Key] = t.Value;
                }
            }
        }

        internal override void ReplaceTypes(Dictionary<string, TypeExpression> types)
        {
            Dictionary<string, TypeExpression> innerTypes = new Dictionary<string, TypeExpression>(types.Comparer);
            foreach (var t in types)
            {
                if (!argNames.Contains(t.Key)) innerTypes.Add(t.Key, t.Value);
            }
            if (innerTypes.Count > 0) Body.ReplaceTypes(innerTypes);
        }

        public override MetaQuery SubstituteSymbols(Dictionary<string, MetaQuery> values)
        {
            Dictionary<string, MetaQuery> innerValues = new Dictionary<string, MetaQuery>(values.Comparer);
            foreach (var v in values)
            {
                if (!argNames.Contains(v.Key)) innerValues.Add(v.Key, v.Value);
            }

            if (innerValues.Count > 0) return MetaLambda.Create(MetaTuple.Create(Arguments), Body.SubstituteSymbols(innerValues));
            return this;
        }

        internal override string ToStringInternal()
        {
            var args =  Arguments.Length == 0 ? "[]" : string.Join(",", Arguments.Select(a => a.ToString()).ToArray());
            if (Arguments.Length > 1) args = "(" + args + ")";
            return args + " => " + Body.ToString();
        }

        private MetaLambda()
        {

        }
    }

    public class MetaLet : MetaQuery
    {
        private MetaQuery Substitued { get; set; }
        public MetaAssign Assignment { get; private set; }
        public MetaQuery Expression { get; private set; }

        public override TypeExpression Type
        {
            get { return Substitued.Type; }
        }

        public override MetaQuery Head
        {
            get { return BasicHeads.LetHead; }
        }

        public static MetaLet Create(MetaQuery assignmentQuery, MetaQuery expression)
        {
            if (!(assignmentQuery is MetaAssign))
            {
                throw new ArgumentException(string.Format("'{0}' is not an assignment in let expression.", assignmentQuery));
            }

            var assignment = assignmentQuery as MetaAssign;
            var substitution = new Dictionary<string, MetaQuery>(StringComparer.OrdinalIgnoreCase) { { assignment.Symbol.Name, assignment.Value } };

            try
            {
                var substitued = expression.SubstituteSymbols(substitution);
                return new MetaLet { Substitued = substitued, Assignment = assignment, Expression = expression };

            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Cannot create let binding: " + e.Message);
            }
        }
        
        internal override void InferTypes(Dictionary<string, TypeExpression> types)
        {
            TypeExpression val;
            if (!types.TryGetValue(Assignment.Symbol.Name, out val))
            {
                Assignment.InferTypes(types);
                Expression.InferTypes(types);
                Substitued.InferTypes(types);
            }

            if (types.Count == 1) return;

            types.Remove(Assignment.Symbol.Name);
            Assignment.InferTypes(types);
            Expression.InferTypes(types);
            Substitued.InferTypes(types);
            types.Add(Assignment.Symbol.Name, val);
        }

        internal override void ReplaceTypes(Dictionary<string, TypeExpression> types)
        {
            TypeExpression val;
            if (!types.TryGetValue(Assignment.Symbol.Name, out val))
            {
                Assignment.ReplaceTypes(types);
                Expression.ReplaceTypes(types);
                Substitued.ReplaceTypes(types);
            }

            if (types.Count == 1) return;

            types.Remove(Assignment.Symbol.Name);
            Assignment.ReplaceTypes(types);
            Expression.ReplaceTypes(types);
            Substitued.ReplaceTypes(types);
            types.Add(Assignment.Symbol.Name, val);
        }

        public override MetaQuery SubstituteSymbols(Dictionary<string, MetaQuery> values)
        {
            MetaQuery val;
            if (!values.TryGetValue(Assignment.Symbol.Name, out val))
            {
                return new MetaLet
                {
                    Assignment = Assignment.SubstituteSymbols(values) as MetaAssign,
                    Expression = Expression.SubstituteSymbols(values),
                    Substitued = Substitued.SubstituteSymbols(values)
                };
            }

            if (values.Count == 1) return this;

            values.Remove(Assignment.Symbol.Name);
            var ret = new MetaLet
            {
                Assignment = Assignment.SubstituteSymbols(values) as MetaAssign,
                Expression = Expression.SubstituteSymbols(values),
                Substitued = Substitued.SubstituteSymbols(values)
            };
            values.Add(Assignment.Symbol.Name, val);
            return ret;
        }

        internal override Query Compile(Dictionary<string, QueryMotive> compileCache)
        {
            return Substitued.Compile(compileCache);
        }

        internal override string ToStringInternal()
        {
            return string.Format("let {0} in {1}", Assignment, Expression);
        }
    }

    public class MetaApply : MetaQuery
    {
        MetaQuery head;
        TypeExpression type;
        
        public override MetaQuery Head { get { return head; } }

        public override TypeExpression Type
        {
            get { return type; }
        }    

        public MetaQuery[] Arguments { get; private set; }
        public Dictionary<string, MetaQuery> Options { get; private set; }

        static void ProcessArgs(SymbolDescriptor symbol, MetaQuery[] tuple, out Dictionary<string, MetaQuery> options, out List<MetaQuery> arguments)
        {
            var argOptions = tuple.Where(a => a is MetaAssign).ToList();
            var symbolOptions = symbol.Options;

            options = new Dictionary<string, MetaQuery>(StringComparer.OrdinalIgnoreCase);

            foreach (MetaAssign opt in argOptions)
            {
                var name = opt.Symbol.Name;
                var value = opt.Value;

                MetaOption spec;
                if (!symbolOptions.TryGetValue(name, out spec))
                {
                    throw new ArgumentException(string.Format("There is no option '{0}' for symbol '{1}'.", name, symbol.Name));
                }
                if (!TypeExpression.Unify(spec.Type, value.Type).Success)
                {
                    throw new ArgumentException(string.Format("The option '{0}' for symbol '{1}' must have type '{2}' but got '{3}' instead.",
                        name, symbol.Name, spec.Type.ToString(), value.Type.ToString()));
                }
                options.Add(name, value);
            }

            foreach (var opt in symbolOptions)
            {
                if (!options.ContainsKey(opt.Key)) options.Add(opt.Key, opt.Value.DefaultValue);
            }

            var traits = symbol.Attributes;
            var args = new List<MetaQuery>();
            
            foreach (var arg in tuple)
            {
                var headSymbol = arg.Head as MetaSymbol;
                if (arg is MetaAssign) continue;
                if (arg.HeadEquals(SymbolTable.SequenceSymbol.Name))
                {
                    args.AddRange((arg as MetaApply).Arguments);
                }
                else args.Add(arg);
            }
            // F<A>[x, F<A>[y, z]] => F<A>[x, y, z]
            if (symbol.Options.Count == 0 && traits.HasFlag(SymbolAttributes.Flat))
            {
                args = args.SelectMany(a => (a.Head is MetaSymbol) && (a.Head as MetaSymbol).Name.Equals(symbol.Name, StringComparison.Ordinal) 
                    ? (a as MetaApply).Arguments.AsEnumerable() : new MetaQuery[] { a })
                    .ToList();
            }

            // [x, Empty, y] => [x, y]
            if (traits.HasFlag(SymbolAttributes.IgnoreEmpty)) args = args.Where(a => !(a is EmptyQueryElement)).ToList();

            // [x, x] => [x]
            if (traits.HasFlag(SymbolAttributes.UniqueArgs)) args = args.Distinct(a => a.ToString(), StringComparer.Ordinal).ToList();
            
            // [y, x, z] => [x, y, z]
            if (traits.HasFlag(SymbolAttributes.Orderless)) args = args.OrderBy(q => q.ToString()).ToList();

            arguments = args;
        }
        
        public static MetaQuery Create(MetaQuery head, MetaQuery arguments)
        {
            if (!(head is MetaSymbol) && !(head is MetaLambda))
            {
                throw new ArgumentException(string.Format("Expression {0} cannot be applied. Only Symbols and Lambdas can be applied.", head.ToString()));
            }
            if (!(arguments is MetaTuple))
            {
                arguments = MetaTuple.Create(new MetaQuery[] { arguments });
                //throw new ArgumentException(string.Format("Expression {0} is not of type Tuple and cannot be applied to.", arguments.ToString()));
            }

            var args = (arguments as MetaTuple).Elements;

            var symbol = head as MetaSymbol;            
            if (symbol != null)
            {
                var descriptor = symbol.Descriptor;
                if (descriptor == null) return new MetaApply { head = head, Arguments = args, Options = new Dictionary<string, MetaQuery>(StringComparer.OrdinalIgnoreCase) };

                Dictionary<string, MetaQuery> options;
                List<MetaQuery> argList;
                ProcessArgs(descriptor, args, out options, out argList);

                var type = TypeArrow.Create(TypeTuple.Create(argList.Select(a => a.Type)), TypeVariable.Create("__ret__"));

                var unification = TypeExpression.Unify(descriptor.FunctionType, type);

                if (!unification.Success)
                {
                    throw new ArgumentException(string.Format("Cannot apply '{0}' to {1} :: '{2}', expected type '{3}'.", 
                        symbol.Name, arguments.ToString(), arguments.Type.ToString(), descriptor.FunctionType.From.ToString()));
                }

                // F[x] => x
                if (descriptor.Attributes.HasFlag(SymbolAttributes.OneIdentity) && argList.Count == 1) return argList[0];

                TypeExpression returnType;
                if (!unification.BSubstitutions.TryGetValue("__ret__", out returnType)) returnType = TypeWildcard.Instance;

                var ret = new MetaApply { head = head, type = returnType, Arguments = argList.ToArray(), Options = options };

                try
                {
                    ret.ReplaceTypes(unification.BSubstitutions);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(string.Format("Type inference error in expression '{0}': {1}", ret.ToString(), e.Message));
                }

                return descriptor.Normalize(ret);
            }

            var lambda = head as MetaLambda;
            if (lambda != null)
            {
                Dictionary<string, MetaQuery> options;
                List<MetaQuery> argList;
                ProcessArgs(SymbolTable.LambdaSymbol, args, out options, out argList);

                var type = TypeArrow.Create(TypeTuple.Create(argList.Select(a => a.Type)), TypeVariable.Create("__ret__"));

                var unification = TypeExpression.Unify(lambda.Type, type);

                if (!unification.Success)
                {
                    throw new ArgumentException(string.Format("Cannot apply '{0}' to {1} :: '{2}', expected type '{3}'.",
                        symbol.Name, arguments.ToString(), arguments.Type.ToString(), (lambda.Type as TypeArrow).From.ToString()));
                }
                                
                TypeExpression returnType;
                if (!unification.BSubstitutions.TryGetValue("__ret__", out returnType)) returnType = TypeWildcard.Instance;

                var ret = new MetaApply { head = head, type = returnType, Arguments = argList.ToArray(), Options = options };

                ret.ReplaceTypes(unification.BSubstitutions);

                return ret;
            }

            throw new NotSupportedException();
        }

        internal override Query Compile(Dictionary<string, QueryMotive> cache)
        {
            Query compiled = null;

            QueryMotive compiledMotive;
            if (cache.TryGetValue(ToString(), out compiledMotive))
            {
                return compiledMotive;
            }

            var symbol = Head as MetaSymbol;
            if (symbol != null)
            {
                var descriptor = symbol.Descriptor;
                if (descriptor == null) throw new InvalidOperationException(string.Format("Cannot compile symbol '{0}' because it is not defined.", symbol.Name));
                compiled = descriptor.Compile(this, cache);
            }
            else
            {
                var lambda = Head as MetaLambda;
                if (lambda != null)
                {
                    var subs = lambda.Arguments.Zip(this.Arguments, (n, a) => new { N = n, A = a }).ToDictionary(a => a.N.Name, a => a.A, StringComparer.OrdinalIgnoreCase);
                    compiled = lambda.Body.SubstituteSymbols(subs).Compile();
                }
            }

            if (compiled == null) throw new NotSupportedException(string.Format("Cannot compile expression '{0}'.", ToString()));

            if (compiled is QueryMotive)
            {
                cache.Add(this.ToString(), compiled as QueryMotive);
            }

            return compiled;
        }
        
        public override MetaQuery SubstituteSymbols(Dictionary<string, MetaQuery> values)
        {
            var options = Options.Select(o => MetaAssign.Create(new MetaSymbol(o.Key), o.Value.SubstituteSymbols(values)));
            return MetaApply.Create(Head.SubstituteSymbols(values), 
                MetaTuple.Create(options.Concat(Arguments.Select(a => a.SubstituteSymbols(values))).ToArray()));
        }

        internal override void InferTypes(Dictionary<string, TypeExpression> types)
        {
            Head.InferTypes(types);
            Arguments.ForEach(a => a.InferTypes(types));
            Options.Values.ForEach(o => o.InferTypes(types));
        }

        internal override void ReplaceTypes(Dictionary<string, TypeExpression> types)
        {
            Head.ReplaceTypes(types);
            Arguments.ForEach(a => a.ReplaceTypes(types));
            Options.Values.ForEach(o => o.ReplaceTypes(types));
        }

        public override string GetString()
        {
            if (Head is MetaSymbol && Arguments.Length == 0) return Head.GetString();
            return base.GetString();
        }

        internal override string ToStringInternal()
        {   
            var options = string.Join(",", Options.Select(o => o.Key + "=" + o.Value));
            var args = string.Join<MetaQuery>(",", Arguments);

            if (Head is MetaSymbol)
            {
                return string.Format("{0}[{1}{3}{2}]", head.ToString(), options, args, (Options.Count > 0 && Arguments.Length > 0) ? "," : "");
            }
            return string.Format("({0})[{1}{3}{2}]", head.ToString(), options, args, (Options.Count > 0 && Arguments.Length > 0) ? "," : "");
        }
    }
}
