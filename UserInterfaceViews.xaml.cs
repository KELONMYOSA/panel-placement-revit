using System;
using System.Collections.Generic;
using System.Windows;

namespace PanelPlacement
{
    public partial class UserInterfaceViews : Window
    {
        public UserInterfaceViews()
        {
            InitializeComponent();
        }

        private void ButtonCreate(Object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
