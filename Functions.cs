using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalendarService
{
	/// <summary>
	/// Класс общих функций.
	/// </summary>
	internal static class Functions
	{
		/// <summary>
		/// Перечисление дат.
		/// </summary>
		/// <param name="start">Дата начала.</param>
		/// <param name="end">Дата окончания.</param>
		/// <returns>Перечисление дат.</returns>
		internal static IEnumerable<DateTime> GetDaysBetween(DateTime start, DateTime end)
		{
			for (DateTime i = start; i < end; i = i.AddDays(1))
				yield return i;
		}

		/// <summary>
		/// Перечисление выходных.
		/// </summary>
		/// <param name="start">Дата начала.</param>
		/// <param name="end">Дата окончания.</param>
		/// <returns>Перечисление выходных.</returns>
		internal static IEnumerable<DateTime> GetWeekends(DateTime start, DateTime end)
		{
			return GetDaysBetween(start, end)
			.Where(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday);
		}

	}
}
