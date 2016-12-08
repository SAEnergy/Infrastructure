using Client.Base;
using Client.Controls;
using Client.Controls.Dialogs;
using Core.Comm;
using Core.Util;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            Jobs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
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
            List<JobConfiguration> jobs = Jobs.SelectedItems.Select(s => s.Job.Clone()).ToList();
            foreach (JobConfiguration job in jobs)
            {
                job.Schedule = Jobs.SelectedItems.FirstOrDefault(j => j.Job.JobConfigurationId == job.JobConfigurationId).Job.Schedule.Clone();
            }
            PropertyGridDialog dlg = new PropertyGridDialog(Window.GetWindow(this));
            dlg.DataContext = jobs.Select(s=>s.Schedule);
            if (dlg.ShowDialog()== true)
            {
                foreach (var iter in dlg.DataContext as IEnumerable<JobSchedule>)
                {
                    JobConfiguration job = jobs.FirstOrDefault(j=>j.Schedule==iter);
                    Execute(() => Channel.UpdateJob(job));
                }
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
            if (jobs == null || jobs.Count == 0) return;
            BeginInvoke(() =>
            {
                foreach (var iter in jobs)
                {
                    JobConfiguration job = iter;
                    JobAdded(job);
                }
            });

            List<JobState> states = Channel.GetStates();
            if (states == null || states.Count == 0) return;
            BeginInvoke(() =>
            {
                foreach (var iter in states)
                {
                    JobState state = iter;
                    JobStateUpdated(state);
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

        public void JobUpdated(JobConfiguration job)
        {
            this.BeginInvoke(() =>
            {
                SchedulerJobModel model = _jobs.FirstOrDefault(j => j.Job.JobConfigurationId == job.JobConfigurationId);
                if (model != null) { model.Job = job; }
            });
        }
    }
}
