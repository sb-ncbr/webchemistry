namespace WebChemistry.Platform.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using WebChemistry.Framework.Core;
    
    /// <summary>
    /// Config for help.
    /// </summary>
    public class ServiceHelpConfig
    {
        public string AppName { get; set; }
        public string ExecutableName { get; set; }
        public AppVersion Version { get; set; }
        public object Example { get; set; }
        public string[] RunningRemarks { get; set; }

        public HelpOutputStructure OutputStructure { get; set; }
    }

    public class HelpOutputStructure
    {
        public string Remark { get; set; }
        public HelpOutputStructureDescription StandardStructure { get; set; }
        public HelpOutputStructureDescription AppSpecificStructure { get; set; }
        public Dictionary<string, HelpOutputSpecificFileDescription> SpecificFileStructure { get; private set; }

        public HelpOutputStructure AddSpecificFileStructure(HelpOutputSpecificFileDescription desc)
        {
            SpecificFileStructure[desc.Name] = desc;
            return this;
        }

        public HelpOutputStructure()
        {
            Remark = "";
            SpecificFileStructure = new Dictionary<string, HelpOutputSpecificFileDescription>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public abstract class HelpOutputSpecificFileDescription
    {
        /// <summary>
        /// Name of the file.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Generic description of the file.
        /// </summary>
        public string Description { get; private set; }

        class _Csv : HelpOutputSpecificFileDescription
        {
            public ListExporter Exporter { get; set; }

            protected override void FormatToWikiInternal(StringBuilder text, HelpOutputStructure config)
            {
                text.AppendLine("The CSV file contains these columns: ");

                foreach (var c in Exporter.Columns)
                {
                    text.AppendLine(string.Format("* '''{0}''' [ {1} ]", c.HeaderString, c.ColumnType.ToString()));
                    text.AppendLine(string.Format(": ''{0}''", c.Description.WikiEncode()));
                }
            }

            protected override void FormatToConsoleInternal(StringBuilder text, HelpOutputStructure config)
            {
                text.AppendLine("The CSV file contains these columns: ");

                foreach (var c in Exporter.Columns)
                {
                    text.AppendLine(string.Format("> {0} [ {1} ]", c.HeaderString, c.ColumnType.ToString()));
                    text.AppendLine(string.Format("{0}", ServiceHelpGenerator.MakeLines(c.Description, 2)));
                }
            } 
        }

        class _Json : HelpOutputSpecificFileDescription
        {
            public Type Type { get; set; }
        }

        class _Generic : HelpOutputSpecificFileDescription
        {
        }

        protected virtual void FormatToWikiInternal(StringBuilder text, HelpOutputStructure config)
        {

        }

        protected virtual void FormatToConsoleInternal(StringBuilder text, HelpOutputStructure config)
        {

        }

        public static HelpOutputSpecificFileDescription Csv(string name, string description, ListExporter exporter)
        {
            return new _Csv { Name = name, Description = description, Exporter = exporter };
        }

        public static HelpOutputSpecificFileDescription Generic(string name, string description)
        {
            return new _Generic { Name = name, Description = description };
        }
        
        public string FormatToWiki(HelpOutputStructure config)
        {
            var text = new StringBuilder();
            text.AppendLine(string.Format("==== <span id='{0}_description'>{0}</span> ====", Name));
            text.AppendLine(Description.WikiEncode());
            text.AppendLine();
            FormatToWikiInternal(text, config);
            return text.ToString();
        }

        public string FormatToConsole(HelpOutputStructure config)
        {
            var text = new StringBuilder();
            text.AppendLine(string.Format("== {0} ==", Name));
            text.AppendLine(ServiceHelpGenerator.MakeLines(Description, 0));
            text.AppendLine();
            FormatToConsoleInternal(text, config);
            return text.ToString();
        }
    }

    public abstract class HelpOutputStructureDescription
    {
        /// <summary>
        /// Name of the entry.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Description of the entry.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Children of the element.
        /// </summary>
        public HelpOutputStructureDescription[] Children { get; private set; }

        protected HelpOutputStructureDescription()
        {
            Children = new HelpOutputStructureDescription[0];
        }

        class _Folder : HelpOutputStructureDescription
        {
        }
        class _File : HelpOutputStructureDescription
        {
        }

        protected void FormatToWikiInternal(StringBuilder text, int level, HelpOutputStructure config)
        {
            if (this is _File)
            {
                if (config.SpecificFileStructure.ContainsKey(Name))
                {
                    text.AppendLine(string.Format("{0}: [[#{1}_description | {1}]] - ''{2}''", new string(':', level), Name, Description));
                }
                else
                {
                    text.AppendLine(string.Format("{0}: '''{1}''' - ''{2}''", new string(':', level), Name, Description));
                }
            }
            else
            {
                text.AppendLine(string.Format("{0}* '''{1}''' - ''{2}''", new string(':', level), Name, Description));
            }
            foreach (var c in Children) c.FormatToWikiInternal(text, level + 1, config);
        }

        protected void FormatToConsoleInternal(StringBuilder text, int level, HelpOutputStructure config)
        {
            text.AppendLine(string.Format("{0}* {1}", new string(' ', 2 * level), Name));
            text.AppendLine(ServiceHelpGenerator.MakeLines(Description, 2 * level + 2));
            foreach (var c in Children) c.FormatToConsoleInternal(text, level + 1, config);
        }

        public string FormatToWiki(HelpOutputStructure config)
        {
            var text = new StringBuilder();
            FormatToWikiInternal(text, 0, config);
            return text.ToString();
        }

        public string FormatToConsole(HelpOutputStructure config)
        {
            var text = new StringBuilder();
            FormatToConsoleInternal(text, 0, config);
            return text.ToString();
        }

        public static HelpOutputStructureDescription Folder(string name, string desctiption, params HelpOutputStructureDescription[] children)
        {
            return new _Folder { Name = name, Description = desctiption, Children = children };
        }

        public static HelpOutputStructureDescription File(string name, string desctiption, params HelpOutputStructureDescription[] children)
        {
            return new _File { Name = name, Description = desctiption, Children = children };
        }
    }

    /// <summary>
    /// Determines that the properties should be nested.
    /// </summary>
    public class HelpNestPropertiesAttribute : Attribute
    {

    }

    /// <summary>
    /// Determines that the type or property should be described.
    /// </summary>
    public class HelpDescribeAttribute : Attribute
    {
        public Type Subtype { get; private set; }

        /// <summary>
        /// Subtype is used for arrays etc.
        /// </summary>
        /// <param name="subtype"></param>
        public HelpDescribeAttribute(Type subtype = null)
        {
            Subtype = subtype;
        }
    }

    /// <summary>
    /// Special typename for help.
    /// </summary>
    public class HelpTypeNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public HelpTypeNameAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Generates the help for service input and output data.
    /// </summary>
    public static class ServiceHelpGenerator
    {
        enum Formatters
        {
            Text,
            Header,
            AttributeHeader,
            AttributeDescription,
            TypeHeader,
            FieldHeader,
            Code,
            OutputStructureDescription,
            OutputSpecificFileDescription
        }

        #region Wiki Formatters
        static Dictionary<Formatters, dynamic> WikiFormatters = new Dictionary<Formatters, dynamic>
        {
            { Formatters.Text, new Func<string, string>(x => x.WikiEncode()) },
            { Formatters.OutputStructureDescription, new Func<HelpOutputStructureDescription, HelpOutputStructure, string>((x, c) => x.FormatToWiki(c).WikiEncode()) },
            { Formatters.OutputSpecificFileDescription, new Func<HelpOutputSpecificFileDescription, HelpOutputStructure, string>((x, c) => x.FormatToWiki(c)) },
            { Formatters.Header, new Func<string, int, string>(WikiHeaderFormatter) },
            { Formatters.AttributeHeader, new Func<string, PropertyInfo, bool, object, int, string>(WikiAttributeHeaderFormatter) },
            { Formatters.AttributeDescription, new Func<string, int, string>(WikiAttributeDescriptionFormatter) },
            { Formatters.TypeHeader, new Func<Type, string>(WikiTypeHeaderFormatter) },
            { Formatters.FieldHeader, new Func<string, string>(WikiFieldHeaderFormatter) },
            { Formatters.Code, new Func<string, string>(WikiCodeFormatter) },
        };
                
        internal static string WikiEncode(this string str)
        {
            return str.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        static string WikiCodeFormatter(string text)
        {
            return "<pre>" + text + "</pre>";
        }

        static string WikiHeaderFormatter(string text, int level)
        {
            level += 1;
            return new string('=', level) + " " + text.WikiEncode() + " " + new string('=', level);
        }

        static string WikiTypeHeaderFormatter(Type type)
        {
            return string.Format("===== <span id='{0}_details'>{0}</span> =====", GetTypeName(type).WikiEncode());
        }

        static string WikiAttributeHeaderFormatter(string text, PropertyInfo property, bool hasDefaultValue, object defaultValue, int level)
        {
            string typeLink;
            if (ShouldLink(property))
            {
                typeLink = string.Format("[ [[#{0}_details|{0}]] ]", GetTypeName(property).WikiEncode());
            }
            else
            {
                typeLink = string.Format("[ {0} ]", GetTypeName(property));
            }

            if (!hasDefaultValue)
            {
                return string.Format("{0} '''{1}''' {2}", new string('*', level), text.WikiEncode(), typeLink);
            }

            return string.Format("{0} '''{1}''' {2}, Default value = {3}", new string('*', level), text.WikiEncode(), typeLink, defaultValue.ToJsonString());
        }

        static string WikiAttributeDescriptionFormatter(string text, int level)
        {
            return string.Format("{0} ''{1}''", new string(':', level), text.WikiEncode());
        }

        static string WikiFieldHeaderFormatter(string text)
        {
            return string.Format("* '''{0}'''", text.WikiEncode());
        }
        #endregion

        #region Console Formatters
        static Dictionary<Formatters, dynamic> ConsoleFormatters = new Dictionary<Formatters, dynamic>
        {
            { Formatters.Text, new Func<string, string>(x => MakeLines(x, 0)) },
            { Formatters.OutputStructureDescription, new Func<HelpOutputStructureDescription, HelpOutputStructure, string>((x, c) => x.FormatToConsole(c)) },
            { Formatters.OutputSpecificFileDescription, new Func<HelpOutputSpecificFileDescription, HelpOutputStructure, string>((x, c) => x.FormatToConsole(c)) },
            { Formatters.Header, new Func<string, int, string>(ConsoleHeaderFormatter) },
            { Formatters.AttributeHeader, new Func<string, PropertyInfo, bool, object, int, string>(ConsoleAttributeHeaderFormatter) },
            { Formatters.AttributeDescription, new Func<string, int, string>(ConsoleAttributeDescriptionFormatter) },
            { Formatters.TypeHeader, new Func<Type, string>(ConsoleTypeHeaderFormatter) },
            { Formatters.FieldHeader, new Func<string, string>(ConsoleFieldHeaderFormatter) },
            { Formatters.Code, new Func<string, string>(ConsoleCodeFormatter) },
        };

        public static string MakeLines(string str, int offset)
        {
            StringBuilder text = new StringBuilder();
            int maxLen = 79;
            Action<string> append = null;
            append = s =>
                {
                    if (s.Length <= maxLen - offset)
                    {
                        text.Append(new string(' ', offset) + s);
                        return;
                    }
                    var index = s.LastIndexOf(' ', maxLen - offset);
                    if (index < 0)
                    {
                        text.Append(new string(' ', offset) + s);
                        return;
                    }
                    text.AppendLine(new string(' ', offset) + s.Substring(0, index));
                    append(s.Substring(index + 1));
                };
            append(str);
            return text.ToString();
        }

        static string ConsoleCodeFormatter(string text)
        {
            return text;
        }

        static string ConsoleHeaderFormatter(string text, int level)
        {
            return new string('=', level) + " " + text + " " + new string('=', level) + Environment.NewLine + new string('-', text.Length + 2 * level + 2);
        }

        static string ConsoleTypeHeaderFormatter(Type type)
        {
            return string.Format("<{0}>", GetTypeName(type));
        }

        static string ConsoleAttributeHeaderFormatter(string text, PropertyInfo property, bool hasDefaultValue, object defaultValue, int level)
        {
            level -= 1;
            string typeLink =string.Format("[ {0} ]", GetTypeName(property));

            if (!hasDefaultValue)
            {
                return string.Format("{0}{1} {2}", new string(' ', 2 * level), text, typeLink);
            }
            return string.Format("{0}{1} {2}, Default value = {3}", new string(' ', 2 * level), text, typeLink, defaultValue.ToJsonString());
        }

        static string ConsoleAttributeDescriptionFormatter(string text, int level)
        {
            level -= 1;
            return MakeLines(text, 2 * level + 4);
        }

        static string ConsoleFieldHeaderFormatter(string text)
        {
            return string.Format("  > {0}", text);
        }
        #endregion


        static string GetTypeName(PropertyInfo p)
        {
            var nameAttribute = p.GetCustomAttributes(typeof(HelpTypeNameAttribute), false).FirstOrDefault() as HelpTypeNameAttribute;
            if (nameAttribute != null) return nameAttribute.Name;
            return GetTypeName(p.PropertyType);
        }

        static string GetTypeName(Type t)
        {
            var nameAttribute = t.GetCustomAttributes(typeof(HelpTypeNameAttribute), false).FirstOrDefault() as HelpTypeNameAttribute;
            if (nameAttribute != null) return nameAttribute.Name;
            return t.Name;
        }

        static string GetDescribeTypeName(PropertyInfo p)
        {
            return null;
        }

        static bool ShouldNest(PropertyInfo p)
        {
            var nestable = p.GetCustomAttributes(typeof(HelpNestPropertiesAttribute), false).FirstOrDefault();
            return nestable != null;
        }

        static bool ShouldLink(PropertyInfo p)
        {
            var linkable = p.GetCustomAttributes(typeof(HelpDescribeAttribute), false).FirstOrDefault();
            return linkable != null;
        }

        static bool ShouldLink(Type t)
        {
            var linkable = t.GetCustomAttributes(typeof(HelpDescribeAttribute), false).FirstOrDefault();
            return linkable != null;
        }

        static string[] MakeAttributes(PropertyInfo[] properties, Dictionary<Formatters, dynamic> formatters, int level)
        {
            List<string> ret = new List<string>();

            foreach (var p in properties)
            {
                var description = p.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
                var defaultValue = p.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                ret.Add(formatters[Formatters.AttributeHeader](p.Name, p, defaultValue != null, defaultValue != null ? defaultValue.Value : null, level));
                if (description != null)
                {
                    ret.Add(formatters[Formatters.AttributeDescription](description.Description, level));
                }
                if (ShouldNest(p))
                {
                    ret.AddRange(MakeAttributes(p.PropertyType.GetProperties(), formatters, level + 1));
                }
                ret.Add("");
            }

            return ret.ToArray();
        }
        
        static string DescribeType(Type t, Dictionary<Formatters, dynamic> formatters)
        {
            List<string> ret = new List<string>();
            ret.Add(formatters[Formatters.TypeHeader](t));
            
            if (t.IsEnum)
            {
                foreach (var f in t.GetFields().Where(f => f.FieldType == t))
                {
                    ret.Add(formatters[Formatters.FieldHeader](f.Name));
                    var description = f.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
                    if (description != null) ret.Add(formatters[Formatters.AttributeDescription](description.Description, 1));
                    ret.Add("");
                }
            }
            else
            {
                foreach (var p in t.GetProperties())
                {
                    var description = p.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
                    var defaultValue = p.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                    ret.Add(formatters[Formatters.AttributeHeader](p.Name, p, defaultValue != null, defaultValue != null ? defaultValue.Value : null, 1));
                    if (description != null)
                    {
                        ret.Add(formatters[Formatters.AttributeDescription](description.Description, 1));
                    }
                    if (p.PropertyType.IsEnum)
                    {
                        ret.Add(formatters[Formatters.AttributeDescription]("Available values are: " + Enum.GetNames(p.PropertyType).JoinBy() + ".", 1));
                    }
                    ret.Add("");
                }
            }

            return string.Join(Environment.NewLine, ret);
        }

        static void VisitPropertyTypes(Type t, HashSet<Type> visitedTypes)
        {
            if (visitedTypes.Contains(t)) return;
            visitedTypes.Add(t);
            foreach (var p in t.GetProperties())
            {
                var describe = p.GetCustomAttributes(typeof(HelpDescribeAttribute), false).FirstOrDefault() as HelpDescribeAttribute;
                if (describe != null && describe.Subtype != null) VisitPropertyTypes(describe.Subtype, visitedTypes);
                VisitPropertyTypes(p.PropertyType, visitedTypes);
            }
        }

        static string GenerateConfigHelp<TConfig>(ServiceHelpConfig config, Dictionary<Formatters, dynamic> formatters)
        {
            var type = typeof(TConfig);

            HashSet<Type> visitedTypes = new HashSet<Type>();
            VisitPropertyTypes(type, visitedTypes);
            var descriptionTypes = visitedTypes.Where(t => ShouldLink(t)).OrderBy(t => t.Name).ToArray();

            Func<string, string> text = t => formatters[Formatters.Text](t);

            var runningRemarks = config.RunningRemarks ?? new string[0];

            var lines = new string[][]
            {
                new string[] 
                { 
                    text(string.Format("This is help for version {0} or newer. The help text for other versions can be viewed using the --help command when running the application.", config.Version)),
                    
                    formatters[Formatters.Header]("Running the Service", 1),
                    text("The service can be executed using the command (latest .NET Framework required): "),
                    formatters[Formatters.Code](string.Format("{0} workingFolder configuration.json", config.ExecutableName)),
                    text("In Linux (where available) and MacOS, the latest version the Mono Framework (http://mono-project.com/) must be used to run the application:"),
                    formatters[Formatters.Code](string.Format("mono {0} workingFolder configuration.json", config.ExecutableName)),
                    text("Alternatively, on Linux and MacOS, an official version of .NET Framework should become available during 2015."),
                    text(string.Join(Environment.NewLine, runningRemarks)),
                    "",
                    formatters[Formatters.Header]("Configuration", 1),
                    text("The configuration is specified using the JSON format."),
                    "",
                    formatters[Formatters.Header]("Configuration Example", 2),
                    text("This is the general shape of the JSON input configuration."),
                    formatters[Formatters.Code](config.Example.ToJsonString(true)),
                    "",
                    text("There has to be exactly one configuration file for each validation run. Every value, as well as settings' names, is surrounded in quotation marks (\" \" or ' '). " +
                        "Backslashes (\\) have to be escaped (\\\\). File system paths can be absolute as well as relative (/ works as well in paths)."),
                    "",
                    formatters[Formatters.Header]("Attributes", 2),
                },

                MakeAttributes(type.GetProperties(), formatters, 1),

                new string[]
                {
                    formatters[Formatters.Header]("Descriptions", 2),
                },

                descriptionTypes.SelectMany(t => new [] { DescribeType(t, formatters), "" }).ToArray(),

                new string[]
                {
                    formatters[Formatters.Header]("Output Description", 1),
                    text(config.OutputStructure.Remark),
                    formatters[Formatters.Header]("General Structure", 2),
                    formatters[Formatters.OutputStructureDescription](config.OutputStructure.StandardStructure, config.OutputStructure),
                    formatters[Formatters.Header](string.Format("{0} Specific Structure", config.AppName), 2),
                    formatters[Formatters.OutputStructureDescription](config.OutputStructure.AppSpecificStructure, config.OutputStructure),
                    formatters[Formatters.Header]("Specific File Descriptions", 2),
                    text("This section contains detailed descriptions of selected specific files.")
                },

                config.OutputStructure.SpecificFileStructure
                    .OrderBy(p => p.Key)
                    .Select(p => (string)formatters[Formatters.OutputSpecificFileDescription](p.Value, config.OutputStructure))
                    .ToArray()
            };

            return string.Join(Environment.NewLine, lines.SelectMany(xs => xs));
        }

        /// <summary>
        /// Generates the wiki help.
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="serviceName"></param>
        /// <param name="example"></param>
        /// <returns></returns>
        public static string GenerateWikiHelp<TConfig>(ServiceHelpConfig config)
        {
            return GenerateConfigHelp<TConfig>(config, WikiFormatters);
        }

        /// <summary>
        /// Generates the help for console output.
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GenerateHelp<TConfig>(ServiceHelpConfig config)
        {
            return GenerateConfigHelp<TConfig>(config, ConsoleFormatters);
        }
    }
}
