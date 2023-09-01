using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
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

                XYZ pointA = (symmetryLine.Location as LocationCurve).Curve.GetEndPoint(0);
                XYZ pointB = (symmetryLine.Location as LocationCurve).Curve.GetEndPoint(1);
                XYZ pointC = pointB + new XYZ(0, 0, 1);
                Plane plane = Plane.CreateByThreePoints(pointA, pointB, pointC);

                using (Transaction transaction = new Transaction(doc))
                {
                    // Создание зеркальных типов
                    IList<ElementType> newPanelTypes = new List<ElementType>();
                    foreach (ElementType panelType in panelTypes)
                    {
                        ElementType newPanelType = new FilteredElementCollector(doc)
                                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                                .WhereElementIsElementType()
                                .Cast<ElementType>()
                                .Where(p => p.Name.Equals("З-" + panelType.Name))
                                .FirstOrDefault();

                        if (newPanelType == null)
                        {
                            transaction.Start("Создание типа З-" + panelType.Name);

                            newPanelType = panelType.Duplicate("З-" + panelType.Name);
                            ChangeParamsFromCsv(panelType, newPanelType, tablePath);

                            transaction.Commit();
                        }

                        newPanelTypes.Add(newPanelType);
                    }

                    // Отзеркаливание панелей
                    IList<ElementId> selectedPanelIds = selectedPanels.Select(p => p.Id).ToList();
                    
                    transaction.Start("Отзеркаливание панелей");

                    IList<ElementId> mirroredPanelIds = ElementTransformUtils.MirrorElements(doc, selectedPanelIds, plane, true);
                    foreach (ElementId mirroredPanelId in mirroredPanelIds)
                    {
                        Element mirroredPanel = doc.GetElement(mirroredPanelId);
                        string mirroredPanelTypeName = (doc.GetElement(mirroredPanel.GetTypeId()) as ElementType).Name;
                        ElementType newType = newPanelTypes.Where(t => t.Name.Equals("З-" + mirroredPanelTypeName)).FirstOrDefault();
                        mirroredPanel.ChangeTypeId(newType.Id);
                    }

                    transaction.Commit();
                }
                
                return Result.Succeeded;
            }    
        }

        private static void ChangeParamsFromCsv(ElementType oldType, ElementType newType, string filePath)
        {

        }
    }
}