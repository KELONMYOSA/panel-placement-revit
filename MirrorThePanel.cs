using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                            bool changeResult = ChangeParamsFromCsv(panelType, newPanelType, tablePath);
                            if (!changeResult) return Result.Cancelled;

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
                        XYZ newPanelXYZ = (mirroredPanel.Location as LocationPoint).Point;
                        double newPanelAngle = (mirroredPanel.Location as LocationPoint).Rotation + Math.PI;
                        doc.Delete(mirroredPanelId);
                        FamilyInstance newMirroredPanel = doc.Create.NewFamilyInstance(newPanelXYZ, newType as FamilySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        newMirroredPanel.Location.Rotate(Line.CreateBound(newPanelXYZ, new XYZ(newPanelXYZ.X, newPanelXYZ.Y, newPanelXYZ.Z + 1)), newPanelAngle);
                    }

                    transaction.Commit();
                }
                
                return Result.Succeeded;
            }    
        }

        private static bool ChangeParamsFromCsv(ElementType oldType, ElementType newType, string filePath)
        {
            using (StreamReader rd = new StreamReader(filePath, Encoding.Default))
            {
                rd.ReadLine();
                while (!rd.EndOfStream)
                {
                    string line = rd.ReadLine();
                    try
                    {
                        string[] cell = line.Split(',');

                        Parameter paramExists = oldType.LookupParameter(cell[0]);
                        
                        if (paramExists != null)
                        {
                            if (cell[1] == "all" || oldType.Name.StartsWith(cell[1]))
                            {
                                bool conditionResult = false;

                                if (cell[2] == "no")
                                {
                                    conditionResult = true;
                                }
                                else
                                {
                                    string[] conditions = cell[2].Split('&');

                                    foreach (string cond in conditions)
                                    {
                                        conditionResult = false;

                                        string[] condition = cond.Split('|');
                                        StorageType paramType = oldType.LookupParameter(condition[0]).StorageType;
                                        if (paramType == StorageType.Integer)
                                        {
                                            if (condition[2] == "true")
                                            {
                                                condition[2] = "1";
                                            }
                                            else if (condition[2] == "false")
                                            {
                                                condition[2] = "0";
                                            }

                                            if (condition[1] == "=")
                                            {
                                                if (oldType.LookupParameter(condition[0]).AsInteger().Equals(Convert.ToInt32(condition[2]))) conditionResult = true;
                                            }
                                            else if (condition[1] == ">")
                                            {
                                                if (oldType.LookupParameter(condition[0]).AsInteger() > Convert.ToInt32(condition[2])) conditionResult = true;
                                            }
                                            else if (condition[1] == "<")
                                            {
                                                if (oldType.LookupParameter(condition[0]).AsInteger() < Convert.ToInt32(condition[2])) conditionResult = true;
                                            }
                                        }
                                        else if (paramType == StorageType.Double)
                                        {
                                            if (condition[1] == "=")
                                            {
                                                if (oldType.LookupParameter(condition[0]).AsDouble().Equals(Convert.ToDouble(condition[2]))) conditionResult = true;
                                            }
                                            else if (condition[1] == ">")
                                            {
                                                if (oldType.LookupParameter(condition[0]).AsDouble() > Convert.ToDouble(condition[2])) conditionResult = true;
                                            }
                                            else if (condition[1] == "<")
                                            {
                                                if (oldType.LookupParameter(condition[0]).AsDouble() < Convert.ToDouble(condition[2])) conditionResult = true;
                                            }
                                        }
                                        else if (paramType == StorageType.String)
                                        {
                                            if (condition[1] == "=")
                                            {
                                                if (oldType.LookupParameter(condition[0]).AsString().Equals(condition[2])) conditionResult = true;
                                            }
                                        }

                                        if (!conditionResult) break;
                                    }
                                }

                                if (conditionResult)
                                {
                                    double paramResult = CalculateParams(oldType, cell[3]);
                                    if (newType.LookupParameter(cell[0]).StorageType == StorageType.Double)
                                    {
                                        newType.LookupParameter(cell[0]).Set(paramResult);
                                    }
                                    else
                                    {
                                        newType.LookupParameter(cell[0]).Set(Convert.ToInt16(paramResult));
                                    }
                                }
                            }
                        }  
                    }
                    catch
                    {
                        MessageBox.Show("Проверьте строку - \"" + line + "\"!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                return true;
            }
        }

        private static double CalculateParams(ElementType type, string expression)
        {
            string[] arrayOfString = expression.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            double result = 0, num = 0;
            bool plus = true;
            for (int i = 0; i < arrayOfString.Length; i++)
            {
                if (i % 2 == 0)
                {
                    if (double.TryParse(arrayOfString[i], out num))
                    {
                        num = num / 304.8;
                    }
                    else
                    {
                        num = type.LookupParameter(arrayOfString[i]).AsDouble();
                    }
                    
                    if (plus)
                    {
                        result = result + num;
                    }   
                    else
                    {
                        result = result - num;
                    }    
                }
                else
                {
                    if (arrayOfString[i] == "+")
                        plus = true;
                    else
                        plus = false;
                }
            }

            return result;
        }
    }
}