using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{
    public enum JobStatus
    {
        Unknown,
        Misconfigured,
        Paused,
        Running,
        Success,
        Error,
        Cancelling,
        Cancelled
    }
}
