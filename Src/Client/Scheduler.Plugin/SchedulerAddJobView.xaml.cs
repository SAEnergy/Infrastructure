using Client.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scheduler.Plugin
{
    /// <summary>
    /// Interaction logic for SchedulerAddJobView.xaml
    /// </summary>
    public partial class SchedulerAddJobView : ViewBase
    {
        public SchedulerAddJobView(IEnumerable<Type> availableTypes)
        {
            ViewModel = new SchedulerAddJobViewModel(availableTypes,this);
            InitializeComponent();
        }
    }
}
