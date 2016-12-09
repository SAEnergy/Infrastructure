using Core.Interfaces.Base;
using Core.Interfaces.Components.Base;
using Core.Interfaces.ServiceContracts;
using Core.Models.Persistent;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Scheduler.Interfaces
{
    [ServiceContract]
    public interface ISchedulerCallback
    {
        [OperationContract(IsOneWay = true)]
        void JobAdded(JobConfiguration job);

        [OperationContract(IsOneWay = true)]
        void JobUpdated(JobConfiguration job);

        [OperationContract(IsOneWay = true)]
        void JobDeleted(JobConfiguration job);

        [OperationContract(IsOneWay = true)]
        void JobStateUpdated(JobState state);
    }

    [ServiceContract]
    public interface ISchedulerBase
    {
        [OperationContract]
        List<JobConfiguration> GetJobs();

        [OperationContract]
        List<JobState> GetStates();

        [OperationContract]
        void AddJob(JobConfiguration job);

        [OperationContract]
        void DeleteJob(JobConfiguration job);

        [OperationContract]
        void UpdateJob(JobConfiguration job);

        [OperationContract]
        void RunJob(JobConfiguration job);

        [OperationContract]
        void CancelJob(JobConfiguration job);
    }

    [ServiceContract(CallbackContract = typeof(ISchedulerCallback))]
    public interface ISchedulerHost : ISchedulerBase, IUserAuthentication
    {
    }

    public interface ISchedulerComponent : ISchedulerBase, IRunnable, IComponent
    {
        event JobStateEventHandler StateUpdated;
    }

}
