﻿using Core.Interfaces.Base;
using Core.Interfaces.Logging;
using Core.IoC.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HostService
{
    public class Heartbeat : IRunnable
    {
        #region Fields

        private readonly ILogger _logger;
        private TimeSpan _heartBeatSpeed = TimeSpan.FromMinutes(5);
        private ManualResetEvent _shutDown = new ManualResetEvent(false);
        private Thread _heartBeatThread;

        #region Properties

        public bool IsRunning { get; set; }

        #endregion

        #endregion

        #region Constructor

        public Heartbeat()
        {
            _logger = IoCContainer.Instance.Resolve<ILogger>();

            _logger.Log(LogMessageSeverity.Information, "Heartbeat created.");
            _heartBeatThread = new Thread(new ThreadStart(heartBeatWorker));

            Start();
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (!IsRunning)
            {
                _heartBeatThread = new Thread(new ThreadStart(heartBeatWorker));
                _heartBeatThread.IsBackground = true;
                _heartBeatThread.Start();
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                _logger.Log(LogMessageSeverity.Information, "Server heartbeat stopping.");

                IsRunning = false;
                _shutDown.Set();

                _heartBeatThread.Join();
            }
        }

        #endregion

        #region Private Methods

        private void heartBeatWorker()
        {
            IsRunning = true;

            _logger.Log(LogMessageSeverity.Information, "Server heartbeat running.");

            while (IsRunning)
            {
                if (!_shutDown.WaitOne(_heartBeatSpeed))
                {
                    _logger.Log(LogMessageSeverity.Information, string.Format("Heartbeat - {0}", DateTime.Now));
                }
            }

            _logger.Log(LogMessageSeverity.Information, "Server heartbeat stopped.");
        }

        #endregion
    }
}