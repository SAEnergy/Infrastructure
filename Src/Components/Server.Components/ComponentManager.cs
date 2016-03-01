﻿using Core.Interfaces.Base;
using Core.Interfaces.Components;
using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models.DataContracts;
using Core.Util;
using System.Reflection;
using Core.Models;

namespace Server.Components
{
    [ComponentRegistration(ComponentType.Server, typeof(IComponentManager))]
    [ComponentMetadata(AllowedActions = ComponentUserActions.Restart, Description = "Controller for all components.", FriendlyName = "Component Manager")]

    public class ComponentManager : Singleton<IComponentManager>, IComponentManager
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IIoCContainer _container;
        private readonly ISystemConfiguration _config;

        private Dictionary<Type, ComponentMetadata> _metadataCache;

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

        public void StartComponent(int componentId)
        {
            _logger.Log(string.Format("Attempting to start component with id \"{0}\".", componentId));

            var type = GetComponentType(componentId);

            if (type.Key != null)
            {
                if (type.Value.UserActions.HasFlag(ComponentUserActions.Start))
                {
                    StartRunnable(GetIRunnable(type.Key));
                }
                else
                {
                    _logger.Log(string.Format("Cannot start component \"{0}\".  This component is not startable", type.Value.FriendlyName), LogMessageSeverity.Warning);
                }
            }
        }

        public void StopComponent(int componentId)
        {
            _logger.Log(string.Format("Attempting to stop component with id \"{0}\".", componentId));

            var type = GetComponentType(componentId);

            if (type.Key != null)
            {
                if (type.Value.UserActions.HasFlag(ComponentUserActions.Stop))
                {
                    StopRunnable(GetIRunnable(type.Key));
                }
                else
                {
                    _logger.Log(string.Format("Cannot stop component \"{0}\".  This component is not stopable", type.Value.FriendlyName), LogMessageSeverity.Warning);
                }
            }
        }

        public void RestartComponent(int componentId)
        {
            _logger.Log(string.Format("Attempting to restart component with id \"{0}\".", componentId));

            var type = GetComponentType(componentId);

            if (type.Key != null)
            {
                if (type.Value.UserActions.HasFlag(ComponentUserActions.Restart))
                {
                    StopRunnable(GetIRunnable(type.Key));
                    StartRunnable(GetIRunnable(type.Key));
                }
                else
                {
                    _logger.Log(string.Format("Cannot restart component \"{0}\".  This component is not restartable", type.Value.FriendlyName), LogMessageSeverity.Warning);
                }
            }
        }

        public void DisableComponent(int componentId)
        {
            _logger.Log(string.Format("Attempting to disable component with id \"{0}\".", componentId));

            var type = GetComponentType(componentId);

            if (type.Key != null)
            {
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
                        _logger.Log(string.Format("Component with id \"{0}\" has already been disabled.", type.Value.FriendlyName), LogMessageSeverity.Warning);
                    }
                }
                else
                {
                    _logger.Log(string.Format("Cannot disable component \"{0}\".", type.Value.FriendlyName), LogMessageSeverity.Warning);
                }
            }
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
                            _logger.Log(string.Format("Cannot start component \"{0}\" because it is disabled.", metadata.FriendlyName), LogMessageSeverity.Warning);
                        }
                    }
                }
                else
                {
                    _logger.Log(string.Format("Cannot start component \"{0}\" because it is already running.", runnable.GetType().Name), LogMessageSeverity.Warning);
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
                    _logger.Log(string.Format("Cannot stop component \"{0}\" because it is not running.", runnable.GetType().Name), LogMessageSeverity.Warning);
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
                _logger.Log(string.Format("Unable to set status of component \"{0}\"", type.Name), LogMessageSeverity.Error);
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
                _logger.Log("Unable to set status, Null argument provided.", LogMessageSeverity.Error);
            }
        }

        private IRunnable GetIRunnable(Type type)
        {
            return _container.Resolve(type) as IRunnable;
        }

        private List<KeyValuePair<Type, Type>> GetRunnableRegisteredTypes()
        {
            return _container.GetRegisteredTypes().Where(i => typeof(IRunnable).IsAssignableFrom(i.Key) && i.Key != typeof(ILogger)).ToList();
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

                if (regAtty != null)
                {
                    info.Type = regAtty.Type;
                }

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
