using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PanelPlacement
{
    public partial class UserInterfaceAssemblies : Window
    {
        bool assembliesSelection = false;

        public UserInterfaceAssemblies(IList<string> types)
        {
            InitializeComponent();
            TypesListBox.ItemsSource = types;
        }

        public IList<string> selectedTypes
        {
            get
            {
                return TypesListBox.SelectedItems.Cast<string>().ToList();
            }
        }

        private void ButtonAll(Object sender, EventArgs e)
        {
            TypesListBox.SelectAll();
            TypesListBox.Focus();
        }

        private void ButtonCreate(Object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ButtonSelect(Object sender, EventArgs e)
        {
            DialogResult = true;
            assembliesSelection = true;
            Close();
        }

        public bool assembliesSelectionMode
        {
            get
            {
                return assembliesSelection;
            }
        }
    }
}
