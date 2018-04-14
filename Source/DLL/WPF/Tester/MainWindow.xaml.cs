
// Copyright Christophe Bertrand.

using System.Collections.Generic;
using System.Windows;
using System.Linq;

namespace Tester
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			this.Loaded += MainWindow_Loaded;
		}

		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			this.NumberOfFailures.Content = null;

			Test_UniversalSerializer.Tests.RunTests(this);

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
				this.NumberOfFailures.Content = Failed.Count.ToString();
			}
		}
	}
}