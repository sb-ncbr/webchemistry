namespace WebChemistry.Queries.Core.Symbols
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core.MetaQueries;
    using WebChemistry.Queries.Core.Queries;

    /// <summary>
    /// Symbol attributes.
    /// </summary>
    [Flags]
    public enum SymbolAttributes
    {
        /// <summary>
        /// Nothing.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// F[x,F[y, z]] = F[x, y, z]
        /// </summary>
        Flat = 1 << 1,

        /// <summary>
        /// F[y, z, x] = F[x, y, z]
        /// </summary>
        Orderless = 1 << 2,

        /// <summary>
        /// F[x] = x
        /// </summary>
        OneIdentity = 1 << 3,

        /// <summary>
        /// F[x, x] = F[x]
        /// </summary>
        UniqueArgs = 1 << 4,
        
        /// <summary>
        /// F[x, Empty, y] = F[x, y]
        /// </summary>
        IgnoreEmpty = 1 << 5,
    }

    /// <summary>
    /// Options descriptor.
    /// </summary>
    public class MetaOption
    {
        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the option.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Default value represented as a query.
        /// </summary>
        public MetaQuery DefaultValue { get; set; }

        /// <summary>
        /// (Return)Type of the option.
        /// </summary>
        public TypeExpression Type { get; set; }
    }

    /// <summary>
    /// Examples for a specific symbol.
    /// </summary>
    public class SymbolExample
    {
        /// <summary>
        /// Example code.
        /// </summary>
        public string ExampleCode { get; set; }

        /// <summary>
        /// Description of the example.
        /// </summary>
        public string ExampleDescription { get; set; }

        /// <summary>
        /// Natural language form of the input.
        /// </summary>
        public string NaturalForm { get; set; }

        /// <summary>
        /// Comment for natural language form.
        /// </summary>
        public string NaturalFormRemark { get; set; }
    }

    /// <summary>
    /// Symbol description used to automatically generate help.
    /// </summary>
    public class SymbolDescription
    {
        /// <summary>
        /// The description text.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Operator form. Can be null.
        /// </summary>
        public string OperatorForm { get; set; }

        /// <summary>
        /// Is the function in . syntax?
        /// </summary>
        public bool IsDotSyntax { get; set; }

        /// <summary>
        /// The symbol is internal.
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// Determine whether to ignore this function for auto-completion.
        /// </summary>
        public bool IgnoreForAutoCompletion { get; set; }

        /// <summary>
        /// Symbol category.
        /// </summary>
        public SymbolCategory Category { get; set; }

        /// <summary>
        /// Examples.
        /// </summary>
        public SymbolExample[] Examples { get; set; }

        public SymbolDescription()
        {
            Examples = new SymbolExample[0];
        }
    }

    /// <summary>
    /// Function parameter wrapper.
    /// </summary>
    public class FunctionArgument
    {
        /// <summary>
        /// The type of the parameter.
        /// </summary>
        public TypeExpression Type { get; set; }

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameter description.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Information about a symbol.
    /// </summary>
    public class SymbolDescriptor
    {
        /// <summary>
        /// This is used to compile MetaFunctions.
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public delegate Query CompileDelegate(MetaApply self, Dictionary<string, QueryMotive> cache);

        /// <summary>
        /// An "optional" normalization function for transforming functions.
        /// For example Near(R, x, ..., x) -> Filter(Cluster(R, ...), Count(....) == N1...)..
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public delegate MetaQuery NormalizeDelegate(MetaApply self);

        /// <summary>
        /// Symbol name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the symbol.
        /// </summary>
        public SymbolDescription Description { get; set; }

        /// <summary>
        /// Symbol attributes used during the (function) normalization.
        /// </summary>
        public SymbolAttributes Attributes { get; set; }

        /// <summary>
        /// Type of the symbol.
        /// </summary>
        public TypeExpression Type { get; set; }

        /// <summary>
        /// Symbol function arguments.
        /// </summary>
        public FunctionArgument[] Arguments { get; set; }

        TypeArrow functionType;
        /// <summary>
        /// Function type of the symbol.
        /// </summary>
        public TypeArrow FunctionType { get { return functionType = functionType ?? TypeArrow.Create(TypeTuple.Create(Arguments.Select(a => a.Type)), Type); } }

        /// <summary>
        /// Compile function used by MetaFunction.Compile.
        /// </summary>
        public CompileDelegate Compile { get; set; }

        /// <summary>
        /// An "optional" normalization function for transforming functions.
        /// For example Near(R, x, ..., x) -> Filter(Cluster(R, ...), Count(....) == N1...)..
        /// </summary>
        public NormalizeDelegate Normalize { get; set; }

        /// <summary>
        /// A dictionary of options. All these options are propagated to the respective "MetaFunction".
        /// </summary>
        public Dictionary<string, MetaOption> Options { get; set; }

        /// <summary>
        /// Returns hash code of the Name as the names must be unique anyway.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// Compares the names, case sensitive.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as SymbolDescriptor;
            if (other == null) return false;
            return other.Name.Equals(Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates an "empty" descriptor.
        /// 
        /// Normalize is set to self => self
        /// Compile is set to "NotSupportedException"
        /// Options is set to an empty dictionary.
        /// </summary>
        public SymbolDescriptor()
        {
            Normalize = (self) => self;
            Compile = (_, __) => { throw new NotSupportedException(); };
            Arguments = new FunctionArgument[0];
            Options = new Dictionary<string, MetaOption>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
