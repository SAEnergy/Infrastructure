using Core.Comm.BaseClasses;
using Core.Interfaces.Components.Logging;
using Core.Interfaces.ServiceContracts;
using Logging.Remote;
using System;
using System.ServiceModel;

namespace Server.Hosts
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession)]
    public class RemoteLogViewerHost : ServiceHostBase<IRemoteLogViewer, IRemoteLogViewerCallback>, IRemoteLogViewer
    {
        private ILogDestination _destination;

        public void Register()
        {
            if(_destination == null)
            {
                _destination = new RemoteLogViewerDestination(_callback);

                _logger.Log(string.Format("Registering RemoteLogViewerDestination with id \"{0}\".", _destination.Id));

                _logger.AddLogDestination(_destination);
            }
            else
            {
                throw new NotSupportedException("Client has already registered to view remote logs.");
            }
        }

        public void Unregister()
        {
            if(_destination != null)
            {
                _logger.Log(string.Format("Unregistering RemoteLogViewerDestination with id \"{0}\".", _destination.Id));

                _logger.RemoveLogDestination(_destination);

                _destination = null;
            }
            else
            {
                throw new NotSupportedException("Client has not registered to view remote logs.");
            }
        }

        public override void Dispose()
        {
            if (_destination != null)
            {
                _logger.Log("Client did not unregister with RemoteLogViewerHost...", severity: LogMessageSeverity.Warning);

                Unregister();
            }

            base.Dispose();
        }
    }
}
