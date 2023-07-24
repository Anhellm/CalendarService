using NLog;
using CalendarService.Service;
using System;
using CalendarService.Api;
using CalendarService.Parse;
using AngleSharp.Attributes;

namespace CalendarService
{
	/// <summary>
	/// Интерфейс для получения данных по выходным дням.
	/// </summary>
	public interface IGetData
	{
		/// <summary>
		/// Получить данные по выходным дням.
		/// </summary>
		/// <param name="logger">Логгер.</param>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <returns>Данные по выходным дням.</returns>
		WeekendData GetData(Logger logger, DataAccess dataAccess);
		
		/// <summary>
		/// Получить адрес для доступа к данным.
		/// </summary>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <returns>Строка с адресом страницы календаря по конкретному году.</returns>
		string GetUrl(DataAccess dataAccess);
	}

	/// <summary>
	/// Класс для получения данных по выходным.
	/// </summary>
	public static class CalendarService
	{
		/// <summary>
		/// Получить данные по выходным дням.
		/// </summary>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <param name="byAPI">Признак использования API.</param>
		/// <returns>Данные по выходным дням.</returns>
		public static WeekendData GetWeekendData(DataAccess dataAccess, bool byAPI)
		{
			var logger = LogManager.GetCurrentClassLogger();

			WeekendData result = null;
			if (dataAccess == null)
			{
				logger.Error("Передан пустой параметр dataAccess");
				return result;
			}

			logger.Debug(dataAccess.ToString());

			var accessor = GetAccessor(dataAccess.DataSource);
			if (accessor == null)
			{
				logger.Error("Не определен источник данных");
				return result;
			}

			dataAccess.ByApi = byAPI && HasApi(accessor);
			dataAccess.ByParser = !byAPI && HasParser(accessor);
			return accessor.GetData(logger, dataAccess);
		}

		/// <summary>
		/// Получить класс доступа.
		/// </summary>
		/// <param name="dataSource">Тип источника.</param>
		/// <returns>Класс доступа.</returns>
		private static IGetData GetAccessor(DataSource dataSource)
		{
			switch (dataSource)
			{
				case DataSource.Consultant:
					return new ConsultantPlus();
				case DataSource.HeadHunter:
					return new HeadHunter();
				case DataSource.XMLCalendar:
					return new XMLCalendar();

				default:
					return null;
			}
		}

		#region Проверка доступности реализации.
		/// <summary>
		/// Проверка доступности API у сервиса.
		/// </summary>
		/// <param name="dataSource">Источник данных.</param>
		/// <returns>True/False.</returns>
		public static bool HasApi(DataSource dataSource)
		{
			var accessor = GetAccessor(dataSource);
			return HasApi(accessor);
		}

		/// <summary>
		/// Проверка доступности парсера у сервиса.
		/// </summary>
		/// <param name="dataSource">Источник данных.</param>
		/// <returns>True/False.</returns>
		public static bool HasParser(DataSource dataSource)
		{
			var accessor = GetAccessor(dataSource);
			return HasParser(accessor);
		}

		/// <summary>
		/// Проверка доступности API у сервиса.
		/// </summary>
		/// <param name="accessor">Класс - сервис для получения данных.</param>
		/// <returns>True/False.</returns>
		private static bool HasApi(IGetData accessor) => accessor is IApiAccessable;

		/// <summary>
		/// Проверка доступности парсера у сервиса.
		/// </summary>
		/// <param name="accessor">Класс - сервис для получения данных.</param>
		/// <returns>True/False.</returns>
		private static bool HasParser(IGetData accessor) => accessor is IParsable;
		#endregion
	}

	/// <summary>
	/// Класс для получения доступа к ресурсам.
	/// </summary>
	public class DataAccess
	{
		/// <summary>
		/// Год.
		/// </summary>
		public int Year { get; set; }

		/// <summary>
		/// Адресс.
		/// </summary>
		public string BaseUrl { get; set; }

		/// <summary>
		/// Источник данных.
		/// </summary>
		public DataSource DataSource { get; set; }

		/// <summary>
		/// Доступ через API.
		/// </summary>
		internal bool ByApi { get; set; }
		
		/// <summary>
		/// Доступ через парсер.
		/// </summary>
		internal bool ByParser { get; set; }

		#region Конструкторы.
		public DataAccess(int year, string baseUrl, DataSource dataSource)
		{
			Year = year;
			BaseUrl = baseUrl;
			DataSource = dataSource;

			Validate();
		}

		private DataAccess() { }
		#endregion

		#region Методы.
		/// <summary>
		/// Валидация данных по доступу.
		/// </summary>
		/// <exception cref="ArgumentNullException">Некорректные входные параметры.</exception>
		void Validate()
		{
			if (string.IsNullOrEmpty(BaseUrl))
				throw new ArgumentNullException("Передан пустой параметр BaseUrl");
		}

		public override string ToString()
		{
			return string.Format("Запрос данных в {0}. URL: {1}. {2} год.", DataSource, BaseUrl, Year);
		}
		#endregion
	}

	/// <summary>
	/// Перечисление источников данных.
	/// </summary>
	public enum DataSource
	{
		Undefined,
		Consultant,
		HeadHunter,
		XMLCalendar
	}

}
