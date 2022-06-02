#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Linq;

#endregion

namespace Worksets
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection

            Selection sel = uidoc.Selection;

            // Retrieves all walls in revit database

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);

            IList<Element> walls = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();


            // creates an empty list to add SSW walls to
            IList<Element> ssw_walls = new List<Element>();
            foreach (Element elem in walls)
            {
                if (elem.Name.Contains("SSW"))
                ssw_walls.Add(elem);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // workset id stuff
            //FilteredWorksetCollector worksets = new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset);
            IList<Workset> worksetList = new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset).ToWorksets();
            int ConcreteWalls = 0;


            foreach (Workset workset in worksetList)
            {
                if (workset.Name.Contains("Concrete"))
                {
                    ConcreteWalls = workset.Id.IntegerValue;
                }
            }
            TaskDialog.Show("Revit", ConcreteWalls.ToString());

            // note we have now identified the Concrete shearwall workset
            

            // Popup showing number of walls that were counted
            TaskDialog.Show("Wall Counter", ssw_walls.Count.ToString() + "walls found");
            // Modify document within a transaction

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Assign Concrete Walls");  
                foreach(Element e in ssw_walls)
                    try
                    {
                        e.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).Set(ConcreteWalls);
                        TaskDialog.Show("Changed", e.Name.ToString());
                    }
                    catch
                    {

                    }

                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
