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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scheduler.Plugin
{
    public partial class SchedulerAddJobDialog : DialogBase
    {
        public SchedulerAddJobView View { get; set; }

        public SchedulerAddJobDialog(IEnumerable<Type> availableTypes, Window owner) : base(owner)
        {
            View = new SchedulerAddJobView(availableTypes);
            this.DataContext = this;
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            View.Dispose();
            base.OnClosed(e);
        }
    }
}
