
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
