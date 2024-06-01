using System;
using System.Reactive.Subjects;

namespace WebChemistry.Framework.Core
{
    public enum LogEntryType
    {
        Message,
        Info,
        Error,
        Warning
    }

    public class LogService
    {
        public class Entry
        {
            public string Message { get; set; }
            public LogEntryType Type { get; set; }
        }

        static LogService defaultSvc = new LogService();
        public static LogService Default
        {
            get { return defaultSvc; }
        }

        Subject<Entry> entries = new Subject<Entry>();
        public IObservable<Entry> Entries { get { return entries; } }

        void Add(Entry entry)
        {
            entries.OnNext(entry);
        }

        public void Aborted()
        {
            Info("Aborted.");
        }

        public void Info(string format, params object[] args)
        {
            Add(new Entry { Message = string.Format(format, args), Type = LogEntryType.Info });
        }

        public void Warning(string format, params object[] args)
        {
            Add(new Entry { Message = string.Format(format, args), Type = LogEntryType.Warning });
        }

        public void Error(string context, string format, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                Add(new Entry { Message = string.Format(format, args), Type = LogEntryType.Error });
            }
            else
            {
                Add(new Entry { Message = "[" + context + "] " + string.Format(format, args), Type = LogEntryType.Error });
            }
        }

        public void Message(string format, params object[] args)
        {
            Add(new Entry { Message = string.Format(format, args), Type = LogEntryType.Message });
        }

        public LogService()
        {
            entries = new Subject<Entry>();
        }
    }
}
