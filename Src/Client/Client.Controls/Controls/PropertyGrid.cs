using Client.Base;
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

        public SimpleCommand CommitCommand { get; private set; }

        public static readonly DependencyProperty LiveEditProperty = DependencyProperty.Register("LiveEdit", typeof(bool), typeof(PropertyGrid));
        public bool LiveEdit
        {
            get { return (bool)GetValue(LiveEditProperty); }
            set { SetValue(LiveEditProperty, value); }
        }

        private Dictionary<string, PropertyGridEditor> _properties;
        private List<object> _items;

        static PropertyGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGrid), new FrameworkPropertyMetadata(typeof(PropertyGrid)));
        }

        public PropertyGrid()
        {
            IsTabStop = false;
            CommitCommand = new SimpleCommand(Commit);
            Properties = new ObservableCollection<PropertyGridEditor>();
            ICollectionView view = CollectionViewSource.GetDefaultView(Properties);
            view.SortDescriptions.Add(new SortDescription("SortOrder", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));

            _items = new List<object>();
            _properties = new Dictionary<string, PropertyGridEditor>();
            DataContextChanged += PropertyGrid_DataContextChanged;
        }

        private void Clear()
        {
            Properties.Clear();
            _items.Clear();
            _properties.Clear();
        }

        private void PropertyGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Clear();

            if (DataContext == null) { return; }

            if (DataContext is IEnumerable<object>)
            {
                _items.AddRange(DataContext as IEnumerable<object>);
            }
            else
            {
                _items.Add(DataContext);
            }
            ParseProperties();
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            Clear();
            _items.AddRange(Items.OfType<object>());
            ParseProperties();
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            Clear();
            _items.AddRange(Items.OfType<object>());
            ParseProperties();
        }

        private void ParseProperties()
        {
            if(_items == null && _items.Count==0) { return; }
            foreach (object obj in _items)
            {
                foreach (PropertyInfo prop in obj.GetType().GetProperties())
                {
                    if (prop.DeclaringType == typeof(DependencyObject)) { continue; }
                    if (prop.DeclaringType == typeof(DispatcherObject)) { continue; }

                    PropertyEditorMetadataAttribute atty = prop.GetCustomAttribute<PropertyEditorMetadataAttribute>();
                    if (atty != null && atty.Hidden) { continue; }

                    PropertyGridEditor editor = null;
                    _properties.TryGetValue(prop.Name, out editor);

                    if (editor == null)
                    {
                        editor = PropertyGridEditorFactory.GetEditor(prop.PropertyType);
                        editor.Property = prop;
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
            if (!LiveEdit) { return; }

            PropertyGridEditor editor = sender as PropertyGridEditor;
            if (editor == null) { return; }
            Commit(editor);
        }

        public void Commit()
        {
            foreach (var editor in Properties)
            {
                if (editor.IsDirty)
                {
                    editor.Commit(_items);
                }
            }
        }

        public void Commit(PropertyGridEditor editor)
        {
            editor.Commit(_items);
        }
    }
}
