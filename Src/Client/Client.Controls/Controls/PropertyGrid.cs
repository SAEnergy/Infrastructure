using Core.Models;
using Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Client.Controls
{
    public class PropertyGrid : ItemsControl
    {

        public static readonly DependencyProperty PropertiesProperty = DependencyProperty.Register("Properties", typeof(ObservableCollection<PropertyGridEditor>), typeof(PropertyGrid));

        public ObservableCollection<PropertyGridEditor> Properties
        {
            get { return (ObservableCollection<PropertyGridEditor>)GetValue(PropertiesProperty); }
            set { SetValue(PropertiesProperty, value); }
        }

        private Dictionary<string, PropertyGridEditor> _properties;

        static PropertyGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGrid), new FrameworkPropertyMetadata(typeof(PropertyGrid)));
        }

        public PropertyGrid()
        {
            IsTabStop = false;
            Properties = new ObservableCollection<PropertyGridEditor>();
            ICollectionView view = CollectionViewSource.GetDefaultView(Properties);
            view.SortDescriptions.Add(new SortDescription("SortOrder", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));

            _properties = new Dictionary<string, PropertyGridEditor>();
            DataContextChanged += PropertyGrid_DataContextChanged;
        }

        private void Clear()
        {
            Properties.Clear();
            _properties.Clear();
        }

        private void PropertyGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Clear();

            if (DataContext == null) { return; }

            ParseProperties(new object[] { DataContext });
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            Clear();

            if (this.Items == null || Items.Count == 0) { return; }

            ParseProperties(Items.OfType<object>().ToArray());
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            Clear();

            if (this.Items == null || Items.Count == 0) { return; }

            ParseProperties(Items.OfType<object>().ToArray());
        }

        private void ParseProperties(IEnumerable<object> items)
        {
            Clear();
            foreach (object obj in items)
            {
                foreach (PropertyInfo prop in obj.GetType().GetProperties())
                {
                    if (prop.DeclaringType == typeof(DependencyObject)) { continue; }
                    if (prop.DeclaringType == typeof(DispatcherObject)) { continue; }

                    PropertyEditorMetadataAttribute atty = prop.GetCustomAttribute<PropertyEditorMetadataAttribute>();
                    if (atty!=null && atty.Hidden) { continue; }

                    PropertyGridEditor editor = null;
                    _properties.TryGetValue(prop.Name, out editor);

                    if (editor == null)
                    {
                        editor = PropertyGridEditorFactory.GetEditor(prop.PropertyType);
                        editor.PropertyType = prop.PropertyType;
                        editor.Name = prop.Name;
                        editor.DisplayName = PascalCaseSplitter.Split(prop.Name);
                        editor.IsReadOnly = prop.SetMethod == null || !prop.SetMethod.IsPublic;
                        editor.Modified += Meta_Modified;

                        _properties.Add(prop.Name, editor);
                        Properties.Add(editor);
                    }

                    editor.Values.Add(prop.GetValue(obj, null));
                }
            }
        }

        private void Meta_Modified(object sender, EventArgs e)
        {
            PropertyGridEditor editor = sender as PropertyGridEditor;
            if (editor == null) { return; }

            List<object> items = new List<object>();
            if (Items != null && Items.Count > 0) { items.AddRange(Items.OfType<object>()); }
            else if (DataContext != null) { items.Add(DataContext); }

            if (items.Count == 0) { return; }

            foreach (object obj in items)
            {
                PropertyInfo prop = obj.GetType().GetProperty(editor.Name);
                if (prop == null) { continue; }
                prop.SetValue(obj, editor.Data);
            }
        }
    }
}
