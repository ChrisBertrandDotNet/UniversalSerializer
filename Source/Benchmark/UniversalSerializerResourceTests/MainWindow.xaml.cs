
// Copyright Christophe Bertrand.

//#define TIMER_DEBUGGING

using System;
using System.Collections;
using System.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using System.IO;

namespace UniversalSerializerResourceTests
{

	public enum StreamManagement { SerializeInRAM, SerializeToFile };

	/// <summary>
	/// Logique d'interaction pour MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		public IEnumerable SerializersCollection { get {
			return
				CommonOptions.SerializerInstances.Select((ser) => ser.Name);
		} }

		public IEnumerable DataCollection
		{			get
			{				return AvailableDataDescriptors.Descriptors;			}		}

		public IEnumerable StreamManagementCollection
		{
			get
			{
				return Enum.GetValues(typeof(StreamManagement));
			}
		}

		ObservableCollection<string> LogStrings { get; set; }
		ObservableCollection<string> SeriesLogStrings { get; set; }

		readonly Brush NormalTextboxBackgroundColor;
		readonly Brush OrangeBrush = new SolidColorBrush(new Color() { R = 255, G = 160, B = 80, A = 255 });

		public MainWindow()
		{
			InitializeComponent();

			this.NormalTextboxBackgroundColor = this.TableFileName.Background;
			this.TableFileName.TextChanged += TableFileName_TextChanged;

			this.LogStrings = new ObservableCollection<string>();
			this.Log.ItemsSource = this.LogStrings;

			this.SeriesLogStrings = new ObservableCollection<string>();
			this.SeriesLog.ItemsSource = this.SeriesLogStrings;

			TableFileName_TextChanged(null, null);
		}

		void TableFileName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			bool PathExists;
			try
			{
				PathExists = Directory.Exists(Path.GetDirectoryName(TableFileName.Text));
			}
			catch
			{
				PathExists = false;
			}

			this.TableFileName.Background =
				PathExists ?
				this.NormalTextboxBackgroundColor
				: OrangeBrush;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.UnitTestOptions.Compute(CommonOptions.SerializerInstances[this.Serializer.SelectedIndex], null,null);
		}


		private void Window_Closed(object sender, EventArgs e)
		{
			//Application.Current.Shutdown(); // closes any dynamic window as well.
		}

		private void ButtonRunWholeTest_Click(object sender, RoutedEventArgs e)
		{
			this.LogStrings.Clear();
			this.WholeTestOptions.Compute(null, this.LogStrings, this.TableFileName.Text);
		}

		private void StartSeries_Click(object sender, RoutedEventArgs e)
		{
			this.SeriesLogStrings.Clear();
		}
	}


	// #############################################################################

	// #############################################################################

}
