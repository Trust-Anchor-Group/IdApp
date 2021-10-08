using System;
using System.Collections.Generic;

namespace IdApp.Services.Data.Countries
{
    /// <summary>
    /// Conversion between Country Names and ISO-3166-1 2-letter country codes.
    /// </summary>
    public static class ISO_3166_1
    {
        private static readonly SortedDictionary<string, string> codeByCountry = new SortedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private static readonly SortedDictionary<string, string> countryByCode = new SortedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
           { "AF", "AFGHANISTAN" },
           { "AX", "ÅLAND ISLANDS" },
           { "AL", "ALBANIA" },
           { "DZ", "ALGERIA" },
           { "AS", "AMERICAN SAMOA" },
           { "AD", "ANDORRA" },
           { "AO", "ANGOLA" },
           { "AI", "ANGUILLA" },
           { "AQ", "ANTARCTICA" },
           { "AG", "ANTIGUA AND BARBUDA" },
           { "AR", "ARGENTINA" },
           { "AM", "ARMENIA" },
           { "AW", "ARUBA" },
           { "AU", "AUSTRALIA" },
           { "AT", "AUSTRIA" },
           { "AZ", "AZERBAIJAN" },
           { "BS", "BAHAMAS" },
           { "BH", "BAHRAIN" },
           { "BD", "BANGLADESH" },
           { "BB", "BARBADOS" },
           { "BY", "BELARUS" },
           { "BE", "BELGIUM" },
           { "BZ", "BELIZE" },
           { "BJ", "BENIN" },
           { "BM", "BERMUDA" },
           { "BT", "BHUTAN" },
           { "BO", "BOLIVIA" },
           { "BA", "BOSNIA AND HERZEGOVINA" },
           { "BW", "BOTSWANA" },
           { "BV", "BOUVET ISLAND" },
           { "BR", "BRAZIL" },
           { "IO", "BRITISH INDIAN OCEAN TERRITORY" },
           { "BN", "BRUNEI DARUSSALAM" },
           { "BG", "BULGARIA" },
           { "BF", "BURKINA FASO" },
           { "BI", "BURUNDI" },
           { "KH", "CAMBODIA" },
           { "CM", "CAMEROON" },
           { "CA", "CANADA" },
           { "CV", "CAPE VERDE" },
           { "KY", "CAYMAN ISLANDS" },
           { "CF", "CENTRAL AFRICAN REPUBLIC" },
           { "TD", "CHAD" },
           { "CL", "CHILE" },
           { "CN", "CHINA" },
           { "CX", "CHRISTMAS ISLAND" },
           { "CC", "COCOS (KEELING) ISLANDS" },
           { "CO", "COLOMBIA" },
           { "KM", "COMOROS" },
           { "CG", "CONGO" },
           { "CD", "CONGO, THE DEMOCRATIC REPUBLIC OF THE" },
           { "CK", "COOK ISLANDS" },
           { "CR", "COSTA RICA" },
           { "CI", "COTE D'IVOIRE" },
           { "HR", "CROATIA" },
           { "CU", "CUBA" },
           { "CY", "CYPRUS" },
           { "CZ", "CZECH REPUBLIC" },
           { "DK", "DENMARK" },
           { "DJ", "DJIBOUTI" },
           { "DM", "DOMINICA" },
           { "DO", "DOMINICAN REPUBLIC" },
           { "EC", "ECUADOR" },
           { "EG", "EGYPT" },
           { "SV", "EL SALVADOR" },
           { "GQ", "EQUATORIAL GUINEA" },
           { "ER", "ERITREA" },
           { "EE", "ESTONIA" },
           { "ET", "ETHIOPIA" },
           { "FK", "FALKLAND ISLANDS (MALVINAS)" },
           { "FO", "FAROE ISLANDS" },
           { "FJ", "FIJI" },
           { "FI", "FINLAND" },
           { "FR", "FRANCE" },
           { "GF", "FRENCH GUIANA" },
           { "PF", "FRENCH POLYNESIA" },
           { "TF", "FRENCH SOUTHERN TERRITORIES" },
           { "GA", "GABON" },
           { "GM", "GAMBIA" },
           { "GE", "GEORGIA" },
           { "DE", "GERMANY" },
           { "GH", "GHANA" },
           { "GI", "GIBRALTAR" },
           { "GR", "GREECE" },
           { "GL", "GREENLAND" },
           { "GD", "GRENADA" },
           { "GP", "GUADELOUPE" },
           { "GU", "GUAM" },
           { "GT", "GUATEMALA" },
           { "GG", "GUERNSEY" },
           { "GN", "GUINEA" },
           { "GW", "GUINEA-BISSAU" },
           { "GY", "GUYANA" },
           { "HT", "HAITI" },
           { "HM", "HEARD ISLAND AND MCDONALD ISLANDS" },
           { "VA", "HOLY SEE (VATICAN CITY STATE)" },
           { "HN", "HONDURAS" },
           { "HK", "HONG KONG" },
           { "HU", "HUNGARY" },
           { "IS", "ICELAND" },
           { "IN", "INDIA" },
           { "ID", "INDONESIA" },
           { "IR", "IRAN, ISLAMIC REPUBLIC OF" },
           { "IQ", "IRAQ" },
           { "IE", "IRELAND" },
           { "IM", "ISLE OF MAN" },
           { "IL", "ISRAEL" },
           { "IT", "ITALY" },
           { "JM", "JAMAICA" },
           { "JP", "JAPAN" },
           { "JE", "JERSEY" },
           { "JO", "JORDAN" },
           { "KZ", "KAZAKHSTAN" },
           { "KE", "KENYA" },
           { "KI", "KIRIBATI" },
           { "KP", "KOREA, DEMOCRATIC PEOPLE'S REPUBLIC OF" },
           { "KR", "KOREA, REPUBLIC OF" },
           { "KW", "KUWAIT" },
           { "KG", "KYRGYZSTAN" },
           { "LA", "LAO PEOPLE'S DEMOCRATIC REPUBLIC" },
           { "LV", "LATVIA" },
           { "LB", "LEBANON" },
           { "LS", "LESOTHO" },
           { "LR", "LIBERIA" },
           { "LY", "LIBYAN ARAB JAMAHIRIYA" },
           { "LI", "LIECHTENSTEIN" },
           { "LT", "LITHUANIA" },
           { "LU", "LUXEMBOURG" },
           { "MO", "MACAO" },
           { "MK", "MACEDONIA, THE FORMER YUGOSLAV REPUBLIC OF" },
           { "MG", "MADAGASCAR" },
           { "MW", "MALAWI" },
           { "MY", "MALAYSIA" },
           { "MV", "MALDIVES" },
           { "ML", "MALI" },
           { "MT", "MALTA" },
           { "MH", "MARSHALL ISLANDS" },
           { "MQ", "MARTINIQUE" },
           { "MR", "MAURITANIA" },
           { "MU", "MAURITIUS" },
           { "YT", "MAYOTTE" },
           { "MX", "MEXICO" },
           { "FM", "MICRONESIA, FEDERATED STATES OF" },
           { "MD", "MOLDOVA, REPUBLIC OF" },
           { "MC", "MONACO" },
           { "MN", "MONGOLIA" },
           { "MS", "MONTSERRAT" },
           { "MA", "MOROCCO" },
           { "MZ", "MOZAMBIQUE" },
           { "MM", "MYANMAR" },
           { "NA", "NAMIBIA" },
           { "NR", "NAURU" },
           { "NP", "NEPAL" },
           { "NL", "NETHERLANDS" },
           { "AN", "NETHERLANDS ANTILLES" },
           { "NC", "NEW CALEDONIA" },
           { "NZ", "NEW ZEALAND" },
           { "NI", "NICARAGUA" },
           { "NE", "NIGER" },
           { "NG", "NIGERIA" },
           { "NU", "NIUE" },
           { "NF", "NORFOLK ISLAND" },
           { "MP", "NORTHERN MARIANA ISLANDS" },
           { "NO", "NORWAY" },
           { "OM", "OMAN" },
           { "PK", "PAKISTAN" },
           { "PW", "PALAU" },
           { "PS", "PALESTINIAN TERRITORY, OCCUPIED" },
           { "PA", "PANAMA" },
           { "PG", "PAPUA NEW GUINEA" },
           { "PY", "PARAGUAY" },
           { "PE", "PERU" },
           { "PH", "PHILIPPINES" },
           { "PN", "PITCAIRN" },
           { "PL", "POLAND" },
           { "PT", "PORTUGAL" },
           { "PR", "PUERTO RICO" },
           { "QA", "QATAR" },
           { "RE", "REUNION" },
           { "RO", "ROMANIA" },
           { "RU", "RUSSIAN FEDERATION" },
           { "RW", "RWANDA" },
           { "SH", "SAINT HELENA" },
           { "KN", "SAINT KITTS AND NEVIS" },
           { "LC", "SAINT LUCIA" },
           { "PM", "SAINT PIERRE AND MIQUELON" },
           { "VC", "SAINT VINCENT AND THE GRENADINES" },
           { "WS", "SAMOA" },
           { "SM", "SAN MARINO" },
           { "ST", "SAO TOME AND PRINCIPE" },
           { "SA", "SAUDI ARABIA" },
           { "SN", "SENEGAL" },
           { "CS", "SERBIA AND MONTENEGRO" },
           { "SC", "SEYCHELLES" },
           { "SL", "SIERRA LEONE" },
           { "SG", "SINGAPORE" },
           { "SK", "SLOVAKIA" },
           { "SI", "SLOVENIA" },
           { "SB", "SOLOMON ISLANDS" },
           { "SO", "SOMALIA" },
           { "ZA", "SOUTH AFRICA" },
           { "GS", "SOUTH GEORGIA AND THE SOUTH SANDWICH ISLANDS" },
           { "ES", "SPAIN" },
           { "LK", "SRI LANKA" },
           { "SD", "SUDAN" },
           { "SR", "SURINAME" },
           { "SJ", "SVALBARD AND JAN MAYEN" },
           { "SZ", "SWAZILAND" },
           { "SE", "SWEDEN" },
           { "CH", "SWITZERLAND" },
           { "SY", "SYRIAN ARAB REPUBLIC" },
           { "TW", "TAIWAN, PROVINCE OF CHINA" },
           { "TJ", "TAJIKISTAN" },
           { "TZ", "TANZANIA, UNITED REPUBLIC OF" },
           { "TH", "THAILAND" },
           { "TL", "TIMOR-LESTE" },
           { "TG", "TOGO" },
           { "TK", "TOKELAU" },
           { "TO", "TONGA" },
           { "TT", "TRINIDAD AND TOBAGO" },
           { "TN", "TUNISIA" },
           { "TR", "TURKEY" },
           { "TM", "TURKMENISTAN" },
           { "TC", "TURKS AND CAICOS ISLANDS" },
           { "TV", "TUVALU" },
           { "UG", "UGANDA" },
           { "UA", "UKRAINE" },
           { "AE", "UNITED ARAB EMIRATES" },
           { "GB", "UNITED KINGDOM" },
           { "US", "UNITED STATES" },
           { "UM", "UNITED STATES MINOR OUTLYING ISLANDS" },
           { "UY", "URUGUAY" },
           { "UZ", "UZBEKISTAN" },
           { "VU", "VANUATU" },
           { "VE", "VENEZUELA" },
           { "VN", "VIET NAM" },
           { "VG", "VIRGIN ISLANDS, BRITISH" },
           { "VI", "VIRGIN ISLANDS, U.S." },
           { "WF", "WALLIS AND FUTUNA" },
           { "EH", "WESTERN SAHARA" },
           { "YE", "YEMEN" },
           { "ZM", "ZAMBIA" },
           { "ZW", "ZIMBABWE" }
        };

        static ISO_3166_1()
        {
            foreach (KeyValuePair<string, string> pair in countryByCode)
                codeByCountry[pair.Value] = pair.Key;
        }

        /// <summary>
        /// Available country codes, ordered alphabetically
        /// </summary>
        public static string[] Codes
        {
            get
            {
                string[] codes = new string[countryByCode.Count];
                countryByCode.Keys.CopyTo(codes, 0);
                return codes;
            }
        }

        /// <summary>
        /// Available counties, ordered alphabetically
        /// </summary>
        public static string[] Countries
        {
            get
            {
                string[] countries = new string[codeByCountry.Count];
                codeByCountry.Keys.CopyTo(countries, 0);
                return countries;
            }
        }

        /// <summary>
        /// Tries to get the country name, given its country code.
        /// </summary>
        /// <param name="code">ISO-3166-1 Country code (case insensitive).</param>
        /// <param name="country">Country name, if found.</param>
        /// <returns>If a country was found matching the country code.</returns>
        public static bool TryGetCountry(string code, out string country)
        {
            return countryByCode.TryGetValue(code, out country);
        }

        /// <summary>
        /// Tries to get the ISO-3166-1 country code, given its country name.
        /// </summary>
        /// <param name="country">Country name (case insensitive).</param>
        /// <param name="code">ISO-3166-1 Country code, if found.</param>
        /// <returns>If a country code was found matching the country name.</returns>
        public static bool TryGetCode(string country, out string code)
        {
            return codeByCountry.TryGetValue(country, out code);
        }

        /// <summary>
        /// Converts the code to a country name (if found). If not found, returns the original code.
        /// </summary>
        /// <param name="code">Country code.</param>
        /// <returns>Country name, or if not found, the original code.</returns>
        public static string ToName(string code)
        {
            if (TryGetCountry(code, out string country))
                return country;
            return code;
        }

        /// <summary>
        /// Converts the name to a country code (if found). If not found, returns the original name.
        /// </summary>
        /// <param name="country">Country name.</param>
        /// <returns>Country code, or if not found, the original name.</returns>
        public static string ToCode(string country)
        {
            if (TryGetCode(country, out string code))
                return code;
            return country;
        }
    }
}
