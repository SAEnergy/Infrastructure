using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Core.Comm
{
    public class EndpointInformation
    {
        // options would go here if there were any
        public const int DefaultPort = 9595;


        public static EndpointAddress BuildEndpoint(EndpointInformation info, ServerConnectionInformation server, Type serviceContractType)
        {
            string connstr = null;
            if (server.ConnectionString.Contains(":"))
            {

                connstr = "net.tcp://" + server.ConnectionString + "/" + serviceContractType.Name + "/";
            }
            else
            {
                connstr = "net.tcp://" + server.ConnectionString + ":" + DefaultPort + "/" + serviceContractType.Name + "/";
            }
            return new EndpointAddress(new Uri(connstr), EndpointIdentity.CreateSpnIdentity(""));
        }

    }
}
