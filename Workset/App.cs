#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;

#endregion

// https://www.youtube.com/watch?v=mHfnsNOEbms for additional info on making this ribbon

namespace Worksets
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            RibbonPanel curPanel = a.CreateRibbonPanel("Stupid Sexy Modelers");


            // line gets where this .dll is location, this gives .dll itself
            string curAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            // line below gets directory name of .dll
            string curAssemblyPath = System.IO.Path.GetDirectoryName(curAssembly);


            PushButtonData pbd1 = new PushButtonData("Stupid Sexy Mike Kieth", "Assigns Concrete Walls to Workset", curAssembly, "Worksets.Command");

            pbd1.LargeImage = new BitmapImage(new Uri(System.IO.Path.Combine(curAssemblyPath, "mike_keith.png")));
            
            PushButton pb1 = (PushButton)curPanel.AddItem(pbd1);

            // set tooltip and contextual help
            pb1.ToolTip = "PUSH ME";


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
