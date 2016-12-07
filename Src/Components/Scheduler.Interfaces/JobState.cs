using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{
    public class JobState
    {
        public int JobId { get; set; }

        public JobStatus Status { get; set; }

        public JobStatistics Statistics { get; set; }
    }
}
