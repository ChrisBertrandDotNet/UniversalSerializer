
// Copyright Christophe Bertrand.

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Test_UniversalSerializer;

namespace Tester
{
	public partial class MainPage : UserControl
	{

		public MainPage()
		{
			InitializeComponent();

			this.Loaded += MainPage_Loaded;
		}

		void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				TestResultsDisplay.ItemsSource = Tests.TestResults;

				Tests.RunTests(this);

				var failures = Tests.TestResults.Count(t => !t.Success);
				NumberOfFailures.Content = failures.ToString();
			}
			catch (Exception ex)
			{
				if (Debugger.IsAttached)
					Debugger.Break();

				MessageBox.Show(Application.Current.MainWindow, ex.Message, Application.Current.MainWindow.Title + " - " + ex.GetType().FullName, MessageBoxButton.OK);
				Application.Current.MainWindow.Close();
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
		}
	}
}