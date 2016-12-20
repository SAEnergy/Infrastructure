using Core.Util;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace Client.Base
{
    public class ClientSettingsEngine
    {
        private static ClientSettingsEngine _instance;
        public static ClientSettingsEngine Instance { get { return _instance; } }

        static ClientSettingsEngine() { _instance = new ClientSettingsEngine(); }

        private const string _keyname = "SimpleClient";
        private List<ClientSettingsBase> Instances = new List<ClientSettingsBase>();

        public void Load()
        {
            lock (Instances)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
                key = key.CreateSubKey(_keyname);
                List<KeyValuePair<string, object>> values = new List<KeyValuePair<string, object>>();
                foreach (string name in key.GetValueNames())
                {
                    values.Add(new KeyValuePair<string, object>(name, key.GetValue(name)));
                }

                List<Type> types = new List<Type>();
                types.AddRange(TypeLocator.FindTypes("*.dll", typeof(ClientSettingsBase)));
                types.AddRange(TypeLocator.FindTypes("*.exe", typeof(ClientSettingsBase)));

                foreach (Type t in types.Distinct())
                {
                    ClientSettingsBase instance = (ClientSettingsBase)Activator.CreateInstance(t);
                    instance.Unserialize(values);
                    Instances.Add(instance);
                }
            }
        }

        public void Save()
        {
            lock (Instances)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
                key = key.CreateSubKey(_keyname);

                foreach (ClientSettingsBase instance in Instances)
                {
                    foreach (var pair in instance.Serialize())
                    {
                        key.SetValue(pair.Key, pair.Value);
                    }
                }
            }
        }

        public IEnumerable<ClientSettingsBase> GetInstances()
        {
            lock (Instances)
            {
                return Instances.ToArray();
            }
        }

        public T GetInstance<T>() where T : ClientSettingsBase
        {
            lock (Instances)
            {
                return (T)Instances.FirstOrDefault(t => t.GetType() == typeof(T));
            }
        }

        public ClientSettingsBase this[string name]
        {
            get
            {
                lock (Instances)
                {
                    return Instances.FirstOrDefault(t => string.Equals(name, t.GetType().Name,StringComparison.CurrentCultureIgnoreCase));
                }
            }
        }

    }
}
