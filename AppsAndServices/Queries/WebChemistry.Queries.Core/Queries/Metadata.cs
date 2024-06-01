namespace WebChemistry.Queries.Core.Queries
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Core.Utils;
    using WebChemistry.Framework.Core.Pdb;

    ////class MetadataQuery : QueryValueBase
    ////{
    ////    string name;
    ////    Query motive;
    ////    Func<Motive, object> getter;

    ////    static Func<Motive, object> CreateGetter(string name)
    ////    {
    ////        switch (name.ToLowerInvariant())
    ////        {
    ////            case "pdbresolution": return m => m.Context.Structure.PdbMetadata().Resolution;
    ////            default: return _ => null;
    ////        }
    ////    }

    ////    internal override object Execute(ExecutionContext context)
    ////    {
    ////        var m = motive.ExecuteObject(context).MustBe<Motive>();
    ////        return getter(m);
    ////    }

    ////    protected override string ToStringInternal()
    ////    {
    ////        return string.Format("Metadata[{0}, \"{1}\"]", motive.ToString(), name);
    ////    }

    ////    public MetadataQuery(string name, Query motive)
    ////    {
    ////        this.name = name.ToLowerInvariant();
    ////        this.getter = CreateGetter(this.name);
    ////        this.motive = motive;
    ////    }
    ////}

    class LambdaPdbMetadataQuery : QueryValueBase
    {
        string name;
        Query motive;
        Func<PdbMetadata, object> getter;
        
        internal override object Execute(ExecutionContext context)
        {
            var m = motive.ExecuteObject(context).MustBe<Motive>().Context.Structure;
            var data = m.PdbMetadata();
            if (data == null) return null;
            return getter(data);
        }

        protected override string ToStringInternal()
        {
            return string.Format("Metadata[{0}, \"{1}\"]", motive.ToString(), name);
        }

        public LambdaPdbMetadataQuery(string name, Query motive, Func<PdbMetadata, object> getter)
        {
            this.name = name.ToLowerInvariant();
            this.getter = getter;
            this.motive = motive;
        }
    }

    class HasMetadataPropertiesQuery : QueryValueBase
    {
        bool isAny;
        string name;
        string[] properties;
        Func<PdbMetadata, string[]> getter;
        Query motive;
        bool caseSensitive;

        internal override object Execute(ExecutionContext context)
        {
            var m = motive.ExecuteObject(context).MustBe<Motive>().Context.Structure;
            var data = m.PdbMetadata();
            if (data == null) return false;

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            var props = getter(data);
            for (int i = 0; i < properties.Length; i++)
            {
                var x = properties[i];
                bool found = false;
                for (int j = 0; j < props.Length; j++)
                {
                    var p = props[j];
                    var o = p.IndexOf(x, comparison);
                    if (o < 0) continue;

                    if ((p.Length <= o + x.Length || !char.IsLetterOrDigit(p[o + x.Length]))
                        && (o == 0 || !char.IsLetterOrDigit(p[o - 1])))
                    {
                        if (isAny) return true;
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            return true;
        }

        protected override string ToStringInternal()
        {
            return string.Format("Has{0}{1}[{2}, {3}]", isAny ? "Any" : "All", name, motive.ToString(), string.Join(", ", properties.Select(a => "\"" + a + "\"")));
        }

        public HasMetadataPropertiesQuery(string name, Func<PdbMetadata, string[]> getter, bool caseSensitive, bool isAny, Query motive, IEnumerable<string> properties)
        {
            this.name = name;
            this.getter = getter;
            this.caseSensitive = caseSensitive;
            this.isAny = isAny;
            this.properties = properties.ToArray();
            this.motive = motive;
        }
    }
}
