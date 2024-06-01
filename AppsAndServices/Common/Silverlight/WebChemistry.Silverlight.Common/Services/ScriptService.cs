namespace WebChemistry.Silverlight.Common.Services
{
    using IronPython.Hosting;
    using Microsoft.Scripting;
    using Microsoft.Scripting.Hosting;
    using Microsoft.Scripting.Silverlight;
    using System;
    using System.IO;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.MetaQueries;
    using WebChemistry.Silverlight.Common.DataModel;

    /// <summary>
    /// WebChem scripting.
    /// </summary>
    public class ScriptService
    {
        static ScriptService svc = new ScriptService();
        public static ScriptService Default { get { return svc; } }

        Subject<ScriptElement> elements = new Subject<ScriptElement>();
        public IObservable<ScriptElement> Elements { get { return elements; } }

        ScriptEngine Engine;
        ScriptScope Scope;
        object session;
        
        void Init()
        {
            var setup = new ScriptRuntimeSetup();
            setup.HostType = typeof(BrowserScriptHost);
            setup.LanguageSetups.Add(Python.CreateLanguageSetup(null));
            setup.Options["SearchPaths"] = new string[] { string.Empty };

            var runtime = new ScriptRuntime(setup);
            Engine = runtime.GetEngine("Python");
            Engine.Runtime.LoadAssembly(typeof(QueryBuilder).Assembly);
            Scope = Engine.CreateScope();
            Engine.Execute("from WebChemistry.Queries.Core.QueryBuilder import *", Scope);
            Engine.Execute("from System import DateTime", Scope);
        }

        public void InitSession(dynamic session)
        {
            this.session = (object)session;
            Scope.SetVariable("Session", session);
            Scope.SetVariable("MQ", session.QueriesScripting);
            Scope.SetVariable("Utils", session.UtilsScripting);
        }

        /// <summary>
        /// Resets the scope.
        /// </summary>
        public void ResetScope()
        {
            Scope = Engine.CreateScope();
            Engine.Execute("from WebChemistry.Queries.Core.QueryBuilder import *", Scope);
            Engine.Execute("from System import DateTime", Scope);
            Scope.SetVariable("Session", session);
            dynamic ds = session;
            Scope.SetVariable("MQ", ds.QueriesScripting);
        }

        /// <summary>
        /// Execute a script.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public dynamic Execute(string code)
        {
            var script = Engine.CreateScriptSourceFromString(code.TrimStart(), Microsoft.Scripting.SourceCodeKind.AutoDetect);
            return script.Execute(Scope);
        }
        
        /// <summary>
        /// Creates an empty element.
        /// </summary>
        public void CreateNewElement()
        {
            elements.OnNext(new ScriptElement { State = ScriptingElementState.Empty });
        }


        /// <summary>
        /// Gets a meta query from a python expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="desiredType">If null, can be any.</param>
        /// <returns></returns>
        public MetaQuery GetMetaQuery(string expression, TypeExpression desiredType = null)
        {
            var script = Engine.CreateScriptSourceFromString(expression.TrimStart(), Microsoft.Scripting.SourceCodeKind.AutoDetect);
            dynamic executed = script.Execute(Scope);

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
            return mq;            
        }

        /// <summary>
        /// Asynchronously executes the script element.
        /// </summary>
        /// <param name="element"></param>
        public async void Execute(ScriptElement element)
        {
            var text = element.ScriptText.TrimStart();

            if (string.IsNullOrWhiteSpace(text))
            {
                element.State = ScriptingElementState.Error;
                element.ErrorMessage = "No input.";
            }

            var cs = ComputationService.Default;
            var progress = cs.Start();
            try
            {
                progress.Update(statusText: "Executing...", canCancel: false);

                using (var ms = new MemoryStream())
                {
                    Engine.Runtime.IO.SetOutput(ms, Encoding.UTF8);

                    var script = Engine.CreateScriptSourceFromString(text, Microsoft.Scripting.SourceCodeKind.AutoDetect);
                    var result = await TaskEx.Run<dynamic>(() => 
                        {
                            dynamic ret = script.Execute(Scope);
                            ms.Seek(0, SeekOrigin.Begin);
                            var reader = new StreamReader(ms);
                            element.StdOut = reader.ReadToEnd();

                            if (ret is QueryBuilderElement)
                            {
                                var compiled = (ret as QueryBuilderElement).ToMetaQuery().Compile();
                            }

                            return ret;
                        });

                    //if (result is Task<dynamic>)
                    //{
                    //    element.Result = await (result as Task<dynamic>);
                    //}
                    //else if (result is Task)
                    //{
                    //    await (result as Task);
                    //    element.Result = null;
                    //}
                    //else
                    element.Result = result;

                    element.State = ScriptingElementState.Executed;
                    
                    CreateNewElement();
                }
            }
            catch (Exception e)
            {
                element.ErrorMessage = e.Message;
                element.State = ScriptingElementState.Error;
            }
            finally
            {
                cs.End();
                Engine.Runtime.IO.RedirectToConsole();
            }
        }

        /// <summary>
        /// Dispatches an action.
        /// </summary>
        /// <param name="a"></param>
        public void Dispatch(Action a)
        {
            Deployment.Current.Dispatcher.BeginInvoke(a);
        }

        /// <summary>
        /// Parses the input. Throws exception if it does not succeed.
        /// </summary>
        /// <param name="input"></param>
        public void TryCompile(string input)
        {
            var script = Engine.CreateScriptSourceFromString(input.TrimStart(), Microsoft.Scripting.SourceCodeKind.AutoDetect);
            var compiled = script.Compile(); 
        }
        
        public ScriptService()
        {
            Init();
        }
    }
}
