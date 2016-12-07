using Core.Models;
using Core.Models.ComplexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{
    public class JobSchedule : ICloneable<JobSchedule>
    {
        public TimeSpanBool StartTime { get; set; }

        public JobTriggerType TriggerType { get; set; }

        public JobTriggerDays TriggerDays { get; set; }

        public JobTriggerWeeks TriggerWeeks { get; set; }

        public JobTriggerMonths TriggerMonths { get; set; }

        public TimeSpanBool RepeatEvery { get; set; }

        public JobSchedule()
        {
            StartTime = new TimeSpanBool();
            RepeatEvery = new TimeSpanBool();
        }

        public JobSchedule Clone()
        {
            return (JobSchedule)this.MemberwiseClone();
        }
    }
}
