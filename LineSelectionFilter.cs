using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;


namespace PanelPlacement
{
    class LineSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {

            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Lines)
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
