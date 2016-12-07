using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{
    public class JobStatistics
    {

        [Key]
        public int ID { get; set; }

        public int JobID { get; set; }

        public JobConfiguration Job { get; set; }

        public DateTime StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public bool CompletedSuccessfully { get; set; }

        public int TotalItems { get; set; }

        public int Completed { get; set; }

        public int Errors { get; set; }
    }
}
