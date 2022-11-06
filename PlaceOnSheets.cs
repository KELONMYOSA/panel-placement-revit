using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

                            ViewSheet newSheet;
                            try
                            {
                                newSheet = doc.GetElement(sheetTemplate.Duplicate(SheetDuplicateOption.DuplicateSheetWithViewsAndDetailing)) as ViewSheet;
                            }
                            catch
                            {
                                MessageBox.Show("На шаблоне листа нет видов!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return Result.Cancelled;
                            }
                            newSheet.Name = assembly;
                            newSheet.SheetNumber = "П-" + sheetIndex.ToString();
                            IList<ElementId> placedViewportIds = newSheet.GetAllViewports() as IList<ElementId>;
                            IList<Viewport> placedViewports = new List<Viewport>();
                            foreach (ElementId viewportId in placedViewportIds)
                            {
                                placedViewports.Add(doc.GetElement(viewportId) as Viewport);
                            }
                            byte n = 0;
                            foreach (Viewport viewport in placedViewports)
                            {
                                if (doc.GetElement(viewport.ViewId).Name.StartsWith("План"))
                                {
                                    if (viewPlan == null)
                                    {
                                        MessageBox.Show("Не найден вид - \"План - " + assembly + "\"", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return Result.Cancelled;
                                    }
                                    else
                                    {
                                        viewport.ViewId = viewPlan.Id;
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
                                        viewport.ViewId = viewFront.Id;
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
                                        viewport.ViewId = viewSection.Id;
                                        n++;
                                    }
                                }
                            }
                            if (n == 0)
                            {
                                MessageBox.Show("На шаблоне листа нет необходимых видов!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return Result.Cancelled;
                            }

                            transaction.Commit();
                            sheetIndex++;
                        }
                    }
                    catch
                    {
                        using (Transaction transaction = new Transaction(doc))
                        {
                            transaction.Start(assembly + " - Создание листа");

                            ViewSheet newSheet = doc.GetElement(sheetTemplate.Duplicate(SheetDuplicateOption.DuplicateSheetWithViewsAndDetailing)) as ViewSheet;
                            newSheet.Name = assembly;
                            newSheet.SheetNumber = "П-" + sheetIndex.ToString();

                            transaction.Commit();
                            sheetIndex++;
                        }
                    }
                }

                return Result.Succeeded;
            }
        }
    }
}
