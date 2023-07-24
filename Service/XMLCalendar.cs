using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using CalendarService.Api;
using NLog;
using CalendarService.Serialize;
using CalendarService.Parse;

namespace CalendarService.Service
{
	/// <summary>
	/// Класс для получения данных из XMLCalendar
	/// </summary>
	internal class XMLCalendar : IGetData, IApiAccessable, IParsable
	{
		string ContentType { get; set; }

		#region Константы.
        /// <summary>
        /// Адрес по умолчанию.
        /// </summary>
        const string DefaultUrl = "http://xmlcalendar.ru/html.php?y=";
        
		/// <summary>
		/// Адрес по умолчанию.
		/// </summary>
		const string DefaultApiUrl = "http://xmlcalendar.ru/data/ru/";

        /// <summary>
        /// Селектор для месяцев.
        /// </summary>
        const string MonthsSelector = ".pcal-month";

        /// <summary>
        /// Селектор для праздников.
        /// </summary>
        const string HolidaySelector = ".pcal-holidays-container";

        /// <summary>
        /// Селектор наименования месяца.
        /// </summary>
        const string MonthNameSelector = ".pcal-month-name";
        
        /// <summary>
        /// Наименование класса с выходными.
        /// </summary>
        const string WeekendClassName = "pcal-day pcal-day-holiday";

        /// <summary>
        /// Наименование класса с предпраздничными днями.
        /// </summary>
        const string PreHolidayClassName = "pcal-day pcal-day-short";

        /// <summary>
        /// Селектор для информации по праздникам.
        /// </summary>
        const string HolidayInfoSelector = "li";
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
			else if (dataAccess.ByApi)
				return GetFromAPI<Serialize.Calendar>(logger, dataAccess);

			return null;
		}

		/// <summary>
		/// Получить адрес для доступа к данным.
		/// </summary>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <returns>Строка с адресом страницы календаря по конкретному году.</returns>
		public string GetUrl(DataAccess dataAccess)
		{
            string baseUrl;
			if (dataAccess.ByParser)
            {
                baseUrl = string.IsNullOrEmpty(dataAccess.BaseUrl) ? DefaultUrl : dataAccess.BaseUrl;
			    return string.Format("{0}{1}", baseUrl, dataAccess.Year);
            }
			else if (dataAccess.ByApi)
			{
				baseUrl = string.IsNullOrEmpty(dataAccess.BaseUrl) ? DefaultApiUrl : dataAccess.BaseUrl;
				return string.Format("{0}/{1}/calendar.xml", baseUrl.TrimEnd('/'), dataAccess.Year);
			}
			
			return string.Empty;
		}

		#region Api.
		/// <summary>
		/// Получить данные из API.
		/// </summary>
		/// <param name="logger">Логгер.</param>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <returns>Данные по выходным дням (null в случае ошибок).</returns>
		public WeekendData GetFromAPI<T>(Logger logger, DataAccess dataAccess)
		{
			return ApiAccessor.GetFromAPI<T>(logger, dataAccess, GetUrl, this.ContentType, GetMonths<T>, GetHolidayInfo<T>);
		}

		/// <summary>
		/// Обработать данные по месяцам.
		/// </summary>
		/// <param name="logger">Логгер.</param>
		/// <param name="deserialized">Десериализованный класс.</param>
		/// <returns>Список данных по месяцам.</returns>
		public List<Month> GetMonths<T>(Logger logger, T deserialized)
		{
			var calendar = deserialized as Serialize.Calendar;
			if (calendar == null)
				return null;

			// Обрабатываем месяца.
			var resultMonths = new List<Month>();
			for (int i = 1; i <= 12; i++)
			{
				var resultMonth = new Month();

				try
				{
					// Наименование.
					resultMonth.Number = i;

					var days = calendar.DayInfos.Where(x => x.Date.Month == i);
					// Предпраздничные дни.
					var preHolidays = days.Where(x => x.Type == 2)
						.Select(x => x.Date.Day.ToString());
					resultMonth.PreHolidays = preHolidays.ToArray();

					// Выходные дни.
					int year = calendar.Year;
					var beginnigOfMonth = new DateTime(year, i, 1);
					var allWeekends = Functions.GetWeekends(beginnigOfMonth, beginnigOfMonth.AddDays(DateTime.DaysInMonth(year, i)));
					
					var weekends = allWeekends.Where(x => !days.Select(d => d.Date).Contains(x))
						.Select(x => x.Day.ToString());
					resultMonth.Weekends = weekends
						.ToArray();
					 
					// Праздничные дни.
					var holidays = days.Where(x => x.Type == 1)
						.Select(x => x.Date.Day.ToString());
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
		/// Получить информация по праздникам.
		/// </summary>
		/// <param name="deserialized">Десериализованный класс.</param>
		/// <returns>Информация по праздникам.</returns>
        public string GetHolidayInfo<T>(T deserialized)
        {
			if (!(deserialized is Serialize.Calendar calendar))
				return string.Empty;
			
			var stringBuilder = new System.Text.StringBuilder();
			foreach (var holiday in calendar.Holidays.Holiday)
			{
				var days = calendar.Days.Day.Where(x => x.HolidayId == holiday.Id).Select(x => x.Date);
				var info = string.Format("{0} - {1}", string.Join(", ", days), holiday.Title);
				stringBuilder.AppendLine(info);
			}

			return stringBuilder.ToString().Trim();
        }
		#endregion
		
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
                        .Select(x => x.TextContent);
                    resultMonth.PreHolidays = preHolidays.ToArray();

                    // Выходные дни.
					var allWeekends = month.GetElementsByClassName(WeekendClassName);
                    resultMonth.Weekends = allWeekends.Select(x => x.TextContent).ToArray();
                    // Праздничные дни.
                    resultMonth.Holidays = new string[0]; // Пустой массив т.к. явно не опеределить.

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

		internal XMLCalendar()
		{
			this.ContentType = "application/xml";
		}
	}

}
