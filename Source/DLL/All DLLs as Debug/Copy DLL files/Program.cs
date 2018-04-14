
// Copyright Christophe Bertrand.

using System;
using System.Linq;
using System.IO;

namespace Copy_DLL_files
{
	class Program
	{
		static void Main(string[] args)
		{
			var d = Directory.GetCurrentDirectory();
			var source = Path.Combine(d, "Files");
			if (!Directory.Exists(source))
				throw new DirectoryNotFoundException("The \"Files\" sub-directory has been renamed or it does not exist.");
			var diSource = new DirectoryInfo(source);

			DirectoryInfo di = new DirectoryInfo(d);
			for (int i = 0; i < 3; i++)
				di = Directory.GetParent(di.FullName);
			if (!di.FullName.EndsWith("All DLLs as Debug") && !di.FullName.EndsWith("All DLLs as Release"))
				throw new DirectoryNotFoundException("A parent directory has been renamed.");
			var dest = Path.Combine(di.FullName, "DLLs");
			if (!Directory.Exists(dest))
			{
				Console.Write("Creating directory \"{0}\" ..", dest);
				di.CreateSubdirectory("DLLs");
				Console.WriteLine("  Done.");
			}

			foreach (var file in diSource.EnumerateFiles())
			{
				Console.Write("Copying file \"{0}\" ..", file.Name);
				var fi = file.CopyTo(Path.Combine(dest, file.Name), true);
				Console.WriteLine("  Done.");
			}

			Console.WriteLine("All DLLs are copied to this directory: \"{0}\"", dest);
			Console.WriteLine("Press any key to close this program..");
			Console.ReadKey();
		}
	}
}