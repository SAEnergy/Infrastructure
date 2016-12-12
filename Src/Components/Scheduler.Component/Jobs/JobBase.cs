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
using Core.IoC.Container;

namespace Scheduler.Component.Jobs
{

    public delegate bool ExecuteHandler();

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

        private TimeSpan _cancelPrintWaitCycle = TimeSpan.FromMilliseconds(5000);
        private Thread _schedulerThread;
        private Thread _taskThread;
        private CancellationTokenSource _taskCancelSource;
        private ManualResetEvent _scheduleResetEvent;
        private bool _isRunning;
        private bool _runImmediately = false;
        private TimeSpan _lastRunDuration;
        private DateTime _nextRunTime;

        protected readonly ILogger _logger;

        public StatisticType Statistics { get; private set; }

        public CancellationToken TaskCancellationToken { get; set; }

        private JobStatus _status;
        public JobStatus Status
        {
            get { return _status; }
            private set
            {
                _status = value;
                FireStatusUpdate();
            }
        }

        public event JobStateEventHandler StateUpdated;
        public event JobStatisticsEventHandler JobCompleted;

        #endregion

        #region Properties

        private ConfigType _configuration;
        public ConfigType Configuration
        {
            get { return _configuration; }
            set
            {
                _configuration = value;
                // recalculate schedule
                if (Status == JobStatus.Misconfigured) { Status = JobStatus.Unknown; }
                _scheduleResetEvent.Set();
            }
        }

        JobConfiguration IJob.Configuration
        {
            get { return Configuration; }
            set { Configuration = (ConfigType)value; }
        }


        #endregion

        #region Constructor

        protected JobBase(ILogger logger, ConfigType config)
        {
            _logger = logger;
            _scheduleResetEvent = new ManualResetEvent(false);
            Configuration = config;
            Status = JobStatus.Unknown;
            _logger.Log(string.Format("Job name \"{0}\" created of type \"{1}\".", config.Name, GetType()));
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _logger.Log(string.Format("Job name \"{0}\" starting.", Configuration.Name));

                _schedulerThread = new Thread(SchedulerThread);
                _schedulerThread.Start();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_schedulerThread != null)
            {
                _scheduleResetEvent.Set();
                while (_schedulerThread != null)
                {
                    Thread.Sleep(10);
                }
            }
            TryCancel();
            _logger.Log(string.Format("Job name \"{0}\" has been stopped.", Configuration.Name));
        }

        private void SchedulerThread()
        {
            try
            {
                ISchedulerComponent sched = IoCContainer.Instance.Resolve<ISchedulerComponent>();
                Statistics = (StatisticType)sched.GetLatestStatistics(Configuration);
                _lastRunDuration = Statistics.Duration;
            }
            catch { }

            while (_isRunning)
            {
                _scheduleResetEvent.Reset();

                _scheduleResetEvent.WaitOne(1000);

                if (Status == JobStatus.Misconfigured) { continue; }

                Status = (_taskThread == null) ? JobStatus.Idle : JobStatus.Running;

                if (Configuration.RunState == JobRunState.Automatic)
                {
                    DateTime startTime = DateTime.MaxValue;

                    try
                    {
                        startTime = CalculateNextStartTime();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log("Error calculating next run time: "+ex.Message, ex, severity: LogMessageSeverity.Error);
                    }

                    if (startTime == DateTime.MaxValue)
                    {
                        Status = JobStatus.Misconfigured;
                        _logger.Log(string.Format("Job by the name of \"{0}\" has a trigger type of \"{1}\" that is misconfigured.  This job will not run!", Configuration.Name, Configuration.Schedule.TriggerType), severity: LogMessageSeverity.Critical);
                        continue;
                    }
                    _nextRunTime = startTime;

                    _logger.Log(string.Format(string.Format("Job \"{0}\" scheduled to start \"{1}\"", Configuration.Name, startTime.ToLocalTime())));

                    //must wait in a cycle so we can check to make sure we are not being canceled
                    while (DateTime.UtcNow.ToLocalTime() < startTime.ToLocalTime())
                    {
                        if (_scheduleResetEvent.WaitOne(1000))
                        {
                            break;
                        }
                    }

                    if (_scheduleResetEvent.WaitOne(0))
                    {
                        _logger.Log(string.Format("The scheduled job, \"{0}\", has been canceled prior to execution.", Configuration.Name), severity: LogMessageSeverity.Warning);
                        continue;
                    }

                    if (_taskThread != null)
                    {
                        if (Configuration.RunImmediatelyIfRunTimeMissed)
                        {
                            _logger.Log(string.Format("Job \"{0}\" missed scheduled execution window, will run immediately after task completion.", Configuration.Name), severity: LogMessageSeverity.Warning);
                            _runImmediately = true;
                        }
                        else
                        {
                            _logger.Log(string.Format("Job \"{0}\" missed scheduled execution window.", Configuration.Name), severity: LogMessageSeverity.Warning);
                        }
                        continue;
                    }

                    _logger.Log(string.Format(string.Format("Job \"{0}\" starting at \"{1}\"", Configuration.Name, DateTime.UtcNow.ToLocalTime())));

                    _taskThread = new Thread(TaskThread);
                    _taskThread.Start();
                }
            }
            _schedulerThread = null;
        }

        public void ForceRun()
        {
            if (Configuration.RunState == JobRunState.Disabled) { throw new InvalidOperationException(string.Format("Cannot force run disabled job by the name of \"{0}\".", Configuration.Name)); }

            if (_taskThread != null) { throw new InvalidOperationException(string.Format("Job \"{0}\" is already running.", Configuration.Name)); }

            _taskThread = new Thread(TaskThread);
            _taskThread.Start();
        }

        public void TryCancel()
        {
            if (_taskThread != null && _taskCancelSource != null)
            {
                _runImmediately = false;
                Status = JobStatus.Cancelling;
                _logger.Log(string.Format("Job name \"{0}\" canceling currently executing task.  ", Configuration.Name), severity: LogMessageSeverity.Warning);
                _taskCancelSource.Cancel();

                TimeSpan totalWait = TimeSpan.Zero;
                TimeSpan incrementWait = TimeSpan.FromMilliseconds(10);
                while (_taskThread != null)
                {
                    Thread.Sleep(incrementWait);
                    totalWait = totalWait.Add(incrementWait);
                    if (totalWait.TotalMilliseconds > _cancelPrintWaitCycle.TotalMilliseconds)
                    {
                        totalWait = TimeSpan.Zero;
                        _logger.Log(string.Format("Job name \"{0}\" waiting on task to cancel...", Configuration.Name), severity: LogMessageSeverity.Warning);
                    }
                }
                _logger.Log(string.Format("Job name \"{0}\" has been canceled.", Configuration.Name));
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

        public bool IsRunning()
        {
            return _isRunning;
        }

        public abstract bool Execute();

        #endregion

        #region Private Methods

        private void StatisticsUpdated()
        {
            FireStatusUpdate();
        }

        private void TaskThread()
        {
            try
            {
                _taskCancelSource = new CancellationTokenSource();
                TaskCancellationToken = _taskCancelSource.Token;

                var watch = Stopwatch.StartNew();

                bool rc = false;

                try
                {
                    Statistics = new StatisticType();
                    Statistics.JobID = Configuration.JobConfigurationId;
                    Statistics.StartTime = DateTime.Now;

                    Status = JobStatus.Running;

                    rc = Execute();
                }
                catch (OperationCanceledException)
                {
                    _logger.Log(string.Format("Job \"{0}\" canceled.", Configuration.Name), severity: LogMessageSeverity.Warning);
                }
                catch (Exception ex)
                {
                    _logger.Log(string.Format("Job \"{0}\" failed! Error - {1}.", Configuration.Name, ex.Message), ex, severity: LogMessageSeverity.Error);
                }

                watch.Stop();

                Statistics.CompletedSuccessfully = rc;
                Statistics.Duration = watch.Elapsed;
                _lastRunDuration = watch.Elapsed;

                if (JobCompleted != null) JobCompleted(Statistics);

                string message = rc ? "completed successfully" : "failed to complete successfully";

                LogMessageSeverity sev = rc ? LogMessageSeverity.Information : LogMessageSeverity.Error;
                _logger.Log(string.Format("Job \"{0}\" {1}, run time = {2:hh\\:mm\\:ss}", Configuration.Name, message, watch.Elapsed), severity: sev);

                Status = JobStatus.Idle;

                if (_runImmediately)
                {
                    _runImmediately = false;
                    TaskThread();
                }
            }
            finally
            {
                _taskThread = null;
            }
        }

        private DateTime CalculateNextStartTime()
        {
            DateTime runTime = DateTime.UtcNow.Add(CalculateNextRunWaitTime());
            // round to nearest second
            return DateTime.MinValue.AddSeconds(Math.Round((runTime - DateTime.MinValue).TotalSeconds));
        }

        private TimeSpan CalculateNextRunWaitTime()
        {
            TimeSpan result = TimeSpan.MaxValue;

            //todo:  This should be one to many, loop through each schedule
            JobSchedule schedule = Configuration.Schedule;

            if (schedule.TriggerType != JobTriggerType.NotConfigured)
            {
                var secondsInDay = (int)Math.Floor(TimeSpan.FromDays(1).TotalSeconds);

                if (schedule.StartTime.TotalSeconds > secondsInDay)
                {
                    _logger.Log(string.Format("Job by the name of \"{0}\" start time set to more that one days worth of seconds, defaulting to start running at midnight.", Configuration.Name), severity: LogMessageSeverity.Warning);
                }

                DateTime startTime = DateTime.Today.Add(schedule.StartTime);

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
                        _logger.Log(string.Format("Job by the name of \"{0}\" has a trigger type of \"{1}\" that is not supported.", Configuration.Name, schedule.TriggerType), severity: LogMessageSeverity.Warning);
                        break;
                }
                if (result != TimeSpan.MaxValue)
                {
                    _logger.Log(string.Format("Job by the name of \"{0}\" scheduled to run in {1} day(s), {2} hour(s) {3} minute(s) and {4} second(s).", Configuration.Name, result.Days, result.Hours, result.Minutes, result.Seconds));
                }
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

            if (schedule.RepeatEvery != TimeSpan.Zero)
            {
                var repeatSeconds = schedule.RepeatEvery.TotalSeconds;

                if (repeatSeconds < 1)
                {
                    _logger.Log(string.Format("Job by the name of \"{0}\" start time set to repeat faster than every 1 second, defaulting to run every 1 second.", Configuration.Name), severity: LogMessageSeverity.Warning);
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

        public JobState State
        {
            get
            {
                JobState state = new JobState();
                state.JobId = Configuration.JobConfigurationId;
                state.Status = Status;
                state.LastRunDuration = _lastRunDuration;
                state.NextRunTime = _nextRunTime.ToLocalTime();
                state.Statistics = Statistics;
                return state;
            }
        }

        public void FireStatusUpdate()
        {
            if (StateUpdated != null)
            {
                StateUpdated(State);
            }
        }

        #endregion
    }
}
