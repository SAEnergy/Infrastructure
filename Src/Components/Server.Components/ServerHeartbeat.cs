using Core.Interfaces.Components;
using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Logging;
using Core.Models;
using System;
using System.Threading;

namespace HostService
{
    [ComponentRegistration(typeof(IServerHeartbeat))]
    [ComponentMetadata(AllowedActions =ComponentUserActions.All, Description = "Server heartbeat component.", FriendlyName = "Server Heartbeat Component")]
    public class ServerHeartbeat : Singleton<IServerHeartbeat>, IServerHeartbeat
    {
        #region Fields

        private readonly ILogger _logger;
        private TimeSpan _heartBeatSpeed = TimeSpan.FromMinutes(5);
        private ManualResetEvent _shutDown = new ManualResetEvent(false);
        private Thread _heartBeatThread;

        #region Properties

        public bool IsRunning { get; private set; }

        #endregion

        #endregion

        #region Constructor

        private ServerHeartbeat(ILogger logger)
        {
            _logger = logger;

            _logger.Log("Heartbeat created.");
        }

        #endregion

        #region Public Methods

        public static IServerHeartbeat CreateInstance(ILogger logger)
        {
            return Instance = new ServerHeartbeat(logger);
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _logger.Log("Server heartbeat starting.");

                _shutDown.Reset();

                _heartBeatThread = new Thread(new ThreadStart(heartBeatWorker));
                _heartBeatThread.IsBackground = true;
                _heartBeatThread.Start();
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                _logger.Log("Server heartbeat stopping.");

                IsRunning = false;
                _shutDown.Set();

                _heartBeatThread.Join();
                _heartBeatThread = null;
            }
        }

        #endregion

        #region Private Methods

        private void heartBeatWorker()
        {
            IsRunning = true;

            _logger.Log("Server heartbeat running.");

            while (IsRunning)
            {
                if (!_shutDown.WaitOne(_heartBeatSpeed))
                {
                    _logger.Log(string.Format("Heartbeat - {0}", DateTime.UtcNow.ToLocalTime()));
                }
            }

            _logger.Log("Server heartbeat stopped.");
        }

        #endregion
    }
}