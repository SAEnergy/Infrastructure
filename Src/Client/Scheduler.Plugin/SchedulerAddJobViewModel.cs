using Client.Base;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Scheduler.Plugin
{
    public class SchedulerAddJobViewModel : ViewModelBase
    {
        public ObservableCollection<Type> AvailableJobTypes { get; set; }
        public JobConfiguration Job { get; set; }

        public SchedulerAddJobViewModel(IEnumerable<Type> availableTypes, ViewBase parent) : base(parent)
        {
            AvailableJobTypes = new ObservableCollection<Type>();
            foreach (Type t in availableTypes) { AvailableJobTypes.Add(t); }
        }

        private Type _selectedType;
        public Type SelectedType
        {
            get { return _selectedType; }
            set
            {
                _selectedType = value;
                Job = (JobConfiguration)Activator.CreateInstance(value);
                NotiftyPropertyChanged("Job");
            }
        }

    }
}
