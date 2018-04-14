
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
			using (var ms = new MemoryStream())
			using (var ser = new UniversalSerializer(ms, SerializerFormatters.XmlSerializationFormatter))
			{
				var data = Guid.NewGuid();
				ser.Serialize(data);
				var d = ser.Deserialize<Guid>();
				var ok = data == d;
			}
#endif
		}
	}


}