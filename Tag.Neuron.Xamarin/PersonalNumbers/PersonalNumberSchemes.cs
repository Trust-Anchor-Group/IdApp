using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Waher.Content.Xml;
using Waher.Script;

namespace Tag.Neuron.Xamarin.PersonalNumbers
{
    /// <summary>
    /// Personal Number Schemes available in different countries.
    /// </summary>
    public static class PersonalNumberSchemes
    {
        private static readonly Dictionary<string, LinkedList<PersonalNumberScheme>> SchemesByCode = new Dictionary<string, LinkedList<PersonalNumberScheme>>();

		private static void LazyLoad()
		{
			try
			{
				XmlDocument doc = new XmlDocument();

				using (MemoryStream ms = new MemoryStream(Waher.Content.Resources.LoadResource($"{typeof(PersonalNumberSchemes).Namespace}.{typeof(PersonalNumberSchemes).Name}.xml")))
				{
					doc.Load(ms);
				}

                XmlNodeList childNodes = doc.DocumentElement?.ChildNodes;
                if (childNodes == null)
                    return;

				foreach (XmlNode n in childNodes)
				{
					if (n is XmlElement e && e.LocalName == "Entry")
					{
						string country = XML.Attribute(e, "country");
						string displayString = XML.Attribute(e, "displayString");
						string variable = null;
						Expression pattern = null;
						Expression check = null;
						Expression normalize = null;

						try
						{
							foreach (XmlNode n2 in e.ChildNodes)
							{
								if (n2 is XmlElement e2)
								{
									switch (e2.LocalName)
									{
										case "Pattern":
											pattern = new Expression(e2.InnerText);
											variable = XML.Attribute(e2, "variable");
											break;

										case "Check":
											check = new Expression(e2.InnerText);
											break;

										case "Normalize":
											normalize = new Expression(e2.InnerText);
											break;
									}
								}
							}
						}
						catch (Exception)
						{
							continue;
						}

						if (pattern is null || string.IsNullOrWhiteSpace(variable) || string.IsNullOrWhiteSpace(displayString))
							continue;

						if (!SchemesByCode.TryGetValue(country, out LinkedList<PersonalNumberScheme> schemes))
						{
							schemes = new LinkedList<PersonalNumberScheme>();
							SchemesByCode[country] = schemes;
						}

						schemes.AddLast(new PersonalNumberScheme(variable, displayString, pattern, check, normalize));
					}
				}
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// Checks if a personal number is valid, in accordance with registered personal number schemes.
		/// </summary>
		/// <param name="countryCode">ISO 3166-1 Country Codes.</param>
		/// <param name="personalNumber">Personal Number</param>
		/// <returns>
		/// true = valid
		/// false = invalid
		/// null = no registered schemes for country.
		/// </returns>
		public static bool? IsValid(string countryCode, ref string personalNumber)
		{
            LazyLoad();
			return IsValid(countryCode, ref personalNumber, out string _);
		}

		/// <summary>
		/// Checks if a personal number is valid, in accordance with registered personal number schemes.
		/// </summary>
		/// <param name="countryCode">ISO 3166-1 Country Codes.</param>
		/// <param name="personalNumber">Personal Number</param>
		/// <param name="displayString">A string that can be displayed to a user, informing the user about the approximate format expected.</param>
		/// <returns>
		/// true = valid: <paramref name="personalNumber"/> may be normalized.
		/// false = invalid
		/// null = no registered schemes for country.
		/// </returns>
		public static bool? IsValid(string countryCode, ref string personalNumber, out string displayString)
		{
            LazyLoad();

			displayString = string.Empty;

			if (SchemesByCode.TryGetValue(countryCode, out LinkedList<PersonalNumberScheme> schemes))
			{
				foreach (PersonalNumberScheme scheme in schemes)
				{
					if (string.IsNullOrWhiteSpace(displayString))
						displayString = scheme.DisplayString;

					bool? valid = scheme.IsValid(ref personalNumber);
					if (valid.HasValue)
						return valid;
				}

				return false;
			}
			else
				return null;
		}

		/// <summary>
		/// Gets the expected personal number format for the given country.
		/// </summary>
		/// <param name="countryCode">ISO 3166-1 Country Codes.</param>
		/// <returns>A string that can be displayed to a user, informing the user about the approximate format expected.</returns>
		public static string DisplayStringForCountry(string countryCode)
        {
            LazyLoad();

            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                if (SchemesByCode.TryGetValue(countryCode, out LinkedList<PersonalNumberScheme> schemes))
                {
                    return schemes.First?.Value?.DisplayString;
                }
            }

            return null;
        }
    }
}
