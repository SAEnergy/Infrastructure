using Core.Interfaces.Components.Logging;
using Core.Logging;
using Core.Logging.LogDestinations;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VersionReader
{
    public class Program
    {
        private static Settings _settings;

        public static int Main(string[] args)
        {
            Environment.ExitCode = 0;

            ILogger logger = Logger.CreateInstance();
            logger.AddLogDestination(new ConsoleLogDestination());

            logger.Start();

            _settings = new Settings(logger);

            if (File.Exists(_settings.AssemblyInfoFile))
            {
                var lines = File.ReadAllLines(_settings.AssemblyInfoFile);

                string oldVersion = string.Empty;
                string finalVersion = string.Empty;

                for (int i = 0; i < lines.Count(); i++)
                {
                    var line = lines[i];

                    if (line.StartsWith("[assembly:"))
                    {
                        if (line.Contains("Version"))
                        {
                            if (string.IsNullOrEmpty(oldVersion))
                            {
                                var reg = new Regex(@"\d+(?:\.\d+)+");
                                var mc = reg.Match(line);

                                if (mc.Success)
                                {
                                    oldVersion = mc.Value;

                                    int indexOfLastDot = oldVersion.LastIndexOf('.');

                                    if (indexOfLastDot > 0)
                                    {
                                        string realVersion = oldVersion.Substring(0, indexOfLastDot);

                                        logger.Log(string.Format("##teamcity[setParameter name='{0}' value='{1}']", _settings.TeamCityParameterName, realVersion));

                                        finalVersion = string.Format("{0}.{1}", realVersion, _settings.BuildNumber);

                                        logger.Log(string.Format("##teamcity[buildNumber '{0}']",  finalVersion));

                                        logger.Log(string.Format("Setting version to \"{0}\".", finalVersion));

                                        lines[i] = line.Replace(oldVersion, finalVersion);
                                    }
                                    else
                                    {
                                        logger.Log(string.Format("Cannot find version number in file \"{0}\".", _settings.AssemblyInfoFile), severity: LogMessageSeverity.Error);
                                        Environment.ExitCode--;
                                    }
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(finalVersion))
                                {
                                    lines[i] = line.Replace(oldVersion, finalVersion);
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(finalVersion))
                {
                    logger.Log(string.Format("Saving to file \"{0}\".", _settings.AssemblyInfoFile));
                    File.WriteAllLines(_settings.AssemblyInfoFile, lines);
                }
            }
            else
            {
                logger.Log(string.Format("Cannot find file \"{0}\".", _settings.AssemblyInfoFile), severity: LogMessageSeverity.Error);
                Environment.ExitCode--;
            }

            logger.Stop();

            return Environment.ExitCode;
        }
    }
}
