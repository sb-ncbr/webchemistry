namespace WebChemistry.Queries.Core.MetaQueries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Queries.Core.Symbols;


    static class BasicHeads
    {
        public static readonly MetaQuery SymbolHead = new MetaSymbol(SymbolTable.SymbolSymbol.Name);
        public static readonly MetaQuery ObjectValueHead = new MetaSymbol(SymbolTable.ObjectValueSymbol.Name);
        public static readonly MetaQuery BoolHead = new MetaSymbol(SymbolTable.BoolSymbol.Name);
        public static readonly MetaQuery StringHead = new MetaSymbol(SymbolTable.StringSymbol.Name);
        public static readonly MetaQuery RealHead = new MetaSymbol(SymbolTable.RealSymbol.Name);
        public static readonly MetaQuery IntegerHead = new MetaSymbol(SymbolTable.IntegerSymbol.Name);
        public static readonly MetaQuery StaticMatrixHead = new MetaSymbol(SymbolTable.StaticMatrixSymbol.Name);
        public static readonly MetaQuery LambdaHead = new MetaSymbol(SymbolTable.LambdaSymbol.Name);
        public static readonly MetaQuery LetHead = new MetaSymbol(SymbolTable.LetSymbol.Name);
        public static readonly MetaQuery TupleHead = new MetaSymbol(SymbolTable.TupleSymbol.Name);
        public static readonly MetaQuery AssignHead = new MetaSymbol(SymbolTable.AssignSymbol.Name);
    }
    
    /// <summary>
    /// It's so meta ...
    /// </summary>
    public abstract class MetaQuery
    {
        /// <summary>
        /// The descriptor of the current symbol.
        /// </summary>
        public abstract MetaQuery Head { get; }

        /// <summary>
        /// The type of the query.
        /// </summary>
        public abstract TypeExpression Type { get; }
        
        /// <summary>
        /// Compile the query.
        /// </summary>
        /// <returns></returns>
        public Query Compile()
        {
            return Compile(new Dictionary<string, QueryMotive>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Compile the query using a cache.
        /// </summary>
        /// <param name="compileCache"></param>
        /// <returns></returns>
        internal virtual Query Compile(Dictionary<string, QueryMotive> compileCache)
        {
            throw new NotSupportedException(string.Format("Expression '{0}' cannot be compiled.", ToString(), this.GetType().Name));
        }

        string _stringCache;
        /// <summary>
        /// To string, cached.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_stringCache != null) return _stringCache;
            _stringCache = ToStringInternal();
            return _stringCache;
        }

        /// <summary>
        /// Convert to string, do not cache.
        /// </summary>
        /// <returns></returns>
        internal abstract string ToStringInternal();
        
        /// <summary>
        /// Create a value from integer.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MetaQuery CreateValue(int value)
        {
            return new MetaInteger(value);
        }

        /// <summary>
        /// Create a value from string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MetaQuery CreateValue(string value)
        {
            return new MetaString(value);
        }

        /// <summary>
        /// Create a value from double.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MetaQuery CreateValue(double value)
        {
            return new MetaReal(value);
        }

        /// <summary>
        /// Create a value from static matrix.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MetaQuery CreateValue(double[][] value)
        {
            return new MetaStaticMatrix(value);
        }

        /// <summary>
        /// Create a value from bool.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MetaQuery CreateValue(bool value)
        {
            return new MetaBool(value);
        }

        /// <summary>
        /// Create a value from object.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MetaQuery CreateObjectValue(object value)
        {
            return new MetaObjectValue(value);
        }

        /// <summary>
        /// Create a symbol.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MetaSymbol CreateSymbol(string name)
        {
            return new MetaSymbol(name);
        }

        /// <summary>
        /// Create a tuple.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static MetaTuple CreateTuple(params MetaQuery[] elements)
        {
            return MetaTuple.Create(elements);
        }

        /// <summary>
        /// Create lambda.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static MetaLambda CreateLambda(MetaQuery args, MetaQuery body)
        {
            return MetaLambda.Create(args, body);
        }

        /// <summary>
        /// Create a let binding.
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static MetaLet CreateLet(MetaQuery assignment, MetaQuery expression)
        {
            return MetaLet.Create(assignment, expression);
        }

        /// <summary>
        /// Create apply.
        /// </summary>
        /// <param name="head"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static MetaQuery CreateApply(MetaQuery head, MetaQuery arguments)
        {
            return MetaApply.Create(head, arguments);
        }

        /// <summary>
        /// Applies this to the arguments.
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public MetaQuery ApplyTo(params MetaQuery[] arguments)
        {
            return CreateApply(this, CreateTuple(arguments));
        }
                
        /// <summary>
        /// Substitute a symbol and return a new query.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        public virtual MetaQuery SubstituteSymbols(Dictionary<string, MetaQuery> values)
        {
            return this;
        }

        /// <summary>
        /// Replace symbol types where appropriate.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        internal virtual void ReplaceTypes(Dictionary<string, TypeExpression> types)
        {

        }
        
        /// <summary>
        /// Tries to replaces the types in the dictionary.
        /// </summary>
        /// <param name="types"></param>
        internal virtual void InferTypes(Dictionary<string, TypeExpression> types)
        {

        }

        /// <summary>
        /// Check if this has a specific head.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal bool HeadEquals(string name)
        {
            var symbol = Head as MetaSymbol;
            if (symbol != null && symbol.Name.EqualOrdinalIgnoreCase(name)) return true;
            return false;
        }
        
        /// <summary>
        /// Get the string value of the query.
        /// </summary>
        /// <returns></returns>
        public virtual string GetString()
        {
            throw new InvalidOperationException(string.Format("Cannot retrieve string value from the type '{0}'.", this.GetType().Name));
        }

        /// <summary>
        /// Get the integer value of the query.
        /// </summary>
        /// <returns></returns>
        public virtual int GetInteger()
        {
            throw new InvalidOperationException(string.Format("Cannot retrieve integer value from the type '{0}'.", this.GetType().Name));
        }

        /// <summary>
        /// Get the bool value of the query.
        /// </summary>
        /// <returns></returns>
        public virtual bool GetBool()
        {
            throw new InvalidOperationException(string.Format("Cannot retrieve bool value from the type '{0}'.", this.GetType().Name));
        }

        /// <summary>
        /// Get the double value of the query.
        /// </summary>
        /// <returns></returns>
        public virtual double GetDouble()
        {
            throw new InvalidOperationException(string.Format("Cannot retrieve real value from the type '{0}'.", this.GetType().Name));
        }

        /// <summary>
        /// Get the double value of the query.
        /// </summary>
        /// <returns></returns>
        public virtual double[][] GetStaticMatrix()
        {
            throw new InvalidOperationException(string.Format("Cannot retrieve static matrix value from the type '{0}'.", this.GetType().Name));
        }

        /// <summary>
        /// Get the inner value.
        /// </summary>
        /// <returns></returns>
        public virtual object GetObjectValue()
        {
            throw new InvalidOperationException(string.Format("Cannot retrieve object value from the type '{0}'.", this.GetType().Name));
        }
    }

    public class EmptyQueryElement : MetaQuery
    {
        public override string ToString()
        {
            return "Empty";
        }

        public override MetaQuery Head
        {
            get { throw new NotImplementedException(); }
        }

        public override TypeExpression Type
        {
            get { throw new NotImplementedException(); }
        }

        internal override string ToStringInternal()
        {
            return "Empty";
        }
    }
}
