using Core.Interfaces.Components.Logging;
using Core.Logging.LogDestinationConfigs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Core.Logging.LogMessageFormats;


namespace Core.Logging.LogDestinations
{
    public sealed class EventViewerDestination : LogDestinationBase
    {
        #region Fields

        private readonly EventViewerDestinationConfig _config;

        #endregion


        #region Constructor

        public EventViewerDestination(EventViewerDestinationConfig config)
        {
            if (null != config)
            {
                if (null == config.LogMessageFormatter)
                {
                    config.LogMessageFormatter = new CSVLogMessageFormatter();
                }

                _config = config;
            }
            else
                throw new ArgumentNullException("config");

            Create();
        }

        #endregion


        #region Private Methods



        private void Create()
        {
            try
            {
                if (!string.IsNullOrEmpty(_config.Source))
                {
                    if (!EventLog.SourceExists(_config.Source))
                    {
                        EventLog.CreateEventSource(_config.Source, _config.LogName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.HandleLoggingException("Error in EventViewerDestination :" + ex.Message, ex);
            }

        }

        private void WriteLine(LogMessage message)
        {
            try
            {
                EventLog.WriteEntry(_config.Source, _config.LogMessageFormatter.Format(message));
            }
            catch (Exception ex)
            {
                _logger.HandleLoggingException("Failed to write message to Event Viewer :" + ex.Message, ex);
            }
        }

        #endregion


        #region Public Methods



        public override void ReportMessages(List<LogMessage> messages)
        {
            foreach (LogMessage message in messages)
            {
                // only log critical messages to the event viewer
                if (message.Severity == LogMessageSeverity.Critical)
                {
                    WriteLine(message);
                }
            }
        }



        #endregion
    }
}
