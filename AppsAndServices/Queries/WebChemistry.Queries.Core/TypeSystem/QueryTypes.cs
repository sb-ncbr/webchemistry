namespace WebChemistry.Queries.Core
{
    using WebChemistry.Framework.TypeSystem;

    /// <summary>
    /// Query types.
    /// </summary>
    public enum QueryTypes
    {
        /// <summary>
        /// Any type goes.
        /// </summary>
        Any,
        /// <summary>
        /// Returns PatternSeq.
        /// </summary>
        PatternSeq,
        /// <summary>
        /// Returns bool.
        /// </summary>
        Boolean
    }

    /// <summary>
    /// Queries type classes.
    /// </summary>
    public static class TypeClasses
    {
        /// <summary>
        /// Motive type.
        /// </summary>
        public static readonly TypeClass Pattern = TypeClass.Create("Pattern");

        /// <summary>
        /// Motive type.
        /// </summary>
        public static readonly TypeClass PatternSeq = TypeClass.Create("PatternSeq");
                
        /// <summary>
        /// Ring class.
        /// </summary>
        public static readonly TypeClass Rings = TypeClass.Create("Rings", PatternSeq);

        /// <summary>
        /// Atom class.
        /// </summary>
        public static readonly TypeClass Atoms = TypeClass.Create("Atoms", PatternSeq);
        
        /// <summary>
        /// Residue class.
        /// </summary>
        public static readonly TypeClass Residues = TypeClass.Create("Residues", PatternSeq);

        /// <summary>
        /// Value type base.
        /// </summary>
        public static readonly TypeClass Value = TypeClass.Create("Value");

        /// <summary>
        /// Number.
        /// </summary>
        public static readonly TypeClass Number = TypeClass.Create("Number", Value);

        /// <summary>
        /// Integers.
        /// </summary>
        public static readonly TypeClass Integer = TypeClass.Create("Integer", Number);

        /// <summary>
        /// Reals (doubles).
        /// </summary>
        public static readonly TypeClass Real = TypeClass.Create("Real", Number);

        /// <summary>
        /// Reals (doubles).
        /// </summary>
        public static readonly TypeClass StaticMatrix = TypeClass.Create("StaticMatrix", Value);

        /// <summary>
        /// Boolean value.
        /// </summary>
        public static readonly TypeClass Bool = TypeClass.Create("Bool", Value);

        /// <summary>
        /// Strings.
        /// </summary>
        public static readonly TypeClass String = TypeClass.Create("String", Value);
        
        /// <summary>
        /// Symbols. Sometimes semantically equivalent to strings.
        /// </summary>
        public static readonly TypeClass Symbol = TypeClass.Create("Symbol", Value);

        /// <summary>
        /// List.
        /// </summary>
        public static readonly TypeClass List = TypeClass.Create("List");
    }

    /// <summary>
    /// Queries basic types.
    /// </summary>
    public static class BasicTypes
    {
        /// <summary>
        /// Single motive (list of atoms)
        /// </summary>
        public static readonly TypeExpression Any = TypeConstant.Create(TypeClass.Any);

        /// <summary>
        /// Single motive (list of atoms)
        /// </summary>
        public static readonly TypeExpression Pattern = TypeConstant.Create(TypeClasses.Pattern);

        /// <summary>
        /// Motive sequence.
        /// </summary>
        public static readonly TypeExpression PatternSeq = TypeConstant.Create(TypeClasses.PatternSeq);

        /// <summary>
        /// Atops.
        /// </summary>
        public static readonly TypeExpression Atoms = TypeConstant.Create(TypeClasses.Atoms);

        /// <summary>
        /// Residues.
        /// </summary>
        public static readonly TypeExpression Residues = TypeConstant.Create(TypeClasses.Residues);

        /// <summary>
        /// Rings.
        /// </summary>
        public static readonly TypeExpression Rings = TypeConstant.Create(TypeClasses.Rings);

        /// <summary>
        /// Value type constant.
        /// </summary>
        public static readonly TypeExpression Value = TypeConstant.Create(TypeClasses.Value);

        /// <summary>
        /// Number type constant.
        /// </summary>
        public static readonly TypeExpression Number = TypeConstant.Create(TypeClasses.Number);

        /// <summary>
        /// Integer type constant.
        /// </summary>
        public static readonly TypeExpression Integer = TypeConstant.Create(TypeClasses.Integer);

        /// <summary>
        /// Real number (double).
        /// </summary>
        public static readonly TypeExpression Real = TypeConstant.Create(TypeClasses.Real);

        /// <summary>
        /// Real number (double).
        /// </summary>
        public static readonly TypeExpression StaticMatrix = TypeConstant.Create(TypeClasses.StaticMatrix);

        /// <summary>
        /// String.
        /// </summary>
        public static readonly TypeExpression String = TypeConstant.Create(TypeClasses.String);

        /// <summary>
        /// Symbol.
        /// </summary>
        public static readonly TypeExpression Symbol = TypeConstant.Create(TypeClasses.Symbol);

        /// <summary>
        /// Boolean.
        /// </summary>
        public static readonly TypeExpression Bool = TypeConstant.Create(TypeClasses.Bool);

        /// <summary>
        /// List.
        /// </summary>
        public static readonly TypeExpression List = TypeConstant.Create(TypeClasses.List);
    }
}
