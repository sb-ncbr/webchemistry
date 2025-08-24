namespace WebChemistry.Framework.Core.PdbQt
{
    using System;
    using System.IO;

    public class PdbQtReader
    {
        readonly string Filename;
        readonly PdbReaderParams Parameters;
        readonly TextReader Reader;

        public StructureReaderResult Read()
        {
            throw new NotImplementedException();
        }

        public PdbQtReader(string filename, TextReader reader, PdbReaderParams parameters)
        {
            this.Filename = filename;
            this.Reader = reader;
            this.Parameters = parameters;
        }
    }
}
