using Core.Interfaces.Components.Logging;
using Core.Models.Persistent;
using Core.Scheduler.Jobs;
using Core.Util;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Core.Scheduler
{
    public static class JobFactory
    {
        #region Fields

        private static Dictionary<string, Type> _jobActionTypeMap;

        #endregion

        #region Properties

        public static ILogger Logger { get; set; }

        #endregion

        #region Constructor

        static JobFactory()
        {
            BuildJobActionTypeMap();
        }

        #endregion

        #region Public Methods

        public static IJob Create(JobConfiguration config)
        {
            IJob retVal = null;

            if (config != null)
            {
                Type type = null;

                if (_jobActionTypeMap.TryGetValue(config.ActionType, out type))
                {
                    if (type != null)
                    {
                        Logger.Log(string.Format("Creating job of type \"{0}\".", type.Name));

                        retVal = Activator.CreateInstance(type, Logger, config) as IJob;
                    }
                }
                else
                {
                    Logger.Log(string.Format("Action type \"{0}\" not supported.  This job will not be created.", config.ActionType), LogMessageSeverity.Error);
                }
            }
            else
            {
                Logger.Log("Job cannot be created without a configuration", LogMessageSeverity.Error);
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        private static void BuildJobActionTypeMap()
        {
            _jobActionTypeMap = new Dictionary<string, Type>();

            var type = typeof(JobBase);

            var types = TypeLocator.FindTypes("*plugin*.dll", typeof(JobBase)).ToList();

            types.AddRange(Assembly.GetExecutingAssembly().GetTypes().Where(t => t != type && type.IsAssignableFrom(t)));

            foreach (var realtype in types)
            {
                _jobActionTypeMap.Add(realtype.Name, realtype);
            }
        }

        #endregion
    }
}
