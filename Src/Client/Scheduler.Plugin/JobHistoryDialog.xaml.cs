using Client.Base;
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
using System.Windows.Shapes;
using System.ComponentModel;

namespace Scheduler.Plugin
{
    /// <summary>
    /// Interaction logic for JobHistoryDialog.xaml
    /// </summary>
    public partial class JobHistoryDialog : DialogBase
    {
        public JobHistoryDialog(Window owner, int jobID) : base(owner)
        {
            InitializeComponent();
            DataContext = new JobHistoryViewModel(null, jobID);

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            (this.DataContext as JobHistoryViewModel).Dispose();
        }
    }
}
