using Core.Models;
using Core.Models.ComplexTypes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Interfaces
{
    public abstract class JobConfiguration : ICloneable<JobConfiguration>
    {
        [Key]
        [PropertyEditorMetadata(Hidden = true)]
        public int JobConfigurationId { get; set; }

        [StringLength(255)]
        public string Name { get; set; }

        public bool RunImmediatelyIfRunTimeMissed { get; set; }

        [PropertyEditorMetadata(Hidden = true)]
        public long TimeoutTicks { get; set; }
        [NotMapped]
        public TimeSpan Timeout
        {
            get { return TimeSpan.FromTicks(TimeoutTicks); }
            set { TimeoutTicks = value.Ticks; }
        }

        public JobRunState RunState { get; set; }

        [PropertyEditorMetadata(Hidden = true)]
        public JobSchedule Schedule { get; set; }

        [PropertyEditorMetadata(Hidden = true)]
        public AuditInfo AuditInfo { get; set; }

        public JobConfiguration()
        {
            Timeout = TimeSpan.FromDays(1);
            //initialize objects
            AuditInfo = new AuditInfo();
            Schedule = new JobSchedule();
        }

        public JobConfiguration Clone()
        {
            return (JobConfiguration)this.MemberwiseClone();
        }
    }

    public enum JobRunState
    {
        Disabled,
        Manual,
        Automatic
    }

    public enum JobTriggerType
    {
        NotConfigured,
        Continuously,
        Daily,
        Weekly,
        Monthly
    }

    [Flags]
    public enum JobTriggerDays
    {
        NotConfigured = 0,
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64,
        All = 127
    }

    [Flags]
    public enum JobTriggerWeeks
    {
        NotConfigured = 0,
        First = 1,
        Second = 2,
        Third = 4,
        Fourth = 8,
        Last = 16,
        All = 31
    }

    [Flags]
    public enum JobTriggerMonths
    {
        NotConfigured = 0,
        January = 1,
        February = 2,
        March = 4,
        April = 8,
        May = 16,
        June = 32,
        July = 64,
        August = 128,
        September = 256,
        October = 512,
        November = 1024,
        December = 2048,
        All = 4095
    }
}
