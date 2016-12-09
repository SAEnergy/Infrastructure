using Core.Comm.BaseClasses;
using Core.Interfaces.Components;
using Core.IoC.Container;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scheduler.Component
{
    public class SchedulerHost : ServiceHostBase<ISchedulerHost, ISchedulerCallback>, ISchedulerHost
    {
        private ISchedulerComponent _scheduler;
        private IDataComponent _data;
        private Thread _stateThread;
        private bool _shouldRun = true;
        private Dictionary<int, JobState> _stateQueue = new Dictionary<int, JobState>();

        public SchedulerHost()
        {
            _scheduler = IoCContainer.Instance.Resolve<ISchedulerComponent>();
            _scheduler.StateUpdated += SchedulerStateUpdated;
            _data = IoCContainer.Instance.Resolve<IDataComponent>();
            _stateThread = new Thread(StateThread);
            _stateThread.Start();
        }

        private void StateThread()
        {
            while (_shouldRun)
            {
                List<JobState> toSend = new List<JobState>();
                lock (_stateQueue)
                {
                    foreach (int id in _stateQueue.Keys.ToArray())
                    {
                        toSend.Add(_stateQueue[id]);
                        _stateQueue.Remove(id);
                    }
                }
                foreach (var item in toSend)
                {
                    Send(s => s.JobStateUpdated(item));
                }
            }
        }

        public override void Dispose()
        {
            _shouldRun = false;
        }

        private void SchedulerStateUpdated(JobState state)
        {
            lock (_stateQueue)
            {
                if (_stateQueue.ContainsKey(state.JobId))
                {
                    _stateQueue[state.JobId] = state;
                }
                else
                {
                    _stateQueue.Add(state.JobId, state);
                }
            }
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

        public List<JobState> GetStates()
        {
            return _scheduler.GetStates();
        }

        public void RunJob(JobConfiguration job)
        {
            _scheduler.RunJob(job);
        }

        public void CancelJob(JobConfiguration job)
        {
            Task.Run(() => _scheduler.CancelJob(job));
        }
    }
}
