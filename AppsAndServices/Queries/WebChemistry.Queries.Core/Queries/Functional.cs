namespace WebChemistry.Queries.Core.Queries
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Filters the query.
    /// </summary>
    public class FilterQuery : QueryMotive
    {
        QueryMotive inner;
        LambdaQuery filter;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            List<Motive> resultList = new List<Motive>();
            var args = new object[1];
            foreach (var m in inner.Execute(context))
            {
                args[0] = m;
                var passed = filter.Execute(context, args);
                if (passed != null && (bool)passed) resultList.Add(m);
            }
            return resultList;
        }

        protected override string ToStringInternal()
        {
            return string.Format("Filter[{0},{1}]", inner.ToString(), filter.ToString());
        }

        public FilterQuery(QueryMotive inner, LambdaQuery filter)
        {
            this.inner = inner;
            this.filter = filter;
        }
    }

    /// <summary>
    /// Executes the query only if a condition on a parent structure is met. Otherwise, return an empty sequence.
    /// </summary>
    public class ExecuteIfQuery : QueryMotive
    {
        QueryMotive inner;
        LambdaQuery filter;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var parent = context.RequestCurrentContext().StructureMotive;
            var passed = filter.Execute(context, new object[] { parent });
            if (passed != null && (bool)passed)
            {
                return inner.Execute(context);
            }
            else return new Motive[0];
        }

        protected override string ToStringInternal()
        {
            return string.Format("ExecuteIf[{0},{1}]", inner.ToString(), filter.ToString());
        }

        public ExecuteIfQuery(QueryMotive inner, LambdaQuery filter)
        {
            this.inner = inner;
            this.filter = filter;
        }
    }

    /// <summary>
    /// SelectMany flattens a sequence.
    /// </summary>
    public class SelectManyQuery : QueryMotive
    {
        QueryMotive source;
        LambdaQuery selector;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            List<Motive> resultList = new List<Motive>();
            var resultSet = new HashSet<Motive>();

            var args = new object[1];
            foreach (var m in source.Execute(context))
            {
                args[0] = m;
                var result = selector.Execute(context, args) as IEnumerable<Motive>;

                if (result == null)
                {
                    throw new InvalidOperationException(string.Format("The selector function of SelectMany must return type Motives, got {0} instead.", result.GetType().Name));
                }

                foreach (var p in result)
                {
                    if (resultSet.Add(p))
                    {
                        resultList.Add(p);
                    }
                }
            }

            return resultList;
        }

        protected override string ToStringInternal()
        {
            return string.Format("SelectMany[{0},{1}]", source.ToString(), selector.ToString());
        }

        public SelectManyQuery(QueryMotive source, LambdaQuery selector)
        {
            this.source = source;
            this.selector = selector;
        }
    }
}