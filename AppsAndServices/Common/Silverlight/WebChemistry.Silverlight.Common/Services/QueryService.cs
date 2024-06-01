using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Core.Queries;
using WebChemistry.Silverlight.Common.DataModel;

namespace WebChemistry.Silverlight.Common.Services
{
    /// <summary>
    /// Service for selecting atoms using Queries.
    /// </summary>
    public class QueryService
    {
        static QueryService defaultSvc = new QueryService();
        public static QueryService Default
        {
            get { return defaultSvc; }
        }

        HashSet<string> uniqueQueries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ObservableCollection<string> QueryHistory { get; private set; }

        /// <summary>
        /// Add a query to history.
        /// </summary>
        /// <param name="query"></param>
        public void AddQueryHistory(string query)
        {
            query = query.Trim();
            if (uniqueQueries.Add(query)) QueryHistory.Insert(0, query);
        }

        /// <summary>
        /// Export history.
        /// </summary>
        /// <returns></returns>
        public XElement ExportHistory()
        {
            return new XElement("QueryHistory",
                QueryHistory.Select(e => new XElement("Entry", e)).ToArray());
        }

        /// <summary>
        /// Load history.
        /// </summary>
        /// <param name="xml"></param>
        public void LoadHistory(XElement xml)
        {
            xml.Elements().ForEach(e => AddQueryHistory(e.Value));
        }
        
        /// <summary>
        /// Passes already selected structures.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structures"></param>
        /// <param name="queryString"></param>
        /// <param name="motiveSymbol"></param>
        /// <returns></returns>
        public async void Execute<T>(SessionBase<T> session, string queryString, bool export)
            where T : StructureWrapBase<T>, new()
        {
            if (export)
            {
                ExecuteExport(session, queryString);
                return;
            }

            var cs = ComputationService.Default;
            var progress = cs.Start();
            try
            {
                progress.Update(statusText: "Executing query...", canCancel: false);
                var query = Query.Parse(queryString);
                var result = await TaskEx.Run(() =>
                {
                    try
                    {
                        session.QueryExecutionContext.CurrentContext = null;
                        return query.ExecuteDynamic(session.QueryExecutionContext);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Runtime error: " + e.Message);
                    }
                });

                var log = LogService.Default;

                if (query is QueryValueBase) log.Message(result.ToString());
                else if (query is Query<Motive>)
                {
                    var atoms = string.Join(", ", (result as Motive).Atoms.OrderBy(a => a.Id).Select(a => a.PdbName() + " " + a.Id));
                    log.Message("<{0}>", atoms);
                }
                else if (query is QueryMotive) log.Message("<Patterns>");
                else log.Message(result.ToString());

                AddQueryHistory(queryString);
                session.QueryString = "";
            }
            catch (Exception e)
            {
                LogService.Default.Error("Query", e.Message);
            }
            finally
            {
                cs.End();
            }
        }

        async void ExecuteExport<T>(SessionBase<T> session, string queryString)
            where T : StructureWrapBase<T>, new()
        {
            var log = LogService.Default;

            var sfd = new SaveFileDialog
            {
                Filter = "Zip files (*.zip)|*.zip"
            };

            if (sfd.ShowDialog() == true)
            {
                var cs = ComputationService.Default;
                var progress = cs.Start();
                progress.UpdateIsIndeterminate(true);
                progress.UpdateCanCancel(false);
                progress.UpdateStatus("Executing query...");
                try
                {
                    using (var stream = sfd.OpenFile())
                    {
                        var query = Query.Parse(queryString);
                        var result = await TaskEx.Run(() =>
                        {
                            try
                            {
                                session.QueryExecutionContext.CurrentContext = null;
                                return query.ExecuteDynamic(session.QueryExecutionContext);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Runtime error: " + e.Message);
                            }
                        });

                        progress.UpdateStatus("Exporting...");

                        await ZipUtils.CreateZip(stream, zip => TaskEx.Run(() =>
                        {
                            if (query is QueryValueBase)
                            {
                                var s = result.ToString();
                                log.Message(s);
                                zip.BeginEntry("result.txt");
                                zip.WriteString(s);
                                zip.EndEntry();
                            }
                            else if (query is Query<Motive>)
                            {
                                var m = (result as Motive);
                                var atoms = string.Join(", ", m.Atoms.OrderBy(a => a.Id).Select(a => a.PdbName() + " " + a.Id));
                                log.Message("<{0}>", atoms);                                
                                var s = m.ToStructure("0", true, true);
                                zip.BeginEntry(s.Id + ".pdb");
                                s.WritePdb(zip.TextWriter);
                                zip.EndEntry();
                            }
                            else if (query is QueryMotive)
                            {
                                log.Message("<Patterns>");
                                var index = 0;
                                foreach (Motive m in result)
                                {
                                    var s = m.ToStructure((index++).ToString(), true, true);
                                    zip.BeginEntry(s.Id + ".pdb");
                                    s.WritePdb(zip.TextWriter);
                                    zip.EndEntry();
                                }
                            }
                            else
                            {
                                var s = result.ToString();
                                log.Message(s);
                                zip.BeginEntry("result.txt");
                                zip.WriteString(s);
                                zip.EndEntry();
                            }
                        }));
                        log.Message("Export successful.");

                        AddQueryHistory(queryString);
                        session.QueryString = "";
                    }               
                }
                catch (Exception e)
                {
                    LogService.Default.Error("Query", e.Message);
                }
                finally
                {
                    cs.End();
                }
            }
        }

        private QueryService()
        {
            QueryHistory = new ObservableCollection<string>();
        }
    }
}
