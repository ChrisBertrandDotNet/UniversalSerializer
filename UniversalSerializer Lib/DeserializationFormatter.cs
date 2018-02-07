
// Copyright Christophe Bertrand.

/* These abstract classes are mainly thought to work on streams, as a sequential process. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniversalSerializerLib3.TypeTools;
using UniversalSerializerLib3.TypeManagement;
using UniversalSerializerLib3.FileTools;

namespace UniversalSerializerLib3
{

	// ######################################################################
	// ######################################################################
	// ######################################################################



	/// <summary>
	/// A formatter works with streams.
	/// This interface is optimized for a serial behaviour. It is definitively not adapted to a direct-access behaviour.
	/// </summary>
	public abstract partial class DeserializationFormatter : IDisposable
	{
		internal StreamFormatVersion Version;

		internal readonly Parameters parameters;
//		internal readonly L3TypeManagerCollection typeManagerCollection;
		internal readonly Stream stream;

		/// <summary>
		/// Derived classes have to call this constructor.
		/// </summary>
		/// <param name="parameters">Contains many options.</param>
		protected DeserializationFormatter(Parameters parameters)
		{
			this.parameters = parameters;
			this.stream = this.parameters.Stream;
			//this.typeManagerCollection =				new L3TypeManagerCollection(this.parameters.customModifiers);
		}

		internal abstract void SetStreamPosition(long NewPosition);

		internal static bool IsAPrimitiveType(Type t)
		{
			return ((int)Type.GetTypeCode(t) >= (int)TypeCode.Boolean);
		}
		
		/// <summary>
		/// Must set "base.Version".
		/// </summary>
		internal abstract void PassPreamble();

		internal abstract void PassClosingTag(ElementTypes ElementType);

		internal abstract void PassTreeEndTag();

		internal abstract void PassDataEndMark();

		/// <summary>
		/// If the right element is not found, an exception occures.
		/// Optionnaly, We can accept a channel section.
		/// </summary>
		internal abstract bool GetNextElementAs(
			ref Element e,
			bool AcceptChannelSection, string Name, L3TypeManager SupposedType, bool NeedsThisElement, bool WantClosingElement);

		internal abstract Object GetSimpleValueInElement(L3TypeManager typeManager);


		internal class SubBranch
		{
			internal readonly string Name;
			internal readonly int? TypeNumber;
			internal SubBranch(
				string Name, int? TypeNumber)
			{
				this.Name = Name;
				this.TypeNumber = TypeNumber;
			}
		}

		// TODO: write a generic method for each Primitive type, in order to avoid boxing to object.
		internal static Object TranscodeStringToPrimitiveObject(string s, L3TypeManager typeManager)
		{
			TypeCode tc = (TypeCode)typeManager.TypeIndex;
#if DEBUG
			if ((int)tc < (int)TypeCode.Boolean)
				throw new ArgumentException(ErrorMessages.GetText(9));//"data is not a Primitive type");
#endif
			switch (tc)
			{
				case TypeCode.Boolean:
					return Boolean.Parse(s);
				case TypeCode.Byte:
					return Byte.Parse(s);
				case TypeCode.Char:
#if SILVERLIGHT || PORTABLE
                    return s[0];
#else
					return Char.Parse(s);
#endif
				case TypeCode.DateTime:
					return Tools.DateTimeFromTicksAndKind(ulong.Parse(s));
				case TypeCode.Decimal:
					return Decimal.Parse(s, Tools.EnUSCulture);
				case TypeCode.Double:
					return Double.Parse(s, Tools.EnUSCulture);
				case TypeCode.Int16:
					return Int16.Parse(s);
				case TypeCode.Int32:
					return Int32.Parse(s);
				case TypeCode.Int64:
					return Int64.Parse(s);
				case TypeCode.SByte:
					return SByte.Parse(s);
				case TypeCode.Single:
					return Single.Parse(s, Tools.EnUSCulture);
				case TypeCode.String:
					return s;
				case TypeCode.UInt16:
					return UInt16.Parse(s);
				case TypeCode.UInt32:
					return UInt32.Parse(s);
				case TypeCode.UInt64:
					return UInt64.Parse(s);
				default:
					throw new Exception();
			}
		}

		// TODO: write a generic method for each Primitive type, in order to avoid boxing to object.
		internal Object ReadPrimitiveObjectFromBinaryStream(BinaryReader2 binaryReader, L3TypeManager typeManager, bool CompressIntsAs7Bits)
		{
			TypeCode tc = (TypeCode)typeManager.TypeIndex;
#if DEBUG
			if ((int)tc < (int)TypeCode.Boolean)
				throw new ArgumentException(ErrorMessages.GetText(9));//"data is not a Primitive type");
#endif
			switch (tc)
			{
				case TypeCode.Boolean:
					return binaryReader.ReadBoolean();
				case TypeCode.Byte:
					return binaryReader.ReadByte();
				case TypeCode.Char:
					return binaryReader.ReadChar();
				case TypeCode.DateTime:
					return Tools.DateTimeFromTicksAndKind(binaryReader.ReadUInt64());
				case TypeCode.Decimal:
					return binaryReader.ReadDecimal();
				case TypeCode.Double:
					return binaryReader.ReadDouble();
				case TypeCode.SByte:
					return binaryReader.ReadSByte();
				case TypeCode.Single:
					return binaryReader.ReadSingle();
				case TypeCode.String:
					return binaryReader.ReadString();

				case TypeCode.Int16:
					if (CompressIntsAs7Bits)
						return binaryReader.ReadSpecial7BitEncodedShort();
					else
						return binaryReader.ReadInt16();
				case TypeCode.Int32:
					if (CompressIntsAs7Bits)
						return binaryReader.ReadSpecial7BitEncodedInt();
					else
						return binaryReader.ReadInt32();
				case TypeCode.Int64:
					if (CompressIntsAs7Bits)
						return binaryReader.ReadSpecial7BitEncodedLong();
					else
						return binaryReader.ReadInt64();
				case TypeCode.UInt16:
					if (CompressIntsAs7Bits)
						return binaryReader.Read7BitEncodedUShort();
					else
						return binaryReader.ReadUInt16();
				case TypeCode.UInt32:
					if (CompressIntsAs7Bits)
						return binaryReader.Read7BitEncodedUInt();
					else
						return binaryReader.ReadUInt32();
				case TypeCode.UInt64:
					if (CompressIntsAs7Bits)
						return binaryReader.Read7BitEncodedULong();
					else
						return binaryReader.ReadUInt64();

				default:
#if DEBUG
					throw new Exception();
#else
					return null;
#endif
			}
		}

		/// <summary>
		/// Dispose internal streams.
		/// </summary>
		abstract public void Dispose();
	}
	// ######################################################################

}