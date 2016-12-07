using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Client.Controls
{
    // from http://grokys.blogspot.ro/2010/07/mvvm-and-multiple-selection-part-iii.html

    public interface IMultiSelectCollectionView
    {
        void AddControl(Selector selector);
        void RemoveControl(Selector selector);
    }

    public class MultiSelectCollectionView<T> : ListCollectionView, IMultiSelectCollectionView
    {
        public MultiSelectCollectionView(IList list)
            : base(list)
        {
            SelectedItems = new ObservableCollection<T>();
        }

        void IMultiSelectCollectionView.AddControl(Selector selector)
        {
            this.controls.Add(selector);
            SetSelection(selector);
            selector.SelectionChanged += control_SelectionChanged;
        }

        void IMultiSelectCollectionView.RemoveControl(Selector selector)
        {
            if (this.controls.Remove(selector))
            {
                selector.SelectionChanged -= control_SelectionChanged;
            }
        }

        public ObservableCollection<T> SelectedItems { get; private set; }

        void SetSelection(Selector selector)
        {
            MultiSelector multiSelector = selector as MultiSelector;
            ListBox listBox = selector as ListBox;

            if (multiSelector != null)
            {
                multiSelector.SelectedItems.Clear();

                foreach (T item in SelectedItems)
                {
                    multiSelector.SelectedItems.Add(item);
                }
            }
            else if (listBox != null)
            {
                listBox.SelectedItems.Clear();

                foreach (T item in SelectedItems)
                {
                    listBox.SelectedItems.Add(item);
                }
            }
        }

        void control_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.ignoreSelectionChanged)
            {
                bool changed = false;

                this.ignoreSelectionChanged = true;

                try
                {
                    foreach (T item in e.AddedItems)
                    {
                        if (!SelectedItems.Contains(item))
                        {
                            SelectedItems.Add(item);
                            changed = true;
                        }
                    }

                    foreach (T item in e.RemovedItems)
                    {
                        if (SelectedItems.Remove(item))
                        {
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        foreach (Selector control in this.controls)
                        {
                            if (control != sender)
                            {
                                SetSelection(control);
                            }
                        }
                    }
                }
                finally
                {
                    this.ignoreSelectionChanged = false;
                }
            }
        }

        bool ignoreSelectionChanged;
        List<Selector> controls = new List<Selector>();
    }

    public static class MultiSelect
    {
        public static bool GetIsEnabled(Selector target)
        {
            return (bool)target.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(Selector target, bool value)
        {
            target.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(MultiSelect),
                new UIPropertyMetadata(IsEnabledChanged));

        static void IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Selector selector = sender as Selector;
            bool enabled = (bool)e.NewValue;

            if (selector != null)
            {
                DependencyPropertyDescriptor itemsSourceProperty =
                    DependencyPropertyDescriptor.FromProperty(Selector.ItemsSourceProperty, typeof(Selector));
                IMultiSelectCollectionView collectionView = selector.ItemsSource as IMultiSelectCollectionView;

                if (enabled)
                {
                    if (collectionView != null) collectionView.AddControl(selector);
                    itemsSourceProperty.AddValueChanged(selector, ItemsSourceChanged);
                }
                else
                {
                    if (collectionView != null) collectionView.RemoveControl(selector);
                    itemsSourceProperty.RemoveValueChanged(selector, ItemsSourceChanged);
                }
            }
        }

        static void ItemsSourceChanged(object sender, EventArgs e)
        {
            Selector selector = sender as Selector;

            if (GetIsEnabled(selector))
            {
                IMultiSelectCollectionView oldCollectionView;
                IMultiSelectCollectionView newCollectionView = selector.ItemsSource as IMultiSelectCollectionView;
                collectionViews.TryGetValue(selector, out oldCollectionView);

                if (oldCollectionView != null)
                {
                    oldCollectionView.RemoveControl(selector);
                    collectionViews.Remove(selector);
                }

                if (newCollectionView != null)
                {
                    newCollectionView.AddControl(selector);
                    collectionViews.Add(selector, newCollectionView);
                }
            }
        }

        static Dictionary<Selector, IMultiSelectCollectionView> collectionViews = new Dictionary<Selector, IMultiSelectCollectionView>();
    }
}
