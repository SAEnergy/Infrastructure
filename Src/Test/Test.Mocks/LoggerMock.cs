using Core.Interfaces.Components.Logging;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System;
using Core.Models;
using System.Collections.Generic;

namespace Test.Mocks
{
    public class LoggerMock : ILogger
    {
        public bool IsRunning { get; private set; }

        public string FriendName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Description
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ComponentUserActions AllowedUserActions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public List<Type> Proxies
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IReadOnlyList<ILogDestination> LogDestinations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void AddLogDestination(ILogDestination logDestination)
        {
            //ignore
        }

        public void HandleLoggingException(string message, Exception exception= null, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            Trace.TraceInformation(message);
        }

        public void Log(LogMessage logMessage)
        {
            Trace.TraceInformation(logMessage.Message);
        }

        public void Log(string message, Exception exception = null, LogMessageCategory category = null, LogMessageSeverity severity = null, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            Trace.TraceInformation(message);
        }

        public void RemoveLogDestination(ILogDestination logDestination)
        {
            //ignore
        }

        public void Start()
        {
            //ignore
        }

        public void Stop()
        {
            //ignore
        }

        public void Flush()
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void RemoveLogDestination(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
