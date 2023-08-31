using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;


namespace PanelPlacement
{
    class LineSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {

            if (elem.Category.BuiltInCategory.ToString() == BuiltInCategory.OST_Lines.ToString())
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
