
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Serialization;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections;
using UniversalSerializerLib3;

namespace Test_UniversalSerializer
{

	/// <summary>
	/// Logique d'interaction pour MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		public MainWindow()
		{
			InitializeComponent();

			this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
		}

		public void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{

			Tests.RunTests(this);

		}

		// ===============================================================================

		private void Window_Closed(object sender, EventArgs e)
		{
			//Application.Current.Shutdown(); // closes any dynamic window as well.
		}

		// ===============================================================================

	}
}
