using Core.Interfaces.Components;
using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Logging;
using Core.Models;
using Core.Models.Persistent;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Server.Components
{
    [ComponentRegistration(typeof(IPermissionsManager))]
    [ComponentMetadata(Description = "Controls permissions throught the system.", FriendlyName = "Permissions Manager")]
    public sealed class PermissionsManager : Singleton<IPermissionsManager>, IPermissionsManager
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IDataComponent _dataComponent;
        private readonly ISystemConfiguration _systemConfig;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }

        #endregion

        #region Constructor

        private PermissionsManager(ILogger logger, IDataComponent dataComponent, ISystemConfiguration systemConfig)
        {
            _logger = logger;
            _dataComponent = dataComponent;
            _systemConfig = systemConfig;
        }

        #endregion

        #region Public Methods

        public static IPermissionsManager CreateInstance(ILogger logger, IDataComponent dataComponent, ISystemConfiguration systemConfig)
        {
            return Instance = new PermissionsManager(logger, dataComponent, systemConfig);
        }

        public bool IsAuthorized(IIdentity identity, Type serviceType, MethodInfo operationMethodInfo)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }
            if (operationMethodInfo == null)
            {
                throw new ArgumentNullException("operationMethodInfo");
            }

            return true;
        }

        public void ClearPermissionsCache()
        {
        }

        public void Initialize()
        {
            if (IsInitialized != true)
            {
                IsInitialized = true;

                _logger.Log("Initializing Permissions Manager...");

                GetLocalGroups();
            }
        }

        #endregion

        #region Private Methods

        private string[] GetRoles(IIdentity identity)
        {
            

            return null;
        }

        private void GetLocalGroups()
        {
            try {
                using (DirectoryEntry machine = new DirectoryEntry(string.Concat("WinNT://", Environment.MachineName)))
                {
                    foreach(var obj in machine.Children)
                    {
                        var dir = obj as DirectoryEntry;

                        if(dir != null)
                        {
                            _logger.Log("Name = " + dir.Name +" Class = " + dir.SchemaClassName);
                        }

                        dir.Dispose();
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Log(ex.Message, severity: LogMessageSeverity.Error);
            }
        }

        #endregion
    }
}
