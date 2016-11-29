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

        // map job configuration types to job implementation types.
        private static Dictionary<Type, Type> _jobTypeMap = new Dictionary<Type, Type>();

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

                if (_jobTypeMap.TryGetValue(config.GetType(), out type))
                {
                    if (type != null)
                    {
                        Logger.Log(string.Format("Creating job of type \"{0}\".", type.Name));
                        retVal = Activator.CreateInstance(type, Logger, config) as IJob;
                    }
                }
                else
                {
                    Logger.Log(string.Format("Action type \"{0}\" not supported.  This job will not be created.", config.GetType()), LogMessageSeverity.Error);
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
            var type = typeof(JobBase<>);

            var jobBases = TypeLocator.FindTypes("*.dll", typeof(JobBase<>)).ToList();
            var jobConfigs = TypeLocator.FindTypes("*.dll", typeof(JobConfiguration)).ToList();

            //types.AddRange(Assembly.GetExecutingAssembly().GetTypes().Where(t => t != type && type.IsAssignableFrom(t)));

            foreach (var realtype in jobBases)
            {
                foreach (ConstructorInfo con in realtype.GetConstructors())
                {
                    foreach (ParameterInfo parm in con.GetParameters())
                    {
                        if (jobConfigs.Contains(parm.ParameterType))
                        {
                            _jobTypeMap.Add(parm.ParameterType, realtype);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
