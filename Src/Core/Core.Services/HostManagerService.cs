﻿using Core.Interfaces.Base;
using Core.Interfaces.Logging;
using Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public class HostManagerService : IHostManagerService
    {
        #region Fields

        private const string _dllSearchPattern = "*.Hosts.dll";
        private readonly ILogger _logger;

        private Dictionary<Type, IHost> _hosts;

        #endregion

        #region Constructor

        public HostManagerService(ILogger logger)
        {
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public void RestartAll()
        {
            _logger.Log(LogMessageSeverity.Information, "HostManager restarting all hosts...");

            StopAll();
            StartAll();
        }

        public void StartAll()
        {
            _logger.Log(LogMessageSeverity.Information, "HostManager starting all hosts...");

            _hosts = FindAllHosts();

            foreach(var host in _hosts.Values)
            {
                _logger.Log(LogMessageSeverity.Information, string.Format("HostManager starting host of type \"{0}\".", host.GetType().Name));

                host.Start();
            }
        }

        public void StopAll()
        {
            _logger.Log(LogMessageSeverity.Information, "HostManager stopping all hosts...");

            foreach (var host in _hosts.Values)
            {
                _logger.Log(LogMessageSeverity.Information, string.Format("HostManager stopping host of type \"{0}\".", host.GetType().Name));

                host.Stop();
            }
        }

        void IHostManagerService.Restart<T>()
        {
            throw new NotImplementedException();
        }

        void IHostManagerService.Start<T>()
        {
            throw new NotImplementedException();
        }

        void IHostManagerService.Stop<T>()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        private Dictionary<Type, IHost> FindAllHosts()
        {
            var hosts = new Dictionary<Type, IHost>();

            var files = Directory.GetFiles(Environment.CurrentDirectory, _dllSearchPattern, SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                var assm = Assembly.LoadFile(file);

                var types = assm.GetTypes().Where(t => typeof(IHost).IsAssignableFrom(t));

                foreach (Type type in types)
                {
                    _logger.Log(LogMessageSeverity.Information, string.Format("HostManager creating host of type \"{0}\".", type));

                    IHost host = Activator.CreateInstance(type) as IHost;

                    hosts.Add(host.InterfaceType, host);
                }
            }

            return hosts;
        }

        #endregion
    }
}
