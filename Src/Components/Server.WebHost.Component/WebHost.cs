using Core.Interfaces.Components;
using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Interfaces.Components.Logging;
using Microsoft.Owin.Hosting;
using Nancy;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models;

namespace Server.WebHost
{
    [ComponentRegistration(typeof(IWebHost))]
    [ComponentMetadata(AllowedActions = ComponentUserActions.Restart, Description = "Self hosted web server.", FriendlyName = "Web Server Component")]
    public class WebHost : Singleton<IWebHost>, IWebHost
    {
        private ILogger _logger;
        //private IDisposable _host;

        private WebHost(ILogger logger)
        {
            _logger = logger;
        }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            //_host = WebApp.Start<OwinStartup>("http://+:9596");
            IsRunning = true;
        }

        public void Stop()
        {
            //_host.Dispose();
            IsRunning = false;
        }

        public static IWebHost CreateInstance(ILogger logger)
        {
            return Instance = new WebHost(logger);
        }
    }

    public class OwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseNancy();
        }
    }

    //public class CustomRootPathProvider : IRootPathProvider
    //{
    //    public string GetRootPath()
    //    {
    //        return Path.GetDirectoryName(@"c:\sandboxes\infrastructure\src\web");
    //    }
    //}
}
