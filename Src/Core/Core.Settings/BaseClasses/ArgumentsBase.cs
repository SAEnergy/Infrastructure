using Core.Interfaces.Components.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Core.Settings
{
    public class ArgumentsBase
    {
        #region Fields

        protected readonly ILogger _logger;

        protected readonly bool _malformedArgumentsAreFatal;

        #endregion

        #region Properties

        public string CommandLineArguments { get; private set; }

        #endregion

        #region Constructor

        protected ArgumentsBase(ILogger logger, bool autoLoad = true, bool malformedArgumentsAreFatal = true)
        {
            _malformedArgumentsAreFatal = malformedArgumentsAreFatal;

            if (logger != null)
            {
                _logger = logger;

                if(autoLoad)
                {
                    Load();
                }

                Print();
            }
            else
            {
                throw new ArgumentNullException("logger");
            }
        }

        #endregion

        #region Public Methods

        public void Load(string[] args = null)
        {
            SetAllPropertiesToDefaultValue(); //always reset properties when Load is called, just incase someone reuses an object

            if (args == null)
            {
                args = Environment.GetCommandLineArgs();
            }

            foreach (var setting in FindArugmentAttributes())
            {
                string tokenName = GetTokenName(setting.Item2);

                int index = Array.FindIndex(args, a => a.ToUpperInvariant().StartsWith(tokenName.ToUpperInvariant()));

                if (setting.Item2.Optional || index >= 0)
                {
                    object value = index >= 0 ? GetValue(args[index], setting) : setting.Item2.DefaultValue;

                    TryChangeTypeAndSetValue(value, setting);
                }
                else
                {
                    _logger.Log(string.Format("Missing required command line argument \"{0}\".", tokenName), LogMessageSeverity.Error);

                    if (_malformedArgumentsAreFatal)
                    {
                        _logger.Log(string.Format("System exiting because missing arguments are configured to be fatal."), LogMessageSeverity.Error);
                        Environment.Exit(-1); //do not allow execution to continue, logging system will automatically clean up
                    }
                }
            }
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Print()
        {
            _logger.Log("Printing Arguments:");

            foreach (PropertyInfo info in FindArugmentAttributes().Select(s => s.Item1))
            {
                var val = info.GetValue(this);
                string message = string.Format("*{0} = {1}", info.Name, val == null ? "[NULL]" : val.ToString());

                _logger.Log(message);
            }
        }

        #endregion

        #region Private Methods

        private void SetAllPropertiesToDefaultValue()
        {
            foreach(PropertyInfo info in GetFilteredProperties())
            {
                info.SetValue(this, null); //trick to set property to default value
            }
        }

        private string GetValue(string arg, Tuple<PropertyInfo, ArgumentAttribute> setting)
        {
            string retVal = string.Empty;

            int index = arg.IndexOf(setting.Item2.Delimiter);

            if(setting.Item1.PropertyType == typeof(bool) && index == -1)
            {
                retVal = "true";
            }
            else
            {
                if(index >= 0 && arg.Length > index + 1)
                {
                    retVal = arg.Substring(index + 1);
                }
                else
                {
                    _logger.Log(string.Format("Argument \"{0}\" requires a delimiter of \"{1}\" with a value of type \"{2}\".", arg, setting.Item2.Delimiter, setting.Item1.PropertyType.Name), LogMessageSeverity.Error);

                    if (_malformedArgumentsAreFatal)
                    {
                        _logger.Log(string.Format("System exiting because errors in arguments are configured to be fatal."), LogMessageSeverity.Error);
                        Environment.Exit(-1); //do not allow execution to continue, logging system will automatically clean up
                    }
                }
            }

            return retVal;
        }

        private string GetTokenName(ArgumentAttribute attribute)
        {
            return string.Format("{0}{1}", attribute.Switch, attribute.Name);
        }

        private List<Tuple<PropertyInfo, ArgumentAttribute>> FindArugmentAttributes()
        {
            var retVal = new List<Tuple<PropertyInfo, ArgumentAttribute>>();

            foreach (var info in GetFilteredProperties())
            {
                var atty = info.GetCustomAttribute<ArgumentAttribute>();

                if (atty != null)
                {
                    //set defaults for the attribute before adding it to the tuple
                    SetAttributeDefaults(info, atty);

                    retVal.Add(new Tuple<PropertyInfo, ArgumentAttribute>(info, atty));
                }

            }

            return retVal;
        }

        private void SetAttributeDefaults(PropertyInfo info, ArgumentAttribute atty)
        {
            if (info != null && atty != null)
            {
                //set the KeyName as the property name if it is empty
                if (string.IsNullOrEmpty(atty.Name))
                {
                    atty.Name = info.Name;
                }
            }
            else
            {
                _logger.Log("SetAttributeDefault called with null parameters!", LogMessageSeverity.Error);
            }
        }

        private void TryChangeTypeAndSetValue(object value, Tuple<PropertyInfo, ArgumentAttribute> setting)
        {
            var info = setting.Item1;

            try
            {
                if(value == null && info.PropertyType == typeof(bool))
                {
                    value = false;
                }

                var obj = Convert.ChangeType(value, info.PropertyType);

                info.SetValue(this, obj);
            }
            catch
            {
                _logger.Log(string.Format("Error: Cannot change value to type {0} for argument with key \"{1}\"", info.PropertyType.Name, setting.Item2.Name), LogMessageSeverity.Error);
            }
        }

        private List<PropertyInfo> GetFilteredProperties()
        {
            return GetType().GetProperties().Where(p => p.DeclaringType != typeof(ArgumentsBase)).ToList(); ;
        }

        #endregion
    }
}
