using Core.Interfaces.Base;
using Core.Interfaces.Components.Base;
using Core.Models.Persistent;
using System;
using System.Collections.Generic;

namespace Scheduler.Interfaces
{
    public interface IScheduler : IRunnable, IComponent
    {
        List<JobConfiguration> GetJobs();

        void AddJob(JobConfiguration job);

        bool DeleteJob(JobConfiguration job);

        bool UpdateJob(JobConfiguration job);
    }
}
