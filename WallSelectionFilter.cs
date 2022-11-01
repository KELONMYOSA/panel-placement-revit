using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;


namespace PanelPlacement
{
    class WallSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {

            if (elem is Wall)
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
