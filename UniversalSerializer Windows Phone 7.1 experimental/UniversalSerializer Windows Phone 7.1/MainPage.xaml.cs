using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Reflection;

namespace Test_UniversalSerializer
{
	public partial class MainPage : PhoneApplicationPage
	{
		// Constructeur
		public MainPage()
		{
			InitializeComponent();

			this.Loaded += new RoutedEventHandler(MainPage_Loaded);
		}

		void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			Tests.RunTests(this);
		}

	}
}