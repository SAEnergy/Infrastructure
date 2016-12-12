using Core.Interfaces.Components.Logging;
using Scheduler.Component.Jobs;
using Scheduler.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Scheduler.Component.Jobs
{
    public sealed class RunProgramJob : JobBase<RunProgramJobConfiguration>
    {
        #region Fields

        private TimeSpan _processWaitTimer = TimeSpan.FromSeconds(1);

        #endregion

        #region Constructor

        public RunProgramJob(ILogger logger, RunProgramJobConfiguration config) : base(logger, config)
        {

        }

        #endregion

        #region Public Methods

        public override bool Execute()
        {
            bool rc = false;

            var proc = CreateProcess();

            if (proc != null)
            {
                proc.Start();
                StartCapturingOutput(proc);

                while (!proc.WaitForExit((int)_processWaitTimer.TotalMilliseconds))
                {
                    if (TaskCancellationToken.IsCancellationRequested)
                    {
                        //if we are not going to kill the proc, then leave it to do it's thing
                        if (Configuration.KillProcOnCancel)
                        {
                            _logger.Log(string.Format("Job \"{0}\" is configured to kill process on cancel.  Killing process...", Configuration.Name), severity: LogMessageSeverity.Error);

                            proc.Kill();
                        }

                        StopCapturingOutput(proc);

                        TaskCancellationToken.ThrowIfCancellationRequested();
                    }
                }

                proc.WaitForExit(); //according to MSDN call this even after the timeout above and returned true

                StopCapturingOutput(proc);

                rc = proc.ExitCode == 0;
            }
            else
            {
                _logger.Log(string.Format("Unable to start process for job \"{0}\".", Configuration.Name), severity: LogMessageSeverity.Error);
            }

            return rc;
        }

        #endregion

        #region Private Methods

        private Process CreateProcess()
        {
            Process proc = null;
            string path = null; 

            if (!string.IsNullOrWhiteSpace(Configuration.WorkingDirectory))
            {
                path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(Configuration.WorkingDirectory));
            }
            else
            {
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

            string fileName = Path.Combine(path, Configuration.FileName);

            if (File.Exists(fileName))
            {
                var info = new ProcessStartInfo();
                info.FileName = fileName;
                info.WorkingDirectory = path;
                info.Arguments = Configuration.Arguments;
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                info.RedirectStandardOutput = Configuration.CaptureOutput;
                info.RedirectStandardError = Configuration.CaptureOutput;

                proc = new Process();
                proc.StartInfo = info;

                if (Configuration.CaptureOutput)
                {
                    proc.OutputDataReceived += (sender, args) =>
                    {
                        _logger.Log(string.Format("Job \"{0}\": {1}", Configuration.Name, args.Data));
                    };

                    proc.ErrorDataReceived += (sender, args) =>
                    {
                        _logger.Log(string.Format("Job \"{0}\": {1}", Configuration.Name, args.Data), severity: LogMessageSeverity.Error);
                    };
                }
            }
            else
            {
                _logger.Log(string.Format("Job \"{0}\" misconfigured.  Unable to locate file \"{1}\" in directory \"{2}.", Configuration.Name, Configuration.FileName, path), severity: LogMessageSeverity.Error);
            }

            return proc;
        }

        private void StartCapturingOutput(Process proc)
        {
            if (Configuration.CaptureOutput)
            {
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }
        }

        private void StopCapturingOutput(Process proc)
        {
            if (Configuration.CaptureOutput)
            {
                proc.CancelOutputRead();
                proc.CancelErrorRead();
            }
        }

        #endregion
    }
}
