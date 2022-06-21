#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;

#endregion

namespace rebarBenderMulti
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            RibbonPanel curPanel = a.CreateRibbonPanel("Revit Rebar Bending");

            // line gets where this .dll is location, this gives .dll itself
            string curAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            // line below gets directory name of .dll
            string curAssemblyPath = System.IO.Path.GetDirectoryName(curAssembly);

            // #3
            PushButtonData pbd1 = new PushButtonData("Bend #3", "Bend #3 Bar", curAssembly, "rebarBenderMulti.nu3");
            // #4 
            PushButtonData pbd2 = new PushButtonData("Bend #4", "Bend #4 Bar", curAssembly, "rebarBenderMulti.nu4");
            // #5
            PushButtonData pbd3 = new PushButtonData("Bend #5", "Bend #5 Bar", curAssembly, "rebarBenderMulti.nu5");
            // #6
            PushButtonData pbd4 = new PushButtonData("Bend #6", "Bend #6 Bar", curAssembly, "rebarBenderMulti.nu6");
            // #7
            PushButtonData pbd5 = new PushButtonData("Bend #7", "Bend #7 Bar", curAssembly, "rebarBenderMulti.nu7");
            // #8
            PushButtonData pbd6 = new PushButtonData("Bend #8", "Bend #8 Bar", curAssembly, "rebarBenderMulti.nu8");
            // #9
            PushButtonData pbd7 = new PushButtonData("Bend #9", "Bend #9 Bar", curAssembly, "rebarBenderMulti.nu9");
            // #10
            PushButtonData pbd8 = new PushButtonData("Bend #10", "Bend #10 Bar", curAssembly, "rebarBenderMulti.nu10");
            // #11
            PushButtonData pbd9 = new PushButtonData("Bend #11", "Bend #11 Bar", curAssembly, "rebarBenderMulti.nu11");
            // #14
            PushButtonData pbd10 = new PushButtonData("Bend #14", "Bend #14 Bar", curAssembly, "rebarBenderMulti.nu14");
            // #18
            PushButtonData pbd11 = new PushButtonData("Bend #18", "Bend #18 Bar", curAssembly, "rebarBenderMulti.nu18");
            

            SplitButtonData splitBtnData = new SplitButtonData("SplitButton", "Split Button");
            SplitButton splitBtn = curPanel.AddItem(splitBtnData) as SplitButton;
            splitBtn.AddPushButton(pbd1);
            splitBtn.AddPushButton(pbd2);
            splitBtn.AddPushButton(pbd3);
            splitBtn.AddPushButton(pbd4);
            splitBtn.AddPushButton(pbd5);
            splitBtn.AddPushButton(pbd6);
            splitBtn.AddPushButton(pbd7);
            splitBtn.AddPushButton(pbd8);
            splitBtn.AddPushButton(pbd9);
            splitBtn.AddPushButton(pbd10);
            splitBtn.AddPushButton(pbd11);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
