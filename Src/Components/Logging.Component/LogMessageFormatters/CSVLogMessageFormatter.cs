using Core.Interfaces.Components.Logging;

namespace Core.Logging.LogMessageFormats
{
    public class CSVLogMessageFormatter : ILogMessageFormatter
    {
        public string Format(LogMessage message)
        {
            return string.Format("{0},{1},{2},\"{3}\",\"{4}\",{5},{6},{7},{8},{9},{10}",
                message.TimeStamp,
                message.Category,
                message.Severity,
                (message.Message != null) ? message.Message.Replace("\"", "\"\"") : null,
                (message.Detail != null) ? message.Detail.Replace("\"", "\"\"") : null,
                message.MachineName,
                message.ProcessName,
                message.ProcessId,
                message.FilePath,
                message.CallerName,
                message.LineNumber);
        }

        public string GetHeader()
        {
            return "TimeStamp,Category,Severity,Message,Detail,MachineName,ProcessName,ProcessId,FilePath,CallerName,LineNumber";
        }
    }
}
