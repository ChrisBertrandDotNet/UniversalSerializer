
// Copyright Christophe Bertrand.

using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Text;
using UniversalSerializerLib3.TypeManagement;

namespace UniversalSerializerLib3
{

	/* Element byte:
	 bits 0-3: BinaryElementCode.
	 bits 4: BinaryAttributeCode.HasATypeNumber
	 */

	/// <summary>
	/// Follows the order of ElementTypes.
	/// </summary>
	enum BinaryElementCode : byte
	{
		TypesChannelSection = 0, //"s0", // When we insert an object at the root of this branch.
		InstancesChannelSection,// = "s1", // When we insert an object at the root of this branch.
		PrimitiveValue,// = "p",
		Reference,// = "r",
		Null,// = "z",
		DefaultValue,// = "f",
		SubBranch,// = "b",
		Collection,// = "c",
		Dictionary,// = "d"
		BitMask=15
	}

	/// <summary>
	/// Follows the order of AttributeTypes.
	/// </summary>
	[Flags]
	enum BinaryAttributeCode : byte
	{
		HasATypeNumber = 1 << 4,// "t"; // for PrimitiveValues, Collections & Dictionaries.
		HasANumberOfElements = 1 << 5// "l"; // for sub-branches over collections and dictionaries.
	}


	internal sealed class BinarySerializationFormatter : SerializationFormatter
	{
		internal const byte DataEndMarkCode = (byte)BinaryElementCode.Null | (byte)BinaryAttributeCode.HasATypeNumber;

		/// <summary>
		/// Always five 8-bits characters.
		/// </summary>
		internal const string DocumentPreambleVersion2 = "02.00";
		internal const string DocumentPreambleVersion3 = "03.00";

		internal override bool IsStringFormatter { get { return false; } } // It is a binary formatter.

		internal override bool CanManageMultiplexStreams
		{
			get
			{
				return true;
			}
		}

		internal BinarySerializationFormatter(Parameters parameters)
			: base(parameters)
		{
		}

		internal override void StartTree()
		{
			this.binaryWriter.Write(DocumentPreambleVersion3);
		}

		protected override void EndTree()
		{
		}

		internal override void InsertDataEndMark()
		{
			this.binaryWriter.Write(DataEndMarkCode);
		}

		// ######################################################################
		// ######################################################################

		/// <summary>
			/// Returns true if the element has already been closed.
			/// Write order:
			/// 1) Element code : byte
			/// 2) typeNumber : 7 bit-compressed int
			/// 3) Reference InstanceIndex : 7 bit-compressed int
			/// 4) NumberOfElements : 7 bit-compressed long
			/// </summary>
			/// <param name="channelInfos"></param>
			/// <param name="element"></param>
			/// <param name="IsStructure"></param>
			/// <param name="TheElementHaveAttributes"></param>
			/// <param name="TypeIndex"></param>
			/// <param name="Value"></param>
			/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal override bool ChannelEnterElementAndWriteValue(
				ref ChannelInfos channelInfos, Element element
, out bool TheElementHaveAttributes
				, TypeCode TypeIndex
								, object Value,
				bool IsStructure)
			{

				var et = element.ElementType;

				if (et == ElementTypes.PrimitiveValue && !element.typeIndexIsKnown && TypeIndex!=TypeCode.String) // No tag for the value primitive types (if not included in an object field or property).
				{
					SerializationFormatter.WriteValueToBinaryWriter(base.binaryWriter, Value, parameters.CompressIntsAs7Bits, TypeIndex);
					TheElementHaveAttributes = false; // useless for this formatter.
					return false;
				}

				if (et == ElementTypes.SubBranch && IsStructure && !element.typeIndexIsKnown && element.NumberOfElements == null) // No tag for structure types (if not included in an object field or property).
				{
					TheElementHaveAttributes = false; // useless for this formatter.
					return true;
				}



				FileTools.BinaryWriter2 bw = base.binaryWriter;

				// 1) Element code : byte
				{
					byte elementCode = unchecked((byte)et);
					if (element.typeIndexIsKnown)
						elementCode |= (byte)BinaryAttributeCode.HasATypeNumber;
					if (element.NumberOfElements != null)
						elementCode |= (byte)BinaryAttributeCode.HasANumberOfElements;
					bw.Write(elementCode);
				}				

				// 2) typeNumber : 7 bit-compressed int
				// Type is written immediately after the tag, if a type attribute is required exists.
				if (element.typeIndexIsKnown)
					bw.Write7BitEncodedInt(element.typeIndex);

				// 3) Reference InstanceIndex : 7 bit-compressed int
				if (et == ElementTypes.Reference)
					bw.Write7BitEncodedInt(element.InstanceIndex.Value);

				// 4) NumberOfElements : 7 bit-compressed long
				if (et == ElementTypes.SubBranch)
				{
					if (element.NumberOfElements != null)
						bw.Write7BitEncodedLong(element.NumberOfElements.Value);
				}
#if DEBUG
				else
					if (element.NumberOfElements != null)
						Debugger.Break();
#endif

				bool alreadyClosed = element.ElementType != ElementTypes.PrimitiveValue;

				if (!alreadyClosed)
				{
#if DEBUG
					if (Value == null)
						throw new Exception();
#endif
					SerializationFormatter.WriteValueToBinaryWriter(bw, Value, parameters.CompressIntsAs7Bits, TypeIndex);
				}

				TheElementHaveAttributes = false; // useless for this formatter.
				return alreadyClosed;
			}
#if DEBUG_WriteCounters
			int typeCounter = SerializationFormatter.TypeIndexationStart;
			int instanceCounter;
#endif

			// --------------------------------------------

			internal override void ChannelExitElement(ref ChannelInfos channelInfos, ElementTypes elementType)
			{
			}

			// --------------------------------------------

			internal override string ChannelPrepareStringFromSimpleValue(ref ChannelInfos channelInfos, object value)
			{
#if DEBUG
				throw new NotSupportedException();
#else
				return null;
#endif				
			}

			// --------------------------------------------
			// --------------------------------------------
			// --------------------------------------------

			internal override void ChannelWriteValueAsString(ref ChannelInfos channelInfos, string value, bool TheElementHaveAttributes)
			{
#if DEBUG
				throw new NotSupportedException();
#endif
			}

		//}

		// ######################################################################
	}

	// ######################################################################
	// ######################################################################

	internal sealed class BinaryDeserializationFormatter : DeserializationFormatter
	{
		readonly FileTools.BinaryReader2 binaryReader;
		//Parameters parameters;

		internal BinaryDeserializationFormatter(Parameters parameters)
			: base(parameters)
		{
			this.binaryReader = new UniversalSerializerLib3.FileTools.BinaryReader2(stream, Encoding.UTF8); // strings will be decoded as UTF-8.
		}

		internal override void PassPreamble()
		{
			string p = this.binaryReader.ReadString();
			if (p == BinarySerializationFormatter.DocumentPreambleVersion2)
				this.Version = StreamFormatVersion.Version2_0;
			else
				if (p == BinarySerializationFormatter.DocumentPreambleVersion3)
					this.Version = StreamFormatVersion.Version3_0;
				else
					throw new Exception(
						ErrorMessages.GetText(7));//"Unknown file version");
		}

		/// <summary>
		/// If the right element is not found, an exception occures.
		/// We can propose one or two element types.
		/// </summary>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal override bool GetNextElementAs(
			ref Element e,
			bool AcceptChannelSection, string Name, L3TypeManager SupposedType, bool NeedsThisElement, bool WantClosingElement)
		{
			bool SupposedTypeNumberIsKnown = SupposedType != null;

			// There is no tag for value primitive types:
			if (SupposedTypeNumberIsKnown
				&& SupposedType.TypeIndex < (int)TypeCode.String
				&& SupposedType.TypeIndex >= (int)TypeCode.Boolean)
			{
				e.ContainsAValue = true;
				e.ElementType = ElementTypes.PrimitiveValue;
				e.IsAClosingTag = true;
				e.typeIndex = SupposedType.TypeIndex;
				e.typeIndexIsKnown = true;
				return true;
			}

			// There is no tag for structures:
			if (SupposedTypeNumberIsKnown
				&& SupposedType.TypeIndex != (int)TypeCode.Object
				&& SupposedType.l2TypeManager.L1TypeManager.IsStructure
				&& (SupposedType.l2TypeManager.Container==null)
				&& !SupposedType.l2TypeManager.L1TypeManager.IsNullable)
			{
				e.ContainsAValue = false;
				e.ElementType = ElementTypes.SubBranch;
				e.IsAClosingTag = true;
				e.typeIndex = SupposedType.TypeIndex;
				e.typeIndexIsKnown = true;
				return true;
			}



			// 1) Element code : byte
			//int b = this.binaryReader.BaseStream.ReadByte(); // Unlike this.binaryReader.ReadByte(), this function does not cause an exception (exceptions can prevent method inlining).
			int b = this.binaryReader.ReadByteNoException(); // Unlike this.binaryReader.ReadByte(), this function does not cause an exception (exceptions can prevent method inlining).

			if (b < 0)
#if DEBUG
				throw new EndOfStreamException();
#else
				return false; // end of file.
#endif
			byte elementCode = unchecked((byte)b);
			if (elementCode==BinarySerializationFormatter.DataEndMarkCode)
				return false; // end of file.

			ElementTypes et = (ElementTypes)(elementCode & (byte)BinaryElementCode.BitMask);

#if DEBUG
			if (!AcceptChannelSection
						&& !(et == ElementTypes.InstancesChannelSection || et == ElementTypes.TypesChannelSection))
				throw new Exception();
#endif

			// 2) typeNumber : 7 bit-compressed int
			bool HasTypeNumber = (elementCode & (byte)BinaryAttributeCode.HasATypeNumber) != 0;

#if DEBUG
			if (!SupposedTypeNumberIsKnown && !HasTypeNumber
				&& (elementCode == (byte)BinaryElementCode.PrimitiveValue || elementCode == (byte)BinaryElementCode.SubBranch))
				throw new Exception();
#endif

			int typeNumber =
				HasTypeNumber ?
				this.binaryReader.Read7BitEncodedInt()
				: (SupposedTypeNumberIsKnown ? SupposedType.TypeIndex : 0);

			// 3) Reference InstanceIndex : 7 bit-compressed int
			int? InstanceNumber =
				et == ElementTypes.Reference ?
				this.binaryReader.Read7BitEncodedInt()
				: (int?)null;

			// 4) NumberOfElements : 7 bit-compressed long			
			bool HasANumberOfElements = (elementCode & (byte)BinaryAttributeCode.HasANumberOfElements) != 0;
			long? numberOfElements =
				HasANumberOfElements ?
				this.binaryReader.Read7BitEncodedLong()
				: (long?)null;
#if DEBUG
			if (HasANumberOfElements && et != ElementTypes.SubBranch)
				Debugger.Break();
#endif

			e.ElementType = et;
#if DEBUG
 e.Name= Name;
#endif
			e.typeIndexIsKnown = SupposedTypeNumberIsKnown | HasTypeNumber;
			e.typeIndex = typeNumber;
			e.InstanceIndex = InstanceNumber;
			e.IsAClosingTag = true;
			e.NumberOfElements = numberOfElements;
			e.ContainsAValue = et == ElementTypes.PrimitiveValue;
			return true;
		}

		// useless in this formatter.
		internal override void PassClosingTag(ElementTypes ElementType)
		{
		}

		internal override Object GetSimpleValueInElement(L3TypeManager typeManager)
		{
#if DEBUG
			if (typeManager==null)
				throw new Exception();
#endif

			return base.ReadPrimitiveObjectFromBinaryStream(this.binaryReader, typeManager, this.parameters.CompressIntsAs7Bits);
		}


		internal override void PassTreeEndTag()
		{ 
			// No tree end mark in this formatter.
		}

		internal override void PassDataEndMark()
		{
			int b = this.binaryReader.ReadByteNoException(); // Unlike this.binaryReader.ReadByte(), this function does not cause an exception (exceptions prevent method inlining).
#if DEBUG
			if (b < 0)
				throw new EndOfStreamException();
			if ((byte)b != BinarySerializationFormatter.DataEndMarkCode)
				throw new EndOfStreamException();
#endif
		}

		internal override void SetStreamPosition(long NewPosition)
		{
			this.binaryReader.SetPosition(NewPosition);
		}

		public override void Dispose()
		{
			if (this.binaryReader != null)
				this.binaryReader.Dispose();
		}
	}

	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################

}
