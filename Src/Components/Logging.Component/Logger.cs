using Core.Interfaces.Components.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Linq;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Base;
using Core.Models;

namespace Core.Logging
{
    [ComponentRegistration(typeof(ILogger))]
    [ComponentMetadata(Description = "Logging system.", FriendlyName = "Logger Component")]
    public sealed class Logger : Singleton<ILogger>, ILogger
    {
        #region Fields

        private Thread _logWorker;
        private List<ILogDestination> _destinations = new List<ILogDestination>();
        private Queue<LogMessage> _messageQueue = new Queue<LogMessage>();
        private Queue<LogMessage> _tempQueue = new Queue<LogMessage>();
        private ManualResetEvent _messagesReady = new ManualResetEvent(false);
        private ManualResetEvent _queueEmpty = new ManualResetEvent(false);

        private TimeSpan _queueTimeOut = TimeSpan.FromMilliseconds(100);
        private const int _defaultMaxQueueSize = 5000;
        private readonly string _processName;
        private readonly string _machineName;
        private readonly int _processId;
        private const int _blockSize = 64;

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

        public int MaxQueueSize { get; set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        #endregion

        #region Constructor

        private Logger()
        {
            MaxQueueSize = _defaultMaxQueueSize;

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
            if (logDestination != null)
            {
                if (IsRunning)
                {
                    logDestination.Start();
                }

                lock (_destinations)
                {
                    _destinations.Add(logDestination);
                }
            }
            else
            {
                Log("Cannot add Null LogDestination...", LogMessageSeverity.Error);
            }
        }

        public void RemoveLogDestination(ILogDestination logDestination)
        {
            if(logDestination != null)
            {
                if (logDestination.IsRunning)
                {
                    logDestination.Stop();
                }

                lock(_destinations)
                {
                    var index = _destinations.FindIndex(d => d.Id == logDestination.Id);

                    if (index > -1)
                    {
                        _destinations.RemoveAt(index);
                    }
                    else
                    {
                        Log(string.Format("Unable to remove LogDestination, cannot find destination with an id of \"{0}\".", logDestination.Id), LogMessageSeverity.Warning);
                    }
                }
            }
            else
            {
                Log("Cannot remove Null LogDestination...", LogMessageSeverity.Error);
            }
        }

        public void RemoveLogDestination(Guid id)
        {
            var destination = _destinations.Where(d => d.Id == id).FirstOrDefault();

            if(destination != null)
            {
                RemoveLogDestination(destination);
            }
            else
            {
                Log(string.Format("Unable to remove LogDestination, cannot find destination with an id of \"{0}\".", id), LogMessageSeverity.Warning);
            }
        }

        //pass through
        public void Log(LogMessage logMessage)
        {
            if (logMessage != null)
            {
                while (true)
                {
                    lock (_messageQueue)
                    {
                        Queue<LogMessage> queue = IsPaused ? _tempQueue : _messageQueue;
                        if (queue.Count < MaxQueueSize)
                        {
                            queue.Enqueue(logMessage);
                            _messagesReady.Set();
                            return;
                        }
                    }
                    // wait for queue to have room and keep trying
                    Thread.Sleep(_queueTimeOut);
                }
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
            Log(CreateMessage(message, category, severity, callerName, callerFilePath, callerLineNumber));
        }

        private LogMessage CreateMessage(string message, LogMessageCategory category, LogMessageSeverity severity, [CallerMemberName] string callerName = "",
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

        public void Start()
        {
            lock (_syncObject)
            {
                if (!IsRunning)
                {
                    Log("Logger starting.");

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
            lock (_syncObject)
            {
                if (IsRunning)
                {
                    Log("Logger stopping.");

                    Flush();
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

        public void Flush()
        {
            Pause();

            while (true)
            {
                _queueEmpty.WaitOne(_queueTimeOut);
                lock (_messageQueue)
                {
                    if (_messageQueue.Count == 0) { break; }
                }
            }
            var dests = LogDestinations;

            foreach (ILogDestination dest in dests)
            {
                dest.Flush();
            }

            Resume();
        }

        #endregion

        #region Private Methods

        private void LogWorker()
        {
            IsRunning = true;

            while (IsRunning)
            {
                _messagesReady.WaitOne(_queueTimeOut);
                _messagesReady.Reset();

                List<LogMessage> messages = null;

                lock (_messageQueue)
                {
                    messages = new List<LogMessage>();

                    if (_messageQueue.Count > 0)
                    {
                        for (int i = 0; i < _blockSize; i++)
                        {
                            if (_messageQueue.Count > 0)
                            {
                                messages.Add(_messageQueue.Dequeue());
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if(_messageQueue.Count == 0)
                    {
                        _queueEmpty.Set();
                    }
                }

                if (messages.Count > 0)
                {
                    IEnumerable<ILogDestination> dests = LogDestinations;

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

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
            lock (_messageQueue)
            {
                foreach (LogMessage message in _tempQueue)
                {
                    _messageQueue.Enqueue(message);
                }
            }
        }

        #endregion
    }
}
