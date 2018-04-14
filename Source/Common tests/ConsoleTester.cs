
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tester
{
	public static class ConsoleTester
	{
		public static void Test()
		{
			Test_UniversalSerializer.Tests.RunTests(null);

			{
				var log = Test_UniversalSerializer.Tests.TestResults;
				List<string> Failed = new List<string>();
				bool AllOK =
					log.All(p =>
					{
						if (!p.Success)
							Failed.Add(p.Title + "\n" + p.Error);
						return p.Success;
					});
				Console.WriteLine("Count of failed tests: {0}", Failed.Count.ToString());
				Console.WriteLine("----------- All tests: -----------");
				foreach (var test in log)
				{
					Console.WriteLine("#{0} {1}", test.Order, test.ToString());
				}

				if (!AllOK)
				{
					Console.WriteLine("----------- FAILED tests: -----------");
					foreach (var f in Failed)
					{
						Console.WriteLine(f);
					}
				}

				Console.WriteLine("Press any key to exit.");
				Console.ReadKey();
			}
		}
	}
}
