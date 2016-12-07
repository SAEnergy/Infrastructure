using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Models.Persistent;
using System.Threading;
using Core.Interfaces.Components.Logging;
using System.Diagnostics;
using System.Globalization;
using Scheduler.Interfaces;

namespace Scheduler.Component.Jobs
{

    public abstract class JobBase<ConfigType> : JobBase<ConfigType, JobStatistics>
        where ConfigType : JobConfiguration
    {
        public JobBase(ILogger logger, ConfigType config) : base(logger, config) { }
    }

    public abstract class JobBase<ConfigType, StatisticType> : IJob<ConfigType> 
        where ConfigType : JobConfiguration
        where StatisticType : JobStatistics, new()
    {
        #region Fields

        private TimeSpan _cancelWaitCycle = TimeSpan.FromMilliseconds(1000);

        protected readonly ILogger _logger;

        private CancellationTokenSource cancelSource;
        private List<JobRunInfo<StatisticType>> _infos = new List<JobRunInfo<StatisticType>>();
        private bool _isRunning;

        public event JobStateEventHandler StateUpdated;
        public event JobStateEventHandler JobCompleted;
        
        #endregion

        #region Properties

        public ConfigType Configuration { get; private set; }

        JobConfiguration IJob.Configuration { get { return Configuration; } }

        public JobState State { get; private set; }

        public JobStatus Status
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Constructor

        protected JobBase(ILogger logger, ConfigType config)
        {
            State = new JobState();
            State.Status = JobStatus.Unknown;

            _logger = logger;
            Configuration = config;
            cancelSource = new CancellationTokenSource();

            _logger.Log(string.Format("Job name \"{0}\" created of type \"{1}\".", config.Name, GetType()));
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (!_isRunning)
            {
                _logger.Log(string.Format("Job name \"{0}\" starting.", Configuration.Name));
                _isRunning = true;

                LaunchNextJob(cancelSource.Token, false);
            }
        }

        public void ForceRun()
        {
            if (Configuration.RunState != JobRunState.Disabled)
            {
                LaunchNextJob(cancelSource.Token, true);
            }
            else
            {
                _logger.Log(string.Format("Cannot force run disabled job by the name of \"{0}\".", Configuration.Name), LogMessageSeverity.Warning);
            }
        }

        public void TryCancel()
        {
            if (cancelSource != null)
            {
                if (InfosCount() > 0)
                {
                    State.Status = JobStatus.Cancelling;
                    FireStatusUpdate();


                    _logger.Log(string.Format("Job name \"{0}\" canceling all scheduled and currently executing tasks.  ", Configuration.Name), LogMessageSeverity.Warning);

                    cancelSource.Cancel();

                    while (InfosCount() > 0)
                    {
                        _logger.Log(string.Format("Job name \"{0}\" waiting on tasks to cancel...", Configuration.Name), LogMessageSeverity.Warning);
                        Thread.Sleep(_cancelWaitCycle);
                    }

                    State.Status = JobStatus.Cancelled;
                    State.Statistics = null;
                    FireStatusUpdate();

                    _logger.Log(string.Format("Job name \"{0}\" all tasks have been canceled.", Configuration.Name));
                }
            }
        }

        //public void TryPause()
        //{
        //    throw new NotImplementedException();
        //}

        //public void UpdateConfiguration(JobConfiguration newConfig)
        //{
        //    throw new NotImplementedException();
        //}

        public bool HasRunningTask()
        {
            bool rc = false;

            if (_infos != null)
            {
                if (InfosCount() > 0)
                {
                    lock (_infos)
                    {
                        rc = _infos.Any(j => j.IsRunning);
                    }
                }
            }

            return rc;
        }

        public int NumberOfRunningTasks()
        {
            int rc = 0;

            if (_infos != null)
            {
                if (InfosCount() > 0)
                {
                    lock (_infos)
                    {
                        rc = _infos.Where(j => j.IsRunning).Count();
                    }
                }
            }

            return rc;
        }

        public abstract bool Execute(JobRunInfo<StatisticType> info);

        #endregion

        #region Private Methods

        private void LaunchNextJob(CancellationToken ct, bool runNow)
        {
            if (Configuration.RunState == JobRunState.Automatic || runNow)
            {
                var info = new JobRunInfo<StatisticType>();
                this.State.Statistics = info.Statistics;
                info.CancellationToken = ct;
                info.StatisticsUpdated += StatisticsUpdated;
                info.Task = new Task<bool>(() => Execute(info), ct);
                info.Statistics.JobID = Configuration.JobConfigurationId;
                info.Statistics.StartTime = runNow ? DateTime.UtcNow : CalculateNextStartTime();

                FireStatusUpdate();

                if (_infos != null)
                {
                    lock (_infos)
                    {
                        _infos.Add(info);
                    }
                }

                Task.Run(() => Run(ct, info)); //does not block here on purpose
                _logger.Log(string.Format("Job \"{0}\" has been setup to run.", Configuration.Name));
            }
        }

        private void StatisticsUpdated()
        {
            FireStatusUpdate();
        }

        //a self relaunching scheduler
        private async Task Run(CancellationToken ct, JobRunInfo<StatisticType> info)
        {
            if (info != null)
            {
                if (Status != JobStatus.Misconfigured)
                {
                    try
                    {
                        _logger.Log(string.Format(string.Format("Job \"{0}\" scheduled to start \"{1}\"", Configuration.Name, info.Statistics.StartTime.ToLocalTime())));
                        WaitTillDoneOrThrow(ct, info.Statistics.StartTime); //wait till it's time, while checking for cancel token

                        _logger.Log(string.Format(string.Format("Job \"{0}\" starting at \"{1}\"", Configuration.Name, DateTime.UtcNow.ToLocalTime())));

                        bool willRun = !HasRunningTask() || Configuration.AllowSimultaneousExecutions;

                        if (!willRun && Configuration.RunImmediatelyIfRunTimeMissed)
                        {
                            _logger.Log(string.Format("Job \"{0}\" missed scheduled execution window, will run immediately after currently executing job.", Configuration.Name), LogMessageSeverity.Warning);

                            Task<bool>[] tasks = null;

                            if (_infos != null)
                            {
                                lock (_infos)
                                {
                                    tasks = _infos.Where(j => j.IsRunning).Select(j => j.Task).ToArray();
                                }
                            }

                            if (tasks != null)
                            {
                                if (tasks.Count() > 0)
                                {
                                    Task.WaitAll(tasks); //blocks till all the tasks are done
                                }
                            }

                            willRun = true;
                        }

                        LaunchNextJob(ct, false); //launch the next job no matter what

                        if (willRun)
                        {
                            await RunTask(info); //actually run it!
                        }
                        else
                        {
                            _logger.Log(string.Format("Job \"{0}\" missed scheduled execution window.", Configuration.Name), LogMessageSeverity.Warning);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Log(string.Format("The scheduled job, \"{0}\", has been canceled prior to execution.", Configuration.Name), LogMessageSeverity.Warning);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(string.Format("The scheduled job, \"{0}\", has encountered an error prior to execution - {1}.", Configuration.Name, ex.Message), LogMessageSeverity.Error);
                    }
                }
                else
                {
                    _logger.Log(string.Format("Job \"{0}\" cannot run because it is misconfigured...", Configuration.Name), LogMessageSeverity.Error);
                }

                RemoveFromInfos(info); //need to remove here no matter what, success, failure, cancel, exception, missed window, etc
            }
        }

        private void WaitTillDoneOrThrow(CancellationToken ct, DateTime startTime)
        {
            var doneYet = new ManualResetEvent(false);

            //must wait in a cycle so we can check to make sure we are not being canceled
            while (!doneYet.WaitOne(_cancelWaitCycle))
            {
                ct.ThrowIfCancellationRequested();

                if (DateTime.UtcNow.ToLocalTime() >= startTime.ToLocalTime())
                {
                    doneYet.Set();
                }
            }
        }

        private void RemoveFromInfos(JobRunInfo<StatisticType> info)
        {
            if (info != null && _infos != null)
            {
                lock (_infos)
                {
                    if (_infos.Contains(info))
                    {
                        _infos.Remove(info);
                    }
                }
            }
        }

        private int InfosCount()
        {
            int rc = 0;

            if (_infos != null)
            {
                lock (_infos)
                {
                    rc = _infos.Count;
                }
            }

            return rc;
        }

        private async Task RunTask(JobRunInfo<StatisticType> info)
        {
            if (info != null && info.Task != null)
            {
                var watch = new Stopwatch();

                watch.Start();

                bool rc = false;

                try
                {
                    info.Task.Start();
                    State.Status = JobStatus.Running;
                    FireStatusUpdate();
                    info.IsRunning = true;

                    rc = await info.Task;
                }
                catch (OperationCanceledException)
                {
                    _logger.Log(string.Format("Job \"{0}\" canceled.", Configuration.Name), LogMessageSeverity.Warning);
                }
                catch (Exception ex)
                {
                    _logger.Log(string.Format("Job \"{0}\" failed! Error - {1}.", Configuration.Name, ex.Message), LogMessageSeverity.Error);
                }

                watch.Stop();

                info.Statistics.CompletedSuccessfully = rc;
                info.Statistics.Duration = watch.Elapsed;
                info.IsRunning = false;

                State.Status = rc ? JobStatus.Success : JobStatus.Error;

                if (JobCompleted!=null) JobCompleted(State);

                FireStatusUpdate();

                string message = rc ? "completed successfully" : "failed to complete successfully";

                _logger.Log(string.Format("Job \"{0}\" {1}, run time = {2:hh\\:mm\\:ss}", Configuration.Name, message, watch.Elapsed), rc ? LogMessageSeverity.Information : LogMessageSeverity.Error);
            }
        }

        private DateTime CalculateNextStartTime()
        {
            return DateTime.UtcNow.Add(CalculateNextRunWaitTime());
        }

        private TimeSpan CalculateNextRunWaitTime()
        {
            TimeSpan result = TimeSpan.MaxValue;

            //todo:  This should be one to many, loop through each schedule
            JobSchedule schedule = Configuration.Schedule;

            if (schedule.TriggerType != JobTriggerType.NotConfigured)
            {
                var secondsInDay = (int)Math.Floor(TimeSpan.FromDays(1).TotalSeconds);

                int seconds = schedule.StartTime.TimeInSeconds;

                if (schedule.StartTime.TimeInSeconds > secondsInDay)
                {
                    _logger.Log(string.Format("Job by the name of \"{0}\" start time set to more that one days worth of seconds, defaulting to start running at midnight.", Configuration.Name), LogMessageSeverity.Warning);
                    seconds = 0;
                }

                DateTime startTime = DateTime.Today.Add(TimeSpan.FromSeconds(seconds));

                TimeSpan offset = startTime.Subtract(DateTime.UtcNow.ToLocalTime());

                switch (schedule.TriggerType)
                {
                    case JobTriggerType.Continuously:
                        result = FindNextTriggerTimeSpanContinuously(schedule, offset);
                        break;

                    case JobTriggerType.Daily:
                        result = FindNextTriggerTimeSpanDaily(schedule, offset);
                        break;

                    case JobTriggerType.Weekly:
                        result = FindNextTriggerTimeSpanWeekly(schedule, offset, DateTime.UtcNow.ToLocalTime());
                        break;

                    case JobTriggerType.Monthly:
                        result = FindNextTriggerTimeSpanMonthly(schedule, offset);
                        break;

                    default:
                        _logger.Log(string.Format("Job by the name of \"{0}\" has a trigger type of \"{1}\" that is not supported.", Configuration.Name, schedule.TriggerType), LogMessageSeverity.Warning);
                        break;
                }
                if (result != TimeSpan.MaxValue)
                {
                    _logger.Log(string.Format("Job by the name of \"{0}\" scheduled to run in {1} day(s), {2} hour(s) {3} minute(s) and {4} second(s).", Configuration.Name, result.Days, result.Hours, result.Minutes, result.Seconds));
                }
            }
            else
            {
                State.Status = JobStatus.Misconfigured;
                FireStatusUpdate();

                _logger.Log(string.Format("Job by the name of \"{0}\" has a trigger type of \"{1}\" that is misconfigured.  This job will not run!", Configuration.Name, schedule.TriggerType), LogMessageSeverity.Critical);
            }

            return result;
        }

        private TimeSpan FindNextTriggerTimeSpanContinuously(JobSchedule schedule, TimeSpan startTimeOffset)
        {
            if (startTimeOffset.TotalSeconds < 0)
            {
                startTimeOffset = startTimeOffset.Add(TimeSpan.FromDays(1));
            }

            var result = startTimeOffset;

            if (schedule.RepeatEvery.Enabled)
            {
                var repeatSeconds = schedule.RepeatEvery.TimeInSeconds;

                if (schedule.RepeatEvery.TimeInSeconds < 1)
                {
                    _logger.Log(string.Format("Job by the name of \"{0}\" start time set to repeat faster than every 1 second, defaulting to run every 1 second.", Configuration.Name), LogMessageSeverity.Warning);
                    repeatSeconds = 1;
                }

                result = TimeSpan.FromSeconds(startTimeOffset.TotalSeconds % repeatSeconds); //this is the wait time till the next repeat
            }

            return result;
        }

        private TimeSpan FindNextTriggerTimeSpanDaily(JobSchedule schedule, TimeSpan startTimeOffset)
        {
            var result = startTimeOffset;

            if (schedule.TriggerDays != JobTriggerDays.NotConfigured)
            {
                var dayStart = (int)DateTime.UtcNow.ToLocalTime().DayOfWeek;

                //if the offset is negative, we missed todays start time.
                if (result.TotalSeconds < 0)
                {
                    result = result.Add(TimeSpan.FromDays(1));
                    dayStart++;
                }

                var dayToCheck = GetJobTriggerDays(dayStart);

                var day = dayStart;

                while (!schedule.TriggerDays.HasFlag(dayToCheck))
                {
                    result = result.Add(TimeSpan.FromDays(1));

                    day = day >= 6 ? 0 : day + 1;  //the days enum goes from 0 to 6, 0 being sunday 6 being saturday
                    dayToCheck = GetJobTriggerDays(day);

                    //safety
                    if (day == dayStart)
                    {
                        break;
                    }
                }
            }
            else
            {
                State.Status = JobStatus.Misconfigured;
                FireStatusUpdate();

                _logger.Log(string.Format("Job by the name of \"{0}\" has a trigger type of \"{1}\" that is misconfigured.  This job will not run!", Configuration.Name, schedule.TriggerType), LogMessageSeverity.Critical);
                result = TimeSpan.MaxValue;
            }

            return result;
        }

        private TimeSpan FindNextTriggerTimeSpanWeekly(JobSchedule schedule, TimeSpan startTimeOffset, DateTime startDate)
        {
            var result = startTimeOffset;

            if (schedule.TriggerWeeks != JobTriggerWeeks.NotConfigured && schedule.TriggerDays != JobTriggerDays.NotConfigured)
            {
                var now = startDate;
                var triggerWeek = GetJobTriggerWeeks(now);

                while (!TriggerWeeksCheck(schedule, now))
                {
                    result = result.Add(TimeSpan.FromDays(1));
                    now = now.AddDays(1);
                    triggerWeek = GetJobTriggerWeeks(now);
                }
            }
            else
            {
                State.Status = JobStatus.Misconfigured;
                FireStatusUpdate();
                _logger.Log(string.Format("Job by the name of \"{0}\" has a trigger type of \"{1}\" that is misconfigured.  This job will not run!", Configuration.Name, schedule.TriggerType), LogMessageSeverity.Critical);
                result = TimeSpan.MaxValue;
            }

            return result;
        }

        private TimeSpan FindNextTriggerTimeSpanMonthly(JobSchedule schedule, TimeSpan startTimeOffset)
        {
            var result = startTimeOffset;

            if (schedule.TriggerMonths != JobTriggerMonths.NotConfigured)
            {
                var now = DateTime.UtcNow.ToLocalTime();
                var triggerMonth = GetJobTriggerMonths(now);
                int numberOfDays = DateTime.DaysInMonth(now.Year, now.Month) - now.Day + 1;

                while (!schedule.TriggerMonths.HasFlag(triggerMonth))
                {
                    result = result.Add(TimeSpan.FromDays(numberOfDays));
                    now = now.AddDays(numberOfDays);
                    numberOfDays = DateTime.DaysInMonth(now.Year, now.Month);
                    triggerMonth = GetJobTriggerMonths(now);
                }

                result = FindNextTriggerTimeSpanWeekly(schedule, result, now);
            }
            else
            {
                State.Status = JobStatus.Misconfigured;
                FireStatusUpdate();

                _logger.Log(string.Format("Job by the name of \"{0}\" has a trigger type of \"{1}\" that is misconfigured.  This job will not run!", Configuration.Name, schedule.TriggerType), LogMessageSeverity.Critical);
                result = TimeSpan.MaxValue;
            }

            return result;
        }

        private bool TriggerWeeksCheck(JobSchedule schedule, DateTime timeToCheck)
        {
            var result = schedule.TriggerWeeks.HasFlag(GetJobTriggerWeeks(timeToCheck));

            if (schedule.TriggerWeeks.HasFlag(JobTriggerWeeks.Last) && !result) //only check if we do not already have a hit
            {
                result = IsLastWeekOfMonth(timeToCheck);

                if (!result) //check is this week has the last configured trigger day of the month
                {
                    result = HasLastTriggerDayOfMonth(schedule, timeToCheck);
                }
            }

            if (result) //only check if we have a hit
            {
                result = schedule.TriggerDays.HasFlag(GetJobTriggerDays((int)timeToCheck.DayOfWeek));
            }

            return result;
        }

        private bool HasLastTriggerDayOfMonth(JobSchedule schedule, DateTime timeToCheck)
        {
            int counter = 0;

            var timeToReallyCheck = timeToCheck;

            while (timeToReallyCheck.Month == timeToCheck.Month)
            {
                var triggerDay = GetJobTriggerDays((int)timeToReallyCheck.DayOfWeek);

                if (schedule.TriggerDays.HasFlag(triggerDay))
                {
                    counter++;
                }

                timeToReallyCheck = timeToReallyCheck.AddDays(1);
            }

            return counter == 1;
        }

        private JobTriggerMonths GetJobTriggerMonths(DateTime toConvert)
        {
            return (JobTriggerMonths)Math.Pow(2, toConvert.Month - 1);
        }

        private JobTriggerWeeks GetJobTriggerWeeks(DateTime toConvert)
        {
            return (JobTriggerWeeks)Math.Pow(2, GetWeekOfMonth(toConvert, 1) - 1);
        }

        private bool IsLastWeekOfMonth(DateTime time)
        {
            int lastDayOfMonth = DateTime.DaysInMonth(time.Year, time.Month);
            return GetWeekOfYear(time) == GetWeekOfYear(new DateTime(time.Year, time.Month, lastDayOfMonth));
        }

        private int GetWeekOfMonth(DateTime time, int dayNumber)
        {
            var dayToCheck = new DateTime(time.Year, time.Month, dayNumber);

            return GetWeekOfYear(time) - GetWeekOfYear(dayToCheck) + 1;
        }

        private int GetWeekOfYear(DateTime time)
        {
            var culture = CultureInfo.CurrentCulture;

            return culture.Calendar.GetWeekOfYear(time, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        }

        private JobTriggerDays GetJobTriggerDays(int day)
        {
            JobTriggerDays jobDay = JobTriggerDays.NotConfigured;

            if (day < 7) //our flag goes from 1 to 7, 1 being sunday and 7 being saturday
            {
                jobDay = (JobTriggerDays)Math.Pow(2, day); //convert to a flag value (e.g. 2^x power)
            }

            return jobDay;
        }

        private void FireStatusUpdate()
        {

        }

        #endregion
    }
}
