namespace WebChemistry.Platform.MoleculeDatabase.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Utils;

    class StringFilterGrammar : SharedGrammar
    {
        public static Rule Element = Node(OneOrMore(MatchChar(c => !"()\"@!|&^,".Contains(c) && !char.IsWhiteSpace(c))));
        public static Rule String = Node(MatchChar('"') + ZeroOrMore(ExceptCharSet("\"")) + MatchChar('"'));
        public static Rule Ring = Node(MatchChar('@') + MatchChar('(') + CommaDelimited(WS + Element + WS)  + MatchChar(')'));

        public static Rule RecExpr = Recursive(() => Expression);
        public static Rule ParanExpr = Node(CharToken('(') + RecExpr + CharToken(')'));
        public static Rule Negate = Node(CharToken('!') + WS + Recursive(() => SimpleExpr));
        public static Rule SimpleExpr = (Ring | Element | String | ParanExpr | Negate) + WS;
        public static Rule BinaryOp = Node(MatchStringSet("& | ^"));
        public static Rule Expression = Node(WS + SimpleExpr + ZeroOrMore(WS + BinaryOp + WS + SimpleExpr));
        static StringFilterGrammar() { InitGrammar(typeof(StringFilterGrammar)); }
    }

    public class StringFilter
    {
        abstract class Expr
        {
            public abstract bool PassesFilter(Func<string, bool> condition);
            public abstract bool PassesNegation(Func<string, bool> condition);

            public abstract void CollectElements(List<ElementNode> elements);
        }

        abstract class NExpr : Expr
        {
            public Expr[] Children;

            public override void CollectElements(List<ElementNode> elements)
            {
                foreach (var c in Children) c.CollectElements(elements);
            }
        }

        class ElementNode : Expr
        {
            public string Value;

            public override bool PassesFilter(Func<string, bool> condition)
            {
                return condition(Value);
            }

            public override bool PassesNegation(Func<string, bool> condition)
            {
                return !condition(Value);
            }

            public override void CollectElements(List<ElementNode> elements)
            {
                elements.Add(this);
            }
        }

        class NegateNode : Expr
        {
            public Expr Child;

            public override bool PassesFilter(Func<string, bool> condition)
            {
                return Child.PassesNegation(condition);
            }

            public override bool PassesNegation(Func<string, bool> condition)
            {
                return Child.PassesFilter(condition);
            }

            public override void CollectElements(List<ElementNode> elements)
            {
                Child.CollectElements(elements);
            }
        }

        class OrNode : NExpr
        {   
            public override bool PassesFilter(Func<string, bool> condition)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    if (Children[i].PassesFilter(condition)) return true;
                }
                return false;
            }

            public override bool PassesNegation(Func<string, bool> condition)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    if (Children[i].PassesFilter(condition)) return false;
                }
                return true;
            }
        }

        class XorNode : Expr
        {
            public Expr Left, Right;

            public override void CollectElements(List<ElementNode> elements)
            {
                Left.CollectElements(elements);
                Right.CollectElements(elements);
            }

            public override bool PassesFilter(Func<string, bool> condition)
            {
                return (Left.PassesFilter(condition) && Right.PassesNegation(condition))
                    || (Left.PassesNegation(condition) && Right.PassesFilter(condition));
            }

            public override bool PassesNegation(Func<string, bool> condition)
            {
                return (Left.PassesFilter(condition) && Right.PassesFilter(condition))
                    || (Left.PassesNegation(condition) && Right.PassesNegation(condition));
            } 
        }

        class AndNode : NExpr
        {
            public override bool PassesFilter(Func<string, bool> condition)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    if (!Children[i].PassesFilter(condition)) return false;
                }
                return true;
            }

            public override bool PassesNegation(Func<string, bool> condition)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    if (!Children[i].PassesFilter(condition)) return true;
                }
                return true;
            }
        }

        Expr root;

        /// <summary>
        /// The original input string.
        /// </summary>
        public string FilterString { get; private set; }

        /// <summary>
        /// Check if the filter is always passed.
        /// </summary>
        public bool AlwaysTrue { get; private set; }

        /// <summary>
        /// Checks if a set passes the filter.
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        public bool Passes(Func<string, bool> condition)
        {
            if (AlwaysTrue) return true;
            return root.PassesFilter(condition);
        }

        static Expr[] GetFlatChilds<T>(Node n, string op)
            where T : NExpr
        {
            List<Expr> transformed = new List<Expr>();
            for (int i = 0; i < n.Count; i++)
            {
                if (i % 2 == 0) transformed.Add(Transform(n[i]));
                else if (n[i].Text != op)
                {
                    throw new ArgumentException(string.Format("Mixed operators detected in expression '{0}'. Please use parentheses to determine precedence.", n.Text));
                }
            }

            return transformed.SelectMany(t => t is T ? (t as T).Children : new Expr[] { t }).ToArray();
        }
        
        static Expr Transform(Node n)
        {
            switch (n.Label)
            {
                case "Element": return new ElementNode { Value = n.Text };
                case "String": return new ElementNode { Value = n.Text.Substring(1, n.Text.Length - 2) };
                case "Ring":
                    var elements = n.Nodes
                        .Where(x => x.Label == "Element")
                        .Select(x => ElementSymbol.Create(x.Text).ToString())
                        .ToArray();

                    return new ElementNode { Value = Ring.GetFingerprint(elements, e => e.ToString()) };
                case "ParanExpr": return Transform(n[0]);
                case "Negate": return new NegateNode { Child = Transform(n[0]) };
                case "Expression":
                    // negation
                    if (n.Count == 1) return Transform(n[0]);
                    
                    //var l = Transform(n[0]);
                    //var r = Transform(n[2]);

                    var op = n[1].Text;

                    if (op == "&")
                    {
                        return new AndNode { Children = GetFlatChilds<AndNode>(n, op) };
                    }
                    else if (op == "|") // == "|"
                    {
                        return new OrNode { Children = GetFlatChilds<OrNode>(n, op) };
                    }
                    else // == "^"
                    {
                        var childs = GetFlatChilds<OrNode>(n, op);
                        return childs.Skip(1).Aggregate(childs[0], (a, e) => new XorNode { Left = a, Right = e });
                    }
                default:
                    throw new ArgumentException("Unexpected type of node " + n.Label);
            }
        }

        void Init(bool isRegex)
        {
            if (isRegex)
            {
                root = new ElementNode { Value = FilterString };
            }
            else
            {
                var node = StringFilterGrammar.Expression.Parse(FilterString)[0];
                if (node.Text.Length != node.Input.Length) throw new ArgumentException("Invalid input string.");
                root = Transform(node);
            }
        }

        /// <summary>
        /// Get unique element values for regex testing.
        /// </summary>
        /// <returns></returns>
        public string[] GetUniqueElementValues()
        {
            var elements = new List<ElementNode>();
            root.CollectElements(elements);
            return elements.Select(e => e.Value).Distinct(StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// Creates an instance of the string filter class.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="isRegex">ignore the DSL for regex queries</param>
        public StringFilter(string filter, bool isRegex)
        {
            FilterString = (filter ?? "").Trim();

            if (string.IsNullOrWhiteSpace(FilterString))
            {
                AlwaysTrue = true;
                return;
            }

            Init(isRegex);
        }
    }
}
