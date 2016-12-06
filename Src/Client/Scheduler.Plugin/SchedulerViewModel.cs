using Client.Base;
using Core.Comm;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Plugin
{
    public class SchedulerViewModel : ViewModelBase<ISchedulerHost>, ISchedulerCallback
    {
        public ObservableCollection<JobConfiguration> Jobs { get; set; }

        public SimpleCommand AddJobCommand { get; private set; }

        public SchedulerViewModel(ViewBase parent) : base(parent)
        {
            Jobs = new ObservableCollection<JobConfiguration>();

            AddJobCommand = new SimpleCommand(OnAddJobCommand);
        }

        private void OnAddJobCommand()
        {
            Execute(() =>
            {
                RunProgramJobConfiguration job = new RunProgramJobConfiguration();
                job.Name = "Hardcoded";
                Channel.AddJob(job);
            });
        }

        protected override void OnConnect(ISubscription source)
        {
            base.OnConnect(source);
            List<JobConfiguration> jobs = Channel.GetJobs();
            BeginInvoke(() =>
            {
                foreach (var iter in jobs)
                {
                    JobConfiguration job = iter;
                    JobAdded(job);
                }
            });
        }

        protected override void OnDisconnect(ISubscription source, Exception error)
        {
            base.OnDisconnect(source, error);
        }

        public void JobAdded(JobConfiguration job)
        {
            BeginInvoke(() => Jobs.Add(job));
        }
    }
}
