
// Copyright Christophe Bertrand.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalSerializerLib3;

namespace Tester
{
	class Program
	{
		static void Main(string[] args)
		{
			LocalTest();

			ConsoleTester.Test();
		}

		static void LocalTest()
		{
#if false
			{
				var data = new Hashtable(); data.Add(0, 1);
				using (var ser = new UniversalSerializer("TestXmlFormatter.uniser.json", SerializerFormatters.JSONSerializationFormatter))
				{
					ser.Serialize(data);
					var deserialized = ser.Deserialize<Hashtable>();
				}
			}

			{
				using (var ms = new MemoryStream())
				using (var ser = new UniversalSerializer(ms, SerializerFormatters.XmlSerializationFormatter))
				{
					var data = new System.Data.DataSet();
					ser.Serialize(data);
					var d = ser.Deserialize<System.Data.DataSet>();
					var ok = data == d;
				}
			}
#endif
		}

	}


}