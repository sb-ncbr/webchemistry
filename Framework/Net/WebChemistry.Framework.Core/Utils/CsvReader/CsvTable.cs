namespace WebChemistry.Framework.Core.Csv
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;

    /// <summary>
    /// Represents a Csv table.
    /// </summary>
    public class CsvTable : ReadOnlyCollection<DataRecord>
    {
        /// <summary>
        /// Table header.
        /// </summary>
        public HeaderRecord Header { get; private set; }
        
        /// <summary>
        /// Creates a new table.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="records"></param>
        public CsvTable(HeaderRecord header, IList<DataRecord> records)
            : base(records)
        {
            this.Header = header;
        }
        
        /// <summary>
        /// Reads CSV from a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="separator"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static CsvTable ReadFile(string filename, char separator = ',', char delimiter = '"')
        {
            return Read(() => new StreamReader(filename), separator, delimiter);
        }

        /// <summary>
        /// Reads CSV from a file.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="separator"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static CsvTable ReadString(string text, char separator = ',', char delimiter = '"')
        {
            return Read(() => new StringReader(text), separator, delimiter);
        }

        /// <summary>
        /// Reads table from a stream.
        /// </summary>
        /// <param name="streamProvider"></param>
        /// <param name="separator"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static CsvTable Read(Func<TextReader> streamProvider, char separator = ',', char delimiter = '"')
        {
            HeaderRecord header;
            List<DataRecord> records = new List<DataRecord>();
            using (var stream = streamProvider())
            using (var reader = new CsvReader(stream))
            {
                reader.ValueDelimiter = delimiter;
                reader.ValueSeparator = separator;
                header = reader.ReadHeaderRecord();
                DataRecord[] buffer = new DataRecord[1000];
                int read;
                while ((read = reader.ReadDataRecords(buffer, 0, 1000)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        records.Add(buffer[i]);
                    }
                }
            }
            return new CsvTable(header, records);
        }
    }
}
