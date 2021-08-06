using System;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="Contract"/> class.
	/// </summary>
	public static class ContractExtensions
	{
		/// <summary>
		/// Returns the language to use when displaying a contract on the device
		/// </summary>
		/// <param name="Contract">Contract</param>
		/// <returns>Language to use when displaying contract.</returns>
		public static string DeviceLanguage(this Contract Contract)
		{
			string[] Languages = Contract.GetLanguages();
			string Language = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
			int i;

			foreach (string Option in Languages)
			{
				i = Option.IndexOf('-');
				if (i < 0)
				{
					if (string.Compare(Option, Language, true) == 0)
						return Option;
				}
				else
				{
					if (string.Compare(Option.Substring(0, i), Language, true) == 0)
						return Option;
				}
			}

			return Contract.DefaultLanguage;
		}
	}
}