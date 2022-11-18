using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;


namespace PanelPlacement
{
    class StructuralFramingSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {

            if (CreateAssembliesAndViews.unusedTypesOfPanels.Contains((elem as FamilyInstance).Symbol.Name))
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
