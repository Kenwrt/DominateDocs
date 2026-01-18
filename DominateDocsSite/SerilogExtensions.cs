using Serilog.Core;
using Serilog.Events;
using System.Text;

namespace DominateDocsSite
{
    public class ExceptionEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Exception == null)
                return;

            var logEventProperty = propertyFactory.CreateProperty("EscapedException", logEvent.Exception.GetAllExceptionMessagesAsString());
            logEvent.AddPropertyIfAbsent(logEventProperty);
        }
    }

    public static class ExceptionExtensions
    {
        public static string GetAllExceptionMessagesAsString(this Exception ex)
        {
            StringBuilder msg = new StringBuilder();
            Exception currentEx = ex;
            msg.Append(currentEx.Message);
            while (currentEx.InnerException != null)
            {
                currentEx = currentEx.InnerException;
                msg.Append($"; INNER EXCEPTION: {currentEx.Message}");
            }
            return msg.ToString().Replace("\r\n", string.Empty);
        }
    }
}