using System;
using System.Collections.Generic;

namespace Waher.IoTGateway.Setup
{
	public partial class XmppConfiguration
	{
		private readonly Dictionary<string, KeyValuePair<string, string>> clp = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.CurrentCultureIgnoreCase)
		{
			{ "domain", new KeyValuePair<string, string>("", "") }
		};
	}
}
