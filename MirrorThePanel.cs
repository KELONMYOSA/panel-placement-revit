using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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

            MessageBox.Show("Панели отзеркалены", "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return Result.Succeeded;
        }
    }
}