using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CalendarService.Parse;
using NLog;

namespace CalendarService.Service
{
    /// <summary>
    /// Класс для получения данных из Консультант Плюс.
    /// </summary>
    internal class ConsultantPlus : IParsable, IGetData
    {
        #region Константы.
        /// <summary>
        /// Адрес по умолчанию.
        /// </summary>
        const string DefaultUrl = "https://www.consultant.ru/law/ref/calendar/proizvodstvennye/";

        /// <summary>
        /// Селектор для месяцев.
        /// </summary>
        const string MonthsSelector = ".cal";

        /// <summary>
        /// Селектор для праздников.
        /// </summary>
        const string HolidaySelector = "blockquote:first-of-type";

        /// <summary>
        /// Селектор наименования месяца.
        /// </summary>
        const string MonthNameSelector = ".month:first-of-type";

        /// <summary>
        /// Селектор для информации по праздникам.
        /// </summary>
        const string HolidayInfoSelector = "p";

        /// <summary>
        /// Наименование класса с выходными.
        /// </summary>
        const string WeekendClassName = "weekend";

        /// <summary>
        /// Наименование класса с предпраздничными днями.
        /// </summary>
        const string PreHolidayClassName = "preholiday";

        /// <summary>
        /// Наименование класса с праздниками.
        /// </summary>
        const string HolidayClassName = "holiday weekend";
        #endregion

        /// <summary>
        /// Получить данные по выходным дням.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="dataAccess">Информация по доступу.</param>
        /// <returns>Данные по выходным дням (null в случае ошибок).</returns>
        public WeekendData GetData(Logger logger, DataAccess dataAccess)
        {
            if (dataAccess.ByParser)
                return ParseData(logger, dataAccess);

            return null;            
        }

        /// <summary>
        /// Получить адрес для доступа к данным.
        /// </summary>
        /// <param name="dataAccess">Информация по доступу.</param>
        /// <returns>Строка с адресом страницы календаря по конкретному году.</returns>
        public string GetUrl(DataAccess dataAccess)
        {
            if (dataAccess.ByParser)
            {
                var baseUrl = string.IsNullOrEmpty(dataAccess.BaseUrl) ? DefaultUrl : dataAccess.BaseUrl;
                return string.Format("{0}/{1}", baseUrl.TrimEnd('/'), dataAccess.Year);
            }

            return string.Empty;
        }

        #region Парсинг.
        /// <summary>
        /// Получить данные по выходным дням с сайта.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="dataAccess">Информация по доступу.</param>
        /// <returns>Данные по выходным дням (null в случае ошибок).</returns>
        public WeekendData ParseData(Logger logger, DataAccess dataAccess)
        {
            return Parser.ParseData(logger, MonthsSelector, HolidaySelector, dataAccess,
                        GetUrl, GetMonths, GetHolidayInfo);
        }

        /// <summary>
        /// Обработать данные по месяцам.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="months">Коллекция данных с сайта.</param>
        /// <returns>Список данных по месяцам.</returns>
        public List<Month> GetMonths(Logger logger, IHtmlCollection<IElement> months)
        {
            var resultMonths = new List<Month>();

            // Обрабатываем месяца.
            foreach (var month in months)
            {
                var resultMonth = new Month();

                try
                {
                    // Наименование.
                    resultMonth.Name = month.QuerySelector(MonthNameSelector)?.TextContent;

                    // Предпраздничные дни.
                    var preHolidays = month.GetElementsByClassName(PreHolidayClassName)
                        .Select(x => x.TextContent.Trim('*'));
                    resultMonth.PreHolidays = preHolidays.ToArray();

                    var allWeekends = month.GetElementsByClassName(WeekendClassName)
                        .Where(x => x is IHtmlTableCellElement && !(x is IHtmlTableHeaderCellElement));
                    // Выходные дни.
                    var weekends = allWeekends.Where(x => x.ClassName == WeekendClassName).Select(x => x.TextContent);
                    resultMonth.Weekends = weekends.ToArray();
                    // Праздничные дни.
                    var holidays = allWeekends.Where(x => x.ClassName == HolidayClassName).Select(x => x.TextContent);
                    resultMonth.Holidays = holidays.ToArray();

                    resultMonths.Add(resultMonth);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Ошибка обработки месяца {0}", resultMonth.Name);
                    return null;
                }
            }

            return resultMonths;
        }

        /// <summary>
        /// Получить информацию по праздникам.
        /// </summary>
        /// <param name="info">Элемент с данными по праздникам.</param>
        /// <returns>Информацию по праздникам.</returns>
        public string GetHolidayInfo(IElement info)
        {
            if (info == null)
                return string.Empty;

            var holidays = info.QuerySelectorAll(HolidayInfoSelector);
            return string.Join(Environment.NewLine, holidays.Select(x => x.TextContent));
        }
        #endregion
    }
}
