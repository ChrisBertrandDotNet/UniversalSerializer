
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UniversalSerializerLib3;

namespace UniversalSerializer_Tester_for_WinForm
{
	public static class WinFormTest
	{

		// ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
		const SerializerFormatters TestSerFormatter = SerializerFormatters.BinarySerializationFormatter;
		// ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑


	
		public static void Test(Form form)
		{
			bool AutomaticTests = true;
			bool showCloneForm = true;

			Dictionary<string, bool> Results = new Dictionary<string, bool>();
#if ! DEBUG
			MessageBox.Show("The application should be compiled in DEBUG mode");
#endif

			// - - - -

			{
				string title = "ToolStrip contains a FlowLayoutSetting that builds using an inherited private field.";
				var data = new ToolStrip();
				data.LayoutStyle = ToolStripLayoutStyle.Flow;

				var data2 = EasySerializeThenDeserializeWinForm(data);
				bool ok = (data2 != null) && data2.LayoutStyle == ToolStripLayoutStyle.Flow;
				Results.Add(title + " {" + data.GetType().Name + "}", ok);
				if (!AutomaticTests)
					Debugger.Break();
			}

			{
				string title = "ControlBindingsCollection";
				var data = new Form();
				data.DataBindings.Add("Width", data, "Height");

				var data2 = EasySerializeThenDeserializeWinForm(data.DataBindings);
				bool ok = (data2 != null) && data2.Count == 1;
				Results.Add(title + " {" + data.GetType().Name + "}", ok);
				if (!AutomaticTests)
					Debugger.Break();
			}

			{
				string title = "Main Form";
				var data = form;
				var t = data.Text;
				data.Text = "fen2";
				var data2 = EasySerializeThenDeserializeWinForm(data);
				bool ok = (data2 != null);
				Results.Add(title + " {" + data.GetType().Name + "}", ok);
				data.Text = t;
				data2.Visible = false;
				if (showCloneForm)
					data2.ShowDialog();
				if (!AutomaticTests)
					Debugger.Break();
			}

			{
				string title = "ControlBindingsCollection + simple Form";
				var data = new Form();
				data.DataBindings.Add("Width", data, "Height");
				var data2 = EasySerializeThenDeserializeWinForm(data);
				bool ok = (data2 != null);
				Results.Add(title + " {" + data.GetType().Name + "}", ok);
				if (!AutomaticTests)
					Debugger.Break();
			}

			{
				string title = "BindingContext";
				var data = new BindingContext();
				var data2 = EasySerializeThenDeserializeWinForm(data);
				bool ok = (data2 != null);
				Results.Add(title + " {" + data.GetType().Name + "}", ok);
				if (!AutomaticTests)
					Debugger.Break();
			}



			// - - - -
			{
				bool AllOK =
					Results.All(p => p.Value);
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
