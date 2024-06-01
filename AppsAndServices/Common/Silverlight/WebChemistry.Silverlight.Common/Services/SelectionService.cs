using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Queries.Core;
using System;
using System.Collections.ObjectModel;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core.MetaQueries;
using WebChemistry.Queries.Core.Symbols;
using WebChemistry.Queries.Core.Queries;
using WebChemistry.Framework.TypeSystem;

namespace WebChemistry.Silverlight.Common.Services
{
    /// <summary>
    /// Service for selecting atoms using Queries.
    /// </summary>
    public class SelectionService
    {
        static SelectionService defaultSvc = new SelectionService();
        public static SelectionService Default
        {
            get { return defaultSvc; }
        }

        /// <summary>
        /// Returns true if the queries were executed successfully.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="queryString"></param>
        /// <param name="isAdditive"></param>
        /// <param name="structures"></param>
        /// <returns></returns>
        public async Task<bool> SelectAtoms<T>(SessionBase<T> session, string queryString, bool isAdditive, IList<T> structures = null)
            where T : StructureWrapBase<T>, new()
        {
            var cs = ComputationService.Default;
            var progress = cs.Start();

            try
            {
                progress.Update(statusText: "Selecting atoms...");

                var metaquery = ScriptService.Default.GetMetaQuery(queryString);
                if (!TypeExpression.Unify(metaquery.Type, BasicTypes.PatternSeq).Success)
                {
                    throw new ArgumentException(string.Format("Expected '{1}', got '{0}'.", metaquery.Type, BasicTypes.PatternSeq.ToString()));
                }

                var query = metaquery.Compile() as QueryMotive;
                
                QueryService.Default.AddQueryHistory(queryString);
                var selections = await TaskEx.Run(() =>
                    {
                        structures = structures ?? session.Structures.Where(s => s.IsSelected).ToArray();
                        progress.ThrowIfCancellationRequested();

                        List<HashSet<IAtom>> result = new List<HashSet<IAtom>>(structures.Count);

                        var ctx = session.QueryExecutionContext;

                        foreach (var s in structures)
                        {
                            ctx.CurrentContext = s.Structure.MotiveContext();
                            result.Add(s.Structure.GetQueryAtoms(query, ctx));
                        }

                        return result;
                    });
                progress.ThrowIfCancellationRequested();
                structures.Zip(selections, (l, r) => Tuple.Create(l, r)).ForEach(t =>
                   {
                       if (isAdditive) t.Item1.SelectAtoms(t.Item2);
                       else
                       {
                           t.Item1.SelectAtoms(t.Item1.Structure.Atoms, false);
                           t.Item1.SelectAtoms(t.Item2);
                       }
                   });
                return true;
            }
            catch (ComputationCancelledException)
            {
                LogService.Default.Info("Aborted.");
                return false;
            }
            catch (Exception e)
            {
                LogService.Default.Error("Selecting Atoms", e.Message);
                return false;
            }
            finally
            {
                cs.End();
            }
        }

        /// <summary>
        /// Passes already selected structures.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structures"></param>
        /// <param name="queryString"></param>
        /// <param name="motiveSymbol"></param>
        /// <returns></returns>
        public async Task<IList<T>> FilterSelectedStructures<T>(SessionBase<T> session, string queryString, bool isAdditive = true)
            where T : StructureWrapBase<T>, new()
        {
            var cs = ComputationService.Default;
            var progress = cs.Start();

            try
            {
                var xs = session.Structures.ToArray();
                progress.Update(statusText: "Selecting structures...", currentProgress: 0, maxProgress: xs.Length, isIndeterminate: false);

                //var innerQuery = MetaQuery.CreateLambda(new MetaSymbol(motiveSymbol), Query.ParseMeta(queryString));
                //Func<string, MetaQuery> query = m => MetaQuery.CreateApply(innerQuery, MetaTuple.Create(new MetaQuery[] { new MetaSymbol(SymbolTable.MotiveSymbol.Name).ApplyTo(MetaQuery.CreateValue(m)) }));

                //var query = Query.Parse(queryString, QueryTypes.Boolean);

                var metaquery = ScriptService.Default.GetMetaQuery(queryString);
                if (!TypeExpression.Unify(metaquery.Type, BasicTypes.Bool).Success)
                {
                    throw new ArgumentException(string.Format("Expected 'Bool', got '{0}'.", metaquery.Type));
                }

                var query = metaquery.Compile();

                QueryService.Default.AddQueryHistory(queryString);
                var filter = await TaskEx.Run(() =>
                {
                    var context = session.QueryExecutionContext;
                    progress.ThrowIfCancellationRequested();

                    List<T> passed = new List<T>();

                    for (int i = 0; i < xs.Length; i++)
                    {
                        var s = xs[i];
                        if (isAdditive && s.IsSelected) continue;
                        context.CurrentContext = s.Structure.MotiveContext();
                        context.CurrentMotive = context.CurrentContext.StructureMotive;
                        var pass = query.ExecuteObject(context);
                        if (pass != null && (bool)pass) passed.Add(s);
                        progress.Update(currentProgress: i);
                    }

                    return passed;
                });
                progress.ThrowIfCancellationRequested();
                return filter;
            }
            catch (ComputationCancelledException)
            {
                LogService.Default.Info("Aborted.");
                return new T[0];
            }
            catch (Exception e)
            {
                LogService.Default.Error("Selecting Structures", e.Message);
                return new T[0];
            }
            finally
            {
                var context = session.QueryExecutionContext;
                context.CurrentContext = null;
                context.CurrentMotive = null;
                cs.End();
            }
        }

        private SelectionService()
        {
        }
    }
}
