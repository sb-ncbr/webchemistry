using System;
using System.Linq;
using System.Web.Mvc;
using WebChemistry.Platform.Users;
using WebChemistry.Framework.Core;
using WebChemistry.Web.Helpers;
using System.Text.RegularExpressions;
using System.Text;

namespace WebChemistry.Web.Controllers
{
    public partial class PatternQueryController : AppControllerBase
    {
        static readonly MarkdownSharp.Markdown Markdown = new MarkdownSharp.Markdown(new MarkdownSharp.MarkdownOptions { AutoHyperlink = true });

        static UserInfo GetSupportUser()
        {
            var user = ServerHelper.Atlas.Users.TryGetByShortId("PatternQuerySupport");
            if (user == null) user = ServerHelper.Atlas.Users.CreateUser("PatternQuerySupport");
            return user;
        }

        static UserSupportSettings GetSupportSettings()
        {
            return new UserSupportSettings
            {
                Email = "webchemistryhelp@gmail.com",
                EmailPassword = "W3bCh3m.",
                Smtp = "smtp.gmail.com",
                SmtpPort = 587
            };
        }

        static UserSupoprtSession GetSupportSession(string id)
        {
            return UserSupoprtSession.TryLoad(GetSupportUser().Repository.GetChildEntityId((id ?? "").Trim()));
        }

        static Random rnd = new Random();
        public ActionResult Support(string id, string type)
        {
            type = (type ?? "").Trim();
            var session = GetSupportSession(id);

            if (session == null)
            {
                return HttpNotFound();

                //var answer = rnd.Next(30, 60);
                //HttpContext.Session["MqSupportCaptchaAnswer"] = answer;
                //ViewBag.Token = Convert.ToBase64String(Encoding.UTF8.GetBytes(answer.ToString()));
                //return View("SupportSubmit");
            }

            if (type.Equals("unsubscribe", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    session.Unsubscribe();
                }
                catch { }
                return "Unsubscribed.".AsJsonResult();
            }

            if (type.Equals("messages", StringComparison.OrdinalIgnoreCase))
            {
                return session.Messages
                    .Select(m => new { timestamp = m.Timestamp.ToString() + " UTC", body = Markdown.Transform(m.Body), fromSupport = m.FromSupport })
                    .ToArray()
                    .AsJsonResult();
            }

            ViewBag.IsSupport = type.Equals("support", StringComparison.OrdinalIgnoreCase);
            ViewBag.Id = id.Trim();
            ViewBag.SupportTitle = session.Title;
            return View("SupportThread");
        }

        [HttpPost]
        public ActionResult SupportSubmit(string email, string answer, string title, string message)
        {
            try
            {
                email = (email ?? "");
                if (email.Length > 0)
                {
                    var emailRegexp = new Regex(@"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$", RegexOptions.IgnoreCase | RegexOptions.ECMAScript);
                    if (!emailRegexp.IsMatch(email)) return new { error = true, message = "Invalid e-mail address." }.AsJsonResult();
                }

                message = (message ?? "").Trim();
                if (string.IsNullOrWhiteSpace(message)) return new { error = true, message = "The message cannot be empty." }.AsJsonResult();
                if (message.Length > 5000) return new { error = true, message = "The message is too long." }.AsJsonResult();

                title = (title ?? "").Trim();
                if (string.IsNullOrWhiteSpace(title)) return new { error = true, message = "The title cannot be empty." }.AsJsonResult();
                if (title.Length > 50) return new { error = true, message = "The title is too long." }.AsJsonResult();

                answer = (answer ?? "");
                int val;
                if (!int.TryParse(answer, out val))
                {
                    var newAnswer = rnd.Next(30, 60);
                    HttpContext.Session["MqSupportCaptchaAnswer"] = newAnswer;
                    return new { error = true, message = "Invalid question answer. The question might have changed because your session expired.", type = "captcha", token = Convert.ToBase64String(Encoding.UTF8.GetBytes(newAnswer.ToString())) }.AsJsonResult();
                }

                var guid = Guid.NewGuid().ToString();
                var id = GetSupportUser().Repository.GetChildEntityId(guid);
                UserSupoprtSession.Create(id, GetSupportSettings(), "PatternQuery", email, Url.Action("Support", "PatternQuery", new { id = guid }, Request.Url.Scheme), UserHelper.GetUserIP(Request), title, message);

                return new { id = guid }.AsJsonResult();
            }
            catch (InvalidOperationException e)
            {
                return new { error = true, message = e.Message }.AsJsonResult();
            }
            catch
            {
                return new { error = true, message = "Submit failed. Please try again later." }.AsJsonResult();
            }
        }

        [HttpPost]
        public ActionResult SupportSubmitUser(string id, string message)
        {
            try
            {
                var session = GetSupportSession(id);
                if (session == null) return new { error = true, message = "Session does not exist." }.AsJsonResult();
                var m = session.PostMessage(GetSupportSettings(), message, false);
                return new { timestamp = m.Timestamp.ToString() + " UTC", body = Markdown.Transform(m.Body), fromSupport = m.FromSupport }.AsJsonResult();
            }
            catch (InvalidOperationException e)
            {
                return new { error = true, message = e.Message }.AsJsonResult();
            }
            catch
            {
                return new { error = true, message = "Submit failed. Please try again later." }.AsJsonResult();
            }
        }

        [HttpPost]
        public ActionResult SupportSubmitSupport(string id, string message)
        {
            try
            {
                var session = GetSupportSession(id);
                if (session == null) return new { error = true, message = "Session does not exist." }.AsJsonResult();
                var m = session.PostMessage(GetSupportSettings(), message, true);
                return new { timestamp = m.Timestamp.ToString() + " UTC", body = Markdown.Transform(m.Body), fromSupport = m.FromSupport }.AsJsonResult();
            }
            catch (InvalidOperationException e)
            {
                return new { error = true, message = e.Message }.AsJsonResult();
            }
            catch
            {
                return new { error = true, message = "Submit failed. Please try again later." }.AsJsonResult();
            }
        }
    }
}