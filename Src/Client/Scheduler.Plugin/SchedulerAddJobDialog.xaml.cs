using Client.Base;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public partial class SchedulerAddJobDialog : DialogBase, INotifyPropertyChanged
    {
        public ObservableCollection<Type> AvailableJobTypes { get; set; }
        public JobConfiguration Job { get; set; }

        public SchedulerAddJobDialog(IEnumerable<Type> availableTypes, Window owner) : base(owner)
        {
            this.DataContext = this;
            AvailableJobTypes = new ObservableCollection<Type>();
            foreach (Type t in availableTypes) { AvailableJobTypes.Add(t); }
            InitializeComponent();
        }

        private Type _selectedType;

        public event PropertyChangedEventHandler PropertyChanged;

        public Type SelectedType
        {
            get { return _selectedType; }
            set
            {
                _selectedType = value;
                Job = (JobConfiguration)Activator.CreateInstance(value);
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Job"));
            }
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            if (Job == null) { return; }
            DialogResult = true;
            Close();
        }
    }
}
