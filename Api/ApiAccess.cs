using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using NLog;
using System.IO;
using System.Xml.Serialization;

namespace CalendarService.Api
{

	/// <summary>
	/// Интерфейс с реализацией доступа к API.
	/// </summary>
	interface IApiAccessable
	{
		/// <summary>
		/// Получить данные по выходным дням с сайта.
		/// </summary>
		/// <param name="logger">Логгер.</param>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <returns>Данные по выходным дням (null в случае ошибок).</returns>
		WeekendData GetFromAPI<T>(Logger logger, DataAccess dataAccess);

		/// <summary>
		/// Обработать данные по месяцам.
		/// </summary>
		/// <typeparam name="T">Десериализованный класс.</typeparam>
		/// <param name="logger">Логгер.</param>
		/// <param name="deserialized">Десериализованный класс.</param>
		/// <returns>Список данных по месяцам.</returns>
		List<Month> GetMonths<T>(Logger logger, T deserialized);

		/// <summary>
		/// Получить информацию по праздникам.
		/// </summary>
		/// <typeparam name="T">Десериализованный класс.</typeparam>
		/// <param name="deserialized">Десериализованный класс.</param>
		/// <returns>Информация по праздникам.</returns>
		string GetHolidayInfo<T>(T deserialized);
	}

	/// <summary>
	/// Класс для парсинга страниц.
	/// </summary>
	internal static class ApiAccessor
	{
		/// <summary>
        /// Get Запрос к API
        /// </summary>
        /// <param name="url">Строка запроса.</param>
		/// <param name="contentType">Тип содержимого.</param>
        /// <param name="result">Значение с результатам запроса (страка с данными/ошибка).</param>
        /// <returns>Признак успешного получения данных.</returns>
		internal static bool Get(string url, string contentType, out string result)
		{
            result = string.Empty;

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
  
            try
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync();
                    
                    result = data.Result;
                    return true;
                }
                
                result = $"{(int)response.StatusCode} ({response.ReasonPhrase})";
                return false;
            }
            catch(Exception ex)
            {
                result = ex.Message;
                return false;
            }
            finally
            {
                client.Dispose();
            }
		}

		/// <summary>
		/// Десериализовать.
		/// </summary>
		/// <typeparam name="T">Класс десериализации.</typeparam>
		/// <param name="content">строка с содержимым.</param>
		/// <returns>Десериализованный класс.</returns>
		/// <exception cref="ArgumentNullException">Пустое значение параметра.</exception>
		public static T Deserialize<T>(string content)
		{
			if (string.IsNullOrEmpty(content))
				throw new ArgumentNullException("Передано пустое содержимое");

			XmlSerializer formater = new XmlSerializer(typeof(T));
			using (var reader = new StringReader(content))
				return (T)formater.Deserialize(reader);
		}

		/// <summary>
		/// Получить данные по выходным дням.
		/// </summary>
		/// <typeparam name="T">Класс десеризализации.</typeparam>
		/// <param name="logger">Логгер.</param>
		/// <param name="dataAccess">Информация по доступу.</param>
		/// <param name="getUrl">Метод определения URL.</param>
		/// <param name="contentType">Тип содержимого.</param>
		/// <param name="getMonths">Метод обработки данных по месяцам.</param>
		/// <param name="getHolidayInfo">Метод обработки данных по праздникам.</param>
		/// <returns>Данные по выходным дням.</returns>
        internal static WeekendData GetFromAPI<T>(Logger logger, DataAccess dataAccess, Func<DataAccess, string> getUrl, string contentType,
			Func<Logger, T, List<Month>> getMonths, Func<T, string> getHolidayInfo)
		{
			var url = getUrl(dataAccess);
			
			// Получаем данные с сайта.
			bool success = Get(url, contentType, out string result);
			if (!success)
			{
				logger.Error(result);
				return null;
			}
			
			// Десериализация.
			T deserialized;
			try
			{
				deserialized = Deserialize<T>(result);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Ошибка при десериализации данных");
				return null;
			}

			return new WeekendData(dataAccess.Year, getMonths(logger, deserialized), getHolidayInfo(deserialized));
		}
	}
}
