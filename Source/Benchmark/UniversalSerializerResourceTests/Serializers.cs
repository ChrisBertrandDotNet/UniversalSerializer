
// Copyright Christophe Bertrand.

//#define USE_SHARPSERIALIZER // Too slow.
//#define USE_FASTBINARYJSON // Causes problems with arrays.
//#define USE_FASTJSON // Causes problems with arrays.
//#define USE_UNIVERSALSERIALIZERV1

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using UniversalSerializerLib3;
using System.Linq;
#if USE_SHARPSERIALIZER
using Polenter.Serialization;
#endif

namespace UniversalSerializerResourceTests
{

	// #############################################################################
	// #############################################################################

	public interface Serializer
	{
		string Name { get; }

		long SerializeThenDeserialize_Once_InRAM(Object data, DataDescriptor dataDescriptor);

		long SerializeThenDeserialize_Once_InFile(Object data, string FileName, DataDescriptor dataDescriptor);

		/// <summary>
		/// One serializer and one stream instances for all loops.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="loopCount"></param>
		/// <param name="dataDescriptor"></param>
		/// <returns></returns>
		long SerializeThenDeserialize_Loop_InRAM(Object data, int loopCount, DataDescriptor dataDescriptor);

		/// <summary>
		/// One serializer and one stream instances for all loops.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="loopCount"></param>
		/// <param name="FileName"></param>
		/// <param name="dataDescriptor"></param>
		/// <returns></returns>
		long SerializeThenDeserialize_Loop_InFile(Object data, int loopCount, string FileName, DataDescriptor dataDescriptor);
	}

	// #############################################################################
	// #############################################################################

	public class UniversalSerializerSerializer : Serializer
	{
		public string Name
		{
			get { return "UniversalSerializer (binary)"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream });
				ser.Serialize(data);
				var deserializedData = ser.Deserialize();
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (var ser = new UniversalSerializer(FileName))
			{
				ser.Serialize(data);
				var deserializedData = ser.Deserialize();
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			return new FileInfo(FileName).Length;
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream });
				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ser.Serialize(data);
					stream.Position = position;
					var deserializedData = ser.Deserialize();
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream });
				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ser.Serialize(data);
					stream.Position = position;
					var deserializedData = ser.Deserialize();
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}

	// #############################################################################
	// #############################################################################

	public class UniversalSerializerAsXmlSerializer : Serializer
	{
		public string Name
		{
			get { return "UniversalSerializer (XML)"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream, SerializerFormatter = SerializerFormatters.XmlSerializationFormatter });
				ser.Serialize(data);
				var deserializedData = ser.Deserialize();
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream, SerializerFormatter = SerializerFormatters.XmlSerializationFormatter });
				ser.Serialize(data);
				var deserializedData = ser.Deserialize();
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream, SerializerFormatter = SerializerFormatters.XmlSerializationFormatter });
				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ser.Serialize(data);
					stream.Position = position;
					var deserializedData = ser.Deserialize();
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream, SerializerFormatter = SerializerFormatters.XmlSerializationFormatter });

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ser.Serialize(data);
					stream.Position = position;
					var deserializedData = ser.Deserialize();
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}

	// #############################################################################

	public class UniversalSerializerAsJSONSerializer : Serializer
	{
		public string Name
		{
			get { return "UniversalSerializer (JSON)"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream, SerializerFormatter = SerializerFormatters.JSONSerializationFormatter });
				ser.Serialize(data);
				var deserializedData = ser.Deserialize();
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream, SerializerFormatter = SerializerFormatters.JSONSerializationFormatter });
				ser.Serialize(data);
				var deserializedData = ser.Deserialize();
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream, SerializerFormatter = SerializerFormatters.JSONSerializationFormatter });

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ser.Serialize(data);
					stream.Position = position;
					var deserializedData = ser.Deserialize();
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var ser = new UniversalSerializer(new Parameters() { Stream = stream, SerializerFormatter = SerializerFormatters.JSONSerializationFormatter });

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ser.Serialize(data);
					stream.Position = position;
					var deserializedData = ser.Deserialize();
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}

	// #############################################################################

#if USE_UNIVERSALSERIALIZERV1
	public class UniversalSerializerV1BasedOnFastBinaryJSONSerializer : Serializer
	{
		public string Name
		{
			get { return "Old UniversalSerializer v1 based on FBJSON"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			byte[] bytes = UniversalSerializerLib.UniversalSerializer.Serialize(data);
			var deserializedData = UniversalSerializerLib.UniversalSerializer.Deserialize(bytes, data.GetType());
			dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			return bytes.LongLength;
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			// UniversalSerializer 1.0 cannot write directly to a Stream. Internally, it uses a MemoryStream.
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				byte[] bytes = UniversalSerializerLib.UniversalSerializer.Serialize(data);
				stream.Write(bytes, 0, bytes.Length);
			}
			long length = new FileInfo(FileName).Length;
			using (FileStream stream = new FileStream(FileName, FileMode.Open))
			{
				byte[] bytes = new byte[length];
				stream.Read(bytes, 0, checked((int)length));
				var deserializedData = UniversalSerializerLib.UniversalSerializer.Deserialize(bytes, data.GetType());
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			return length;
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			// UniversalSerializer 1.0 does not have a main Serializer instanciable class.
			long length = 0;

			for (int iLoop = 0; iLoop < loopCount; iLoop++)
			{
				length += this.SerializeThenDeserialize_Once_InRAM(data, dataDescriptor);
			}
			return length;
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			// UniversalSerializer 1.0 does not have a main Serializer instanciable class.
			FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite);
			BinaryWriter outfile = new BinaryWriter(stream, Encoding.UTF8);
			BinaryReader infile = new BinaryReader(stream, Encoding.UTF8);

			for (int iLoop = 0; iLoop < loopCount; iLoop++)
			{
				byte[] bytes = UniversalSerializerLib.UniversalSerializer.Serialize(data);
				long position = stream.Position;
				outfile.Write(bytes);
				outfile.Flush();

				stream.Position = position;
				byte[] bytes2 = infile.ReadBytes(bytes.Length);
				var deserializedData = UniversalSerializerLib.UniversalSerializer.Deserialize(bytes, data.GetType());

				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			long ret = stream.Position;
			infile.Dispose(); // it disposes stream too.
			if (stream.CanWrite)
			{
				outfile.Dispose();
				stream.Dispose();
			}
			return ret;
		}
	}
#endif

	// #############################################################################

#if USE_FASTBINARYJSON
	public class FastBinaryJSONSerializer : Serializer
	{
		public string Name
		{
			get { return "FastBinaryJSON"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			data = new Box<object>() { value = data }; // FastBinaryJSON 1.3.12 does not deserialize correctly if not boxed.
			byte[] bytes = fastBinaryJSON.BJSON.ToBJSON(data);
			var deserializedData = fastBinaryJSON.BJSON.ToObject(bytes, data.GetType());
			dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			return bytes.LongLength;	
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			data = new Box<object>() { value = data }; // FastBinaryJSON 1.3.12 does not deserialize correctly if not boxed.
			// FastBinaryJSON 1.3.12 cannot write directly to a Stream. Internally, it uses a MemoryStream.
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				byte[] bytes = fastBinaryJSON.BJSON.ToBJSON(data);
				stream.Write(bytes, 0, bytes.Length);
			}
			long length = new FileInfo(FileName).Length;
			using (FileStream stream = new FileStream(FileName, FileMode.Open))
			{
				byte[] bytes = new byte[length];
				stream.Read(bytes, 0, checked((int)length));
				var deserializedData = fastBinaryJSON.BJSON.ToObject(bytes, data.GetType());
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			return length;
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			// FastBinaryJSON 1.3.12 cannot write directly to a Stream. Internally, it uses a MemoryStream.
			long length = 0;

			for (int iLoop = 0; iLoop < loopCount; iLoop++)
			{
				length += this.SerializeThenDeserialize_Once_InRAM(data, dataDescriptor);
			}
			return length;
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			data = new Box<object>() { value = data }; // FastBinaryJSON 1.3.12 does not deserialize correctly if not boxed.

			using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite))
			{

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					// FastBinaryJSON 1.3.12 cannot write directly to a Stream. Internally, it uses a MemoryStream.
					byte[] bytes = fastBinaryJSON.BJSON.ToBJSON(data);

					long position = stream.Position;
					using (BinaryWriter outfile = new BinaryWriter(stream, Encoding.UTF8/*, true*/))
					{
						outfile.Write(bytes);
					}
					stream.Position = position;
					using (BinaryReader infile = new BinaryReader(stream, Encoding.UTF8/*, true*/))
					{
						bytes = infile.ReadBytes(bytes.Length);
					}
					var deserializedData = fastBinaryJSON.BJSON.ToObject(bytes, data.GetType());
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}
#endif

	// #############################################################################

#if USE_FASTJSON
	public class FastJSONSerializer : Serializer
	{
		public string Name
		{
			get { return "FastJSON"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			data = new Box<object>() { value = data }; // FastJSON 2.0.26 does not deserialize correctly if not boxed.
			string s = fastJSON.JSON.ToJSON(data);
			var deserializedData = fastJSON.JSON.ToObject(s, data.GetType());
			dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			return s.Length;
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			data = new Box<object>() { value = data }; // FastJSON 2.0.26 does not deserialize correctly if not boxed.
			// FastJSON 2.0.26 cannot write directly to a stream. Internally, it uses a StringBuilder.
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				string s = fastJSON.JSON.ToJSON(data);
				using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
				{
					writer.Write(s);
				}
			}
			using (FileStream stream = new FileStream(FileName, FileMode.Open))
			{
				using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
				{
					string s = reader.ReadToEnd();
					var deserializedData = fastJSON.JSON.ToObject(s, data.GetType());
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
			}
			return new FileInfo(FileName).Length;
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			//data = new Box<object>() { value = data }; // FastJSON 2.0.26 does not deserialize correctly if not boxed.
			// FastJSON 2.0.26 cannot write directly to a stream. Internally, it uses a StringBuilder.
			long length = 0;

			for (int iLoop = 0; iLoop < loopCount; iLoop++)
			{
				length += this.SerializeThenDeserialize_Once_InRAM(data, dataDescriptor);
			}
			return length;
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			data = new Box<object>() { value = data }; // FastJSON 2.0.26 does not deserialize correctly if not boxed.

			using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite))
			{

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					// FastJSON 2.0.26 cannot write directly to a stream. Internally, it uses a StringBuilder.
					string s = fastJSON.JSON.ToJSON(data);
					long position = stream.Position;
					using (StreamWriter outfile = new StreamWriter(stream, Encoding.UTF8, 1024/*, true*/))
					{
						outfile.Write(s);
					}
					stream.Position = position;
					using (StreamReader infile = new StreamReader(stream, Encoding.UTF8, false, 1024/*, true*/))
					{
						s = infile.ReadToEnd();
					}
					var deserializedData = fastJSON.JSON.ToObject(s, data.GetType());
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}
#endif

	// #############################################################################

	public class protobuf_netSerializer : Serializer
	{
		public string Name
		{
			get { return "Protobuf_net"; }
		}

		static readonly Func<Stream, int> f = (Func<Stream, int>)(ProtoBuf.Serializer.Deserialize<int>);
		static readonly MethodInfo gf = f.Method.GetGenericMethodDefinition();

		/// <summary>
		/// Creates equivalent to ProtoBuf.Serializer.Deserialize<Test>(stream).
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		static MethodInfo GetGenericDeserializeMethod(Type t)
		{
			MethodInfo ret;
			if (!_GetGenericDeserializeMethodCache.TryGetValue(t, out ret))
			{
				ret = _GetGenericDeserializeMethod(t);
				_GetGenericDeserializeMethodCache.Add(t, ret);
			}
			return ret;
		}
		static MethodInfo _GetGenericDeserializeMethod(Type t)
		{
			return gf.MakeGenericMethod(t);
		}
		static readonly Dictionary<Type, MethodInfo> _GetGenericDeserializeMethodCache = new Dictionary<Type, MethodInfo>();

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				ProtoBuf.Serializer.Serialize(stream, data);
				/*var tm=ProtoBuf.Meta.RuntimeTypeModel.Create();
				tm.Serialize(stream,data);*/

				stream.Position = 0;
				var mi = GetGenericDeserializeMethod(data.GetType());
				var deserializedData =
					mi.Invoke(null, new object[1] { stream }); // Equivalent to ProtoBuf.Serializer.Deserialize<Test>(stream)
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);

				//var deserialized = tm.Deserialize(stream, null, data.GetType());

				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				ProtoBuf.Serializer.Serialize(stream, data);
				var mi = GetGenericDeserializeMethod(data.GetType());

				stream.Position = 0;
				var deserializedData =
					mi.Invoke(null, new object[1] { stream }); // Equivalent to ProtoBuf.Serializer.Deserialize<Test>(stream)
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var mi = GetGenericDeserializeMethod(data.GetType());

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ProtoBuf.Serializer.Serialize(stream, data);
					stream.Position = position;
					var deserializedData =
						mi.Invoke(null, new object[1] { stream }); // Equivalent to ProtoBuf.Serializer.Deserialize<Test>(stream)
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var mi = GetGenericDeserializeMethod(data.GetType());

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ProtoBuf.Serializer.Serialize(stream, data);
					stream.Position = position;
					var deserializedData =
						mi.Invoke(null, new object[1] { stream }); // Equivalent to ProtoBuf.Serializer.Deserialize<Test>(stream)
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}

	// #############################################################################

	public class BinaryFormatterSerializer : Serializer
	{
		public string Name
		{
			get { return "BinaryFormatter (.NET)"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var bf = new BinaryFormatter();
				bf.Serialize(stream, data);
				stream.Position = 0;
				var deserializedData = bf.Deserialize(stream);
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var bf = new BinaryFormatter();
				bf.Serialize(stream, data);
				stream.Position = 0;
				var deserializedData = bf.Deserialize(stream);
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var bf = new BinaryFormatter();

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					bf.Serialize(stream, data);
					stream.Position = position;
					var deserializedData = bf.Deserialize(stream);
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var bf = new BinaryFormatter();

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					bf.Serialize(stream, data);
					stream.Position = position;
					var deserializedData = bf.Deserialize(stream);
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}

	// #############################################################################

	public class DataContractSerializerSerializer : Serializer
	{
		public string Name
		{
			get { return "DataContractSerializer (.NET)"; }
		}

		static DataContractSerializerSettings settings = new DataContractSerializerSettings() { PreserveObjectReferences = true };

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var bf = new DataContractSerializer(data.GetType(), DataContractSerializerSerializer.settings);
				bf.WriteObject(stream, data);
				stream.Position = 0;
				var deserializedData = bf.ReadObject(stream);
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var bf = new DataContractSerializer(data.GetType(), DataContractSerializerSerializer.settings);
				bf.WriteObject(stream, data);
				stream.Position = 0;
				var deserializedData = bf.ReadObject(stream);
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var bf = new DataContractSerializer(data.GetType(), DataContractSerializerSerializer.settings);

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					bf.WriteObject(stream, data);
					stream.Position = position;
					var deserializedData = bf.ReadObject(stream);
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var bf = new DataContractSerializer(data.GetType(), DataContractSerializerSerializer.settings);

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					bf.WriteObject(stream, data);
					stream.Position = position;
					var deserializedData = bf.ReadObject(stream);
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}

	// #############################################################################

	public class JavaScriptSerializerSerializer : Serializer
	{
		const int maxStringLength = 1000000000;
		const int recursionLimit = 1000;

		public string Name
		{
			get { return "JavaScriptSerializer (.NET)"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			var bf = new JavaScriptSerializer();
			bf.MaxJsonLength = maxStringLength;
			bf.RecursionLimit = recursionLimit;
			string s;
			s = bf.Serialize(data);
			var deserializedData = bf.Deserialize(s, data.GetType());
			dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			return s.Length;
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			// JavaScriptSerializer does not manage streams, only strings.
			string s;

			var bf = new JavaScriptSerializer();
			bf.MaxJsonLength = maxStringLength;
			bf.RecursionLimit = recursionLimit;
			s = bf.Serialize(data);

			using (StreamWriter outfile = new StreamWriter(FileName))
			{
				outfile.Write(s);
			}
			using (StreamReader infile = new StreamReader(FileName))
			{
				s = infile.ReadToEnd();
			}
			var deserializedData = bf.Deserialize(s, data.GetType());
			dataDescriptor.CheckPartialDeserializedData(data, deserializedData);

			return s.Length;
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			var bf = new JavaScriptSerializer();
			bf.MaxJsonLength = maxStringLength;
			bf.RecursionLimit = recursionLimit;
			long length = 0;

			for (int iLoop = 0; iLoop < loopCount; iLoop++)
			{
				string s = bf.Serialize(data);
				length += s.Length;
				var deserializedData = bf.Deserialize(s, data.GetType());
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			return length;
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			// JavaScriptSerializer does not manage streams, only strings.

			var bf = new JavaScriptSerializer();
			bf.MaxJsonLength = maxStringLength;
			bf.RecursionLimit = recursionLimit;

			using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite))
			{
				using (StreamWriter outfile = new StreamWriter(stream, Encoding.UTF8, 1024, true))
				{
					using (StreamReader infile = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
					{
						for (int iLoop = 0; iLoop < loopCount; iLoop++)
						{
							string s = bf.Serialize(data);
							long position = stream.Position;
							outfile.Write(s);
							outfile.Flush();

							stream.Position = position;
							string s2 = infile.ReadToEnd();
							var deserializedData = bf.Deserialize(s2, data.GetType());

							dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
						}
						return stream.Position;
					}
				}
			}
		}
	}

	// #############################################################################

	public class SoapFormatterSerializer : Serializer
	{
		public string Name
		{
			get { return "SoapFormatter (.NET)"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			byte[] bytes;
			using (MemoryStream stream = new MemoryStream())
			{
				var bf = new SoapFormatter();
				bf.Serialize(stream, data);
				bytes = stream.ToArray();
			}
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				var bf = new SoapFormatter();
				var deserializedData = bf.Deserialize(stream);
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			return bytes.LongLength;
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var bf = new SoapFormatter();
				bf.Serialize(stream, data);
			}
			using (FileStream stream = new FileStream(FileName, FileMode.Open))
			{
				var bf = new SoapFormatter();
				var deserializedData = bf.Deserialize(stream);
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			return new FileInfo(FileName).Length;
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var bf = new SoapFormatter();

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					bf.Serialize(stream, data);
					stream.Position = position;
					var deserializedData = bf.Deserialize(stream);
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var bf = new SoapFormatter();

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					bf.Serialize(stream, data);
					stream.Position = position;
					var deserializedData = bf.Deserialize(stream);
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}

	// #############################################################################

#if USE_SHARPSERIALIZER
	public class SharpSerializerSerializer : Serializer
	{
		public string Name
		{
			get { return "SharpSerializer"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			byte[] bytes;
			using (MemoryStream stream = new MemoryStream())
			{
				var serializer = new SharpSerializer(true);
				serializer.Serialize(data, stream);
				bytes = stream.ToArray();
			}
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				var serializer = new SharpSerializer(true);
				var bf = new SoapFormatter();
				var deserializedData = serializer.Deserialize(stream);
				dataDescriptor.CheckDeserializedData(data, deserializedData);
			}
			return bytes.LongLength;
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var serializer = new SharpSerializer(true);
				serializer.Serialize(data, stream);
			}
			using (FileStream stream = new FileStream(FileName, FileMode.Open))
			{
				var serializer = new SharpSerializer(true);
				var deserializedData = serializer.Deserialize(stream);
				dataDescriptor.CheckDeserializedData(data, deserializedData);
			}
			return new FileInfo(FileName).Length;
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var serializer = new SharpSerializer(true);

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					serializer.Serialize(data, stream);
					stream.Position = position;
					var deserializedData = serializer.Deserialize(stream);
					dataDescriptor.CheckDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var serializer = new SharpSerializer(true);

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					serializer.Serialize(data, stream);
					stream.Position = position;
					var deserializedData = serializer.Deserialize(stream);
					dataDescriptor.CheckDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}
#endif

	// #############################################################################

	public class XmlSerializerSerializer : Serializer
	{
		public string Name
		{
			get { return "XmlSerializer (.NET)"; }
		}

		public long SerializeThenDeserialize_Once_InRAM(object data, DataDescriptor dataDescriptor)
		{
			byte[] bytes;
			using (MemoryStream stream = new MemoryStream())
			{
				var bf = new XmlSerializer(data.GetType());
				bf.Serialize(stream, data);
				bytes = stream.ToArray();
			}
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				var bf = new XmlSerializer(data.GetType());
				var deserializedData = bf.Deserialize(stream);
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			return bytes.LongLength;
		}

		public long SerializeThenDeserialize_Once_InFile(object data, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var bf = new XmlSerializer(data.GetType());
				bf.Serialize(stream, data);
			}
			using (FileStream stream = new FileStream(FileName, FileMode.Open))
			{
				var bf = new XmlSerializer(data.GetType());
				var deserializedData = bf.Deserialize(stream);
				dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
			}
			return new FileInfo(FileName).Length;
		}

		public long SerializeThenDeserialize_Loop_InRAM(object data, int loopCount, DataDescriptor dataDescriptor)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var ser = new XmlSerializer(data.GetType());

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ser.Serialize(stream, data);
					stream.Position = position;
					var deserializedData = ser.Deserialize(stream);
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}

		public long SerializeThenDeserialize_Loop_InFile(object data, int loopCount, string FileName, DataDescriptor dataDescriptor)
		{
			using (FileStream stream = new FileStream(FileName, FileMode.Create))
			{
				var ser = new XmlSerializer(data.GetType());

				for (int iLoop = 0; iLoop < loopCount; iLoop++)
				{
					long position = stream.Position;
					ser.Serialize(stream, data);
					stream.Position = position;
					var deserializedData = ser.Deserialize(stream);
					dataDescriptor.CheckPartialDeserializedData(data, deserializedData);
				}
				return stream.Length;
			}
		}
	}

	// #############################################################################
	// #############################################################################
	// #############################################################################
	// #############################################################################
	// #############################################################################
	// #############################################################################

}