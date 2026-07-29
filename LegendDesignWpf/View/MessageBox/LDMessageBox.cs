using LegendDesignWpf.Core.Enums;
using LegendDesignWpf.Core.Models;
using System.Windows;

namespace LegendDesignWpf.View.MessageBox
{
    public partial class LDMessageBox
    {
        public static MessageBoxResult Show(string message, string title = "", MessageBoxButton mboxBtn = MessageBoxButton.OK)
        {
            var box = new LDMessageBoxWindow { Message = message, Title = title };

            var btns = BuildButtons(mboxBtn, box);

            foreach (var button in btns)
                box.Buttons.Add(button);

            box.ShowDialog();

            return box.Result;
        }

        public static MessageBoxResult Show(
            string message, 
            ButtonModel[] buttons,
            string title = "", 
            string loadingMessage = "Loading...")
        {
            var box = new LDMessageBoxWindow 
            { 
                Message = message, 
                Title = title,
                LoadingMessage = loadingMessage,
            };

            foreach ( var button in buttons )
                box.Buttons.Add(button);

            box.ShowDialog();

            return box.Result;
        }

        private static List<ButtonModel> BuildButtons(MessageBoxButton mboxBtn, LDMessageBoxWindow box)
        {
            var btnList = new List<ButtonModel>();

            if (mboxBtn == MessageBoxButton.OK)
            {
                btnList.Add(new ButtonModel
                {
                    Title = "Ок",
                    Action = () =>
                    {
                        box.Result = MessageBoxResult.OK;
                        return Task.CompletedTask;
                    }
                });
            }
            else if (mboxBtn == MessageBoxButton.OKCancel)
            {
                btnList.Add(new ButtonModel
                {
                    Title = "Ок",
                    Action = () =>
                    {
                        box.Result = MessageBoxResult.OK;
                        return Task.CompletedTask;
                    }
                });

                btnList.Add(new ButtonModel
                {
                    Title = "Отмена",
                    Action = () =>
                    {
                        box.Result = MessageBoxResult.Cancel;
                        return Task.CompletedTask;
                    }
                });
            }
            else if (mboxBtn == MessageBoxButton.YesNo)
            {
                btnList.Add(new ButtonModel
                {
                    Title = "Да",
                    Action = () =>
                    {
                        box.Result = MessageBoxResult.Yes;
                        return Task.CompletedTask;
                    }
                });

                btnList.Add(new ButtonModel
                {
                    Title = "Нет",
                    Action = () =>
                    {
                        box.Result = MessageBoxResult.No;
                        return Task.CompletedTask;
                    }
                });
            }
            else if (mboxBtn == MessageBoxButton.YesNoCancel)
            {
                btnList.Add(new ButtonModel
                {
                    Title = "Да",
                    Action = () =>
                    {
                        box.Result = MessageBoxResult.Yes;
                        return Task.CompletedTask;
                    }
                });

                btnList.Add(new ButtonModel
                {
                    Title = "Нет",
                    Action = () =>
                    {
                        box.Result = MessageBoxResult.No;
                        return Task.CompletedTask;
                    }
                });

                btnList.Add(new ButtonModel
                {
                    Title = "Отмена",
                    Action = () =>
                    {
                        box.Result = MessageBoxResult.Cancel;
                        return Task.CompletedTask;
                    }
                });
            }

            return btnList;
        }
    }
}
