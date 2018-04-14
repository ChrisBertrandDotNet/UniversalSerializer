
// Copyright Christophe Bertrand.

using System.Diagnostics;
using System.Linq;
using Xamarin.Forms;

namespace Tester
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			Test_UniversalSerializer.Tests.RunTests(this);

			var tests = Test_UniversalSerializer.Tests.TestResults;
			var failureCount = tests.Count(t => !t.Success);
			this.FailureCount.Text = failureCount.ToString();
			var failedTests = tests.Where(t => !t.Success).ToArray();
			this.TestList.ItemsSource = tests;
			/*if (failureCount != 0 && Debugger.IsAttached)
				Debugger.Break();*/
		}
	}
}