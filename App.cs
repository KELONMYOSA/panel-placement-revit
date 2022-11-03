using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace PanelPlacement
{
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        public static string assemblyPath = "";
        public Result OnStartup(UIControlledApplication application)
        {
            assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string tabName = "Стеновые панели";
            
            //Разделы вкладки
            //Размещение
            try { application.CreateRibbonTab(tabName); } catch { }
            string panelName = "Размещение";
            RibbonPanel panelCreation = null;
            List<RibbonPanel> tryPanels = application.GetRibbonPanels(tabName).Where(i => i.Name == panelName).ToList();
            if (tryPanels.Count == 0)
            {
                panelCreation = application.CreateRibbonPanel(tabName, panelName);
            }
            else
            {
                panelCreation = tryPanels.First();
            }
            //Оформление
            try { application.CreateRibbonTab(tabName); } catch { }
            panelName = "Оформление";
            RibbonPanel panelViews = null;
            tryPanels = application.GetRibbonPanels(tabName).Where(i => i.Name == panelName).ToList();
            if (tryPanels.Count == 0)
            {
                panelViews = application.CreateRibbonPanel(tabName, panelName);
            }
            else
            {
                panelViews = tryPanels.First();
            }

            //Кнопки
            PushButton btn1 = panelCreation.AddItem(new PushButtonData(
                            "CreatePanels",
                            "Разместить панели",
                            assemblyPath,
                            "PanelPlacement.CreatePanels")
                            ) as PushButton;
            btn1.LargeImage = ConverPngToBitmap(Properties.Resources.Create);
            btn1.ToolTip = "Размещение панелей по оси стены";

            PushButton btn2 = panelViews.AddItem(new PushButtonData(
                            "CreateAssemblies",
                            "Создать сборку",
                            assemblyPath,
                            "PanelPlacement.CreateAssembliesAndViews")
                            ) as PushButton;
            btn2.LargeImage = ConverPngToBitmap(Properties.Resources.AssembliesAndViews);
            btn2.ToolTip = "Создание сборок и видов";

            PushButton btn3 = panelViews.AddItem(new PushButtonData(
                            "CreateSheets",
                            "Разместить на листы",
                            assemblyPath,
                            "PanelPlacement.PlaceOnSheets")
                            ) as PushButton;
            btn3.LargeImage = ConverPngToBitmap(Properties.Resources.PlaceOnSheets);
            btn3.ToolTip = "Разместить виды на листах";

            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        private BitmapImage ConverPngToBitmap(Image img)
        {
            using (var memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
