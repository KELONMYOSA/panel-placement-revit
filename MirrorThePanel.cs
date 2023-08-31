using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    class MirrorThePanel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            var ui = new UserInterfaceMirror(doc, sel);
            bool tdRes = (bool)ui.ShowDialog();

            if (tdRes == false)
            {
                return Result.Cancelled;
            }
            else
            {
                IList<Element> selectedPanels = ui.getSelectedPanels;
                string tablePath = ui.getTablePath;
                Element symmetryLine = ui.getLine;

                IList<ElementType> panelTypes = selectedPanels.Select(p => doc.GetElement(p.GetTypeId()) as ElementType).ToList();
                panelTypes = panelTypes.GroupBy(u => u.Name).Select(g => g.First()).ToList();
                IList<string> panelTypeNames = panelTypes.Select(p => p.Name).ToList();

                XYZ pointA = (symmetryLine.Location as LocationCurve).Curve.GetEndPoint(0);
                XYZ pointB = (symmetryLine.Location as LocationCurve).Curve.GetEndPoint(1);

                MessageBox.Show("- " + string.Join(", ", panelTypeNames) + "\n" + 
                    "- " + tablePath + "\n" +
                    "- " + pointA.ToString() + ", " + pointB.ToString() + "\n",
                    "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                return Result.Succeeded;
            }    
        }
    }
}