namespace WebChemistry.Queries.Service.DataModel
{
    using System;

    /// <summary>
    /// Marks a type or property that should be expanded in generated standalone help.
    /// </summary>
    public class HelpDescribeAttribute : Attribute
    {
        public Type Subtype { get; private set; }

        public HelpDescribeAttribute(Type subtype = null)
        {
            Subtype = subtype;
        }
    }

    /// <summary>
    /// Provides a friendly type name for generated standalone help.
    /// </summary>
    public class HelpTypeNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public HelpTypeNameAttribute(string name)
        {
            Name = name;
        }
    }
}
