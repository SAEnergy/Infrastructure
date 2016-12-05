using Core.Interfaces.Components.Logging;
using Core.Interfaces.Components;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Base;
using Core.Models;
using System;

namespace Server.Components
{
    [ComponentRegistration(typeof(ISystemConfiguration))]
    [ComponentMetadata(Description = "Maintains system wide configurations.", FriendlyName = "System Configuration Component")]
    public class SystemConfiguration : Singleton<ISystemConfiguration>, ISystemConfiguration
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IDataComponent _dataComponent;

        #endregion

        #region Constructor

        private SystemConfiguration(ILogger logger, IDataComponent dataComponent)
        {
            _logger = logger;
            _dataComponent = dataComponent;
        }

        #endregion

        #region Public Methods

        public static ISystemConfiguration CreateInstance(ILogger logger, IDataComponent dataComponent)
        {
            return Instance = new SystemConfiguration(logger, dataComponent);
        }

        public T GetConfig<T>(string sectionName, string configName)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
