using System.Globalization;

namespace VendingService.API.Pages.Web;

public static class WebText
{
    private static bool IsEnglish
        => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    public static string Title => IsEnglish ? "Franchisee module" : "Модуль франчайзера";
    public static string MenuMachines => IsEnglish ? "Machines" : "ТА";
    public static string MenuCalendar => IsEnglish ? "Maintenance calendar" : "Календарь обслуживания";
    public static string MenuSchedule => IsEnglish ? "Work schedule" : "График работ";
    public static string UploadTitle => IsEnglish ? "Import machines" : "Загрузка торговых автоматов";
    public static string CalendarTitle => IsEnglish ? "Maintenance calendar" : "Календарь обслуживания";
    public static string ScheduleTitle => IsEnglish ? "Work schedule" : "График работ";
    public static string FilterAll => IsEnglish ? "All machines" : "Все автоматы";
    public static string FilterSingle => IsEnglish ? "Single machine" : "Один автомат";
    public static string ViewWeek => IsEnglish ? "Week" : "Неделя";
    public static string ViewMonth => IsEnglish ? "Month" : "Месяц";
    public static string ViewYear => IsEnglish ? "Year" : "Год";
    public static string Apply => IsEnglish ? "Apply" : "Применить";
    public static string Refresh => IsEnglish ? "Refresh" : "Обновить";
    public static string Generate => IsEnglish ? "Generate" : "Сформировать";
    public static string AutoAssign => IsEnglish ? "Auto-assign" : "Автораспределение";
    public static string Upload => IsEnglish ? "Upload" : "Загрузить";
    public static string ChooseFile => IsEnglish ? "Choose file" : "Выберите файл";
    public static string Success => IsEnglish ? "Success" : "Готово";
    public static string Errors => IsEnglish ? "Errors" : "Ошибки";
}
