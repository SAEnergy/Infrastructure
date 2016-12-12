using Core.Interfaces.Components.Logging;
using Core.Logging.LogDestinationConfigs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Core.Logging.LogDestinations
{
    public sealed class TCPLogDestination : LogDestinationBase
    {
        #region Fields

        private readonly TCPLogDestinationConfig _config;
        private TcpClient _tcpClient;

        #endregion


        #region Constructor

        public TCPLogDestination(TCPLogDestinationConfig config)
        {
            if (null != config)
                _config = config;
            else
                throw new ArgumentNullException("config");
        }

        #endregion


        #region Private Methods



        private void Connect()
        {
            if (null == _tcpClient)
                _tcpClient = new TcpClient();

            if (!_tcpClient.Connected)
            {
                try
                {
                    _tcpClient.Connect(_config.HostName, _config.Port);
                }
                catch (Exception ex)
                {
                    _logger.HandleLoggingException("Error in TCPLogDestination :" + ex.Message, ex);
                }
            }
        }

        public override void ShutDownDestination()
        {
            if (null != _tcpClient && _tcpClient.Connected)
                _tcpClient.Close();
        }

        private void WriteLine(LogMessage message)
        {
            try
            {
                Byte[] byteMessage;
                NetworkStream stream = _tcpClient.GetStream();

                if (null != _config.LogMessageFormatter)
                    byteMessage = Encoding.UTF8.GetBytes(_config.LogMessageFormatter.Format(message) + "\r\n");
                else
                    byteMessage = Encoding.UTF8.GetBytes(message.ToString() + "\r\n");

                stream.Write(byteMessage, 0, byteMessage.Length);
            }
            catch (Exception ex)
            {
                _logger.HandleLoggingException("Failed to write message to stream :"+ ex.Message,ex);
            }
        }

        #endregion


        #region Public Methods



        public override void ReportMessages(List<LogMessage> messages)
        {
            Connect();

            foreach (LogMessage message in messages)
            {
                WriteLine(message);
            }
        }



        #endregion
    }
}
