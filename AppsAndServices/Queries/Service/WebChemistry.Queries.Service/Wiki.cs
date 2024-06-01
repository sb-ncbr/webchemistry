namespace WebChemistry.Queries.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WebChemistry.Queries.Core.Symbols;

    class QueriesWikiReference
    {
        static void FormatDescriptor(TextWriter text, SymbolDescriptor symbol)
        {
            text.WriteLine("=== {0}{1} ===", symbol.Name,
                symbol.Description.OperatorForm != null
                ? " (" + symbol.Description.OperatorForm.Replace(">", "&gt;").Replace("<", "&lt;").Replace(":", "&#58;") + ")"
                : "");

            if (symbol.Description.Category != SymbolCategories.ElementaryTypes)
            {
                text.WriteLine("<code>{0}{1} -&gt; {2}</code><br/>", symbol.Name, symbol.Arguments.Length > 0
                    ? "(" + string.Join(", ", symbol.Arguments.Select(a => a.Name + "&#58; " + a.Type.ToString())) + ")"
                    : "()", symbol.Type);
                text.WriteLine("''{0}{1}''<br/>", symbol.Description.Description, symbol.Description.IsInternal ? " (internal)" : "");

                if (symbol.Description.Category == SymbolCategories.MiscFunctions)
                {
                    text.WriteLine("<small>''Note:'' This function cannot be used directly to query patterns from MotiveExplorer or Queries service.</small><br/>");
                }

                if (symbol.Arguments.Length > 0) text.WriteLine(";Arguments");
                foreach (var arg in symbol.Arguments)
                {
                    text.WriteLine(": {0}&#58; {1} - ''{2}'' ", arg.Name, arg.Type, arg.Description);
                    //text.WriteLine(":: ''{0}''", arg.Description);
                }
                if (symbol.Options.Count > 0) text.WriteLine(";Options");
                foreach (var opt in symbol.Options.Values.OrderBy(o => o.Name))
                {
                    text.WriteLine(": {0}&#58; {1} = {2} - ''{3}'' ", opt.Name, opt.Type, opt.DefaultValue, opt.Description);
                    //text.WriteLine(":: ''{0}''", arg.Description);
                }
            }
            //text.WriteLine(";Return type");
            //text.WriteLine(": {0}", symbol.Type);
            if (symbol.Description.Examples.Length > 0)
            {
                text.WriteLine(";Examples");
                foreach (var ex in symbol.Description.Examples)
                {
                    text.WriteLine(": <code>{0}</code>", ex.ExampleCode);
                    text.WriteLine(":: ''{0}''", ex.ExampleDescription);
                }
            }
        }

        public static void Create(string introFilename, string outputFilename)
        {
            var text = new StringWriter();

            foreach (var group in SymbolTable.AllSymbols.GroupBy(s => s.Description.Category).Where(g => !g.Key.ExcludeFromHelp).OrderBy(g => g.Key.Index))
            {
                text.WriteLine("== {0} ==", group.Key.Name);
                text.WriteLine("''{0}''", group.Key.Description);

                var ordered = group.OrderBy(s => s.Name).ToArray();
                var index = 0;
                foreach (var symbol in ordered)
                {
                    FormatDescriptor(text, symbol);
                    index++;
                    if (index != ordered.Length) text.WriteLine("----");
                }
                text.WriteLine("<br/>", group.Key.Name);
            }

            File.WriteAllText(outputFilename, File.ReadAllText(introFilename) + text.ToString());
        }
    }
}
