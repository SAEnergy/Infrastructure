using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using Core.Interfaces.ServiceContracts;
using System.ServiceModel.Description;

namespace Core.Comm
{

    public enum SubscriptionState { Disconnected, Connecting, Connected, };

    public class Subscription<T> : ISubscription<T> where T : IUserAuthentication
    {
        public event SubscriptionConnectedEvent Connected;
        public event SubscriptionDisconnectedEvent Disconnected;
        protected ServerConnectionInformation _serverInfo;
        protected ChannelFactory<T> _factory;
        protected object _callback;
        protected Exception _lastException;
        private Thread _worker;
        public SubscriptionState State { get; private set; }

        private object _stateLock = new object();
        private ManualResetEvent _resetEvent = new ManualResetEvent(false);
        private ManualResetEvent _reconnectEvent = new ManualResetEvent(false);

        public Subscription(ServerConnectionInformation serverConnectionInformation, object callback)
        {
            _serverInfo = serverConnectionInformation;
            _serverInfo.Reconnect += ReconnectRequested;
            _callback = callback;
        }

        public T Channel { get; protected set; }

        public void Start()
        {
            lock (_stateLock)
            {
                State = SubscriptionState.Connecting;
            }
            _worker = new Thread(new ThreadStart(WorkerThread));
            _worker.Start();
        }

        private void WorkerThread()
        {
            try
            {
                while (true)
                {
                    _resetEvent.Reset();
                    _reconnectEvent.Reset();

                    if (_factory == null)
                    {
                        EndpointAddress endpoint = EndpointInformation.BuildEndpoint(new EndpointInformation(), _serverInfo, typeof(T));
                        Binding binding = BindingInformation.BuildBinding(new BindingInformation(), _serverInfo);
                        if (_callback != null)
                        {
                            _factory = new DuplexChannelFactory<T>(_callback, binding, endpoint);
                        }
                        else
                        {
                            _factory = new ChannelFactory<T>(binding, endpoint);
                        }

                        foreach (var operation in _factory.Endpoint.Contract.Operations)
                        {
                            operation.Behaviors.Find<DataContractSerializerOperationBehavior>().DataContractResolver = new DataContractTypeResolver();
                        }
                    }

                    if (_factory.State != CommunicationState.Opened)
                    {
                        try
                        {
                            _factory.Open();
                            Channel = _factory.CreateChannel();
                            Channel.Ping();
                            ((ICommunicationObject)Channel).Closed += Channel_Closed;
                            ((ICommunicationObject)Channel).Faulted += Channel_Closed;
                            lock (_stateLock)
                            {
                                State = SubscriptionState.Connected;
                            }
                            if (Connected != null) { Connected(this); }
                        }
                        catch (ThreadAbortException)
                        {

                        }
                        catch (Exception ex)
                        {
                            _lastException = ex;
                            OnDisconnect(ex);
                        }
                    }
                    else
                    {
                        _resetEvent.WaitOne(300000);
                        try
                        {
                            Channel.Ping();
                        }
                        catch (ThreadAbortException) { }
                        catch (Exception ex)
                        {
                            _lastException = ex;
                            OnDisconnect(ex);
                        }
                    }

                    switch (WaitHandle.WaitAny(new WaitHandle[] { _resetEvent, _reconnectEvent },15000))
                    {
                        case WaitHandle.WaitTimeout:break;
                        case 0: break;
                        case 1:
                            {
                                Cleanup();
                                lock(_stateLock)
                                {
                                    State = SubscriptionState.Connecting;
                                }
                            }
                            break;
                    }
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception ex)
            {
                _lastException = ex;
                OnDisconnect(ex);
            }
            finally
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {

            Channel = default(T);

            try
            {
                if (_factory != null) { _factory.Close(); }
            }
            catch
            {
                /* nom */
            }

            try
            {
                if (_factory != null) { _factory.Abort(); }
            }
            catch
            {
                /* nom */
            }

            _factory = null;
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            OnDisconnect(new CommunicationException("Channel Closed."));
            _resetEvent.Set();
        }

        private void ReconnectRequested()
        {
            _reconnectEvent.Set();
            _resetEvent.Set();
        }

        protected void OnDisconnect(Exception ex)
        {
            try
            {
                lock (_stateLock)
                {
                    if (State == SubscriptionState.Disconnected) { return; }
                    State = SubscriptionState.Disconnected;
                }

                if (Disconnected != null) { Disconnected(this, ((ex != null) ? ex : new Exception("Unknown error."))); }
            }
            finally
            {
                Cleanup();
            }
        }

        public void Stop()
        {
            _worker.Abort();
        }

        public bool Verify()
        {
            throw new NotImplementedException();
        }
    }
}
