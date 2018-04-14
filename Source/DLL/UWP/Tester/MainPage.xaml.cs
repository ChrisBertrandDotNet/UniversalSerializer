
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalSerializerLib3;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Test_UniversalSerializer;

namespace Tester
{
	/// <summary>
	/// </summary>
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

			Tests.RunTests(this);
			var failures = Tests.TestResults.Where(t => !t.Success).ToArray();
			var failureCount = failures.Length;

			this.FailureCount.Text = failureCount.ToString();
			this.TestList.ItemsSource = Tests.TestResults;
		}

		private void CheckBox_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}