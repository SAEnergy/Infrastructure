using Core.Interfaces.Components.Logging;
using Core.Logging;
using Core.Logging.LogDestinations;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectUpdater
{
    public class Program
    {
        private static Settings _settings;
        private static ILogger _logger;
        private const string _searchPath = "*.csproj";
        private const string _globalAssemblyInfo = "GlobalAssemblyInfo.cs";
        private const string _defaultFolderPath = "Run";
        private const string _folderPathFormat = "$(SolutionDir)Out\\{0}\\$(Configuration)";

        private static Dictionary<string, string> _folderMap = new Dictionary<string, string>()
        {
            { "Test", "Test"},
            { "Utilities", "Util" },
            { "Installer", "Installer" }
        };

        public static int Main(string[] args)
        {
            Environment.ExitCode = 0;

            _logger = Logger.CreateInstance();
            _logger.AddLogDestination(new ConsoleLogDestination());
            _logger.Start();

            try
            {
                _settings = new Settings(_logger);
                _settings.SourceFolder = Path.GetFullPath(_settings.SourceFolder);

                string[] files = Directory.GetFiles(_settings.SourceFolder, _searchPath, SearchOption.AllDirectories);
                _logger.Log("Found "+ files.Length + " projects.");

                foreach (var projectFile in files)
                {
                    if (!string.IsNullOrEmpty(projectFile))
                    {
                        if (File.Exists(projectFile))
                        {
                            var project = new Project(projectFile);

                            GlobalAssemblyUpdater(project);

                            OutFolderUpdater(project);
                        }
                    }
                }

            }
            catch(Exception ex)
            {
                _logger.Log(ex.ToString(), severity: LogMessageSeverity.Critical);
                Environment.ExitCode--;
            }

            _logger.Log("Done, returning " + Environment.ExitCode + ", bye!");
            _logger.Stop();
            return Environment.ExitCode;
        }

        private static void GlobalAssemblyUpdater(Project project)
        {
            if (project != null)
            {
                var globalAssemblyResult = project.Items.Where(i => i.ItemType.Contains("Compile") && i.UnevaluatedInclude.Contains(_globalAssemblyInfo)).FirstOrDefault();

                if (globalAssemblyResult == null)
                {
                    if (!_settings.IsReadOnly)
                    {
                        _logger.Log(string.Format("Adding \"{0}\" link to project \"{1}\"", _globalAssemblyInfo, project.FullPath));

                        var kvp = new KeyValuePair<string, string>("Link", string.Concat("Properties\\", _globalAssemblyInfo));

                        var rc = project.AddItem("Compile", string.Concat(FindRelativePathFromFolder(project.FullPath), _globalAssemblyInfo), new[] { kvp });

                        project.Save();
                    }

                    if (_settings.Verify)
                    {
                        _logger.Log(string.Format("Project \"{0}\" did not include link to \"{1}\"", project.FullPath, _globalAssemblyInfo));
                        Environment.ExitCode--;
                    }
                }
            }
        }

        private static void OutFolderUpdater(Project project)
        {
            if (project != null)
            {
                foreach(var child in project.Xml.AllChildren.Where(c => c is ProjectPropertyElement))
                {
                    var element = child as ProjectPropertyElement;

                    if(element != null)
                    {
                        if(element.Name == "OutputPath")
                        {
                            var path = string.Format(_folderPathFormat, GetPathMap(project.FullPath));

                            if (element.Value != path)
                            {
                                if (!_settings.IsReadOnly)
                                {
                                    _logger.Log(string.Format("Setting project \"{0}\" property \"OutputPath\" value  to \"{1}\"", project.FullPath, path));

                                    element.Value = path;
                                    project.Save();
                                }

                                if(_settings.Verify)
                                {
                                    _logger.Log(string.Format("Project \"{0}\" does not have property \"OutputPath\" set to value \"{1}\"", project.FullPath, path));
                                    Environment.ExitCode--;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string FindRelativePathFromFolder(string file)
        {
            string retVal = string.Empty;
            string folder = _settings.SourceFolder;

            int index = file.IndexOf(folder);

            if(index > -1)
            {
                var uncommonPath = file.Substring(index + folder.Length + 1);

                int count = uncommonPath.Count(c => c.Equals(Path.DirectorySeparatorChar));

                for(int i = 0; i < count; i++)
                {
                    retVal = string.Concat(retVal, "..", Path.DirectorySeparatorChar);
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            return retVal;
        }

        private static string GetPathMap(string path)
        {
            var retVal = _defaultFolderPath;

            int index = path.IndexOf(_settings.SourceFolder);

            if(index > -1)
            {
                var substring = path.Substring(index + _settings.SourceFolder.Length + 1);

                var split = substring.Split(new char[] { Path.DirectorySeparatorChar });

                if(split.Length > 1)
                {
                    string val;

                    if(_folderMap.TryGetValue(split[0], out val))
                    {
                        retVal = val;
                    }
                }
            }
            else
            {
                throw new NotSupportedException(); 
            }

            return retVal;
        }
    }
}
