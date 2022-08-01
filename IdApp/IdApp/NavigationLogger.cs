using System;
using System.Diagnostics;

namespace IdApp
{
	internal static class NavigationLogger
	{
		public static void Log(string Message)
		{
			Debug.Write("[" + DateTime.Now.ToString("HH:mm:ss.ffffff") + "] " + Message, nameof(NavigationLogger));
		}
	}
}
