
// Copyright Christophe Bertrand.

using ProtoBuf;
using System;
using System.Collections.Generic;

namespace UniversalSerializerResourceTests
{

	// ###############################################################
	// ###############################################################

	public struct AvailableDataDescriptors
	{
		public static DataDescriptor[] Descriptors = new DataDescriptor[] {
			new MyInt32ColorArray(),
			new MyByteColorArray(),
			new MyByteColorUsingPropertiesArray(),
			new MyInt32PixelArray(),
			new PrimitiveTypesStructureArray(),
			new PrimitiveValueTypesStructureArray(),
#if WPF
			new WPFWindow(),
#endif
			new MyObjectContainerArray(),
			new MyByteColorClassArray(),
			new ReferenceComparerArray(),
			new CircularTypeArray(),
			new CircularTypeWithGenericListArray(),
			new GenericDictionaryArray(),
			new InheritedGenericParameterArray(),
			new NoDefaultConstructorArray(),
			new ReadonlyFieldArray(),
			//new ReadonlyPropertyArray(),
			new NotAuthoredArray(),
			new GenericBoxArray(),
			new NeedsDefaultConstructionArray(),
			new NeedsParametricConstructionArray()
			//new NullableFieldArray() // useless
		};
	}

	// ###############################################################
	// ###############################################################

	public abstract class DataDescriptor
	{
		/// <summary>
		/// We presume a reference should be serialized as 32 bits.
		/// That is pure speculation and approximation.
		/// </summary>
		public const int SerializedReferenceSize = 4;

		public string Description;

		/// <summary>
		/// Size the structure should occupy in file.
		/// Not compressed.
		/// </summary>
		public abstract int IdealStructureSize { get; }

		public abstract Array BuildASampleArray(int NumerOfElements);

		public override string ToString()
		{
			return this.Description;
		}

		protected abstract bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex);

		public bool Check10Items(Array originalArray, Array deserializedArray)
		{
			if (originalArray.GetType() != deserializedArray.GetType())
				return false;

			bool ok = true;
			int l = originalArray.Length;
			if (l <= 10)
				for (int i = 0; i < l; i++)
					ok &= this.QuickCheckSampleFromArrays(originalArray, deserializedArray, i);
			else
			{
				double step = (double)l / 10.0;
				for (double i = 0.0; i < l; i += step)
				{
					ok &= this.QuickCheckSampleFromArrays(originalArray, deserializedArray, (int)i);
				}
			}
			return ok;
		}

		internal bool CheckNItems(Array originalArray, Array deserializedArray, int NumberOfTests)
		{
			if (originalArray.GetType() != deserializedArray.GetType())
				return false;

			bool ok = true;
			int l = originalArray.Length;
			if (l <= NumberOfTests)
				for (int i = 0; i < l; i++)
					ok &= this.QuickCheckSampleFromArrays(originalArray, deserializedArray, i);
			else
			{
				double step = (double)l / (double)NumberOfTests;
				for (double i = 0.0; i < l; i += step)
				{
					ok &= this.QuickCheckSampleFromArrays(originalArray, deserializedArray, (int)i);
				}
			}
			return ok;
		}

		internal bool CheckAllItems(Array originalArray, Array deserializedArray)
		{
			if (originalArray.GetType() != deserializedArray.GetType())
				return false;

			bool ok = true;
			for (int i = 0; i < originalArray.Length; i++)
				ok &= this.QuickCheckSampleFromArrays(originalArray, deserializedArray, i);
			return ok;
		}

		/// <summary>
		/// Checks 10 items per array.
		/// </summary>
		/// <param name="SourceData"></param>
		/// <param name="deserializedData"></param>
		public void CheckPartialDeserializedData(object SourceData, object deserializedData)
		{
			var sourceArray =
				SourceData is Box<object> ?
				(Array)(((Box<object>)SourceData).value) // mainly for FastBinaryJSON & FastJSON.
				: (Array)SourceData;
			var deserializedArray =
				deserializedData is Box<object> ?
				(Array)(((Box<object>)deserializedData).value) // mainly for FastBinaryJSON & FastJSON.
				: (Array)deserializedData;
			bool ok = this.Check10Items(sourceArray, deserializedArray);
			if (!ok)
				throw new Exception("Deserialized data is not conform.");
		}
	}

	// ###############################################################
	// ###############################################################

	/// <summary>
	/// Without this box, FastBinaryJSON 1.3.7 can not serialize MYColor[].
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable] // For BinaryFormatter and for SoapFormatter.
	[ProtoContract] // For protobuf-net.
	public struct Box<T>
	{
		[ProtoMember(1)] // For protobuf-net.
		public T value;
	}

	// ###############################################################
	// ###############################################################

	public class ReferenceComparerArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class MyByteColorClass
		{
			[ProtoMember(1)] // For protobuf-net.
			public byte R;
			[ProtoMember(2)] // For protobuf-net.
			public byte G;
			[ProtoMember(3)] // For protobuf-net.
			public byte B;
		}

		public ReferenceComparerArray()
		{
			this.Description = "Reference test. One instance of MyByteColorClass is referenced in all the array. MyByteColorClass is a class with 3 bytes inside.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new MyByteColorClass[NumerOfElements];

			MyByteColorClass instance = new MyByteColorClass() { R = 1, G = 2, B = 3 };

			for (int i = 0; i < NumerOfElements; i++)
				data[i] = instance;
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				MyByteColorClass FirstInstance = (MyByteColorClass)deserializedArray.GetValue(0);
				MyByteColorClass data = (MyByteColorClass)deserializedArray.GetValue(ArrayIndex);
				return object.ReferenceEquals(FirstInstance, data);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return SerializedReferenceSize; }
		}
	}

	// ###############################################################
	// ###############################################################

	public class MyByteColorClassArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class MyByteColorClass
		{
			[ProtoMember(1)] // For protobuf-net.
			public byte R;
			[ProtoMember(2)] // For protobuf-net.
			public byte G;
			[ProtoMember(3)] // For protobuf-net.
			public byte B;
		}

		public MyByteColorClassArray()
		{
			this.Description = "Instances test. MyByteColorClass is a class with 3 bytes inside.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new MyByteColorClass[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = new MyByteColorClass() { R = (byte)(i & 255), G = (byte)(i >> 8 & 255), B = (byte)(i >> 16 & 255) };
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				MyByteColorClass data = (MyByteColorClass)deserializedArray.GetValue(ArrayIndex);
				return data.R == (byte)(ArrayIndex & 255) && data.G == (byte)(ArrayIndex >> 8 & 255) && data.B == (byte)(ArrayIndex >> 16 & 255);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return 3*sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class MyByteColorArray : DataDescriptor
	{
		// Note: do not modify this structure: compatibility test (with version 2.0 files) needs it unchanged.
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public struct MyByteColor
		{
			[ProtoMember(1)] // For protobuf-net.
			public byte R;
			[ProtoMember(2)] // For protobuf-net.
			public byte G;
			[ProtoMember(3)] // For protobuf-net.
			public byte B;

			public static bool operator ==(MyByteColor mbc1, MyByteColor mbc2)
			{
				return mbc1.R==mbc2.R && mbc1.G==mbc2.G && mbc1.B==mbc2.B;
			}
			public static bool operator !=(MyByteColor mbc1, MyByteColor mbc2)
			{
				return !(mbc1==mbc2);
			}
			public override bool Equals(object obj)
			{
				return this==(MyByteColorArray.MyByteColor)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public MyByteColorArray()
		{
			this.Description = "Field bytes in a structure. MyByteColor is a structure with 3 bytes inside.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new MyByteColor[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = new MyByteColor() { R = (byte)(i & 255), G = (byte)(i >> 8 & 255), B = (byte)(i >> 16 & 255) };
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				MyByteColor data = (MyByteColor)deserializedArray.GetValue(ArrayIndex);
				return data.R == (byte)(ArrayIndex & 255) && data.G == (byte)(ArrayIndex >> 8 & 255) && data.B == (byte)(ArrayIndex >> 16 & 255);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return 3*sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class MyByteColorUsingPropertiesArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public struct MyByteColorUsingProperties
		{
			[ProtoMember(1)] // For protobuf-net.
			public byte R { get; set; }
			[ProtoMember(2)] // For protobuf-net.
			public byte G { get; set; }
			[ProtoMember(3)] // For protobuf-net.
			public byte B { get; set; }
		}

		public MyByteColorUsingPropertiesArray()
		{
			this.Description = "Property bytes in a structure. MyByteColor is a structure with 3 bytes inside, managed as 3 properties.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new MyByteColorUsingProperties[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = new MyByteColorUsingProperties() { R = (byte)(i & 255), G = (byte)(i >> 8 & 255), B = (byte)(i >> 16 & 255) };
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				MyByteColorUsingProperties data = (MyByteColorUsingProperties)deserializedArray.GetValue(ArrayIndex);
				return data.R == (byte)(ArrayIndex & 255) && data.G == (byte)(ArrayIndex >> 8 & 255) && data.B == (byte)(ArrayIndex >> 16 & 255);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return 3*sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class MyInt32ColorArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public struct MyInt32Color
		{
			[ProtoMember(1)] // For protobuf-net.
			public int R;
			[ProtoMember(2)] // For protobuf-net.
			public int G;
			[ProtoMember(3)] // For protobuf-net.
			public int B;
		}

		public MyInt32ColorArray()
		{
			this.Description = "Field Int32 in a structure. MyInt32Color is a structure with 3 int32 inside.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new MyInt32Color[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = new MyInt32Color() { R = (byte)(i & 255), G = (byte)(i >> 8 & 255), B = (byte)(i >> 16 & 255) };
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				MyInt32Color data = (MyInt32Color)deserializedArray.GetValue(ArrayIndex);
				return data.R == (byte)(ArrayIndex & 255) && data.G == (byte)(ArrayIndex >> 8 & 255) && data.B == (byte)(ArrayIndex >> 16 & 255);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return 3*sizeof(int); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class PrimitiveTypesStructureArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public struct PrimitiveTypesStructure
		{
			[ProtoMember(1)] // For protobuf-net.
			public bool Boolean;
			[ProtoMember(2)] // For protobuf-net.
			public DateTime Date;
			[ProtoMember(3)] // For protobuf-net.
			public sbyte Integer8;
			[ProtoMember(4)] // For protobuf-net.
			public byte UnsignedInteger8;
			[ProtoMember(5)] // For protobuf-net.
			public short Integer16;
			[ProtoMember(6)] // For protobuf-net.
			public ushort UnsignedInteger16;
			[ProtoMember(7)] // For protobuf-net.
			public int Integer32;
			[ProtoMember(8)] // For protobuf-net.
			public uint UnsignedInteger32;
			[ProtoMember(9)] // For protobuf-net.
			public long Integer64;
			[ProtoMember(10)] // For protobuf-net.
			public ulong UnsignedInteger64;
			[ProtoMember(11)] // For protobuf-net.
			public Single SingleFloat;
			[ProtoMember(12)] // For protobuf-net.
			public double DoubleFloat;
			[ProtoMember(13)] // For protobuf-net.
			public Decimal DecimalNumber;
			[ProtoMember(14)] // For protobuf-net.
			public char Character;
			[ProtoMember(15)] // For protobuf-net.
			public string CharacterString;

			public override bool Equals(object obj)
			{
				PrimitiveTypesStructure b = (PrimitiveTypesStructure)obj;
				return
					this.Boolean == b.Boolean
					&& this.Character == b.Character
					&& this.CharacterString == b.CharacterString
					&& this.Date == b.Date
					&& this.DecimalNumber == b.DecimalNumber
					&& this.DoubleFloat == b.DoubleFloat
					&& this.Integer16 == b.Integer16
					&& this.Integer32 == b.Integer32
					&& this.Integer64 == b.Integer64
					&& this.Integer8 == b.Integer8
					&& this.SingleFloat == b.SingleFloat
					&& this.UnsignedInteger16 == b.UnsignedInteger16
					&& this.UnsignedInteger32 == b.UnsignedInteger32
					&& this.UnsignedInteger64 == b.UnsignedInteger64
					&& this.UnsignedInteger8 == b.UnsignedInteger8;
			}

			public static bool operator ==(PrimitiveTypesStructure a, PrimitiveTypesStructure b)
			{
				return a.Equals(b);
			}
			public static bool operator !=(PrimitiveTypesStructure a, PrimitiveTypesStructure b)
			{
				return !(a == b);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public PrimitiveTypesStructureArray()
		{
			this.Description = "All primitive types in a structure.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new PrimitiveTypesStructure[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = new PrimitiveTypesStructure() { Boolean = i != 0, Character = i.ToString()[0], CharacterString = i.ToString(), Date = DateTime.Now, DecimalNumber = new decimal(i), DoubleFloat = i, Integer16 = unchecked((Int16)i), Integer32 = i, Integer64 = i << 8, Integer8 = unchecked((sbyte)i), SingleFloat = i, UnsignedInteger16 = unchecked((UInt16)i), UnsignedInteger32 = unchecked((UInt32)i), UnsignedInteger64 = (UInt64)i, UnsignedInteger8 = unchecked((byte)i) };
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				/*MyInt32Color data = (MyInt32Color)deserializedArray.GetValue(ArrayIndex);
				return data.R == (byte)(ArrayIndex & 255) && data.G == (byte)(ArrayIndex >> 8 & 255) && data.B == (byte)(ArrayIndex >> 16 & 255);
				 */
				var a = (originalArray as PrimitiveTypesStructure[])[ArrayIndex];
				var b = (deserializedArray as PrimitiveTypesStructure[])[ArrayIndex];
				return a.Equals(b);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(bool)+sizeof(ulong)+sizeof(sbyte)+sizeof(byte)+sizeof(short)+sizeof(ushort)+sizeof(int)+sizeof(uint)+sizeof(long)+sizeof(ulong)+sizeof(Single)+sizeof(double)+sizeof(Decimal)+sizeof(char)+5*sizeof(Char); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class PrimitiveValueTypesStructureArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public struct PrimitiveValueTypesStructure
		{
			[ProtoMember(1)] // For protobuf-net.
			public bool Boolean;
			[ProtoMember(2)] // For protobuf-net.
			public DateTime Date;
			[ProtoMember(3)] // For protobuf-net.
			public sbyte Integer8;
			[ProtoMember(4)] // For protobuf-net.
			public byte UnsignedInteger8;
			[ProtoMember(5)] // For protobuf-net.
			public short Integer16;
			[ProtoMember(6)] // For protobuf-net.
			public ushort UnsignedInteger16;
			[ProtoMember(7)] // For protobuf-net.
			public int Integer32;
			[ProtoMember(8)] // For protobuf-net.
			public uint UnsignedInteger32;
			[ProtoMember(9)] // For protobuf-net.
			public long Integer64;
			[ProtoMember(10)] // For protobuf-net.
			public ulong UnsignedInteger64;
			[ProtoMember(11)] // For protobuf-net.
			public Single SingleFloat;
			[ProtoMember(12)] // For protobuf-net.
			public double DoubleFloat;
			[ProtoMember(13)] // For protobuf-net.
			public Decimal DecimalNumber;
			[ProtoMember(14)] // For protobuf-net.
			public char Character;
			/*[ProtoMember(15)] // For protobuf-net.
			public string CharacterString;*/

			public override bool Equals(object obj)
			{
				PrimitiveValueTypesStructure b = (PrimitiveValueTypesStructure)obj;
				return
					this.Boolean == b.Boolean
					&& this.Character == b.Character
					//&& this.CharacterString == b.CharacterString
					&& this.Date == b.Date
					&& this.DecimalNumber == b.DecimalNumber
					&& this.DoubleFloat == b.DoubleFloat
					&& this.Integer16 == b.Integer16
					&& this.Integer32 == b.Integer32
					&& this.Integer64 == b.Integer64
					&& this.Integer8 == b.Integer8
					&& this.SingleFloat == b.SingleFloat
					&& this.UnsignedInteger16 == b.UnsignedInteger16
					&& this.UnsignedInteger32 == b.UnsignedInteger32
					&& this.UnsignedInteger64 == b.UnsignedInteger64
					&& this.UnsignedInteger8 == b.UnsignedInteger8;
			}

			public static bool operator ==(PrimitiveValueTypesStructure a, PrimitiveValueTypesStructure b)
			{
				return a.Equals(b);
			}
			public static bool operator !=(PrimitiveValueTypesStructure a, PrimitiveValueTypesStructure b)
			{
				return !(a == b);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public PrimitiveValueTypesStructureArray()
		{
			this.Description = "All primitive value (no reference) types in a structure.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new PrimitiveValueTypesStructure[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = new PrimitiveValueTypesStructure() { Boolean = i != 0, Character = i.ToString()[0], /*CharacterString = i.ToString(),*/ Date = DateTime.Now, DecimalNumber = new decimal(i), DoubleFloat = i, Integer16 = unchecked((Int16)i), Integer32 = i, Integer64 = i << 8, Integer8 = unchecked((sbyte)i), SingleFloat = i, UnsignedInteger16 = unchecked((UInt16)i), UnsignedInteger32 = unchecked((UInt32)i), UnsignedInteger64 = (UInt64)i, UnsignedInteger8 = unchecked((byte)i) };
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				/*MyInt32Color data = (MyInt32Color)deserializedArray.GetValue(ArrayIndex);
				return data.R == (byte)(ArrayIndex & 255) && data.G == (byte)(ArrayIndex >> 8 & 255) && data.B == (byte)(ArrayIndex >> 16 & 255);
				 */
				var a = (originalArray as PrimitiveValueTypesStructure[])[ArrayIndex];
				var b = (deserializedArray as PrimitiveValueTypesStructure[])[ArrayIndex];
				return a.Equals(b);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(bool) + sizeof(ulong) + sizeof(sbyte) + sizeof(byte) + sizeof(short) + sizeof(ushort) + sizeof(int) + sizeof(uint) + sizeof(long) + sizeof(ulong) + sizeof(Single) + sizeof(double) + sizeof(Decimal) + sizeof(char); }
		}
	}

	// ###############################################################
	// ###############################################################

#if WPF
	public class WPFWindow : DataDescriptor
	{
		public WPFWindow()
		{
			this.Description = "Complex Framework type. WPFWindow is a WPF Window instance.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new System.Windows.Window[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = new System.Windows.Window() { Title = "Dynamic Window", Top = 50 };
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				var data = (System.Windows.Window)deserializedArray.GetValue(ArrayIndex);
				bool ret = data.Title == "Dynamic Window" && data.Top == 50;
				{
					// releases these Windows from the WPF Application:
					var ww1 = System.Windows.Application.Current.Windows;
					data.Close();
					((System.Windows.Window)originalArray.GetValue(ArrayIndex)).Close();
					var ww2 = System.Windows.Application.Current.Windows;
				}
				return ret;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			// The ideal size is unknown and depends on framework changes.
			get { return 1000; }
		}
	}
#endif

	// ###############################################################
	// ###############################################################

	public class MyInt32PixelArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class MyInt32Color
		{
			[ProtoMember(1)] // For protobuf-net.
			public int R;
			[ProtoMember(2)] // For protobuf-net.
			public int G;
			[ProtoMember(3)] // For protobuf-net.
			public int B;
		}

		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class MyInt32Pixel : MyInt32Color
		{
			[ProtoMember(1)] // For protobuf-net.
			public int X;
			[ProtoMember(2)] // For protobuf-net.
			public int Y;
		}

		public MyInt32PixelArray()
		{
			this.Description = "Inheritance and fields. MyInt32Pixel is a class with 2 int32, that inherits MyInt32Color which contains 3 int32.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new MyInt32Color[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = new MyInt32Pixel() { R = (byte)(i & 255), G = (byte)(i >> 8 & 255), B = (byte)(i >> 16 & 255), X = i, Y = -i };
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				MyInt32Pixel data = (MyInt32Pixel)deserializedArray.GetValue(ArrayIndex);
				return data.R == (byte)(ArrayIndex & 255) && data.G == (byte)(ArrayIndex >> 8 & 255) && data.B == (byte)(ArrayIndex >> 16 & 255) && data.X == ArrayIndex && data.Y == -ArrayIndex;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return 3*sizeof(int); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class MyObjectContainerArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public struct MyObjectContainer
		{
			[ProtoMember(1)] // For protobuf-net.
			public object A;
			[ProtoMember(2)] // For protobuf-net.
			public object B;
			[ProtoMember(3)] // For protobuf-net.
			public int C;
			[ProtoMember(4)] // For protobuf-net.
			public object D;
			[ProtoMember(5)] // For protobuf-net.
			public object E;
			[ProtoMember(6)] // For protobuf-net.
			public object F;
			[ProtoMember(7)] // For protobuf-net.
			public object G;
			[ProtoMember(8)] // For protobuf-net.
			public object H;
			[ProtoMember(9)] // For protobuf-net.
			public object I;
			[ProtoMember(10)] // For protobuf-net.
			public object J;

			public static bool operator ==(MyObjectContainer a, MyObjectContainer b)
			{
				return
					a.A.Equals(b.A)
					&& a.B.Equals(b.B)
					&& a.C.Equals(b.C)
					&& a.D.Equals(b.D)
					&& a.E.Equals(b.E)
					&& a.F.Equals(b.F)
					&& a.G.Equals(b.G)
					&& a.H.Equals(b.H)
					&& a.I.Equals(b.I)
					&& a.J.Equals(b.J);
			}
			public static bool operator !=(MyObjectContainer a, MyObjectContainer b)
			{
				return !(a == b);
			}
			public override bool Equals(object obj)
			{
				return this == (MyObjectContainer)obj;
			}
			public override int GetHashCode()
			{
				return this.C;
			}
		}

		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class MyInt32Coordinate
		{
			[ProtoMember(1)] // For protobuf-net.
			public int X;
			[ProtoMember(2)] // For protobuf-net.
			public int Y;
			public static bool operator ==(MyInt32Coordinate a, MyInt32Coordinate b)
			{
				return
					a.X == b.X
					&& a.Y == b.Y;
			}
			public static bool operator !=(MyInt32Coordinate a, MyInt32Coordinate b)
			{
				return !(a == b);
			}

			public override bool Equals(object obj)
			{
				if (object.ReferenceEquals(this, obj))
					return true;
				if (!(obj is MyInt32Coordinate))
					return false;
				return this == (MyInt32Coordinate)obj;
			}

			public override int GetHashCode()
			{
				return this.X ^ this.Y;
			}
		}

		public MyObjectContainerArray()
		{
			this.Description = "Field Objects container. MyObjectContainerArray is a structure that contains some primitive types and 2 instances of structure of 2 int32. But all fields are objects.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new MyObjectContainer[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
				data[i] = CreateAnItem(i);
			return data;
		}

		static MyObjectContainer CreateAnItem(int index)
		{
			return new MyObjectContainer()
			{
				A = index.ToString(),
				B = (index << 4).ToString(),
				C = index,
				D = -index,
				E = (long)(index ^ 0x12345678),
				F = new MyInt32Coordinate() { X = index, Y = -index },
				G = (index & 1) == 0,
				H = (index & 1) == 1,
				I = new MyInt32Coordinate() { X = -index, Y = index },
				J = (ushort)index
			};
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				var data = (MyObjectContainer)deserializedArray.GetValue(ArrayIndex);
				var sameAsOriginal = CreateAnItem(ArrayIndex);
				return data == sameAsOriginal;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			// Very approximative since it depends on the total item count. Hyp: 1000000.
			get { return 3+5+sizeof(int)+sizeof(int)+sizeof(long)+2*sizeof(int)+1+1+2*sizeof(int)+sizeof(ushort); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class CircularTypeArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class CircularType
		{
			[ProtoMember(1)] // For protobuf-net.
			public int Id;
			[ProtoMember(2)] // For protobuf-net.
			public CircularType SubItem;
		}

		public CircularTypeArray()
		{
			this.Description = "Circular type and reference test. CircularType contains a reference to itself.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new CircularType[NumerOfElements];

			for (int i = 0; i < NumerOfElements; i++)
			{
				var instance = new CircularType() { Id = i };
				instance.SubItem = instance;
				data[i] = instance;
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				var deserialized = (CircularType)deserializedArray.GetValue(ArrayIndex);
				return deserialized.Id==ArrayIndex && object.ReferenceEquals(deserialized,deserialized.SubItem);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(int) + SerializedReferenceSize; }
		}
	}

	// ###############################################################
	// ###############################################################

	public class CircularTypeWithGenericListArray : DataDescriptor
	{
		const int ItemReferenceCount = 2;

		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class CircularTypeWithGenericList
		{
			[ProtoMember(1)] // For protobuf-net.
			public int Id;
			[ProtoMember(2)] // For protobuf-net.
			public readonly List<CircularTypeWithGenericList> SubItems = new List<CircularTypeWithGenericList>();
		}

		public CircularTypeWithGenericListArray()
		{
			this.Description = "Circular type and reference test in a generic list. CircularTypeWithGenericList contains a generic list of references to itself.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new CircularTypeWithGenericList[NumerOfElements];

			for (int i = 0; i < NumerOfElements; i++)
			{
				var instance = new CircularTypeWithGenericList() { Id = i };
				for (int a = 0; a < ItemReferenceCount; a++)
					instance.SubItems.Add(instance);
				data[i] = instance;
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				var deserialized = (CircularTypeWithGenericList)deserializedArray.GetValue(ArrayIndex);
				return deserialized.Id == ArrayIndex && object.ReferenceEquals(deserialized, deserialized.SubItems[1]);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(int) + ItemReferenceCount*SerializedReferenceSize; }
		}
	}

	// ###############################################################
	// ###############################################################

	public class GenericDictionaryArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class GenericDictionary : Dictionary<int, string>
		{
			[ProtoMember(1)] // For protobuf-net.
			public byte Field;

			public static bool operator ==(GenericDictionary mbc1, GenericDictionary mbc2)
			{
				bool ret = mbc1.Count == mbc2.Count;
				if (ret)
				{
					ret &= mbc1.Field == mbc2.Field;
					if (ret)
					{
						foreach (var keyValuePair in mbc1)
						{
							ret &= mbc2.ContainsKey(keyValuePair.Key);
							if (!ret)
								break;
							ret = mbc2[keyValuePair.Key] == keyValuePair.Value;
							if (!ret)
								break;
						}
					}
				}
				return ret;
			}
			public static bool operator !=(GenericDictionary mbc1, GenericDictionary mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (GenericDictionaryArray.GenericDictionary)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public GenericDictionaryArray()
		{
			this.Description = "Dictionary more a value. GenericDictionary inherits Dictionary<int, string> and contains a value.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new GenericDictionary[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new GenericDictionary() { { 1, "One" }, { 2, "Two" } };
				data[i].Field = 3;
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				GenericDictionary data = (GenericDictionary)deserializedArray.GetValue(ArrayIndex);
				return data[1] == "One" && data[2] == "Two" && data.Field == 3;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return 2 * (sizeof(int) + 4); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class InheritedGenericParameterArray : DataDescriptor
	{

		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public abstract class AbstractClass
		{
			[ProtoMember(1)] // For protobuf-net.
			public abstract int Value { get; set; }
		}

		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class InheritedClass : AbstractClass
		{
			[ProtoMember(1)] // For protobuf-net.
			public override int Value { get; set; }
		}

		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class InheritedGenericParameter<T>
			where T:AbstractClass
		{
			[ProtoMember(1)] // For protobuf-net.
			public T Field;

			public static bool operator ==(InheritedGenericParameter<T> mbc1, InheritedGenericParameter<T> mbc2)
			{
				return mbc1.Field.Value == mbc2.Field.Value;
			}
			public static bool operator !=(InheritedGenericParameter<T> mbc1, InheritedGenericParameter<T> mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (InheritedGenericParameterArray.InheritedGenericParameter<T>)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public InheritedGenericParameterArray()
		{
			this.Description = "Inherited generic parameter. InheritedGenericParameter<T> where T is an abstract.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new InheritedGenericParameter<InheritedClass>[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new InheritedGenericParameter<InheritedClass>() { Field = new InheritedClass() };
				data[i].Field.Value = 1;
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				InheritedGenericParameter<InheritedClass> data = (InheritedGenericParameter<InheritedClass>)deserializedArray.GetValue(ArrayIndex);
				return data.Field.Value == 1;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(int); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class NoDefaultConstructorArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class NoDefaultConstructor
		{
			[ProtoMember(1)] // For protobuf-net.
			public byte Value;

			public NoDefaultConstructor(byte value)
			{
				this.Value = value;
			}

			public static bool operator ==(NoDefaultConstructor mbc1, NoDefaultConstructor mbc2)
			{
				return mbc1.Value == mbc2.Value;
			}
			public static bool operator !=(NoDefaultConstructor mbc1, NoDefaultConstructor mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (NoDefaultConstructorArray.NoDefaultConstructor)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public NoDefaultConstructorArray()
		{
			this.Description = "No default constructor. NoDefaultConstructor is a class with a parametric constructor only.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new NoDefaultConstructor[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new NoDefaultConstructor(1);
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				NoDefaultConstructor data = (NoDefaultConstructor)deserializedArray.GetValue(ArrayIndex);
				return data.Value == 1;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class ReadonlyFieldArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class ReadonlyField
		{
			[ProtoMember(1)] // For protobuf-net.
			public readonly byte Value;

			public ReadonlyField()
			{
				this.Value = 10;
			}

			public ReadonlyField(byte value)
			{
				this.Value = value;
			}

			public static bool operator ==(ReadonlyField mbc1, ReadonlyField mbc2)
			{
				return mbc1.Value == mbc2.Value;
			}
			public static bool operator !=(ReadonlyField mbc1, ReadonlyField mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (ReadonlyFieldArray.ReadonlyField)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public ReadonlyFieldArray()
		{
			this.Description = "Readonly field. ReadonlyField is a class where a field is readonly and initialized by a parametric constructor.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new ReadonlyField[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new ReadonlyField(1);
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				ReadonlyField data = (ReadonlyField)deserializedArray.GetValue(ArrayIndex);
				return data.Value == 1;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

#if false // incorrect test, depends on serializer options
	public class ReadonlyPropertyArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public struct ReadonlyProperty
		{
			[ProtoMember(1)] // For protobuf-net.
			private byte _Value;

			public byte Value { get { return this._Value; } }

			public ReadonlyProperty(byte value)
			{
				this._Value = value;
			}

			public static bool operator ==(ReadonlyProperty mbc1, ReadonlyProperty mbc2)
			{
				return mbc1.Value == mbc2.Value;
			}
			public static bool operator !=(ReadonlyProperty mbc1, ReadonlyProperty mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (ReadonlyPropertyArray.ReadonlyProperty)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public ReadonlyPropertyArray()
		{
			this.Description = "Readonly property. ReadonlyProperty is a class where a property has no 'set' and is initialized by a parametric constructor.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new ReadonlyProperty[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new ReadonlyProperty(1);
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				ReadonlyProperty data = (ReadonlyProperty)deserializedArray.GetValue(ArrayIndex);
				return data.Value == 1;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(byte); }
		}
	}
#endif

	// ###############################################################
	// ###############################################################
	
	public class NotAuthoredArray : DataDescriptor
	{
		// No attributes since we are not supposed to be the author of this class.
		public class NotAuthored
		{
			public byte Value;

			public NotAuthored()
			{
			}

			public NotAuthored(byte value)
			{
				this.Value = value;
			}

			public static bool operator ==(NotAuthored mbc1, NotAuthored mbc2)
			{
				return mbc1.Value == mbc2.Value;
			}
			public static bool operator !=(NotAuthored mbc1, NotAuthored mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (NotAuthoredArray.NotAuthored)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public NotAuthoredArray()
		{
			this.Description = "Not authored. NotAuthored is from another author, we can not add attributes.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new NotAuthored[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new NotAuthored(1);
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				NotAuthored data = (NotAuthored)deserializedArray.GetValue(ArrayIndex);
				return data.Value == 1;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class GenericBoxArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class GenericBox<T>
		{
			[ProtoMember(1)] // For protobuf-net.
			public T Value;

			public GenericBox()
			{
			}

			public GenericBox(T value)
			{
				this.Value = value;
			}

			public static bool operator ==(GenericBox<T> mbc1, GenericBox<T> mbc2)
			{
				return mbc1.Value.Equals(mbc2.Value);
			}
			public static bool operator !=(GenericBox<T> mbc1, GenericBox<T> mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (GenericBoxArray.GenericBox<T>)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public GenericBoxArray()
		{
			this.Description = "Generics. GenericBox<T> contains a T as a value.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new GenericBox<int>[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new GenericBox<int>(1);
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				GenericBox<int> data = (GenericBox<int>)deserializedArray.GetValue(ArrayIndex);
				return data.Value == 1;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class NeedsParametricConstructionArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class NeedsParametricConstruction:IDisposable
		{
			[ProtoMember(1)] // For protobuf-net.
			public int Value;

			public NeedsParametricConstruction()
			{
				throw new NotSupportedException(); // Needs registration and a value.
			}

			public NeedsParametricConstruction(int value)
			{
				this.Value = value;
				StaticValidator.Register(this);
			}

			public static bool operator ==(NeedsParametricConstruction mbc1, NeedsParametricConstruction mbc2)
			{
				return object.ReferenceEquals(mbc1, mbc2);
			}
			public static bool operator !=(NeedsParametricConstruction mbc1, NeedsParametricConstruction mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (NeedsParametricConstructionArray.NeedsParametricConstruction)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}

			public void Dispose()
			{
				StaticValidator.Unregister(this);
			}
		}

		static class StaticValidator
		{
			static List<NeedsParametricConstruction> KnownInstances = new List<NeedsParametricConstruction>();

			public static void Register(NeedsParametricConstruction instance)
			{
				if (KnownInstances.Contains(instance))
					throw new ArgumentException("Already known instance.");
				KnownInstances.Add(instance);
			}

			public static void Unregister(NeedsParametricConstruction instance)
			{
				if (!KnownInstances.Contains(instance))
					throw new ArgumentException("Unknown instance.");
				KnownInstances.Remove(instance);
			}

			public static bool IsKnown(NeedsParametricConstruction instance)
			{
				return KnownInstances.Contains(instance);
			}
		}

		public NeedsParametricConstructionArray()
		{
			this.Description = "Parametric construction needed. NeedsParametricConstruction should not be constructed with no parameter.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new NeedsParametricConstruction[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new NeedsParametricConstruction(checked(counter++));
			}
			return data;
		}
		static int counter = 1;

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				NeedsParametricConstruction data = (NeedsParametricConstruction)deserializedArray.GetValue(ArrayIndex);
				return data.Value != 0 && StaticValidator.IsKnown(data);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class NeedsDefaultConstructionArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class NeedsDefaultConstruction : IDisposable
		{
			[ProtoMember(1)] // For protobuf-net.
			public int Value;

			public NeedsDefaultConstruction()
			{
				this.Value = 1;
				StaticValidator.Register(this);
			}

			public static bool operator ==(NeedsDefaultConstruction mbc1, NeedsDefaultConstruction mbc2)
			{
				return object.ReferenceEquals(mbc1, mbc2);
			}
			public static bool operator !=(NeedsDefaultConstruction mbc1, NeedsDefaultConstruction mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (NeedsDefaultConstructionArray.NeedsDefaultConstruction)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}

			public void Dispose()
			{
				StaticValidator.Unregister(this);
			}
		}

		static class StaticValidator
		{
			static List<NeedsDefaultConstruction> KnownInstances = new List<NeedsDefaultConstruction>();

			public static void Register(NeedsDefaultConstruction instance)
			{
				if (KnownInstances.Contains(instance))
					throw new ArgumentException("Already known instance.");
				KnownInstances.Add(instance);
			}

			public static void Unregister(NeedsDefaultConstruction instance)
			{
				if (!KnownInstances.Contains(instance))
					throw new ArgumentException("Unknown instance.");
				KnownInstances.Remove(instance);
			}

			public static bool IsKnown(NeedsDefaultConstruction instance)
			{
				return KnownInstances.Contains(instance);
			}
		}

		public NeedsDefaultConstructionArray()
		{
			this.Description = "Default construction needed. NeedsDefaultConstruction should not be instanciated without due construction.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new NeedsDefaultConstruction[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new NeedsDefaultConstruction();
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				NeedsDefaultConstruction data = (NeedsDefaultConstruction)deserializedArray.GetValue(ArrayIndex);
				return data.Value != 0 && StaticValidator.IsKnown(data);
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################

	public class NullableFieldArray : DataDescriptor
	{
		[Serializable] // For BinaryFormatter and for SoapFormatter.
		[ProtoContract] // For protobuf-net.
		public class NullableField
		{
			[ProtoMember(1)] // For protobuf-net.
			public byte? Value;

			public NullableField()
			{
				this.Value = 10;
			}

			public NullableField(byte value)
			{
				this.Value = value;
			}

			public static bool operator ==(NullableField mbc1, NullableField mbc2)
			{
				return mbc1.Value == mbc2.Value;
			}
			public static bool operator !=(NullableField mbc1, NullableField mbc2)
			{
				return !(mbc1 == mbc2);
			}
			public override bool Equals(object obj)
			{
				return this == (NullableFieldArray.NullableField)obj;
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public NullableFieldArray()
		{
			this.Description = "Nullable field. NullableField is a class where a field is a byte?.";
		}

		public override Array BuildASampleArray(int NumerOfElements)
		{
			var data = new NullableField[NumerOfElements];
			for (int i = 0; i < NumerOfElements; i++)
			{
				data[i] = new NullableField(1);
			}
			return data;
		}

		protected override bool QuickCheckSampleFromArrays(Array originalArray, Array deserializedArray, int ArrayIndex)
		{
			try
			{
				NullableField data = (NullableField)deserializedArray.GetValue(ArrayIndex);
				return data.Value == 1;
			}
			catch
			{
				return false;
			}
		}

		public override int IdealStructureSize
		{
			get { return sizeof(byte); }
		}
	}

	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################


}