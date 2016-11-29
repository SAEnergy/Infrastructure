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
        public static IEnumerable<Type> FindTypes(string searchPattern, Type supportedInterface)
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            List<Type> found = new List<Type>();

            AppDomain tempDomain = AppDomain.CreateDomain("bob", null, baseDir, null, false);

            TypeLocatorWorker bob = (TypeLocatorWorker)tempDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().ToString(), typeof(TypeLocatorWorker).FullName);

            string[] foundTypes = null;
            string[] errors = null;

            bob.Search(new string[] { supportedInterface.AssemblyQualifiedName }, baseDir, searchPattern, out foundTypes, out errors);

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

                        foreach (Type type in types)
                        {
                            if (type.IsGenericType)
                            {
                                foreach (Type st in assm.GetTypes())
                                {
                                    if (st.Name.Contains("UnitTestJob")) { Console.WriteLine("moo"); }
                                    if (st== type) { continue; }
                                    if (st.BaseType==null) { continue; }
                                    if (st.BaseType.IsGenericType && st.BaseType.GetGenericTypeDefinition()==type)
                                    {
                                        toRet.Add(st.AssemblyQualifiedName);
                                    }
                                }
                            }
                            else
                            {
                                var found = assm.GetTypes().Where(t =>
                                    t != type &&
                                    type.IsAssignableFrom(t) &&
                                    t.ContainsGenericParameters == false);
                                toRet.AddRange(found.Select(t => t.AssemblyQualifiedName));
                            }
                        }
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
