using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using NLog;

namespace CalendarService.Parse
{
	/// <summary>
	/// Интерфейс с реализацией данных для парсинга страниц.
	/// </summary>
	internal interface IParsable
	{
		/// <summary>
		/// Получить данные по выходным дням с сайта.
		/// </summary>
		/// <param name="logger">Логгер.</param>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <returns>Данные по выходным дням (null в случае ошибок).</returns>
		WeekendData ParseData(Logger logger, DataAccess dataAccess);

		/// <summary>
		/// Обработать данные по месяцам.
		/// </summary>
		/// <param name="logger">Логгер.</param>
		/// <param name="months">Коллекция данных с сайта.</param>
		/// <returns>Список данных по месяцам.</returns>
		List<Month> GetMonths(Logger logger, IHtmlCollection<IElement> months);

		/// <summary>
		/// Получить информацию по праздникам.
		/// </summary>
		/// <param name="info">Элемент с данными по праздникам.</param>
		/// <returns>Информация по праздникам.</returns>
		string GetHolidayInfo(IElement info);
	}

	/// <summary>
	/// Класс для парсинга страниц.
	/// </summary>
	internal static class Parser
	{
		/// <summary>
		/// Получить коллекцию с данными по месяцам.
		/// </summary>
		/// <param name="config">Конфигурация (если null используется стандарнтная).</param>
		/// <param name="url">Адресс.</param>
		/// <param name="monthSelector">Селектор для месяцев.</param>
		/// <param name="holidaySelector">Селектор праздников.</param>
		/// <returns>Коллекция с данными по месяцам.</returns>
		/// <exception cref="ArgumentNullException">Некорректные входные параметры.</exception>
		private static ParseResult GetParseResult(IConfiguration config, string url, string monthSelector, string holidaySelector)
		{
			if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(monthSelector) || string.IsNullOrEmpty(holidaySelector))
				throw new ArgumentNullException("Переданы некорректные параметры url/className/holidaySelector");

			if (config == null)
				config = Configuration.Default.WithDefaultLoader();

			using (var context = BrowsingContext.New(config))
			{
				using (var document = context.OpenAsync(url))
				{
					var htmlResult = document.Result;
					return new ParseResult(htmlResult.QuerySelectorAll(monthSelector), htmlResult.QuerySelector(holidaySelector));
				}
			}
		}

		/// <summary>
		/// Валидация данных коллекции с месяцами.
		/// </summary>
		/// <param name="months">Коллекция с месяцами.</param>
		/// <returns>Строка с ошибкой или пустая строка.</returns>
		private static string ValidateMonthsData(IHtmlCollection<IElement> months)
		{
			if (months == null)
				return "Не получены данные по месяцам";

			if (months.Length != 12)
				return string.Format("Получено некорректное количество месяцев {0}", months.Length);

			return string.Empty;
		}

		/// <summary>
		/// Получить данные по выходным дням с сайта.
		/// </summary>
		/// <param name="logger">Логгер.</param>
		/// <param name="monthsSelector">Селектор месяца.</param>
		/// <param name="holidaySelector">Селектор информации о праздниках</param>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <param name="getUrl">Метод определения URL.</param>
		/// <param name="getMonthsResult">Метод обработки данных по месяцам.</param>
		/// <param name="getHolidayInfo">Метод обработки данных по праздникам.</param>
		/// <returns>Данные по выходным дням (null в случае ошибок).</returns>
		internal static WeekendData ParseData(Logger logger, string monthsSelector, string holidaySelector,
			 DataAccess dataAccess, Func<DataAccess, string> getUrl,
			 Func<Logger, IHtmlCollection<IElement>, List<Month>> getMonths, Func<IElement, string> getHolidayInfo)
		{
			var url = getUrl(dataAccess);
			ParseResult parseResult;

			// Получаем данные с сайта.
			try
			{
				parseResult = GetParseResult(null, url, monthsSelector, holidaySelector);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Ошибка при получении данных со страницы");
				return null;
			}

			// Валидация данных по месяцам.
			var months = parseResult.Months;
			string validate = ValidateMonthsData(months);
			if (!string.IsNullOrEmpty(validate))
			{
				logger.Error(validate);
				return null;
			}

			return new WeekendData(dataAccess.Year, getMonths(logger, months), getHolidayInfo(parseResult.HolidaysInfo));
		}
	}

	/// <summary>
	/// Результат парсинга страницы.
	/// </summary>
	internal class ParseResult
	{
		/// <summary>
		/// Элементы месяца.
		/// </summary>
		internal IHtmlCollection<IElement> Months { get; set; }

		/// <summary>
		/// Инфо о праздниках.
		/// </summary>
		internal IElement HolidaysInfo { get; set; }

		internal ParseResult(IHtmlCollection<IElement> months, IElement holidaysInfo)
		{
			Months = months;
			HolidaysInfo = holidaysInfo;
		}
	}
}
