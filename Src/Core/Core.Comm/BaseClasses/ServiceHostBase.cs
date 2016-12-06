using Core.Interfaces;
using Core.Interfaces.Components.Logging;
using Core.Interfaces.ServiceContracts;
using Core.IoC.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Core.Comm.BaseClasses
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession)]
    public abstract class ServiceHostBase<CHANNEL> : IServiceHost<CHANNEL>, IUserAuthentication, IDisposable where CHANNEL : IUserAuthentication
    {
        protected ILogger _logger;
        protected static List<IServiceHost<CHANNEL>> _instances = new List<IServiceHost<CHANNEL>>();

        public void Ping()
        {
            // todo: real authentication
        }

        public virtual void Dispose()
        {
            lock (_instances)
            {
                _instances.Remove(this);
            }
            _logger.Log("Host of type " + this.GetType() + " closed.");
        }

        public ServiceHostBase()
        {
            _logger = (ILogger)IoCContainer.Instance.Resolve(typeof(ILogger));
            _logger.Log("Host of type " + this.GetType() + " created.");
            lock (_instances)
            {
                _instances.Add(this);
            }
        }

        protected I[] GetInstances<I>()
        {
            lock (_instances)
            {
                return _instances.OfType<I>().ToArray();
            }
        }
    }


    public abstract class ServiceHostBase<CHANNEL, CALLBACK> : ServiceHostBase<CHANNEL> where CHANNEL : IUserAuthentication
    {
        protected CALLBACK _callback;

        public ServiceHostBase()
        {
            _callback = OperationContext.Current.GetCallbackChannel<CALLBACK>();
        }

        private void Broadcast(Action<CALLBACK> action, bool includeSelf)
        {
            List<CALLBACK> callbacks = new List<CALLBACK>();
            lock (_instances)
            {
                if (includeSelf)
                {
                    callbacks.AddRange(_instances.OfType<ServiceHostBase<CHANNEL, CALLBACK>>().Select(c => c._callback));
                }
                else
                {
                    callbacks.AddRange(_instances.OfType<ServiceHostBase<CHANNEL, CALLBACK>>().Where(i => i != this).Select(c => c._callback));
                }
            }
            foreach (CALLBACK iter in callbacks)
            {
                CALLBACK callback = iter;
                Task.Run(() =>
                {
                    TryCallBack(action, callback);
                }
                );
            }
        }

        // send a message to all hosts, including self
        protected void Broadcast(Action<CALLBACK> action)
        {
            Broadcast(action, true);

        }

        // send a message to all other hosts, excluding self
        protected void BroadcastOther(Action<CALLBACK> action)
        {
            Broadcast(action, false);
        }

        private void TryCallBack(Action<CALLBACK> action, CALLBACK callback)
        {
            try
            {
                action.Invoke(callback);
            }
            catch (Exception ex)
            {
                _logger.Log("Exception during callback.  Host=" + typeof(CHANNEL).Name + ", Message=" + ex.Message, LogMessageSeverity.Warning);
            }
        }

        // send a message only to single client
        protected void Send(Action<CALLBACK> action)
        {
            TryCallBack(action, _callback);
        }

        // asynchronously send a message only to single client
        protected void Post(Action<CALLBACK> action)
        {
            Task.Run(() =>
            {
                TryCallBack(action, _callback);
            });
        }
    }
}