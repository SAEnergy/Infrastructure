using Core.Models;
using Core.Models.ComplexTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{
    public class JobSchedule : ICloneable<JobSchedule>
    {
        [PropertyEditorMetadata(Hidden = true)]
        public long StartTimeTicks { get; set; }
        [NotMapped]
        public TimeSpan StartTime
        {
            get { return TimeSpan.FromTicks(StartTimeTicks); }
            set { StartTimeTicks = value.Ticks; }
        }

        public JobTriggerType TriggerType { get; set; }

        public JobTriggerDays TriggerDays { get; set; }

        public JobTriggerWeeks TriggerWeeks { get; set; }

        public JobTriggerMonths TriggerMonths { get; set; }

        //[PropertyEditorMetadata(Hidden = true)]
        //public long? RepeatEveryTicks { get; set; }
        //[NotMapped]
        //public TimeSpan? RepeatEvery
        //{
        //    get { return RepeatEveryTicks.HasValue ? TimeSpan.FromTicks(RepeatEveryTicks.Value) : (TimeSpan?)null; }
        //    set { RepeatEveryTicks = (value.HasValue) ? (value.Value.Ticks) : (long?)null; }
        //}
        [PropertyEditorMetadata(Hidden = true)]
        public long RepeatEveryTicks { get; set; }
        [NotMapped]
        public TimeSpan RepeatEvery
        {
            get { return TimeSpan.FromTicks(RepeatEveryTicks); }
            set { RepeatEveryTicks = value.Ticks; }
        }

        public JobSchedule()
        {
        }

        public JobSchedule Clone()
        {
            return (JobSchedule)this.MemberwiseClone();
        }
    }
}
