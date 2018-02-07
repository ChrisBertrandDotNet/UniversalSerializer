
// Copyright Christophe Bertrand.

//#define USE_FASTBINARYJSON
//#define USE_FASTJSON
//#define USE_UNIVERSALSERIALIZERV1

using System;
using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace UniversalSerializerResourceTests
{
	/// <summary>
	/// Logique d'interaction pour CommonOptions.xaml
	/// </summary>
	public partial class CommonOptions : UserControl
	{

		static readonly string[] TableTitles = new string[] { 
			"Serializer", "Time (ms)", "File length", "Average item size", "Consumed GC memory", "Consumed Working set", "Time (%)", "File length (%)", "Consumed GC memory (%)", "Consumed Working set (%)", "Total (%)", "Bytes/ms", "Data/file lengths", "data length/GC memory", "data length/Working set" };

		readonly Brush NormalTextboxBackgroundColor;
		readonly Brush OrangeBrush = new SolidColorBrush(new Color() { R = 255, G = 160, B = 80, A = 255 });

		// ------------------------------------------------------

		public IEnumerable DataCollection
		{
			get
			{ return AvailableDataDescriptors.Descriptors; }
		}

		public IEnumerable StreamManagementCollection
		{
			get
			{
				return Enum.GetValues(typeof(StreamManagement));
			}
		}

		// ------------------------------------------------------

		public CommonOptions()
		{
			InitializeComponent();

			this.NormalTextboxBackgroundColor = this.FileName.Background;
			this.FileName.TextChanged += FileName_TextChanged;

			FileName_TextChanged(null, null);

		}

		void FileName_TextChanged(object sender, TextChangedEventArgs e)
		{
			bool PathExists;
			try
			{
				PathExists = Directory.Exists(Path.GetDirectoryName(FileName.Text));
			}
			catch
			{
				PathExists = false;
			}

			this.FileName.Background =
				PathExists ?
				this.NormalTextboxBackgroundColor
				: OrangeBrush;
		}

		// ------------------------------------------------------

		public void Compute(Serializer SelectedSerializer, ObservableCollection<string> Log, string csvTableFileName)
		{
			this.TextBoxState.Text = "Computing... please wait.";
			this.StreamSize.Text = string.Empty;
			var cursor = this.Cursor;
			this.Cursor = Cursors.Wait;
			DoEvents();

			int itemCount = int.Parse(this.ItemCount.Text);
			int loopCount = int.Parse(this.LoopCount.Text);

			DataDescriptor dataDescriptor = (DataDescriptor)this.Data.SelectedItem;

			object data = dataDescriptor.BuildASampleArray(itemCount);

			long StreamSize = 0;

			bool inRam = (StreamManagement)this.StreamManagementChoice.SelectedItem == StreamManagement.SerializeInRAM;
			string fileName = this.FileName.Text;

			{
				if (!inRam && !Directory.Exists(Path.GetDirectoryName(fileName)))
				{
					MessageBox.Show("The serialization file path does not exist !");
					return;
				}
				if (csvTableFileName != null && !Directory.Exists(Path.GetDirectoryName(csvTableFileName)))
				{
					MessageBox.Show("The result table file path does not exist !");
					return;
				}
			}

			bool ShareTheDeSerializer = this.ShareTheDeSerializer.IsChecked.Value;


			ResourceCounter resourceCounter;

			if (SelectedSerializer != null)
			{	// Only one serializer will be tested:
				Exception error;
				StreamSize = RunUnitTest(SelectedSerializer, data, dataDescriptor, out resourceCounter, inRam, fileName, ShareTheDeSerializer, loopCount, itemCount, out error);
				if (error != null)
					MessageBox.Show("ERROR: " + error.Message);
			}
			else // All serializers will be tested:
			{
				CsvTable csvTable = new CsvTable(TableTitles);

				bool IsUniversalSerializerBinary = true;
				double usbTime = 0.0, usbSize = 0.0, usbRAM = 0.0;
				foreach (var serInstance in SerializerInstances)
				{
					Exception error;
					StreamSize = RunUnitTest(serInstance, data, dataDescriptor, out resourceCounter, inRam, fileName, ShareTheDeSerializer, loopCount, itemCount, out error);

					if (error == null)
						Log.Add(string.Format(
							". Serializer \"{0}\": Time={1:G3} ms; File length={2:G3} Mio; Average item size={3:G3} bytes; GC consumption={4:G3} Mio; Working set consumption={5:G3} Mio.",
								serInstance.Name,
								resourceCounter.ElapsedTimeInMs,
								(((double)StreamSize) / (1024.0 * 1024.0)),
								((double)StreamSize / ((double)itemCount * (ShareTheDeSerializer ? (double)loopCount : 1.0))),
								(((double)resourceCounter.GCConsumptionPeak) / (1024.0 * 1024.0)),
								(((double)resourceCounter.WorkingSet64ConsumptionPeak) / (1024.0 * 1024.0))
							));
					else
						Log.Add(string.Format(
							". Serializer \"{0}\": ERROR \"{1}\"",
								serInstance.Name,
								error.Message.TrimEnd(new char[] { '\n', '\r' }))
									+ (error.InnerException == null ? string.Empty : string.Format(" (inner exception:\"{0}\")", error.InnerException.Message))
									);


					if (IsUniversalSerializerBinary)
					{
						usbTime = resourceCounter.ElapsedTimeInMs;
						usbSize = StreamSize;
						usbRAM = resourceCounter.GCConsumptionPeak;
					}

					double TimePercent = IsUniversalSerializerBinary ? 100.0 : resourceCounter.ElapsedTimeInMs * 100.0 / usbTime;
					double SizePercent = IsUniversalSerializerBinary ? 100.0 : 100.0 * (double)StreamSize / usbSize;
					double GCconsumptionPercent = IsUniversalSerializerBinary ? 100.0 : 100.0 * (double)resourceCounter.GCConsumptionPeak / usbRAM;
					double ConsumedWorkingSetPercent = IsUniversalSerializerBinary ? 100.0 : 100.0 * (double)resourceCounter.WorkingSet64ConsumptionPeak / usbRAM;
					double TotalPercent = IsUniversalSerializerBinary ? 100.0 : (TimePercent + SizePercent + GCconsumptionPercent) / 3.0;

					csvTable.AddLine(new object[] {
							serInstance.Name,
							resourceCounter.ElapsedTimeInMs,
							StreamSize,
							((double)StreamSize / ((double)itemCount * (ShareTheDeSerializer ? (double)loopCount : 1.0))),
							resourceCounter.GCConsumptionPeak,
							resourceCounter.WorkingSet64ConsumptionPeak,
							TimePercent,
							SizePercent,
							GCconsumptionPercent,
							ConsumedWorkingSetPercent,
							TotalPercent,
							((double)itemCount * (double)loopCount * (double)dataDescriptor.IdealStructureSize)/resourceCounter.ElapsedTimeInMs,//"Bytes/ms"
							((double)itemCount * (ShareTheDeSerializer ? (double)loopCount : 1.0)* (double)dataDescriptor.IdealStructureSize)/(double)StreamSize,//"Data/file lengths"
							((double)itemCount * (double)loopCount * (double)dataDescriptor.IdealStructureSize)/(double)resourceCounter.GCConsumptionPeak,//"data bytes/GC memory"
							((double)itemCount * (double)loopCount * (double)dataDescriptor.IdealStructureSize)/(double)resourceCounter.WorkingSet64ConsumptionPeak//"data bytes/Working set"
					});
					DoEvents();
					IsUniversalSerializerBinary = false;
				}

				csvTable.ExportToFile(csvTableFileName);
			}

			this.TextBoxState.Text = "Computation completed";
			this.Cursor = cursor;
			DoEvents();
		}

		// ------------------------------------------------------

		//[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		public long RunUnitTest(Serializer serInstance, object data, DataDescriptor dataDescriptor, out ResourceCounter resourceCounter, bool inRam, string fileName, bool ShareTheDeSerializer, int loopCount, int itemCount, out Exception Error)
		{
			if (!inRam)
				File.Delete(fileName);

			#region Main test
			resourceCounter = new ResourceCounter();

			long StreamSize = 0;
			Exception error = null;
			{
				try
				{
					if (!ShareTheDeSerializer)
					{
						for (int iLoop = 0; iLoop < loopCount; iLoop++)
						{
							if (inRam)
								StreamSize = serInstance.SerializeThenDeserialize_Once_InRAM(data, dataDescriptor);
							else
								StreamSize = serInstance.SerializeThenDeserialize_Once_InFile(data, fileName, dataDescriptor);
						}
					}
					else
					{
						if (inRam)
							StreamSize = serInstance.SerializeThenDeserialize_Loop_InRAM(data, loopCount, dataDescriptor);
						else
							StreamSize = serInstance.SerializeThenDeserialize_Loop_InFile(data, loopCount, fileName, dataDescriptor);
					}
				}
				catch (Exception ex)
				{
					if (ex is OutOfMemoryException)
					{
						// a serializer can be very RAM-hungry, but this error should not stop the tests.
						GC.Collect();
						GC.WaitForPendingFinalizers();
					}
					StreamSize = 0;
					error = ex;
				}
				resourceCounter.StopAndGetResourceMesures();
			#endregion Main test

			}

			double time = resourceCounter.ElapsedTimeInMs;

			this.StreamSize.Text = StreamSize.ToString();
			this.ItemSize.Text = ((double)StreamSize / ((double)itemCount * (ShareTheDeSerializer ? (double)loopCount : 1.0))).ToString();
			this.Time.Text = time.ToString();
			this.RAM.Text = (((double)resourceCounter.GCConsumptionPeak) / (1024.0 * 1024.0)).ToString();

			Error = error;
			return StreamSize;
		}

		// ------------------------------------------------------

		public static void DoEvents()
		{
			Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
																						new Action(delegate { }));
		}

		// ------------------------------------------------------

		public static Serializer[] SerializerInstances = new Serializer[] {
			new UniversalSerializerSerializer(), // Must always be first. Do not move it in this list.
#if USE_AUTOSERIALIZER
			new AutoSerializerSerializer(),
#endif
			new protobuf_netSerializer(),
			new UniversalSerializerAsXmlSerializer(), 
			new UniversalSerializerAsJSONSerializer(), 
#if USE_UNIVERSALSERIALIZERV1
			new UniversalSerializerV1BasedOnFastBinaryJSONSerializer(), 
#endif
#if USE_FASTBINARYJSON
			new FastBinaryJSONSerializer(), 
#endif
#if USE_FASTJSON
			new FastJSONSerializer(),
#endif
			//new OtherSerializer(), // type initialization is not dynamic.
			//new SharpSerializerSerializer(), // Too slow for these tests (10 X slower than the slowest serializers here). And it does not manage fields.
			new BinaryFormatterSerializer(), 
			new DataContractSerializerSerializer(), 
			new JavaScriptSerializerSerializer(), 
			new SoapFormatterSerializer(), 
			new XmlSerializerSerializer()
		};

		// ------------------------------------------------------
		// ------------------------------------------------------
		// ------------------------------------------------------
		// ------------------------------------------------------
		// ------------------------------------------------------



	}
}
