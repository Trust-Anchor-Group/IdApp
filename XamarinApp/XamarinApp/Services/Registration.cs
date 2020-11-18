using System;
using System.Collections.Generic;

namespace XamarinApp.Services
{
	public partial class TagProfile
	{
		private Dictionary<string, KeyValuePair<string, string>> clp = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.CurrentCultureIgnoreCase)
		{/*
		  *	{ "domain", new KeyValuePair<string, string>("", "") }
		  *
		  *	Add records as above, for featured servers.
		  *
		  *	NOTE: Don't check such records into the repository, as this would allow anyone with access to the repository easy access
		  *	      to your keys. Instead, create a duplicate Registration.cs file and put it in the parent folder of the solution
		  *	      folder. This version will be temporarily copied to this file before compilation, and this file will be restored
		  *	      after.
		  */
		};
	}
}
