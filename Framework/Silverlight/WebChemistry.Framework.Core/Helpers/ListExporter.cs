namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Xml.Linq;

    public static class ListExporterExtensions
    {
        /// <summary>
        /// Exporter extension method for all IEnumerableOfT
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <param name="xmlRootName"></param>
        /// <param name="xmlElementName"></param>
        /// <returns></returns>
        public static ListExporter<T> GetExporter<T>(
            this IEnumerable<T> source, string separator = ",", string xmlRootName = "Entries", string xmlElementName = "Entry") where T : class
        {
            return new ListExporter<T>(source, separator, xmlRootName, xmlElementName);
        }

        /// <summary>
        /// Converts a list of elements to a CSV string. Automatically add all properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ToCsvString<T>(this IEnumerable<T> source, string separator = ",") where T : class
        {
            return GetExporter(source, separator).AddPropertyColumns().ToCsvString();
        }
    }

    /// <summary>
    /// Column type
    /// </summary>
    public enum ColumnType
    {
        /// <summary>
        /// The value is enclosed in " "
        /// </summary>
        String = 0,
        /// <summary>
        /// Number
        /// </summary>
        Number
    }

    /// <summary>
    /// Base information about columns.
    /// </summary>
    public abstract class ExportableColumn
    {
        /// <summary>
        /// Column header.
        /// </summary>
        public string HeaderString { get; protected set; }

        /// <summary>
        /// Column description.
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// Type of the column.
        /// </summary>
        public ColumnType ColumnType { get; protected set; }
    }

    /// <summary>
    /// Represents custom exportable column with a expression for the property name
    /// and a custom format string
    /// </summary>
    public class ExportableColumn<T> : ExportableColumn
    {
        public Func<T, Object> Func { get; private set; }

        public ExportableColumn(Func<T, Object> func, ColumnType type, string headerString, string description = "")
        {
            this.Func = func;
            this.HeaderString = headerString;
            this.ColumnType = type;
            this.Description = description;
        }
    }
    
    public abstract class ListExporter
    {
        public abstract List<Dictionary<string, object>> ToDictionaryList();
        public abstract void WriteCsvString(TextWriter writer);
        public abstract string ToCsvString();
        public abstract XElement ToXml();

        public abstract IEnumerable<ExportableColumn> Columns { get; }
    }

    /// <summary>
    /// Exporter that uses Expression tree parsing to work out what values to export for 
    /// columns, and will use additional data as specified in the List of ExportableColumn
    /// which defines whethere to use custom headers, or formatted output
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListExporter<T> : ListExporter where T : class
    {
        private List<ExportableColumn<T>> columns = new List<ExportableColumn<T>>();
        private IEnumerable<T> sourceList;
        private string separator;
        private string xmlRootName, xmlElementName;

        /// <summary>
        /// Get the columns
        /// </summary>
        public override IEnumerable<ExportableColumn> Columns { get { return columns.AsEnumerable(); } }

        /// <summary>
        /// Creates the exporter.
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="separator"></param>
        /// <param name="xmlRootName"></param>
        /// <param name="xmlElementName"></param>
        public ListExporter(IEnumerable<T> sourceList, string separator = ",", string xmlRootName = "Entries", string xmlElementName = "Entry")
        {
            this.sourceList = sourceList;
            this.separator = separator;
            this.xmlRootName = xmlRootName;
            this.xmlElementName = xmlElementName;
        }


        /// <summary>
        /// Adds a columns.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="type"></param>
        /// <param name="headerString"></param>
        /// <returns></returns>
        public ListExporter<T> AddExportableColumn(
            Func<T, Object> func,
            ColumnType type,
            string headerString,
            string desctiption = "")
        {
            columns.Add(new ExportableColumn<T>(func, type, headerString, desctiption));
            return this;
        }

        /// <summary>
        /// Adds a numeric column
        /// </summary>
        /// <param name="func"></param>
        /// <param name="headerString"></param>
        /// <returns></returns>
        public ListExporter<T> AddNumericColumn(
            Func<T, Object> func,
            string headerString,
            string desctiption = "")
        {
            return AddExportableColumn(func, ColumnType.Number, headerString, desctiption);
        }

        static Func<T, object> MakeLambda(PropertyInfo propertyInfo)
        {
            var instance = Expression.Parameter(propertyInfo.DeclaringType, "x");
            var property = Expression.Property(instance, propertyInfo);
            var convert = Expression.TypeAs(property, typeof(object));
            return (Func<T, object>)Expression.Lambda(convert, instance).Compile();
        }
        
        /// <summary>
        /// Automatically add columns for each property.
        /// </summary>
        /// <returns></returns>
        public ListExporter<T> AddPropertyColumns()
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                var type = prop.PropertyType;
                if (type == typeof(int) || type == typeof(double) || type == typeof(float) || type == typeof(long))
                {
                    AddExportableColumn(MakeLambda(prop), ColumnType.Number, prop.Name);
                }
                else
                {
                    AddExportableColumn(MakeLambda(prop), ColumnType.String, prop.Name);
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a columns for a specific property/
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ListExporter<T> AddPropertyColumn(string name, string desctiption = "")
        {
            var prop = typeof(T).GetProperty(name);
            var type = prop.PropertyType;
            if (type == typeof(int) || type == typeof(double) || type == typeof(float) || type == typeof(long))
            {
                AddExportableColumn(MakeLambda(prop), ColumnType.Number, prop.Name, desctiption);
            }
            else
            {
                AddExportableColumn(MakeLambda(prop), ColumnType.String, prop.Name, desctiption);
            }
            return this;
        }

        /// <summary>
        /// Adds a string column.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="headerString"></param>
        /// <returns></returns>
        public ListExporter<T> AddStringColumn(
            Func<T, Object> func,
            string headerString,
            string desctiption = "")
        {
            return AddExportableColumn(func, ColumnType.String, headerString, desctiption);
        }


        /// <summary>
        /// Export all specified columns as a string, 
        /// using seperator and column data provided
        /// where we may use custom or default headers 
        /// (depending on whether a custom header string was supplied)
        /// where we may use custom fomatted column data or default data 
        /// (depending on whether a custom format string was supplied)
        /// </summary>
        public override void WriteCsvString(TextWriter writer)
        {
            if (columns.Count == 0)
            {
                throw new InvalidOperationException("You need to specify at least one column to export value");
            }

            var headers = string.Join(separator, columns.Select(c => string.Format("\"{0}\"", c.HeaderString.Replace("\"", "\"\""))).ToArray());
            writer.WriteLine(headers);

            int columnCount = columns.Count;

            StringBuilder sb = new StringBuilder();
            foreach (T item in sourceList)
            {
                sb.Clear();
                for (int i = 0; i < columns.Count; i++)
                {                   
                    var column = columns[i];
                    var value = column.Func(item);

                    if (value != null)
                    {
                        if (column.ColumnType == ColumnType.Number) writer.Write(value.ToString());
                        else writer.Write("\"{0}\"", value.ToString().Replace("\"", "\"\""));
                    }

                    if (i < columnCount - 1) writer.Write(separator);
                }
                writer.Write(Environment.NewLine);
            }
        }

        /// <summary>
        /// Convert the object a dictionary list. Ordinal comparer is used for the keys.
        /// </summary>
        /// <returns></returns>
        public override List<Dictionary<string, object>> ToDictionaryList()
        {
            if (columns.Count == 0)
            {
                throw new InvalidOperationException("You need to specify at least one column to export value");
            }
            
            int columnCount = columns.Count;
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
            foreach (T item in sourceList)
            {
                var row = new Dictionary<string, object>(columns.Count, StringComparer.Ordinal);
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    var value = column.Func(item);
                    row[column.HeaderString] = value;
                }
                ret.Add(row);
            }
            return ret;
        }

        /// <summary>
        /// Convert the list to XML.
        /// </summary>
        /// <returns></returns>
        public override XElement ToXml()
        {
            if (columns.Count == 0)
                throw new InvalidOperationException(
                    "You need to specify at least one column to export value");

            XElement root = new XElement(xmlRootName);
            foreach (T item in sourceList)
            {
                XElement node = new XElement(xmlElementName);
                foreach (ExportableColumn<T> exportableColumn in columns)
                {
                    var value = exportableColumn.Func(item);
                    node.Add(new XAttribute(exportableColumn.HeaderString, value));
                }
                root.Add(node);
            }
            return root;
        }

        /// <summary>
        /// Convert the list to a CSV string.
        /// </summary>
        /// <returns></returns>
        public override string ToCsvString()
        {
            using (var sw = new StringWriter())
            {
                WriteCsvString(sw);
                return sw.ToString();
            }
        }
    }
}