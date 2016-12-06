using Core.Comm;
using Core.Interfaces.ServiceContracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Client.Base
{
    public class ViewModelBase : DependencyObject, INotifyPropertyChanged, IDisposable
    {
        protected SynchronizationContext _context;
        protected ViewBase _parent;

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModelBase(ViewBase parent)
        {
            _parent = parent;
            _context = SynchronizationContext.Current;
        }

        public virtual void Dispose() { }

        protected void NotiftyPropertyChanged(string s)
        {
            if (PropertyChanged!=null) { PropertyChanged(this, new PropertyChangedEventArgs(s)); }
        }

        protected virtual void HandleTransactionException(Exception error)
        {
            //todo: log when logger is present in client
        }

        protected void Invoke(Action task)
        {
            _context.Send(delegate { task(); }, null);
        }

        protected void BeginInvoke(Action task)
        {
            _context.Post(delegate { task(); }, null);
        }

        protected void RevalidateAllCommands()
        {
            // Go through all of the commands in this view model and trigger a re-evaluation of the CanExecute flag.
            foreach (PropertyInfo prop in this.GetType().GetProperties().Where(p => typeof(SimpleCommand).IsAssignableFrom(p.PropertyType)))
            {
                SimpleCommand commie = (SimpleCommand) prop.GetValue(this);
                commie.FireCanExecuteChangedEvent();
            }
        }
    }

    public class ViewModelBase<T> : ViewModelBase where T : IUserAuthentication
    {
        protected Subscription<T> _sub;
        private object _queueLock = new object();
        private bool _isExecuting = false;
        private Queue<ManualResetEvent> _taskQueue = new Queue<ManualResetEvent>();

        public ViewModelBase(ViewBase parent) : base(parent)
        {
            _sub = new Subscription<T>(ServerConnectionInformation.Instance,this);
            _sub.Connected += OnConnect;
            _sub.Disconnected += OnDisconnect;
            _sub.Start();
        }

        protected T Channel { get { return _sub.Channel; } }

        protected virtual void OnConnect(ISubscription source)
        {
            RevalidateAllCommands();
        }

        protected virtual void OnDisconnect(ISubscription source, Exception error)
        {
            RevalidateAllCommands();
            HandleTransactionException(error);
        }

        public override void Dispose()
        {
            _sub.Stop();
            base.Dispose();
        }

        // guaranteed to execute sequentially in calling order
        protected Task Execute(Action action)
        {
            return Task.Run(()=> 
            {
                ManualResetEvent evt = null;
                lock (_queueLock)
                {
                    if (_isExecuting)
                    {
                        evt = new ManualResetEvent(false);
                        _taskQueue.Enqueue(evt);
                    }
                    _isExecuting = true;
                }
                if (evt!=null) { evt.WaitOne(); }
                try
                {
                    action();
                }
                catch(Exception ex)
                {
                    HandleTransactionException(ex);
                }
                finally
                {
                    lock (_queueLock)
                    {
                        if (_taskQueue.Count > 0)
                        {
                            _taskQueue.Dequeue().Set();
                        }
                        else
                        {
                            _isExecuting = false;
                        }
                    }
                }
            });
        }
    }
}
