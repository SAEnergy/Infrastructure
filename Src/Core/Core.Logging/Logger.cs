﻿using Core.Interfaces.Base;
using Core.Interfaces.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Core.Logging
{
    public sealed class Logger : Singleton<ILogger>, ILogger
    {
        #region Fields

        private List<ILogDestination> _destinations;
        private Thread _logWorker;
        private LogMessageQueue _loggerQueue;

        private readonly string _processName;
        private readonly string _machineName;
        private readonly int _processId;

        private static object _syncObject = new object();

        #endregion

        #region Properties

        public IReadOnlyList<ILogDestination> LogDestinations
        {
            get
            {
                lock (_destinations)
                {
                    return _destinations.AsReadOnly();
                }
            }
        }

        public bool IsRunning { get; private set; }

        #endregion

        #region Constructor

        private Logger()
        {
            _destinations = new List<ILogDestination>();
            _loggerQueue = new LogMessageQueue() { IsBlocking = true };

            _machineName = Environment.MachineName;

            var process = Process.GetCurrentProcess();

            _processName = process.ProcessName;
            _processId = process.Id;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        #endregion

        #region Public Methods

        public static ILogger CreateInstance()
        {
            return Instance = new Logger();
        }

        public void AddLogDestination(ILogDestination logDestination)
        {
            if(IsRunning)
            {
                logDestination.Start();
            }

            lock(_destinations)
            {
                Log(string.Format("LogDestination of type \"{0}\" added.", logDestination.GetType().Name));

                _destinations.Add(logDestination);
            }
        }

        //pass through
        public void Log(LogMessage logMessage)
        {
            if (logMessage != null)
            {
                _loggerQueue.EnqueueMessage(logMessage);
            }
        }

        public void Log(string message, [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            Log(message, LogMessageCategory.General, LogMessageSeverity.Information, callerName, callerFilePath, callerLineNumber);
        }

        public void Log(string message, LogMessageSeverity severity, [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            Log(message, LogMessageCategory.General, severity, callerName, callerFilePath, callerLineNumber);
        }

        public void Log(string message, LogMessageCategory category, [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            Log(message, category, LogMessageSeverity.Information, callerName, callerFilePath, callerLineNumber);
        }

        public void Log(string message, LogMessageCategory category, LogMessageSeverity severity, [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
        {


            _loggerQueue.EnqueueMessage(CreateMessage(message, category, severity, callerName, callerFilePath, callerLineNumber));
        }

        protected LogMessage CreateMessage(string message, LogMessageCategory category, LogMessageSeverity severity, [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            var logMessage = new LogMessage();

            logMessage.Category = category;
            logMessage.Severity = severity;
            logMessage.Message = message;
            logMessage.CallerName = callerName;
            logMessage.FilePath = callerFilePath;
            logMessage.LineNumber = callerLineNumber;
            logMessage.MachineName = _machineName;
            logMessage.ProcessId = _processId;
            logMessage.ProcessName = _processName;

            return logMessage;
        }

        public void RemoveLogDestination(ILogDestination logDestination)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            lock (_syncObject)
            {
                if (!IsRunning)
                {
                    Log(string.Format("Logger starting.", this.GetType().Name));

                    lock (_destinations)
                    {
                        foreach (var destination in _destinations)
                        {
                            destination.Start();
                        }
                    }

                    _logWorker = new Thread(new ThreadStart(LogWorker));

                    _logWorker.Start();
                }
            }
        }

        public void Stop()
        {
            lock(_syncObject)
            {
                if (IsRunning)
                {
                    Log(string.Format("Logger stopping.", this.GetType().Name));

                    IsRunning = false;

                    _logWorker.Join();

                    lock (_destinations)
                    {
                        foreach (var destination in _destinations)
                        {
                            destination.Stop();
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void LogWorker()
        {
            IsRunning = true;

            while (IsRunning || !_loggerQueue.IsQueueEmpty)
            {
                //will block for max of the timespan timeout, or return a list the size of the batch size constant
                var messages = _loggerQueue.DequeueMessages();

                if (messages.Count > 0)
                {
                    List<ILogDestination> dests = new List<ILogDestination>();
                    lock (_destinations)
                    {
                        dests.AddRange(_destinations);
                    }
                    foreach (var destination in dests)
                    {
                        destination.ProcessMessages(messages);
                    }
                }
            }
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (IsRunning)
            {
                Log("Process exiting, shutting down logging system...", LogMessageSeverity.Warning);
                Stop();
            }
        }

        public void HandleLoggingException(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            LogMessage lm = CreateMessage(message, LogMessageCategory.General, LogMessageSeverity.Error, callerName, callerFilePath, callerLineNumber);
            List<ILogDestination> dests = new List<ILogDestination>();
            lock (_destinations)
            {
                dests.AddRange(_destinations);
            }
            foreach (var destination in dests)
            {
                destination.HandleLoggingException(lm);
            }
        }

        #endregion
    }
}
