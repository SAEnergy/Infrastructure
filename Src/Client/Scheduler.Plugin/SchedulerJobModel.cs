using Client.Base;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Plugin
{
    public class SchedulerJobModel : ModelBase
    {

        private SchedulerViewModel _parent;
        public SimpleCommand EditJobCommand { get; private set; }
        public SimpleCommand EditScheduleCommand { get; private set; }
        public SimpleCommand DeleteJobCommand { get; private set; }
        public SimpleCommand RunJobCommand { get; private set; }
        public SimpleCommand CancelJobCommand { get; private set; }

        public string Type {  get { return Job.GetType().Name; } }

        private JobConfiguration _job;
        public JobConfiguration Job
        {
            get { return _job; }
            set { _job = value; NotifyChanged("Job"); NotifyChanged("Type"); }
        }

        private JobState _state;
        public JobState State
        {
            get { return _state; }
            set { _state = value; NotifyChanged("State"); }
        }

        public SchedulerJobModel(SchedulerViewModel parent)
        {
            _parent = parent;
            EditJobCommand = new SimpleCommand(OnEditJobCommand);
            EditScheduleCommand = new SimpleCommand(OnEditScheduleCommand);
            DeleteJobCommand = new SimpleCommand(OnDeleteJobCommand);
            RunJobCommand = new SimpleCommand(OnRunJobCommand);
            CancelJobCommand = new SimpleCommand(OnCancelJobCommand);
        }

        private void OnCancelJobCommand()
        {
            _parent.CancelSelectedJobs();
        }

        private void OnRunJobCommand()
        {
            _parent.RunSelectedJobs();
        }

        private void OnDeleteJobCommand()
        {
            _parent.DeleteSelectedJobs();
        }

        private void OnEditScheduleCommand()
        {
            _parent.EditSelectedSchedules();
        }

        private void OnEditJobCommand()
        {
            _parent.EditSelectedJobs();
        }
    }
}
