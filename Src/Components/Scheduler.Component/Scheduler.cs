using Core.Interfaces;
using Core.Interfaces.Components;
using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Logging;
using Core.Models;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Scheduler.Component
{
    [ComponentRegistration(typeof(ISchedulerComponent))]
    [ComponentMetadata(AllowedActions = ComponentUserActions.All, Description = "Scheduling system.", FriendlyName = "Scheduler Component")]
    public sealed class Scheduler : Singleton<ISchedulerComponent>, ISchedulerComponent
    {
        #region Fields

        private TimeSpan _schedulerShutDownTimeOut = TimeSpan.FromMinutes(5);

        private readonly ILogger _logger;
        private readonly IDataComponent _dataComponent;

        private List<IJob> _jobs;

        private static object _syncObject = new object();

        public event JobStateEventHandler StateUpdated;

        #endregion

        #region Properties

        public bool IsRunning { get; private set; }

        #endregion

        #region Constructor

        private Scheduler(ILogger logger, IDataComponent dataComponent)
        {
            _logger = logger;
            JobFactory.Logger = logger;
            _dataComponent = dataComponent;
        }

        #endregion

        #region Public Methods

        public static ISchedulerComponent CreateInstance(ILogger logger, IDataComponent dataComponent)
        {
            return Instance = new Scheduler(logger, dataComponent);
        }

        public void Start()
        {
            lock (_syncObject)
            {
                if (!IsRunning)
                {
                    _logger.Log("Scheduler component starting...");

                    Task.Run(() => LoadAllJobs()); //does not block here on purpose
                }
            }
        }

        public void Stop()
        {
            lock (_syncObject)
            {
                if (IsRunning)
                {
                    _logger.Log("Scheduler component stopping.");

                    var task = Task.Run(() => CancelAllJobs());

                    task.Wait(_schedulerShutDownTimeOut); //wait for a while, and give up if it doesn't complete

                    IsRunning = false;

                    _logger.Log("Scheduler component stopped.");
                }
            }
        }

        public List<JobConfiguration> GetJobs()
        {
            return _dataComponent.Find<JobConfiguration>(j => !j.AuditInfo.IsArchived);
        }

        public void AddJob(JobConfiguration job)
        {
            if (_jobs != null)
            {
                lock (_jobs)
                {
                    var newJob = JobFactory.Create(job);

                    if (newJob != null)
                    {
                        _logger.Log(string.Format("Scheduler component adding new job named \"{0}\".", job.Name));
                        _jobs.Add(newJob);
                        newJob.StateUpdated += JobStateUpdated;
                        newJob.JobCompleted += JobCompleted;
                        newJob.Start();
                    }
                }
            }
        }

        private void JobStateUpdated(JobState state)
        {
            if (StateUpdated != null) { StateUpdated(state); }
        }

        private void JobCompleted(JobStatistics stats)
        {
            if (stats != null)
            {
                try
                {
                    _dataComponent.Insert<JobStatistics>(stats);
                }
                catch(Exception ex)
                {
                    _logger.Log("Exception while saving statistics: " + ex.Message, ex, severity: LogMessageSeverity.Error);
                }
            }
        }

        public void DeleteJob(JobConfiguration job)
        {
            lock (_jobs)
            {
                var found = _jobs.FirstOrDefault(j => j.Configuration.JobConfigurationId == job.JobConfigurationId);
                if (found == null) throw new InvalidOperationException("JobID " + job.JobConfigurationId + " not found.");
                if (found.Status == JobStatus.Running) { throw new InvalidOperationException("Cannot delete running job."); }
                _jobs.Remove(found);
            }
        }

        public void UpdateJob(JobConfiguration job)
        {
            lock (_jobs)
            {
                var found = _jobs.FirstOrDefault(j => j.Configuration.JobConfigurationId == job.JobConfigurationId);
                if (found == null) throw new InvalidOperationException("JobID " + job.JobConfigurationId + " not found.");
                found.Configuration = job;
            }
        }

        #endregion

        #region Private Methods

        private void CancelAllJobs()
        {
            if (_jobs != null)
            {
                lock (_jobs)
                {
                    if (_jobs.Count > 0)
                    {
                        _logger.Log("Scheduler component attempting to stop all jobs.");

                        List<Task> tasks = new List<Task>();
                        foreach (var iter in _jobs)
                        {
                            var job = iter;
                            tasks.Add(Task.Run(() => job.Stop()));
                        }

                        while(true)
                        {
                            foreach (var task in tasks)
                            {
                                if (!task.IsCompleted)
                                {
                                    Thread.Sleep(100);
                                    continue;
                                }
                            }
                            break;
                        }

                        _logger.Log("Scheduler component has stopped all jobs.");
                    }
                }
            }
        }

        private void LoadAllJobs()
        {
            try
            {
                _jobs = new List<IJob>();

                _logger.Log("Scheduler loading job types...");

                JobFactory.Initialize();

                _logger.Log("Scheduler loading all jobs from storage...");

                var query = GetJobs();

                if (query != null)
                {
                    IsRunning = true;

                    foreach (var jobConfig in query)
                    {
                        AddJob(jobConfig);
                    }

                    _logger.Log("Scheduler loaded " + query.Count + " jobs.");
                }
                else
                {
                    _logger.Log("Storage returned null as result!", severity: LogMessageSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Log("Error loading jobs: " + ex.ToString(), ex, severity: LogMessageSeverity.Error);
            }
        }

        public List<JobState> GetStates()
        {
            lock (_jobs)
            {
                return _jobs.Select(j => j.State).ToList();
            }
        }

        public void RunJob(JobConfiguration job)
        {
            lock (_jobs)
            {
                _jobs.First(j => j.Configuration.JobConfigurationId == job.JobConfigurationId).ForceRun();
            }
        }

        public void CancelJob(JobConfiguration job)
        {
            IJob toCancel = null;
            lock (_jobs)
            {
                toCancel = _jobs.First(j => j.Configuration.JobConfigurationId == job.JobConfigurationId);
            }
            toCancel.TryCancel();
        }

        public JobStatistics GetLatestStatistics(JobConfiguration job)
        {
            return _dataComponent.All<JobStatistics>()
                .Where(s => s.JobID == job.JobConfigurationId)
                .OrderByDescending(j => j.StartTime)
                .FirstOrDefault();
        }

        #endregion
    }
}
