using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using View = Autodesk.Revit.DB.View;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    class PlaceOnSheets : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //Собираем шаблонные листы и сборки
            IList<string> sheetsTemplates = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Sheets)
                .WhereElementIsNotElementType()
                .Where(s => s.get_Parameter(BuiltInParameter.SHEET_NUMBER).AsString().StartsWith("Панель"))
                .Select(s => s.Name)
                .ToList();
            IList<string> createdAssemblies = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Assemblies)
                .WhereElementIsNotElementType()
                .Select(a => a.Name)
                .ToList();
            IList<ViewSheet> sheetsInProject = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Sheets)
                .WhereElementIsNotElementType()
                .Cast<ViewSheet>()
                .ToList();
            int sheetIndex = 1;
            foreach (ViewSheet sheet in sheetsInProject)
            {
                if (createdAssemblies.Contains(sheet.Name))
                {
                    createdAssemblies.Remove(sheet.Name);
                }
                //Поиск последнего номера листа
                if (sheet.SheetNumber.StartsWith("П-"))
                {
                    int num = int.Parse(sheet.SheetNumber.Substring(2));
                    if (num >= sheetIndex)
                    {
                        sheetIndex = num + 1;
                    }
                }
            }
            sheetsTemplates = sheetsTemplates.OrderBy(q => q).ToList();
            createdAssemblies = createdAssemblies.OrderBy(q => q).ToList();

            //Вызываем окно
            var uiSheets = new UserInterfaceSheets(createdAssemblies, sheetsTemplates);
            bool tdRes = (bool)uiSheets.ShowDialog();

            if (tdRes == false)
            {
                return Result.Cancelled;
            }
            else
            {
                if (uiSheets.selectedSheetTemplate == null)
                {
                    MessageBox.Show("Выберите шаблон листа!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Result.Cancelled;
                }
                if (uiSheets.selectedAssemblies.Count == 0)
                {
                    MessageBox.Show("Выберите сборки!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Result.Cancelled;
                }

                //Получаем элементы видов и лист
                ViewSheet sheetTemplate = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Sheets)
                .WhereElementIsNotElementType()
                .Where(s => s.Name.Equals(uiSheets.selectedSheetTemplate))
                .Cast<ViewSheet>()
                .First();
                IList<View> allViews = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Views)
                .WhereElementIsNotElementType()
                .Cast<View>()
                .ToList();

                //Копируем лист и заменяем виды
                IList<string> selectedAssemblies = uiSheets.selectedAssemblies;
                selectedAssemblies = selectedAssemblies.OrderBy(q => q).ToList();
                bool dontShowAllCreatedViews = false;
                string allCreatedViewsString = "Листы созданы:\n";
                foreach (string assembly in selectedAssemblies)
                {
                    try
                    {
                        View viewPlan = null;
                        View viewFront = null;
                        View viewSection = null;
                        foreach (View view in allViews)
                        {
                            if (view.Name.Equals("План - " + assembly))
                            {
                                viewPlan = view;
                            }
                            if (view.Name.Equals("Вид спереди - " + assembly))
                            {
                                viewFront = view;
                            }
                            if (view.Name.Equals("Разрез - " + assembly))
                            {
                                viewSection = view;
                            }
                        }

                        using (Transaction transaction = new Transaction(doc))
                        {
                            transaction.Start(assembly + " - Создание листа");

                            IList<ElementId> placedViewportIds = sheetTemplate.GetAllViewports() as IList<ElementId>;
                            if (!placedViewportIds.Any())
                            {
                                MessageBox.Show("На шаблоне листа нет видов!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return Result.Cancelled;
                            }
                            IList<Viewport> placedViewports = new List<Viewport>();
                            foreach (ElementId viewportId in placedViewportIds)
                            {
                                placedViewports.Add(doc.GetElement(viewportId) as Viewport);
                            }

                            Element titleBlockTemplate = new FilteredElementCollector(doc)
                                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                .WhereElementIsNotElementType()
                                .Where(t => t.get_Parameter(BuiltInParameter.SHEET_NUMBER).AsString().Equals(sheetTemplate.SheetNumber))
                                .First();
                            ViewSheet newSheet = ViewSheet.Create(doc, titleBlockTemplate.GetTypeId());
                            newSheet.Name = assembly;
                            newSheet.SheetNumber = "П-" + sheetIndex.ToString();
                            Element titleBlock = new FilteredElementCollector(doc)
                                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                .WhereElementIsNotElementType()
                                .Where(t => t.get_Parameter(BuiltInParameter.SHEET_NUMBER).AsString().Equals(newSheet.SheetNumber))
                                .First();
                            BoundingBoxXYZ titleBlockLocation = titleBlockTemplate.get_BoundingBox(doc.GetElement(titleBlockTemplate.OwnerViewId) as View);
                            ElementTransformUtils.MoveElement(doc, titleBlock.Id, new XYZ(titleBlockLocation.Max.X, titleBlockLocation.Min.Y, titleBlockLocation.Min.Z));

                            byte n = 0;
                            foreach (Viewport viewport in placedViewports)
                            {
                                BoundingBoxXYZ xyzLocation = viewport.get_BoundingBox(doc.GetElement(viewport.OwnerViewId) as View);
                                XYZ xyzPosition = (xyzLocation.Max + xyzLocation.Min) / 2.0;
                                if (doc.GetElement(viewport.ViewId).Name.StartsWith("План"))
                                {
                                    if (viewPlan == null)
                                    {
                                        MessageBox.Show("Не найден вид - \"План - " + assembly + "\"", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return Result.Cancelled;
                                    }
                                    else
                                    {
                                        Viewport newViewport = Viewport.Create(doc, newSheet.Id, viewPlan.Id, xyzPosition);
                                        n++;
                                    }
                                }
                                if (doc.GetElement(viewport.ViewId).Name.StartsWith("Вид спереди"))
                                {
                                    if (viewFront == null)
                                    {
                                        MessageBox.Show("Не найден вид - \"Вид спереди - " + assembly + "\"", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return Result.Cancelled;
                                    }
                                    else
                                    {
                                        Viewport newViewport = Viewport.Create(doc, newSheet.Id, viewFront.Id, xyzPosition);
                                        n++;
                                    }
                                }
                                if (doc.GetElement(viewport.ViewId).Name.StartsWith("Разрез"))
                                {
                                    if (viewSection == null)
                                    {
                                        MessageBox.Show("Не найден вид - \"Разрез - " + assembly + "\"", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return Result.Cancelled;
                                    }
                                    else
                                    {
                                        Viewport newViewport = Viewport.Create(doc, newSheet.Id, viewSection.Id, xyzPosition);
                                        n++;
                                    }
                                }
                            }

                            //Проверяем пересекаются ли видовые экраны
                            if (viewFront != null && viewSection != null && viewPlan != null)
                            {
                                IList<ElementId> placedViewportIdsCheck = newSheet.GetAllViewports() as IList<ElementId>;
                                IList<Viewport> placedViewportsCheck = new List<Viewport>();
                                foreach (ElementId viewportId in placedViewportIdsCheck)
                                {
                                    placedViewportsCheck.Add(doc.GetElement(viewportId) as Viewport);
                                }
                                Viewport planCheck = null;
                                Viewport frontCheck = null;
                                Viewport sectionCheck = null;
                                foreach (Viewport viewport in placedViewportsCheck)
                                {
                                    if (doc.GetElement(viewport.ViewId).Name.StartsWith("План"))
                                    {
                                        planCheck = viewport;
                                    }
                                    if (doc.GetElement(viewport.ViewId).Name.StartsWith("Вид спереди"))
                                    {
                                        frontCheck = viewport;
                                    }
                                    if (doc.GetElement(viewport.ViewId).Name.StartsWith("Разрез"))
                                    {
                                        sectionCheck = viewport;
                                    }
                                }
                                ElementTransformUtils.MoveElement(doc, sectionCheck.Id, new XYZ(frontCheck.get_BoundingBox(doc.GetElement(frontCheck.OwnerViewId) as View).Max.X - sectionCheck.get_BoundingBox(doc.GetElement(sectionCheck.OwnerViewId) as View).Min.X + 10 / 304.8, 0, 0));
                                ElementTransformUtils.MoveElement(doc, planCheck.Id, new XYZ(0, frontCheck.get_BoundingBox(doc.GetElement(frontCheck.OwnerViewId) as View).Min.Y - planCheck.get_BoundingBox(doc.GetElement(planCheck.OwnerViewId) as View).Max.Y - 10 / 304.8, 0));
                                ElementTransformUtils.MoveElement(doc, titleBlock.Id, new XYZ(-((titleBlock.Location as LocationPoint).Point.X - titleBlock.get_Parameter(BuiltInParameter.SHEET_WIDTH).AsDouble() - frontCheck.get_BoundingBox(doc.GetElement(frontCheck.OwnerViewId) as View).Min.X + 30 / 304.8), 0, 0));   
                            }
                                
                            if (n == 0)
                            {
                                MessageBox.Show("На шаблоне листа нет необходимых видов!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return Result.Cancelled;
                            }

                            transaction.Commit();
                            sheetIndex++;
                            allCreatedViewsString = allCreatedViewsString + "- " + assembly + "\n";
                        }
                    }
                    catch
                    {
                        using (Transaction transaction = new Transaction(doc))
                        {
                            MessageBox.Show("Для сборки " + assembly + " не будет создан лист. Виды уже размещены на листе.", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            if (selectedAssemblies.Count == 1)
                            {
                                dontShowAllCreatedViews = true;
                            }
                        }
                    }
                }

                if (dontShowAllCreatedViews == false)
                {
                    MessageBox.Show(allCreatedViewsString, "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                return Result.Succeeded;
            }
        }
    }
}
