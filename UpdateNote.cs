using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
    class UpdateNote : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //Собираем параметры панели
            IList<string> createdAssemblies = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Assemblies)
                .WhereElementIsNotElementType()
                .Select(a => a.Name)
                .ToList();
            IList<string> sheetsInProject = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Sheets)
                .WhereElementIsNotElementType()
                .Select(s => s.Name)
                .Cast<string>()
                .ToList();
            IList<string> createdAssembliesList = new List<string>();
            foreach (string assembly in createdAssemblies)
            {
                if (sheetsInProject.Contains(assembly))
                {
                    createdAssembliesList.Add(assembly);
                }
            }

            IList<string> paramListForAssemble = new List<string>();
            foreach (string assembly in createdAssemblies)
            {
                AssemblyInstance assemblyElem = new FilteredElementCollector(doc)
                                .OfCategory(BuiltInCategory.OST_Assemblies)
                                .WhereElementIsNotElementType()
                                .Where(a => a.Name == assembly)
                                .Cast<AssemblyInstance>()
                                .First();
                ParameterSet paramsForAssemble = doc.GetElement(assemblyElem.GetMemberIds().First()).Parameters;
                foreach (Parameter param in paramsForAssemble)
                {
                    if (!paramListForAssemble.Contains(param.Definition.Name))
                    {
                        paramListForAssemble.Add(param.Definition.Name);
                    }
                }

                ParameterSet paramsForAssembleType = (doc.GetElement(assemblyElem.GetMemberIds().First()) as FamilyInstance).Symbol.Parameters;
                foreach (Parameter param in paramsForAssembleType)
                {
                    if (!paramListForAssemble.Contains(param.Definition.Name))
                    {
                        paramListForAssemble.Add(param.Definition.Name);
                    }
                }
            }
            paramListForAssemble = paramListForAssemble.OrderBy(q => q).ToList();


            //Вызываем окно
            var uiSheets = new UserInterfaceNote(paramListForAssemble);
            bool tdRes = (bool)uiSheets.ShowDialog();

            if (tdRes == false)
            {
                return Result.Cancelled;
            }
            else
            {
                if (uiSheets.selectedParam == null)
                {
                    MessageBox.Show("Выберите параметр примечания!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Result.Cancelled;
                }

                //Получаем все листы П-*
                IList<ViewSheet> sheetsToUpdate = new FilteredElementCollector(doc)
                       .OfCategory(BuiltInCategory.OST_Sheets)
                       .WhereElementIsNotElementType()
                       .Cast<ViewSheet>()
                       .Where(s => s.SheetNumber.StartsWith("П-"))
                       .ToList();

                //Обновление примечания на листах
                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Обновление примечания на листах");

                    foreach (ViewSheet sheet in sheetsToUpdate)
                    {
                        try
                        {
                            //Поиск примечания на листе
                            TextNote textNoteToUpdate = new FilteredElementCollector(doc)
                                    .OfCategory(BuiltInCategory.OST_TextNotes)
                                    .WhereElementIsNotElementType()
                                    .Cast<TextNote>()
                                    .Where(t => t.OwnerViewId.Equals(sheet.Id))
                                    .Where(t => t.Text.StartsWith("Примечание:"))
                                    .First();

                            //Получаем значение параметра из панели на листе
                            Element firstAssemblyFromView = new FilteredElementCollector(doc, (doc.GetElement((sheet.GetAllViewports() as IList<ElementId>).First()) as Viewport).ViewId)
                                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                                    .WhereElementIsNotElementType()
                                    .First();
                            ParameterSet firstAssemblyFromViewParams = firstAssemblyFromView.Parameters;
                            Parameter selectedParam = null;
                            foreach (Parameter param in firstAssemblyFromViewParams)
                            {
                                if (param.Definition.Name == uiSheets.selectedParam)
                                {
                                    selectedParam = param;
                                    break;
                                }
                            }
                            string selectedParamInAssembly = null;
                            if (selectedParam == null)
                            {
                                ParameterSet firstAssemblyFromViewParamsType = (firstAssemblyFromView as FamilyInstance).Symbol.Parameters;
                                foreach (Parameter param in firstAssemblyFromViewParamsType)
                                {
                                    if (param.Definition.Name == uiSheets.selectedParam)
                                    {
                                        selectedParam = param;
                                        break;
                                    }
                                }
                                if (selectedParam != null)
                                {
                                    selectedParamInAssembly = selectedParam.AsValueString();
                                    if (selectedParamInAssembly == null)
                                    {
                                        selectedParamInAssembly = selectedParam.AsString();
                                    }
                                }
                            }
                            else
                            {
                                selectedParamInAssembly = selectedParam.AsValueString();
                                if (selectedParamInAssembly == null)
                                {
                                    selectedParamInAssembly = selectedParam.AsString();
                                }
                            }

                            //Изменяем примечание
                            if (selectedParamInAssembly != null)
                            {
                                textNoteToUpdate.Text = selectedParamInAssembly;
                            }
                        }
                        catch { }
                    }

                    transaction.Commit();
                }

                MessageBox.Show("Примечания были обновлены!", "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return Result.Succeeded;
            }
        }
    }
}
