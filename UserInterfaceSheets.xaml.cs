using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PanelPlacement
{
    public partial class UserInterfaceSheets : Window
    {
        public UserInterfaceSheets(IList<string> assemblies, IList<string> sheets)
        {
            InitializeComponent();
            AssembliesListBox.ItemsSource = assemblies;
            SheetTemplates.ItemsSource = sheets;
        }

        public IList<string> selectedAssemblies
        {
            get
            {
                return AssembliesListBox.SelectedItems.Cast<string>().ToList();
            }
        }

        public string selectedSheetTemplate
        {
            get
            {
                return SheetTemplates.SelectedItem as string;
            }
        }

        private void ButtonAll(Object sender, EventArgs e)
        {
            AssembliesListBox.SelectAll();
            AssembliesListBox.Focus();
        }

        private void ButtonCreate(Object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
