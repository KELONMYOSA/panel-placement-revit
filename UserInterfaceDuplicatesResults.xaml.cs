using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace PanelPlacement
{
    public partial class UserInterfaceDuplicatesResults : Window
    {
        public UserInterfaceDuplicatesResults(string textResult)
        {
            InitializeComponent();
            TextBlockResults.Text = textResult;
        }

        private void ButtonOk(Object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ButtonSave(object sender, System.EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.FileName = "Отчет_дубликаты_сборок";

            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog1.FileName, TextBlockResults.Text);
            }
        }
    }
}
