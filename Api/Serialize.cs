using System;
using System.Linq;
using System.Globalization;
using System.Xml.Serialization;

namespace CalendarService.Serialize
{
	
	#region XMLCalendar.
	/// <summary>
	/// Общий класс календаря.
	/// </summary>
	[XmlRoot(ElementName = "calendar")]
	[Serializable()]
	public class Calendar
	{
		/// <summary>
		/// Год.
		/// </summary>
		[XmlAttribute("year")]
		public int Year { get; set; }

		/// <summary>
		/// Названия праздников.
		/// </summary>
		[XmlElement("holidays")]
		public Holidays Holidays { get; set; }

		/// <summary>
		/// Массив дней  в виде строк (праздники/короткие дни/рабочие дни).
		/// </summary>
		[XmlElement("days")]
		public Days Days { get; set; }

		/// <summary>
		/// Массив дней в виде дат (праздники/короткие дни/рабочие дни).
		/// </summary>
		internal DayInfo[] DayInfos
		{
			get
			{
				return Days.Day
					.Select(x =>
							{
								var dayInfo = new DayInfo();
								dayInfo.Type = x.Type;
								dayInfo.Date =  DateTime.TryParseExact($"{x.Date}.{Year}", "MM.dd.yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime date) ? 
								date : 
								DateTime.MinValue;
								return dayInfo;
							})
					.Where(x => x.Date > DateTime.MinValue)
					.ToArray();
			}
		}
	}

	/// <summary>
	/// Класс описания названий праздников.
	/// </summary>
	[XmlRoot()]
	[Serializable()]
	public class Holidays
	{
		/// <summary>
		/// Названия праздников.
		/// </summary>
		[XmlElement("holiday")]
		public Holiday[] Holiday { get; set; }
	}

	/// <summary>
	/// Класс описания дней.
	/// </summary>
	[XmlRoot()]
	[Serializable()]
	public class Days
	{
		/// <summary>
		/// Массив дней  в виде строк (праздники/короткие дни/рабочие дни).
		/// </summary>
		[XmlElement("day")]
		public Day[] Day { get; set; }
	}

	/// <summary>
	/// Класс описания праздника.
	/// </summary>
	[XmlType("holiday")]
	[Serializable()]
	public class Holiday
	{
		/// <summary>
		/// Ид.
		/// </summary>
		[XmlAttribute("id")]
		public byte Id { get; set; }

		/// <summary>
		/// Название праздника.
		/// </summary>
		[XmlAttribute("title")]
		public string Title { get; set; }
	}

	/// <summary>
	/// Класс описания дня.
	/// </summary>
	[XmlType("day")]
	[Serializable()]
	public class Day
	{
		/// <summary>
		/// Тип дня.
		/// </summary>
		/// <remarks>1 - выходной день, 2 - рабочий и сокращенный (может быть использован для любого дня недели), 3 - рабочий день (суббота/воскресенье)</remarks>
		[XmlAttribute("t")]
		public byte Type { get; set; }

		/// <summary>
		/// День в формате ММ.ДД.
		/// </summary>
		[XmlAttribute("d")]
		public string Date { get; set; }

		/// <summary>
		/// Номер праздника (ссылка на атрибут id тэга holiday)
		/// </summary>
		[XmlAttribute("h")]
		public byte HolidayId { get; set; }
	}

	/// <summary>
	/// Структура описания дня.
	/// </summary>
	internal struct DayInfo
	{
		/// <summary>
		/// Дата.
		/// </summary>
		internal DateTime Date { get; set; }

		/// <summary>
		/// Тип дня.
		/// </summary>
		/// <remarks>1 - выходной день, 2 - рабочий и сокращенный (может быть использован для любого дня недели), 3 - рабочий день (суббота/воскресенье)</remarks>
		internal byte Type { get; set; }
	}
	#endregion
}
