using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TeleClassic
{
    public static class Logger
    {
        private class LogEvent
        {
            public readonly string Category;
            public readonly string AssociatedUser;
            public readonly string Description;
            public readonly DateTime Time;

            public LogEvent(string category, string associatedUser, string description, DateTime time)
            {
                Category = category;
                AssociatedUser = associatedUser;
                Description = description;
                Time = time;
            }

            public void WriteBack(StringBuilder stringBuilder)
            {
                stringBuilder.Append("[");
                stringBuilder.Append(Category);
                stringBuilder.Append("-");
                stringBuilder.Append(Time);
                stringBuilder.Append("]:");
                stringBuilder.Append(Description);
                stringBuilder.Append("(");
                stringBuilder.Append(AssociatedUser);
                stringBuilder.AppendLine(")");
            }
        }

        private static readonly List<LogEvent> events;

        static Logger()
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");
            events = new List<LogEvent>();
        }

        public static void Log(string category, string description, string associatedUser)
        {
            if (category == "Info")
                Console.WriteLine("[" + category + "-" + DateTime.Now + "]:" + description + "(" + associatedUser + ")");
            events.Add(new LogEvent(category, associatedUser, description, DateTime.Now));
        }

        public static void EndSession()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (LogEvent logEvent in events)
                logEvent.WriteBack(stringBuilder);
            File.WriteAllText("logs//log_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt", stringBuilder.ToString());
        }

        public static void PrintAll()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (LogEvent logEvent in events)
                logEvent.WriteBack(stringBuilder);
            Console.WriteLine(stringBuilder.ToString());
        }
    }
}