using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Script;

namespace XamarinApp.PersonalNumbers
{
    /// <summary>
    /// Personal Number Schemes available in different countries.
    /// </summary>
    public static class PersonalNumberSchemes
    {
        private static readonly Dictionary<string, LinkedList<PersonalNumberScheme>> SchemesByCode = new Dictionary<string, LinkedList<PersonalNumberScheme>>();

        private static void LazyLoad()
        {
            if (SchemesByCode.Count > 0)
            {
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();

                using (Stream ms = typeof(PersonalNumberSchemes).Assembly.GetManifestResourceStream($"{typeof(PersonalNumberSchemes).Namespace}.{typeof(PersonalNumberSchemes).Name}.xml"))
                {
                    doc.Load(ms);
                }

                foreach (XmlNode n in doc.DocumentElement.ChildNodes)
                {
                    if (n is XmlElement e && e.LocalName == "Entry")
                    {
                        string country = XML.Attribute(e, "country");
                        string variable = null;
                        string displayString = XML.Attribute(e, "displayString");
                        Expression pattern = null;
                        Expression check = null;

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
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Critical(ex);
                            continue;
                        }

                        if (pattern is null || string.IsNullOrEmpty(variable) || string.IsNullOrEmpty(displayString))
                            continue;

                        if (!SchemesByCode.TryGetValue(country, out LinkedList<PersonalNumberScheme> schemes))
                        {
                            schemes = new LinkedList<PersonalNumberScheme>();
                            SchemesByCode[country] = schemes;
                        }

                        schemes.AddLast(new PersonalNumberScheme(variable, displayString, pattern, check));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
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
        public static bool? IsValid(string countryCode, string personalNumber)
        {
            LazyLoad();

            return IsValid(countryCode, personalNumber, out string _);
        }

        /// <summary>
        /// Checks if a personal number is valid, in accordance with registered personal number schemes.
        /// </summary>
        /// <param name="countryCode">ISO 3166-1 Country Codes.</param>
        /// <param name="personalNumber">Personal Number</param>
        /// <param name="displayString">A string that can be displayed to a user, informing the user about the approximate format expected.</param>
        /// <returns>
        /// true = valid
        /// false = invalid
        /// null = no registered schemes for country.
        /// </returns>
        public static bool? IsValid(string countryCode, string personalNumber, out string displayString)
        {
            LazyLoad();

            displayString = string.Empty;

            if (SchemesByCode.TryGetValue(countryCode, out LinkedList<PersonalNumberScheme> schemes))
            {
                foreach (PersonalNumberScheme scheme in schemes)
                {
                    if (string.IsNullOrEmpty(displayString))
                        displayString = scheme.DisplayString;

                    bool? valid = scheme.IsValid(personalNumber);
                    if (valid.HasValue)
                        return valid;
                }

                return false;
            }
            return null;
        }
    }
}
