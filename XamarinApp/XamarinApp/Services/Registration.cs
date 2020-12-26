using System;
using System.Collections.Generic;

namespace XamarinApp.Services
{
	public partial class TagProfile
	{
		private Dictionary<string, KeyValuePair<string, string>> clp = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.CurrentCultureIgnoreCase)
		{
			{ "lab.tagroot.io", new KeyValuePair<string, string>("866aae6b9f03ef5c6d662152d72b5a29378dee9e6080b4e7be78964a3530d1e1", "ba1a94e29a716421ee1bbeed022582cb16904905b625fbf8323b1aae25df3801") }
		};
	}
}
