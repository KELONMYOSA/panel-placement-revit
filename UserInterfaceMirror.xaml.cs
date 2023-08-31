using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace PanelPlacement
{
    public partial class UserInterfaceMirror : Window
    {
        private Document document;
        private Selection selection;

        private IList<Element> selectedPanels = new List<Element>();
        private string tablePath = null;
        private Element line = null;

        public UserInterfaceMirror(Document doc, Selection sel)
        {
            InitializeComponent();
            document = doc;
            selection = sel;
        }

        public IList<Element> getSelectedPanels
        {
            get
            {
                return selectedPanels;
            }
        }

        public string getTablePath
        {
            get
            {
                return tablePath;
            }
        }

        public Element getLine
        {
            get
            {
                return line;
            }
        }

        private void ButtonPanels(Object sender, EventArgs e)
        {
            Hide();
            PanelSelectionFilter panelSelectionFilter = new PanelSelectionFilter();
            IList<Reference> panelRefList = null;
            selectedPanels.Clear();
            try
            {
                panelRefList = selection.PickObjects(ObjectType.Element, panelSelectionFilter, "Выберите панели!");
                
                foreach (Reference refElem in panelRefList)
                {
                    selectedPanels.Add(document.GetElement(refElem));
                }
                
                if (selectedPanels.Count != 0)
                {
                    BtnPanels.Background = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    BtnPanels.Background = System.Windows.Media.Brushes.IndianRed;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                BtnPanels.Background = System.Windows.Media.Brushes.IndianRed;
                MessageBox.Show("Панели не были выбраны!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ShowDialog();
        }

        private void ButtonTable(Object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.InitialDirectory = Path.GetDirectoryName(document.PathName);
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV|*.csv|Text|*.txt";

            DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                BtnTable.Background = System.Windows.Media.Brushes.LightGreen;
                BtnTable.Content = Path.GetFileName(dlg.FileName);
                tablePath = dlg.FileName;
            }
            else
            {
                BtnTable.Background = System.Windows.Media.Brushes.IndianRed;
                BtnTable.Content = "Выбрать";
                tablePath = null;
            }
        }

        private void ButtonLine(Object sender, EventArgs e)
        {
            Hide();
            LineSelectionFilter lineSelectionFilter = new LineSelectionFilter();
            Reference lineRef = null;
            line = null;
            try
            {
                lineRef = selection.PickObject(ObjectType.Element, lineSelectionFilter, "Выберите линию!");

                line = document.GetElement(lineRef);

                BtnLine.Background = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                BtnLine.Background = System.Windows.Media.Brushes.IndianRed;
                MessageBox.Show("Линия не была выбрана!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ShowDialog();
        }

        private void ButtonOK(Object sender, EventArgs e)
        {
            if (BtnPanels.Background == System.Windows.Media.Brushes.LightGreen
                && BtnTable.Background == System.Windows.Media.Brushes.LightGreen
                && BtnLine.Background == System.Windows.Media.Brushes.LightGreen)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Не были выбраны необходимые параметры!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
