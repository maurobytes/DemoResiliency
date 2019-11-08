using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using ConsoleClient.OutputHelpers;

namespace ConsoleClient
{
    public abstract class DemoBase
    {
 
        protected bool TerminateDemosByKeyPress { get; } = (ConfigurationManager.AppSettings["TerminateDemosByKeyPress"] ?? string.Empty).Equals(Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase);

        public virtual string Description
        {
            get { return $"[Description for demo {GetType().Name} not yet provided.]"; }
        }

        public abstract Statistic[] LatestStatistics { get; }

        public Progress ProgressWithMessage(string message)
        {
            return new Progress(LatestStatistics, new ColoredMessage(message, Color.Default));
        }

        public Progress ProgressWithMessage(string message, Color color)
        {
            return new Progress(LatestStatistics, new ColoredMessage(message, color));
        }

        public Progress ProgressWithMessages(IEnumerable<ColoredMessage> messages)
        {
            return new Progress(LatestStatistics, messages);
        }
    }
}
