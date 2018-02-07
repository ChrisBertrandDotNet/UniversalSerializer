using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Test_UniversalSerializer;
using UniversalSerializerLib3;

namespace Test_UniversalSerializer
{
	public partial class MainPage : UserControl
	{

		const SerializerFormatters TestSerFormatter = SerializerFormatters.BinarySerializationFormatter;
		//const DeserializerFormatters TestDeserFormatter = DeserializerFormatters.BinaryDeserializationFormatter;


		public MainPage()
		{
			InitializeComponent();

			this.Loaded += MainPage_Loaded;

		}

		void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			Tests.RunTests(this);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
		}
	}
}
