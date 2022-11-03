using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    class PlaceOnSheets : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            
            TaskDialog.Show("Кнопка", "Разместить на листы");

            return Result.Succeeded;
        }
    }
}
