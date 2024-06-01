namespace WebChemistry.Queries.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core.MetaQueries;
    using WebChemistry.Queries.Core.Queries;


    /// <summary>
    /// Extensions.
    /// </summary>
    public static class QueryEx
    {
        /// <summary>
        /// Select atoms that match the query.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static HashSet<IAtom> GetQueryAtoms(this IStructure structure, QueryMotive query, ExecutionContext context)
        {
            var matches = query.Execute(context);
            HashSet<IAtom> atoms = new HashSet<IAtom>();
            var sAtoms = structure.Atoms;
            foreach (var m in matches)
            {
                m.Atoms.VisitLeaves(a =>
                {
                    IAtom newAtom;
                    if (sAtoms.TryGetAtom(a.Id, out newAtom))
                    {
                        atoms.Add(newAtom);
                    }
                });
            }
            return atoms;
        }
    }

    /// <summary>
    /// Query base.
    /// </summary>
    public abstract class Query
    {
        public static readonly AppVersion Version = new AppVersion(1, 0, 21, 12, 14, 'a');
        
        /// <summary>
        /// Parse a query filter from a string.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Query Parse(string input, QueryTypes type = QueryTypes.Any)
        {
            switch (type)
            {
                case QueryTypes.Any: return Parse(input, TypeWildcard.Instance);
                case QueryTypes.PatternSeq: return Parse(input, BasicTypes.PatternSeq);
                case QueryTypes.Boolean: return Parse(input, BasicTypes.Bool);
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Parse a query.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Query Parse(string input, TypeExpression type)
        {
            var q = QueryLanguageConverter.ParseMeta(input);
            if (!TypeExpression.Unify(q.Type, type).Success) throw new ArgumentException(string.Format("The input has type '{0}' instead of '{1}'.", q.Type.ToString(), type.ToString()));
            var ret = q.Compile();
            return ret;
        }

        /// <summary>
        /// Just for testing.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static MetaQuery ParseMeta(string input)
        {
            return QueryLanguageConverter.ParseMeta(input);
        }

        /// <summary>
        /// Just for testing.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static MetaQuery ParseMeta(string input, TypeExpression type)
        {
            var ret = QueryLanguageConverter.ParseMeta(input);
            if (!TypeExpression.Unify(ret.Type, type).Success) throw new ArgumentException(string.Format("The input has type '{0}' instead of '{1}'.", ret.Type.ToString(), type.ToString()));
            return ret;
        }

        /// <summary>
        /// Parse a query filter from a string.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static MetaQuery ParseMeta(string input, QueryTypes type)
        {
            switch (type)
            {
                case QueryTypes.Any: return ParseMeta(input, TypeWildcard.Instance);
                case QueryTypes.PatternSeq: return ParseMeta(input, BasicTypes.PatternSeq);
                case QueryTypes.Boolean: return ParseMeta(input, BasicTypes.Bool);
            }

            throw new NotImplementedException();
        }

        string cachedName;
        /// <summary>
        /// Convert the query to a string. The result is cached.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (cachedName == null) cachedName = ToStringInternal();
            return cachedName;
        }

        /// <summary>
        /// Noncached version of ToString()
        /// </summary>
        /// <returns></returns>
        protected abstract string ToStringInternal();
        
        /// <summary>
        /// Find a list of all matches in a given structure.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public IList<Motive> Matches(IStructure structure)
        {
            var qm = this as QueryMotive;
            return qm.Execute(ExecutionContext.Create(structure)).ToList();
        }

        /// <summary>
        /// Find matches using a given context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IList<Motive> Matches(ExecutionContext context)
        {
            var qm = this as QueryMotive;
            return qm.Execute(context).ToList();
        }

        /// <summary>
        /// Matches multiple queries in a single structure (the Query context gets reused making the algorithm a lot faster).
        /// </summary>
        /// <param name="queries"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IList<IList<IStructure>> MatchMany(IList<QueryMotive> queries, IStructure structure, bool includeBonds = false, bool asPdb = false)
        {
            var context = ExecutionContext.Create(structure);

            int counter = 0;
            return queries.Select(q =>
                (IList<IStructure>)q.Execute(context).Select(m => m.ToStructure((counter++).ToString(), includeBonds, asPdb)).ToList()).ToList();
        }
                
        /// <summary>
        /// Execute the query as a specific type.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public abstract dynamic ExecuteDynamic(ExecutionContext context);

        /// <summary>
        /// Execute the query as if it was Query of T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract object ExecuteObject(ExecutionContext context);

        /// <summary>
        /// Create the query and assign the type.
        /// </summary>
        /// <param name="type"></param>
        protected Query()
        {
        }
    }
}
