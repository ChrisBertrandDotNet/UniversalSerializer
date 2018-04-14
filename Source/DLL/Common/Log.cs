
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// Logs non-critical messages.
	/// </summary>
	public static class Log
	{
		static readonly List<string> log;
		/// <summary>
		/// 
		/// </summary>
		public static readonly ReadOnlyCollection<string> LogStrings;

		static Log()
		{
			Log.DLLName = typeof(Log).GetAssembly()// Assembly.GetExecutingAssembly()
#if PORTABLE || SILVERLIGHT
.FullName.Split(',')[0];
#else
				.GetName().Name;
#endif
			Log.log = new List<string>();
			Log.LogStrings = new ReadOnlyCollection<string>(Log.log);
		}

		/// <summary>
		/// Write a line to the Log and to the 'Out' window of the debugger (if launched).
		/// </summary>
		/// <param name="text"></param>
		public static void WriteLine(string text)
		{
			if (Debugger.IsAttached)
			{
				var time = DateTime.Now;
				Debug.WriteLine(string.Format("{0} [{1:00}:{2:00}:{3:00}]: {4}", Log.DLLName, time.Hour, time.Minute, time.Second, text));
			}
			Log.log.Add(text);
		}
		static string DLLName;
	}
}
