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

namespace Server.Components
{
    [ComponentRegistration(ComponentType.Server, typeof(IComponentManager))]
    [ComponentMetadata(AllowedActions = ComponentUserActions.Restart, Description = "Controller for all components.", FriendlyName = "Component Manager")]

    public class ComponentManager : Singleton<IComponentManager>, IComponentManager
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IIoCContainer _container;

        private Dictionary<Type, ComponentMetadata> _metadataCache;

        #endregion

        #region Constructor

        private ComponentManager(ILogger logger, IIoCContainer container)
        {
            _logger = logger;
            _container = container;

            _metadataCache = new Dictionary<Type, ComponentMetadata>();
        }

        #endregion

        #region Public Methods

        public static IComponentManager CreateInstance(ILogger logger, IIoCContainer container)
        {
            return Instance = new ComponentManager(logger, container);
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
                _logger.Log(string.Format("Starting component of type {0}", type.Name));

                var runnable = _container.Resolve(type) as IRunnable;

                if (runnable != null)
                {
                    runnable.Start();
                }
                else
                {
                    _logger.Log(string.Format("Failed to get runnable object from type {0}.", type.Name), LogMessageSeverity.Error);
                }
            }
        }

        public void StartComponent(ComponentMetadata info)
        {
            throw new NotImplementedException();
        }

        public void StopAll()
        {
            _logger.Log("Stopping all runnable components");

            foreach (var type in GetRunnableRegisteredTypes())
            {
                _logger.Log(string.Format("Stopping component of type {0}", type.Name));

                var runnable = _container.Resolve(type) as IRunnable;

                if (runnable != null)
                {
                    runnable.Stop();
                }
                else
                {
                    _logger.Log(string.Format("Failed to get runnable object from type {0}.", type.Name), LogMessageSeverity.Error);
                }
            }
        }

        public void StopComponent(ComponentMetadata info)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        private List<Type> GetRunnableRegisteredTypes()
        {
            return _container.GetRegisteredTypes().Select(k => k.Key).Where(i => typeof(IRunnable).IsAssignableFrom(i) && i != typeof(ILogger)).ToList();
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

        private ComponentMetadata GetMetadata(KeyValuePair<Type, Type> type)
        {
            ComponentMetadata info;

            if (!_metadataCache.TryGetValue(type.Key, out info))
            {
                info = new ComponentMetadata();
                info.InterfaceTypeName = type.Key.Name;
                info.ConcreteTypeName = type.Value.Name;

                var atty = type.Value.GetAttribute<ComponentMetadataAttribute>();

                if (atty != null)
                {
                    info.Description = atty.Description;
                    info.FriendlyName = atty.FriendlyName;
                }

                info.Dependencies = GetDependencies(type.Value).ToArray();

                info.ComponentId = info.GetHashCode();

                _metadataCache.Add(type.Key, info);
            }

            return info;
        }

        #endregion
    }
}
