using System.Threading;
using Core.Models.Persistent;
using Scheduler.Component.Jobs;
using Core.Interfaces.Components.Logging;
using System.Collections.Generic;
using Scheduler.Interfaces;

namespace Test.Plugins.Mocks
{
    public class UnitTestJobConfiugration : RunProgramJobConfiguration
    {

    }

    public class UnitTestJob : JobBase<UnitTestJobConfiugration>
    {
        public static List<UnitTestJob> Instances { get; private set; }

        public delegate void JobExecuteHandler();

        public event JobExecuteHandler JobExecuting;

        public UnitTestJob(ILogger logger, UnitTestJobConfiugration config) : base(logger, config)
        {
            if(Instances == null)
            {
                Instances = new List<UnitTestJob>();
            }

            Instances.Add(this);
        }

        public override bool Execute(JobRunInfo<JobStatistics> info)
        {
            if(JobExecuting != null)
            {
                JobExecuting();
            }

            return true;
        }
    }
}
