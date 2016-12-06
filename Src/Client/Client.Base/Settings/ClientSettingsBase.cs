using Core.Models;
using Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace Client.Base
{
    public class ClientSettingsBase : DependencyObject
    {
        [PropertyEditorMetadata(Hidden = true)]
        public virtual string Name { get { return PascalCaseSplitter.Split(this.GetType().Name); } }

        public virtual IEnumerable<KeyValuePair<string, object>> Serialize()
        {
            List<KeyValuePair<string, object>> values = new List<KeyValuePair<string, object>>();

            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                if (prop.DeclaringType == typeof(DependencyObject)) { continue; }
                if (prop.DeclaringType == typeof(DispatcherObject)) { continue; }

                PropertyEditorMetadataAttribute atty = prop.GetCustomAttribute<PropertyEditorMetadataAttribute>();
                if (atty!=null && atty.Hidden) { continue; }

                values.Add(new KeyValuePair<string, object>(GetKeyName(prop), SerializeOneProperty(prop)));
            }
            return values;
        }

        protected virtual object SerializeOneProperty(PropertyInfo prop)
        {
            //if (typeof(ICollection).IsAssignableFrom(prop.PropertyType))
            //{
            //    ICollection val = (ICollection)prop.GetValue(this);
            //    List<string> values = new List<string>();
            //    foreach (object obj in val)
            //    {
            //        values.Add(ReflectionHelper.CoerceToString(obj));
            //    }
            //    return new KeyValuePair<string, object>(GetKeyName(prop), values.ToArray());
            //}
            //else
            //{
            //    object val = prop.GetValue(this);
            //    return new KeyValuePair<string, object>(GetKeyName(prop), ReflectionHelper.CoerceToString(val));
            //}

            object val = prop.GetValue(this);
            return CoerceToString(val);
        }

        public virtual void Unserialize(IEnumerable<KeyValuePair<string, object>> values)
        {
            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                if (prop.DeclaringType == typeof(DependencyObject)) { continue; }
                if (prop.DeclaringType == typeof(DispatcherObject)) { continue; }

                var value = values.FirstOrDefault(p => p.Key == GetKeyName(prop));
                if (value.Key != null)
                {
                    UnserializeOneProperty(prop, value.Value);
                }
            }
        }

        protected virtual void UnserializeOneProperty(PropertyInfo prop, object value)
        {
            if (prop.SetMethod == null || !prop.SetMethod.IsPublic) { return; }
            //if (typeof(ICollection).IsAssignableFrom(prop.PropertyType))
            //{
            ////ICollection col = (ICollection) prop.GetValue(this, null);
            //ICollection target = (ICollection) prop.GetValue(this);
            //Type iColType = prop.PropertyType.GetInterfaces().FirstOrDefault(i => i.Name.StartsWith("ICollection") && i.IsGenericType);
            //MethodInfo addMethod = iColType.GetMethods().FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 1);
            //if (addMethod != null)
            //{
            //    addMethod.Invoke(target, new object[] { new KeyValuePair<string, string>("c", "d") });
            //}

            //ICollection val = new Dictionary<string,string>()
            //System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bob
            //IEnumerable val = (IEnumerable)prop.GetValue(this);
            //List<string> values = new List<string>();
            //foreach (object obj in val)
            //{
            //    values.Add(ReflectionHelper.CoerceToString(obj));
            //}
            //return new KeyValuePair<string, object>(GetKeyName(prop), values.ToArray());
            //}
            //else
            //{
            //    prop.SetValue(this, ReflectionHelper.CoerceFromString(prop.PropertyType, value.Value));
            //}

            prop.SetValue(this, CoerceFromString(prop.PropertyType, value));
        }

        private static string GetKeyName(PropertyInfo prop)
        {
            return prop.DeclaringType.Name + "." + prop.Name;
        }

        private static T CoerceFromString<T>(string value)
        {
            // we may need some special logic in here someday, but this currently handles everything
            return (T)CoerceFromString(typeof(T), value);
        }

        private static object CoerceFromString(Type type, object value)
        {
            // we may need some special logic in here someday, but this currently handles everything
            return Convert.ChangeType(value, type);
        }

        private static string CoerceToString(object value)
        {
            if (value == null) return string.Empty;
            // we may need some special logic in here someday, but this currently handles everything
            return value.ToString();
        }
    }
}
