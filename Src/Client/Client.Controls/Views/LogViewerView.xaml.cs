using System;
using System.Windows;
using System.Windows.Controls;

namespace Client.Controls
{
    /// <summary>
    /// Interaction logic for LogView.xaml
    /// </summary>
    public partial class LogViewerView : Client.Base.ViewBase
    {
        public static readonly DependencyProperty IsLocalProperty = DependencyProperty.Register("IsLocal", typeof(bool), typeof(LogViewerView), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnLocalChanged)));

        private static void OnLocalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LogViewerView l = d as LogViewerView;

            if (l.ViewModel != null) { l.ViewModel.Dispose(); }

            if (l.IsLocal)
            {
                l.ViewModel = new LocalLogViewerViewModel(l);
            }
            else
            {
                l.ViewModel = new RemoteLogViewerViewModel(l);
            }
        }

        public LogViewerView()
        {
            InitializeComponent();
            this.Loaded += LogViewerView_Loaded;
        }

        private void LogViewerView_Loaded(object sender, RoutedEventArgs e)
        {
            OnLocalChanged(this, new DependencyPropertyChangedEventArgs());
        }

        public bool IsLocal
        {
            get
            {
                return (bool)this.GetValue(IsLocalProperty);
            }
            set
            {
                this.SetValue(IsLocalProperty, value);
            }
        }

        private void datagrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // If the entire contents fit on the screen, ignore this event
            if (e.ExtentHeight < e.ViewportHeight)
                return;

            // If no items are available to display, ignore this event
            if (logDataGrid.Items.Count <= 0)
                return;

            // If the ExtentHeight and ViewportHeight haven't changed, ignore this event
            if (e.ExtentHeightChange == 0.0 && e.ViewportHeightChange == 0.0)
                return;

            // If we were close to the bottom when a new item appeared,
            // scroll the new item into view.  We pick a threshold of 5
            // items since issues were seen when resizing the window with
            // smaller threshold values.
            var oldExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
            var oldVerticalOffset = e.VerticalOffset - e.VerticalChange;
            var oldViewportHeight = e.ViewportHeight - e.ViewportHeightChange;
            if (oldVerticalOffset + oldViewportHeight + 5 >= oldExtentHeight)
                logDataGrid.ScrollIntoView(logDataGrid.Items[logDataGrid.Items.Count - 1]);
        }
    }
}
