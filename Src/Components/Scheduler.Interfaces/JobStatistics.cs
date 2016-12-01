using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{
    public class JobStatistics
    {
        public DateTime StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public bool CompletedSuccessfully { get; set; }

        public int TotalItems { get; set; }

        public int Errors { get; set; }
    }
}
