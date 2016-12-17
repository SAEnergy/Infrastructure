using Client.Base;
using System.Threading;
using System.Windows;
using System;

namespace Client.Plugins.Test
{

    [PanelMetadata(DisplayName = "Log Viewer", IconPath = "images/warning.png")]
    public partial class LogViewerPanel : PanelBase
    {
        public LogViewerPanel()
        {
            InitializeComponent();
        }

        public override void Dispose()
        {
            logViewerView.Dispose();

            base.Dispose();
        }
    }
}
