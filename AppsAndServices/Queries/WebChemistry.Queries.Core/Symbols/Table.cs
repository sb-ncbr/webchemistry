namespace WebChemistry.Queries.Core.Symbols
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Queries.Core.MetaQueries;
    using System.Collections.ObjectModel;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// A symbol table.
    /// </summary>
    public static partial class SymbolTable
    {
        private static readonly Dictionary<string, SymbolDescriptor> symbols;

        public static readonly ReadOnlyCollection<SymbolDescriptor> AllSymbols;

        /// <summary>
        /// A helper function for "sets computation".
        /// </summary>
        /// <param name="isAtom"></param>
        /// <param name="isComplement"></param>
        /// <returns></returns>
        static SymbolDescriptor.CompileDelegate CompileSet(bool isAtom, bool isComplement)
        {
            if (isAtom)
            {
                return new SymbolDescriptor.CompileDelegate((f, c) => new AtomSetQuery(f.Arguments.Select(a => a.GetString()), isComplement));
            }

            return new SymbolDescriptor.CompileDelegate((f, c) => new ResidueSetQuery(f.Arguments.Select(a => a.GetString()), isComplement));
        }

        ///// <summary>
        ///// Empty symbol. Not really used right now.
        ///// More of a relic of the past, but it might be useful again later.
        ///// </summary>      
        //public static SymbolDescriptor EmptySymbol = new SymbolDescriptor { Name = "Empty", Type = null };


        ///// <summary>
        ///// Motive.
        ///// </summary>
        //public static SymbolDescriptor MotiveTypeSymbol = new SymbolDescriptor
        //{
        //    Name = "MotiveType",
        //    Type = BasicTypes.Pattern,
        //    Description = new SymbolDescription
        //    {
        //        IsInternal = true,
        //        Category = SymbolCategories.MotiveTypes,
        //        Description = "A motive is defined as a set of atoms."
        //    }
        //};

        /// <summary>
        /// Object value.
        /// </summary>
        public static SymbolDescriptor ObjectValueSymbol = new SymbolDescriptor
        {
            Name = "Object",
            Type = BasicTypes.Value,
            Description = new SymbolDescription { IsInternal = true, Category = SymbolCategories.ElementaryTypes, Description = "Arbitrary object value", Examples = Examples() }
        };

        /// <summary>
        /// Boolean.
        /// </summary>
        public static SymbolDescriptor BoolSymbol = new SymbolDescriptor
        {
            Name = "Bool",
            Type = BasicTypes.Bool,
            Description = new SymbolDescription { IsInternal = true, Category = SymbolCategories.ElementaryTypes, Description = "True or False value", Examples = Examples(new Dictionary<string, string> { { "True", "True value." } }) }
        };

        /// <summary>
        /// Integer.
        /// </summary>
        public static SymbolDescriptor IntegerSymbol = new SymbolDescriptor
        {
            Name = "Integer",
            Type = BasicTypes.Integer,
            Description = new SymbolDescription { IsInternal = true, Category = SymbolCategories.ElementaryTypes, Description = "Integer value.", Examples = Examples(new Dictionary<string, string> { { "42", "The ultimate answer." } }) }
        };

        /// <summary>
        /// Integer.
        /// </summary>
        public static SymbolDescriptor StaticMatrixSymbol = new SymbolDescriptor
        {
            Name = "StaticMatrix",
            Type = BasicTypes.StaticMatrix,
            Description = new SymbolDescription { IsInternal = true, Category = SymbolCategories.ElementaryTypes, Description = "Static matrix value.", Examples = Examples(new Dictionary<string, string> { { "[[1,2],[3,4]]", "2x2 matrix." } }) }
        };

        /// <summary>
        /// Real (double type)
        /// </summary>
        public static SymbolDescriptor RealSymbol = new SymbolDescriptor
        {
            Name = "Real",
            Type = BasicTypes.Real,
            Description = new SymbolDescription { IsInternal = true, Category = SymbolCategories.ElementaryTypes, Description = "Real value.", Examples = Examples(new Dictionary<string, string> { { "12.87", "Real number." } }) }
        };

        /// <summary>
        /// String.
        /// </summary>
        public static SymbolDescriptor StringSymbol = new SymbolDescriptor
        {
            Name = "String",
            Type = BasicTypes.String,
            Description = new SymbolDescription
            {
                IsInternal = true,
                Category = SymbolCategories.ElementaryTypes,
                Description = "String value. Must be enclosed in double (\") quotes.",
                Examples = Examples(new Dictionary<string, string> { { "\"aBcD\"", "String." } })
            }
        };

        /// <summary>
        /// Symbol.
        /// </summary>
        public static SymbolDescriptor SymbolSymbol = new SymbolDescriptor
        {
            Name = "Symbol",
            //Arguments = new FunctionArgument[]
            //{
            //    new FunctionArgument { Name = "name", Description = "Symbol name.", Type = BasicTypes.String },
            //},
            Type = BasicTypes.Symbol,
            Description = new SymbolDescription
            {
                IsInternal = true,
                Category = SymbolCategories.ElementaryTypes,
                Description = "Identifier. In some contexts, such as atom or residue names, equivalent to the String type.",
                Examples = Examples()
            },
            Normalize = f => new MetaSymbol(f.Arguments[0].GetString())
        };

        /// <summary>
        /// Tuple.
        /// </summary>
        public static SymbolDescriptor TupleSymbol = new SymbolDescriptor
        {
            Name = "Tuple",
            //Arguments = new FunctionArgument[]
            //{
            //    new FunctionArgument { Name = "elements", Description = "Elements.", Type = TypeMany.Create(TypeWildcard.Instance) },
            //},
            Type = TypeWildcard.Instance,
            Description = new SymbolDescription
            {
                IsInternal = true,
                Category = SymbolCategories.LanguagePrimitives,
                Description = "A tuple of elements. Tuples serve as arguments for functions. Internal use only.",
                Examples = Examples()
            }
        };


        /// <summary>
        /// A list of elements.
        /// </summary>
        public static SymbolDescriptor ListSymbol = new SymbolDescriptor
        {
            Name = "List",
            Arguments = new FunctionArgument[]
            {
                new FunctionArgument { Name = "elements", Description = "Elements.", Type = TypeMany.Create(TypeWildcard.Instance) },
            },
            Type = BasicTypes.List,
            Description = new SymbolDescription
            {
                Category = SymbolCategories.LanguagePrimitives,
                Description = "A list of elements.",
                Examples = Examples(new Dictionary<string, string> { { "[1, \"a\", True, [3, 4]]", "Create a list with 4 elements." } })
            },
            Compile = (f, c) => new QueryList(f.Arguments.Select(a => a.Compile(c)))
        };

        /// <summary>
        /// Lambda.
        /// </summary>
        public static SymbolDescriptor LambdaSymbol = new SymbolDescriptor
        {
            Name = "Lambda",
            Type = TypeExpression.Function().Arg("a").Return("b"),
            Description = new SymbolDescription
            {
                Category = SymbolCategories.LanguagePrimitives,
                //OperatorForm = "=>",
                Description = "An anonymous (nameless) function.",
                Examples = Examples(new Dictionary<string, string> { { "lambda m: Residues(\"HIS\").Count(m)", "A function that counts number of HIS residues in Motive m." } })
            }
        };

        ///// <summary>
        ///// Lambda.
        ///// </summary>
        //public static SymbolDescriptor MemberDot = new SymbolDescriptor
        //{
        //    Name = "MemberDot",
        //    Type =  //TypeExpression.Function().Arg("a").Arg(TypeExpression.Function().Arg("a").Return("b")).Return(TypeExpression.Function().Arg("a").Return("b")),
        //    Description = new SymbolDescription
        //    {
        //        Category = SymbolCategories.LanguagePrimitives,
        //        OperatorForm = ".",
        //        Description = "Member function. Transforms a.Func(...) to Func(a, ...)."
        //    },
        //    Normalize = f => MetaQuery.CreateApply(f.Arguments[1].Head, MetaQuery.CreateTuple(new[] { f.Arguments[0] }.Concat((f.Arguments[1] as MetaApply).Arguments).ToArray()))

        //        //MetaQuery.CreateSymbol(SequenceSymbol.Name).ApplyTo(Enumerable.Range(0, f.Arguments[1].GetInteger()).Select(a => f.Arguments[0]).ToArray())
        //};

        /// <summary>
        /// Apply.
        /// </summary>
        public static SymbolDescriptor ApplySymbol = new SymbolDescriptor
        {
            Name = "Apply",
            //Arguments = new FunctionArgument[]
            //{
            //    new FunctionArgument { Name = "f", Description = "Function to apply.", Type = TypeArrow.Create(TypeVariable.Create("a"), TypeVariable.Create("b")) },
            //    new FunctionArgument { Name = "args", Description = "Arguments.", Type = TypeVariable.Create("a") }
            //},
            Type = TypeWildcard.Instance,
            Description = new SymbolDescription
            {
                IsInternal = true,
                Category = SymbolCategories.LanguagePrimitives,
                Description = "Application of a function to its arguments.",
                Examples = Examples()
            }
        };

        /////// <summary>
        /////// Pipe operator
        /////// </summary>
        ////public static SymbolDescriptor PipeSymbol = new SymbolDescriptor
        ////{
        ////    Name = "Pipe",
        ////    //Arguments = new FunctionArgument[]
        ////    //{
        ////    //    new FunctionArgument { Name = "f", Description = "Function to apply.", Type = TypeArrow.Create(TypeVariable.Create("a"), TypeVariable.Create("b")) },
        ////    //    new FunctionArgument { Name = "args", Description = "Arguments.", Type = TypeVariable.Create("a") }
        ////    //},
        ////    Type = TypeWildcard.Instance,
        ////    Description = new SymbolDescription
        ////    {
        ////        Category = SymbolCategories.LanguagePrimitives,
        ////        OperatorForm = "|>",
        ////        Description = "An alternative way to apply functions.",
        ////        Examples = Examples(new Dictionary<string, string> { {  "Atoms[Fe] |> Named",
        ////        ExampleDescription = "Same as writing <code>Named[Atoms[Fe]]</code>."
        ////    }
        ////};

        /// <summary>
        /// Options get special treatment.
        /// </summary>
        public static SymbolDescriptor AssignSymbol = new SymbolDescriptor
        {
            Name = "Assign",
            Type = TypeWildcard.Instance,
            Description = new SymbolDescription
            {
                IsInternal = true,
                Category = SymbolCategories.LanguagePrimitives,
                OperatorForm = "=",
                Description = "This symbol is used for assigning optional parameters of functions.",
                Examples = Examples(new Dictionary<string, string> { { "NotAminoAcids(NoWaters = 1)", "All residues that are not amino acids or waters." } })
            }
        };

        /// <summary>
        /// Let construction.
        /// </summary>
        public static SymbolDescriptor LetSymbol = new SymbolDescriptor
        {
            Name = "Let",
            Type = TypeWildcard.Instance,
            Description = new SymbolDescription
            {
                IsInternal = true,
                Category = SymbolCategories.LanguagePrimitives,
                Description = "Let bindings allow to define local 'variables'.",
                Examples = Examples()
            }
        };

        /// <summary>
        /// Sequences automatically flatten into functions.
        /// </summary>
        public static SymbolDescriptor SequenceSymbol = new SymbolDescriptor
        {
            Name = "Sequence",
            Arguments = new FunctionArgument[]
            {
                new FunctionArgument { Name = "xs", Description = "Values.", Type = TypeMany.Create(TypeWildcard.Instance, allowEmpty: true) },
            },
            Type = TypeWildcard.Instance,
            Description = new SymbolDescription
            {
                IsInternal = true,
                Category = SymbolCategories.LanguagePrimitives,
                Description = "Sequence of elements that is automatically flattened to the argument list of the parent function.",
                Examples = Examples()
            }
        };

        /// <summary>
        /// Repeat[x, n] = Sequence[x,...,x];
        /// </summary>
        public static SymbolDescriptor RepeatSymbol = new SymbolDescriptor
        {
            Name = "Repeat",
            Arguments = new FunctionArgument[]
            {
                new FunctionArgument { Name = "x", Description = "Expression to be repeated.", Type = TypeWildcard.Instance },
                new FunctionArgument { Name = "n", Description = "Count.", Type = BasicTypes.Integer }
            },
            Type = TypeWildcard.Instance,
            Description = new SymbolDescription
            {
                IsInternal = true,
                Category = SymbolCategories.LanguagePrimitives,
                OperatorForm = "*",
                Description = "Creates a Sequence of x's repeated n times. This symbol is not directly supported and can be accessed through the function Many or List concatenation of Python.",
                Examples = Examples(new Dictionary<string, string> { { "Rings(5 * [\"C\"] + [\"O\"])", "Equivalent to <code>Rings([\"C\",\"C\",\"C\",\"C\",\"C\",\"O\"])</code>." } })
            },
            Normalize = f => MetaQuery.CreateSymbol(SequenceSymbol.Name).ApplyTo(Enumerable.Range(0, f.Arguments[1].GetInteger()).Select(a => f.Arguments[0]).ToArray())
        };

        /// <summary>
        /// Pattern symbol.
        /// </summary>
        public static SymbolDescriptor MotiveSymbol = new SymbolDescriptor
        {
            Name = "Pattern",
            Arguments = new FunctionArgument[] { new FunctionArgument { Name = "structureName", Description = "Name of a structure.", Type = BasicTypes.String } },
            Type = BasicTypes.Pattern,
            Description = new SymbolDescription
            {
                Category = SymbolCategories.MiscFunctions,
                Description = "Returns a structure represented as a pattern.",
                Examples = Examples(new Dictionary<string, string> { { "Pattern(\"1tqn_12\")", "Returns the structure '1tqn_12' represented as a pattern. Usable in defining descriptors or in Scripting window (using MQ.Execute)." } })
            },
            Compile = (f, _) => new StructureQueries(f.Arguments[0].GetString())
        };

        static Dictionary<string, MetaOption> CreateOptions(params MetaOption[] options)
        {
            return options.ToDictionary(o => o.Name, StringComparer.OrdinalIgnoreCase);
        }

        static string GetOpString(RelationOp op)
        {
            switch (op)
            {
                case RelationOp.Equal: return "==";
                case RelationOp.NotEqual: return "!=";
                case RelationOp.Less: return "<";
                case RelationOp.LessEqual: return "<=";
                case RelationOp.Greater: return ">";
                case RelationOp.GreaterEqual: return ">=";
            }

            return op.ToString();
        }

        static string GetOpString(LogicalOp op)
        {
            switch (op)
            {
                case LogicalOp.LogicalAnd: return "&";
                case LogicalOp.LogicalOr: return "|";
            }

            return op.ToString();
        }


        public static MetaOption ProbeRadiusOption = new MetaOption { Name = "ProbeRadius", DefaultValue = MetaQueries.MetaQuery.CreateValue(3.0), Type = BasicTypes.Real, Description = "Used to determine the molecular surface." };
        public static MetaOption InteriorThresholdOption = new MetaOption { Name = "InteriorThreshold", DefaultValue = MetaQueries.MetaQuery.CreateValue(1.25), Type = BasicTypes.Real, Description = "Used to determine cavities in the molecule." };
        public static MetaOption BottleneckRadiusOption = new MetaOption { Name = "BottleneckRadius", DefaultValue = MetaQueries.MetaQuery.CreateValue(1.25), Type = BasicTypes.Real, Description = "Used to determine the narrowest point of a tunnel." };

        public static MetaOption IgnoreWatersOption = new MetaOption { Name = "NoWaters", DefaultValue = MetaQueries.MetaQuery.CreateValue(true), Type = BasicTypes.Bool, Description = "Ignore water residues such as HOH." };
        public static MetaOption ExcludeBaseOption = new MetaOption { Name = "ExcludeBase", DefaultValue = MetaQueries.MetaQuery.CreateValue(false), Type = BasicTypes.Bool, Description = "Exclude the central original pattern." };
        public static MetaOption YieldNamedDuplicatesOption = new MetaOption { Name = "YieldNamedDuplicates", DefaultValue = MetaQueries.MetaQuery.CreateValue(false), Type = BasicTypes.Bool, Description = "Yield duplicate patterns if they have a different name." };
        public static MetaOption ChargeTypeOption = new MetaOption
        {
            Name = "ChargeType",
            DefaultValue = MetaQueries.MetaQuery.CreateValue(""),
            Type = BasicTypes.String,
            Description = string.Format("Specify type of the charge. Allowed values: {0}.", string.Join(", ", Enum.GetNames(typeof(ResidueChargeType)).Where(n => n != "Unknown")))
        };
        public static MetaOption RegularMotifTypeOption = new MetaOption
        { 
            Name = "Type", 
            DefaultValue = MetaQueries.MetaQuery.CreateValue("Amino"), 
            Type = BasicTypes.String, Description = "Determines the type of the query. Allowed values: " + EnumHelper.GetNames<RegexQueryTypes>().JoinBy() + "." 
        };

        public static MetaOption DistanceMatrixMinOption = new MetaOption
        {
            Name = "DistanceMin",
            DefaultValue = MetaQueries.MetaQuery.CreateValue(new[] { new [] { 0.0 } }),
            Type = BasicTypes.StaticMatrix,
            Description = "Lower triangle (without diagonal) of the distance matrix with minimum distances."
        };

        public static MetaOption DistanceMatrixMaxOption = new MetaOption
        {
            Name = "DistanceMax",
            DefaultValue = MetaQueries.MetaQuery.CreateValue(new[] { new[] { 0.0 } }),
            Type = BasicTypes.StaticMatrix,
            Description = "Lower triangle (without diagonal) of the distance matrix with maximum distances."
        };

        static SymbolExample[] Examples(Dictionary<string, string> examples = null)
        {
            if (examples == null) return new SymbolExample[0];
            return examples.Select(e => new SymbolExample { ExampleCode = e.Key, ExampleDescription = e.Value }).ToArray();
        }

        static SymbolTable()
        {
            const SymbolAttributes SetTraits = SymbolAttributes.Flat | SymbolAttributes.UniqueArgs | SymbolAttributes.Orderless;

            var equalityType = TypeExpression.Function().Arg(BasicTypes.Value).Arg(BasicTypes.Value).Return(BasicTypes.Bool);
            var relationType = TypeExpression.Function().Arg(BasicTypes.Number).Arg(BasicTypes.Number).Return(BasicTypes.Bool);
            var logicalType = TypeExpression.Function().Many(BasicTypes.Bool).Return(BasicTypes.Bool);

            Func<RelationOp, SymbolDescriptor> relation = op => new SymbolDescriptor
            {
                Name = op.ToString(),
                Type = BasicTypes.Bool,
                Arguments = op == RelationOp.Equal || op == RelationOp.NotEqual
                    ? new FunctionArgument[] { new FunctionArgument { Name = "x", Description = "Left argument.", Type = BasicTypes.Value }, new FunctionArgument { Name = "y", Description = "Right argument.", Type = BasicTypes.Value } }
                    : new FunctionArgument[] { new FunctionArgument { Name = "x", Description = "Left argument.", Type = BasicTypes.Number }, new FunctionArgument { Name = "y", Description = "Right argument.", Type = BasicTypes.Number } },
                Description = new SymbolDescription
                {
                    Category = SymbolCategories.ValueFunctions,
                    OperatorForm = GetOpString(op),
                    Description = "Determines the '" + op.ToString() + "' relation between two values.",
                    Examples = Examples(new Dictionary<string, string> { { "x " + GetOpString(op) + " y", "Evaluates to True or False based on the value of x and y." } })
                },
                Compile = (f, c) => new BinaryFunctionQuery(op.ToString(), DynamicFunctions.RelationOpFunctions[op], f.Arguments[0].Compile(c), f.Arguments[1].Compile(c))
            };

            Func<string, string, Func<dynamic, dynamic, dynamic>, SymbolDescriptor> binaryOp = (name, op, func) => new SymbolDescriptor
            {
                Name = name,
                Type = BasicTypes.Number,
                Arguments = new FunctionArgument[] 
                { 
                    new FunctionArgument { Name = "x", Description = "Left argument.", Type = BasicTypes.Number }, 
                    new FunctionArgument { Name = "y", Description = "Right argument.", Type = BasicTypes.Number } 
                },
                Description = new SymbolDescription
                {
                    Category = SymbolCategories.ValueFunctions,
                    OperatorForm = op,
                    Description = "Computes the '" + name + "' function of the values.",
                    Examples = Examples(new Dictionary<string, string> { { "x " + op + " y", "Evaluates the expression." } })
                },
                Compile = (f, c) => new BinaryFunctionQuery(name, func, f.Arguments[0].Compile(c), f.Arguments[1].Compile(c))
            };

            Func<string, Func<dynamic, dynamic>, TypeExpression, TypeExpression, SymbolDescriptor> unaryFunction = (name, func, arg, ret) => new SymbolDescriptor
            {
                Name = name,
                Type = ret,
                Arguments = new FunctionArgument[] 
                { 
                    new FunctionArgument { Name = "x", Description = "Argument.", Type = arg }
                },
                Description = new SymbolDescription
                {
                    Category = SymbolCategories.ValueFunctions,
                    Description = "Computes the '" + name + "' function of the argument.",
                    Examples = Examples(new Dictionary<string, string> { { name + "(x)", "Evaluates the expression." } })
                },
                Compile = (f, c) => new UnaryFunctionQuery(name, func, f.Arguments[0].Compile(c))
            };

            Func<LogicalOp, SymbolDescriptor> logicalAndOrOr = op => new SymbolDescriptor
            {
                Name = op.ToString(),
                Type = BasicTypes.Bool,
                Arguments = new FunctionArgument[] { new FunctionArgument { Name = "xs", Description = "Arguments.", Type = TypeMany.Create(BasicTypes.Bool) } },
                Attributes = SymbolAttributes.Flat | SymbolAttributes.Orderless | SymbolAttributes.OneIdentity | SymbolAttributes.UniqueArgs,
                Description = new SymbolDescription
                {
                    Category = SymbolCategories.ValueFunctions,
                    OperatorForm = GetOpString(op),
                    Description = "Computes '" + op.ToString() + "' of the input values.",
                    Examples = Examples(new Dictionary<string, string> { { "x " + GetOpString(op) + " y", "Evaluates to True or False based on the values of x and y." } })
                },
                Compile = (f, c) => new NaryFunctionQuery(op.ToString(), DynamicFunctions.NAryLogicalFunctions[op], f.Arguments.Select(a => a.Compile(c)))
            };


            Func<string, string, string, TypeExpression, Func<PdbMetadata, object>, SymbolDescriptor> metadataProperty = (name, desc, exampleValue, type, getter) => new SymbolDescriptor
            {
                Name = name,
                Arguments = new FunctionArgument[] 
                {
                    new FunctionArgument { Name = "pattern", Description = "Pattern.", Type = BasicTypes.Pattern },
                },
                Type = type,
                Description = new SymbolDescription
                {
                    IsDotSyntax = true,
                    IgnoreForAutoCompletion = true,
                    Category = SymbolCategories.MetadataFunctions,
                    Description = desc + " If the value is not available, null is returned.",
                    Examples = Examples(new Dictionary<string, string> 
                    { 
                        { string.Format("Residues().ExecuteIf(lambda p: p.{0}() {1})", name, exampleValue), "Returns residues in structures that satisfy the property." },
                    })
                },
                Compile = (f, c) => new LambdaPdbMetadataQuery(name, f.Arguments[0].Compile(c), getter)
            };

            Func<string, string, TypeExpression, Func<PdbMetadata, object>, SymbolDescriptor> metadataPropertyNoExample = (name, desc, type, getter) => new SymbolDescriptor
            {
                Name = name,
                Arguments = new FunctionArgument[] 
                {
                    new FunctionArgument { Name = "pattern", Description = "Pattern.", Type = BasicTypes.Pattern },
                },
                Type = type,
                Description = new SymbolDescription
                {
                    IsDotSyntax = true,
                    IgnoreForAutoCompletion = true,
                    Category = SymbolCategories.MetadataFunctions,
                    Description = desc + " If the value is not available, null is returned."
                },
                Compile = (f, c) => new LambdaPdbMetadataQuery(name, f.Arguments[0].Compile(c), getter)
            };

            Func<string, string, string[], bool, Func<PdbMetadata, string[]>, SymbolDescriptor> metadataHasAllProperties = (name, friendlyName, examples, caseSensitive, getter) => new SymbolDescriptor
            {
                Name = "HasAll" + name,
                Arguments = new FunctionArgument[] 
                {
                    new FunctionArgument { Name = "pattern", Description = "Pattern.", Type = BasicTypes.Pattern },
                    new FunctionArgument { Name = "properties", Description = "Properties.", Type = TypeMany.Create(BasicTypes.String) }
                },
                Type = BasicTypes.Bool,
                Description = new SymbolDescription
                {
                    IsDotSyntax = true,
                    IgnoreForAutoCompletion = true,
                    Category = SymbolCategories.MetadataFunctions,
                    Description = string.Format("Determines if the parent structure contains all given {0}. The comparison is {1}case sensitive.", friendlyName, caseSensitive ? "" : "not "),
                    Examples = Examples(new Dictionary<string, string> 
                    { 
                        { string.Format("Residues().ExecuteIf(lambda p: p.HasAll{0}({1}))", name, examples.JoinBy(e => "\"" + e + "\"")), "Returns residues in structures that contain all given properties." },
                    })
                },
                Compile = (f, c) => new HasMetadataPropertiesQuery(name, getter, caseSensitive, false, f.Arguments[0].Compile(c), f.Arguments.Skip(1).Select(a => a.GetString()))
            };

            Func<string, string, string[], bool, Func<PdbMetadata, string[]>, SymbolDescriptor> metadataHasAnyProperty = (name, friendlyName, examples, caseSensitive, getter) => new SymbolDescriptor
            {
                Name = "HasAny" + name,
                Arguments = new FunctionArgument[] 
                {
                    new FunctionArgument { Name = "pattern", Description = "Pattern.", Type = BasicTypes.Pattern },
                    new FunctionArgument { Name = "properties", Description = "Properties.", Type = TypeMany.Create(BasicTypes.String) }
                },
                Type = BasicTypes.Bool,
                Description = new SymbolDescription
                {
                    IsDotSyntax = true,
                    IgnoreForAutoCompletion = true,
                    Category = SymbolCategories.MetadataFunctions,
                    Description = string.Format("Determines if the parent structure contains any of the given {0}s. The comparison is {1}case sensitive.", friendlyName, caseSensitive ? "" : "not "),
                    Examples = Examples(new Dictionary<string, string> 
                    { 
                        { string.Format("Residues().ExecuteIf(lambda p: p.HasAny{0}({1}))", name, examples.JoinBy(e => "\"" + e + "\"")), "Returns residues in structures that contain all given properties." },
                    })
                },
                Compile = (f, c) => new HasMetadataPropertiesQuery(name, getter, caseSensitive, true, f.Arguments[0].Compile(c), f.Arguments.Skip(1).Select(a => a.GetString()))
            };

            var symbols = new SymbolDescriptor[] 
            {
                ///////////////////////////////////////////////////////////////////
                // META LANGUAGE "HELPERS"
                ///////////////////////////////////////////////////////////////////
                LambdaSymbol,
                //LetSymbol,
                TupleSymbol,
                ListSymbol,
                ApplySymbol,
                //PipeSymbol,
                AssignSymbol,
                SequenceSymbol,
                RepeatSymbol,

                ///////////////////////////////////////////////////////////////////
                // "PRIMITIVES"
                ///////////////////////////////////////////////////////////////////
               
                //MotiveTypeSymbol,
                //MotiveSeqTypeSymbol,

                //EmptySymbol,
                ObjectValueSymbol,
                BoolSymbol,
                IntegerSymbol,
                RealSymbol,
                StringSymbol,
                StaticMatrixSymbol,
                SymbolSymbol,
                
                ///////////////////////////////////////////////////////////////////
                // BACIS ELEMENTS
                ///////////////////////////////////////////////////////////////////
                
                new SymbolDescriptor
                {
                    Name = "Atoms",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "symbols", Description = "Allowed element symbols.", Type = TypeMany.Create(BasicTypes.String, allowEmpty: true) } },
                    Type = BasicTypes.Atoms,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of atoms with specified element symbols. If no symbols are specified, yields all atoms one by one.",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms(\"Zn\",\"Ca\")", "Returns all atoms with element symbol Zn or Ca" } } )
                    },
                    Compile = CompileSet(true, false)
                },
                new SymbolDescriptor
                {
                    Name = "NotAtoms",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "symbols", Description = "Forbidden element symbols.", Type = TypeMany.Create(BasicTypes.String) } },
                    Type = BasicTypes.Atoms,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of atoms that are not particular elements.",
                        Examples = Examples(new Dictionary<string, string> { {  "NotAtoms(\"O\")", "Returns all atoms that are not O." } } )
                    },
                    Compile = CompileSet(true, true)
                },
                new SymbolDescriptor
                {
                    Name = "AtomNames",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "names", Description = "Allowed names.", Type = TypeMany.Create(BasicTypes.String) } },
                    Type = BasicTypes.Atoms,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of atoms with specified names.",
                        Examples = Examples(new Dictionary<string, string> { {  "AtomNames(\"O1\",\"NH1\")", "Returns all atoms with names O1 or NH1." } } )
                    },
                    Compile = (f, c) => new AtomNamesQuery(f.Arguments.Select(a => a.GetString()), false)
                },
                new SymbolDescriptor
                {
                    Name = "NotAtomNames",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "names", Description = "Forbidden names.", Type = TypeMany.Create(BasicTypes.String) } },
                    Type = BasicTypes.Atoms,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of atoms that do not have a specified name.",
                        Examples = Examples(new Dictionary<string, string> { {  "NotAtomNames(\"O4\")", "Returns all atoms that are not called O4." } } )
                    },
                    Compile = (f, c) => new AtomNamesQuery(f.Arguments.Select(a => a.GetString()), true)
                },
                new SymbolDescriptor
                {
                    Name = "AtomIds",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "ids", Description = "Identifiers.", Type = TypeMany.Create(BasicTypes.Integer) } },
                    Type = BasicTypes.Atoms,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of atoms with specified identifiers.",
                        Examples = Examples(new Dictionary<string, string> { {  "AtomIds(1, 2, 3)", "Returns atoms with ids 1, 2, 3." } } )
                    },
                    Compile = (f, c) => new AtomIdsQuery(f.Arguments.Select(a => a.GetInteger()), false)
                },
                new SymbolDescriptor
                {
                    Name = "NotAtomIds",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "ids", Description = "Identifiers.", Type = TypeMany.Create(BasicTypes.Integer) } },
                    Type = BasicTypes.Atoms,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of atoms that do not have specified identifiers.",
                        Examples = Examples(new Dictionary<string, string> { {  "NotAtomIds(1, 2, 3)", "Returns atoms that do not have id 1, 2, nor 3." } } )
                    },
                    Compile = (f, c) => new AtomIdsQuery(f.Arguments.Select(a => a.GetInteger()), true)
                },
                new SymbolDescriptor
                {
                    Name = "AtomIdRange",
                    Arguments = new FunctionArgument[] 
                    { 
                        new FunctionArgument { Name = "minId", Description = "Minimum id.", Type = BasicTypes.Integer } ,
                        new FunctionArgument { Name = "maxId", Description = "Maximum id. If not specified, maxId = minId.", Type = TypeMany.Create(BasicTypes.Integer, isOption: true) }
                    },
                    Type = BasicTypes.Atoms,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of atoms with minId <= atomId <= maxId.",
                        Examples = Examples(new Dictionary<string, string> { {  "AtomIdRange(152, 161)", "Returns all atoms with id between 152 and 161 inclusive." } } )
                    },
                    Compile = (f, c) => f.Arguments.Length == 1 
                        ? new AtomIdRangeQuery(f.Arguments[0].GetInteger(), f.Arguments[0].GetInteger())
                        : new AtomIdRangeQuery(f.Arguments[0].GetInteger(), f.Arguments[1].GetInteger())
                },
                new SymbolDescriptor
                {
                    Name = "Residues",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "names", Description = "Allowed residue names.", Type = TypeMany.Create(BasicTypes.String, allowEmpty: true) } },
                    Type = BasicTypes.Residues,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        //OperatorForm = "#",
                        Description = "Sequence of residues with specified names. If no names are specified, yields all residues one by one.",
                        Examples = Examples(new Dictionary<string, string> { {  "Residues(\"HIS\", \"CYS\")", "Returns all HIS or CYS residues." } } )
                    },
                    Compile = CompileSet(false, false)
                },
                new SymbolDescriptor
                {
                    Name = "NotResidues",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "names", Description = "Forbidden residue names.", Type = TypeMany.Create(BasicTypes.Value) } },
                    Type = BasicTypes.Residues,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of residues that are not called by the specified names.",
                        Examples = Examples(new Dictionary<string, string> { {  "NotResidues(\"THR\",\"CYS\")", "Returns all residues that are not THR or CYS." } } )
                    },
                    Compile = CompileSet(false, true)
                },
                new SymbolDescriptor
                {
                    Name = "ModifiedResidues",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "parentNames", Description = "Parent residue names.", Type = TypeMany.Create(BasicTypes.String, allowEmpty: true) } },
                    Type = BasicTypes.Residues,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        //OperatorForm = "#",
                        Description = "Sequence of modified residues that originate from the specified name. If no names are specified, yields all modified residues one by one.",
                        Examples = Examples(new Dictionary<string, string> { {  "ModifiedResidues(\"MET\")", "Returns all residues modified from MET (for example MSE)." } } )
                    },
                    Compile = (f, c) => new ModifiedResiduesQuery(f.Arguments.Select(a => a.GetString()))
                },
                new SymbolDescriptor
                {
                    Name = "ResidueIdRange",
                    Arguments = new FunctionArgument[] 
                    { 
                        new FunctionArgument { Name = "chain", Description = "Chain identifier. Case sensitive (a != A).", Type = BasicTypes.String } ,
                        new FunctionArgument { Name = "min", Description = "Minimum sequence number.", Type = BasicTypes.Integer } ,
                        new FunctionArgument { Name = "max", Description = "Maximum sequence number. If not specified, max = min.", Type = TypeMany.Create(BasicTypes.Integer, isOption: true) }
                    },
                    Type = BasicTypes.Residues,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of residues with specific chain and min <= sequence number <= max.",
                        Examples = Examples(new Dictionary<string, string> { {  "ResidueIdRange(\"A\", 161, 165)", "Returns all residues on chain A with seq. number between 161 and 165 inclusive." } } )
                    },
                    Compile = (f, c) => 
                    {
                        var chain = f.Arguments[0].GetString().Trim();
                        return f.Arguments.Length == 2
                            ? new ResidueIdRangeQuery(chain, f.Arguments[1].GetInteger(), f.Arguments[1].GetInteger())
                            : new ResidueIdRangeQuery(chain, f.Arguments[1].GetInteger(), f.Arguments[2].GetInteger());
                    }
                },
                new SymbolDescriptor
                {
                    Name = "ResidueIds",
                    Arguments = new FunctionArgument[] 
                    { 
                        new FunctionArgument { Name = "ids", Description = "One or more identifiers in the format 'NUMBER [CHAIN] [i:INSERTIONCODE]' (parameters in [] are optional, for example '175 i:12' or '143 B'). Case sensitive (a != A).", Type = TypeMany.Create(BasicTypes.String) }
                    },
                    Type = BasicTypes.Residues,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of residues with specific identifiers. If the structure does not contain a residue with the given identifier, it is skipped.",
                        Examples = Examples(new Dictionary<string, string> { {  "ResidueIds(\"132 A\", \"178 A\")", "Returns residues A 123 and A 178 (provided the input structure contains them)." } } )
                    },
                    Compile = (f, c) => new ResidueIdsQuery(f.Arguments.Select(a => a.GetString()))
                },

                ////new SymbolDescriptor
                ////{
                ////    Name = "ResidueCategory",
                ////    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "categories", Description = "Desired categories.", Type = TypeMany.Create(BasicTypes.Value, allowEmpty: false) } },
                ////    Type = BasicTypes.Residues,
                ////    Attributes = SetTraits,
                ////    Description = new SymbolDescription
                ////    {
                ////        Category = SymbolCategories.BasicMotiveFunctions,
                ////        Description = "Sequence of residues with names that fall into a specific category or categories. Allowed categories are: " + EnumHelper.GetNames<QueriesResidueCategories>().JoinBy(", ") + ".",
                ////        Examples = Examples(new Dictionary<string, string> 
                ////        { 
                ////            {  "ResidueCategory(\"AminoAcids\")", "Returns all amino acids." } ,
                ////            {  "ResidueCategory(\"Ligands\", \"Nucleotides\")", "Returns all ligands or nucleotides." } 
                ////        })
                ////    },
                ////    Compile = (f, c) => new ResidueCategoryQuery(f.Arguments.Select(a => a.GetString()), false)
                ////},

                ////new SymbolDescriptor
                ////{
                ////    Name = "NotResidueCategory",
                ////    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "categories", Description = "Not desired categories.", Type = TypeMany.Create(BasicTypes.Value, allowEmpty: false) } },
                ////    Type = BasicTypes.Residues,
                ////    Attributes = SetTraits,
                ////    Description = new SymbolDescription
                ////    {
                ////        Category = SymbolCategories.BasicMotiveFunctions,
                ////        Description = "Sequence of residues with names that do NOT fall into a specific category or categories. Allowed categories are: " + EnumHelper.GetNames<QueriesResidueCategories>().JoinBy(", ") + ".",
                ////        Examples = Examples(new Dictionary<string, string> 
                ////        { 
                ////            {  "NotResidueCategory(\"AminoAcids\")", "Returns all residues that are not amino acids." } ,
                ////            {  "NotResidueCategory(\"Ligands\", \"Nucleotides\")", "Returns all residues that are not ligands or nucleotides." } 
                ////        })
                ////    },
                ////    Compile = (f, c) => new ResidueCategoryQuery(f.Arguments.Select(a => a.GetString()), true)
                ////},

                new SymbolDescriptor
                {
                    Name = "AminoAcids",
                    Type = BasicTypes.Residues,
                    Options = CreateOptions(ChargeTypeOption),
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of all residues with the 20 basic amino acid names.",
                        Examples = Examples(new Dictionary<string, string> 
                        { 
                            {  "AminoAcids()", "All amino acids." },
                            {  string.Format("AminoAcids(ChargeType = \"{0}\")", ResidueChargeType.Polar), "Amino acids with polar charge." } 
                        } )
                    },
                    Compile = (f, c) => new AminoAcidQuery(f.Options[ChargeTypeOption.Name].GetString())
                },
                new SymbolDescriptor
                {
                    Name = "NotAminoAcids",
                    Type = BasicTypes.Residues,
                    Options = CreateOptions(IgnoreWatersOption),
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of all residues that are not any of the 20 basic amino acids.",
                        Examples = Examples(new Dictionary<string, string> { {  "NotAminoAcids()", "Returns all residues that are not amino acids." } } )
                    },
                    Compile = (f, c) => new NotAminoAcidQuery(f.Options[IgnoreWatersOption.Name].GetBool())
                },
                new SymbolDescriptor
                {
                    Name = "HetResidues",
                    Type = BasicTypes.Residues,
                    Options = CreateOptions(IgnoreWatersOption),
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of all residues that contain HET atoms.",
                        Examples = Examples(new Dictionary<string, string> 
                        { 
                            {  "HetResidues()", "Returns all residues that contain HET atoms (ignores water)." } ,
                            {  "HetResidues(NoWaters=False)", "Returns all residues that contain HET atoms (includes water)." } 
                        })
                    },
                    Compile = (f, c) => new HetResiduesQuery(f.Options[IgnoreWatersOption.Name].GetBool())
                },

                new SymbolDescriptor
                {
                    Name = "Rings",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "atoms", Description = "Ring atoms.", Type = TypeMany.Create(BasicTypes.Value, allowEmpty: true) } },
                    Type = BasicTypes.Rings,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Sequence of rings with particular atoms. If no atoms are specified, yields all rings (cycles) one by one. The order of atoms matters.",
                        Examples = Examples(new Dictionary<string, string>
                        {
                            {  "Rings()", "Returns all rings." },
                            {  "Rings(5 * [\"C\"] + [\"O\"])", "Returns all rings with 5C and 1O atoms." } ,
                            {  "Rings([\"C\", \"C\", \"N\", \"C\", \"N\"])", "Returns all rings with C-C-N-C-N atoms." },
                            {  "Or(Rings([\"C\", \"C\", \"N\", \"C\", \"N\"]), Rings([\"C\", \"C\", \"C\", \"N\", \"N\"]))", "Returns all rings with C-C-N-C-N or C-C-C-N-N atoms." },
                        })
                    },
                    Compile = (f, c) => f.Arguments.Length == 0 ? new RingQuery(null) : new RingQuery(f.Arguments.Select(a => a.GetString()).ToArray())
                },
                new SymbolDescriptor
                {
                    Name = "RingAtoms",
                    Arguments = new FunctionArgument[] 
                    { 
                        new FunctionArgument { Name = "atom", Description = "Atom types.", Type = BasicTypes.Atoms },
                        new FunctionArgument { Name = "ring", Description = "Specific ring.", Type = TypeMany.Create(BasicTypes.Rings, isOption: true) } 
                    },
                    Type = BasicTypes.Atoms,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Returns all rings atoms.",
                        Examples = Examples(new Dictionary<string, string> { {  "RingAtoms(Atoms(\"C\"), Rings(4 * [\"C\"] + [\"O\"]))", "Returns all C atoms on a ring with 4C and O." } } )
                    },
                    Compile = (f, c) => f.Arguments.Length == 1 
                        ? new OnRingQuery(f.Arguments[0].Compile(c) as AtomSetQuery, null) 
                        : new OnRingQuery(f.Arguments[0].Compile(c) as AtomSetQuery, f.Arguments[1].Compile(c) as RingQuery)
                },

                
                new SymbolDescriptor 
                { 
                    Name = "RegularMotifs", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "regex", Description = "Regular expression on one letter abbreviations of amino acids.", Type = BasicTypes.Value } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Options = CreateOptions(RegularMotifTypeOption),
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Identifies regular motifs. The protein is split into individual chains and the residues are sorted by their Sequence Number before the motifs are identified. The query does not check if adjacent residues have consecutive Sequence Numbers. " +
                                    "The query works in two modes: Amino and Nucleotide, on amino acids and nucleotides respectively. In the Amino mode, all the derivatives of standard residues are treated as the standard residues, as long as this information is properly annotated in MODRES or _pdbx_struct_mod_residue field. " + 
                                    "The default mode is 'Amino'",
                        Examples = Examples(new Dictionary<string, string> 
                        { 
                            {  "RegularMotifs(\"RGD\")", "Finds all RGD motifs." } ,
                            {  "RegularMotifs(\"ACGTU\", Type = 'Nucleotide')", "Finds all consecutive occurrences of the ACGTU nucleotides." } ,
                            {  "RegularMotifs(\".HC.\").Filter(lambda m: m.IsConnected())", "Finds all 4 residue motifs with ?-HIS-CYS-? that are connected." }
                        })
                    },
                    Compile = (f, c) => new RegexQuery(f.Arguments[0].Compile(), f.Options[RegularMotifTypeOption.Name].GetString())
                },                           
    
                new SymbolDescriptor
                {
                    Name = "Chains",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "identifiers", Description = "Chain identifiers.", Type = TypeMany.Create(BasicTypes.Value, allowEmpty: true) } },
                    Type = BasicTypes.PatternSeq,
                    Attributes = SetTraits,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Splits the structures into chains. If no identifiers are specified, all chains are returned.",
                        Examples = Examples(new Dictionary<string, string> 
                        {
                            { "Chains()", "Returns all chains." },
                            { "Chains(\"\")", "Returns chains without specific identifier." },
                            { "Chains(\"A\", \"B\")", "Returns chains A and B." }
                        })
                    },
                    Compile = (f,c) => new ChainsQuery(f.Arguments.Select(a => a.GetString()))
                },

                new SymbolDescriptor
                {
                    Name = "Sheets",
                    Arguments = new FunctionArgument[0],
                    Type = BasicTypes.PatternSeq,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Returns all sheets. This assumes the information about sheets was present in the input structure.",
                        Examples = Examples(new Dictionary<string, string> { {  "Sheets()", "Returns all sheets." } } )
                    },
                    Compile = (f, c) => new SecondaryElementQuery(SecondaryElementQuery.ElementType.Sheet)
                },

                new SymbolDescriptor
                {
                    Name = "Helices",
                    Arguments = new FunctionArgument[0],
                    Type = BasicTypes.PatternSeq,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "Returns all helices. This assumes the information about helices was present in the input structure.",
                        Examples = Examples(new Dictionary<string, string> { {  "Helices()", "Returns all helices." } } )
                    },
                    Compile = (f, c) => new SecondaryElementQuery(SecondaryElementQuery.ElementType.Helix)
                },
                
                new SymbolDescriptor 
                { 
                    Name = "Named", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to name.", Type = BasicTypes.PatternSeq } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.BasicMotiveFunctions,
                        Description = "'Names' the pattern by its lowest atom id.",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms(\"Zn\").Named().AmbientAtoms(7)", "When exported, the result files will have names in the format '[parent id]_[pseudorandom number]_[zn atomid]'. If the Named function was not used, the name would be just '[parent id]_[pseudorandom number]'." } } )
                    },
                    Compile = (f, c) => new NamedQuery(f.Arguments[0].Compile(c) as QueryMotive)
                },

                ///////////////////////////////////////////////////////////////////
                // ADVANCED ELEMENTS
                ///////////////////////////////////////////////////////////////////

                new SymbolDescriptor 
                { 
                    Name = "Flatten", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to project.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "selector", Description = "The selector.", Type = TypeExpression.Function().Arg(TypeClasses.Pattern).Return(TypeClasses.PatternSeq) } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.AdvancedFunctions,
                        Description = "Converts a sequence of sequence of patterns into a single 'flat' sequence.",
                        Examples = Examples(new Dictionary<string, string> { {  "Residues(\"HIS\").Flatten(lambda m: m.Find(Atoms(\"C\")))", "Returns all C atoms on HIS residues." } } )
                    },
                    Compile = (f, c) => new SelectManyQuery(f.Arguments[0].Compile(c) as QueryMotive, f.Arguments[1].Compile(c) as LambdaQuery)
                },

                new SymbolDescriptor 
                { 
                    Name = "Inside", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to find.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "where", Description = "Where to find them.", Type = BasicTypes.PatternSeq }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.AdvancedFunctions,
                        Description = "Finds patterns within another pattern. Equivalent to where.Flatten(lambda m: m.Find(patterns))",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms(\"C\").Inside(Residues(\"HIS\"))", "Returns all C atoms on HIS residues." } } )
                    },
                    Normalize = f => MetaQuery.CreateSymbol("Flatten")
                            .ApplyTo(
                                f.Arguments[1], 
                                MetaQuery.CreateLambda(MetaQuery.CreateSymbol("$m"), 
                                    MetaQuery.CreateSymbol("Find").ApplyTo(MetaQuery.CreateSymbol("$m"), f.Arguments[0])))
                },

                new SymbolDescriptor 
                { 
                    Name = "Or", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to merge.", Type = TypeMany.Create(BasicTypes.PatternSeq) } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Attributes = SymbolAttributes.IgnoreEmpty | SymbolAttributes.Orderless | SymbolAttributes.UniqueArgs | SymbolAttributes.Flat | SymbolAttributes.OneIdentity, 
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.AdvancedFunctions,
                        //OperatorForm = "|",
                        Description = "Merges several pattern sequences into one.",
                        Examples = Examples(new Dictionary<string, string> { {  "Or(Atoms(\"Zn\").ConnectedResidues(1), Rings())", "Finds all zincs and their connected residues or rings." } } )
                    },
                    Compile = (f, c) => new OrQuery(f.Arguments.Select(x => x.Compile(c) as QueryMotive))
                },

                new SymbolDescriptor 
                { 
                    Name = "Union", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to merge.", Type = BasicTypes.PatternSeq } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.AdvancedFunctions,
                        Description = "Collects all 'inner' patterns and yields one created from their unique atoms.",
                        Examples = Examples(new Dictionary<string, string> { {  "Rings().Union()", "Creates a single pattern that contains all rings." } } )
                    },
                    Compile = (f, c) => new UnionQuery(f.Arguments[0].Compile(c) as QueryMotive)
                },
                
                new SymbolDescriptor 
                { 
                    Name = "ToAtoms", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to split.", Type = BasicTypes.PatternSeq } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.AdvancedFunctions,
                        Description = "Collects all 'inner' patterns and yields all unique atoms one by one.",
                        Examples = Examples(new Dictionary<string, string> { {  "Residues(\"HIS\").ToAtoms()", "Returns all atoms on HIS residues one by one." } } )
                    },
                    Compile = (f, c) => new ToElementsQuery(ToElementsQueryType.Atoms, f.Arguments[0].Compile(c) as QueryMotive)
                },
                new SymbolDescriptor 
                { 
                    Name = "ToResidues", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to split.", Type = BasicTypes.PatternSeq } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.AdvancedFunctions,
                        Description = "Collects all 'inner' patterns and yields all unique residues one by one. The residues contain only the atoms that have been yielded by the inner query.",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms(\"C\").ToResidues()", "Returns all C atoms grouped by residues." } } )
                    },
                    Compile = (f, c) => new ToElementsQuery(ToElementsQueryType.Residues, f.Arguments[0].Compile(c) as QueryMotive)
                },

                ///////////////////////////////////////////////////////////////////
                // FILTER ELEMENTS
                ///////////////////////////////////////////////////////////////////
                
                new SymbolDescriptor 
                { 
                    Name = "Count", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "where", Description = "Where to count it.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "what", Description = "What pattern to count.", Type = BasicTypes.PatternSeq }
                    },
                    Type = BasicTypes.Integer, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Counts all occurrences of pattern 'what' in pattern 'where'.",
                        Examples = Examples(new Dictionary<string, string>
                        { 
                            {  "m.Count(Residues(\"HIS\"))", "Returns the count of HIS residues in the pattern m. Where m is a Pattern (for example when using the Filter function or returned by the ToPattern() function). This example will not work directly and is here to illustrate a concept." }, 
                            {  "Atoms(\"Zn\").ConnectedResidues(1).Filter(lambda m: m.Count(Residues(\"HIS\")) == 2)", "Patterns with Zn atoms and its connected residues with exactly 2 HIS residues." } 
                        })
                    },
                    Compile = (f, c) => new CountQuery(f.Arguments[1].Compile(c) as QueryMotive, f.Arguments[0].Compile(c))
                },

                new SymbolDescriptor 
                { 
                    Name = "SeqCount", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "what", Description = "What pattern sequence to count.", Type = BasicTypes.PatternSeq }
                    },
                    Type = BasicTypes.Integer, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Counts the length of a sequence of motifs.",
                        Examples = Examples(new Dictionary<string, string>
                        { 
                            {  "Atoms(\"Zn\").AmbientResidues(3).Filter(lambda m: m.Find(Residues(\"HIS\")).SeqCount() > 1)", "Returns a sequence of patterns with Zn atom and residues within 3ang if the pattern contains at least 2 HIS residues." }
                        })
                    },
                    Compile = (f, c) => new SeqCountQuery(f.Arguments[0].Compile(c) as QueryMotive)
                },

                new SymbolDescriptor 
                { 
                    Name = "Contains", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "where", Description = "Where to look.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "what", Description = "What to find.", Type = BasicTypes.PatternSeq }
                    },
                    Type = BasicTypes.Bool, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Checks if a pattern is contained within another one. Equivalent to where.Count(what) > 0.",
                        Examples = Examples(new Dictionary<string, string> { {  "HetResidues().Filter(lambda m: m.Contains(Rings(5*['C']+['O'])).Not())", "Returns all HET residues that do not contain a 5CO ring." } } )
                    },
                    Normalize = f => MetaQuery.CreateSymbol(RelationOp.Greater.ToString())
                        .ApplyTo(
                            MetaQuery.CreateSymbol("Count").ApplyTo(f.Arguments[0], f.Arguments[1]), 
                            MetaQuery.CreateValue((int)0))
                },
                
                new SymbolDescriptor 
                { 
                    Name = "Filter", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to filter.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "filter", Description = "Filter predicate.", Type = TypeExpression.Function().Arg(TypeClasses.Pattern).Return(TypeClasses.Bool) } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Filters a sequence of patterns with a given predicate.",
                        Examples = Examples(new Dictionary<string, string> { {  "Residues().Filter(lambda m: m.Count(Atoms(\"C\")) >= 3)", "Returns all residues that contain at least 3 C atoms." } } )
                    },
                    Compile = (f, c) => new FilterQuery(f.Arguments[0].Compile(c) as QueryMotive, f.Arguments[1].Compile(c) as LambdaQuery)
                },

                new SymbolDescriptor 
                { 
                    Name = "ExecuteIf", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "query", Description = "Query to execute.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "condition", Description = "Condition that must be satisfied for the parent structure/pattern.", Type = TypeExpression.Function().Arg(TypeClasses.Pattern).Return(TypeClasses.Bool) } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Executes a query only if the condition is met. Otherwise, return an empty sequence of patterns.",
                        Examples = Examples(new Dictionary<string, string> 
                        { 
                            { "Residues().ExecuteIf(lambda p: p.Resolution() <= 2.4)", "Returns residues in structures that have resolution lower than 2.4ang." },
                            { "AminoAcids().ExecuteIf(lambda p: p.Count(Atoms('Fe')) > 3)", "Returns all amino acids in structures that have at least 3 Fe atoms." } 
                        })
                    },
                    Compile = (f, c) => new ExecuteIfQuery(f.Arguments[0].Compile(c) as QueryMotive, f.Arguments[1].Compile(c) as LambdaQuery)
                },

                new SymbolDescriptor 
                { 
                    Name = "IsConnected", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "pattern", Description = "A pattern to test.", Type = BasicTypes.Pattern },
                    },
                    Type = BasicTypes.Bool, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Checks if a particular pattern is a connected graph.",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms(\"Zn\").AmbientResidues(3).Filter(lambda m: m.IsConnected())", "Finds all patterns with a Zn and residues within 3 ang, where all the ambient residues are connected to the central atom." } } )
                    },
                    Compile = (f, c) => new IsConnectedQuery(f.Arguments[0].Compile(c))
                },

                new SymbolDescriptor 
                { 
                    Name = "NearestDistanceTo", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "where", Description = "A pattern to test.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "patterns", Description = "Pattern sequence to test against.", Type = BasicTypes.PatternSeq }  
                    },
                    Type = BasicTypes.Real, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Finds the distance to a particular pattern.",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms().Filter(lambda m: m.NearestDistanceTo(Residues(\"ASP\")) >= 5)", "Finds all atoms that are at least 5 (angstroms) away from any ASP residue." } } )
                    },
                    Compile = (f, c) => new NearestDistanceToQuery(f.Arguments[0].Compile(c), f.Arguments[1].Compile(c) as QueryMotive)
                },

                
                new SymbolDescriptor 
                { 
                    Name = "IsConnectedTo", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "where", Description = "A pattern to test.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "patterns", Description = "Pattern sequence to test against.", Type = BasicTypes.PatternSeq } 
                    },
                    Type = BasicTypes.Bool, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Checks if a particular pattern is connected to any other specified pattern. The patterns must have empty intersection for this function to return true.",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms().Filter(lambda a: a.IsConnectedTo(Rings()))", "Finds all atoms that are connected to a ring they do not belong to." } } )
                    },
                    Compile = (f, c) => new IsConnectedToQuery(false, f.Arguments[0].Compile(c), f.Arguments[1].Compile(c) as QueryMotive)
                },

                new SymbolDescriptor 
                { 
                    Name = "IsNotConnectedTo", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "what", Description = "A pattern to test.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "patterns", Description = "Pattern sequence to test against.", Type = BasicTypes.PatternSeq } 
                    },
                    Type = BasicTypes.Bool, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.FilterFunctions,
                        Description = "Checks if a particular pattern is not connected to any other specified pattern. The patterns must have empty intersection for this function to return true.",
                        Examples = Examples(new Dictionary<string, string> { {  "Residues().Filter(lambda r: r.IsNotConnectedTo(Atoms(\"Ca\")))", "Finds all residues that are not connected to Ca atoms. The residue itself can still contain Ca atoms." } } )
                    },
                    Compile = (f, c) => new IsConnectedToQuery(true, f.Arguments[0].Compile(c), f.Arguments[1].Compile(c) as QueryMotive)
                },
                
                ////new SymbolDescriptor
                ////{
                ////    Name = "FirstAtomId",
                ////    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "m", Description = "Pattern.", Type = BasicTypes.Motive } },
                ////    Type = BasicTypes.Interger,
                ////    Description = new SymbolDescription
                ////    {
                ////        Category = SymbolCategories.AdvancedMotiveFunctions,
                ////        Description = "Returns the lowest atom id in a motive.",
                ////        Examples = Examples(new Dictionary<string, string> { {  "AtomId[Motive[\"1tqn_12\"]]",
                ////        ExampleDescription = "Returns the id of the first atom in the motive 1tqn_12."
                ////    },
                ////    Compile = (f, c) => new AtomIdQuery(f.Arguments[0].Compile(c))
                ////},

                ////new SymbolDescriptor
                ////{
                ////    Name = "FirstAtomChain",
                ////    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "m", Description = "Pattern.", Type = BasicTypes.Motive } },
                ////    Type = BasicTypes.String,
                ////    Description = new SymbolDescription
                ////    {
                ////        Category = SymbolCategories.AdvancedMotiveFunctions,
                ////        Description = "Returns the chain identifier (as a string) of the atom with the lowest id.",
                ////        Examples = Examples(new Dictionary<string, string> { {  "AtomChain[Motive[\"1tqn_12\"]]",
                ////        ExampleDescription = "Returns the chain identifier of the first atom in the motive 1tqn_12."
                ////    },
                ////    Compile = (f, c) => new AtomChainQuery(f.Arguments[0].Compile(c))
                ////},
                                                                          
                ///////////////////////////////////////////////////////////////////
                // TOPOLOGY
                ///////////////////////////////////////////////////////////////////

                new SymbolDescriptor 
                { 
                    Name = "ConnectedAtoms", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "pattern", Description = "Basic pattern.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "n", Description = "Number of atom layers to connect.", Type = BasicTypes.Integer }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Options = CreateOptions(YieldNamedDuplicatesOption),
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.TopologyFunctions,
                        Description = "Surrounds the inner pattern by n layers of atoms.",
                        Examples = Examples(new Dictionary<string, string> { {  "Residues(\"MAN\").ConnectedAtoms(2)", "Finds all MAN residues and then adds two connected levels of atoms to them." } } )
                    },
                    Compile = (f, c) => new ConnectedAtomsQuery(f.Arguments[0].Compile(c) as QueryMotive, f.Arguments[1].GetInteger(), f.Options[YieldNamedDuplicatesOption.Name].GetBool())
                },

                new SymbolDescriptor 
                { 
                    Name = "ConnectedResidues", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "pattern", Description = "Basic pattern.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "n", Description = "Number of residue layers to connect.", Type = BasicTypes.Integer }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Options = CreateOptions(YieldNamedDuplicatesOption),
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.TopologyFunctions,
                        Description = "Surrounds the inner pattern by n layers of residues.",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms(\"Zn\").ConnectedResidues(1)", "Finds all Zn atoms and adds all residues that are connected to them." } } )
                    },
                    Compile = (f, c) => new ConnectedResiduesQuery(f.Arguments[0].Compile(c) as QueryMotive, f.Arguments[1].GetInteger(), f.Options[YieldNamedDuplicatesOption.Name].GetBool())
                },

                new SymbolDescriptor 
                { 
                    Name = "Path", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to path.", Type = TypeMany.Create(BasicTypes.PatternSeq) }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.TopologyFunctions,
                        Description = "Creates a new pattern from 'connected' parts.",
                        Examples = Examples(new Dictionary<string, string> { {  "Path(Atoms(\"O\"), Atoms(\"C\"), Atoms(\"O\"))", "Finds patterns with two O and one C atoms where the C atoms is connected to the O ones." } } )
                    },
                    Compile = (f, c) => new PathQuery(f.Arguments.Select(q => q.Compile(c) as QueryMotive))
                },

                new SymbolDescriptor 
                { 
                    Name = "Star", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "center", Description = "Center pattern.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "patterns", Description = "Patterns to chain.", Type = TypeMany.Create(BasicTypes.PatternSeq) }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.TopologyFunctions,
                        Description = "Creates a new pattern from a central one and connected parts.",
                        Examples = Examples(new Dictionary<string, string> 
                        {
                            {  "Star(Atoms(\"C\"), Atoms(\"O\"), Atoms(\"O\"))", "Finds patterns with two O atoms and one C atom in the center." },
                            {  "Atoms(\"C\").Star(Atoms(\"O\"), Atoms(\"O\"))", "Finds patterns with two O atoms and one C atom in the center." } 
                        })
                    },
                    Compile = (f, c) => new StarQuery(f.Arguments[0].Compile() as QueryMotive, f.Arguments.Skip(1).Select(q => q.Compile(c) as QueryMotive))
                },
                 
                ///////////////////////////////////////////////////////////////////
                // GEOMETRY
                ///////////////////////////////////////////////////////////////////

                new SymbolDescriptor 
                { 
                    Name = "Cluster", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "r", Description = "Maximum distance between two patterns in the cluster.", Type = BasicTypes.Number },
                        new FunctionArgument { Name = "patterns", Description = "Patterns to cluster.", Type = TypeMany.Create(BasicTypes.PatternSeq) } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Attributes =  SymbolAttributes.IgnoreEmpty | SymbolAttributes.Orderless | SymbolAttributes.UniqueArgs,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Clusters all patterns that are pairwise closer than r (angstroms).",
                        Examples = Examples(new Dictionary<string, string> { {  "Cluster(4, Atoms(\"Ca\"), Rings(5 * [\"C\"] + [\"O\"]))", "Finds all instance of one or more rings with 5C and O atoms and one or more Ca atoms that are closer than 4 (angstroms)." } } )
                    },
                    Compile = (f, c) => new ClusterQuery(f.Arguments[0].GetDouble(), f.Arguments.Skip(1).Select(a => a.Compile(c)))
                },

                new SymbolDescriptor 
                { 
                    Name = "Near", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "r", Description = "Maximum distance between two sub-patterns in the pattern.", Type = BasicTypes.Number },
                        new FunctionArgument { Name = "patterns", Description = "Patterns to 'cluster'.", Type = TypeMany.Create(BasicTypes.PatternSeq) } 
                    },
                    Type = BasicTypes.PatternSeq, 
                    Attributes =  SymbolAttributes.IgnoreEmpty | SymbolAttributes.Orderless,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Clusters all patterns that are pairwise closer than r (angstroms) and checks if the \"counts\" match.",
                        Examples = Examples(new Dictionary<string, string> { {  "Near(4, Atoms(\"Ca\"), Atoms(\"Ca\"), Rings(5 * [\"C\"] + [\"O\"]))", "Finds all instance of a single ring with 5C and O atoms and two Ca atoms that are closer than 4 (angstroms)." } } )
                    },
                    Compile = (f, c) => new NearQuery(f.Arguments[0].GetDouble(), f.Arguments.Skip(1).Select(a => a.Compile(c)))
                },

                new SymbolDescriptor 
                { 
                    Name = "AmbientAtoms", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "pattern", Description = "Basic pattern.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "r", Description = "Radius.", Type = BasicTypes.Number }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Options = CreateOptions(IgnoreWatersOption, ExcludeBaseOption, YieldNamedDuplicatesOption),
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Surrounds the inner pattern by atoms that within the given radius from the inner pattern.",
                        Examples = Examples(new Dictionary<string, string> { {  "Atoms(\"Fe\").AmbientAtoms(4)", "Finds Fe atoms and all atoms within 4 (angstroms) from each of them." } })
                    },
                    Compile = (f, c) => new AmbientAtomsQuery(f.Arguments[1].GetDouble(), f.Arguments[0].Compile(c) as QueryMotive, 
                        f.Options[IgnoreWatersOption.Name].GetBool(), 
                        f.Options[ExcludeBaseOption.Name].GetBool(),
                        f.Options[YieldNamedDuplicatesOption.Name].GetBool())
                },

                new SymbolDescriptor 
                { 
                    Name = "AmbientResidues", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "pattern", Description = "Basic pattern.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "r", Description = "Radius.", Type = BasicTypes.Number }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Options = CreateOptions(IgnoreWatersOption, ExcludeBaseOption, YieldNamedDuplicatesOption),
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Surrounds the inner pattern by residues that have at least one atom within the given radius from the inner pattern.",
                        Examples = Examples(new Dictionary<string, string> { {  "Rings(6 * [\"C\"]).AmbientResidues(4)", "Finds rings with 6C atoms and all residues within 4 (angstroms) from each of them."  } } )
                    },
                    Compile = (f, c) => new AmbientResiduesQuery(f.Arguments[1].GetDouble(), f.Arguments[0].Compile(c) as QueryMotive, 
                        f.Options[IgnoreWatersOption.Name].GetBool(), 
                        f.Options[ExcludeBaseOption.Name].GetBool(),
                        f.Options[YieldNamedDuplicatesOption.Name].GetBool())
                },

                new SymbolDescriptor
                {
                    Name = "Spherify",
                    Arguments = new FunctionArgument[]
                    {
                        new FunctionArgument { Name = "pattern", Description = "Basic pattern.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "r", Description = "Radius.", Type = BasicTypes.Number }
                    },
                    Type = BasicTypes.PatternSeq,
                    Options = CreateOptions(IgnoreWatersOption, ExcludeBaseOption, YieldNamedDuplicatesOption),
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Identifies the geometrical center of the base pattern and then includes all atoms within the specified radius.",
                        Examples = Examples(new Dictionary<string, string> { {  "Rings(6 * [\"C\"]).Spherify(5)", "Finds rings with 6C atoms, computes centroid of each pattern, and includes all atoms within 5 angstroms from it."  } } )
                    },
                    Compile = (f, c) => new SpherifyQuery(f.Arguments[1].GetDouble(), f.Arguments[0].Compile(c) as QueryMotive,
                        f.Options[IgnoreWatersOption.Name].GetBool(),
                        f.Options[ExcludeBaseOption.Name].GetBool(),
                        f.Options[YieldNamedDuplicatesOption.Name].GetBool())
                },

                new SymbolDescriptor 
                { 
                    Name = "Filled", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "pattern", Description = "Basic pattern.", Type = BasicTypes.PatternSeq }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Options = CreateOptions(
                        IgnoreWatersOption,
                        new MetaOption 
                        { 
                            Name = "RadiusFactor", 
                            DefaultValue = MetaQueries.MetaQuery.CreateValue(0.75), 
                            Type = BasicTypes.Number, 
                            Description = "Circumsphere radius factor."
                        }),
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Adds all atoms that fall within the circumsphere (with radius multiplied by the factor) of the basic pattern.",
                        Examples = Examples(new Dictionary<string, string> { {  "Cluster(4, Residues(\"HIS\")).Filled(RadiusFactor = 0.75)", "Finds clusters of HIS residues and all atoms within the circumsphere." } } )
                    },
                    Compile = (f, c) => new FilledQuery(f.Arguments[0].Compile(c) as QueryMotive, 
                        f.Options["RadiusFactor"].GetDouble(),
                        f.Options[IgnoreWatersOption.Name].GetBool())
                },

                new SymbolDescriptor
                {
                    Name = "DistanceCluster",
                    Arguments = new FunctionArgument[]
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to cluster using the provided distance matrices.", Type = TypeMany.Create(BasicTypes.PatternSeq) }
                    },
                    Type = BasicTypes.PatternSeq,
                    Options = CreateOptions(DistanceMatrixMinOption, DistanceMatrixMaxOption),
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Clusters all patterns that satisfy the provided min and max distance matrices. This query is computationally intensive and may take longer than most of other queries.",
                        Examples = Examples(new Dictionary<string, string> {
                            {  "DistanceCluster([Residues('ALA'), Residues('ALA'), Residues('HIS')], [[2],[2,2]], [[9],[9,9]])", "Finds all instances of 2 ALA and 1 HIS residues that are 2 to 9 angstroms apart." }
                        } )
                    },
                    Compile = (f, c) => new DistanceClusterQuery(f.Arguments.Select(a => a.Compile()).ToArray(), f.Options[DistanceMatrixMinOption.Name].GetStaticMatrix(), f.Options[DistanceMatrixMaxOption.Name].GetStaticMatrix())
                },
                
                new SymbolDescriptor 
                { 
                    Name = "EmptySpace", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "where", Description = "In which patterns to identify the empty space.", Type = BasicTypes.PatternSeq }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Options = CreateOptions(ProbeRadiusOption, InteriorThresholdOption),
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Computes empty space (cavities and voids) inside the given patterns using the MOLE algorithm. This query is computationally intensive and will take longer than most of other queries.",
                        Examples = Examples(new Dictionary<string, string> { {  "InputAsPattern().EmptySpace(ProbeRadius=50.0, InteriorThreshold=1.0)", "Finds empty space (cavities and voids) in the input structure." } })
                    },
                    Compile = (f, c) => new EmptySpaceQuery(f.Arguments[0].Compile(c) as QueryMotive,  
                        f.Options[ProbeRadiusOption.Name].GetDouble(), 
                        f.Options[InteriorThresholdOption.Name].GetDouble())
                },

                new SymbolDescriptor
                {
                    Name = "Stack2",
                    Arguments = new FunctionArgument[]
                    {
                        new FunctionArgument { Name = "minDistance", Description = "Minimum center distance.", Type = BasicTypes.Real },
                        new FunctionArgument { Name = "maxDistance", Description = "Maximum center distance.", Type = BasicTypes.Real },
                        new FunctionArgument { Name = "minProjDistance", Description = "Minimum projected center distance.", Type = BasicTypes.Real },
                        new FunctionArgument { Name = "maxProjDistance", Description = "Maximum projected center distance.", Type = BasicTypes.Real },
                        new FunctionArgument { Name = "minAngleDeg", Description = "Minimum plane angle in degrees.", Type = BasicTypes.Real },
                        new FunctionArgument { Name = "maxAngleDeg", Description = "Maximum plane angle in degrees.", Type = BasicTypes.Real },
                        new FunctionArgument { Name = "pattern1", Description = "First pattern.", Type = BasicTypes.PatternSeq },
                        new FunctionArgument { Name = "pattern2", Description = "Second pattern.", Type = BasicTypes.PatternSeq }
                    },
                    Type = BasicTypes.PatternSeq,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.GeometryFunctions,
                        Description = "Determines if two patterns are stacked using the provided distance and angle criteria. A pattern must have at least 3 atoms to be considered (so that a plane can be uniquely determined).",
                        Examples = Examples(new Dictionary<string, string> { { "Stack2(1, 15, 3, 5, 0, 5, Rings(), Rings())", "TODO a proper example." } } )
                    },
                    Compile = (f, c) => new Stack2Query(
                        f.Arguments[0].GetDouble(),
                        f.Arguments[1].GetDouble(),
                        f.Arguments[2].GetDouble(),
                        f.Arguments[3].GetDouble(),
                        f.Arguments[4].GetDouble(),
                        f.Arguments[5].GetDouble(),
                        f.Arguments[6].Compile(c) as QueryMotive,
                        f.Arguments[7].Compile(c) as QueryMotive)
                },

                ////new SymbolDescriptor 
                ////{ 
                ////    Name = "Tunnels", 
                ////    Arguments = new FunctionArgument[] 
                ////    {
                ////        new FunctionArgument { Name = "pattern", Description = "Basic motif.", Type = BasicTypes.MotifSeq },
                ////        new FunctionArgument { Name = "start", Description = "Start motif.", Type = BasicTypes.MotifSeq }
                ////    },
                ////    Type = BasicTypes.MotifSeq, 
                ////    Options = CreateOptions(ProbeRadiusOption, InteriorThresholdOption, BottleneckRadiusOption),
                ////    Description = new SymbolDescription
                ////    {
                ////        IsDotSyntax = true,
                ////        Category = SymbolCategories.GeometryFunctions,
                ////        Description = "Computes tunnels from a given motif as a start point(s). This feature currently works in only in MotiveExplorer.",
                ////        Examples = Examples(new Dictionary<string, string> { {  "InputAsPattern().Tunnels(Residues(\"HEM\"), ProbeRadius=5.0, InteriorThreshold=1.0, BottleneckRadius=1.0)", "Finds tunnels from HEM residues in the current structure." } })
                ////    },
                ////    Compile = (f, c) => new TunnelsQuery(f.Arguments[0].Compile(c) as QueryMotive, f.Arguments[1].Compile(c) as QueryMotive, 
                ////        f.Options[ProbeRadiusOption.Name].GetDouble(), 
                ////        f.Options[InteriorThresholdOption.Name].GetDouble(),
                ////        f.Options[BottleneckRadiusOption.Name].GetDouble())
                ////},
                                
                ///////////////////////////////////////////////////////////////////
                // MISC
                ///////////////////////////////////////////////////////////////////
                  
                new SymbolDescriptor 
                { 
                    Name = "Current", 
                    Type = BasicTypes.Pattern, 
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.MiscFunctions,
                        Description = "A variable that is assigned by the application environment.",
                        Examples = Examples(new Dictionary<string, string> { {  "AtomSimilarity(Current(), Pattern(\"model\"))", "Returns the atom similarity of the current pattern and the model. This example will work for example when defining a structure descriptor in SiteBinder and there is a structure with id 'model' loaded." } } )
                    },
                    Compile = (f, c) => new CurrentQueries()
                },
                MotiveSymbol,

                new SymbolDescriptor
                {
                    Name = "InputAsPattern",
                    Type = BasicTypes.PatternSeq,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Returns the input structure represented as an atom pattern sequence.",
                        Examples = Examples(new Dictionary<string, string> { {  "ParentPattern().EmptySpace()", "Finds empty space inside the input structure." }})
                    },
                    Compile = (f, _) => new ParentQueries()
                },

                new SymbolDescriptor
                {
                    Name = "CommonAtoms",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "modelId", Description = "Id of the model.", Type = BasicTypes.String } },
                    Type = BasicTypes.PatternSeq,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Returns a single pattern with atoms common with the model. Properties checked: atom id, element symbol, chain, residue number. This query is useful for example for atom selection in SiteBinder.",
                        Examples = Examples(new Dictionary<string, string> { {  "CommonAtoms(\"1tqn_12\")", "Returns a pattern formed by atoms common with the '1tqn_12' structure." }})
                    },
                    Compile = (f, _) => new CommonAtomsQuery(f.Arguments[0].GetString())
                },

                new SymbolDescriptor
                {
                    Name = "AminoSequenceString",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "pattern", Description = "Pattern to convert.", Type = BasicTypes.Pattern } },
                    Type = BasicTypes.String,
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Returns a string of single-letter amino acids contained in the pattern.",
                        Examples = Examples(new Dictionary<string, string>
                        { 
                            { "Pattern(\"1tqn_12\").AminoSequenceString()", "Returns a string representing amino acids in the pattern '1tqn_12'." },
                            { "RegularMotifs(Pattern(\"1tqn_12\").AminoSequenceString())", "Returns all regular patterns that correspond to the AMK sequence in 1tqn_12." }
                        })
                    },
                    Compile = (f, _) => new AminoSequenceStringQuery(f.Arguments[0].Compile())
                },
                
                new SymbolDescriptor 
                { 
                    Name = "Find", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "source", Description = "Where to look.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "patterns", Description = "Patterns to find.", Type = BasicTypes.PatternSeq }
                    },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Converts the source pattern to a structure and finds patterns within it.",
                        Examples = Examples(new Dictionary<string, string>
                        { 
                            {  "AtomSimilarity(Current().Find(NotAtoms(\"N\")).ToPattern(), Pattern(\"model\").Find(NotAtoms(\"N\")).ToPattern())", "Computes the atom similarity of the 'current' and 'model' patterns, but ignores N atoms. This example can be used in SiteBinder to create a descriptor (assuming a structure with id 'model' is loaded)." } 
                        })
                    },
                    Compile = (f, c) => new FindQuery(f.Arguments[1].Compile(c) as QueryMotive, f.Arguments[0].Compile(c))
                },

                new SymbolDescriptor 
                { 
                    Name = "ToPattern", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "patterns", Description = "Patterns to convert.", Type = BasicTypes.PatternSeq } 
                    },
                    Type = BasicTypes.Pattern, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Converts a sequence of Patterns (AtomSetSeq) to a single Pattern by merging all atoms into a single atom set. The Pattern type is required by some function such as AtomSimilarity.",
                        Examples = Examples(new Dictionary<string, string> 
                        { 
                            {  "Residues(\"HIS\").ToPattern()", "Converts a sequence of HIS residue Patterns to a single Pattern." },
                            {  "AtomSimilarity(Current().Find(NotAtoms(\"N\")).ToPattern(), Pattern(\"model\").Find(NotAtoms(\"N\")).ToPattern())", "Computes the atom similarity of the 'current' and 'model' patterns, but ignores N atoms." }
                        } )
                    },
                    Compile = (f, c) => new ToQueries(f.Arguments[0].Compile(c) as QueryMotive)
                },

                new SymbolDescriptor 
                { 
                    Name = "CSA", 
                    Arguments = new FunctionArgument[] { },
                    Type = BasicTypes.PatternSeq, 
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Entries from Catalytic Site Atlas represented as patterns. Works only if used from the command line version of Queries and property configured.",
                        Examples = Examples(new Dictionary<string, string> { {  "CSA()", "All CSA sites for the given structure. This example will only work if used from the command line version of Queries and property configured." } } )
                    },
                    Compile = (f, c) => new CSAQuery()
                },

                new SymbolDescriptor 
                { 
                    Name = "AtomSimilarity", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "a", Description = "First pattern.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "b", Description = "Second pattern.", Type = BasicTypes.Pattern },
                    },
                    Type = BasicTypes.Real, 
                    Attributes = SymbolAttributes.Orderless, 
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Computes Jaccard/Tanimoto coefficient on atoms (element symbols) of both structures.",
                        Examples = Examples(new Dictionary<string, string> { {  "AtomSimilarity(Current(),Pattern(\"1tqn_12\"))", "Computes the atom similarity between the current pattern and 1tqn_12. This example can be used in SiteBinder to create a descriptor (assuming a structure with id '1tqn_12' is loaded)." } } )
                    },
                    Compile = (f, c) => new MotiveSimilarityQuery(MotiveSimilarityType.AtomJaccard, f.Arguments[0].Compile(c), f.Arguments[1].Compile(c))
                },
                new SymbolDescriptor 
                { 
                    Name = "ResidueSimilarity", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "a", Description = "First pattern.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "b", Description = "Second pattern.", Type = BasicTypes.Pattern },
                    },
                    Type = BasicTypes.Real, 
                    Attributes = SymbolAttributes.Orderless, 
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Computes Jaccard/Tanimoto coefficient on residue names of both structures.",
                        Examples = Examples(new Dictionary<string, string> { {  "ResidueSimilarity(Current(), Pattern(\"1tqn_12\"))", "Computes the residue similarity between the current pattern and '1tqn_12'. This example can be used in SiteBinder to create a descriptor (assuming a structure with id '1tqn_12' is loaded)." } } )
                    },
                    Compile = (f, c) => new MotiveSimilarityQuery(MotiveSimilarityType.ResidueJaccard, f.Arguments[0].Compile(c), f.Arguments[1].Compile(c))
                },
                new SymbolDescriptor 
                { 
                    Name = "DynamicInvoke", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "value", Description = "InnerQuery.", Type = BasicTypes.Value },
                        new FunctionArgument { Name = "name", Description = "Member name.", Type = BasicTypes.String },
                        new FunctionArgument { Name = "isProperty", Description = "Is this a property.", Type = BasicTypes.Bool },
                        new FunctionArgument { Name = "args", Description = "Array of arguments.", Type = BasicTypes.Value }
                    },
                    Type = BasicTypes.Value, 
                    Description = new SymbolDescription
                    {
                        IsInternal = true,
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Dynamically invoke a member.",
                        Examples = Examples()
                    },
                    Compile = (f, c) => new DynamicInvokeQuery(f.Arguments[0].Compile(c), f.Arguments[1].GetString(), f.Arguments[2].GetBool(), f.Arguments[3].GetObjectValue() as object[])
                },
                new SymbolDescriptor 
                { 
                    Name = "AtomProperty", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "atomPattern", Description = "Single atom pattern.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "name", Description = "Property name.", Type = BasicTypes.String }
                    },
                    Type = TypeWildcard.Instance, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.MiscFunctions,
                        Description = "If the property exists and the pattern consists of a single atom, returns the property. Otherwise, returns Nothing.",
                        Examples = Examples(new Dictionary<string, string> 
                        { 
                            { "a.AtomProperty(\"charge\")", "Gets the 'charge' property of the atom a. Where a is a single atom Pattern. This example will not work directly." },
                            { "Atoms().Filter(lambda a: a.AtomProperty(\"charge\") >= 2)", "All atoms with the charge property greater or equal to 2. This example will only work in cases where a suitable property is defined. For example in Scripting window in the Charges app." }
                        })
                    },
                    Compile = (f, c) => new AtomPropertyQuery(f.Arguments[1].GetString(), f.Arguments[0].Compile(c))
                },
                new SymbolDescriptor 
                { 
                    Name = "Descriptor", 
                    Arguments = new FunctionArgument[] 
                    {
                        new FunctionArgument { Name = "pattern", Description = "Pattern that represents entire structure.", Type = BasicTypes.Pattern },
                        new FunctionArgument { Name = "name", Description = "Descriptor name.", Type = BasicTypes.String }
                    },
                    Type = TypeWildcard.Instance, 
                    Description = new SymbolDescription
                    {
                        IsDotSyntax = true,
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Returns the descriptor. If the descriptor does not exist, 'null' is returned.",
                        Examples = Examples(new Dictionary<string, string> { {  "Current().Descriptor(\"similarity\") >= 0.75", "Returns True if 'similarity' descriptor of the current pattern is at least 0.75. This example will work for example in SiteBinder's structure selection if the 'similarity' descriptor has been previously defined." } } )
                    },
                    Compile = (f, c) => new StructureDescriptorQuery(f.Arguments[1].GetString(), f.Arguments[0].Compile(c))
                },

                new SymbolDescriptor
                {
                    Name = "GroupedAtoms",
                    Arguments = new FunctionArgument[] { new FunctionArgument { Name = "symbols", Description = "Allowed element symbols.", Type = TypeMany.Create(BasicTypes.String, allowEmpty: true) } },
                    Type = BasicTypes.PatternSeq,
                    Attributes = SymbolAttributes.UniqueArgs | SymbolAttributes.Orderless,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.MiscFunctions,
                        Description = "Groups atoms using the element symbol and then returns each group as a single pattern.",
                        Examples = Examples(new Dictionary<string, string> 
                        {
                            {  "GroupedAtoms()", "Returns groups of all atom types. For example if the input has atoms 5xC-1xO, patterns with 5xC and 1xO atoms are returned." } ,
                            {  "GroupedAtoms('C', 'O')", "Returns groups of all atom types. For example if the input has atoms 5xC-1xO-6xH, patterns with 5xC and 1xO atoms are returned (but the H atoms are ignored)." } 
                        })
                    },
                    Compile = (f, c) => new GroupedAtomsQuery(f.Arguments.Select(a => a.GetString()))
                },

                ///////////////////////////////////////////////////////////////////
                // METADATA
                ///////////////////////////////////////////////////////////////////
                
                metadataProperty("ReleaseDate", "Get the release date of the parent structure. The value has to be compared to the DateTime(year, month, day) object.", ">= DateTime(2008, 1, 1)", BasicTypes.Value, data => data.Released),
                metadataProperty("ReleaseYear", "Get the release year of the parent structure.", "== 2006",  BasicTypes.Integer, data => data.Released != null ? (int?)data.Released.Value.Year : null),

                metadataProperty("LatestRevisionDate", "Get the latest revision date of the parent structure. The value has to be compared to the DateTime(year, month, day) object.", ">= DateTime(2008, 1, 1)", BasicTypes.Value, data => data.LatestRevision),
                metadataProperty("LatestRevisionYear", "Get the latest revision year of the parent structure.", "== 2006", BasicTypes.Integer, data => data.LatestRevision != null ? (int?)data.LatestRevision.Value.Year : null),

                metadataProperty("Resolution", "Get the resolution in angstroms of the parent structure.", "<= 2.4", BasicTypes.Real, data => data.Resolution),

                metadataProperty("ProteinStoichiometry", "Get the protein stoichiometry of the parent structure. Possible values are: " + Enum.GetNames(typeof(PdbStoichiometryType)).JoinBy() + ".", "== \"" + PdbStoichiometryType.Heteromer.ToString() + "\"", BasicTypes.String, data => EnumHelper.ToString(data.ProteinStoichiometry)),
                metadataPropertyNoExample("ProteinStoichiometryString", "Get the protein stoichiometry string of the parent structure.", BasicTypes.String, data => data.ProteinStoichiometryString),

                metadataProperty("Weight", "Get the weight of the molecule in kDa.", "> 100000.0", BasicTypes.Real, data => data.TotalWeightInKda),
                metadataProperty("PolymerType", "Get the polymer type of the structure. Possible values are: " + Enum.GetNames(typeof(PdbPolymerType)).JoinBy() + ".", "== \"" + PdbPolymerType.ProteinDNA.ToString() + "\"", BasicTypes.String, data => EnumHelper.ToString(data.PolymerType)),
                metadataProperty("ExperimentMethod", "Get the experiment method. The value is always an upper-case string.", "== \"INFRARED SPECTROSCOPY\"", BasicTypes.String, data => data.ExperimentMethod.Trim().ToUpperInvariant()),

                metadataPropertyNoExample("Authors", "Returns authors of the parent structure separated by a semicolon.", BasicTypes.String, data => data.Authors.JoinBy("; ")),                
                metadataHasAllProperties("Authors", "authors",  new[] { "Holmes", "Watson" }, true, data => data.Authors),
                metadataHasAnyProperty("Author", "author", new[] { "Holmes", "Watson" }, true, data => data.Authors),

                metadataPropertyNoExample("Keywords", "Returns keywords of the parent structure separated by a semicolon.", BasicTypes.String, data => data.Keywords.JoinBy("; ")),                
                metadataHasAllProperties("Keywords", "keywords", new[] { "membrane", "glycoprotein" }, false, data => data.Keywords),
                metadataHasAnyProperty("Keyword", "keyword",  new[] { "membrane", "glycoprotein" }, false, data => data.Keywords),

                metadataPropertyNoExample("EntitySources", "Returns entity sources of the parent structure separated by a semicolon.", BasicTypes.String, data => data.EntitySources.Select(e => EnumHelper.ToString(e)).JoinBy("; ")),
                metadataHasAllProperties("EntitySources", "entity sources", new[] { "GMO", "Natural", "Synthetic" }, false, data => data.EntitySources.Select(e => EnumHelper.ToString(e)).ToArray()),
                metadataHasAnyProperty("EntitySources", "entity source", new[] { "GMO", "Natural", "Synthetic" }, false, data => data.EntitySources.Select(e => EnumHelper.ToString(e)).ToArray()),

                metadataPropertyNoExample("ECNumbers", "Returns Enzymatic Commission numbers assigned to enzymes in the parent structure separated by a semicolon.", BasicTypes.String, data => data.EcNumbers.JoinBy("; ")),                
                metadataHasAllProperties("ECNumbers", "Enzymatic Commission numbers. It is possible to enter just a number prefix without the '.'",  new[] { "3.2.1.18", "3.3" }, false, data => data.EcNumbers),
                metadataHasAnyProperty("ECNumber", "Enzymatic Commission number. It is possible to enter just a number prefix without the '.'",  new[] { "3.2.1.19", "3.3" }, false, data => data.EcNumbers),

                metadataPropertyNoExample("OriginOrganisms", "Returns origin organism names of the parent structure separated by a semicolon.", BasicTypes.String, data => data.OriginOrganisms.JoinBy("; ")),                
                metadataHasAllProperties("OriginOrganisms", "origin organism names",  new[] { "Nipah virus", "Mus musculus" }, false, data => data.OriginOrganisms),
                metadataHasAnyProperty("OriginOrganism", "origin organism name",  new[] { "Nipah virus", "Mus musculus" }, false, data => data.OriginOrganisms),

                metadataPropertyNoExample("OriginOrganismIds", "Returns origin organism identifiers of the parent structure separated by a semicolon.", BasicTypes.String, data => data.OriginOrganismsId.JoinBy("; ")),                
                metadataHasAllProperties("OriginOrganismIds", "origin organism identifiers",  new[] { "121791", "10090" }, false, data => data.OriginOrganismsId),
                metadataHasAnyProperty("OriginOrganismId", "origin organism identifiers",  new[] { "121791", "10090" }, false, data => data.OriginOrganismsId),

                metadataPropertyNoExample("OriginOrganismGenus", "Returns origin organism identifiers of the parent structure separated by a semicolon.", BasicTypes.String, data => data.OriginOrganismsGenus.JoinBy("; ")),                
                metadataHasAllProperties("OriginOrganismGenus", "origin organism identifiers",  new[] { "X", "Y" }, false, data => data.OriginOrganismsGenus),
                metadataHasAnyProperty("OriginOrganismGenus", "origin organism identifiers",  new[] { "X", "Y" }, false, data => data.OriginOrganismsGenus),

                metadataPropertyNoExample("HostOrganisms", "Returns host organism names of the parent structure separated by a semicolon.", BasicTypes.String, data => data.HostOrganisms.JoinBy("; ")),                
                metadataHasAllProperties("HostOrganisms", "host organism names",  new[] { "Spodoptera frugiperda", "Mus musculus" }, false, data => data.HostOrganisms),
                metadataHasAnyProperty("HostOrganism", "host organism name",  new[] { "Spodoptera frugiperda", "Mus musculus" }, false, data => data.HostOrganisms),

                metadataPropertyNoExample("HostOrganismIds", "Returns host organism identifiers of the parent structure separated by a semicolon.", BasicTypes.String, data => data.HostOrganismsId.JoinBy("; ")),                
                metadataHasAllProperties("HostOrganismIds", "host organism identifiers",  new[] { "7108", "11244" }, false, data => data.HostOrganismsId),
                metadataHasAnyProperty("HostOrganismId", "host organism identifiers",  new[] { "7108", "11244" }, false, data => data.HostOrganismsId),

                metadataPropertyNoExample("HostOrganismGenus", "Returns host organism identifiers of the parent structure separated by a semicolon.", BasicTypes.String, data => data.HostOrganismsGenus.JoinBy("; ")),                
                metadataHasAllProperties("HostOrganismGenus", "host organism identifiers",  new[] { "X", "Y" }, false, data => data.HostOrganismsGenus),
                metadataHasAnyProperty("HostOrganismGenus", "host organism identifiers",  new[] { "X", "Y" }, false, data => data.HostOrganismsGenus),
                                                    
                ///////////////////////////////////////////////////////////////////
                // VALUE LOGIC
                ///////////////////////////////////////////////////////////////////

                binaryOp("Plus", "+", (x, y) => (x + y)),
                binaryOp("Subtract", "-", (x, y) => (x - y)),
                binaryOp("Times", "*", (x, y) => (x * y)),
                binaryOp("Divide", "/", (x, y) => (x / y)),
                binaryOp("Power", "^", DynamicFunctions.Power),
                new SymbolDescriptor
                {
                    Name = "Minus",
                    Type = BasicTypes.Number,
                    Arguments = new FunctionArgument[] 
                    { 
                        new FunctionArgument { Name = "x", Description = "Argument.", Type = BasicTypes.Number }
                    },
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.ValueFunctions,
                        OperatorForm = "-",
                        Description = "Computes the arithmetic negation of the argument.",
                        Examples = Examples(new Dictionary<string, string> { {  "-x", "Arithmetic negation of x." } } )
                    },
                    Compile = (f, c) => new UnaryFunctionQuery("Minus", x => -x, f.Arguments[0].Compile(c))
                },

                unaryFunction("Abs", x => Math.Abs(x), BasicTypes.Number, BasicTypes.Number),

                relation(RelationOp.Equal),
                relation(RelationOp.NotEqual),
                relation(RelationOp.Greater),
                relation(RelationOp.GreaterEqual),
                relation(RelationOp.Less),
                relation(RelationOp.LessEqual),

                logicalAndOrOr(LogicalOp.LogicalAnd),
                logicalAndOrOr(LogicalOp.LogicalOr),
                new SymbolDescriptor
                {
                    Name = "LogicalXor",
                    Type = BasicTypes.Bool,
                    Arguments = new FunctionArgument[] {  new FunctionArgument { Name = "xs", Description = "Arguments.", Type = TypeMany.Create(BasicTypes.Bool) } },
                    Attributes = SymbolAttributes.Flat | SymbolAttributes.Orderless | SymbolAttributes.OneIdentity | SymbolAttributes.UniqueArgs,
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.ValueFunctions,
                        Description = "Computes '" + LogicalOp.LogicalXor.ToString() + "' of the input values. This function can be called using the shorthand 'Xor'.",
                        OperatorForm = "Xor",
                        Examples = Examples(new Dictionary<string, string> { {  "LogicalXor(x, y)", "Evaluates to True or False based on the values of x and y." }, {  "Xor(x, y)", "Evaluates to True or False based on the values of x and y." } } )
                    },
                    Compile = (f, c) => new NaryFunctionQuery(LogicalOp.LogicalXor.ToString(), DynamicFunctions.NAryLogicalFunctions[LogicalOp.LogicalXor], f.Arguments.Select(a => a.Compile(c)))
                },
                new SymbolDescriptor
                {
                    Name = LogicalOp.LogicalNot.ToString(),
                    Type = BasicTypes.Bool,
                    Arguments = new FunctionArgument[] {  new FunctionArgument { Name = "x", Description = "Argument.", Type = BasicTypes.Bool } },
                    Description = new SymbolDescription
                    {
                        Category = SymbolCategories.ValueFunctions,
                        Description = "Computes '" + LogicalOp.LogicalNot.ToString() + "' of the input value. This function can be called using the shorthand 'Not'.",
                        OperatorForm = "Not",
                        Examples = Examples(new Dictionary<string, string> 
                        { 
                            {  "LogicalNot(x)", "Evaluates to True or False based on the value of x." },
                            {  "x.Not()", "Evaluates to True or False based on the value of x." } 
                        } )
                    },
                    Compile = (f, c) => new UnaryFunctionQuery(LogicalOp.LogicalNot.ToString(), x => !x, f.Arguments[0].Compile(c))
                }
            };

            SymbolTable.symbols = symbols.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
            AllSymbols = new ReadOnlyCollection<SymbolDescriptor>(symbols);
        }

        /// <summary>
        /// Return null if the symbol does not exist, the descriptor otherwise.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static SymbolDescriptor TryGetDescriptor(string symbol)
        {
            SymbolDescriptor ret;
            if (symbols.TryGetValue(symbol, out ret)) return ret;
            return null;
        }
    }
}