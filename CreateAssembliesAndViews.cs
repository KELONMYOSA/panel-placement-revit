using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using View = Autodesk.Revit.DB.View;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    class CreateAssembliesAndViews : IExternalCommand
    {
        public static IList<string> unusedTypesOfPanels = new List<string>();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            unusedTypesOfPanels.Clear();
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //Поиск начальной координаты для вставки примеров типоразмера
            XYZ startPlacementPoint = new XYZ(-30000 / 304.8, -30000 / 304.8, 0);
            IList<FamilyInstance> allPlacedTemplatePanels = new List<FamilyInstance>();
            try
            {
                allPlacedTemplatePanels = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(p => p.Symbol.FamilyName.Contains("Панель") && p.LookupParameter("Не включать в спецификацию").AsInteger().Equals(1))
                .ToList();
            }
            catch
            {
                MessageBox.Show("У панелей отсутствует параметр \"Не включать в спецификацию\"!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return Result.Cancelled;
            }
            bool found = false;
            foreach (FamilyInstance panel in allPlacedTemplatePanels)
            {
                if ((panel.Location as LocationPoint).Point.Y < startPlacementPoint.Y)
                {
                    startPlacementPoint = (panel.Location as LocationPoint).Point;
                    found = true;
                    
                }
                if (Math.Round((panel.Location as LocationPoint).Point.Y, 3) == Math.Round(startPlacementPoint.Y, 3))
                {
                    if ((panel.Location as LocationPoint).Point.X <= startPlacementPoint.X)
                    {
                        startPlacementPoint = (panel.Location as LocationPoint).Point;
                        found = true;
                    }
                }
            }
            if (found)
            {
                if (Math.Round(startPlacementPoint.X, 3) == Math.Round(-50000 / 304.8, 3))
                {
                    startPlacementPoint = new XYZ(-30000 / 304.8, startPlacementPoint.Y - 5000 / 304.8, 0);
                }
                else
                {
                    startPlacementPoint = new XYZ(startPlacementPoint.X - 5000 / 304.8, startPlacementPoint.Y, 0);
                }
            }

            //Собираем не использованные и не пустые типоразмеры
            IList<string> createdAssemblies = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Assemblies)
                .Select(a => a.Name)
                .ToList();
            IList<string> typesOfPanels = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .Where(p => p.FamilyName.Contains("Панель"))
                .Select(p => p.Name)
                .ToList();
            IList<string> emptyTypesOfPanels = new List<string>();
            foreach (string type in typesOfPanels)
            {
                IList<Element> allElementsOfType = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType()
                    .Where(p => (p as FamilyInstance).Symbol.Name.Equals(type))
                    .ToList();
                if (allElementsOfType.Count == 0)
                {
                    emptyTypesOfPanels.Add(type);
                }
            }
                foreach (string type in typesOfPanels)
            {
                if (!emptyTypesOfPanels.Contains(type))
                {
                    if (createdAssemblies.Count != 0)
                    {
                        if (!createdAssemblies.Contains(type))
                        {
                            unusedTypesOfPanels.Add(type);
                        }
                    }
                    else
                    {
                        unusedTypesOfPanels.Add(type);
                    }
                } 
            }
            unusedTypesOfPanels = unusedTypesOfPanels.OrderBy(q => q).ToList();

            //Вызываем окно с типами панелей
            var uiAssemblies = new UserInterfaceAssemblies(unusedTypesOfPanels);
            bool tdRes = (bool)uiAssemblies.ShowDialog();

            if (tdRes == false)
            {
                return Result.Cancelled;
            }
            else
            {
                IList<string> selectedTypes = new List<string>();
                if (uiAssemblies.assembliesSelectionMode == true)
                {
                    Selection sel = commandData.Application.ActiveUIDocument.Selection;
                    StructuralFramingSelectionFilter structuralFramingSelectionFilter = new StructuralFramingSelectionFilter();
                    IList<Reference> structuralFramingRefList = null;
                    try
                    {
                        structuralFramingRefList = sel.PickObjects(ObjectType.Element, structuralFramingSelectionFilter, "Выберите стены!");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                    foreach (Reference refElem in structuralFramingRefList)
                    {
                        selectedTypes.Add((doc.GetElement(refElem).Name));
                    }
                }
                else
                {
                   selectedTypes = uiAssemblies.selectedTypes;
                }

                if (selectedTypes.Count == 0)
                {
                    MessageBox.Show("Выберите типы панелей!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Result.Cancelled;
                }

                //Собираем шаблоны видов
                IList<View> viewTemplates = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate && v.Name.StartsWith("Панель"))
                .ToList();
                IList<string> viewTemplateNames = new List<string>();
                foreach (View viewTemplate in viewTemplates)
                {
                    viewTemplateNames.Add(viewTemplate.Name);
                }
                viewTemplateNames = viewTemplateNames.OrderBy(q => q).ToList();

                //Вызываем окно выбора шаблонов вида
                var uiAssembliesViews = new UserInterfaceViews(viewTemplateNames);
                bool tdResViews = (bool)uiAssembliesViews.ShowDialog();
                if (tdResViews == false)
                {
                    return Result.Cancelled;
                }
                else
                {
                    double offsetTextPlan;
                    double offsetTextFrontAndSection;
                    try
                    {
                        offsetTextPlan = double.Parse(uiAssembliesViews.selectedOffsetPlan) / 304.8;
                        offsetTextFrontAndSection = double.Parse(uiAssembliesViews.selectedOffsetFrontAndSection) / 304.8;
                    }
                    catch
                    {
                        MessageBox.Show("Введите корректные значения отступа заголовка!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return Result.Cancelled;
                    }

                    //Получаем id выбранных шаблонов
                    ElementId templatePlan = ElementId.InvalidElementId;
                    ElementId templateFront = ElementId.InvalidElementId;
                    ElementId templateSection = ElementId.InvalidElementId;
                    string selectedTemplatePlan = uiAssembliesViews.selectedTemplatePlan;
                    string selectedTemplateFront = uiAssembliesViews.selectedTemplateFront;
                    string selectedTemplateSection = uiAssembliesViews.selectedTemplateSection;
                    foreach (View view in viewTemplates)
                    {
                        if (view.Name.Equals(selectedTemplatePlan))
                        {
                            templatePlan = view.Id;
                        }
                        if (view.Name.Equals(selectedTemplateFront))
                        {
                            templateFront = view.Id;
                        }
                        if (view.Name.Equals(selectedTemplateSection))
                        {
                            templateSection = view.Id;
                        }
                    }
                    if (templatePlan.Equals(ElementId.InvalidElementId) || templateFront.Equals(ElementId.InvalidElementId) || templateSection.Equals(ElementId.InvalidElementId))
                    {
                        MessageBox.Show("Необходимо выбрать шаблоны видов!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return Result.Cancelled;
                    }

                    string allCreatedViewsString = "Виды созданы:\n";
                    //Создаем сборки и виды
                    foreach (string type in selectedTypes)
                    {
                        using (Transaction transaction = new Transaction(doc))
                        {
                            transaction.Start(type + " - Создание шаблонного экземпляра типа");
                            FamilySymbol currentType = new FilteredElementCollector(doc)
                                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                                .WhereElementIsElementType()
                                .Where(p => p.Name.Equals(type))
                                .Select(p => p as FamilySymbol)
                                .First();
                            FamilyInstance createdElement = doc.Create.NewFamilyInstance(startPlacementPoint, currentType, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            createdElement.LookupParameter("Не включать в спецификацию").Set(1);
                            transaction.Commit();
                            
                            double offsetPanelPlan = currentType.LookupParameter("ADSK_Размер_Толщина").AsDouble() / 2;
                            double offsetPanelFrontAndSection = currentType.LookupParameter("ADSK_Размер_Высота").AsDouble() / 2;

                            if (Math.Round(startPlacementPoint.X, 3) == Math.Round(-50000 / 304.8, 3))
                            {
                                startPlacementPoint = new XYZ(-30000 / 304.8, startPlacementPoint.Y - 5000 / 304.8, 0);
                            }
                            else
                            {
                                startPlacementPoint = new XYZ(startPlacementPoint.X - 5000 / 304.8, startPlacementPoint.Y, 0);
                            }

                            IList<ElementId> elementIds = new List<ElementId>();
                            elementIds.Add(createdElement.Id);

                            AssemblyInstance assemblyInstance = null;
                        
                            ElementId categoryId = doc.GetElement(elementIds.First()).Category.Id;
                            if (AssemblyInstance.IsValidNamingCategory(doc, categoryId, elementIds))
                            {
                                transaction.Start(type + " - Создание сборки");
                                assemblyInstance = AssemblyInstance.Create(doc, elementIds, categoryId);
                                transaction.Commit();

                                if (transaction.GetStatus() == TransactionStatus.Committed)
                                {
                                    transaction.Start(type + " - Назначение имени сборки");
                                    assemblyInstance.AssemblyTypeName = type;
                                    transaction.Commit();
                                }
                            }
                            if (assemblyInstance.AllowsAssemblyViewCreation())
                            {
                                if (transaction.GetStatus() == TransactionStatus.Committed)
                                {
                                    transaction.Start(type + " - Создание вида сборки");

                                    ViewSection viewPlan = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, AssemblyDetailViewOrientation.HorizontalDetail, templatePlan, true);
                                    ViewSection viewFront = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, AssemblyDetailViewOrientation.ElevationFront, templateFront, true);
                                    ViewSection viewSection = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, AssemblyDetailViewOrientation.DetailSectionB, templateSection, true);

                                    viewPlan.Name = "План - " + type;
                                    viewFront.Name = "Вид спереди - " + type;
                                    viewSection.Name = "Разрез - " + type;

                                    transaction.Commit();

                                    if (transaction.GetStatus() == TransactionStatus.Committed)
                                    {
                                        transaction.Start(type + " - Размещение заголовка");

                                        Element textNoteType = null;
                                        try
                                        {
                                            textNoteType = new FilteredElementCollector(doc)
                                            .OfClass(typeof(TextNoteType))
                                            .WhereElementIsElementType()
                                            .Where(p => p.Name.StartsWith("Панель"))
                                            .First();
                                        } catch
                                        {
                                            MessageBox.Show("Не обнаружен тип текста! Название типа должно начинаться с \"Панель\".", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            return Result.Cancelled;
                                        }
                                        XYZ planTextXYZ = viewPlan.Origin + new XYZ((viewPlan.get_BoundingBox(doc.GetElement(viewPlan.GetPrimaryViewId()) as View).Max.X - viewPlan.get_BoundingBox(doc.GetElement(viewPlan.GetPrimaryViewId()) as View).Min.X) / 2,
                                            (viewPlan.get_BoundingBox(doc.GetElement(viewPlan.GetPrimaryViewId()) as View).Max.Y - viewPlan.get_BoundingBox(doc.GetElement(viewPlan.GetPrimaryViewId()) as View).Min.Y) / 2 + offsetPanelPlan + offsetTextPlan, 0);
                                        TextNote planTextNote = TextNote.Create(doc, viewPlan.Id, planTextXYZ, "1-1", textNoteType.Id);
                                        XYZ frontTextXYZ = new XYZ(viewSection.Origin.X, viewFront.Origin.Y, viewFront.Origin.Z + (viewFront.get_BoundingBox(doc.GetElement(viewFront.GetPrimaryViewId()) as View).Max.Z - viewFront.get_BoundingBox(doc.GetElement(viewFront.GetPrimaryViewId()) as View).Min.Z) / 2 + offsetPanelFrontAndSection + offsetTextFrontAndSection);
                                        TextNote frontTextNote = TextNote.Create(doc, viewFront.Id, frontTextXYZ, type, textNoteType.Id);
                                        XYZ sectionTextXYZ = viewFront.Origin + new XYZ(0, (viewFront.get_BoundingBox(doc.GetElement(viewFront.GetPrimaryViewId()) as View).Max.Y - viewFront.get_BoundingBox(doc.GetElement(viewFront.GetPrimaryViewId()) as View).Min.Y) / 2,
                                            (viewFront.get_BoundingBox(doc.GetElement(viewFront.GetPrimaryViewId()) as View).Max.Z - viewFront.get_BoundingBox(doc.GetElement(viewFront.GetPrimaryViewId()) as View).Min.Z) / 2 + offsetPanelFrontAndSection + offsetTextFrontAndSection);
                                        TextNote sectionTextNote = TextNote.Create(doc, viewSection.Id, sectionTextXYZ, "2-2", textNoteType.Id);

                                        transaction.Commit();
                                        
                                        if (transaction.GetStatus() == TransactionStatus.Committed)
                                        {
                                            transaction.Start(type + " - Текст заголовка");

                                            XYZ planTextOffset = new XYZ((planTextNote.get_BoundingBox(doc.GetElement(planTextNote.OwnerViewId) as View).Min.X - planTextNote.get_BoundingBox(doc.GetElement(planTextNote.OwnerViewId) as View).Max.X) / 2,
                                            (planTextNote.get_BoundingBox(doc.GetElement(planTextNote.OwnerViewId) as View).Max.Y - planTextNote.get_BoundingBox(doc.GetElement(planTextNote.OwnerViewId) as View).Min.Y) / 2, 0);
                                            ElementTransformUtils.MoveElement(doc, planTextNote.Id, planTextOffset);
                                            XYZ frontTextOffset = new XYZ((frontTextNote.get_BoundingBox(doc.GetElement(frontTextNote.OwnerViewId) as View).Min.X - frontTextNote.get_BoundingBox(doc.GetElement(frontTextNote.OwnerViewId) as View).Max.X) / 2,
                                            0, (frontTextNote.get_BoundingBox(doc.GetElement(frontTextNote.OwnerViewId) as View).Max.Z - frontTextNote.get_BoundingBox(doc.GetElement(frontTextNote.OwnerViewId) as View).Min.Z) / 2);
                                            ElementTransformUtils.MoveElement(doc, frontTextNote.Id, frontTextOffset);
                                            XYZ sectionTextOffset = new XYZ(0, (sectionTextNote.get_BoundingBox(doc.GetElement(sectionTextNote.OwnerViewId) as View).Min.Y - sectionTextNote.get_BoundingBox(doc.GetElement(sectionTextNote.OwnerViewId) as View).Max.Y) / 4,
                                            (sectionTextNote.get_BoundingBox(doc.GetElement(sectionTextNote.OwnerViewId) as View).Max.Z - sectionTextNote.get_BoundingBox(doc.GetElement(sectionTextNote.OwnerViewId) as View).Min.Z) / 2);
                                            ElementTransformUtils.MoveElement(doc, sectionTextNote.Id, sectionTextOffset);

                                            transaction.Commit();
                                            allCreatedViewsString = allCreatedViewsString + "- " + type + "\n";
                                        }
                                    }
                                }
                            }      
                        }
                    }
                    MessageBox.Show(allCreatedViewsString, "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            return Result.Succeeded;        
        }
    }
}
