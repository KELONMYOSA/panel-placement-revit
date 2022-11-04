using System;
using System.Collections.Generic;
using System.Windows;

namespace PanelPlacement
{
    public partial class UserInterfaceViews : Window
    {
        public UserInterfaceViews(IList<string> viewTemplates)
        {
            InitializeComponent();
            comboBox1.ItemsSource = viewTemplates;
            comboBox2.ItemsSource = viewTemplates;
            comboBox3.ItemsSource = viewTemplates;
        }

        public string selectedTemplatePlan
        {
            get
            {
                return comboBox1.SelectedItem as string;
            }
        }

        public string selectedTemplateFront
        {
            get
            {
                return comboBox2.SelectedItem as string;
            }
        }

        public string selectedTemplateSection
        {
            get
            {
                return comboBox3.SelectedItem as string;
            }
        }

        private void ButtonCreate(Object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
