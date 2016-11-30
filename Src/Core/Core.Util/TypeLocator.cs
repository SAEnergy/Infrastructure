using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Util
{
    public static class TypeLocator
    {
        public static IEnumerable<Type> FindTypes(string searchPattern, Type searchType)
        {
            return FindTypes(searchPattern, new Type[] { searchType });
        }

        public static IEnumerable<Type> FindTypes(string searchPattern, IEnumerable<Type> searchTypes)
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            List<Type> found = new List<Type>();

            AppDomain tempDomain = AppDomain.CreateDomain("bob", null, baseDir, null, false);

            TypeLocatorWorker bob = (TypeLocatorWorker)tempDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().ToString(), typeof(TypeLocatorWorker).FullName);

            string[] foundTypes = null;
            string[] errors = null;

            bob.Search(searchTypes.Select(s => s.AssemblyQualifiedName).ToArray(), baseDir, searchPattern, out foundTypes, out errors);

            foreach (string s in errors)
            {
                // todo: get this back to logging system?
            }

            foreach (string s in foundTypes)
            {
                found.Add(Type.GetType(s));
            }

            AppDomain.Unload(tempDomain);

            return found;
        }

        public static Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

        public static Type FindType(string typeName)
        {
            return FindType(null, typeName);
        }

        public static Type FindType(Type baseType, string typeName)
        {
            Type retval = null;
            if (_typeCache.TryGetValue(typeName, out retval)) { return retval; }

            foreach (Assembly assy in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assy.GetTypes())
                {
                    if (type.FullName == typeName || type.Name == typeName)
                    {
                        if (baseType != null)
                        {
                            Type cType = type.BaseType;
                            while (cType != null && cType != baseType)
                            {
                                cType = cType.BaseType;
                            }
                            if (cType != baseType) { continue; }
                        }
                        _typeCache.Add(typeName, type);
                        return type;
                    }
                }
            }

            throw new ArgumentException("Unable to locate type '" + typeName + "' in any loaded assembly.");
        }
        public static List<Type> SearchAssembly(Assembly assy, Type searchType)
        {
            return SearchAssembly(assy, new Type[] { searchType });
        }

        public static List<Type> SearchAssembly(Assembly assy, IEnumerable<Type> searchTypes)
        {
            List<Type> toRet = new List<Type>();

            foreach (Type searchType in searchTypes)
            {
                foreach (Type iter in assy.GetTypes())
                {
                    if (iter == searchType) { continue; }
                    if (iter.IsAbstract) { continue; }
                    if (searchType.IsAssignableFrom(iter))
                    {
                        toRet.Add(iter);
                        continue;
                    }
                    if (searchType.IsGenericType)
                    {
                        if (iter.BaseType.IsGenericType && iter.BaseType.GetGenericTypeDefinition() == searchType)
                        {
                            toRet.Add(iter);
                            continue;
                        }
                        if (iter.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == searchType))
                        {
                            toRet.Add(iter);
                            continue;
                        }
                    }
                }
            }
            return toRet;
        }
    }

    public class TypeLocatorWorker : MarshalByRefObject
    {
        public void Search(string[] typeNames, string baseDirectory, string searchPattern, out string[] foundTypes, out string[] errors)
        {
            List<string> toRet = new List<string>();
            List<string> errRet = new List<string>();
            List<Type> types = new List<Type>();

            foreach (string s in typeNames)
            {
                types.Add(Type.GetType(s));
            }

            try
            {
                var files = Directory.GetFiles(baseDirectory, searchPattern, SearchOption.TopDirectoryOnly);

                foreach (string file in files)
                {
                    try
                    {
                        var assm = Assembly.LoadFile(file);
                        toRet.AddRange(TypeLocator.SearchAssembly(assm, types).Select(t => t.AssemblyQualifiedName));

                    }
                    catch (Exception ex)
                    {
                        errRet.Add(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                errRet.Add(ex.ToString());
            }

            foundTypes = toRet.ToArray();
            errors = errRet.ToArray();
        }
    }
}
