
// Copyright Christophe Bertrand.

using System;
using System.Linq;
using System.Windows.Forms;

namespace Tester
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();

			this.Shown += Form1_Shown;
		}

		void Form1_Shown(object sender, EventArgs e)
		{
			var results = Tester.WinFormTest.TestResults;
			this.dataGridViewResults.DataSource = results;

			WinFormTest.Test(this);

			var nof = results.Count(r => !r.Success);
			this.NumberOfFailures.Text = nof.ToString();
		}
	}
}