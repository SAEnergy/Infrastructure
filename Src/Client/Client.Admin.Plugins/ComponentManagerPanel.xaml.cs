﻿using Client.Base;
using Core.Comm;
using Core.Interfaces.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Admin.Plugins
{
    /// <summary>
    /// Interaction logic for ComponentManagerPanel.xaml
    /// </summary>
    [PanelMetadata(DisplayName = "Component Manager", IconPath = "images/globe.png")]
    public partial class ComponentManagerPanel : PanelBase, IComponentManagerCallback
    {
        private Subscription<IComponentManager> _conn;

        public ComponentManagerPanel()
        {
            this.DataContext = this;
            InitializeComponent();
            _conn = new Subscription<IComponentManager>(this);
            _conn.Connected += _conn_Connected;
            _conn.Disconnected += _conn_Disconnected;
            _conn.Start();
        }

        public override void Dispose()
        {
            _conn.Stop();
            base.Dispose();
        }

        public void MooBack(string moo)
        {
            throw new NotImplementedException();
        }

        private void _conn_Disconnected(ISubscription source, Exception ex)
        {
            //this.BeginInvokeIfRequired(() => Messages.Add("Disconnected. " + ex.Message));
        }

        private void _conn_Connected(object sender, EventArgs e)
        {
            //this.BeginInvokeIfRequired(() => Messages.Add("Connected to Server."));
            //try
            //{
            //    _conn.Channel.Moo();
            //}
            //catch (Exception ex)
            //{
            //    this.BeginInvokeIfRequired(() => Messages.Add("Error: " + ex.Message));
            //}
        }

    }
}
