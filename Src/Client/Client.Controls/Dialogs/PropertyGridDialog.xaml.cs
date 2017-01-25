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

namespace Client.Controls.Dialogs
{
    public partial class PropertyGridDialog : DialogBase
    {
        public PropertyGridDialog(Window owner, bool liveEdit= false) : base(owner)
        {
            InitializeComponent();
            if (liveEdit) propertyGrid.LiveEdit = true;
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ClickSave(object sender, RoutedEventArgs e)
        {
            propertyGrid.Commit();
            DialogResult = true;
            this.Close();
        }
    }
}
