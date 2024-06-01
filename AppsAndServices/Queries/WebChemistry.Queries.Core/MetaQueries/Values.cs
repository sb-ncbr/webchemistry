namespace WebChemistry.Queries.Core.MetaQueries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Queries.Core.Symbols;

    
    /// <summary>
    /// Meta value base class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MetaValue<T> : MetaQuery
    {
        public T Value { get; private set; }

        internal override string ToStringInternal()
        {
            if (Value == null) return "null";
            return Value.ToString();
        }

        public override string GetString()
        {
            return ToString();
        }

        public override object GetObjectValue()
        {
            return Value;
        }

        protected MetaValue(T value)
        {
            this.Value = value;
        }
    }

    /// <summary>
    /// Bool value.
    /// </summary>
    public class MetaObjectValue : MetaValue<object>
    {
        public override MetaQuery Head
        {
            get { return BasicHeads.ObjectValueHead; }
        }

        public override TypeExpression Type
        {
            get { return BasicTypes.Value; }
        }

        internal override Query Compile(Dictionary<string, QueryMotive> cache)
        {
            return new QueryValue(Value);
        }

        public MetaObjectValue(object value)
            : base(value)
        {

        }
    }

    /// <summary>
    /// Bool value.
    /// </summary>
    public class MetaBool : MetaValue<bool>
    {
        public override MetaQuery Head
        {
            get { return BasicHeads.BoolHead; }
        }

        public override TypeExpression Type
        {
            get { return BasicTypes.Bool; }
        }

        internal override Query Compile(Dictionary<string, QueryMotive> cache)
        {
            return new QueryValue(Value);
        }

        public override bool GetBool()
        {
 	         return Value;
        }

        public MetaBool(bool value)
            : base(value)
        {

        }
    }

    /// <summary>
    /// Integer value.
    /// </summary>
    public class MetaInteger : MetaValue<int>
    {
        public override MetaQuery Head
        {
            get { return BasicHeads.IntegerHead; }
        }

        public override TypeExpression Type
        {
            get { return BasicTypes.Integer; }
        }

        internal override Query Compile(Dictionary<string, QueryMotive> cache)
        {
            return new QueryValue(Value);
        }

        public override int GetInteger()
        {
            return Value;
        }

        public override double GetDouble()
        {
            return Value;
        }

        public MetaInteger(int value)
            : base(value)
        {

        }
    }

    /// <summary>
    /// Shit just got real.
    /// </summary>
    public class MetaReal : MetaValue<double>
    {
        public override MetaQuery Head
        {
            get { return BasicHeads.RealHead; }
        }

        public override TypeExpression Type
        {
            get { return BasicTypes.Real; }
        }

        internal override Query Compile(Dictionary<string, QueryMotive> cache)
        {
            return new QueryValue(Value);
        }

        public override double GetDouble()
        {
            return Value;
        }

        internal override string ToStringInternal()
        {
            return Value.ToStringInvariant();
        }

        public MetaReal(double value)
            : base(value)
        {

        }
    }

    /// <summary>
    /// Bool value.
    /// </summary>
    public class MetaStaticMatrix : MetaValue<double[][]>
    {
        public override MetaQuery Head
        {
            get { return BasicHeads.StaticMatrixHead; }
        }

        public override TypeExpression Type
        {
            get { return BasicTypes.StaticMatrix; }
        }

        internal override Query Compile(Dictionary<string, QueryMotive> cache)
        {
            return new QueryValue(Value);
        }

        public override double[][] GetStaticMatrix()
        {
            return Value;
        }

        public MetaStaticMatrix(double[][] value)
            : base(value)
        {

        }
    }

    /// <summary>
    /// Strings.
    /// </summary>
    public class MetaString : MetaValue<string>
    {
        public override MetaQuery Head
        {
            get { return BasicHeads.StringHead; }
        }

        public override TypeExpression Type
        {
            get { return BasicTypes.String; }
        }

        internal override Query Compile(Dictionary<string, QueryMotive> cache)
        {
            return new QueryValue(Value);
        }

        public override string GetString()
        {
            return Value;
        }

        internal override string ToStringInternal()
        {
            return "\"" + Value + "\"";
        }

        public MetaString(string value)
            : base(value)
        {

        }
    }
    
    /// <summary>
    /// Symbol.
    /// </summary>
    public class MetaSymbol : MetaQuery
    {
        SymbolDescriptor innerDescriptor;
        TypeExpression innerType, inferredType;

        public string Name { get; private set; }

        public override MetaQuery Head
        {
            get { return BasicHeads.SymbolHead; }
        }

        public SymbolDescriptor Descriptor
        {
            get { return innerDescriptor; }
        }

        public override TypeExpression Type
        {
            get { return innerType; }
        }

        public TypeExpression InferredType
        {
            get { return inferredType; }
        }

        //internal int TypeTag { get; set; }
        //public TypeExpression TaggedType
        //{
        //    get { return TypeVariable.Create(Name + "$" + TypeTag); }
        //}
        
        public override string GetString()
        {
            return Name;
        }

        internal override string ToStringInternal()
        {
            return Name;
        }

        internal override void ReplaceTypes(Dictionary<string, TypeExpression> types)
        {
            TypeExpression type;
            if (types.TryGetValue(Name, out type))
            {
                var unification = TypeExpression.Unify(type, inferredType);
                if (!unification.Success)
                {
                    throw new InvalidOperationException(string.Format("Could not unify types '{0}' and '{1}' of symbol '{2}'.", type, inferredType, Name));
                }
                this.inferredType = unification.InferedExpression;
            }
        }

        internal override void InferTypes(Dictionary<string, TypeExpression> types)
        {
            TypeExpression type;
            if (types.TryGetValue(Name, out type))
            {
                var unification = TypeExpression.Unify(type, inferredType);
                if (!unification.Success)
                {
                    throw new InvalidOperationException(string.Format("Could not unify types '{0}' and '{1}' of symbol '{2}'.", type, inferredType, Name));
                }
                types[Name] = unification.InferedExpression;
            }
        }

        public override MetaQuery SubstituteSymbols(Dictionary<string, MetaQuery> values)
        {
            MetaQuery sub;
            if (values.TryGetValue(Name, out sub)) return sub;
            return this;
        }
        
        internal override Query Compile(Dictionary<string, QueryMotive> compileCache)
        {
            if (innerDescriptor != null) return ApplyTo().Compile(compileCache);
            return new SymbolQuery(Name);
        }

        public MetaSymbol(string name)
        {
            this.Name = name;
            this.innerDescriptor = SymbolTable.TryGetDescriptor(name);
            this.innerType = TypeVariable.Create(name);
            this.inferredType = innerType;
            if (innerDescriptor != null) this.Name = innerDescriptor.Name;
        }
    }
}
