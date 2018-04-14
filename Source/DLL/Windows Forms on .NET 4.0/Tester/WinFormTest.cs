
// Copyright Christophe Bertrand.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UniversalSerializerLib3;

namespace Tester
{
	public static class WinFormTest
	{
		// ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
		const SerializerFormatters TestSerFormatter = SerializerFormatters.BinarySerializationFormatter;
		// ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑

		public static BindingList<TestResult> TestResults = new BindingList<TestResult>(); // Windowsbase.dll

		#region TestResult
		public class TestResult : INotifyPropertyChanged
		{
			int _Order;
			public int Order
			{
				get { return this._Order; }
				set
				{
					this._Order = value;
					this.NotifyPropertyChanged("Order");
				}
			}

			string _Title;
			public string Title
			{
				get { return this._Title; }
				set
				{
					this._Title = value;
					this.NotifyPropertyChanged("Title");
				}
			}

			bool _Success;
			public bool Success
			{
				get { return this._Success; }
				set
				{
					this._Success = value;
					this.NotifyPropertyChanged("Success");
				}
			}

			string _Error;
			public string Error
			{
				get { return this._Error; }
				set
				{
					this._Error = value;
					this.NotifyPropertyChanged("Error");
				}
			}

			public override string ToString()
			{
				if (Success)
					return this.Title + " : success.";
				return this.Title + " : " + Error;
			}

			public event PropertyChangedEventHandler PropertyChanged;

			public void NotifyPropertyChanged(string MemberName)
			{
				if (this.PropertyChanged != null)
					this.PropertyChanged(this, new PropertyChangedEventArgs(MemberName));
			}
		}

		static int _testOrder;
		static void _AddTestResult(TestResult tr)
		{
			tr.Order = _testOrder++;
			TestResults.Add(tr);
		}

		static void AddSuccessTestResult(string title)
		{
			_AddTestResult(new TestResult() { Title = title, Success = true });
		}

		static void AddFailedTestResult(string title, string error = "FAILED")
		{
			_AddTestResult(new TestResult() { Title = title, Error = error, Success = false });
		}

		static void AddTestResult(string title, bool success)
		{
			_AddTestResult(new TestResult() { Title = title, Error = success ? null : "FAILED", Success = success });
		}
		#endregion TestResult


		public static void Test(Form form)
		{
			bool AutomaticTests = true;
			bool showCloneForm = !AutomaticTests;

#if ! DEBUG
			if (!AutomaticTests)
				MessageBox.Show("The application should be compiled in DEBUG mode");
#endif

			// - - - -

			TestResults.Clear();

			{
				string title = "ToolStrip contains a FlowLayoutSetting that builds using an inherited private field.";
				var data = new ToolStrip();
				data.LayoutStyle = ToolStripLayoutStyle.Flow;

				var data2 = EasySerializeThenDeserializeWinForm(data);
				bool ok = (data2 != null) && data2.LayoutStyle == ToolStripLayoutStyle.Flow;
				AddTestResult(title + " {" + data.GetType().Name + "}", ok);
				if (!AutomaticTests)
					Debugger.Break();
			}

			{
				string title = "ControlBindingsCollection";
				var data = new Form();
				data.DataBindings.Add("Width", data, "Height");

				var data2 = EasySerializeThenDeserializeWinForm(data.DataBindings);
				bool ok = (data2 != null) && data2.Count == 1;
				AddTestResult(title + " {" + data.GetType().Name + "}", ok);
				if (!AutomaticTests)
					Debugger.Break();
			}

			{
				string title = "Main Form";
				var data = new Form();
				var t = data.Text;
				data.Text = "fen2";
				data.Controls.Add(new Label() { Text = "Hello" });
				var data2 = EasySerializeThenDeserializeWinForm(data);
				bool ok = (data2 != null);
				AddTestResult(title + " {" + data.GetType().Name + "}", ok);
				data.Text = t;
				data2.Visible = false;
				if (showCloneForm)
					data2.ShowDialog();
				else
				{
					data2.WindowState = FormWindowState.Minimized;
					data2.Show();
				}
				data2.Close();
				if (!AutomaticTests)
					Debugger.Break();
			}

			{
				string title = "ControlBindingsCollection + simple Form";
				var data = new Form();
				data.DataBindings.Add("Width", data, "Height");
				var data2 = EasySerializeThenDeserializeWinForm(data);
				bool ok = (data2 != null);
				AddTestResult(title + " {" + data.GetType().Name + "}", ok);
				if (!AutomaticTests)
					Debugger.Break();
			}

			{
				string title = "BindingContext";
				var data = new BindingContext();
				var data2 = EasySerializeThenDeserializeWinForm(data);
				bool ok = (data2 != null);
				AddTestResult(title + " {" + data.GetType().Name + "}", ok);
				if (!AutomaticTests)
					Debugger.Break();
			}



			if (!AutomaticTests)
			{
				bool AllOK =
					TestResults.All(p => p.Success);
				Debugger.Break();
			}
		}

		static T EasySerializeThenDeserializeWinForm<T>(T source)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				Parameters parameters = new Parameters() { Stream = ms, SerializerFormatter = TestSerFormatter };
				UniversalSerializerWinForm ser = new UniversalSerializerWinForm(parameters);
				ser.Serialize(source);
				return ser.Deserialize<T>();
			}
		}
	}
}