namespace WebChemistry.Queries.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Utils;
    using WebChemistry.Queries.Core.MetaQueries;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Queries.Core.Symbols;

    /// <summary>
    /// Needs to be public because of reflection in Silverlight.
    /// </summary>
    public class QueryGrammar : SharedGrammar
    {
        //public static Rule NegAtomSet = Node(CharToken('[') + CharToken('^') + CommaDelimited(WS + InnerSimpleElement) + CharToken(']'));
        //public static Rule NegResidueSet = Node(CharToken('{') + CharToken('^') + CommaDelimited(WS + InnerSimpleElement) + CharToken('}'));

        public static Rule InfixOp = Node(
              CharToken('=') + Opt(CharToken('=') | CharToken('>')) // Equals and Lambda
            | CharToken('<') + Opt(CharToken('=')) // less, less eq.
            | CharToken('>') + Opt(CharToken('=')) // greater, greater eq.
            | CharToken('&') + CharToken('&') // logical and
            | CharToken('|') + Opt(CharToken('|') | CharToken('>')) // motive or, logical or, pipe
            | CharToken('~') // replicate
            | CharToken(',') // sequence
            | CharToken('-') | CharToken('+') | CharToken('*') | CharToken('/') | CharToken('^') // arithmetic
            ); 

        public static Rule PrefixOp = Node(
              CharToken('!') // not
            | CharToken('-') // unary minus
            | CharToken('@') // atom
            | CharToken('#') // residue
            );

        public static Rule ForbiddenPrefixes = Not(StringToken("let")) + Not(StringToken("in"));

        public static Rule QuotedString = Node(ForbiddenPrefixes + (DoubleQuotedString | SingleQuotedString));
        public static Rule SymbolOrBoolOrNumber = Node(ForbiddenPrefixes /*+ Opt(MatchChar('-'))*/ + OneOrMore(MatchChar(c => char.IsLetterOrDigit(c) | c == '.')));
        
        public static Rule Tuple = Node(Parenthesize(Recursive(() => Expression), left: '[', right: ']'));

        public static Rule Symbol = Node(ForbiddenPrefixes + MatchChar(c => char.IsLetter(c)) + ZeroOrMore(MatchChar(c => char.IsLetterOrDigit(c))));
        public static Rule Apply = Node(Symbol + WS + Tuple);

        public static Rule PrefixOperator = Node(PrefixOp + WS + Recursive(() => Term));
        public static Rule Term = Apply | PrefixOperator | Parenthesize(Recursive(() => Expression)) | QuotedString | SymbolOrBoolOrNumber | Tuple;
         
        public static Rule Let = Node(StringToken("let") + WS + Recursive(() => Expression) + WS + StringToken("in") + WS + Recursive(() => Expression));
        public static Rule InfixOperator = Node(Term + ZeroOrMore(WS + InfixOp + WS + Term));

        public static Rule Expression = Node(WS + (Let | InfixOperator));
                
        static QueryGrammar()
        {
            InitGrammar(typeof(QueryGrammar));
        }
    }

    /// <summary>
    /// Converts AST to a Query.
    /// </summary>
    static class QueryLanguageConverter
    {
        static Dictionary<string, int> infixOperatorPrioties = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { ".", 3000 },

            { "~", 2000 },

            { "+", 1500 },
            { "-", 1500 },
            { "*", 1501 },
            { "/", 1501 },
            { "^", 1502 },
                        
            { "=", 500 },            
            { "|>", 550 },
            { "=>", 600 },

            { "==", 1000 },
            { ">", 1000 },
            { ">=", 1000 },
            { "<", 1000 },
            { "<=", 1000 },            
            { "&&", 100 },
            { "&", 100 },
            { "||", 100 },
            { "|", 100 },            
            { ",", 5 }
        };

        static MetaQuery Apply(string symbol, params MetaQuery[] args)
        {
            return MetaQuery.CreateApply(MetaQuery.CreateSymbol(symbol), MetaQuery.CreateTuple(args));
        }

        static MetaQuery Apply(string symbol, params Node[] args)
        {
            return MetaQuery.CreateApply(MetaQuery.CreateSymbol(symbol), MetaQuery.CreateTuple(args.Select(n => ToMetaQuery(n)).ToArray()));
        }

        static MetaQuery SplitSequence(IList<Node> xs)
        {
            int start = 0;
            var len = xs.Count;
            List<MetaQuery> elements = new List<MetaQuery>(len);
            for (int i = 1; i < len; i++)
            {
                var x = xs[i];
                if (x.Label == "InfixOp" && x.Text.Trim() == ",")
                {
                    if (i - start == 1) elements.Add(ToMetaQuery(xs[start]));
                    else elements.Add(SplitInfixNode(xs.Skip(start).Take(i - start).ToArray()));
                    start = i + 1;
                }
            }
            if (len - start == 1) elements.Add(ToMetaQuery(xs[start]));
            else elements.Add(SplitInfixNode(xs.Skip(start).ToArray()));
            return Apply(SymbolTable.SequenceSymbol.Name, elements.ToArray());
        }
        
        static MetaQuery SplitInfixNode(IList<Node> xs)
        {
            if (xs.Count == 1) return ToMetaQuery(xs[0]);

            var len = xs.Count;
            for (int i = 0; i < len; i++)
            {
                var x = xs[i];
                if (x.Label == "InfixOp" && x.Text.Trim() == ",")
                {
                    return SplitSequence(xs);
                }
            }

            var splitElements = xs.MinBy(x => x.Label == "InfixOp" ? infixOperatorPrioties[x.Text.Trim()] : int.MaxValue);

            var splitElement = splitElements[0];

            // left associativity...
            if (splitElement.Text.StartsWith("|>", StringComparison.Ordinal)
                || splitElement.Text.StartsWith("-", StringComparison.Ordinal)
                || splitElement.Text.StartsWith(".", StringComparison.Ordinal))
            {
                splitElement = splitElements[splitElements.Count - 1];
            }

            var splitIndex = xs.IndexOf(splitElement);

            var op = splitElement.Text.Trim();
            switch (op)
            {
                case ".":
                    {
                        var left = SplitInfixNode(xs.Take(splitIndex).ToArray());
                        var right = xs.Skip(splitIndex + 1).First();

                        if (right.Label != "Apply")
                        {
                            throw new InvalidOperationException("Invalid member notation (.).");
                        }

                        return MetaQuery.CreateApply(ToMetaQuery(right[0]), MetaQuery.CreateTuple(new[] { left }.Concat((ToMetaQuery(right[1]) as MetaTuple).Elements).ToArray()));
                    }
                case "~":
                    return Apply(SymbolTable.RepeatSymbol.Name, SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "=":
                    return MetaAssign.Create(SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "==":
                    return Apply(RelationOp.Equal.ToString(), SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "<":
                    return Apply(RelationOp.Less.ToString(), SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "<=":
                    return Apply(RelationOp.LessEqual.ToString(), SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case ">":
                    return Apply(RelationOp.Greater.ToString(), SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case ">=":
                    return Apply(RelationOp.GreaterEqual.ToString(), SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "&":
                case "&&":
                    return Apply(LogicalOp.LogicalAnd.ToString(), SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "||":
                    return Apply(LogicalOp.LogicalOr.ToString(), SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "|":
                    return Apply("Or", SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "+":
                    return Apply("Plus", SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "-":
                    return Apply("Subtract", SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "*":
                    return Apply("Times", SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "/":
                    return Apply("Divide", SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "^":
                    return Apply("Power", SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "=>":
                    return MetaQuery.CreateLambda(SplitInfixNode(xs.Take(splitIndex).ToArray()), SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()));
                case "|>": // pipe
                    return MetaQuery.CreateApply(SplitInfixNode(xs.Skip(splitIndex + 1).ToArray()), MetaTuple.Create(SplitInfixNode(xs.Take(splitIndex).ToArray()).ToSingletonArray()));
                default:
                    throw new InvalidOperationException();
            }
        }

        static MetaQuery PrefixNode(Node n)
        {
            switch (n.Nodes[0].Text.Trim())
            {
                case "!": return Apply(LogicalOp.LogicalNot.ToString(), ToMetaQuery(n.Nodes[1]));
                case "-": return Apply("Minus", ToMetaQuery(n.Nodes[1]));
                case "@": return Apply("Atoms", ToMetaQuery(n.Nodes[1]));
                case "#": return Apply("Residues", ToMetaQuery(n.Nodes[1]));
                default:
                    throw new InvalidOperationException();
            }
        }
                    
        //static Regex floatRegex = new Regex(@"^[-]?[0-9]+\.[0-9]+$");
        //static Regex intRegex = new Regex(@"^[-]?[0-9]+$");
        static Regex floatRegex = new Regex(@"^[0-9]+\.[0-9]+$");
        static Regex intRegex = new Regex(@"^[0-9]+$");

        static MetaQuery ToMetaQuery(Node n)
        {
            switch (n.Label)
            {
                case "SymbolOrBoolOrNumber":
                    if (n.Text.Equals("true", StringComparison.OrdinalIgnoreCase)) return MetaQuery.CreateValue(true);
                    if (n.Text.Equals("false", StringComparison.OrdinalIgnoreCase)) return MetaQuery.CreateValue(false);
                    if (intRegex.IsMatch(n.Text)) return MetaQuery.CreateValue(int.Parse(n.Text, CultureInfo.InvariantCulture));
                    if (floatRegex.IsMatch(n.Text)) return MetaQuery.CreateValue(double.Parse(n.Text, CultureInfo.InvariantCulture));
                    return MetaQuery.CreateSymbol(n.Text);
                case "Symbol": return MetaQuery.CreateSymbol(n.Text);
                case "QuotedString": return MetaQuery.CreateValue(n.Text.Substring(1, n.Length - 2));
                case "Tuple": return MetaQuery.CreateTuple(n.Nodes.Select(x => ToMetaQuery(x)).ToArray());
                case "InfixOperator": return SplitInfixNode(n.Nodes);
                case "PrefixOperator": return PrefixNode(n);
                case "Let": return MetaQuery.CreateLet(ToMetaQuery(n[0]), ToMetaQuery(n[1]));
                case "Lambda": return MetaQuery.CreateLambda(MetaQuery.CreateTuple(n[0].Nodes.Select(t => MetaQuery.CreateSymbol(t.Text)).ToArray()), ToMetaQuery(n[1]));
                case "Expression": return ToMetaQuery(n[0]);
                case "Term": return ToMetaQuery(n.Nodes[0]);
                case "Apply": return MetaQuery.CreateApply(ToMetaQuery(n[0]), ToMetaQuery(n[1]));
                default:
                    throw new ArgumentException("Could not parse element: " + n.Text);
            }
        }

        public static MetaQuery ParseMeta(string input)
        {
            Node root;

            try
            {
                root = Parser.Parse(QueryGrammar.Expression, input);
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Incomplete or incorrect query ({0}).", e.Message));
            }

            return ToMetaQuery(root);
        }
    }
}
