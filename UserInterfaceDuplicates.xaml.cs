using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using ComboBox = System.Windows.Controls.ComboBox;
using Autodesk.Revit.UI;
using System.IO;
using System.Reflection;

namespace PanelPlacement
{
    public partial class UserInterfaceDuplicates : Window
    {
        private IList<string> docsTitles = new List<string>();
        private IList<string> paramList = new List<string>();

        public UserInterfaceDuplicates(string currentDocName, IList<string> docsName, IList<string> paramListForAssemble)
        {
            InitializeComponent();
            CurrentDocName.Content = currentDocName;
            docsTitles = docsName;
            paramList = paramListForAssemble;
        }

        public int findMode
        {
            get
            {
                if ((bool)AllParams.IsChecked)
                {
                    return 1;
                }
                else if ((bool)GeometryParams.IsChecked)
                {
                    return 2;
                }
                else
                {
                    return 3;
                }
            }
        }

        public string comparingParam
        {
            get
            {
                if (!paramSelectionIsActive) 
                { 
                    return null;
                }
                else
                {
                    return comboBoxParam.SelectedItem.ToString();
                }
            }
        }

        public IList<string> comparingDocs
        {
            get
            {
                IList<string> docsOut = new List<string>();
                foreach (ComboBox box in comboBoxes)
                {
                    docsOut.Add(box.SelectedItem.ToString());
                }

                return docsOut;
            }
        }

        public IList<string> comparingParamsList
        {
            get
            {
                IList<string> lines = new List<string>();
                string assemplyPath = Assembly.GetExecutingAssembly().Location;
                string path = Path.GetDirectoryName(assemplyPath) + @"\PanelPlacementCompareParams.txt";
                FileStream file = new FileStream(path, FileMode.Open);
                StreamReader readFile = new StreamReader(file);
                while (!readFile.EndOfStream)
                {
                    lines.Add(readFile.ReadLine());
                }
                readFile.Close();

                return lines;
            }
        }

        private void ButtonFind(Object sender, EventArgs e)
        {
            if (paramSelectionIsActive && comboBoxParam.SelectedItem == null)
            {
                System.Windows.Forms.MessageBox.Show("Параметр для сравнения не выбран!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (comboBoxes.Count > 0 && comboBoxes.Last().SelectedItem == null)
            {
                System.Windows.Forms.MessageBox.Show("Файл для сравнения не выбран!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private IList<ComboBox> comboBoxes = new List<ComboBox>();
        
        private void ButtonAddDoc(Object sender, EventArgs e)
        {
            if (comboBoxes.Count > 0 && comboBoxes.Last().SelectedItem == null)
            {
                System.Windows.Forms.MessageBox.Show("Выберите предыдущий документ!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Show();
            }
            else
            {
                if (comboBoxes.Count > 0)
                {
                    docsTitles.Remove(comboBoxes.Last().SelectedItem as string);
                    comboBoxes.Last().Focusable = false;
                    comboBoxes.Last().IsHitTestVisible = false;
                }
                
                ComboBox comboBox = new ComboBox();
                comboBox.ItemsSource = docsTitles;
                comboBox.Width = 250;
                comboBox.Height = 25;
                comboBox.VerticalAlignment = VerticalAlignment.Top;
                comboBox.Margin = new Thickness(0, 100 + comboBoxes.Count * 40, 0, 0);
                WindowGrid.Children.Add(comboBox);
                Grid.SetRow(comboBox, 1);
                comboBoxes.Add(comboBox);

                BtnAddDoc.Margin = new Thickness(0, BtnAddDoc.Margin.Top + 40, 0, 0);

                if (docsTitles.Count == 1)
                { 
                    BtnAddDoc.Visibility = Visibility.Hidden; 
                }
                else
                {
                    WindowGrid.RowDefinitions[1].Height = new GridLength(WindowGrid.RowDefinitions[1].Height.Value + 40);
                }
            }
        }

        private void ButtonOpenedDocs(Object sender, EventArgs e)
        {
            if (docsTitles.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("Не найдены открытые файлы!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                WindowGrid.RowDefinitions[1].Height = new GridLength(140);
                WindowGrid.RowDefinitions[0].Height = new GridLength(WindowGrid.RowDefinitions[0].Height.Value - 30);
                BtnOpenedDocs.Visibility = Visibility.Hidden;
            }
        }

        private ComboBox comboBoxParam = new ComboBox();
        private bool paramSelectionIsActive = false;

        private void ClickOneParam(Object sender, EventArgs e)
        {
            if (!paramSelectionIsActive)
            {
                comboBoxParam.ItemsSource = paramList;
                comboBoxParam.Width = 250;
                comboBoxParam.Height = 25;
                comboBoxParam.VerticalAlignment = VerticalAlignment.Top;
                comboBoxParam.Margin = new Thickness(0, 130, 0, 0);
                WindowGrid.Children.Add(comboBoxParam);
                Grid.SetRow(comboBoxParam, 0);
                WindowGrid.RowDefinitions[0].Height = new GridLength(WindowGrid.RowDefinitions[0].Height.Value + 30);
                paramSelectionIsActive = true;
            } 
        }

        private void DisableClickOneParam(Object sender, EventArgs e)
        {
            WindowGrid.Children.Remove(comboBoxParam);
            if (paramSelectionIsActive)
            {
                WindowGrid.RowDefinitions[0].Height = new GridLength(WindowGrid.RowDefinitions[0].Height.Value - 30);
                paramSelectionIsActive = false;
            }
        }

        private void ClickEditParams(Object sender, EventArgs e)
        {
            IList<string> lines = new List<string>();
            string assemplyPath = Assembly.GetExecutingAssembly().Location;
            string path = Path.GetDirectoryName(assemplyPath) + @"\PanelPlacementCompareParams.txt";
            FileStream file = new FileStream(path, FileMode.Open);
            StreamReader readFile = new StreamReader(file);
            while (!readFile.EndOfStream)
            {
                lines.Add(readFile.ReadLine());
            }
            readFile.Close();

            var uiEdit = new UserInterfaceDuplicatesParams(lines);
            bool tdEdit = (bool)uiEdit.ShowDialog();
        }
    }
}
