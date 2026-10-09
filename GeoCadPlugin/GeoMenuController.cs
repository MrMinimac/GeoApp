using Autodesk.AutoCAD.ApplicationServices;
using GeoUIWpf.Views.Windows;

namespace GeoCadPlugin
{
    public static class GeoMenuController
    {
        public static GeoMenu? CurrentMenu { get; private set; } = null;

        public static void Show()
        {
            if (CurrentMenu != null)
            {
                if (!CurrentMenu.IsVisible)
                    CurrentMenu.Show();

                CurrentMenu.WindowState = System.Windows.WindowState.Normal;
                CurrentMenu.Activate();
                return;
            }

            var items = CreateItems();

            CurrentMenu = new GeoMenu(items);

            CurrentMenu.Closed += (_, _) => CurrentMenu = null;

            Application.ShowModelessWindow(CurrentMenu);
        }

        public static List<GeoMenuItem> CreateItems()
        {
            return new List<GeoMenuItem>
            {
                // Импорт
                new()
                {
                    Title = "Импорт координат",
                    Description = "Импорт координат скважин из Excel и создание окружностей.",
                    Action = () => ExecuteCommand("IMPORTCOORDS")
                },

                // Скважины
                new()
                {
                    Title = "Случайное смещение скважин",
                    Description = "Случайным образом смещает выбранные окружности.",
                    Action = () => ExecuteCommand("RANDOMCIRCLES")
                },
                new()
                {
                    Title = "Найти отметки скважин",
                    Description = "Определяет высотные отметки скважин по горизонталям.",
                    Action = () => ExecuteCommand("FindWellElevation")
                },

                // Полилинии
                new()
                {
                    Title = "Разделить полилинии",
                    Description = "Размещает окружности вдоль выбранных полилиний с заданным шагом.",
                    Action = () => ExecuteCommand("DIVIDEPOLYLINE")
                },

                // Топография
                new()
                {
                    Title = "Загрузить топографию",
                    Description = "Загружает DEM, строит горизонтали и позволяет настроить их обработку.",
                    Action = () => ExecuteCommand("GETTOPOGRAPHY")
                },
            };
        }

        private static void ExecuteCommand(string command)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            doc.SendStringToExecute(
                command + " ",
                true,
                false,
                false
            );

            //if (CurrentMenu != null && CurrentMenu.IsVisible)
            //{
            //    CurrentMenu.WindowState = System.Windows.WindowState.Minimized;
            //}
        }
    }
}
