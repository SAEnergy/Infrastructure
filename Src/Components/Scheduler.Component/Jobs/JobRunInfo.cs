using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scheduler.Component.Jobs
{
    public class JobRunInfo<T> where T : JobStatistics, new()
    {
        public Task<bool> Task { get; set; }

        public bool IsRunning { get; set; }

        public T Statistics { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public event Action StatisticsUpdated;

        public JobRunInfo()
        {
            Statistics = new T();
        }
        public void FireStatisticsUpdated()
        {
            if (StatisticsUpdated != null) { StatisticsUpdated(); }
        }
    }
}
