using System;
using System.Collections.Generic;

namespace Waher.IoTGateway.Setup
{
	public partial class XmppConfiguration
	{
		private static Dictionary<string, KeyValuePair<string, string>> clp = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.CurrentCultureIgnoreCase)
		{/*
		  *	{ "lab.tagroot.io", new KeyValuePair<string, string>("866aae6b9f03ef5c6d662152d72b5a29378dee9e6080b4e7be78964a3530dlel", "bala94e29a71642leelbbeed022582cb16904905b625fbf8323blaae25df3801") }
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
