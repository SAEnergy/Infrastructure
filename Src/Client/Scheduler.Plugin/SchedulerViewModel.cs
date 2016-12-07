using Client.Base;
using Client.Controls;
using Client.Controls.Dialogs;
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

        private ObservableCollection<SchedulerJobModel> _jobs { get; set; }
        public MultiSelectCollectionView<SchedulerJobModel> Jobs { get; set; }

        public SimpleCommand AddJobCommand { get; private set; }

        public SchedulerViewModel(ViewBase parent) : base(parent)
        {
            _jobs = new ObservableCollection<SchedulerJobModel>();
            Jobs = new MultiSelectCollectionView<SchedulerJobModel>(_jobs);
            AddJobCommand = new SimpleCommand(OnAddJobCommand);
        }

        private void OnAddJobCommand()
        {
            SchedulerAddJobDialog dlg = new SchedulerAddJobDialog(_jobTypes, Window.GetWindow(this));
            if (dlg.ShowDialog() == true)
            {
                JobConfiguration job = dlg.Job;
                Execute(() =>
                {
                    Channel.AddJob(job);
                });
            }
        }

        public void DeleteSelectedJobs()
        {
            foreach (var iter in Jobs.SelectedItems)
            {
                var job = iter.Job;
                Execute(() => Channel.DeleteJob(job));
            }
        }

        public void EditSelectedJobs()
        {
            PropertyGridDialog dlg = new PropertyGridDialog(Window.GetWindow(this));
            dlg.DataContext = Jobs.SelectedItems.Select(s => s.Job.Clone()).ToList();
            if (dlg.ShowDialog() == true)
            {
                foreach(var iter in dlg.DataContext as IEnumerable<JobConfiguration>)
                {
                    JobConfiguration job = iter;
                    Execute(() => Channel.UpdateJob(job));
                }
            }
        }

        public void EditSelectedSchedules()
        {
            PropertyGridDialog dlg = new PropertyGridDialog(Window.GetWindow(this));
            dlg.DataContext = Jobs.SelectedItems.Select(s => s.Job.Schedule.Clone()).ToList();
            if (dlg.ShowDialog()== true)
            {
                //foreach (var iter in dlg.DataContext as IEnumerable<JobSchedule>)
                //{
                //    JobConfiguration job = _jobs.Select(j=>j.Job).Where(j=>j.JobConfigurationId)==iter.jo
                //    Execute(() => Channel.UpdateJob(job));
                //}
            }
        }

        protected override void OnConnect(ISubscription source)
        {
            base.OnConnect(source);

            if (_jobTypes.Count == 0)
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
            BeginInvoke(() => _jobs.Clear());
        }

        public void JobAdded(JobConfiguration job)
        {
            SchedulerJobModel model = new SchedulerJobModel(this);
            model.Job = job;
            BeginInvoke(() => _jobs.Add(model));
        }

        public void JobDeleted(JobConfiguration job)
        {
            this.BeginInvoke(() =>
            {
                SchedulerJobModel model = _jobs.FirstOrDefault(j => j.Job.JobConfigurationId == job.JobConfigurationId);
                if (model==null) { return; }
                _jobs.Remove(model);
            });
        }

        public void JobStateUpdated(JobState state)
        {
            this.BeginInvoke(() =>
            {
                SchedulerJobModel model = _jobs.FirstOrDefault(j => j.Job.JobConfigurationId == state.JobId);
                if (model!=null) { model.State = state; }
            });
        }
    }
}
