using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    class CreateAssembliesAndViews : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //Собираем не использованные типоразмеры
            IList<string> unusedTypesOfPanels = new List<string>();
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
            foreach (string type in typesOfPanels)
            {
                if (createdAssemblies.Count != 0)
                {
                    byte n = 0;
                    foreach (string assemble in createdAssemblies)
                    {
                        if (assemble.Equals(type))
                        {
                            n++;
                        }
                    }
                    if (n == 0)
                    {
                        unusedTypesOfPanels.Add(type);
                    }
                }
                else
                {
                    unusedTypesOfPanels.Add(type);
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
                IList<string> selectedTypes = uiAssemblies.selectedTypes;

                if (selectedTypes.Count == 0)
                {
                    MessageBox.Show("Выберите типы панелей!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Result.Cancelled;
                }

                //Вызываем окно выбора шаблонов вида
                var uiAssembliesViews = new UserInterfaceViews();
                bool tdResViews = (bool)uiAssembliesViews.ShowDialog();

                if (tdResViews == false)
                {
                    return Result.Cancelled;
                }
                else
                {
                    //Создаем сборки
                    IList<Element> assemblies = new List<Element>();

                    foreach (string type in selectedTypes)
                    {
                        IList<Element> allElementsOfType = new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_StructuralFraming)
                            .WhereElementIsNotElementType()
                            .Where(p => (p as FamilyInstance).Symbol.Name.Equals(type))
                            .ToList();
                        IList<ElementId> elementIds = new List<ElementId>();
                        foreach (Element element in allElementsOfType)
                        {
                            elementIds.Add(element.Id);
                        }

                        AssemblyInstance assemblyInstance = null;
                        using (Transaction transaction = new Transaction(doc))
                        {
                            ElementId categoryId = doc.GetElement(elementIds.First()).Category.Id;
                            if (AssemblyInstance.IsValidNamingCategory(doc, categoryId, elementIds))
                            {
                                transaction.Start("Create Assembly Instance");
                                assemblyInstance = AssemblyInstance.Create(doc, elementIds, categoryId);
                                transaction.Commit();

                                if (transaction.GetStatus() == TransactionStatus.Committed)
                                {
                                    transaction.Start("Set Assembly Name");
                                    assemblyInstance.AssemblyTypeName = type;
                                    transaction.Commit();
                                }
                            }
                        }
                    }
                }
            }
            return Result.Succeeded;        
        }
    }
}
