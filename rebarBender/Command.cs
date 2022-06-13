#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion

namespace rebarBender
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        // A function that returns the minimum bend radius for a bar
        private static double BendRadius(int bar_size)
        {
            double bar_bend = 0;
            double bar_dia = 0;
            if (bar_size <= 5)
            {
                bar_bend = bar_size * 4;
            }
            else
            {
                bar_bend = bar_size * 6;
            }
            return bar_bend;
        }
        private static double ClockWise(List<XYZ> pnts)
        {
            double y = 0;
            for (int i = 0; i <= pnts.Count-1; i++)
            {
                if (i==pnts.Count-1)
                {
                    double x = (pnts[0].X - pnts[i].X) * (pnts[0].Y + pnts[i].Y);
                    y = (double)y + (double)x;
                }
                else
                {
                    double x = (pnts[i + 1].X - pnts[i].X) * (pnts[i + 1].Y + pnts[i].Y);
                    y = (double)y + (double)x;
                }
            }
            bool CW_CCW = true;
            if(y<=(double)0)
            {
                CW_CCW = false;
            }
            return y;
        }
        // a dictionary of bar diameters
        //var bar_dia = new Dictionary<int, double>(){
        //    {3, 0.375},
        //    {4, 0.5},
        //    {5, 0.625},
        //    {6, 0.75},
        //    {7, 0.875},
        //    {8, 1.0},
        //    {9, 1.128},
        //    {10, 1.27},
        //    {11, 1.41},
        //    {14, 1.693},
        //    {18, 2.257}
        //    };


        // we will be using the vector from point 1 to 2 to determine global rotation
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // setting up snaps for 

            ObjectSnapTypes snapTypes = ObjectSnapTypes.Endpoints | ObjectSnapTypes.Perpendicular | ObjectSnapTypes.Points | ObjectSnapTypes.Intersections;

            //Pick point
            Selection sel = uiapp.ActiveUIDocument.Selection;
            XYZ pt1 = sel.PickPoint(snapTypes, "Point 1");
            XYZ pt2 = sel.PickPoint("Point 2");
            XYZ pt3 = sel.PickPoint("Point 3");

            List<XYZ> selected_pnts = new List<XYZ>();
            selected_pnts.Add(pt1);
            selected_pnts.Add(pt2);
            selected_pnts.Add(pt3);

            // testing for clockwise or counter-clockwise.
            // if the value returned from clockwise is +, CW, if -, CCW

            double CW_CCW = ClockWise(selected_pnts);

            XYZ pt1_mod = new XYZ(pt1.X, pt1.Y, 0);
            XYZ pt2_mod = new XYZ(pt2.X, pt2.Y, 0);
            XYZ pt3_mod = new XYZ(pt3.X, pt3.Y, 0);

            // note in order for offset to work, the z vector cannot be 0, odd that vector has 0 in the y direction
            XYZ vector = new XYZ((pt2.Y - pt1.Y), 0, -(pt2.X - pt1.X));
            XYZ vector_2 = new XYZ((pt3.X - pt2.X), 0, -(pt3.Y - pt2.Y));

            //find rotation of the L12 line segement in the global axis
            XYZ vector_12 = new XYZ((pt2.X - pt1.X), (pt2.Y - pt1.Y), 0);
            double rotation = Math.Atan(vector_12.Y / vector_12.X);


            Curve L12 = Line.CreateBound(pt1_mod, pt2_mod);
            Line L23 = Line.CreateBound(pt2_mod, pt3_mod);
            Line L13 = Line.CreateBound(pt1_mod, pt3_mod);

            //reverses the direction of L21

            Curve L21 = Line.CreateBound(pt2_mod, pt1_mod);

            //https://stackoverflow.com/questions/1211212/how-to-calculate-an-angle-from-three-points;

            // determines the internal angle of the (3) points
            Double angle_int = Math.Acos((L12.Length * L12.Length + L23.Length * L23.Length - L13.Length*L13.Length) / (2 * L12.Length * L23.Length));



            // for a #8 bar
            double bar_dia = (double)1/(double)12;
            // offset based on bar size chosen

            //double b = -BendRadius(4)/12 * Math.Tan((angle_int - Math.PI)/2);
            double value = (double)8 / (double)12;
            double b = -value * Math.Tan((angle_int - Math.PI) / 2);

            //XYZ arc1 = CL1.Flip.Evaluate(0.5, false);
            XYZ arc1 = L21.Evaluate(b, false);
            XYZ arc2 = L23.Evaluate(b, false);

            // making an arc for plotting results

            XYZ x_vec = new XYZ(1, 0, 0);
            XYZ y_vec = new XYZ(0, 1, 0);
            XYZ vector3 = XYZ.BasisZ;

            //creates circles for plotting

            Arc arc_1= Arc.Create(arc1, 0.05 ,0.0, 14, x_vec, y_vec);
            Arc arc_2 = Arc.Create(arc2, 0.05, 0.0, 14, x_vec, y_vec);

            // creates lines L1b and L3b

            Line L1b = Line.CreateBound(pt1_mod, arc1);
            Line L3b = Line.CreateBound(pt3_mod, arc2);
            Line L3b_rev = Line.CreateBound(arc2, pt3_mod);



            // offset lines L1B and L3b, if we are CCW, we multiply our offset by -1
            double value_2 = 0;
            double bar_dia_2 = bar_dia;
            if (CW_CCW < 0)
                bar_dia = -1 * bar_dia;
            if (CW_CCW < 0)
                value_2 = (-1 * value);
            else
                value_2 = value;
           

            Curve L1b_off = L1b.CreateOffset(bar_dia, vector3);
            Curve L1b_rev = Line.CreateBound(L1b_off.GetEndPoint(1), L1b_off.GetEndPoint(0));
            Curve L3b_off = L3b.CreateOffset(-bar_dia, vector3);

            Line L3b_L3b_off = Line.CreateBound(L3b.GetEndPoint(0), L3b_off.GetEndPoint(0));
            Line L1b_off_fL1b = Line.CreateBound(L1b_off.GetEndPoint(0), L1b.GetEndPoint(0));

            // center point calculation

            Curve center = L1b.CreateOffset(value_2, vector3);
            XYZ center_rotation = center.GetEndPoint(1);

            Arc arc_3 = Arc.Create(center_rotation, 0.05, 0.0, 14, x_vec, y_vec);

            // create a line between center of circle and point 2

            Line L2C = Line.CreateBound(center_rotation, pt2);
            //XYZ C1 = L2C.Evaluate(BendRadius(4)/12, false);
            XYZ C1 = L2C.Evaluate((value) - bar_dia_2, false);
            XYZ C1_mod = new XYZ(C1.X, C1.Y, 0);

            //XYZ C2 = L2C.Evaluate(BendRadius(4)/12 + bar_dia, false);
            XYZ C2 = L2C.Evaluate((value), false);
            XYZ C2_mod = new XYZ(C2.X, C2.Y, 0);

            // create arcs of bend

            Arc outside_arc = Arc.Create(L1b.GetEndPoint(1), L3b.GetEndPoint(1), C2_mod);
            Arc inside_arc = Arc.Create(L3b_off.GetEndPoint(1), L1b_off.GetEndPoint(1), C1_mod);

            // setup the local coordinate system of line segement L12

            Transform trans2 = Transform.CreateRotationAtPoint(XYZ.BasisZ, rotation, pt2);
            XYZ tPoint = trans2.OfPoint(pt3);
            double Y_trans = tPoint.Y;

            // test with transforms

            Transform trans_test = Transform.CreateRotationAtPoint(XYZ.BasisZ, Math.PI / 4, pt2);
            XYZ loc_coords = trans_test.OfPoint(pt3);
            


            // test

            XYZ pt00 = new XYZ(0, 0, 0);
            XYZ pt10 = new XYZ(10, 0, 0);
            XYZ pt20 = new XYZ(0, bar_dia, 0);

            View view = doc.ActiveView;

            // Popup showing the points of the lines

            //TaskDialog.Show("Lengths", "Start L12 " + L12.GetEndPoint(0).ToString() + "\n" +
            //    "End L12 " + L12.GetEndPoint(1).ToString() + "\n" +
            //    "Start L21 " + L21.GetEndPoint(0).ToString() + "\n" +
            //    "End L21 " + L21.GetEndPoint(1).ToString());


            // Popup showing the vector

            TaskDialog.Show("Angle","Global Angle " + (rotation*180/Math.PI).ToString() + "\n" + 
                "Your transformed Y coordinate = " + Y_trans.ToString() + "\n" +
                "the points you have selected are CW, " + CW_CCW.ToString());
            List<CurveLoop> profileLoops = new List<CurveLoop>();
            CurveLoop rebarLoop = new CurveLoop();
            rebarLoop.Append(L1b);
            rebarLoop.Append(outside_arc);
            rebarLoop.Append(L3b_rev);
            rebarLoop.Append(L3b_L3b_off);
            rebarLoop.Append(L3b_off);
            rebarLoop.Append(inside_arc);
            rebarLoop.Append(L1b_rev);
            rebarLoop.Append(L1b_off_fL1b);

            profileLoops.Add(rebarLoop);

            //Getting the region type for the filled region
            //var regionType = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement("S");
            // find teh type
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_FilledRegion);


            //IList<Element> myPatterns = fillRegionTypes.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            IList<Element> myPatterns = fillRegionTypes.WherePasses(filter).WhereElementIsElementType().ToElements();
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            

            //var regionType = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).First(f => f.Name == "Solid Black");


            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                doc.Create.NewDetailCurve(view, L1b);
                doc.Create.NewDetailCurve(view, L1b_off);
                doc.Create.NewDetailCurve(view, L3b);
                doc.Create.NewDetailCurve(view, L3b_off);
                doc.Create.NewDetailCurve(view, outside_arc);
                doc.Create.NewDetailCurve(view, inside_arc);
                doc.Create.NewDetailCurve(view, center);
                doc.Create.NewDetailCurve(view, arc_3);
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}

