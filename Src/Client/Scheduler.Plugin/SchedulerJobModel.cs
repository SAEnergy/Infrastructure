using Client.Base;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Plugin
{
    public class SchedulerJobModel
    {

        private SchedulerViewModel _parent;
        public SimpleCommand EditJobCommand { get; private set; }
        public SimpleCommand EditScheduleCommand { get; private set; }
        public SimpleCommand DeleteJobCommand { get; private set; }
        public JobConfiguration Job { get; set; }
        public JobStatistics Statistics { get; set; }

        public SchedulerJobModel(SchedulerViewModel parent)
        {
            _parent = parent;
            EditJobCommand = new SimpleCommand(OnEditJobCommand);
            EditScheduleCommand = new SimpleCommand(OnEditScheduleCommand);
            DeleteJobCommand = new SimpleCommand(OnDeleteJobCommand);
        }

        private void OnDeleteJobCommand()
        {
            _parent.DeleteSelectedJobs();
        }

        private void OnEditScheduleCommand()
        {
            throw new NotImplementedException();
        }

        private void OnEditJobCommand()
        {
            throw new NotImplementedException();
        }
    }
}
