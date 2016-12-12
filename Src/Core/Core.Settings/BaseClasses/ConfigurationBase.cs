using Core.Interfaces.Components.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace Core.Settings.BaseClasses
{
    public abstract class ConfigurationBase
    {
        #region Fields

        protected readonly ILogger _logger;

        #endregion

        #region Properties

        public string ConfigurationFile { get; private set; }

        #endregion

        #region Constructor

        protected ConfigurationBase(ILogger logger)
        {
            if (logger != null)
            {
                _logger = logger;
                Load();
                Print();
            }
            else
            {
                throw new ArgumentNullException("logger");
            }
        }

        #endregion

        #region Public Methods

        public void Load()
        {
            SetConfigurationFile();

            _logger.Log(string.Format("Loading configuration file from \"{0}\".", ConfigurationFile));

            var config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = ConfigurationFile }, ConfigurationUserLevel.None);

            foreach (var setting in FindConfigurationAttributes())
            {
                string key = config.AppSettings.Settings.AllKeys.Where(k => k.Equals(setting.Item2.KeyName)).FirstOrDefault();

                object value = key != null ? config.AppSettings.Settings[key].Value : setting.Item2.DefaultValue;

                TryChangeTypeAndSetValue(value, setting);
            }
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Print()
        {
            _logger.Log("Printing Configurations:");

            foreach (PropertyInfo info in FindConfigurationAttributes().Select(s => s.Item1))
            {
                var val = info.GetValue(this);
                string message = string.Format("*{0} = {1}", info.Name, val == null ? "[NULL]" : val.ToString());

                _logger.Log(message);
            }
        }

        #endregion

        #region Private Methods

        private void SetConfigurationFile()
        {
            //for now just load the one from the local directory, need to check other directory first in future
            ConfigurationFile = string.Format("{0}{1}", Assembly.GetEntryAssembly().Location, ".config");
        }

        private List<Tuple<PropertyInfo, ConfigurationAttribute>> FindConfigurationAttributes()
        {
            var retVal = new List<Tuple<PropertyInfo, ConfigurationAttribute>>();

            foreach(var info in GetFilteredProperties())
            {
                var atty = info.GetCustomAttribute<ConfigurationAttribute>();

                if (atty != null)
                {
                    //set defaults for the attribute before adding it to the tuple
                    SetAttributeDefaults(info, atty);

                    retVal.Add(new Tuple<PropertyInfo, ConfigurationAttribute>(info, atty));
                }

            }

            return retVal;
        }

        private void SetAttributeDefaults(PropertyInfo info, ConfigurationAttribute atty)
        {
            if (info != null && atty != null)
            {
                //set the KeyName as the property name if it is empty
                if (string.IsNullOrEmpty(atty.KeyName))
                {
                    atty.KeyName = info.Name;
                }
            }
            else
            {
                _logger.Log("SetAttributeDefault called with null parameters!", severity: LogMessageSeverity.Error);
            }
        }

        private void TryChangeTypeAndSetValue(object value, Tuple<PropertyInfo, ConfigurationAttribute> setting)
        {
            var info = setting.Item1;

            try
            {
                var obj = Convert.ChangeType(value, info.PropertyType);

                info.SetValue(this, obj);
            }
            catch
            {
                _logger.Log(string.Format("Error: Cannot change value to type {0} for AppSetting with key \"{1}\"", info.PropertyType.Name, setting.Item2.KeyName), severity: LogMessageSeverity.Error);
            }
        }

        private List<PropertyInfo> GetFilteredProperties()
        {
            return GetType().GetProperties().Where(p => !typeof(ConfigurationBase).IsAssignableFrom(p.PropertyType)).ToList();
        }

        #endregion
    }
}
