
// Copyright Christophe Bertrand.

#if DEBUG
#define DEBUG_WriteCounters
#endif

using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Xml;
using UniversalSerializerLib3.TypeManagement;

namespace UniversalSerializerLib3
{

	// Why the hell C# can not use strings in enums ????
	/// <summary>
	/// Follows the order of ElementTypes.
	/// </summary>
	struct XmlElementName
	{
		internal const string TypesChannelSection = "s0"; // When we insert an object at the root of this branch.
		internal const string InstancesChannelSection = "s1"; // When we insert an object at the root of this branch.
		internal const string PrimitiveValue = "p";
		internal const string Reference = "r";
		internal const string Null = "z";
		internal const string DefaultValue = "f";
		internal const string SubBranch = "b";
		internal const string Collection = "c";
		internal const string Dictionary = "d";
	};

	/// <summary>
	/// Follows the order of AttributeTypes.
	/// </summary>
	struct XmlAttributeName
	{
		internal const string InstanceIndex = "i";
		internal const string TypeNumber = "t";
		internal const string NumberOfElements = "l"; // for collections and dictionaries.
#if DEBUG
		internal const string Name = "n";
		internal const string TypeName = "typeName"; // Only for debugging.
#endif
	}

	internal sealed class XmlSerializationFormatter : SerializationFormatter
	{
		internal const string DataEndMarkTag = "end";

		internal const string DocumentPreamble = "<?xml version=\"1.0\"?>\n<data><version>3.0</version>\n";

		internal override bool IsStringFormatter { get { return true; } } // As XML or JSON. Otherwize, it is a binary formatter.

		/// <summary>
		/// Follows the order of ElementTypes .
		/// </summary>
		internal static readonly string[] XmlElementNames = new string[]
		{
			XmlElementName.TypesChannelSection,//  "s0", // When we insert an object at the root of this branch.
			XmlElementName.InstancesChannelSection, //  "s1", // When we insert an object at the root of this branch.
			XmlElementName.PrimitiveValue , // "e",
			XmlElementName.Reference, // "r",
			XmlElementName.Null, // "z",
			XmlElementName.DefaultValue, // "f",
			XmlElementName.SubBranch, // "b",
			XmlElementName.Collection, // "c",
			XmlElementName.Dictionary // "d",
		};

		/// <summary>
		/// Follows the order of AttributeName and of AttributeTypes.
		/// </summary>
		internal static readonly string[] XmlAttributeNames = new string[]
		{
			XmlAttributeName.InstanceIndex,// "i",
			XmlAttributeName.TypeNumber, // "t",
			XmlAttributeName.NumberOfElements, // "l", // for collections and dictionaries.
	#if DEBUG
			XmlAttributeName.Name, // "n",
			XmlAttributeName.TypeName // "typeName" // Only for debugging.
#endif
		};

		internal override bool CanManageMultiplexStreams
		{
			get
			{
				return true;
			}
		}

		internal XmlSerializationFormatter(Parameters parameters)
			: base(parameters)
		{
		}

		internal override void StartTree()
		{
			this.WriteStringToAssembledOrMultiplexStream(DocumentPreamble);
		}

		protected override void EndTree()
		{
			this.WriteStringToAssembledOrMultiplexStream("</data>\n");
		}

		internal override void InsertDataEndMark()
		{
			this.WriteStringToAssembledOrMultiplexStream("<" + XmlSerializationFormatter.DataEndMarkTag + "/>\n");
		}

		void WriteStringToAssembledOrMultiplexStream(string s)
		{
			this.streamWriter.Write(s);
		}

		// ######################################################################

		/// <summary>
		/// Returns true if the element has already been closed.
		/// </summary>
		/// <param name="element"></param>
		/// <param name="Value"></param>
		/// <param name="channelInfos"></param>
		/// <param name="IsStructure"></param>
		/// <param name="TheElementHaveAttributes"></param>
		/// <param name="TypeIndex"></param>
		/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal override bool ChannelEnterElementAndWriteValue(
			ref ChannelInfos channelInfos,
			Element element
, out bool TheElementHaveAttributes
			, TypeCode TypeIndex
			, object Value,
			bool IsStructure)
		{
			bool IsThereAnAttribute = false;

			this.ChannelWriteStringToStream(ref channelInfos, "<");
			this.ChannelWriteStringToStream(ref channelInfos, XmlSerializationFormatter.XmlElementNames[(int)element.ElementType]);

			IsThereAnAttribute |= _enterElementAttribute(ref channelInfos, AttributeTypes.InstanceIndex, element.InstanceIndex);
			if (element.typeIndexIsKnown)
				IsThereAnAttribute |= _enterElementAttribute(ref channelInfos, AttributeTypes.TypeNumber, element.typeIndex);
			{
				// special in xml: we do not write the string length except if it is 0 (to differenciate String.Empty and null at least).
				// The reason is xml can determine string length by itself (contrary to binary formatters).
				long? noe =
					element.NumberOfElements != null ?
					(element.ElementType == ElementTypes.PrimitiveValue ? (element.NumberOfElements.Value == 0 ? element.NumberOfElements : null)
					: element.NumberOfElements)
					: null;
				IsThereAnAttribute |= _enterElementAttribute(ref channelInfos, AttributeTypes.NumberOfElements, noe);
			}

			bool closeElement = element.IsAClosingTag;
			if (closeElement)
				this.ChannelWriteStringToStream(ref channelInfos, "/>");
			else
				this.ChannelWriteStringToStream(ref channelInfos, ">");
			if (element.ElementType != ElementTypes.PrimitiveValue)
				this.ChannelWriteStringToStream(ref channelInfos, "\n");

			if (Value != null)
				this.ChannelWriteValueAsString(ref channelInfos, (string)Value, IsThereAnAttribute);

			TheElementHaveAttributes = IsThereAnAttribute;
			return closeElement;
		}
#if false//DEBUG_WriteCounters
		int typeCounter = SerializationFormatter.TypeIndexationStart;
		int instanceCounter;
#endif

		bool _enterElementAttribute(ref ChannelInfos channelInfos, AttributeTypes AttributeType, object value)
		{
			if (value == null)
				return false;

			this.ChannelWriteStringToStream(ref channelInfos, " ");
			this.ChannelWriteStringToStream(ref channelInfos, XmlSerializationFormatter.XmlAttributeNames[(int)AttributeType]);
			this.ChannelWriteStringToStream(ref channelInfos, "=\"");
			this.ChannelWriteStringToStream(ref channelInfos, Tools.TranscodeToXmlCompatibleString(value.ToString()));
			this.ChannelWriteStringToStream(ref channelInfos, "\"");

			return true;
		}

		// --------------------------------------------

		internal override void ChannelExitElement(ref ChannelInfos channelInfos, ElementTypes elementType)
		{
			this.ChannelWriteStringToStream(
				ref channelInfos,
				"</"
			 + XmlSerializationFormatter.XmlElementNames[(int)elementType]
			 + ">\n");
		}

		// --------------------------------------------

		internal override string ChannelPrepareStringFromSimpleValue(ref ChannelInfos channelInfos, object value)
		{
			string StringValue = value as string;
			if (StringValue != null)
				return Tools.TranscodeToXmlCompatibleString(StringValue); // transcodes xml syntax characters.

			return this.TranscodeSimpleValueToString(value);
		}

		// --------------------------------------------
		// --------------------------------------------
		// --------------------------------------------

		internal override void ChannelWriteValueAsString(ref ChannelInfos channelInfos, string value, bool TheElementHaveAttributes)
		{
			this.ChannelWriteStringToStream(ref channelInfos, value); // In Xml, an element value is wrote alone.
		}

	}

	// ######################################################################
	//}

	// ######################################################################
	// ######################################################################

	internal sealed class XmlDeserializationFormatter : DeserializationFormatter
	{
		XmlReader xmlReader;

		internal XmlDeserializationFormatter(Parameters parameters)
			: base(parameters)
		{
		}

		internal override void PassPreamble()
		{
			{
				// we can not create the XmlReader in the formatter class constructor since this reader constructor starts to read the stream.
				// We need to create a new reader for each serialization.
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.IgnoreComments = true;
				settings.IgnoreProcessingInstructions = true;
				settings.IgnoreWhitespace = true;
				this.xmlReader = XmlReader.Create(this.stream, settings);
			}

			this.xmlReader.Read();
			this.xmlReader.Read();
			this.xmlReader.Read();
			this.xmlReader.Read();
			if (this.xmlReader.Value == "2.0")
				this.Version = StreamFormatVersion.Version2_0;
			else
				if (this.xmlReader.Value == "3.0")
					this.Version = StreamFormatVersion.Version3_0;
				else
					throw new Exception(ErrorMessages.GetText(7));// "Unknown file version."
			this.xmlReader.Read();
		}


		bool GetNextXmlPart()
		{
			bool ok = this.xmlReader.Read();
			return ok;
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
			int? SupposedTypeNumber = SupposedType != null ? SupposedType.TypeIndex : (int?)null;

			if (!this.GetNextXmlPart())
				if (NeedsThisElement)
					throw new Exception();
				else
					return false; // end of document.

			if (this.xmlReader.Name == XmlSerializationFormatter.DataEndMarkTag)
				return false; // end of document.

			ElementTypes? et2 = this.GetElementType();
			if (et2 == null)
			{
#if DEBUG
				if (this.xmlReader.NodeType != XmlNodeType.EndElement && this.xmlReader.Name != "data")
					Debugger.Break();
#endif
				return false;
			}
			ElementTypes et = et2.Value;

			if (WantClosingElement && this.xmlReader.NodeType == XmlNodeType.Element)
				if (NeedsThisElement)
					throw new Exception();
				else
					return false;

#if DEBUG
			if (!AcceptChannelSection
						&& !(et == ElementTypes.InstancesChannelSection || et == ElementTypes.TypesChannelSection))
				throw new Exception();


			// Checks name correspondance:
			string name = this.xmlReader.GetAttribute(XmlAttributeName.Name);
			if (Name != null && name != null && name != Name)
				throw new Exception();
#endif

			string typeNumber = this.xmlReader.GetAttribute(XmlAttributeName.TypeNumber);
#if DEBUG
			// Checks type correspondance:
			if (typeNumber != null && typeNumber != string.Empty && SupposedTypeNumber != null)
			{
				int foundTypeNumber = int.Parse(typeNumber);
				if (foundTypeNumber != SupposedTypeNumber.Value)
				{
					var found = this.typeManagerCollection.GetTypeManager(foundTypeNumber);
					var foundGtd = found.l2TypeManager.L1TypeManager;
					if (!foundGtd.type.Is(typeof(ITypeContainer)))
					{
						var supposed = this.typeManagerCollection.GetTypeManager(SupposedTypeNumber.Value);
						var supposedGtd = supposed.l2TypeManager.L1TypeManager;
						if (!foundGtd.type.Is(supposedGtd.type))
							if (!supposedGtd.IsNullable || supposedGtd.type.GetGenericArguments().Length == 0 || !foundGtd.type.Is(supposedGtd.type.GetGenericArguments()[0]))
								throw new Exception();
					}
				}
			}
#endif
			int? tn =
				typeNumber != null ? (
				int.Parse(typeNumber))
				: SupposedTypeNumber;

			string inum = this.xmlReader.GetAttribute(XmlAttributeName.InstanceIndex);
			int? InstanceNumber =
				inum != null ?
				int.Parse(inum)
				: (int?)null;

			string noe = this.xmlReader.GetAttribute(XmlAttributeName.NumberOfElements);
			long? numberOfElements =
				noe != null ?
				long.Parse(noe)
				: (long?)null;

			e.ContainsAValue = !this.xmlReader.IsEmptyElement;
			e.ElementType = et;
			e.InstanceIndex = InstanceNumber;
			e.IsAClosingTag = this.xmlReader.NodeType == XmlNodeType.EndElement;
#if DEBUG
			e.Name = name != null ? name : Name;
#endif
			e.NeedsAnEndElement = !this.xmlReader.IsEmptyElement;
			e.NumberOfElements = numberOfElements;
			if (tn != null)
			{
				e.typeIndex = tn.Value;
				e.typeIndexIsKnown = true;
			}

			return true;
		}



		/// <summary>
		/// Current this.xmlReader to ElementTypes?.
		/// </summary>
		ElementTypes? GetElementType()
		{
			if (this.xmlReader.NodeType != XmlNodeType.Element)
				return null;
			string en = this.xmlReader.Name;
			int i = (XmlSerializationFormatter.XmlElementNames as IList<string>).IndexOf(this.xmlReader.Name);
			if (i < 0)
				return null;
			ElementTypes et = (ElementTypes)i;
			return et;
		}

		internal override void PassClosingTag(ElementTypes ElementType)
		{
			this.xmlReader.Read();
#if DEBUG
			if (this.xmlReader.NodeType != XmlNodeType.EndElement
					|| this.xmlReader.Name != XmlSerializationFormatter.XmlElementNames[(int)ElementType])
				throw new Exception(
					string.Format("Unattended xml element {0} while waiting a closing tag {1}", xmlReader.Name, ElementType.ToString()));
#endif
		}

		internal override Object GetSimpleValueInElement(L3TypeManager typeManager)
		{
			if (!GetNextXmlPart())
				throw new Exception();

			if (this.xmlReader.NodeType == XmlNodeType.EndElement)
			{
#if DEBUG
				if (typeManager.l2TypeManager.L1TypeManager.type != typeof(string))
					throw new Exception();
#endif
				return DeserializationFormatter.NoValue;
			}

#if DEBUG
			if (this.xmlReader.NodeType != XmlNodeType.Text)
				throw new Exception();
#endif
			var rawValue = this.xmlReader.Value;
			return DeserializationFormatter.TranscodeStringToPrimitiveObject(rawValue, typeManager);
		}


		internal override void PassTreeEndTag()
		{
			this.xmlReader.Read();
#if DEBUG
			if (this.xmlReader.NodeType != XmlNodeType.EndElement
					|| this.xmlReader.Name != "data")
				throw new Exception(
					string.Format("Unattended xml element {0} while waiting a Data End Mark", xmlReader.Name));
#endif
		}

		internal override void PassDataEndMark()
		{
			this.xmlReader.Read();
#if DEBUG
			if (this.xmlReader.NodeType != XmlNodeType.Element
				|| !this.xmlReader.IsEmptyElement
					|| this.xmlReader.Name != XmlSerializationFormatter.DataEndMarkTag)
				throw new Exception(
					string.Format("Unattended xml element {0} while waiting a Data End Mark", xmlReader.Name));
#endif
		}

		internal override void SetStreamPosition(long NewPosition)
		{
			if (this.stream.Position != NewPosition)
				this.stream.Position = NewPosition;
		}

		public override void Dispose()
		{
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
