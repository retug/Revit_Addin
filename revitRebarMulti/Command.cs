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

namespace rebarBenderMulti
{
    public class global_rebar
    {
        //Dictionary that returns bar diameter given an int 3, #3
        public static Dictionary<int, double> bar_dia = new Dictionary<int, double>(){
            {3, 0.375},
            {4, 0.5},
            {5, 0.625},
            {6, 0.75},
            {7, 0.875},
            {8, 1.0},
            {9, 1.128},
            {10, 1.27},
            {11, 1.41},
            {14, 1.693},
            {18, 2.257}
        };
        // Function that returns the minimum bend radius for a bar
        public static double BendRadius(int bar_size)
        {
            double bar_bend = 0;
            if (bar_size <= 5)
            {
                bar_bend = global_rebar.bar_dia[bar_size] * (double)4;
            }
            else
            {
                bar_bend = global_rebar.bar_dia[bar_size] * (double)6;
            }
            return bar_bend;
        }
        //function that tests if (3) points are placed counter clockwise or clockwise
        public static double ClockWise(List<XYZ> pnts)
        {
            double y = 0;
            for (int i = 0; i <= pnts.Count - 1; i++)
            {
                if (i == pnts.Count - 1)
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
            if (y <= (double)0)
            {
                CW_CCW = false;
            }
            return y;
        }
        public static List<double> createOffsetrebar(IList<XYZ> pnts, int bar_size)
        {
            List<double> b_offset = new List<double>();
            for (int i = 0; i <= pnts.Count - 3; i++)
            {
                Line L12 = Line.CreateBound(pnts[i], pnts[i + 1]);
                Line L23 = Line.CreateBound(pnts[i + 1], pnts[i + 2]);
                Line L31 = Line.CreateBound(pnts[i + 2], pnts[i]);
                Double angle_int = Math.Acos((L12.Length * L12.Length + L23.Length * L23.Length - L31.Length * L31.Length) / (2 * L12.Length * L23.Length));

                double bar_dia_chosen = bar_dia[bar_size];
                double bar_bend_chosen = BendRadius(bar_size);

                double b = -(bar_bend_chosen / (double)12) * Math.Tan((angle_int - Math.PI) / 2);


                b_offset.Add(b);
            }
            return b_offset;
        }
        public static List<Curve> createLine(IList<XYZ> pnts, List<double> b_values, int bar_size)
        {
            List<Curve> mainCurve = new List<Curve>();
            double bar_bend_chosen = global_rebar.BendRadius(bar_size) / 12;

            for (int i = 0; i <= pnts.Count - 2; i++)
            {
                //THE FIRST LINE SEGEMENT, NEED TO test for CCW vs CW
                if (i == 0)
                {
                    Line L12 = Line.CreateBound(pnts[i + 1], pnts[i]);

                    XYZ P1b = L12.Evaluate(b_values[i], false);
                    Line L1b = Line.CreateBound(pnts[i], P1b);
                    mainCurve.Add(L1b);

                    //check for CCW
                    List<XYZ> forCheck = new List<XYZ>();
                    forCheck.Add(pnts[i]);
                    forCheck.Add(pnts[i + 1]);
                    forCheck.Add(pnts[i + 2]);

                    double CW_CCW = ClockWise(forCheck);

                    // reverse direction if CCW
                    double value_2 = 0;
                    if (CW_CCW < 0)
                        value_2 = (-1 * bar_bend_chosen);
                    else
                        value_2 = (1 * bar_bend_chosen);

                    //center point calculation
                    Curve bC = L1b.CreateOffset(value_2, XYZ.BasisZ);
                    // line from the center to 2
                    Line C2 = Line.CreateBound(bC.GetEndPoint(1), pnts[i + 1]);
                    XYZ arc_pnt = C2.Evaluate(bar_bend_chosen, false);



                    //we need to calculate the future values to create the arc
                    Line L23 = Line.CreateBound(pnts[i + 1], pnts[i + 2]);
                    XYZ P3b = L23.Evaluate(b_values[i], false);



                    Arc arc12 = Arc.Create(L1b.GetEndPoint(1), P3b, arc_pnt);
                    mainCurve.Add(arc12);

                }
                //THE LAST LINE SEGEMENT
                else if (i == pnts.Count - 2)
                {
                    Line L23 = Line.CreateBound(pnts[i], pnts[i + 1]);
                    XYZ P2b = L23.Evaluate(b_values[pnts.Count - 3], false);
                    Line L2b = Line.CreateBound(P2b, pnts[i + 1]);
                    mainCurve.Add(L2b);

                }
                else
                {
                    Line L12 = Line.CreateBound(pnts[i], pnts[i + 1]);
                    Line L21 = Line.CreateBound(pnts[i + 1], pnts[i]);

                    XYZ P1b = L12.Evaluate(b_values[i - 1], false);
                    XYZ P2b = L21.Evaluate(b_values[i], false);
                    // this is the interior line segment
                    Line L1bL2b = Line.CreateBound(P1b, P2b);
                    // we now need to create the future arc, pnts 2, 3, ,4

                    //check for CCW
                    List<XYZ> forCheck = new List<XYZ>();
                    forCheck.Add(pnts[i]);
                    forCheck.Add(pnts[i + 1]);
                    forCheck.Add(pnts[i + 2]);
                    double CW_CCW = ClockWise(forCheck);

                    // reverse direction if CCW
                    double value_2 = 0;
                    if (CW_CCW < 0)
                        value_2 = (-1 * bar_bend_chosen);
                    else
                        value_2 = (1 * bar_bend_chosen);

                    //center point calculation
                    Curve bC = L1bL2b.CreateOffset(value_2, XYZ.BasisZ);
                    // line from the center to 2
                    Line C2 = Line.CreateBound(bC.GetEndPoint(1), pnts[i + 1]);
                    XYZ arc_pnt = C2.Evaluate(bar_bend_chosen, false);

                    //we need to calculate the future values to create the arc
                    Line L23 = Line.CreateBound(pnts[i + 1], pnts[i + 2]);
                    XYZ P3b = L23.Evaluate(b_values[i], false);
                    Arc arc12 = Arc.Create(L1bL2b.GetEndPoint(1), P3b, arc_pnt);


                    mainCurve.Add(L1bL2b);
                    mainCurve.Add(arc12);
                }
            }
            //check for CCW
            List<XYZ> forCheck_1 = new List<XYZ>();
            forCheck_1.Add(pnts[0]);
            forCheck_1.Add(pnts[0 + 1]);
            forCheck_1.Add(pnts[0 + 2]);
            //Begin Offset commands
            double CW_CCW_1 = ClockWise(forCheck_1);
            double bar_dia_chosen = bar_dia[bar_size] / 12;
            if (CW_CCW_1 < 0)
                bar_dia_chosen = -1 * bar_dia_chosen;
            CurveLoop offset_loop = new CurveLoop();
            List<Curve> offsetCurves = new List<Curve>();
            foreach (Curve elem in mainCurve)
            {
                offsetCurves.Add(elem.CreateOffset(bar_dia_chosen, XYZ.BasisZ));
                offset_loop.Append(elem.CreateOffset(bar_dia_chosen, XYZ.BasisZ));
            }

            Line cnx1 = Line.CreateBound(pnts[pnts.Count - 1], offsetCurves[offsetCurves.Count - 1].GetEndPoint(1));
            Line cnx2 = Line.CreateBound(offsetCurves[0].GetEndPoint(0), pnts[0]);
            //mainCurve.AddRange(cnx1);
            offset_loop.Flip();
            mainCurve.Add(cnx1);
            mainCurve.AddRange(offset_loop);
            mainCurve.Add(cnx2);

            return mainCurve;
        }
    }

    /// <summary>
    /// Start calling
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class nu3 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 3;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class nu4 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 4;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class nu5 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 5;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class nu6 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 6;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class nu7 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 7;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class nu8 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 8;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class nu9 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 9;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class nu10 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 10;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class nu14 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 14;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class nu18 : IExternalCommand
    {
        // here starts all the calling of functions and stuff
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;
            // Initialize a list of points
            IList<XYZ> selectedPoints = new List<XYZ>();

            // Hitting Escape will exit out of the loop for selecting points
            bool value = true;
            while (value)
            {
                try
                {
                    XYZ point1 = uidoc.Selection.PickPoint("Select a point");
                    selectedPoints.Add(point1);
                }
                catch (Exception exp)
                {
                    value = false;
                }
            }


            int bar_size_chosen = 18;
            //crate line function creates the first main curve, from which all other lines will be created

            List<Double> b_offsets = global_rebar.createOffsetrebar(selectedPoints, bar_size_chosen);
            List<Curve> mainCurve = global_rebar.createLine(selectedPoints, b_offsets, bar_size_chosen);

            CurveLoop rebarLoop = new CurveLoop();
            List<CurveLoop> profileLoops = new List<CurveLoop>();

            foreach (Curve e in mainCurve)
            {
                rebarLoop.Append(e);
            }
            profileLoops.Add(rebarLoop);

            // find solid fill type in our revit list
            FilteredElementCollector fillRegionTypes = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType));
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).FirstElement();
            foreach (Element elem in fillRegionTypes)
            {
                if (elem.Name == "Solid Black")
                {
                    patterns = elem;
                }
            }
            View view = doc.ActiveView;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");
                FilledRegion filledRegion = FilledRegion.Create(doc, patterns.Id, view.Id, profileLoops);
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
}