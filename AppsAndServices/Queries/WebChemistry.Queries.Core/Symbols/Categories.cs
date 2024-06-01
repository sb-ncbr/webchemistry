
namespace WebChemistry.Queries.Core.Symbols
{
    /// <summary>
    /// Symbol category.
    /// </summary>
    public class SymbolCategory
    {
        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Index for automatic help generation.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Automatically exclude these function from generated help.
        /// </summary>
        public bool ExcludeFromHelp { get; set; }
    }

    /// <summary>
    /// Default symbol categoies.
    /// </summary>
    public static class SymbolCategories
    {
        /// <summary>
        /// Elementary types such as Bool or Int.
        /// </summary>
        public static readonly SymbolCategory ElementaryTypes = new SymbolCategory
        {
            Name = "Elementary Types",
            Description = "Basic value types.",
            Index = 0,
            ExcludeFromHelp = true
        };

        public static readonly SymbolCategory MotiveTypes = new SymbolCategory
        {
            Name = "Motive Types",
            Description = "Types of motives.",
            Index = 1,
            ExcludeFromHelp = true
        };

        public static readonly SymbolCategory LanguagePrimitives = new SymbolCategory
        {
            Name = "Basic Language Syntax",
            Description = "Basic syntactic elements of the Queries language.",
            Index = 2,
            ExcludeFromHelp = true
        };
        
        public static readonly SymbolCategory BasicMotiveFunctions = new SymbolCategory
        {
            Name = "Basic Query Functions",
            Description = "Basic building blocks of the language - i.e. atoms, residues, and the like.",
            Index = 4
        };


        public static readonly SymbolCategory AdvancedFunctions = new SymbolCategory
        {
            Name = "Advanced Query Functions",
            Description = "Advanced building blocks of the language.",
            Index = 5
        };

        public static readonly SymbolCategory FilterFunctions = new SymbolCategory
        {
            Name = "Filter Functions",
            Description = "Functions useful for filtering patterns.",
            Index = 6
        };
        
        public static readonly SymbolCategory TopologyFunctions = new SymbolCategory
        {
            Name = "Topology Functions",
            Description = "Functions that rely on the topology of patterns.",
            Index = 7
        };

        public static readonly SymbolCategory GeometryFunctions = new SymbolCategory
        {
            Name = "Geometry Functions",
            Description = "Functions that rely on the geometry of patterns.",
            Index = 8
        };

        public static readonly SymbolCategory MetadataFunctions = new SymbolCategory
        {
            Name = "Meta-data Functions",
            Description = "Functions dealing with meta-data about structures such as release date or authors.",
            Index = 9
        };

        public static readonly SymbolCategory MiscFunctions = new SymbolCategory
        {
            Name = "Miscellaneous Functions",
            Description = "Various useful functions. These function often require a special setup (i.e. only useful in Scripting window or in specific applications).",
            Index = 10
        };

        public static readonly SymbolCategory ValueFunctions = new SymbolCategory
        {
            Name = "Value Functions",
            Description = "Functions such as addition or comparison of numbers.",
            Index = 25
        };
    }
}
