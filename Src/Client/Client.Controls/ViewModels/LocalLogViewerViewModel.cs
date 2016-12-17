using Client.Base;
using Core.Comm;
using Core.Interfaces.Components.Logging;
using Core.Interfaces.ServiceContracts;
using Core.IoC.Container;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Client.Controls
{
    public class LocalLogViewerViewModel : ViewModelBase, ILogDestination
    {
        public static readonly DependencyProperty ViewDetailProperty = DependencyProperty.Register("ViewDetail", typeof(bool), typeof(LocalLogViewerViewModel));
        public bool ViewDetail
        {
            get { return (bool)GetValue(ViewDetailProperty); }
            set { SetValue(ViewDetailProperty, value); }
        }

        public static readonly DependencyProperty MaxMessagesProperty = DependencyProperty.Register("MaxMessages", typeof(int), typeof(LocalLogViewerViewModel));
        public int MaxMessages
        {
            get { return (int)GetValue(MaxMessagesProperty); }
            set { SetValue(MaxMessagesProperty, value); }
        }

        public ObservableCollection<LogMessage> LogMessages { get; private set; }

        private Guid _id = Guid.NewGuid();
        public Guid Id { get { return _id; } }

        public bool IsRunning { get { return true; } }
        public void Start() { }
        public void Stop() { }

        public LocalLogViewerViewModel(ViewBase parent) : base(parent)
        {
            IoCContainer.Instance.Resolve<ILogger>().AddLogDestination(this);
            LogMessages = new ObservableCollection<LogMessage>();
            MaxMessages = 5000;
        }

        public override void Dispose()
        {
            IoCContainer.Instance.Resolve<ILogger>().RemoveLogDestination(this);
            base.Dispose();
        }

        public void Flush() { }

        public void ProcessMessages(List<LogMessage> messages)
        {
            this.BeginInvoke(() =>
            {
                foreach (var message in messages)
                {
                    while (LogMessages.Count > MaxMessages - 1)
                    {
                        LogMessages.RemoveAt(0);
                    }

                    LogMessages.Add(message);
                }
            });
        }

        public void HandleLoggingException(LogMessage message) { }
    }
}
