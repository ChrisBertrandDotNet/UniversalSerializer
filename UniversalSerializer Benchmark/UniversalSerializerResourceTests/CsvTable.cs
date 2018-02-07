
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalSerializerResourceTests
{
	/// <summary>
	/// This class exports a table to a CSV file.
	/// </summary>
	public class CsvTable
	{
		readonly IEnumerable<string> Titles;
		readonly List<IEnumerable<object>> Lines = new List<IEnumerable<object>>();
		readonly char Separator;

		public CsvTable(IEnumerable<string> Titles, char Separator = '\t')
		{
			this.Titles = Titles;
			this.Separator = Separator;
		}

		public void AddLine(IEnumerable<object> Cells)
		{
			this.Lines.Add(Cells);
		}

		public void ExportToFile(string FileName)
		{
			using (var sw = File.CreateText(FileName))
			{
				// Write the titles:
				foreach (string s in this.Titles)
				{
					sw.Write(s);
					sw.Write(this.Separator);
				}
				sw.WriteLine();

				// write lines:
				foreach (var line in this.Lines)
				{
					// Write cells of this line:
					foreach (var cell in line)
					{
						sw.Write(cell);
						sw.Write(this.Separator);
					}
					sw.WriteLine();
				}
			}
		}
	}
}
