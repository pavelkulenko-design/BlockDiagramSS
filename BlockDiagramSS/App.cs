using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace BlockDiagramSS
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "BlockDiagramSS";
            try { application.CreateRibbonTab(tabName); }
            catch { }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Структурная схема");

            string path = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData(
                "BlockDiagramSS_Button",
                "Структурная\nсхема",
                path,
                "BlockDiagramSS.Command");

            panel.AddItem(buttonData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
    }
}
