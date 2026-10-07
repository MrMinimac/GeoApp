using GeoAppWpf.Converters;
using GeoAppWpf.Models;
using System.Windows;

namespace GeoAppCore.Services
{
    public enum Message
    {
        CrtErr,
        ErrTittle,
        AcImpErr,
        AcDocEmp,
        AcImpOk,
    }

    public static class LocaleService
    {
        public static void ShowError(Exception e)
        {
            MessageBox.Show(
                e.Message,
                LocaleService.Get(Message.ErrTittle),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static string Get(Message m)
        {
            return m switch
            {
                Message.ErrTittle => "Ошибка",
                Message.CrtErr => "Произошла критическая ошибка",
                Message.AcDocEmp => "Документ пуст!",
                Message.AcImpOk => "Проект отправлен в AutoCAD",
                Message.AcImpErr => "Произошла ошибка при импорте.\nУбедитесь что у вас запущен AutoCad и загружен плагин.",
                _ => "NULL"
            };
        }
    }
}
