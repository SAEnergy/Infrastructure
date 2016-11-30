using Core.Interfaces.Components.Logging;
using Core.Interfaces.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Core.Interfaces.Components.IoC;
using Core.Models;
using Core.Util;

namespace Server.Components
{
    [ComponentRegistration(ComponentType.All, typeof(IDataComponent))]
    [ComponentMetadata(Description = "Data access layer for XML storage.", FriendlyName = "XML Data Component")]
    public sealed class XMLDataComponent : IDataComponent
    {
        #region Fields

        private const string ROOTNAME = "Data";

        private readonly ILogger _logger;

        private const string defaultFolder = "%PROGRAMDATA%\\HostService";
        private const string defaultFileName = "Data.xml";

        private static object _syncObject = new object();

        private XDocument _document = null;

        #endregion

        #region Properties

        public static string Folder { get; set; }

        public static string FileName { get; set; }

        #endregion

        #region Constructor

        public XMLDataComponent(ILogger logger)
        {
            Folder = string.IsNullOrEmpty(Folder) ? defaultFolder : Folder;
            FileName = string.IsNullOrEmpty(FileName) ? defaultFileName : FileName;

            _logger = logger;
        }

        #endregion

        #region Public Methods

        public bool Delete<T>(Func<T, bool> where) where T : class
        {
            bool rc = false;

            if(where != null)
            {
                var children = GetObjects<T>();

                var toRemove = children.Where(where).ToList();

                if (toRemove.Count > 0)
                {
                    toRemove.ForEach(r => children.Remove(r));

                    var element = GetTypeElement<T>();

                    element.RemoveAll();

                    children.ForEach(o => element.Add(ToXElement(o)));

                    rc = SaveXMLFile();
                }
            }

            return rc;
        }

        public bool Delete<T>(T obj) where T : class
        {
            bool rc = false;

            if (obj != null)
            {
                int key = GetKeyValue(obj);

                rc = Delete<T>(key);
            }

            return rc;
        }

        public bool Delete<T>(int key) where T : class
        {
            bool rc = false;

            var element = GetElements<T>().Where(x => GetKeyValue(FromXElement<T>(x)) == key).FirstOrDefault();

            if (element != null)
            {
                element.Remove();

                rc = SaveXMLFile();
            }

            return rc;
        }

        public T Find<T>(int key) where T : class
        {
            var children = GetObjects<T>();

            return children.Where(x => GetKeyValue(x) == key).FirstOrDefault();
        }

        public List<T> Find<T>(Func<T, bool> where) where T : class
        {
            var list = new List<T>();

            if(where != null)
            {
                list.AddRange(GetObjects<T>().Where(where));
            }

            return list;
        }

        public bool Insert<T>(T obj) where T : class
        {
            bool rc = false;

            if (obj != null)
            {
                var children = GetObjects<T>();

                var lastObject = children.OrderByDescending(x => GetKeyValue(x)).FirstOrDefault();

                int currentKey = lastObject == null ? 0 : GetKeyValue(lastObject);

                SetKey(obj, ++currentKey);

                var element = GetTypeElement<T>();

                element.Add(ToXElement(obj));

                rc = SaveXMLFile();
            }

            return rc;
        }

        public bool Update<T>(T obj) where T : class
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        private void LoadXMLFile()
        {
            if(_document == null)
            {
                lock(_syncObject)
                {
                    if(_document == null)
                    {
                        string fullFileName = GetFullFileName();

                        CreateNewFileIfNeeded(fullFileName);

                        _logger.Log(string.Format("XMLDataComponent loading xml data from \"{0}\".", fullFileName));

                        _document = XDocument.Load(fullFileName);

                        if (_document == null)
                        {
                            throw new Exception(string.Format("Cannot load XML file \"{0}\".", fullFileName));
                        }
                    }
                }
            }
        }

        private bool SaveXMLFile()
        {
            bool rc = false;

            if(_document != null)
            {
                lock(_document)
                {
                    string fullFileName = GetFullFileName();

                    try
                    {
                        _document.Save(fullFileName);

                        rc = true;
                    }
                    catch(Exception ex)
                    {
                        _logger.Log(string.Format("XMLDataComponent cannot save xml data to \"{0}\" - Error {1}.", fullFileName, ex.Message), LogMessageSeverity.Error);
                    }
                }
            }

            return rc;
        }

        private XElement ToXElement<T>(T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (TextWriter streamWriter = new StreamWriter(memoryStream))
                {
                    var emptyNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
                    var xmlSerializer = new XmlSerializer(obj.GetType());

                    xmlSerializer.Serialize(streamWriter, obj, emptyNs);
                    XElement elem = XElement.Parse(Encoding.ASCII.GetString(memoryStream.ToArray()));
                    return elem;
                }
            }
        }

        private T FromXElement<T>(XElement xElement)
        {
            Type objType = null;
            if (xElement.Name.LocalName != typeof(T).Name)
            {
                 objType = TypeLocator.FindType(typeof(T),xElement.Name.LocalName);
            }
            var xmlSerializer = new XmlSerializer(objType??typeof(T));
            return (T)xmlSerializer.Deserialize(xElement.CreateReader());
        }

        private List<T> GetObjects<T>()
        {
            var list = new List<T>();

            var elements = GetElements<T>();

            foreach(var element in elements)
            {
                list.Add(FromXElement<T>(element));
            }

            return list;
        }

        private string GetFullFileName()
        {
            return string.Format(string.Format("{0}\\{1}", CreateAndGetFolderName(), FileName));
        }

        private void CreateNewFileIfNeeded(string fullFileName)
        {
            if (!File.Exists(fullFileName))
            {
                _logger.Log(string.Format("File \"{0}\" not found, creating file.", fullFileName));

                var doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("XML File for storing " + ROOTNAME));
                doc.Add(new XElement(ROOTNAME));
                doc.Save(fullFileName);
            }
        }

        private XElement GetTypeElement<T>()
        {
            if(_document == null)
            {
                LoadXMLFile();
            }

            var element = _document.Root.Element(GetTypeElementName(typeof(T)));

            if (element == null)
            {
                element = new XElement(GetTypeElementName(typeof(T)));
                _document.Root.Add(element);
                SaveXMLFile();
            }

            return element;
        }

        private List<XElement> GetElements<T>()
        {
            var element = GetTypeElement<T>();

            return element.Elements().ToList();
        }

        private string GetTypeElementName(Type type)
        {
            return string.Format("{0}s", type.Name);
        }

        private string CreateAndGetFolderName()
        {
            string fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(Folder));

            if(!Directory.Exists(fullPath))
            {
                _logger.Log(string.Format("Creating directory \"{0}\" for XMLDataComponent", fullPath));

                Directory.CreateDirectory(Folder);
            }

            return fullPath;
        }

        private void SetKey<T>(T obj, int keyValue)
        {
            var prop = GetKeyProperty<T>();

            if (prop != null)
            {
                prop.SetValue(obj, keyValue);
            }
        }

        private int GetKeyValue<T>(T obj)
        {
            int retVal = -1;

            var prop = GetKeyProperty<T>();

            if (prop != null)
            {
                retVal = Convert.ToInt32(prop.GetValue(obj));
            }

            return retVal;
        }

        private PropertyInfo GetKeyProperty<T>()
        {
            Type type = typeof(T);

            var prop = type.GetProperties().Where(p => p.CustomAttributes.Any(a => a.GetType() == typeof(KeyAttribute))).FirstOrDefault();

            if (prop == null)
            {
                prop = type.GetProperty(GetKeyPropertyName(type));
            }

            if(prop == null)
            {
                _logger.Log(string.Format("Unable to find primary key property!  Check implementation for type \"{0}\", must have a property named \"{1}\" or [Key] attribute.", type.Name, GetKeyPropertyName(type)), LogMessageSeverity.Critical);
            }

            return prop;
        }

        private string GetKeyPropertyName(Type type)
        {
            return string.Format("{0}{1}",type.Name,"Id");
        }

        #endregion
    }
}
