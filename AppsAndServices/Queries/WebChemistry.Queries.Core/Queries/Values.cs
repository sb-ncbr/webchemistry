namespace WebChemistry.Queries.Core.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using WebChemistry.Framework.Core;


    public abstract class QueryValueBase : Query<object>
    {
    }

    public class QueryList : Query<IList<Query>>
    {
        ReadOnlyCollection<Query> data;

        internal override IList<Query> Execute(ExecutionContext context)
        {
            return data;
        }

        protected override string ToStringInternal()
        {
            return "List[" + string.Join(",", data.Select(e => e.ToString())) + "]";
        }

        public QueryList(IEnumerable<Query> data)
        {
            this.data = new ReadOnlyCollection<Query>(data.ToArray());
        }
    }

    public class QueryValue : QueryValueBase
    {
        protected readonly object Value;

        internal override object Execute(ExecutionContext context)
        {
            return Value;
        }

        public QueryValue(object value)
        {
            this.Value = value;
        }

        protected override string ToStringInternal()
        {
            if (Value == null) return "";

            if (Value is double || Value is float)
            {
                return ((double)Value).ToStringInvariant();
            }
            if (Value is string)
            {
                return "\"" + Value + "\"";
            }
            
            return Value.ToString();
        }
    }
        
    public enum RelationOp
    {
        Equal,
        NotEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual
    }

    public enum LogicalOp
    {
        LogicalAnd,
        LogicalOr,
        LogicalXor,
        LogicalNot
    }

    public static class DynamicFunctions
    {
        const double Epsilon = 0.000000001;
        
        public static readonly Dictionary<RelationOp, Func<dynamic, dynamic, dynamic>> RelationOpFunctions
            = new Dictionary<RelationOp, Func<dynamic, dynamic, dynamic>>
        {
            { 
                RelationOp.Equal, (x, y) => 
                {
                    if (x == null && y == null) return true;
                    if (x == null || y == null) return false;

                    var sl = x as string;
                    var sr = y as string;
                    if (sl != null && sr != null) return sl.Equals(sr, StringComparison.Ordinal);
                    if (x is double || y is double) return Math.Abs(x - y) <= Epsilon;
                    return x.Equals(y);
                }
            },
            { 
                RelationOp.NotEqual, (x, y) =>
                {
                    if (x == null && y == null) return false;
                    if (x == null || y == null) return true;

                    var sl = x as string;
                    var sr = y as string;
                    if (sl != null && sr != null) return !sl.Equals(sr, StringComparison.Ordinal);
                    if (x is double || y is double) return Math.Abs(x - y) > Epsilon;
                    return !x.Equals(y);
                } 
            },
            { 
                RelationOp.Less, (x, y) =>
                {
                    var sl = x as string;
                    var sr = y as string;
                    if (sl != null && sr != null) return StringComparer.Ordinal.Compare(sl, sr) < 0;
                    return x < y;
                }
            },
            {
                RelationOp.LessEqual, (x, y) => 
                {
                    var sl = x as string;
                    var sr = y as string;
                    if (sl != null && sr != null) return StringComparer.Ordinal.Compare(sl, sr) <= 0;
                    return x <= y;
                } 
            },
            { 
                RelationOp.Greater, (x, y) => 
                {
                    var sl = x as string;
                    var sr = y as string;
                    if (sl != null && sr != null) return StringComparer.Ordinal.Compare(sl, sr) > 0;
                    return x > y;
                } 
            },
            { 
                RelationOp.GreaterEqual, (x, y) => 
                {
                    var sl = x as string;
                    var sr = y as string;
                    if (sl != null && sr != null) return StringComparer.Ordinal.Compare(sl, sr) >= 0;
                    return x >= y;
                } 
            }
        };

        public static readonly Dictionary<LogicalOp, Func<ExecutionContext, Query[], dynamic>> NAryLogicalFunctions 
            = new Dictionary<LogicalOp, Func<ExecutionContext, Query[], dynamic>>
        {
            { 
                LogicalOp.LogicalAnd, (ctx, xs) => 
                {
                    for (int i = xs.Length - 1; i >= 0; i--)
                    {
                        var x = xs[i].ExecuteDynamic(ctx);
                        if (x == null) return null;
                        if (!x) return false;
                    }
                    return true;
                }
            },
            { 
                LogicalOp.LogicalOr, (ctx, xs) => 
                {
                    for (int i = xs.Length - 1; i >= 0; i--)
                    {
                        var x = xs[i].ExecuteDynamic(ctx);
                        if (x == null) return null;
                        if (x) return true;
                    }
                    return false;
                }
            },
            { 
                LogicalOp.LogicalXor, (ctx, xs) => 
                {
                    var x = xs[0].ExecuteDynamic(ctx);
                    if (x == null) return null;
                    bool ret = x;
                    for (int i = xs.Length - 1; i > 0; i--)
                    {
                        x = xs[i].ExecuteDynamic(ctx);
                        if (x == null) return null;
                        ret = ret ^ x;
                    }
                    return ret;
                }
            }
        };

        //static int IntPower(int x, short power)
        //{
        //    if (power == 0) return 1;
        //    if (power == 1) return x;
        //    // ----------------------
        //    int n = 15;
        //    while ((power <<= 1) >= 0) n--;

        //    int tmp = x;
        //    while (--n > 0)
        //        tmp = tmp * tmp *
        //             (((power <<= 1) < 0) ? x : 1);
        //    return tmp;
        //}

        public static dynamic Power(dynamic x, dynamic y)
        {
            if (x is int && y is int)
            {
                short power = (short)y;
                int t = (int)x;

                if (power == 0) return 1;
                if (power == 1) return t;
                // ----------------------
                int n = 15;
                while ((power <<= 1) >= 0) n--;

                int tmp = t;
                while (--n > 0)
                {
                    tmp = tmp * tmp * (((power <<= 1) < 0) ? t : 1);
                }
                return tmp;
            }
            return Math.Pow(x, y);
        }
    }

    public class UnaryFunctionQuery : QueryValueBase
    {
        string name;
        Query x;
        Func<dynamic, dynamic> function;

        internal override object Execute(ExecutionContext context)
        {
            var t = x.ExecuteObject(context);
            if (t == null) return null;
            return function(t);
        }

        protected override string ToStringInternal()
        {
            return string.Format("{0}[{1}]", name, x.ToString());
        }

        public UnaryFunctionQuery(string name, Func<dynamic, dynamic> function, Query x)
        {
            //if (!function.Method.IsStatic)
            //{
            //    throw new ArgumentException("The unary op function must be static.");
            //}

            this.name = name;
            this.function = function;
            this.x = x;
        }
    }

    public class BinaryFunctionQuery : QueryValueBase
    {
        string name;
        Query left, right;
        Func<dynamic, dynamic, dynamic> function;

        internal override object Execute(ExecutionContext context)
        {
            var x = left.ExecuteObject(context);
            if (x != null)
            {
                var y = right.ExecuteObject(context);
                if (y != null) return function(x, y);
            }
            return null;
        }

        protected override string ToStringInternal()
        {
            return string.Format("{0}[{1},{2}]", name, left.ToString(), right.ToString());
        }

        public BinaryFunctionQuery(string name, Func<dynamic, dynamic, dynamic> function, Query left, Query right)
        {
            //if (!function.Method.IsStatic)
            //{
            //    throw new ArgumentException("The binary op function must be static.");
            //}

            this.name = name;
            this.function = function;
            this.left = left;
            this.right = right;
        }
    }

    public class NaryFunctionQuery : QueryValueBase
    {
        string name;
        Query[] xs;
        Func<ExecutionContext, Query[], dynamic> function;

        internal override object Execute(ExecutionContext context)
        {
            return function(context, xs);
        }

        protected override string ToStringInternal()
        {
            return string.Format("{0}[{1}]", name, String.Join(",", xs.Select(x => x.ToString())));
        }

        public NaryFunctionQuery(string name, Func<ExecutionContext, Query[], dynamic> function, IEnumerable<Query> xs)
        {
            //if (!function.Method.IsStatic)
            //{
            //    throw new ArgumentException("The unary op function must be static.");
            //}

            this.name = name;
            this.function = function;
            this.xs = xs.ToArray();
        }
    }
}
