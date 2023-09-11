using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;


namespace PanelPlacement
{
    class PanelSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming
                && (elem.Document.GetElement(elem.GetTypeId()) as ElementType).FamilyName.Contains("Панель"))
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}