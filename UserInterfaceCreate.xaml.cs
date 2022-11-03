using System;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace PanelPlacement
{
    public partial class UserInterfaceCreate : Window
    {
        public UserInterfaceCreate()
        {
            InitializeComponent();
        }

        public string getWidth
        {
            get
            {
                string width = Width.Text;
                return width;
            }
        }

        public string getHeight
        {
            get
            {
                string height = Height.Text;
                return height;
            }
        }

        public bool GetDivideResult
        {
            get
            {
                bool divideByWalls = (bool)DivideTrue.IsChecked;
                return divideByWalls;
            }
        }

        public bool GetAnalyseLinks
        {
            get
            {
                bool analyseLinks = (bool)AnalyseLinks.IsChecked;
                return analyseLinks;
            }
        }

        private void DivideFalse_Click(object sender, RoutedEventArgs e)
        {
            AnalyseLinks.IsChecked = false;
            AnalyseLinks.Visibility = Visibility.Collapsed;
        }

        private void DivideTrue_Click(object sender, RoutedEventArgs e)
        {
            AnalyseLinks.IsChecked = true;
            AnalyseLinks.Visibility = Visibility.Visible;
        }

        private void ButtonCreate(Object sender, EventArgs e)
        {
            int parsedNumber;
            if (int.TryParse(Width.Text, out parsedNumber) && int.TryParse(Height.Text, out parsedNumber))
            {
                DialogResult = true;
                Close();
                
            }
            else
            {
                MessageBox.Show("Габариты должны быть числами!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = false;
                Close();
            }
        }
    }
}
