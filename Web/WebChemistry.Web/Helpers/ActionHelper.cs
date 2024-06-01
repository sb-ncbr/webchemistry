using Newtonsoft.Json;
using System.Text;
using System.Web.Mvc;

namespace WebChemistry.Web.Helpers
{
    public static class ActionHelper
    {
        public static ActionResult AsJsonResult<T>(this T obj)
        {
            return new ContentResult { Content = JsonConvert.SerializeObject(obj, Formatting.Indented), ContentEncoding = Encoding.UTF8, ContentType = "application/json" };
        }

        public static ActionResult JsonContent(string json)
        {
            return new ContentResult { Content = json, ContentEncoding = Encoding.UTF8, ContentType = "application/json" };
        }

        public static ActionResult FileJsonContent(string filename)
        {
            return new ContentResult { Content = System.IO.File.ReadAllText(filename), ContentEncoding = Encoding.UTF8, ContentType = "application/json" };
        }

        public static ActionResult FileTextContent(string filename)
        {
            return new ContentResult { Content = System.IO.File.ReadAllText(filename), ContentEncoding = Encoding.UTF8, ContentType = "text/plain" };
        }

        public static ActionResult TextContent(string text)
        {
            return new ContentResult { Content = text, ContentEncoding = Encoding.UTF8, ContentType = "text/plain" };
        }

        public static ActionResult JavaScriptContent(string text)
        {
            return new ContentResult { Content = text, ContentEncoding = Encoding.UTF8, ContentType = "text/javascript" };
        }

        public static ActionResult CsvContent(string text)
        {
            return new ContentResult { Content = text, ContentEncoding = Encoding.UTF8, ContentType = "text/csv" };
        }
    }
}