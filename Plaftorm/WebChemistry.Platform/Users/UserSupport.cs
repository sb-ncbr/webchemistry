namespace WebChemistry.Platform.Users
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Text;

    public class UserSupportSettings
    {
        public string Smtp { get; set; } 
        public int SmtpPort { get; set; }
        public string Email { get; set; }
        public string EmailPassword { get; set; }
    }

    public class UserComputationFinishedNotificationConfig
    {
        public string SettingsPath { get; set; }

        public string Email { get; set; }

        public string ServiceName { get; set; }

        /// <summary>
        /// -id- to be replaced by the actual computation id.
        /// </summary>
        public string ResultUrlTemplate { get; set; }
    }

    public static class UserComputationFinishedNotification
    {
        public static bool TrySend(UserComputationFinishedNotificationConfig config, string compShortId)
        {
            try
            {
                if (config == null) return false;

                var settings = JsonHelper.ReadJsonFile<UserSupportSettings>(config.SettingsPath);
                if (string.IsNullOrEmpty(settings.Email)) return false;

                using (var client = new SmtpClient(settings.Smtp, settings.SmtpPort) { Credentials = new NetworkCredential(settings.Email, settings.EmailPassword), EnableSsl = true, Timeout = 30 * 1000 })
                {
                    var body = new StringBuilder();
                    body.AppendLine("Hello,");
                    body.AppendLine("");
                    body.AppendLine(string.Format("the result of your computation can be viewed at {0}.", config.ResultUrlTemplate.Replace("-id-", compShortId)));
                    body.AppendLine("");
                    body.AppendLine("Have a nice day,");
                    body.AppendLine("WebChemistry Team");
                    client.Send(settings.Email, config.Email, string.Format("[WebChemistry Support] {0} computation has finished", config.ServiceName), body.ToString());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UserSupoprtSession : PersistentObjectBase<UserSupoprtSession>
    {
        public class Message
        {
            public DateTime Timestamp { get; set; }
            public bool FromSupport { get; set; }
            public string Body { get; set; }
        }

        public string Url { get; set; }
        public string Context { get; set; }
        
        public string Email { get; set; }
        public string Source { get; set; }

        public string Title { get; set; }

        public List<Message> Messages { get; set; }

        static void SendEmail(UserSupportSettings settings, string to, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(settings.Smtp, settings.SmtpPort) { Credentials = new NetworkCredential(settings.Email, settings.EmailPassword), EnableSsl = true })
                {
                    client.Send(settings.Email, to, subject, body);
                }
            }
            catch { }
        }        

        public void Unsubscribe()
        {
            Email = "";
            Save();
        }

        public Message PostMessage(UserSupportSettings settings, string message, bool fromSupport)
        {
            if (this.Messages.Count >= 100)
            {
                throw new InvalidOperationException("Message count limit (100) exceeded.");
            }

            var msg = new Message
            {
                Timestamp = DateTimeService.GetCurrentTime(),
                FromSupport = fromSupport,
                Body = message
            };
            if (Messages == null) Messages = new List<Message>();
            Messages.Add(msg);
            Save();

            if (fromSupport && !string.IsNullOrEmpty(Email))
            {
                var mail = new StringBuilder();
                mail.AppendLine("Hello,");
                mail.AppendLine("");
                mail.AppendLine(string.Format("a reply has been posted to your user support request for {0}.", Context));
                mail.AppendLine("");
                mail.AppendLine(string.Format("You can view the reply at {0}.", Url));
                mail.AppendLine("");
                mail.AppendLine("Have a nice day,");
                mail.AppendLine("WebChemistry Team");
                mail.AppendLine("");
                mail.AppendLine("-----------------------------");
                mail.AppendLine(string.Format("If you did not request any help or no longer wish to receive notifications for this user support request, please use this link: {0}", Url + "?type=unsubscribe"));
                SendEmail(settings, Email, string.Format("[WebChemistry Support] {0} - {1}", Context, Title), mail.ToString());
            }
            
            if (!fromSupport)
            {
                SendEmail(
                    settings,
                    settings.Email,
                    "[ActionRequired] " + Context + " - " + Title,
                    string.Format("Url {0}\nEmail {1}", Url + "?type=support", string.IsNullOrWhiteSpace(Email) ? "n/a" : Email));
            }

            return msg;
        }

        public static UserSupoprtSession Create(EntityId id, UserSupportSettings settings, string context, string email, string url, string source, string title, string initialMessage)
        {

            var ret = CreateAndSave(id, o =>
            {
                o.Context = context;
                o.Url = url;

                o.Title = title;
                o.Email = email;
                o.Source = source;

                o.Messages = new List<Message>();
            });

            ret.PostMessage(settings, initialMessage, false);
            if (!string.IsNullOrEmpty(email))
            {
                var mail = new StringBuilder();
                mail.AppendLine("Hello,");
                mail.AppendLine("");
                mail.AppendLine(string.Format("an user support request for {0} was created and can be viewed at {1}.", context, url));
                mail.AppendLine("");
                mail.AppendLine("Have a nice day,");
                mail.AppendLine("WebChemistry Team");
                mail.AppendLine("");
                mail.AppendLine("-----------------------------");
                mail.AppendLine(string.Format("If you did not request any help or no longer wish to receive the notifications for this request, please use this link: {0}", url + "?type=unsubscribe"));
                SendEmail(settings, email, string.Format("[WebChemistry Support] {0} - {1}", context, title), mail.ToString());
            }
            
            return ret;
        }
    }
}
