namespace WebChemistry.Queries.Core.Queries
{
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    
    /// <summary>
    /// Joins sub-queries
    /// </summary>
    class OrQuery : QueryMotive
    {
        QueryMotive[] subqueries;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            HashSet<Motive> yielded = new HashSet<Motive>();

            foreach (var q in subqueries)
            {
                foreach (var m in q.Execute(context))
                {
                    if (yielded.Add(m)) yield return m;
                }
            }
        }

        protected override string ToStringInternal()
        {
            return "Or[" + string.Join(",", subqueries.Select(q => q.ToString())) + "]";
        }

        public OrQuery(IEnumerable<QueryMotive> subqueries)
        {
            this.subqueries = subqueries.ToArray();
        }
    }
}
