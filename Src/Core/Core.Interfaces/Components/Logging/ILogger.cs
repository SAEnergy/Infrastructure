using Core.Interfaces.Base;
using Core.Interfaces.Components.Base;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Core.Interfaces.Components.Logging
{
    public interface ILogger : IRunnable, IComponent
    {
        IReadOnlyList<ILogDestination> LogDestinations { get; }

        void Log(LogMessage logMessage);

        void Log(string message, Exception exception = null, LogMessageCategory category = null, LogMessageSeverity severity = null, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1);

        void HandleLoggingException(string message, Exception exception = null, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1);

        void AddLogDestination(ILogDestination logDestination);

        void RemoveLogDestination(ILogDestination logDestination);

        void RemoveLogDestination(Guid id);

        void Flush();

        void Pause();

        void Resume();
    }
}
