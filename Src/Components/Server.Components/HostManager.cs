using Core.Comm;
using Core.Interfaces.Components;
using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Logging;
using Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Core.Models;
using Core.Interfaces;

namespace Server.Components
{
    public class ServiceHostInfo
    {
        public Type InterfaceType { get; set; }
        public ServiceHost Host { get; set; }
        public ILogger Logger { get; set; }
    }

    [ComponentRegistration(typeof(IHostManager))]
    [ComponentMetadata(AllowedActions = ComponentUserActions.Restart, Description = "Controller for all host endpoints.", FriendlyName = "Host Manager Component")]
    public sealed class HostManager : Singleton<IHostManager>, IHostManager
    {
        #region Fields

        private const string _dllSearchPattern = "*.dll";
        private readonly ILogger _logger;

        private Dictionary<Type, ServiceHostInfo> _infos = new Dictionary<Type, ServiceHostInfo>();

        #endregion

        #region Properties
        public bool IsRunning { get; private set; }

        #endregion

        #region Constructor

        private HostManager(ILogger logger)
        {
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public static IHostManager CreateInstance(ILogger logger)
        {
            return Instance = new HostManager(logger);
        }

        public void RestartAll()
        {
            _logger.Log("HostManager restarting all hosts...");

            Stop();
            Start();
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _logger.Log("HostManager starting all hosts...");

                IsRunning = true;

                lock (_infos)
                {
                    _infos.Clear();
                    var dic = FindAllHosts();
                    foreach (var key in dic.Keys) { _infos.Add(key, dic[key]); }

                    foreach (var type in _infos.Keys)
                    {
                        Start(type);
                    }
                }
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                _logger.Log("HostManager stopping all hosts...");
                IsRunning = false;

                lock (_infos)
                {
                    foreach (var type in _infos.Keys)
                    {
                        Stop(type);
                    }
                }
            }
        }

        public void Restart<T>()
        {
            Stop<T>();
            Start<T>();
        }

        private void Start(Type t)
        {
            ServiceHostInfo host;

            lock (_infos)
            {
                if (!_infos.TryGetValue(t, out host))
                {
                    _logger.Log(string.Format("HostManager cannot find host with interface type of \"{0}\".", t.Name), severity: LogMessageSeverity.Error);
                }
            }
            if (host != null)
            {
                _logger.Log(string.Format("HostManager starting host with interface type of \"{0}\".", t.Name));
                host.Host.Open();
                foreach (var endpoint in host.Host.Description.Endpoints)
                {
                    _logger.Log("Listening on " + endpoint.Address.Uri.ToString());
                }
            }
        }

        public void Stop(Type t)
        {
            ServiceHostInfo host;

            lock (_infos)
            {
                if (!_infos.TryGetValue(t, out host))
                {
                    _logger.Log(string.Format("HostManager cannot find host with interface type of \"{0}\".", t.Name), severity: LogMessageSeverity.Error);
                }
            }
            if (host != null)
            {
                _logger.Log(string.Format("HostManager stopping host with interface type of \"{0}\".", t.Name));

                host.Host.Abort();
            }
        }

        public void Start<T>()
        {
            Start(typeof(T));
        }

        public void Stop<T>()
        {
            Stop(typeof(T));
        }

        #endregion

        #region Private Methods

        private Dictionary<Type, ServiceHostInfo> FindAllHosts()
        {
            var hosts = new Dictionary<Type, ServiceHostInfo>();

            foreach (Type type in TypeLocator.FindTypes(_dllSearchPattern, typeof(IServiceHost<>)))
            {
                ServiceHostInfo info = new ServiceHostInfo();

                Type interfaceType = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IServiceHost<>));
                interfaceType = interfaceType.GetGenericArguments().First();

                info.InterfaceType = interfaceType;
                info.Logger = _logger;
                info.Host = new ServiceHost(type);

                info.Host.Description.Behaviors.Add(new HostErrorHandlerBehavior(info));
                info.Host.Authorization.ServiceAuthorizationManager = new RoleBasedAuthorizationManager();

                ContractDescription contract = ContractDescription.GetContract(interfaceType);

                EndpointAddress endpoint = EndpointInformation.BuildEndpoint(new EndpointInformation(), ServerConnectionInformation.Instance, interfaceType);
                Binding binding = BindingInformation.BuildBinding(new BindingInformation(), ServerConnectionInformation.Instance);
                ServiceEndpoint service = new ServiceEndpoint(contract, binding, endpoint);

                foreach (OperationDescription operation in service.Contract.Operations)
                {
                    operation.Behaviors.Find<DataContractSerializerOperationBehavior>().DataContractResolver = new DataContractTypeResolver();
                }

                info.Host.AddServiceEndpoint(service);

                hosts.Add(interfaceType, info);
            }

            return hosts;
        }

        #endregion
    }


    public class HostErrorHandler : IErrorHandler
    {
        private ServiceHostInfo _info;

        public HostErrorHandler(ServiceHostInfo info) { _info = info; }

        public bool HandleError(Exception error)
        {
            _info.Logger.Log(_info.InterfaceType.Name + " exception: " + error.Message, severity: LogMessageSeverity.Warning);
            //prevent host from closing
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            if (error is FaultException) { return; }

            //bool serviceDebug = OperationContext.Current.EndpointDispatcher.ChannelDispatcher.IncludeExceptionDetailInFaults;

            FaultException faultException = new FaultException("The server encountered an error of type " + error.GetType());
            MessageFault faultMessage = faultException.CreateMessageFault();
            fault = Message.CreateMessage(version, faultMessage, faultException.Action);
        }
    }

    public class HostErrorHandlerBehavior : IServiceBehavior
    {
        private ServiceHostInfo _info;

        public HostErrorHandlerBehavior(ServiceHostInfo info) { _info = info; }

        public void AddBindingParameters(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, System.ServiceModel.Channels.BindingParameterCollection bindingParameters) { }
        public void Validate(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase) { }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcherBase dispatcherBase in
                 serviceHostBase.ChannelDispatchers)
            {
                var channelDispatcher = dispatcherBase as ChannelDispatcher;
                if (channelDispatcher != null)
                    channelDispatcher.ErrorHandlers.Add(new HostErrorHandler(_info));
            }

        }
    }
}
