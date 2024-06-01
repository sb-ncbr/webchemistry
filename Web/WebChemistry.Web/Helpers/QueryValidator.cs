namespace WebChemistry.Web.Helpers
{
    using IronPython.Hosting;
    using Microsoft.Scripting;
    using Microsoft.Scripting.Hosting;
    using System;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.MetaQueries;

    /// <summary>
    /// WebChem scripting.
    /// </summary>
    public static class PatternQueryParser
    {        
        static ScriptEngine Engine;
        
        static PatternQueryParser()
        {
            var setup = new ScriptRuntimeSetup();
            setup.HostType = typeof(ScriptHost);
            setup.LanguageSetups.Add(Python.CreateLanguageSetup(null));
            setup.Options["SearchPaths"] = new string[] { string.Empty };

            var runtime = new ScriptRuntime(setup);
            Engine = runtime.GetEngine("Python");
            Engine.Runtime.LoadAssembly(typeof(QueryBuilder).Assembly);            
        }
                
        /// <summary>
        /// Throws if not valid.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="desiredType">If null, can be any.</param>
        /// <returns></returns>
        public static void Validate(string expression, TypeExpression desiredType = null)
        {
            Parse(expression, desiredType);
        }

        /// <summary>
        /// Parses a query.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="desiredType"></param>
        /// <returns></returns>
        public static Query Parse(string expression, TypeExpression desiredType = null)
        {
            var scope = Engine.CreateScope();
            Engine.Execute("from WebChemistry.Queries.Core.QueryBuilder import *", scope);
            Engine.Execute("from System import DateTime", scope);
            var script = Engine.CreateScriptSourceFromString(expression, Microsoft.Scripting.SourceCodeKind.AutoDetect);
            dynamic executed = script.Execute(scope);

            MetaQuery mq;
            try
            {
                mq = (MetaQuery)executed;
            }
            catch (Exception)
            {
                throw new InvalidCastException(string.Format("Expected 'Query', got '{0}'.", executed.GetType()));
            }

            if (desiredType != null)
            {
                if (!TypeExpression.Unify(desiredType, mq.Type).Success)
                {
                    throw new ArgumentTypeException(string.Format("Got '{0}', expected '{1}'.", mq.Type, desiredType));
                }
            }

            return mq.Compile();
        }
    }
}
