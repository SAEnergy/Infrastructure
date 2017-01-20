using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Controls
{
    public abstract class PropertyGridEditor : Control
    {
        protected const string _multipleValuesString = "«Multiple»";
        protected bool _freeze;

        public PropertyGridEditor()
        {
            IsTabStop = false;
            Values = new ObservableCollection<object>();
            Values.CollectionChanged += Values_CollectionChanged;
        }

        public string PropertyName { get; set; }

        public PropertyInfo Property { get; set; }

        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register("DisplayName", typeof(string), typeof(PropertyGridEditor));
        public virtual string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PropertyGridEditor));
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsMultipleValuesProperty = DependencyProperty.Register("IsMultipleValues", typeof(bool), typeof(PropertyGridEditor));
        public bool IsMultipleValues
        {
            get { return (bool)GetValue(IsMultipleValuesProperty); }
            set { SetValue(IsMultipleValuesProperty, value); }
        }

        public bool IsDirty { get; set; }

        public event EventHandler Modified;

        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(PropertyGridEditor), new PropertyMetadata(null, OnDataPropertyChanged, OnCoercePropertyData));
        public object Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyPropertyKey ValuesPropertyKey = DependencyProperty.RegisterReadOnly("Values", typeof(ObservableCollection<object>), typeof(PropertyGridEditor), new PropertyMetadata());
        public static readonly DependencyProperty ValuesProperty = ValuesPropertyKey.DependencyProperty;
        public ObservableCollection<object> Values
        {
            get { return (ObservableCollection<object>)GetValue(ValuesProperty); }
            protected set { SetValue(ValuesPropertyKey, value); }
        }

        protected static object OnCoercePropertyData(DependencyObject d, object baseValue)
        {
            PropertyGridEditor ed = ((PropertyGridEditor)d);
            return ed.OnCoerceData(baseValue);
        }

        protected virtual object OnCoerceData(object value)
        {
            if (_freeze) { return value; }
            if (value == null) return null;
            if (value.GetType() == Property.PropertyType) { return value; }
            var conv = TypeDescriptor.GetConverter(Property.PropertyType);
            object newval = conv.ConvertFromInvariantString(value.ToString());
            return newval;
            //return Convert.ChangeType(value, PropertyType);
        }

        protected static void OnDataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyGridEditor ed = ((PropertyGridEditor)d);
            ed.OnDataChanged();
        }

        protected virtual void OnDataChanged()
        {
            if (_freeze) { return; }
            if (Values.Count == 0) { return; }
            IsDirty = true;
            IsMultipleValues = false;
            if (Modified != null) { Modified(this, null); }
        }

        protected void Values_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _freeze = true;
            try
            {
                IsMultipleValues = false;
                if (Property.PropertyType.IsValueType)
                {
                    Data = Activator.CreateInstance(Property.PropertyType);
                }
                else { Data = null; }

                if (Values.Count == 0) { return; }

                Data = Values[0];

                foreach (object obj in Values)
                {
                    if (object.Equals(Data, obj)) { continue; }
                    IsMultipleValues = true;
                    OnMultipleValues();
                }
            }
            finally
            {
                _freeze = false;
            }
        }

        protected virtual void OnMultipleValues()
        {
            Data = Values[0];
        }

        public virtual void Commit(List<object> items)
        {
            foreach (object obj in items)
            {
                Property.SetValue(obj, Data);
            }
        }
    }

    public class PropertyGridTextEditor : PropertyGridEditor
    {
        static PropertyGridTextEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGridTextEditor), new FrameworkPropertyMetadata(typeof(PropertyGridTextEditor)));
        }

        protected override void OnMultipleValues()
        {
            Data = _multipleValuesString;// + " " + string.Join(",", Values.Select(o => (o == null ? "" : o.ToString())).ToArray());
        }

        public override void Commit(List<object> items)
        {
            string bob = Data as string;

            if (!string.IsNullOrWhiteSpace(bob) && bob.Contains(_multipleValuesString))
            {
                return;
            }

            base.Commit(items);
        }
    }

    public class PropertyGridTimeSpanEditor : PropertyGridEditor
    {
        static PropertyGridTimeSpanEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGridTimeSpanEditor), new FrameworkPropertyMetadata(typeof(PropertyGridTimeSpanEditor)));
        }
    }

    public class PropertyGridBoolEditor : PropertyGridEditor
    {
        static PropertyGridBoolEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGridBoolEditor), new FrameworkPropertyMetadata(typeof(PropertyGridBoolEditor)));
        }

        protected override void OnMultipleValues()
        {
            // null for indeterminite check box
            Data = null;
        }
    }

    public class PropertyGridNullableEditor : PropertyGridEditor
    {
        static PropertyGridNullableEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGridNullableEditor), new FrameworkPropertyMetadata(typeof(PropertyGridNullableEditor)));
        }

        protected override void OnMultipleValues()
        {
            // null for indeterminite check box
            Data = _multipleValuesString;
        }
    }

    public class PropertyGridEnumEditor : PropertyGridEditor
    {
        static PropertyGridEnumEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGridEnumEditor), new FrameworkPropertyMetadata(typeof(PropertyGridEnumEditor)));
        }

        public static readonly DependencyProperty AvailableValuesProperty = DependencyProperty.Register("AvailableValues", typeof(ObservableCollection<object>), typeof(PropertyGridEditor));
        public ObservableCollection<object> AvailableValues
        {
            get { return (ObservableCollection<object>)GetValue(AvailableValuesProperty); }
            set { SetValue(AvailableValuesProperty, value); }
        }

        public PropertyGridEnumEditor()
        {
            AvailableValues = new ObservableCollection<object>();
            SetAvailableValues();
        }

        protected void SetAvailableValues()
        {
            if (Property == null) { return; }
            if (AvailableValues.Count == 0)
            {
                foreach (object obj in Enum.GetValues(Property.PropertyType))
                {
                    AvailableValues.Add(obj);
                }
            }
            if (IsMultipleValues)
            {
                if (!AvailableValues.Contains(_multipleValuesString))
                {
                    AvailableValues.Add(_multipleValuesString);
                }
            }
            else
            {
                if (AvailableValues.Contains(_multipleValuesString))
                {
                    AvailableValues.Remove(_multipleValuesString);
                }
            }
        }

        protected override void OnDataChanged()
        {
            base.OnDataChanged();
            SetAvailableValues();
        }

        protected override object OnCoerceData(object value)
        {
            if (value == null) { return Data; }
            return base.OnCoerceData(value);
        }

        protected override void OnMultipleValues()
        {
            Data = _multipleValuesString;
            SetAvailableValues();
        }
    }
}
