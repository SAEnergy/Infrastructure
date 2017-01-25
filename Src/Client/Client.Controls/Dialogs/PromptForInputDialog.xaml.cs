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
using System.Windows.Shapes;

namespace Client.Controls.Dialogs
{
    public partial class PromptForInputDialog : DialogBase
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(string), typeof(PromptForInputDialog));
        public string Data
        {
            get { return (string)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty InputPromptProperty = DependencyProperty.Register("InputPrompt", typeof(string), typeof(PromptForInputDialog));
        public string InputPrompt
        {
            get { return (string)GetValue(InputPromptProperty); }
            set { SetValue(InputPromptProperty, value); }
        }

        public PromptForInputDialog(Window owner) : base(owner)
        {
            this.DataContext = this;
            InputPrompt = "Value:";
            InitializeComponent();
        }

        private void ClickCanel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void ClickOK(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}
