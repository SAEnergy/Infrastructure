using Core.Models.Persistent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{

    public delegate void JobStatisticsEventHandler(JobStatistics stats);
    public delegate void JobStateEventHandler(JobState state);

    public interface IJob
    {
        JobStatus Status { get; }

        JobState State { get; }

        JobConfiguration Configuration { get; set; }

        event JobStateEventHandler StateUpdated;

        event JobStatisticsEventHandler JobCompleted;

        void ForceRun();

        void TryCancel();

        void Start();

        void Stop();

        //void TryPause();

        //void UpdateConfiguration(JobConfiguration newConfig);
    }


    public interface IJob<T> : IJob where T : JobConfiguration
    {
        new T Configuration { get; set; }
    }

}
