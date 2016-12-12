using Core.Interfaces.Components.Logging;
using Core.Util;
using Scheduler.Component.Jobs;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Scheduler.Component
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

        #endregion

        #region Public Methods

        public static IJob Create(JobConfiguration config)
        {
            IJob retVal = null;

            if (config != null)
            {
                Type type = null;

                lock (_jobTypeMap)
                {
                    if (_jobTypeMap.Keys.Count == 0)
                    {
                        Initialize();
                    }
                }

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
                    Logger.Log(string.Format("Action type \"{0}\" not supported.  This job will not be created.", config.GetType()), severity: LogMessageSeverity.Error);
                }
            }
            else
            {
                Logger.Log("Job cannot be created without a configuration", severity: LogMessageSeverity.Error);
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        public static void Initialize()
        {
            lock (_jobTypeMap)
            {
                _jobTypeMap.Clear();

                var jobBases = TypeLocator.FindTypes("*.dll", typeof(JobBase<,>)).ToList();
                var jobConfigs = TypeLocator.FindTypes("*.dll", typeof(JobConfiguration)).ToList();

                Logger.Log("Scheduler Available Jobs: " + Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", jobBases.Select(s => s.Name)));
                Logger.Log("Scheduler Available Configurations: " + Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", jobConfigs.Select(s => s.Name)));

                //types.AddRange(Assembly.GetExecutingAssembly().GetTypes().Where(t => t != type && type.IsAssignableFrom(t)));

                foreach (var realtype in jobBases)
                {
                    foreach (ConstructorInfo con in realtype.GetConstructors())
                    {
                        foreach (ParameterInfo parm in con.GetParameters())
                        {
                            if (jobConfigs.Contains(parm.ParameterType))
                            {
                                if (_jobTypeMap.ContainsKey(parm.ParameterType))
                                {
                                    throw new InvalidOperationException("Job Configuration Type '" + parm.ParameterType + "' maps to two Job Types '" + realtype + "','" + _jobTypeMap[parm.ParameterType] + "'.");
                                }
                                _jobTypeMap.Add(parm.ParameterType, realtype);
                            }
                        }
                    }
                }

                Logger.Log("Scheduler Jobs Mapping: " + Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", _jobTypeMap.Keys.Select(s => s.Name + "->" + _jobTypeMap[s].Name)));
            }
        }

        #endregion
    }
}
