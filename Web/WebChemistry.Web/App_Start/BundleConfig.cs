//using System;
//using System.IO;
//using System.IO.Compression;
//using System.Text;
//using System.Web;
//using System.Web.Optimization;

using System;
using System.Web.Optimization;
namespace WebChemistry.Web
{
    public class BundleConfig
    {
        //public class GZipTransform : IBundleTransform
        //{
        //    string _contentType;

        //    public GZipTransform(string contentType)
        //    {
        //        _contentType = contentType;
        //    }

        //    public void Process(BundleContext context, BundleResponse response)
        //    {
        //        var contentBytes = new UTF8Encoding().GetBytes(response.Content);

        //        using (var outputStream = new MemoryStream())
        //        using (var gzipOutputStream = new GZipStream(outputStream, CompressionMode.Compress))
        //        {
        //            gzipOutputStream.Write(contentBytes, 0, contentBytes.Length);
        //            gzipOutputStream.Flush();

        //            var outputBytes = outputStream.GetBuffer();
        //            response.Content = Convert.ToBase64String(outputBytes);
        //        }
                
        //        // NOTE: this part is broken
        //        context.HttpContext.Response.Headers["Content-Encoding"] = "gzip";
        //        response.ContentType = _contentType;
        //    }
        //}

        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            //addBundle(new ScriptBundle("~/bundles/browserdetect").Include(
            //            "~/Scripts/browser-detection.js"));

            Action<Bundle> addBundle = b =>
                {
                    //b.Transforms.Add(new GZipTransform(b is ScriptBundle ? "text/javascript" : "text/css"));
                    bundles.Add(b);
                };
                        
            addBundle(new ScriptBundle("~/bundles/jquery")
                .Include("~/Scripts/browser-detection.js")
                .Include("~/Scripts/GoogleAnalytics.js")
                .Include("~/Scripts/jquery-{version}.js", "~/Scripts/spin.js", "~/Scripts/lodash.js"));

            addBundle(new ScriptBundle("~/bundles/jqueryui").Include(
                        "~/Scripts/jquery-ui-{version}.js"));

            addBundle(new ScriptBundle("~/bundles/bootstrap").Include(
                        "~/Scripts/bootstrap.js"));

            addBundle(new ScriptBundle("~/bundles/chemdoodle").Include(
                        //"~/Scripts/ChemDoodle/glMatrix-0.9.5.js",
                        //"~/Scripts/jquery.mousewheel.js",
                        "~/Scripts/ChemDoodle/ChemDoodleWeb.js"));

            addBundle(new ScriptBundle("~/bundles/highcharts").Include(
                        "~/Scripts/highcharts/highcharts.js",
                        "~/Scripts/highcharts/modules/exporting.js",
                        "~/Scripts/highcharts/modules/data.js"));

            addBundle(new ScriptBundle("~/bundles/knockout").Include(
                        "~/Scripts/knockout-{version}.js"));

            addBundle(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.unobtrusive*",
                        "~/Scripts/jquery.validate*"));

            addBundle(new ScriptBundle("~/bundles/jqdrag").Include(
                        "~/Scripts/jquery.event.drag-2.2.js"));

            addBundle(new ScriptBundle("~/bundles/slickgrid").Include(
                        "~/Scripts/SlickGrid/slick.core.js",
                        "~/Scripts/SlickGrid/slick.grid.js",
                        "~/Scripts/SlickGrid/slick.dataview.js"));

            addBundle(new ScriptBundle("~/bundles/LiteMol")
                .Include("~/Scripts/LiteMol/lib/three.js", "~/Scripts/LiteMol/lib/*.js", "~/Scripts/LiteMol/LiteMol.js"));

            addBundle(new ScriptBundle("~/bundles/ace")
                .Include("~/Scripts/ace/ace.js"));

            addBundle(new ScriptBundle("~/bundles/validatorindex")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/helpers/bootstrap-fileupload.js")
                .Include("~/Scripts/WebChemCommon/ajax-form-uploader.js")
                .Include("~/Scripts/WebChemCommon/recently-submitted.js")
                .Include("~/Scripts/MotiveValidator/index.js"));
            
            addBundle(new ScriptBundle("~/bundles/validator-result")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/jquery.event.drag-2.2.js")
                .Include("~/Scripts/highcharts/highcharts.js", "~/Scripts/highcharts/modules/exporting.js", "~/Scripts/highcharts/modules/data.js")
                .Include("~/Scripts/SlickGrid/slick.core.js", "~/Scripts/SlickGrid/slick.grid.js", "~/Scripts/SlickGrid/slick.dataview.js")
                .Include("~/Scripts/ChemDoodle/ChemDoodleWeb.js")
                .Include("~/Scripts/MotiveValidator/validator-globals.js")
                .Include("~/Scripts/MotiveValidator/result-errors.js")
                .Include("~/Scripts/MotiveValidator/result-details-notvalidated.js")
                .Include("~/Scripts/MotiveValidator/overview-plot.js")
                .Include("~/Scripts/MotiveValidator/result-overview.js")
                .Include("~/Scripts/MotiveValidator/result-summaryanalysis.js")
                .Include("~/Scripts/MotiveValidator/result.js")
                .Include("~/Scripts/intro.js")
                .Include("~/Scripts/MotiveValidator/result-intro.js"));

            addBundle(new ScriptBundle("~/bundles/validatordb-index")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/jquery.event.drag-2.2.js")
                .Include("~/Scripts/SlickGrid/slick.core.js", "~/Scripts/SlickGrid/slick.grid.js", "~/Scripts/SlickGrid/slick.dataview.js")
                .Include("~/Scripts/highcharts/highcharts.js", "~/Scripts/highcharts/modules/exporting.js", "~/Scripts/highcharts/modules/data.js")
                .Include("~/Scripts/jquery.csvparse.js")
                .Include("~/Scripts/WebChemCommon/recently-submitted.js")
                .Include("~/Scripts/MotiveValidator/validator-globals.js")
                .Include("~/Scripts/MotiveValidator/overview-plot.js")
                .Include("~/Scripts/MotiveValidator/db-index.js")
                .Include("~/Scripts/MotiveValidator/db-details.js")
                .Include("~/Scripts/MotiveValidator/db-custom.js")
                .Include("~/Scripts/MotiveValidator/db-wwpdbdict.js")
                .Include("~/Scripts/intro.js")
                .Include("~/Scripts/MotiveValidator/db-index-intro.js"));

            addBundle(new ScriptBundle("~/bundles/validatordb-index-custom")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/jquery.event.drag-2.2.js")
                .Include("~/Scripts/SlickGrid/slick.core.js", "~/Scripts/SlickGrid/slick.grid.js", "~/Scripts/SlickGrid/slick.dataview.js")
                .Include("~/Scripts/highcharts/highcharts.js", "~/Scripts/highcharts/modules/exporting.js", "~/Scripts/highcharts/modules/data.js")
                .Include("~/Scripts/jquery.csvparse.js")
                .Include("~/Scripts/WebChemCommon/StatusAndResult.js")
                .Include("~/Scripts/MotiveValidator/validator-globals.js")
                .Include("~/Scripts/MotiveValidator/overview-plot.js")
                .Include("~/Scripts/MotiveValidator/db-index-custom.js")
                .Include("~/Scripts/MotiveValidator/db-details.js")
                .Include("~/Scripts/MotiveValidator/db-wwpdbdict.js"));

            addBundle(new ScriptBundle("~/bundles/ChargeCalculatorIndex")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/helpers/bootstrap-fileupload.js")
                .Include("~/Scripts/WebChemCommon/recently-submitted.js")
                .Include("~/Scripts/WebChemCommon/ajax-form-uploader.js")
                .Include("~/Scripts/ChargeCalculator/Index.js"));

            addBundle(new ScriptBundle("~/bundles/slickgrid-ChargeCalculatorConfig").Include(
                        "~/Scripts/SlickGrid/slick.core.js",
                        "~/Scripts/SlickGrid/slick.formatters.js",
                        "~/Scripts/SlickGrid/slick.editors.js",
                        "~/Scripts/SlickGrid/slick.grid.js",
                        "~/Scripts/SlickGrid/slick.groupitemmetadataprovider.js",
                        "~/Scripts/SlickGrid/slick.dataview.js",
                        "~/Scripts/SlickGrid/Plugins/slick.rowselectionmodel.js"));

            addBundle(new ScriptBundle("~/bundles/ChargeCalculatorConfig")
                .Include("~/Scripts/ChargeCalculator/codemirror.js")
                .Include("~/Scripts/ChargeCalculator/codemirror-xml.js")
                .Include("~/Scripts/ChargeCalculator/slick-ChargeTextEditor.js")
                .Include("~/Scripts/ChargeCalculator/Config-SelectorColumn.js")
                .Include("~/Scripts/ChargeCalculator/Config.js")
                .Include("~/Scripts/WebChemCommon/StatusAndResult.js")
                .Include("~/Scripts/ChargeCalculator/Config-Method.js")
                .Include("~/Scripts/intro.js")
                .Include("~/Scripts/ChargeCalculator/Config-Intro.js"));

            addBundle(new ScriptBundle("~/bundles/ChargeCalculatorResult")
                .Include("~/Scripts/ChargeCalculator/Result.js")
                .Include("~/Scripts/WebChemCommon/StatusAndResult.js")
                .Include("~/Scripts/intro.js")
                .Include("~/Scripts/ChargeCalculator/Result-Intro.js"));

            addBundle(new ScriptBundle("~/bundles/ChargeCalculatorDetails")
                .Include("~/Scripts/highcharts/highcharts.js", "~/Scripts/highcharts/modules/exporting.js", "~/Scripts/highcharts/modules/data.js")
                .Include("~/Scripts/jquery.event.drag-2.2.js")
                .Include("~/Scripts/SlickGrid/slick.core.js", "~/Scripts/SlickGrid/slick.grid.js", "~/Scripts/SlickGrid/slick.dataview.js")
                .Include("~/Scripts/ChargeCalculator/Details.js")
                .Include("~/Scripts/ChargeCalculator/Details-aggregates.js")
                .Include("~/Scripts/ChargeCalculator/Details-correlations.js")
                .Include("~/Scripts/ChargeCalculator/Details-3d.js")
                .Include("~/Scripts/WebChemCommon/StatusAndResult.js")
                .Include("~/Scripts/intro.js")
                .Include("~/Scripts/jquery.colorPicker.js")
                .Include("~/Scripts/ChargeCalculator/Details-Intro.js"));

            addBundle(new ScriptBundle("~/bundles/CreateDbView")
                .Include("~/Scripts/Data/CreateView.js"));

            addBundle(new ScriptBundle("~/bundles/PatternQueryIndex")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/WebChemCommon/recently-submitted.js")
                .Include("~/Scripts/ace/ace.js")
                .Include("~/Scripts/PatternQuery/metadata*")
                .Include("~/Scripts/PatternQuery/query-editor.js")
                .Include("~/Scripts/PatternQuery/configure/*.js")
                .Include("~/Scripts/PatternQuery/support/support-submit.js")
                .Include("~/Scripts/intro.js"));

            addBundle(new ScriptBundle("~/bundles/PatternQueryResult")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/ChemDoodle/ChemDoodleWeb.js")
                .Include("~/Scripts/jquery.event.drag-2.2.js")
                .Include("~/Scripts/SlickGrid/slick.core.js", "~/Scripts/SlickGrid/slick.grid.js", "~/Scripts/SlickGrid/slick.dataview.js")
                .Include("~/Scripts/WebChemCommon/StatusAndResult.js")
                .Include("~/Scripts/WebChemCommon/csv-exporter.js")
                .Include("~/Scripts/PatternQuery/metadata-info.js")
                .Include("~/Scripts/PatternQuery/utils.js")
                .Include("~/Scripts/PatternQuery/result/*.js")
                .Include("~/Scripts/intro.js"));

            addBundle(new ScriptBundle("~/bundles/PatternQueryExplorer")
                .Include("~/Scripts/WebChemCommon/ajax-form-uploader.js")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/ChemDoodle/ChemDoodleWeb.js")
                .Include("~/Scripts/jquery.event.drag-2.2.js")
                .Include("~/Scripts/SlickGrid/slick.core.js", "~/Scripts/SlickGrid/slick.grid.js", "~/Scripts/SlickGrid/slick.dataview.js")
                .Include("~/Scripts/ace/ace.js")
                .Include("~/Scripts/PatternQuery/query-editor.js")
                .Include("~/Scripts/PatternQuery/utils.js")
                .Include("~/Scripts/PatternQuery/metadata*")
                .Include("~/Scripts/PatternQuery/configure/configure-list.js")
                .Include("~/Scripts/PatternQuery/explorer/*.js")
                .Include("~/Scripts/WebChemCommon/csv-exporter.js")
                .Include("~/Scripts/intro.js"));

            addBundle(new ScriptBundle("~/bundles/PatternQuerySupportThread")
                .Include("~/Scripts/knockout-{version}.js")
                .Include("~/Scripts/PatternQuery/support/support-thread.js"));

            addBundle(new ScriptBundle("~/bundles/aceTools")
                .Include("~/Scripts/ace/ext-language_tools.js"));
            
            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            addBundle(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            addBundle(new StyleBundle("~/Content/css")
                .Include("~/Content/site.css")
                .Include("~/Content/browser-detection.css")
                .Include("~/Content/perfect-scrollbar.css")
                //.Include("~/Content/bootstrap-responsive.css")
                .Include("~/Content/bootstrap.css")
                .Include("~/Content/bootstrap-fileupload.css"));
                //.Include("~/Content/bootstrap*"));

            addBundle(new StyleBundle("~/Content/slickgrid/css")
                .Include("~/Content/slick.grid.css")
                /*.Include("~/Content/slick.jquery.ui.css")*/);
                //.Include("~/Content/slick*"));
                //.Include("~/Content/themes/base/jquery.ui.*"));


            addBundle(new StyleBundle("~/Content/PatternQueryIndex/css")
                .Include("~/Content/PatternQuery/PatternQueryIndex.css")
                .Include("~/Content/PatternQuery/PatternQueryEditor.css")
                .Include("~/Content/introjs.css"));

            addBundle(new StyleBundle("~/Content/PatternQueryResult/css")
                .Include("~/Content/PatternQuery/PatternQueryResult.css")
                .Include("~/Content/PatternQuery/PatternQueryGrids.css")
                .Include("~/Content/introjs.css"));

            addBundle(new StyleBundle("~/Content/PatternQueryExplorer/css")
                .Include("~/Content/PatternQuery/PatternQueryExplorer.css")
                .Include("~/Content/PatternQuery/PatternQueryGrids.css")
                .Include("~/Content/PatternQuery/PatternQueryEditor.css")
                .Include("~/Content/introjs.css"));

            addBundle(new StyleBundle("~/Content/PatternQuerySupport/css")
                .Include("~/Content/PatternQuery/PatternQuerySupport.css"));

            addBundle(new StyleBundle("~/Content/MotiveValidator/css")
                .Include("~/Content/MotiveValidator.css")
                .Include("~/Content/introjs.css"));
            
            addBundle(new StyleBundle("~/Content/ChargeCalculator/css")
                .Include("~/Content/ChargeCalculator.css")
                .Include("~/Content/slick.columnpicker.css")
                .Include("~/Content/introjs.css"));

            addBundle(new StyleBundle("~/Content/ChargeCalculatorConfig/css")
                .Include("~/Content/codemirror.css")
                .Include("~/Content/ChargeCalculator.css")
                .Include("~/Content/slick.columnpicker.css")
                .Include("~/Content/introjs.css"));

            addBundle(new StyleBundle("~/Content/themes/base/css").Include(
                        "~/Content/themes/base/jquery.ui.core.css",
                        "~/Content/themes/base/jquery.ui.resizable.css",
                        "~/Content/themes/base/jquery.ui.selectable.css",
                        "~/Content/themes/base/jquery.ui.accordion.css",
                        "~/Content/themes/base/jquery.ui.autocomplete.css",
                        "~/Content/themes/base/jquery.ui.button.css",
                        "~/Content/themes/base/jquery.ui.dialog.css",
                        "~/Content/themes/base/jquery.ui.slider.css",
                        "~/Content/themes/base/jquery.ui.tabs.css",
                        "~/Content/themes/base/jquery.ui.datepicker.css",
                        "~/Content/themes/base/jquery.ui.progressbar.css",
                        "~/Content/themes/base/jquery.ui.theme.css"));
        }
    }
}