
// Copyright Christophe Bertrand.

#if !JSON_DISABLED // Temporarily disabled

using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.Xml;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using UniversalSerializerLib3.TypeManagement;

namespace UniversalSerializerLib3
{

	// Why the hell C# can not use strings in enums ????
	/// <summary>
	/// Follows the order of ElementTypes.
	/// </summary>
	struct JSONElementName
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
	/// In JSON, it is essential that attributes names are different from any element name, as in JSON attributes will be serialized as elements.
	/// </summary>
	struct JSONAttributeName
	{
		internal const string InstanceIndex = "i";
		internal const string TypeNumber = "t";
		internal const string NumberOfElements = "l"; // for collections and dictionaries.
#if DEBUG
		internal const string Name = "n";
		internal const string TypeName = "typeName"; // Only for debugging.
#endif
	}

	internal sealed class JSONSerializationFormatter : SerializationFormatter
	{
		internal const string DataEndMarkTag = "end";

		internal const string DocumentPreamble = "{\n\"version\": \"3.0\",";

		internal override bool IsStringFormatter { get { return true; } } // As XML or JSON. Otherwize, it is a binary formatter.

		/// <summary>
		/// Follows the order of ElementTypes and JSONElementName.
		/// </summary>
		internal static readonly string[] JSONElementNames = new string[]
		{
			JSONElementName.TypesChannelSection,//  "s0", // When we insert an object at the root of this branch.
			JSONElementName.InstancesChannelSection, //  "s1", // When we insert an object at the root of this branch.
			JSONElementName.PrimitiveValue , // "p",
			JSONElementName.Reference, // "r",
			JSONElementName.Null, // "z",
			JSONElementName.DefaultValue, // "f",
			JSONElementName.SubBranch, // "b",
			JSONElementName.Collection, // "c",
			JSONElementName.Dictionary // "d",
		};

		/// <summary>
		/// Follows the order of JSONAttributeName and of AttributeTypes.
		/// </summary>
		internal static readonly string[] JSONAttributeNames = new string[]
		{
			JSONAttributeName.InstanceIndex,// "i",
			JSONAttributeName.TypeNumber, // "t",
			JSONAttributeName.NumberOfElements, // "l", // for collections and dictionaries.
#if DEBUG
			JSONAttributeName.Name, // "n",
			JSONAttributeName.TypeName // "typeName" // Only for debugging.
#endif
		};

		internal override bool CanManageMultiplexStreams
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Compulasary to place comas at the right places (specific to JSON).
		/// That works because this serializer is a total serial process. That would not work with a threaded serializer. It is not a hack, it is an optimisation.
		/// </summary>
		internal bool CurrentSubBranchIsEmptyYet;

		internal JSONSerializationFormatter(Parameters parameters)
			: base(parameters)
		{
		}

		internal override void StartTree()
		{
			this.CurrentSubBranchIsEmptyYet = true;
			this.WriteStringToAssembledOrMultiplexStream(DocumentPreamble);
		}

		protected override void EndTree()
		{
			this.WriteStringToAssembledOrMultiplexStream("\n}");
		}

		internal override void InsertDataEndMark()
		{
			this.WriteStringToAssembledOrMultiplexStream(
				",\n\""+ JSONSerializationFormatter.DataEndMarkTag+ "\":\"\"");
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
			internal override bool ChannelEnterElementAndWriteValue(
				ref ChannelInfos channelInfos, 
				Element element
, out bool TheElementHaveAttributes
				, TypeCode TypeIndex
				, object Value,
				bool IsStructure)
			{
				StringBuilder s = new StringBuilder();
				if (!this.CurrentSubBranchIsEmptyYet)
					s.Append(',');

				this.CurrentSubBranchIsEmptyYet = !element.IsAClosingTag;

				s.Append("\n\"");
				s.Append(JSONSerializationFormatter.JSONElementNames[(int)element.ElementType]);
				s.Append("\": {");

				bool IsThereAnAttribute = false;

				IsThereAnAttribute |= _enterElementAttribute(s, AttributeTypes.InstanceIndex, element.InstanceIndex, !IsThereAnAttribute);
				if (element.typeIndexIsKnown)
					IsThereAnAttribute |= _enterElementAttribute(s, AttributeTypes.TypeNumber, element.typeIndex, !IsThereAnAttribute);
				{
					// special in JSON: we do not write the string length except if it 0 (to differenciate String.Empty and null at least).
					// The reason is JSON can determine string length by itself (contrary to binary formatters).
					long? noe =
						element.NumberOfElements != null ?
						(element.ElementType == ElementTypes.PrimitiveValue ? (element.NumberOfElements.Value == 0 ? element.NumberOfElements : null)
						: element.NumberOfElements)
						: null;
					IsThereAnAttribute |= _enterElementAttribute(s, AttributeTypes.NumberOfElements, noe, !IsThereAnAttribute);
				}

				this.CurrentSubBranchIsEmptyYet &= !IsThereAnAttribute;

				bool closeElement = element.IsAClosingTag;
				if (closeElement)
					s.Append("}");

				this.ChannelWriteStringToStream(ref channelInfos, s.ToString());

					if (Value != null)
						this.ChannelWriteValueAsString(ref channelInfos, (string)Value, IsThereAnAttribute);


				TheElementHaveAttributes = IsThereAnAttribute;
				return closeElement;
			}
#if DEBUG_WriteCounters
			int typeCounter = SerializationFormatter.TypeIndexationStart;
			int instanceCounter;
#endif

			// -----------------------------------------------

			// -----------------------------------------------

			static bool _enterElementAttribute(StringBuilder s, AttributeTypes AttributeType, object value, bool firstAttributeOrSubBranch)
			{
				if (value == null)
					return false;

				if (!firstAttributeOrSubBranch)
					s.Append(",");
				s.Append("\n\"");
				s.Append(JSONSerializationFormatter.JSONAttributeNames[(int)AttributeType]);
				s.Append("\": \"");
				s.Append(Tools.TranscodeToJSONCompatibleString(value.ToString()));
				s.Append("\"");
				return true;
			}

			static bool _enterElementAttribute(StringBuilder s, string AttributeName, string value, bool firstAttributeOrSubBranch)
			{
				if (value == null)
					return false;

				if (!firstAttributeOrSubBranch)
					s.Append(",");
				s.Append("\n\"");
				s.Append(AttributeName);
				s.Append("\": \"");
				s.Append(value);
				s.Append("\"");
				return true;
			}

			// --------------------------------------------

			internal override void ChannelExitElement(ref ChannelInfos channelInfos, ElementTypes elementType)
			{
				this.ChannelWriteStringToStream(ref channelInfos, "\n}");
			}

			// --------------------------------------------

			internal override string ChannelPrepareStringFromSimpleValue(ref ChannelInfos channelInfos, object value)
			{
				string StringValue = value as string;
				if (StringValue != null)
					return Tools.TranscodeToJSONCompatibleString(StringValue); // transcodes JSON syntax characters.

				return this.TranscodeSimpleValueToString(value);
			}

			// --------------------------------------------
			// --------------------------------------------
			// --------------------------------------------

			internal override void ChannelWriteValueAsString(ref ChannelInfos channelInfos, string value, bool TheElementHaveAttributes)
			{
				StringBuilder sb = new StringBuilder();
				_enterElementAttribute(sb,
					string.Empty, // The Value attribute has no name.
					value, !TheElementHaveAttributes);
				this.ChannelWriteStringToStream(ref channelInfos, sb.ToString());

				this.CurrentSubBranchIsEmptyYet = false;
			}

		//}

		// ######################################################################
	}

	// ######################################################################
	// ######################################################################

	internal sealed class JSONDeserializationFormatter : DeserializationFormatter
	{
		XmlReader _JSONReader;

		// ------------------------------

		internal JSONDeserializationFormatter(Parameters parameters)
			: base(parameters)
		{
		}

		// ------------------------------

		internal override void PassPreamble()
		{
			// Passes the UTF8 prefix "ï»¿":
			if (!stream.CanSeek || ( stream.CanSeek && stream.Position == 0))
			{
				byte[] prefix = new byte[3];
				int n = this.stream.Read(prefix, 0, 3);
				if (n != 3 || prefix[0] != 'ï' || prefix[1] != '»' || prefix[2] != '¿')
					throw new FormatException("stream");
			}

			{
				// We have to build a new JSONReader on each deserialization because it just do not manage multiple json data in one stream correctly.

				XmlDictionaryReaderQuotas quotas = XmlDictionaryReaderQuotas.Max;
				this._JSONReader = JsonReaderWriterFactory.CreateJsonReader(stream,
#if !SILVERLIGHT
 Encoding.UTF8,
#endif
 quotas
#if !SILVERLIGHT
, null
#endif
);
			}

			this._JSONReader.Read();
			this._JSONReader.Read();
			this._JSONReader.Read();
			if (this._JSONReader.Value == "2.0")
				this.Version = StreamFormatVersion.Version2_0;
			else
				if (this._JSONReader.Value == "3.0")
					this.Version = StreamFormatVersion.Version3_0;
				else
					throw new Exception(ErrorMessages.GetText(7));// "Unknown file version."
			this._JSONReader.Read();
		}

		// ------------------------------

		bool GetNextJSONPart()
		{
			if (WeAlreadyObtainedJSONPart)
			{
				WeAlreadyObtainedJSONPart = false;
				return true;
			}
			try
			{
				bool ok = this._JSONReader.Read();
				return ok;
			}
			catch (Exception ex)
			{
				if (ex is XmlException && this._JSONReader.NodeType == XmlNodeType.EndElement && this._JSONReader.Name == "root")
				{
					// JSONReader complains about multiple docs in the same stream, but it is not a real problem, we only ignore this exception.
					return true;
				}
				throw;
			}
		}
		bool WeAlreadyObtainedJSONPart = false;

		// ------------------------------

		/// <summary>
		/// If the right element is not found, an exception occures.
		/// We can propose one or two element types.
		/// </summary>
		internal override bool GetNextElementAs(
			ref Element e,
			bool AcceptChannelSection, string SupposedName, L3TypeManager SupposedType, bool NeedsThisElement, bool WantClosingElement
)
		{
			int? SupposedTypeNumber = SupposedType != null ? SupposedType.TypeIndex : (int?)null;

			if (!this.GetNextJSONPart())
				if (NeedsThisElement)
					throw new Exception();
				else
					return false;

			if (this._JSONReader.Name == JSONSerializationFormatter.DataEndMarkTag)
			{
				WeAlreadyObtainedJSONPart = true;
				this.PassDataEndMark();
				return false;
			}

			ElementTypes? et2 = this.GetElementType();
			if (et2 == null)
			{
#if DEBUG
				if (this._JSONReader.NodeType != XmlNodeType.EndElement && this._JSONReader.Name != "data")
					Debugger.Break();
#endif
				return false;
			}
			ElementTypes et = et2.Value;

			if (WantClosingElement && this._JSONReader.NodeType == XmlNodeType.Element)
				if (NeedsThisElement)
					throw new Exception();
				else
					return false;

#if DEBUG
			if (!AcceptChannelSection
						&& !(et == ElementTypes.InstancesChannelSection || et == ElementTypes.TypesChannelSection))
				throw new Exception();
#endif



#region Analyse attributes
			// Contrary to xml, json does not have attributes. We have to take the right elements as attribute replacement, analysing its name.

			int? tn = null;
			int? InstanceNumber = null;
			long? numberOfElements = null;
#if DEBUG
			string name = null;
#endif

			while (this.GetNextJSONPart())
			{
				int iattribute = ((IList<string>)JSONSerializationFormatter.JSONAttributeNames).IndexOf(this._JSONReader.Name);
				if (iattribute < 0)
					break;

				{
					AttributeTypes at = (AttributeTypes)iattribute; // more clear.

					switch (at)
					{

						case AttributeTypes.InstanceIndex:
							{
								string inum = this.JSONGetValue();
								InstanceNumber =
									inum != null ?
									int.Parse(inum)
									: (int?)null;
							}
							break;


						case AttributeTypes.TypeNumber:
							{
								string typeNumber = this.JSONGetValue();
#if DEBUG
								// Checks type correspondance:
								if (typeNumber != null && typeNumber != string.Empty && SupposedTypeNumber != null)
								{
									int foundTypeNumber = int.Parse(typeNumber);
									if (foundTypeNumber != SupposedTypeNumber.Value)
									{
										var found = base.typeManagerCollection.GetTypeManager(foundTypeNumber);
										var foundGtd = found.l2TypeManager.L1TypeManager;
										if (!foundGtd.type.Is(typeof(ITypeContainer)))
										{
											var supposed = base.typeManagerCollection.GetTypeManager(SupposedTypeNumber.Value);
											var supposedGtd = supposed.l2TypeManager.L1TypeManager;
											if (!foundGtd.type.Is(supposedGtd.type))
												if (!supposedGtd.IsNullable || supposedGtd.type.GetGenericArguments().Length == 0 || !foundGtd.type.Is(supposedGtd.type.GetGenericArguments()[0]))
													throw new Exception();
										}
									}
								}
#endif
								tn =
									typeNumber != null ? (
									int.Parse(typeNumber))
									: SupposedTypeNumber;
							}
							break;

						case AttributeTypes.NumberOfElements:
							{
								string noe = this.JSONGetValue();
								numberOfElements =
									noe != null ?
									long.Parse(noe)
									: (long?)null;
							}
							break;

#if DEBUG
						case AttributeTypes.Name:
							{
								name = this.JSONGetValue();

								// Checks name correspondance:
								if (SupposedName != null && name != null && name != SupposedName)
									throw new Exception();
							}
							break;

						case AttributeTypes.TypeName:
							{
								string typeName = this.JSONGetValue();
							}
							break;
						default:
							throw new Exception();
#endif
					}
				}
			}
			WeAlreadyObtainedJSONPart = true; // the previous read element is kept for following process.
#endregion Analyse attributes

			this.GetNextJSONPart();
			this.WeAlreadyObtainedJSONPart=true;
			bool ContainsAValue = this._JSONReader.NodeType != XmlNodeType.EndElement;

			e.ContainsAValue = ContainsAValue;		
			e.ElementType = et;
			e.InstanceIndex = InstanceNumber;
			e.IsAClosingTag = this._JSONReader.NodeType == XmlNodeType.EndElement;			
#if DEBUG
			e.Name= name != null ? name : SupposedName;
#endif
			e.NeedsAnEndElement = !this._JSONReader.IsEmptyElement;
			e.NumberOfElements = numberOfElements;
			if (tn != null)
			{
				e.typeIndex = tn.Value;
				e.typeIndexIsKnown = true;
			}
			return true;
		}

		string JSONGetValue()
		{
			this.GetNextJSONPart();
#if DEBUG
			if (this._JSONReader.NodeType != XmlNodeType.Text)
				throw new Exception();
#endif
			string ret = this._JSONReader.Value;
			this.GetNextJSONPart(); // Passes EndElement.
			return ret;
		}

		// ------------------------------

		/// <summary>
		/// Current this.JSONReader to ElementTypes?.
		/// </summary>
		ElementTypes? GetElementType()
		{
			if (this._JSONReader.NodeType != XmlNodeType.Element)
				return null;
			string en = this._JSONReader.Name;
			int i = (JSONSerializationFormatter.JSONElementNames as IList<string>).IndexOf(this._JSONReader.Name);
			if (i < 0)
				return null;
			ElementTypes et = (ElementTypes)i;
			return et;
		}

		// ------------------------------

		internal override void PassClosingTag(ElementTypes ElementType)
		{
			this.GetNextJSONPart();
			if (this._JSONReader.Name == JSONSerializationFormatter.DataEndMarkTag)
			{
				this.WeAlreadyObtainedJSONPart = true;
				return;
			}
#if DEBUG
			if (this._JSONReader.NodeType != XmlNodeType.EndElement
					|| this._JSONReader.Name != JSONSerializationFormatter.JSONElementNames[(int)ElementType])
				throw new Exception(
					string.Format("Unattended JSON element {0} while waiting a closing tag {1}", _JSONReader.Name, ElementType.ToString()));
#endif
		}

		// ------------------------------

		internal override Object GetSimpleValueInElement(L3TypeManager typeManager)
		{
			if (!GetNextJSONPart())
				throw new Exception();
			if (this._JSONReader.NodeType == XmlNodeType.EndElement)
			{ // empty 'e' element.
				this.WeAlreadyObtainedJSONPart = true;
				return null;
			}
#if DEBUG
			if (this._JSONReader.NodeType != XmlNodeType.Element || this._JSONReader.Name != "a:item")
				throw new Exception();
#endif

			{
				if (!GetNextJSONPart())
					throw new Exception();
#if DEBUG
				if (this._JSONReader.NodeType != XmlNodeType.Text || this._JSONReader.Name != string.Empty)
					throw new Exception();
#endif
			}

			string rawValue;
			{
				// JsonReaderWriterFactory is very special: it splits string when it encounters a special character.
				// Therfore, we need to concatenate this parts.

				var Texts = new StringBuilder(this._JSONReader.Value);
				while (GetNextJSONPart())
				{
					if (this._JSONReader.NodeType != XmlNodeType.Text)
					{
						this.WeAlreadyObtainedJSONPart = true;
						break;
					}
					Texts.Append(this._JSONReader.Value);
				}
				rawValue = Texts.ToString();
			}

			{ // passes EndElement:
				if (!GetNextJSONPart())
					throw new Exception();
#if DEBUG
				if (this._JSONReader.NodeType != XmlNodeType.EndElement || this._JSONReader.Name != "a:item")
					throw new Exception();
#endif
			}

			return DeserializationFormatter.TranscodeStringToPrimitiveObject(rawValue, typeManager);
		}

		// ------------------------------
		// ------------------------------
		// ------------------------------

		internal override void PassTreeEndTag()
		{
			this.GetNextJSONPart();
#if DEBUG
			if (this._JSONReader.NodeType != XmlNodeType.EndElement
					|| this._JSONReader.Name != "root"
				|| this._JSONReader.Value != string.Empty)
				throw new Exception(
					string.Format("Unattended JSON element {0} while waiting a Tree End Tag", _JSONReader.Name));
#endif
		}

		internal override void PassDataEndMark()
		{
			this.GetNextJSONPart();
#if DEBUG
			if (this._JSONReader.NodeType != XmlNodeType.Element
					|| this._JSONReader.Name != JSONSerializationFormatter.DataEndMarkTag
				|| this._JSONReader.Value!=string.Empty)
				throw new Exception(
					string.Format("Unattended JSON element {0} while waiting a Data End Mark", _JSONReader.Name));
#endif	
			this.GetNextJSONPart();
#if DEBUG
			if (this._JSONReader.NodeType != XmlNodeType.Text
					|| this._JSONReader.Name != string.Empty
				|| this._JSONReader.Value != string.Empty)
				throw new Exception(
					string.Format("Unattended JSON element {0} while waiting a Data End Mark", _JSONReader.Name));
#endif
			this.GetNextJSONPart();
#if DEBUG
			if (this._JSONReader.NodeType != XmlNodeType.EndElement
					|| this._JSONReader.Name != JSONSerializationFormatter.DataEndMarkTag
				|| this._JSONReader.Value != string.Empty)
				throw new Exception(
					string.Format("Unattended JSON element {0} while waiting a Data End Mark", _JSONReader.Name));
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

#endif