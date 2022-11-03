using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    class CreatePanels : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            IList<Wall> walls = GetAllWalls(doc);

            var ui = new UserInterface();
            bool tdRes = (bool)ui.ShowDialog();

            if (tdRes == false)
            {
                return Result.Cancelled;
            }
            else
            {
                //Выбор стен
                WallSelectionFilter wallSelectionFilter = new WallSelectionFilter();
                IList<Reference> wallRefList = null;
                List<Wall> selectedWalls = new List<Wall>();
                try
                {
                    wallRefList = sel.PickObjects(ObjectType.Element, wallSelectionFilter, "Выберите стены!");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
                foreach (Reference refElem in wallRefList)
                {
                    selectedWalls.Add((doc.GetElement(refElem) as Wall));
                }
                if (selectedWalls.Count == 0)
                {
                    MessageBox.Show("Выберите стены!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Result.Cancelled;
                }

                TaskDialog.Show("Заданные параметры", "Ширина: " + ui.getWidth.ToString() + " Высота: " + ui.getHeight.ToString() + " Разделять: " + ui.GetDivideResult.ToString() + " Связи: " + ui.GetAnalyseLinks.ToString());

                return Result.Succeeded;
            }
        }
        public static IEnumerable<ExternalFileReference> GetLinkedFileReferences(Document _doc)
        {
            var collector = new FilteredElementCollector(_doc);
            var linkedElements = collector
                .OfClass(typeof(RevitLinkType))
                .Select(x => x.GetExternalFileReference())
                .ToList();

            return linkedElements;
        }
        public static IEnumerable<Document> GetLinkedDocuments(Document _doc)
        {
            var linkedfiles = GetLinkedFileReferences(_doc);

            var linkedFileNames = linkedfiles
                .Select(x => ModelPathUtils.ConvertModelPathToUserVisiblePath(x.GetAbsolutePath()))
                .ToList();

            return _doc.Application.Documents
                .Cast<Document>()
                .Where(doc => linkedFileNames
                    .Any(fileName => doc.PathName.Equals(fileName)));
        }
        public static IList<Wall> GetAllWalls(Document _doc)
        {
            IEnumerable<Document> linkedDocuments = GetLinkedDocuments(_doc);
            IList<Element> allWalls = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .ToList();

            foreach (Document linkeddoc in linkedDocuments)
            {
                IList<Element> wallsFromLink = new FilteredElementCollector(linkeddoc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .ToList();

                foreach (Element item in wallsFromLink)
                    allWalls.Add(item);

            }

            return allWalls as List<Wall>;
        }
    }
}
