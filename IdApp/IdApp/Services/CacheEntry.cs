using System;
using Waher.Persistence.Attributes;

namespace IdApp.Services
{
	/// <summary>
	/// Contains information about a file in the local cache.
	/// </summary>
	[TypeName(TypeNameSerialization.FullName)]
	public class CacheEntry
	{
		private DateTime timeStamp = DateTime.MinValue;
		private string localFileName = string.Empty;

		/// <summary>
		/// Contains information about a file in the local cache.
		/// </summary>
		public CacheEntry()
		{
		}

		/// <summary>
		/// Timestamp of entry
		/// </summary>
		[DefaultValueDateTimeMinValue]
		public DateTime TimeStamp 
		{
			get => this.timeStamp;
			set => this.timeStamp = value;
		}

		/// <summary>
		/// Local file name.
		/// </summary>
		[DefaultValueStringEmpty]
		public string LocalFileName 
		{
			get => this.localFileName;
			set => this.localFileName = value;
		}
	}
}