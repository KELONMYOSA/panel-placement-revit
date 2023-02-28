using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PanelPlacement
{
    public partial class UserInterfaceNote : Window
    {
        public UserInterfaceNote(IList<string> assembleParams)
        {
            InitializeComponent();
            SelectionParam.ItemsSource = assembleParams;
        }

        public string selectedParam
        {
            get
            {
                return SelectionParam.SelectedItem as string;
            }
        }

        private void ButtonCreate(Object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
