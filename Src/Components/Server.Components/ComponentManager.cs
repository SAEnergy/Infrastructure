using Core.Interfaces.Base;
using Core.Interfaces.Components;
using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models.DataContracts;
using Core.Util;
using System.Reflection;
using Core.Models;
using Core.Proxies;

namespace Server.Components
{
    [ComponentRegistration(typeof(IComponentManager))]
    [ComponentMetadata(Description = "Controller for all components.", FriendlyName = "Component Manager")]
    [ProxyDecorator(typeof(MethodTimerProxy<>))]

    public class ComponentManager : Singleton<IComponentManager>, IComponentManager
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IIoCContainer _container;
        private readonly ISystemConfiguration _config;

        private Dictionary<Type, ComponentMetadata> _metadataCache;

        #endregion

        #region Properties

        public bool HasInitializedComponents { get; private set; }

        #endregion

        #region Constructor

        private ComponentManager(ILogger logger, IIoCContainer container, ISystemConfiguration config)
        {
            _logger = logger;
            _container = container;
            _config = config;

            _metadataCache = new Dictionary<Type, ComponentMetadata>();
        }

        #endregion

        #region Public Methods

        public static IComponentManager CreateInstance(ILogger logger, IIoCContainer container, ISystemConfiguration config)
        {
            return Instance = new ComponentManager(logger, container, config);
        }

        public ComponentMetadata[] GetComponents()
        {
            var infos = new List<ComponentMetadata>();

            foreach (var type in _container.GetRegisteredTypes())
            {
                infos.Add(GetMetadata(type));
            }

            return infos.ToArray();
        }

        public void StartAll()
        {
            _logger.Log("Starting all runnable components");

            if(!HasInitializedComponents)
            {
                _logger.Log("Component Manager has not yet initialized components.");

                InitializeComponents();
            }

            foreach (var type in GetRunnableRegisteredTypes())
            {
                _logger.Log(string.Format("Starting component of type {0}", type.Key.Name));

                StartRunnable(GetIRunnable(type.Key));
            }
        }

        public void StopAll()
        {
            _logger.Log("Stopping all runnable components");

            foreach (var type in GetRunnableRegisteredTypes())
            {
                _logger.Log(string.Format("Stopping component of type {0}", type.Key.Name));

                StopRunnable(GetIRunnable(type.Key));
            }
        }

        public void InitializeComponents()
        {
            if (!HasInitializedComponents)
            {
                HasInitializedComponents = true;

                _logger.Log("Initializing all components...");

                var componentTypes = GetInitalizableRegisteredTypes();

                foreach (var componentType in componentTypes)
                {
                    var component = GetIInitalizable(componentType.Key);

                    if (component != null)
                    {
                        if (!component.IsInitialized)
                        {
                            _logger.Log(string.Format("Initializing component of type {0}", componentType.Key.Name));

                            component.Initialize();
                        }
                        else
                        {
                            _logger.Log(string.Format("Component of type {0} has already been initialized", componentType.Key.Name), severity: LogMessageSeverity.Warning);
                        }
                    }
                }
            }
        }

        public ComponentMetadata StartComponent(int componentId)
        {
            ComponentMetadata rc = null;

            _logger.Log(string.Format("Attempting to start component with id \"{0}\".", componentId));

            var type = GetComponentType(componentId);

            if (type.Key != null)
            {
                rc = type.Value;

                if (type.Value.UserActions.HasFlag(ComponentUserActions.Start))
                {
                    StartRunnable(GetIRunnable(type.Key));
                }
                else
                {
                    _logger.Log(string.Format("Cannot start component \"{0}\".  This component is not startable", type.Value.FriendlyName), severity: LogMessageSeverity.Warning);
                }
            }

            return rc;
        }

        public ComponentMetadata StopComponent(int componentId)
        {
            ComponentMetadata rc = null;

            _logger.Log(string.Format("Attempting to stop component with id \"{0}\".", componentId));

            var type = GetComponentType(componentId);

            if (type.Key != null)
            {
                rc = type.Value;

                if (type.Value.UserActions.HasFlag(ComponentUserActions.Stop))
                {
                    StopRunnable(GetIRunnable(type.Key));
                }
                else
                {
                    _logger.Log(string.Format("Cannot stop component \"{0}\".  This component is not stopable", type.Value.FriendlyName), severity: LogMessageSeverity.Warning);
                }
            }

            return rc;
        }

        public ComponentMetadata RestartComponent(int componentId)
        {
            ComponentMetadata rc = null;

            _logger.Log(string.Format("Attempting to restart component with id \"{0}\".", componentId));

            var type = GetComponentType(componentId);

            if (type.Key != null)
            {
                rc = type.Value;

                if (type.Value.UserActions.HasFlag(ComponentUserActions.Restart))
                {
                    StopRunnable(GetIRunnable(type.Key));
                    StartRunnable(GetIRunnable(type.Key));
                }
                else
                {
                    _logger.Log(string.Format("Cannot restart component \"{0}\".  This component is not restartable", type.Value.FriendlyName), severity: LogMessageSeverity.Warning);
                }
            }

            return rc;
        }

        public ComponentMetadata DisableComponent(int componentId)
        {
            ComponentMetadata rc = null;

            _logger.Log(string.Format("Attempting to disable component with id \"{0}\".", componentId));

            var type = GetComponentType(componentId);

            if (type.Key != null)
            {
                rc = type.Value;

                if (type.Value.UserActions.HasFlag(ComponentUserActions.Disable))
                {
                    if (!type.Value.IsDisabled)
                    {
                        StopRunnable(GetIRunnable(type.Key));
                        type.Value.IsDisabled = true;

                        _logger.Log(string.Format("Component with id \"{0}\" has been disabled.", type.Value.FriendlyName));
                    }
                    else
                    {
                        _logger.Log(string.Format("Component with id \"{0}\" has already been disabled.", type.Value.FriendlyName), severity: LogMessageSeverity.Warning);
                    }
                }
                else
                {
                    _logger.Log(string.Format("Cannot disable component \"{0}\".", type.Value.FriendlyName), severity: LogMessageSeverity.Warning);
                }
            }

            return rc;
        }

        #endregion

        #region Private Methods

        private KeyValuePair<Type, ComponentMetadata> GetComponentType(int componentId)
        {
            KeyValuePair<Type, ComponentMetadata> kvp = new KeyValuePair<Type, ComponentMetadata>();

            var query = _metadataCache.Where(k => k.Value.ComponentId == componentId);

            if (query.Any())
            {
                kvp = query.First();

                _logger.Log(string.Format("Resolved component with id \"{0}\" to \"{1}\".", componentId, kvp.Value.FriendlyName));
            }

            return kvp;
        }

        private void StartRunnable(IRunnable runnable)
        {
            if (runnable != null)
            {
                if (!runnable.IsRunning)
                {
                    var metadata = GetMetadata(runnable.GetType());

                    if (metadata != null)
                    {
                        if (!metadata.IsDisabled)
                        {
                            runnable.Start();

                            SetStatus(metadata, ComponentStatus.Running);
                        }
                        else
                        {
                            _logger.Log(string.Format("Cannot start component \"{0}\" because it is disabled.", metadata.FriendlyName), severity: LogMessageSeverity.Warning);
                        }
                    }
                }
                else
                {
                    _logger.Log(string.Format("Cannot start component \"{0}\" because it is already running.", runnable.GetType().Name), severity: LogMessageSeverity.Warning);
                }
            }
        }

        private void StopRunnable(IRunnable runnable)
        {
            if (runnable != null)
            {
                if (runnable.IsRunning)
                {
                    runnable.Stop();

                    SetStatus(runnable.GetType(), ComponentStatus.Stopped);
                }
                else
                {
                    _logger.Log(string.Format("Cannot stop component \"{0}\" because it is not running.", runnable.GetType().Name), severity: LogMessageSeverity.Warning);
                }
            }
        }

        private void SetStatus(Type type, ComponentStatus status)
        {
            if (type != null)
            {
                SetStatus(GetMetadata(type), status);
            }
            else
            {
                _logger.Log(string.Format("Unable to set status of component \"{0}\"", type.Name), severity: LogMessageSeverity.Error);
            }
        }

        private void SetStatus(ComponentMetadata info, ComponentStatus status)
        {
            if(info != null)
            {
                info.Status = status;

                _logger.Log(string.Format("Set component \"{0}\" status to \"{1}\".", info.FriendlyName, info.Status));
            }
            else
            {
                _logger.Log("Unable to set status, Null argument provided.", severity: LogMessageSeverity.Error);
            }
        }

        private IRunnable GetIRunnable(Type type)
        {
            IRunnable runnable = _container.Resolve(type) as IRunnable;

            if(runnable == null)
            {
                _logger.Log(string.Format("Unable to cast type \"{0}\" as IRunnable.  ComponentMetadata AllowedActions is misconfigred.", type.Name), severity: LogMessageSeverity.Error);
            }

            return runnable;
        }

        private IInitializable GetIInitalizable(Type type)
        {
            IInitializable initializable = _container.Resolve(type) as IInitializable;

            if (initializable == null)
            {
                _logger.Log(string.Format("Unable to cast type \"{0}\" as IInitializable.", type.Name), severity: LogMessageSeverity.Error);
            }

            return initializable;
        }

        private List<KeyValuePair<Type, Type>> GetRunnableRegisteredTypes()
        {
            return _container.GetRegisteredTypes().Where(i => typeof(IRunnable).IsAssignableFrom(i.Key) && i.Key != typeof(ILogger) && typeof(IComponent).IsAssignableFrom(i.Key)).ToList();
        }

        private List<KeyValuePair<Type, Type>> GetInitalizableRegisteredTypes()
        {
            return _container.GetRegisteredTypes().Where(i => typeof(IInitializable).IsAssignableFrom(i.Key) && i.Key != typeof(IComponentManager) && typeof(IComponent).IsAssignableFrom(i.Key)).ToList();
        }

        private List<ComponentMetadata> GetDependencies(Type type)
        {
            var dependencies = new List<ComponentMetadata>();

            if (type != null)
            {
                var constructor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();

                if (constructor != null)
                {
                    var parameterInfoes = constructor.GetParameters().ToList();

                    if (parameterInfoes.Count > 0)
                    {
                        foreach (var parameter in parameterInfoes)
                        {
                            if (parameter.ParameterType.IsInterface)
                            {
                                var query = _container.GetRegisteredTypes().Where(p => p.Key == parameter.ParameterType);

                                if (query.Any())
                                {
                                    var kvp = query.First();

                                    dependencies.Add(GetMetadata(kvp));
                                }
                            }
                        }
                    }
                }
            }

            return dependencies;
        }

        private ComponentMetadata GetMetadata(Type type)
        {
            ComponentMetadata info = null;

            if (type != null)
            {
                IEnumerable<KeyValuePair<Type, Type>> query;

                if (type.IsInterface)
                {
                    query = _container.GetRegisteredTypes().Where(k => k.Key == type);
                }
                else
                {
                    query = _container.GetRegisteredTypes().Where(k => k.Value == type);
                }

                if (query.Any())
                {
                    info = GetMetadata(query.First());
                }
            }

            return info;
        }

        private ComponentMetadata GetMetadata(KeyValuePair<Type, Type> type)
        {
            ComponentMetadata info;

            if (!_metadataCache.TryGetValue(type.Key, out info))
            {
                info = new ComponentMetadata();
                info.InterfaceTypeName = type.Key.Name;
                info.ConcreteTypeName = type.Value.Name;

                var metaAtty = type.Value.GetAttribute<ComponentMetadataAttribute>();

                if (metaAtty != null)
                {
                    info.Description = metaAtty.Description;
                    info.FriendlyName = metaAtty.FriendlyName;
                    info.UserActions = metaAtty.AllowedActions;
                }

                var regAtty = type.Value.GetAttribute<ComponentRegistrationAttribute>();

                //TODO: use configuration system to get the real data
                //info.IsDisabled = _config.GetConfig<bool>(GetType().Name,string.Format(type.Value.Name));
                info.IsDisabled = false;

                info.Dependencies = GetDependencies(type.Value).ToArray();

                info.ComponentId = info.GetHashCode();

                _metadataCache.Add(type.Key, info);
            }

            return info;
        }

        #endregion
    }
}
