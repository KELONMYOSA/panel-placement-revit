using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    class FindDuplicateAssemblies : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            DocumentSet docSet = commandData.Application.Application.Documents;
            IList<Document> docs = new List<Document>();
            IList<string> docTitles = new List<string>();
            foreach (Document document in docSet) { 
                docs.Add(document);
                docTitles.Add(document.Title);
            }
            docs.Remove(doc);
            docTitles.Remove(doc.Title);

            //Собираем параметры панели
            IList<Family> currentFamilysOfPanels = new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_StructuralFraming)
                            .WhereElementIsElementType()
                            .Cast<FamilySymbol>()
                            .Where(p => p.FamilyName.Contains("Панель"))
                            .Select(p => p.Family)
                            .Distinct(new FamilyComparer())
                            .ToList();

            IList<string> paramListForAssemble = new List<string>();
            foreach (Family family in currentFamilysOfPanels)
            {
                IList<FamilySymbol> currentAllTypesInFamily = new List<FamilySymbol>();
                foreach (ElementId typeId in family.GetFamilySymbolIds())
                {
                    FamilySymbol currentFamilyType = doc.GetElement(typeId) as FamilySymbol;
                    ParameterSet paramsInType = currentFamilyType.Parameters;
                    foreach (Parameter param in paramsInType)
                    {
                        if (!paramListForAssemble.Contains(param.Definition.Name))
                        {
                            paramListForAssemble.Add(param.Definition.Name);
                        }
                    }
                }
            }
            paramListForAssemble = paramListForAssemble.OrderBy(q => q).ToList();

            var ui = new UserInterfaceDuplicates(doc.Title, docTitles, paramListForAssemble);
            bool tdRes = (bool)ui.ShowDialog();

            if (tdRes == false)
            {
                return Result.Cancelled;
            }
            else
            { 
                int findMode = ui.findMode;
                string comparingParam = ui.comparingParam;
                IList<string> comparingParamsList = ui.comparingParamsList;
                IList<string> comparingDocsTitles = ui.comparingDocs;
                IList<Document> listOfCamparingDocs = docs.Where(x => comparingDocsTitles.Contains(x.Title)).ToList();
                listOfCamparingDocs.Add(doc);

                if (findMode == 1)
                {
                    Dictionary<string, Dictionary<List<string>, int>> hashAndTypeNameInDocs = new Dictionary<string, Dictionary<List<string>, int>>();

                    foreach (Document curDoc in listOfCamparingDocs)
                    {
                        //Поиск семейств, которые содержат "Панель"
                        IList<Family> familysOfPanels = new FilteredElementCollector(curDoc)
                            .OfCategory(BuiltInCategory.OST_StructuralFraming)
                            .WhereElementIsElementType()
                            .Cast<FamilySymbol>()
                            .Where(p => p.FamilyName.Contains("Панель"))
                            .Select(p => p.Family)
                            .Distinct(new FamilyComparer())
                            .ToList();
                        IList<int> hashOfTypes = new List<int>();
                        IList<string> allTypeNames = new List<string>();

                        foreach (Family family in familysOfPanels)
                        {
                            //Получаем все типы панелей
                            IList<FamilySymbol> allTypesInFamily = new List<FamilySymbol>();
                            foreach (ElementId typeId in family.GetFamilySymbolIds())
                            {
                                allTypesInFamily.Add(curDoc.GetElement(typeId) as FamilySymbol);
                            }
                            allTypesInFamily = allTypesInFamily.OrderBy(q => q.Name).ToList();

                            //Получаем список хэш-кодов для всех типов
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
                                allTypeNames.Add(type.Name);
                            }
                        }

                        //Группируем типы с одинаковыми хэшами
                        try
                        {
                            Dictionary<string, int> typeNameAndHash = allTypeNames.Zip(hashOfTypes, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
                            Dictionary<List<string>, int> groupedTypeNameAndHash = typeNameAndHash.GroupBy(r => r.Value).ToDictionary(t => t.Select(r => r.Key).ToList(), t => t.Key);
                            hashAndTypeNameInDocs.Add(curDoc.Title, groupedTypeNameAndHash);
                        }
                        catch
                        {
                            MessageBox.Show("Обнаружены дубликаты типоразмеров с одинаковым названием, но в разных семействах!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return Result.Cancelled;
                        }
                    }

                    string outListOfTypes = "Одинаковые типы панелей:\n";
                    if (hashAndTypeNameInDocs.Keys.Count != 1)
                    {
                        Dictionary<List<string>, int> firstDict = hashAndTypeNameInDocs[doc.Title];
                        hashAndTypeNameInDocs.Remove(doc.Title);
                        int nDuplicate = 0;

                        foreach (List<string> firstKey in firstDict.Keys)
                        {
                            int hash = firstDict[firstKey];
                            bool compareResult = false;
                            Dictionary<string, List<string>> typesInDocsToAdd= new Dictionary<string, List<string>>();
                            foreach (string key in hashAndTypeNameInDocs.Keys)
                            {
                                List<string> types = hashAndTypeNameInDocs[key].Where(x => x.Value == hash).FirstOrDefault().Key;
                                if (types == null) 
                                {
                                    compareResult = false;
                                    break; 
                                }
                                compareResult = true;
                                typesInDocsToAdd.Add(key, types);
                            }
                            if (compareResult)
                            {
                                nDuplicate++;
                                outListOfTypes += "\n" + nDuplicate + ".\n";
                                outListOfTypes += "- " + doc.Title + ": ";
                                foreach (string typeName in firstKey)
                                {
                                    outListOfTypes += typeName + ", ";
                                }
                                outListOfTypes = outListOfTypes.Remove(outListOfTypes.Length - 2);
                                outListOfTypes += "\n";
                                foreach (string docNameKey in typesInDocsToAdd.Keys)
                                {
                                    outListOfTypes += "- " + docNameKey + ": ";
                                    foreach (string typeName in typesInDocsToAdd[docNameKey])
                                    {
                                        outListOfTypes += typeName + ", ";
                                    }
                                    outListOfTypes = outListOfTypes.Remove(outListOfTypes.Length - 2);
                                    outListOfTypes += "\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (List<string> list in hashAndTypeNameInDocs[doc.Title].Keys.ToList())
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
                        MessageBox.Show("Не найдены дубликаты сборок!", "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        var uiResult = new UserInterfaceDuplicatesResults(outListOfTypes);
                        bool tdResult = (bool)uiResult.ShowDialog();
                    }

                    return Result.Succeeded;
                }
                else if (findMode == 2)
                {
                    Dictionary<string, Dictionary<List<string>, int>> hashAndTypeNameInDocs = new Dictionary<string, Dictionary<List<string>, int>>();

                    foreach (Document curDoc in listOfCamparingDocs)
                    {
                        //Поиск семейств, которые содержат "Панель"
                        IList<Family> familysOfPanels = new FilteredElementCollector(curDoc)
                            .OfCategory(BuiltInCategory.OST_StructuralFraming)
                            .WhereElementIsElementType()
                            .Cast<FamilySymbol>()
                            .Where(p => p.FamilyName.Contains("Панель"))
                            .Select(p => p.Family)
                            .Distinct(new FamilyComparer())
                            .ToList();
                        IList<int> hashOfTypes = new List<int>();
                        IList<string> allTypeNames = new List<string>();

                        foreach (Family family in familysOfPanels)
                        {
                            //Получаем все типы панелей
                            IList<FamilySymbol> allTypesInFamily = new List<FamilySymbol>();
                            foreach (ElementId typeId in family.GetFamilySymbolIds())
                            {
                                allTypesInFamily.Add(curDoc.GetElement(typeId) as FamilySymbol);
                            }
                            allTypesInFamily = allTypesInFamily.OrderBy(q => q.Name).ToList();

                            //Получаем список хэш-кодов для всех типов
                            foreach (FamilySymbol type in allTypesInFamily)
                            {
                                int hash = 0;
                                ParameterSet typeParams = type.Parameters;
                                foreach (Parameter param in typeParams)
                                {
                                    if (!comparingParamsList.Contains(param.Definition.Name)) { continue; }

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
                                if (hash != 0)
                                {
                                    hashOfTypes.Add(hash);
                                    allTypeNames.Add(type.Name);
                                }
                            }
                        }

                        //Группируем типы с одинаковыми хэшами
                        try
                        {
                            Dictionary<string, int> typeNameAndHash = allTypeNames.Zip(hashOfTypes, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
                            Dictionary<List<string>, int> groupedTypeNameAndHash = typeNameAndHash.GroupBy(r => r.Value).ToDictionary(t => t.Select(r => r.Key).ToList(), t => t.Key);
                            hashAndTypeNameInDocs.Add(curDoc.Title, groupedTypeNameAndHash);
                        }
                        catch
                        {
                            MessageBox.Show("Обнаружены дубликаты типоразмеров с одинаковым названием, но в разных семействах!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return Result.Cancelled;
                        }
                    }

                    string outListOfTypes = "Одинаковые типы панелей:\n";
                    if (hashAndTypeNameInDocs.Keys.Count != 1)
                    {
                        Dictionary<List<string>, int> firstDict = hashAndTypeNameInDocs[doc.Title];
                        hashAndTypeNameInDocs.Remove(doc.Title);
                        int nDuplicate = 0;

                        foreach (List<string> firstKey in firstDict.Keys)
                        {
                            int hash = firstDict[firstKey];
                            bool compareResult = false;
                            Dictionary<string, List<string>> typesInDocsToAdd = new Dictionary<string, List<string>>();
                            foreach (string key in hashAndTypeNameInDocs.Keys)
                            {
                                List<string> types = hashAndTypeNameInDocs[key].Where(x => x.Value == hash).FirstOrDefault().Key;
                                if (types == null)
                                {
                                    compareResult = false;
                                    break;
                                }
                                compareResult = true;
                                typesInDocsToAdd.Add(key, types);
                            }
                            if (compareResult)
                            {
                                nDuplicate++;
                                outListOfTypes += "\n" + nDuplicate + ".\n";
                                outListOfTypes += "- " + doc.Title + ": ";
                                foreach (string typeName in firstKey)
                                {
                                    outListOfTypes += typeName + ", ";
                                }
                                outListOfTypes = outListOfTypes.Remove(outListOfTypes.Length - 2);
                                outListOfTypes += "\n";
                                foreach (string docNameKey in typesInDocsToAdd.Keys)
                                {
                                    outListOfTypes += "- " + docNameKey + ": ";
                                    foreach (string typeName in typesInDocsToAdd[docNameKey])
                                    {
                                        outListOfTypes += typeName + ", ";
                                    }
                                    outListOfTypes = outListOfTypes.Remove(outListOfTypes.Length - 2);
                                    outListOfTypes += "\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (List<string> list in hashAndTypeNameInDocs[doc.Title].Keys.ToList())
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
                        MessageBox.Show("Не найдены дубликаты сборок!", "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        var uiResult = new UserInterfaceDuplicatesResults(outListOfTypes);
                        bool tdResult = (bool)uiResult.ShowDialog();
                    }

                    return Result.Succeeded;
                }
                else
                {
                    Dictionary<string, Dictionary<List<string>, int>> hashAndTypeNameInDocs = new Dictionary<string, Dictionary<List<string>, int>>();

                    foreach (Document curDoc in listOfCamparingDocs)
                    {
                        //Поиск семейств, которые содержат "Панель"
                        IList<Family> familysOfPanels = new FilteredElementCollector(curDoc)
                            .OfCategory(BuiltInCategory.OST_StructuralFraming)
                            .WhereElementIsElementType()
                            .Cast<FamilySymbol>()
                            .Where(p => p.FamilyName.Contains("Панель"))
                            .Select(p => p.Family)
                            .Distinct(new FamilyComparer())
                            .ToList();
                        IList<int> hashOfTypes = new List<int>();
                        IList<string> allTypeNames = new List<string>();

                        foreach (Family family in familysOfPanels)
                        {
                            //Получаем все типы панелей
                            IList<FamilySymbol> allTypesInFamily = new List<FamilySymbol>();
                            foreach (ElementId typeId in family.GetFamilySymbolIds())
                            {
                                allTypesInFamily.Add(curDoc.GetElement(typeId) as FamilySymbol);
                            }
                            allTypesInFamily = allTypesInFamily.OrderBy(q => q.Name).ToList();

                            //Получаем список хэш-кодов для всех типов
                            foreach (FamilySymbol type in allTypesInFamily)
                            {
                                int hash = 0;
                                ParameterSet typeParams = type.Parameters;
                                foreach (Parameter param in typeParams)
                                {
                                    if (param.Definition.Name != comparingParam) { continue; }

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
                                allTypeNames.Add(type.Name);
                            }
                        }

                        //Группируем типы с одинаковыми хэшами
                        try
                        {
                            Dictionary<string, int> typeNameAndHash = allTypeNames.Zip(hashOfTypes, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
                            Dictionary<List<string>, int> groupedTypeNameAndHash = typeNameAndHash.GroupBy(r => r.Value).ToDictionary(t => t.Select(r => r.Key).ToList(), t => t.Key);
                            hashAndTypeNameInDocs.Add(curDoc.Title, groupedTypeNameAndHash);
                        }
                        catch
                        {
                            MessageBox.Show("Обнаружены дубликаты типоразмеров с одинаковым названием, но в разных семействах!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return Result.Cancelled;
                        }
                    }

                    string outListOfTypes = "Одинаковые типы панелей:\n";
                    if (hashAndTypeNameInDocs.Keys.Count != 1)
                    {
                        Dictionary<List<string>, int> firstDict = hashAndTypeNameInDocs[doc.Title];
                        hashAndTypeNameInDocs.Remove(doc.Title);
                        int nDuplicate = 0;

                        foreach (List<string> firstKey in firstDict.Keys)
                        {
                            int hash = firstDict[firstKey];
                            bool compareResult = false;
                            Dictionary<string, List<string>> typesInDocsToAdd = new Dictionary<string, List<string>>();
                            foreach (string key in hashAndTypeNameInDocs.Keys)
                            {
                                List<string> types = hashAndTypeNameInDocs[key].Where(x => x.Value == hash).FirstOrDefault().Key;
                                if (types == null)
                                {
                                    compareResult = false;
                                    break;
                                }
                                compareResult = true;
                                typesInDocsToAdd.Add(key, types);
                            }
                            if (compareResult)
                            {
                                nDuplicate++;
                                outListOfTypes += "\n" + nDuplicate + ".\n";
                                outListOfTypes += "- " + doc.Title + ": ";
                                foreach (string typeName in firstKey)
                                {
                                    outListOfTypes += typeName + ", ";
                                }
                                outListOfTypes = outListOfTypes.Remove(outListOfTypes.Length - 2);
                                outListOfTypes += "\n";
                                foreach (string docNameKey in typesInDocsToAdd.Keys)
                                {
                                    outListOfTypes += "- " + docNameKey + ": ";
                                    foreach (string typeName in typesInDocsToAdd[docNameKey])
                                    {
                                        outListOfTypes += typeName + ", ";
                                    }
                                    outListOfTypes = outListOfTypes.Remove(outListOfTypes.Length - 2);
                                    outListOfTypes += "\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (List<string> list in hashAndTypeNameInDocs[doc.Title].Keys.ToList())
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
                        MessageBox.Show("Не найдены дубликаты сборок!", "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        var uiResult = new UserInterfaceDuplicatesResults(outListOfTypes);
                        bool tdResult = (bool)uiResult.ShowDialog();
                    }

                    return Result.Succeeded;
                }
            }
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