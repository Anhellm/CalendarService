using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalendarService
{
	/// <summary>
	/// Класс с данными по выходным дням.
	/// </summary>
	public class WeekendData
	{
		/// <summary>
		/// Год.
		/// </summary>
		public int Year { get; set; }

		/// <summary>
		/// Список месяцев.
		/// </summary>
		public List<Month> Months { get; set; }

		/// <summary>
		/// Информация о праздниках.
		/// </summary>
		public string HolidayInfo { get; set; }

		#region Конструкторы.
		public WeekendData(int year, List<Month> months, string holidayInfo)
		{
			Year = year;
			Months = months;
			HolidayInfo = holidayInfo;
		}

		private WeekendData() { }
		#endregion
	}

	/// <summary>
	/// Класс с данными по выходням дням за месяц.
	/// </summary>
	public class Month
	{
		private string name;
		private int number;

		/// <summary>
		/// Наименование.
		/// </summary>
		public string Name
		{
			get => name;
			set
			{
				name = value;

				if (DateTime.TryParseExact(value, "MMMM", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime month))
					number = month.Month;
			}
		}

		/// <summary>
		/// Номер.
		/// </summary>
		public int Number
		{
			get => number;
			set
			{
				number = value;
				name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(number);
			}
		}

		/// <summary>
		/// Список выходных дней.
		/// </summary>
		public string[] Weekends { get; set; }

		/// <summary>
		/// Список праздничных дней.
		/// </summary>
		public string[] Holidays { get; set; }

		/// <summary>
		/// Список предпраздничных дней.
		/// </summary>
		public string[] PreHolidays { get; set; }
	}
}
