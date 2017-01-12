using Core.Interfaces.Base;
using Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Client.Base
{
    public class ModelBase : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyChanged()
        {
            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                NotifyChanged(prop.Name);
            }
        }

        protected void NotifyChanged(string s)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(s)); }
        }
    }

    public class ModelBase<T> : ModelBase where T : ICloneable<T>
    {
        public event Action Modified;

        public T OriginalObject { get; protected set; }
        public T ModifiedObject { get; protected set; }

        public ModelBase()
        {
            OriginalObject = (T)Activator.CreateInstance<T>();
            ModifiedObject = OriginalObject.Clone();
        }

        public virtual void UpdateFrom(T source)
        {
            IsDirty = false;
            OriginalObject = source.Clone();
            ModifiedObject = source.Clone();
            NotifyChanged();
        }

        public virtual bool IsDirty { get; protected set; }

        protected virtual void SetDirty()
        {
            IsDirty = true;
            NotifyChanged();
            if (Modified != null) { Modified(); }
        }
    }
}
