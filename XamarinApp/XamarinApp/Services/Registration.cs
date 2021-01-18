using System;
using System.Collections.Generic;
using System.Linq;
using Tag.Sdk.Core.Models;

namespace XamarinApp.Services
{
	public class Registration
	{
		private Dictionary<string, KeyValuePair<string, string>> clp = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.CurrentCultureIgnoreCase)
		{
			{ "domain", new KeyValuePair<string, string>("", "") }
		};

		public DomainModel[] ToArray()
        {
            return clp.Select(x => new DomainModel(x.Key, x.Value.Key, x.Value.Value)).ToArray();
        }
	}
}
