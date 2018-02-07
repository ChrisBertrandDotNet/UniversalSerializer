using System;
using System.Windows.Forms;

namespace UniversalSerializer_Tester_for_WinForm
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
			if (this.Text == "fen2")
				return;

			WinFormTest.Test(this);
		}

	}
}
