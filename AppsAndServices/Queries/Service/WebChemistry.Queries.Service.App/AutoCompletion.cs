
namespace WebChemistry.Queries.Service.App
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.Symbols;

    public static class QueriesAutoCompletion
    {
        static string script;

        class Snippet
        {
            public string content { get; set; }
            public string name { get; set; }
            public string tabTrigger { get; set; }
        }

        class Example
        {
            public string Description { get; set; }
            public string Query { get; set; }
        }

        class SymbolDescription
        {
            public string Name { get; set; }
            public string Tooltip { get; set; }
            public Snippet Snippet { get; set; }
            public Example[] Examples { get; set; }
        }

        static string GetArgString(FunctionArgument arg, int index)
        {
            var type = arg.Type;
            if (type == BasicTypes.String) return string.Format("\"${{{0}}}\"", index);
            if (type is TypeArrow) return string.Format("lambda ${{{0}}}: ", index);
            var many = type as TypeMany;
            if (many != null && many.Inner == BasicTypes.String) return string.Format("\"${{{0}}}\"", index);
            return string.Format("${{{0}}}", index);
        }

        static SymbolDescription FormatDescriptor(SymbolDescriptor symbol)
        {
            var tooltip = new StringWriter();
            var snippet = new StringWriter();

            //var exampleSnippets = new List<Snippet>();

            tooltip.Write("<div class='mq-auto-tooltip'>");
            ////tooltip.Write("<h5>{0}{1}</h5>", symbol.Name,
            ////    symbol.Description.OperatorForm != null
            ////    ? " (" + symbol.Description.OperatorForm.Replace(">", "&gt;").Replace("<", "&lt;") + ")"
            ////    : "");

            tooltip.Write("<div class='mq-auto-header'>{0}{1} -&gt; {2}</div>", symbol.Name, symbol.Arguments.Length > 0
                ? "(" + string.Join(", ", symbol.Arguments.Select(a => a.Name + "&#58; " + a.Type.ToString())) + ")"
                : "()", symbol.Type);
            tooltip.Write("<i>{0}{1}</i><br/>", symbol.Description.Description, symbol.Description.IsInternal ? " (internal)" : "");

            //if (symbol.Description.Category == SymbolCategories.MiscFunctions)
            //{
            //    tooltip.Write("<small><i>Note:</i> This function might not be directly used to query patterns from MotiveExplorer or Queries service.</small><br/>");
            //}

            if (symbol.Arguments.Length > 0)
            {
                tooltip.Write("<b>Arguments</b><ul>");
                foreach (var arg in symbol.Arguments)
                {
                    tooltip.Write("<li>{0}: {1} - <i>{2}</i></li>", arg.Name, arg.Type, arg.Description);
                }
                tooltip.Write("</ul>");
            }

            if (symbol.Options.Count > 0)
            {
                tooltip.Write("<b>Options</b><ul>");
                foreach (var opt in symbol.Options.Values.OrderBy(o => o.Name))
                {
                    tooltip.Write("<li>{0}: {1} = {2} - <i>{3}</i></li>", opt.Name, opt.Type, opt.DefaultValue, opt.Description);
                }
                tooltip.Write("</ul>");
            }

            if (symbol.Description.Examples.Length > 0)
            {
                tooltip.Write("<b>Examples</b><dl>");
                foreach (var ex in symbol.Description.Examples)
                {
                    tooltip.Write("<dt><code>{0}</code></dt>", ex.ExampleCode);
                    tooltip.Write("<dd>{0}</dd>", ex.ExampleDescription);
                }
                tooltip.Write("</dl>");
            }
            tooltip.Write("</div>");


            if (symbol.Description.IsDotSyntax)
            {
                snippet.Write(symbol.Name);
                var args = symbol.Arguments.Skip(1).ToArray();
                if (args.Length == 0) snippet.Write("()${1}");
                else 
                {
                    snippet.Write('(');
                    snippet.Write(args.Select((a, i) => GetArgString(a, i + 1)).JoinBy());
                    snippet.Write(')');
                }
            }
            else
            {
                snippet.Write(symbol.Name);
                if (symbol.Arguments.Length == 0) snippet.Write("()${1}");
                else
                {
                    snippet.Write('(');
                    snippet.Write(symbol.Arguments.Select((a, i) => GetArgString(a, i + 1)).JoinBy());
                    snippet.Write(')');
                }
            }

            return new SymbolDescription
            {
                Name = symbol.Name,
                Tooltip = tooltip.ToString(),
                Snippet = new Snippet
                {
                    name = symbol.Name,
                    tabTrigger = symbol.Name,
                    content = snippet.ToString()
                },
                Examples = symbol.Description.Examples.Select(e => new Example
                {
                    Description = e.ExampleDescription,
                    Query = e.ExampleCode
                }).ToArray()
            };
        }


        static void MakeScript()
        {
            if (script != null) return;

            var symbols = SymbolTable.AllSymbols
                .Where(s => s.Description.Category != SymbolCategories.ElementaryTypes
                    && s.Description.Category != SymbolCategories.LanguagePrimitives 
                    && !s.Description.IsInternal
                    && !s.Description.IgnoreForAutoCompletion)
                .Select(FormatDescriptor)
                .ToDictionary(s => s.Name);

            script = "var QueriesAutoCompletion = " + symbols.ToJsonString() + ";";
        }

        public static string GetScript()
        {
            MakeScript();
            return script;
        }
    }
}
