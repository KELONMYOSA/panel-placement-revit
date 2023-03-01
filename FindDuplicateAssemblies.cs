using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    class FindDuplicateAssemblies : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //Поиск семейств, которые содержат "Панель"
            IList<Family> familysOfPanels = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .Where(p => p.FamilyName.Contains("Панель"))
                .Select(p => p.Family)
                .Distinct(new FamilyComparer())
                .ToList();

            string outListOfTypes = "Одинаковые типы панелей:\n";

            foreach (Family family in familysOfPanels)
            {
                //Получаем все типы панелей
                IList<FamilySymbol> allTypesInFamily = new List<FamilySymbol>();
                foreach (ElementId typeId in family.GetFamilySymbolIds())
                {
                    allTypesInFamily.Add(doc.GetElement(typeId) as FamilySymbol);
                }
                allTypesInFamily = allTypesInFamily.OrderBy(q => q.Name).ToList();

                //Получаем список хэш-кодов для всех типов
                IList<int> hashOfTypes = new List<int>();
                foreach (FamilySymbol type in allTypesInFamily)
                {
                    int hash = 0;
                    ParameterSet typeParams = type.Parameters;
                    foreach (Parameter param in typeParams)
                    {
                        if (!param.UserModifiable) continue;
                        
                        try
                        {
                            BuiltInParameter paramAsBuiltIn = (param.Definition as InternalDefinition).BuiltInParameter;
                            if (paramAsBuiltIn == BuiltInParameter.ELEM_TYPE_PARAM ||
                                paramAsBuiltIn == BuiltInParameter.ALL_MODEL_TYPE_NAME ||
                                paramAsBuiltIn == BuiltInParameter.SYMBOL_ID_PARAM ||
                                paramAsBuiltIn == BuiltInParameter.INSTANCE_FREE_HOST_PARAM ||
                                paramAsBuiltIn == BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM ||
                                paramAsBuiltIn == BuiltInParameter.SCHEDULE_LEVEL_PARAM ||
                                paramAsBuiltIn == BuiltInParameter.FAMILY_LEVEL_PARAM ||
                                paramAsBuiltIn == BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM ||
                                paramAsBuiltIn == BuiltInParameter.STRUCTURAL_REFERENCE_LEVEL_ELEVATION) continue;
                        } catch { }
                        
                        string paramValue = param.AsValueString();
                        if (paramValue == null)
                        {
                            paramValue = param.AsString();
                            if (paramValue == null)
                            {
                                continue;
                            }
                        }
                        hash += (paramValue + param.Definition.Name).GetHashCode();
                    }
                    hashOfTypes.Add(hash);
                }

                //Группируем типы с одинаковыми хэшами
                IList<string> allTypesInFamilyNames = allTypesInFamily.Select(t => t.Name).ToList();
                Dictionary<string, int> typeNameAndHash = allTypesInFamilyNames.Zip(hashOfTypes, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
                Dictionary<List<string>, int> groupedTypeNameAndHash = typeNameAndHash.GroupBy(r => r.Value).ToDictionary(t => t.Select(r => r.Key).ToList(), t => t.Key);

                //Добавляем в список типов одинаковые
                foreach (List<string> list in groupedTypeNameAndHash.Keys.ToList())
                {
                    if (list.Count > 1)
                    {
                        string addString = "- ";

                        foreach (string typeName in list)
                        {
                            addString += typeName + ", ";
                        }

                        addString = addString.Remove(addString.Length - 2);
                        addString += "\n";

                        outListOfTypes += addString;
                    }
                }
            }

            if (outListOfTypes == "Одинаковые типы панелей:\n")
            {
                MessageBox.Show("Не найдены дубликаты сборок в проекте!", "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(outListOfTypes, "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return Result.Succeeded;
        }
    }

    class FamilyComparer : IEqualityComparer<Family>
    {
        public bool Equals(Family x, Family y)
        {
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(Family obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}