using Client.Base;
using Core.Comm;
using Core.Util;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Scheduler.Plugin
{
    public class SchedulerViewModel : ViewModelBase<ISchedulerHost>, ISchedulerCallback
    {
        private static List<Type> _jobTypes = new List<Type>();

        public ObservableCollection<JobConfiguration> Jobs { get; set; }

        public SimpleCommand AddJobCommand { get; private set; }

        public SchedulerViewModel(ViewBase parent) : base(parent)
        {
            Jobs = new ObservableCollection<JobConfiguration>();
            AddJobCommand = new SimpleCommand(OnAddJobCommand);
        }

        private void OnAddJobCommand()
        {
            SchedulerAddJobDialog dlg = new SchedulerAddJobDialog(_jobTypes,Window.GetWindow(this));
            dlg.ShowDialog();

            //Execute(() =>
            //{
            //    RunProgramJobConfiguration job = new RunProgramJobConfiguration();
            //    job.Name = "Hardcoded";
            //    Channel.AddJob(job);
            //});
        }

        protected override void OnConnect(ISubscription source)
        {
            base.OnConnect(source);

            if (_jobTypes.Count==0)
            {
                IEnumerable<Type> types = TypeLocator.FindTypes("*.dll", typeof(JobConfiguration));
                BeginInvoke(() =>
                {
                    foreach (Type t in types)
                    {
                        _jobTypes.Add(t);
                    }
                });
            }

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
            BeginInvoke(()=>Jobs.Clear());
        }

        public void JobAdded(JobConfiguration job)
        {
            BeginInvoke(() => Jobs.Add(job));
        }

        public void JobStateUpdated()
        {
        }

        public void JobStatisticsUpdated(JobStatistics stats)
        {
        }
    }
}
