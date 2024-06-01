namespace WebChemistry.Framework.TypeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Simple type class system.
    /// </summary>
    public class TypeClass
    {
        /// <summary>
        /// Any is a parent of all type classes.
        /// </summary>
        public static TypeClass Any = new TypeClass { Name = "Any" };

        /// <summary>
        /// Name of the type class.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Parent of the class. Always "at least" Any.
        /// </summary>
        public TypeClass DerivedFrom { get; private set; }

        /// <summary>
        /// Check if the class is derived from some other one.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool IsDerivedFrom(TypeClass c)
        {
            if (DerivedFrom == null) return c.Name == Name;
            return c.Name == Name || DerivedFrom.IsDerivedFrom(c);
        }
        
        /// <summary>
        /// Create an instace of a type class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static TypeClass Create(string name, TypeClass parent = null)
        {
            return new TypeClass { Name = name, DerivedFrom = parent ?? Any };
        }

        private TypeClass()
        {

        }
    }

    /// <summary>
    /// Type expression base class.
    /// </summary>
    public abstract class TypeExpression
    {
        /// <summary>
        /// Substitute procedure used by the unification algorithm.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public abstract TypeExpression Substitute(Dictionary<string, TypeExpression> values);
        
        string _stringCache = null;
        /// <summary>
        /// Convert to string, cached.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_stringCache != null) return _stringCache;
            _stringCache = ToStringInternal();
            return _stringCache;
        }

        /// <summary>
        /// non-cached version of ToString.
        /// </summary>
        /// <returns></returns>
        internal abstract string ToStringInternal();

        /// <summary>
        /// Create a function builder.
        /// </summary>
        /// <returns></returns>
        public static FunctionTypeBuilder Function()
        {
            return new FunctionTypeBuilder();
        }


        /// <summary>
        /// Unify types 'a' and 'b'.
        /// 'a' is the "pivot" type. Ie. when matching two type constants b.IsDerivedFrom(a) is called.
        /// </summary>
        /// <param name="a">The "pivot" type (usually without variables)</param>
        /// <param name="b">The type to match.</param>
        /// <returns></returns>
        public static UnificationResult Unify(TypeExpression a, TypeExpression b)
        {
            return TypeUnification.Unify(a, b);
        }
    }

    /// <summary>
    /// Matches anything.
    /// </summary>
    public class TypeWildcard : TypeExpression
    {
        /// <summary>
        /// Instance of the type.
        /// </summary>
        public static readonly TypeExpression Instance = new TypeWildcard();

        /// <summary>
        /// Substitution.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public override TypeExpression Substitute(Dictionary<string, TypeExpression> values)
        {
            return this;
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        internal override string ToStringInternal()
        {
            return "?";
        }

        private TypeWildcard()
        {

        }
    }

    /// <summary>
    /// Type variable (i.e. x)
    /// </summary>
    public class TypeVariable : TypeExpression
    {
        /// <summary>
        /// Name of the variable.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Substitution.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public override TypeExpression Substitute(Dictionary<string, TypeExpression> values)
        {
            TypeExpression value;
            if (values.TryGetValue(Name, out value)) return value;
            return this;
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        internal override string ToStringInternal()
        {
            return "'" + Name;
        }

        /// <summary>
        /// Create a type variable.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TypeVariable Create(string name)
        {
            return new TypeVariable { Name = name };
        }

        private TypeVariable()
        {

        }
    }

    /// <summary>
    /// Type constant with a type class assigned.
    /// </summary>
    public class TypeConstant : TypeExpression
    {
        /// <summary>
        /// Type class of this constant.
        /// </summary>
        public TypeClass Class { get; private set; }

        /// <summary>
        /// Does nothing really...
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public override TypeExpression Substitute(Dictionary<string, TypeExpression> values)
        {
            return this;
        }
        
        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        internal override string ToStringInternal()
        {
            return Class.Name;
        }

        /// <summary>
        /// If null, Class = TypeClass.Any
        /// </summary>
        /// <param name="class"></param>
        /// <returns></returns>
        public static TypeConstant Create(TypeClass @class = null)
        {
            return new TypeConstant { Class = @class ?? TypeClass.Any };
        }

        private TypeConstant()
        {

        }
    }

    /// <summary>
    /// Many type, i.e. a*. Matches one or more "inner" types.
    /// </summary>
    public class TypeMany : TypeExpression
    {
        /// <summary>
        /// Allow empty.
        /// </summary>
        public bool AllowEmpty { get; private set; }

        /// <summary>
        /// Max number of occurences. If -1, "unlimited"
        /// </summary>
        public bool IsOption { get; private set; }

        /// <summary>
        /// Inner type.
        /// </summary>
        public TypeExpression Inner { get; private set; }

        /// <summary>
        /// Substitute.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public override TypeExpression Substitute(Dictionary<string, TypeExpression> values)
        {
            return new TypeMany { Inner = Inner.Substitute(values), AllowEmpty = AllowEmpty, IsOption = IsOption };
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        internal override string ToStringInternal()
        {
            if (IsOption)
            {
                if (Inner is TypeArrow) return "?(" + Inner.ToStringInternal() + ")";
                return "?" + Inner.ToStringInternal();
            }
            if (AllowEmpty)
            {
                if (Inner is TypeArrow) return "(" + Inner.ToStringInternal() + ")*";
                return Inner.ToStringInternal() + "*";
            }
            if (Inner is TypeArrow) return "(" + Inner.ToStringInternal() + ")+";
            return Inner.ToStringInternal() + "+";
        }

        /// <summary>
        /// Create the type.
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="allowEmpty"></param>
        /// <param name="isOption"></param>
        /// <returns></returns>
        public static TypeMany Create(TypeExpression inner, bool allowEmpty = false, bool isOption = false)
        {
            return new TypeMany { Inner = inner, AllowEmpty = isOption ? true : allowEmpty, IsOption = isOption };
        }

        private TypeMany()
        {

        }
    }
    
    /// <summary>
    /// Tuple type, i.e. (x, y, z)
    /// </summary>
    public class TypeTuple : TypeExpression
    {
        /// <summary>
        /// Elements of the tuple.
        /// </summary>
        public ReadOnlyCollection<TypeExpression> Elements { get; private set; }

        /// <summary>
        /// Substitute in each element.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public override TypeExpression Substitute(Dictionary<string, TypeExpression> values)
        {
            return TypeTuple.Create(Elements.Select(e => e.Substitute(values)).ToArray());
        }
        
        /// <summary>
        /// ....
        /// </summary>
        /// <returns></returns>
        internal override string ToStringInternal()
        {
            if (Elements.Count > 1) return "(" + string.Join(",", Elements.Select(e => e.ToStringInternal()).ToArray()) + ")";
            if (Elements.Count == 0) return "()";
            return Elements[0].ToStringInternal();
        }

        /// <summary>
        /// Create the tuple.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static TypeTuple Create(IEnumerable<TypeExpression> elements)
        {
            return new TypeTuple { Elements = new ReadOnlyCollection<TypeExpression>(elements.ToList()) };
        }

        private TypeTuple()
        {

        }
    }

    /// <summary>
    /// Arrow/function type, i.e. a -> b
    /// </summary>
    public class TypeArrow : TypeExpression
    {
        /// <summary>
        /// From...
        /// </summary>
        public TypeExpression From { get; private set; }

        /// <summary>
        /// ...To
        /// </summary>
        public TypeExpression To { get; private set; }

        /// <summary>
        /// Substitute.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public override TypeExpression Substitute(Dictionary<string, TypeExpression> values)
        {
            return new TypeArrow { From = From.Substitute(values), To = To.Substitute(values) };
        }
        
        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        internal override string ToStringInternal()
        {
            return From.ToStringInternal() + "->" + To.ToStringInternal();
        }

        /// <summary>
        /// Creates the arrow.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static TypeArrow Create(TypeExpression from, TypeExpression to)
        {
            return new TypeArrow { From = from, To = to };
        }
        
        private TypeArrow()
        {

        }
    }
        
    /// <summary>
    /// Function type builder.
    /// </summary>
    public class FunctionTypeBuilder
    {
        List<TypeExpression> args = new List<TypeExpression>();

        /// <summary>
        /// Type constant argument.
        /// </summary>
        /// <param name="class"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Arg(TypeClass @class)
        {
            args.Add(TypeConstant.Create(@class));
            return this;
        }

        /// <summary>
        /// Variable.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Arg(string variable)
        {
            args.Add(TypeVariable.Create(variable));
            return this;
        }

        /// <summary>
        /// Any type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Arg(TypeExpression type)
        {
            args.Add(type);
            return this;
        }

        /// <summary>
        /// Optional argument constant.
        /// </summary>
        /// <param name="class"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Opt(TypeClass @class)
        {
            args.Add(TypeMany.Create(TypeConstant.Create(@class), isOption: true));
            return this;
        }

        /// <summary>
        /// Optional argument variable.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Opt(string variable)
        {
            args.Add(TypeMany.Create(TypeVariable.Create(variable), isOption: true));
            return this;
        }

        /// <summary>
        /// Any type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Opt(TypeExpression type)
        {
            args.Add(TypeMany.Create(type, isOption: true));
            return this;
        }

        /// <summary>
        /// Many arguments constant.
        /// </summary>
        /// <param name="class"></param>
        /// <param name="allowEmpty"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Many(TypeClass @class, bool allowEmpty = false)
        {
            args.Add(TypeMany.Create(TypeConstant.Create(@class), isOption: false, allowEmpty: allowEmpty));
            return this;
        }

        /// <summary>
        /// Many arguments variable.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="allowEmpty"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Many(string variable, bool allowEmpty = false)
        {
            args.Add(TypeMany.Create(TypeVariable.Create(variable), isOption: false, allowEmpty: allowEmpty));
            return this;
        }

        /// <summary>
        /// Any type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="allowEmpty"></param>
        /// <returns></returns>
        public FunctionTypeBuilder Many(TypeExpression type, bool allowEmpty = false)
        {
            args.Add(TypeMany.Create(type, isOption: false, allowEmpty: allowEmpty));
            return this;
        }

        /// <summary>
        /// Return constant.
        /// </summary>
        /// <param name="class"></param>
        /// <returns></returns>
        public TypeExpression Return(TypeClass @class)
        {
            return TypeArrow.Create(TypeTuple.Create(args), TypeConstant.Create(@class));
        }

        /// <summary>
        /// Return variable.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public TypeExpression Return(string variable)
        {
            return TypeArrow.Create(TypeTuple.Create(args), TypeVariable.Create(variable));
        }

        /// <summary>
        /// Any type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TypeExpression Return(TypeExpression type)
        {
            return TypeArrow.Create(TypeTuple.Create(args), type);
        }
    }
}
