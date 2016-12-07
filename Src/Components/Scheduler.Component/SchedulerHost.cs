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
            _data = IoCContainer.Instance.Resolve<IDataComponent>();
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
            throw new NotImplementedException();
        }
    }
}
