using Core.Comm.BaseClasses;
using Core.Interfaces.Components;
using Core.IoC.Container;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Component
{
    public class SchedulerHost : ServiceHostBase<ISchedulerHost, ISchedulerCallback>, ISchedulerHost
    {
        private ISchedulerComponent _scheduler;
        private IDataComponent _data;

        public SchedulerHost()
        {
            _scheduler = IoCContainer.Instance.Resolve<ISchedulerComponent>();
            _scheduler.StateUpdated += SchedulerStateUpdated;
            _data = IoCContainer.Instance.Resolve<IDataComponent>();
        }

        private void SchedulerStateUpdated(JobState state)
        {
            this.Broadcast(s => s.JobStateUpdated(state));
        }

        public void AddJob(JobConfiguration job)
        {
            _scheduler.AddJob(job);
            if (_data.Insert<JobConfiguration>(job))
            {
                this.Broadcast(c => c.JobAdded(job));
            }
        }

        public void DeleteJob(JobConfiguration job)
        {
            _scheduler.DeleteJob(job);
            if (_data.Delete<JobConfiguration>(job))
            {
                this.Broadcast(j => j.JobDeleted(job));
            }
        }

        public List<JobConfiguration> GetJobs()
        {
            return _scheduler.GetJobs();
        }

        public void UpdateJob(JobConfiguration job)
        {
            _scheduler.UpdateJob(job);
            if (_data.Update<JobConfiguration>(job))
            {
                this.Broadcast(j => j.JobUpdated(job));
            }
        }
    }
}
