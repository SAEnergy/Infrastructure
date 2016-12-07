using Core.Models.Persistent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{

    public delegate void EventHandlerJobStatistics(JobStatistics stats);

    public interface IJob
    {
        JobState State { get; }

        JobConfiguration Configuration { get; set; }

        event JobStateEventHandler StateUpdated;
        event JobStateEventHandler JobCompleted;

        void ForceRun();

        void TryCancel();

        void Start();

        //void TryPause();

        //void UpdateConfiguration(JobConfiguration newConfig);
    }


    public interface IJob<T> : IJob where T : JobConfiguration
    {
        new T Configuration { get; set; }
    }

}
