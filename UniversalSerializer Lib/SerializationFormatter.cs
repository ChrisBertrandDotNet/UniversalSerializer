
// Copyright Christophe Bertrand.

/* These abstract classes are mainly thought to work on streams, as a sequential process. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniversalSerializerLib3.TypeTools;
using UniversalSerializerLib3.TypeManagement;
using UniversalSerializerLib3.StreamFormat3;
using UniversalSerializerLib3.FileTools;

namespace UniversalSerializerLib3
{

	// ######################################################################
	// ######################################################################
	// ######################################################################

	internal enum ElementTypes
	{
		TypesChannelSection = 0,		// "s0"
		InstancesChannelSection,		// "s1"
		PrimitiveValue,						// "p";
		Reference,									// "r";
		Null,											// "z";
		DefaultValue,							// "f";
		SubBranch,									// "b"
		Collection,								// "c"
		Dictionary,								// "d"
	};

	// ######################################################################

	/// <summary>
	/// Attributes of the Elements.
	/// </summary>
	internal enum AttributeTypes
	{
		InstanceIndex = 0,				// "i"
		TypeNumber,							// "t"
		NumberOfElements,				// "e" For collections and dictionaries.
#if DEBUG
		Name,										// "n" Only for debugging.
		TypeName									// "TypeName" Only for debugging.
#endif
	};

	// ######################################################################

	internal enum StreamFormatVersion { Version2_0, Version3_0 };

	// ######################################################################

	internal class AttributeUsage
	{
		internal readonly AttributeTypes AttributeType;
		internal readonly bool IsOptional;
		internal AttributeUsage(AttributeTypes AttributeType, bool IsOptional)
		{
			this.AttributeType = AttributeType;
			this.IsOptional = IsOptional;
		}
	}

	// ######################################################################

	internal static class FormattersInfos
	{

		/// <summary>
		/// Declares what Attribute can be used by what Element.
		/// 1st index: (int)ElementTypes .
		/// 2nd index: (int)AttributeTypes .
		/// AttributeUsageOfElements is an optimization that gives all AttributeUsages of all ElementTypes.
		/// </summary>
		internal static readonly AttributeUsage[,] AttributeUsageOfElements;

		internal static int nbElements = typeof(ElementTypes).GetEnumValuesCount();
		internal static int nbAttributes = typeof(AttributeTypes).GetEnumValuesCount();

		static FormattersInfos()
		{
			AttributeUsageOfElements = new AttributeUsage[nbElements, nbAttributes];

			// Build AttributeUsageOfElements from ElementInfosOfAllElements.
			// AttributeUsageOfElements is an optimization that gives all AttributeUsages of all ElementTypes.
			foreach (ElementTypes e in
#if !WINDOWS_PHONE && !PORTABLE
 Enum.GetValues(typeof(ElementTypes))
#else
 TypeTools.Types.GetEnumValues<ElementTypes>()
#endif
)
			{
				ElementInfos ei = ElementInfosOfAllElements.First((einfo) => einfo.ElementType == e);
				foreach (AttributeUsage ausages in ei.AttributeUsages)
					AttributeUsageOfElements[(int)e, (int)ausages.AttributeType] = ausages;
			}
		}

		/// <summary>
		/// For private declarations.
		/// </summary>
		struct ElementInfos
		{
			internal readonly ElementTypes ElementType;

			/// <summary>
			/// Collection of AttributeUsages for this Element.
			/// There is no order.
			/// </summary>
			internal readonly AttributeUsage[] AttributeUsages;

			internal ElementInfos(
				ElementTypes ElementType,
				AttributeUsage[] AttributeUsages)
			{
				this.AttributeUsages = AttributeUsages;
				this.ElementType = ElementType;
			}
		}

		/// <summary>
		/// Declares the attribute usage for each Element.
		/// Will be used to create FormattersInfos.AttributeUsageOfElements .
		/// </summary>
		static readonly ElementInfos[] ElementInfosOfAllElements =
			new ElementInfos[] {

			new ElementInfos(
				ElementTypes.TypesChannelSection,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true)
#endif
				}),
			
			new ElementInfos(
				ElementTypes.InstancesChannelSection,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true)
#endif
				}),
			
			new ElementInfos(
				ElementTypes.PrimitiveValue,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true),
#endif
					new AttributeUsage( AttributeTypes.TypeNumber, true),
					new AttributeUsage( AttributeTypes.InstanceIndex, false),
					new AttributeUsage( AttributeTypes.NumberOfElements, false),
				}),
			
			new ElementInfos(
				ElementTypes.Reference,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true),
#endif
					new AttributeUsage( AttributeTypes.TypeNumber, true),
					new AttributeUsage( AttributeTypes.InstanceIndex, true),
					new AttributeUsage( AttributeTypes.NumberOfElements, false),
				}),
			
			new ElementInfos(
				ElementTypes.Null,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true),
#endif
					new AttributeUsage( AttributeTypes.TypeNumber, true),
					new AttributeUsage( AttributeTypes.InstanceIndex, false),
					new AttributeUsage( AttributeTypes.NumberOfElements, false),
				}),
			
			new ElementInfos(
				ElementTypes.DefaultValue,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true),
#endif
					new AttributeUsage( AttributeTypes.TypeNumber, false),
					new AttributeUsage( AttributeTypes.InstanceIndex, false),
					new AttributeUsage( AttributeTypes.NumberOfElements, false),
				}),
			
			new ElementInfos(
				ElementTypes.SubBranch,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true),
#endif
					new AttributeUsage( AttributeTypes.TypeNumber, true),
					new AttributeUsage( AttributeTypes.NumberOfElements, true),
				}),
			
			// Collection type and number of items are told in its parent SubBranch.
			new ElementInfos(
				ElementTypes.Collection,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true)
#endif
				}),
			
			// Dictionary type and number of items (pairs) are told in its parent SubBranch.
			new ElementInfos(
				ElementTypes.Dictionary,
				new AttributeUsage[] {
#if DEBUG
					new AttributeUsage( AttributeTypes.Name, true),
					new AttributeUsage( AttributeTypes.TypeName, true)
#endif
				})			
			
			};
	}

	// ######################################################################
	// ######################################################################
	// ######################################################################

	/// <summary>
	/// A formatter works with streams.
	/// This class is optimized for a serial behaviour. It is definitively not adapted to a direct-access behaviour.
	/// </summary>
	internal abstract partial class SerializationFormatter:IDisposable
	{
		internal struct ChannelInfos:IDisposable // Ex-SerializingChannel.
		{
			internal readonly Stream stream;
#if DEBUG
			internal readonly string NameForDebugging;
#endif
			internal readonly ChannelNumber ChannelNumber;
			internal readonly StreamWriter streamWriterForText; // the one of the Formatter when multiplexed.
			internal readonly FileTools.BinaryWriter2 binaryWriter; // the one of the Formatter when multiplexed.

			internal ChannelInfos(
				SerializationFormatter Formatter,
				Stream stream,
#if DEBUG
 string NameForDebugging,
#endif
 ChannelNumber ChannelNumber
)
			{
				this.stream = stream;

#if DEBUG
				this.NameForDebugging = NameForDebugging;
#endif
				this.ChannelNumber = ChannelNumber;


				this.streamWriterForText =
					Formatter.IsStringFormatter ?
					(Formatter.parameters.TheStreamingMode == StreamingModes.SetOfStreams ?
					new StreamWriter(stream)
					: Formatter.streamWriter)
					: null;

				this.binaryWriter =
					!Formatter.IsStringFormatter ?
					(Formatter.parameters.TheStreamingMode == StreamingModes.SetOfStreams ?
					new FileTools.BinaryWriter2(stream, Encoding.UTF8)
					: Formatter.binaryWriter)
					: null;
			}

			public void Dispose()
			{
				if (this.streamWriterForText != null)
				{
					this.streamWriterForText.Dispose();
				}
				if (this.binaryWriter != null)
				{
					this.binaryWriter.Dispose();
				}
			}
		}

		internal ChannelInfos[] channelInfos = new ChannelInfos[2]; // 2 because of ChannelNumber.

		internal abstract bool CanManageMultiplexStreams { get; }

		internal abstract bool IsStringFormatter { get; } // As XML or JSON. Otherwize, it is a binary formatter.

		/// <summary>
		/// Channels in a multiplexed stream, or in its own stream.
		/// </summary>
		internal enum ChannelNumber { TypeDescriptorsChannel = 0, InstancesChannel = 1 };

		/// <summary>
		/// Types indexes from 0 to 18 are from System.TypeCode and are for Primitive types.
		/// Other types are indexed from 100.
		/// </summary>
		internal const int PredefinedTypeIndexationStart = 25; // Types number < 25 are reserved for Primitive types.
		internal const int TypeIndexationStartVersion2_0 = 35; // Types number < 35 are reserved for simple or predefined types.
		internal const int TypeIndexationStartVersion3_0 = 45; // Types number < 45 are reserved for simple or predefined types.
		#region Compulsory types for the type descriptors branch
		internal enum CompulsoryType : int
		{
			IntArrayIndex = PredefinedTypeIndexationStart,
			StringArrayIndex,
			SerializationTypeDescriptorIndex,
			SerializationTypeDescriptorCollectionIndex,
			AssemblyIdentifierIndex,
			AssemblyIdentifierArrayIndex,
			HeaderIndex
		};
		internal static Array CompulsoryTypeIndexes =
#if !PORTABLE && !SILVERLIGHT
 Enum.GetValues(typeof(SerializationFormatter.CompulsoryType));
#else
			TypeTools.Types.GetEnumValues<SerializationFormatter.CompulsoryType>();
#endif
		internal static Type[] CompulsoryTypes = new Type[] {
			typeof(Int32[]),
			typeof(String[]),
			typeof(SerializationTypeDescriptor),
			typeof(SerializationTypeDescriptorCollection),
			typeof(AssemblyIdentifier),
			typeof(AssemblyIdentifier[]),
			typeof(Header)
		};
		#endregion Compulsory types for the type descriptors branch

		internal readonly Parameters parameters;
		internal readonly L3TypeManagerCollection l3typeManagerCollection;

		/// <summary>
		/// Only for text formatters.
		/// </summary>
		internal readonly StreamWriter streamWriter;

		/// <summary>
		/// Only for binary formatters.
		/// </summary>
		internal readonly FileTools.BinaryWriter2 binaryWriter;

		internal SerializationFormatter(
			Parameters parameters)
		{
			this.l3typeManagerCollection =
				new L3TypeManagerCollection(parameters.customModifiers, StreamFormatVersion.Version3_0);

			#region For Serializer.cs
#if !DEBUG
			var f2 =
	(delAddAllStructs<int>)(addAllStructs<int>);
			this.gfAddAllStructs = f2.Method.GetGenericMethodDefinition();
#endif
			#endregion For Serializer.cs


			if (this.IsStringFormatter && parameters.Stream != null)
				this.streamWriter = new StreamWriter(parameters.Stream, Encoding.UTF8);
			else
				this.binaryWriter = new FileTools.BinaryWriter2(parameters.Stream, Encoding.UTF8);

			parameters.TheStreamingMode =
			 (parameters.StreamingMode != null) ?
			 parameters.StreamingMode.Value
			 :
				(parameters.setOfStreams != null ?
					StreamingModes.SetOfStreams :
					(this.CanManageMultiplexStreams ? StreamingModes.MultiplexStream : StreamingModes.AssembledStream));

			this.parameters = parameters;

			{
				bool UseMemoryStreams = this.parameters.TheStreamingMode == StreamingModes.AssembledStream;

				if (parameters.SerializeTypeDescriptors)
					this.channelInfos[(int)ChannelNumber.TypeDescriptorsChannel] = new ChannelInfos(
						this,
											parameters.setOfStreams != null ? parameters.setOfStreams.TypesStream :
											UseMemoryStreams ? new MemoryStream() : parameters.Stream
#if DEBUG
, "Types branch"
#endif
, ChannelNumber.TypeDescriptorsChannel
					);
				this.channelInfos[(int)ChannelNumber.InstancesChannel] = new ChannelInfos(
					this,
										parameters.setOfStreams != null ? parameters.setOfStreams.InstancesStream :
										UseMemoryStreams ? new MemoryStream() : parameters.Stream
#if DEBUG
, "Instances branch"
#endif
, ChannelNumber.InstancesChannel
				);
			}

		}

		/// <summary>
		/// Write the opening data (header, etc) to the stream.
		/// </summary>
		internal abstract void StartTree();

		/// <summary>
		/// Write the termination data to the stream.
		/// </summary>
		protected abstract void EndTree();

		/// <summary>
		/// Insert a mark that end the document.
		/// No data can be added after this mark.
		/// </summary>
		internal abstract void InsertDataEndMark();

		/// <summary>
		/// In the end, the independent branches are written to the stream.
		/// </summary>
		internal void FinalizeTreeToStream()
		{
			bool concatenateBranches = this.parameters.TheStreamingMode == StreamingModes.AssembledStream;
			if (this.channelInfos[(int)ChannelNumber.TypeDescriptorsChannel].stream != null)
			{
				if (concatenateBranches)
				{
					this.channelInfos[(int)ChannelNumber.TypeDescriptorsChannel].stream.Position = 0;
					this.channelInfos[(int)ChannelNumber.TypeDescriptorsChannel].stream.CopyTo(this.parameters.Stream);
				}
			}

			if (concatenateBranches)
			{
				this.channelInfos[(int)ChannelNumber.InstancesChannel].stream.Position = 0;
				this.channelInfos[(int)ChannelNumber.InstancesChannel].stream.CopyTo(this.parameters.Stream);
			}

			this.InsertDataEndMark();

			this.EndTree(); // Finalizes file.

			// Flushes streams:
			{
				if (this.binaryWriter != null)
					this.binaryWriter.Flush();

				if (this.IsStringFormatter)
					if (this.streamWriter != null)
						this.streamWriter.Flush();
					else
					{
						this.channelInfos[(int)ChannelNumber.TypeDescriptorsChannel].streamWriterForText.Flush();
						this.channelInfos[(int)ChannelNumber.InstancesChannel].streamWriterForText.Flush();
					}

				if (this.parameters.Stream != null)
					this.parameters.Stream.Flush();
				else
				{
					this.channelInfos[(int)ChannelNumber.TypeDescriptorsChannel].stream.Flush();
					this.channelInfos[(int)ChannelNumber.InstancesChannel].stream.Flush();
				}
			}
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
		internal abstract bool ChannelEnterElementAndWriteValue(
			ref ChannelInfos channelInfos,
			Element element
, out bool TheElementHaveAttributes
			, TypeCode TypeIndex
							, object Value,
			bool IsStructure
);
		internal abstract void ChannelExitElement(ref ChannelInfos channelInfos, ElementTypes elementType);


		#region Enter & Exit elements

		/// <summary>
		/// For multiplex streams: enter in a types channel section, in order to put an object at the root of this section.
		/// </summary>
		/// <param name="channelInfos"></param>
		internal void ChannelEnterChannelSection(ref ChannelInfos channelInfos)
		{
			switch (channelInfos.ChannelNumber)
			{
				case SerializationFormatter.ChannelNumber.InstancesChannel:
					this.ChannelEnterInstancesChannelSection(ref channelInfos);
					break;
				case SerializationFormatter.ChannelNumber.TypeDescriptorsChannel:
					this.ChannelEnterTypesChannelSection(ref channelInfos);
					break;
			}
		}

		/// <summary>
		/// For multiplex streams: enter in a types channel section, in order to put an object at the root of this section.
		/// </summary>
		/// <param name="channelInfos"></param>
		internal void ChannelEnterTypesChannelSection(ref ChannelInfos channelInfos)
		{
			bool TheElementHaveAttributes;
			this.ChannelEnterElementAndWriteValue(
				ref channelInfos,
				new Element
				{
					ElementType = ElementTypes.TypesChannelSection,
					NeedsAnEndElement = true
				}
, out TheElementHaveAttributes
, TypeCode.Empty
, null
, false
					);
		}

		/// <summary>
		/// For multiplex streams: enter in a instances channel section, in order to put an object at the root of this section.
		/// </summary>
		/// <param name="channelInfos"></param>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void ChannelEnterInstancesChannelSection(ref ChannelInfos channelInfos)
		{
			bool TheElementHaveAttributes;
			this.ChannelEnterElementAndWriteValue(
				ref channelInfos,
				new Element
				{
					ElementType = ElementTypes.InstancesChannelSection,
					NeedsAnEndElement = true
				}
, out TheElementHaveAttributes
, TypeCode.Empty
, null
, false
);
		}

		internal void ChannelExitTypesChannelSection()
		{
			this.ChannelExitElement(ref this.channelInfos[(int)ChannelNumber.TypeDescriptorsChannel], ElementTypes.TypesChannelSection);
		}

		internal void ChannelExitInstancesChannelSection()
		{
			this.ChannelExitElement(ref this.channelInfos[(int)ChannelNumber.InstancesChannel], ElementTypes.InstancesChannelSection);
		}

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void ChannelExitChannelSection(ref ChannelInfos channelInfos)
		{
			switch (channelInfos.ChannelNumber)
			{
				case SerializationFormatter.ChannelNumber.TypeDescriptorsChannel:
					this.ChannelExitElement(ref channelInfos, ElementTypes.TypesChannelSection);
					break;
				case SerializationFormatter.ChannelNumber.InstancesChannel:
					this.ChannelExitElement(ref channelInfos, ElementTypes.InstancesChannelSection);
					break;
			}
		}

		/* Pour les formats binaires : pas de nom ni de type, la branche vient dans l'ordre du descripteur de type.
		Pour les formats textuels : on donne le nom et le type de la propriété/champ, ou bien la branche vient dans l'ordre du descripteur de type.*/
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void ChannelEnterSubBranch(
			ref ChannelInfos channelInfos,
							long? NumberOfElements,
#if DEBUG
 string Name,
#endif
 int? TypeNumber,
			bool? IsDefaultValue
#if DEBUG
, Type type
#endif
, bool IsStructure
			)
		{
			bool TheElementHaveAttributes;
			this.ChannelEnterElementAndWriteValue(
				ref channelInfos,
				new Element
				{
					ElementType = ElementTypes.SubBranch,
					typeIndex = TypeNumber != null ? TypeNumber.Value : 0,
					typeIndexIsKnown = TypeNumber != null,
					NeedsAnEndElement = true,
					NumberOfElements = NumberOfElements
				}
, out TheElementHaveAttributes
, TypeCode.Empty
, null
, IsStructure
);
		}

		internal void ChannelExitSubBranch(ref ChannelInfos channelInfos)
		{
			this.ChannelExitElement(ref channelInfos, ElementTypes.SubBranch);
		}

		/* → le type T est un type simple (listé dans System.TypeCode à partir de Boolean, à trouver avec Type.GetTypeCode()), ou bien un byte[],
		 OU BIEN une référence à une instance de classe*/

		internal void ChannelEnterCollection(
			ref ChannelInfos channelInfos
#if DEBUG
, string Name,
Type type
#endif
)
		{
			bool TheElementHaveAttributes;
			this.ChannelEnterElementAndWriteValue(
				ref channelInfos,
				new Element
				{
					ElementType = ElementTypes.Collection,
					NeedsAnEndElement = true
				}
, out TheElementHaveAttributes
, TypeCode.Empty
, null
, false
);
		}

		/*internal abstract void AddCollectionPrimitiveElement<T>( // semble redondant avec AddPrimitiveElement, mais peut-être parfois utile.
			T Value, string Name = null, int? ElementType = null, int? ReferenceToInstanceNumber = null);*/
		internal void ChannelExitCollection(ref ChannelInfos channelInfos)
		{
			this.ChannelExitElement(ref channelInfos, ElementTypes.Collection);
		}

		internal void ChannelEnterDictionary(
			ref ChannelInfos channelInfos)
		{
			bool TheElementHaveAttributes;
			this.ChannelEnterElementAndWriteValue(
				ref channelInfos,
				new Element
				{
					ElementType = ElementTypes.Dictionary,
					NeedsAnEndElement = true
				}
, out TheElementHaveAttributes
, TypeCode.Empty
, null
, false
);
		}


		internal void ChannelExitDictionary(ref ChannelInfos channelInfos)
		{
			this.ChannelExitElement(ref channelInfos, ElementTypes.Dictionary);
		}

		#endregion Enter & Exit elements

		/// <summary>
		/// Text formatters can transcode a value to a string that respects the format syntax.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="channelInfos"></param>
		/// <returns></returns>
		internal abstract string ChannelPrepareStringFromSimpleValue(ref ChannelInfos channelInfos, object value);

		// -------------------------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void ChannelAddPrimitiveElementToRoot(
			ref ChannelInfos channelInfos,
			object Value,
#if DEBUG
 string debug_Name,
#endif
 int? TypeNumber,
			TypeCode typeCode
#if DEBUG
, Type debug_type
#endif
)
		{
			this.ChannelEnterInstancesChannelSection(ref channelInfos);

			this._ChannelAddPrimitiveElement(
				ref this.channelInfos[(int)ChannelNumber.InstancesChannel],
				Value,
#if DEBUG
 debug_Name,
#endif
 TypeNumber,
typeCode
#if DEBUG
, debug_type
#endif
);

			this.ChannelExitInstancesChannelSection();
		}

		// -------------------------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void _ChannelAddPrimitiveElement(
			ref ChannelInfos channelInfos,
			object Value,
#if DEBUG
 string debug_Name,
#endif
 int? TypeNumber,
			TypeCode typeCode
#if DEBUG
, Type debug_type
#endif
)
		{
#if DEBUG
			if (Value == null)
				throw new ArgumentNullException();
#endif

			if (this.IsStringFormatter)
				Value = this.ChannelPrepareStringFromSimpleValue(ref channelInfos, Value); // Depends on the formatter.


			bool TheElementHaveAttributes;

			if (!this.ChannelEnterElementAndWriteValue(
				ref channelInfos,
				new Element
				{
					ElementType = ElementTypes.PrimitiveValue,
					typeIndex = TypeNumber != null ? TypeNumber.Value : 0,
					typeIndexIsKnown = TypeNumber != null,
					NeedsAnEndElement = true,
					ContainsAValue = true
				}
, out TheElementHaveAttributes // TODO: determine IsDefaultValue using default values in the TypeManager.
, typeCode
				, Value
, false
				))
			{
				this.ChannelExitElement(ref channelInfos, ElementTypes.PrimitiveValue);
			}
		}

		// -------------------------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void _ChannelAddReference(
			ref ChannelInfos channelInfos,
#if DEBUG
 string debug_Name,
#endif
 int ReferenceToInstanceNumber
#if DEBUG
, Type debug_type
#endif
)
		{
			bool TheElementHaveAttributes;

			this.ChannelEnterElementAndWriteValue(
				ref channelInfos,
				new Element
				{
					ElementType = ElementTypes.Reference,
					InstanceIndex = ReferenceToInstanceNumber,
					IsAClosingTag = true,
				}
, out TheElementHaveAttributes
, TypeCode.Empty
, null
, false
);
		}

		// -------------------------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void ChannelAddNull(
			ref ChannelInfos channelInfos
)
		{
			bool TheElementHaveAttributes;

			this.ChannelEnterElementAndWriteValue(
				ref channelInfos,
				new Element
				{
					ElementType = ElementTypes.Null,
					IsAClosingTag = true
				}
, out TheElementHaveAttributes
, TypeCode.Empty
, null
, false
);
		}

		// -------------------------------------------------

		internal abstract void ChannelWriteValueAsString(ref ChannelInfos channelInfos, string value, bool TheElementHaveAttributes);

		// -------------------------------------------------

		/// <summary>
		/// Only for text formatters.
		/// </summary>
		/// <param name="channelInfos"></param>
		/// <param name="s"></param>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		protected void ChannelWriteStringToStream(ref ChannelInfos channelInfos, string s)
		{
			if (this.streamWriter != null)
				this.streamWriter.Write(s); // multiplex.
			else
				channelInfos.streamWriterForText.Write(s); // separate files.
		}

		// ----------------------------------------------------------------------

		// TODO: write a generic method for each Primitive type, in order to avoid boxing to object.
		/// <summary>
		/// Returns the byte length of the coded data.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		protected string TranscodeSimpleValueToString(object data)
		{
			var tc = Type.GetTypeCode(data.GetType());
#if DEBUG
			if ((int)tc < (int)TypeCode.Boolean)
				throw new ArgumentException("data is not a Primitive type");
#endif
			string s;
			switch (tc)
			{
				case TypeCode.Boolean:
					s = ((bool)data).ToString(
#if !PORTABLE
Tools.EnUSCulture
#endif
);
					break;
				case TypeCode.Byte:
					s = ((byte)data).ToString();
					break;
				case TypeCode.Char:
					s = ((char)data).ToString();
					break;
				case TypeCode.DateTime:
					s = (((DateTime)data).ToTicksAndKind()).ToString();
					break;
				case TypeCode.Decimal:
					s = ((Decimal)data).ToString(Tools.EnUSCulture);
					break;
				case TypeCode.Double:
					s = ((double)data).ToString(Tools.EnUSCulture);
					break;
				case TypeCode.Int16:
					s = ((Int16)data).ToString();
					break;
				case TypeCode.Int32:
					s = ((Int32)data).ToString();
					break;
				case TypeCode.Int64:
					s = ((Int64)data).ToString();
					break;
				case TypeCode.SByte:
					s = ((SByte)data).ToString();
					break;
				case TypeCode.Single:
					s = ((Single)data).ToString(Tools.EnUSCulture);
					break;
				case TypeCode.String:
					s = (string)data;
					break;
				case TypeCode.UInt16:
					s = ((UInt16)data).ToString();
					break;
				case TypeCode.UInt32:
					s = ((UInt32)data).ToString();
					break;
				case TypeCode.UInt64:
					s = ((UInt64)data).ToString();
					break;
				default:
					throw new Exception();
			}
			{
				return s;
			}
		}

		// ----------------------------------------------------------------------

		//[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		static protected void WriteValueToBinaryWriter(
			FileTools.BinaryWriter2 bw, object data, bool CompressIntsAs7Bits, TypeCode typeCode)
		{
#if DEBUG
			if ((int)typeCode < (int)TypeCode.Boolean)
				throw new ArgumentException("data is not a Primitive type");
#endif
			switch (typeCode)
			{
				case TypeCode.Boolean:
					bw.Write((bool)data);
					break;
				case TypeCode.Byte:
					bw.Write((byte)data);
					break;
				case TypeCode.Char:
					bw.Write((char)data);
					break;
				case TypeCode.DateTime:
					bw.Write(((DateTime)data).ToTicksAndKind());
					break;
				case TypeCode.Decimal:
#if (SILVERLIGHT || PORTABLE) && !WINDOWS_PHONE8
						bw.WriteDecimal((Decimal)data);
#else
					bw.Write((Decimal)data);
#endif
					break;
				case TypeCode.Double:
					bw.Write((double)data);
					break;
				case TypeCode.SByte:
					bw.Write((SByte)data);
					break;
				case TypeCode.Single:
					bw.Write((Single)data);
					break;
				case TypeCode.String:
					bw.Write((string)data);
					break;
				case TypeCode.Int16:
					if (CompressIntsAs7Bits)
						bw.WriteSpecial7BitEncodedShort((Int16)data);
					else
						bw.Write((Int16)data);
					break;
				case TypeCode.Int32:
					if (CompressIntsAs7Bits)
						bw.WriteSpecial7BitEncodedInt((Int32)data);
					else
						bw.Write((Int32)data);
					break;
				case TypeCode.Int64:
					if (CompressIntsAs7Bits)
						bw.WriteSpecial7BitEncodedLong((Int64)data);
					else
						bw.Write((Int64)data);
					break;
				case TypeCode.UInt16:
					if (CompressIntsAs7Bits)
						bw.Write7BitEncodedUShort((UInt16)data);
					else
						bw.Write((UInt16)data);
					break;
				case TypeCode.UInt32:
					if (CompressIntsAs7Bits)
						bw.Write7BitEncodedUInt((UInt32)data);
					else
						bw.Write((UInt32)data);
					break;
				case TypeCode.UInt64:
					if (CompressIntsAs7Bits)
						bw.Write7BitEncodedULong((UInt64)data);
					else
						bw.Write((UInt64)data);
					break;

				default:
#if DEBUG
					throw new Exception();
#else
						break;
#endif
			}
		}

		// ----------------------------------------------------------------------
		// ----------------------------------------------------------------------
		// ----------------------------------------------------------------------
		// ----------------------------------------------------------------------
		// ----------------------------------------------------------------------
		//}

		// ######################################################################


		public void Dispose()
		{
			this.channelInfos[0].Dispose();
			this.channelInfos[1].Dispose();

			if (this.streamWriter != null)
				this.streamWriter.Dispose();
			if (this.binaryWriter != null)
				this.binaryWriter.Dispose();
		}
	}

	/// <summary>
	/// Represents an element (xml vocabulary).
	/// </summary>
	internal struct Element
	{
		internal ElementTypes ElementType;
#if DEBUG
		internal string Name;
#endif
		internal int typeIndex; // if typeIndexIsKnown is true.
		internal bool typeIndexIsKnown;
		internal int? InstanceIndex;
		internal bool NeedsAnEndElement; // ex in xml: <e> does need while <e/> does not need.
		internal bool ContainsAValue; // ex in xml: <e>value</e>.
		internal bool IsAClosingTag; // ex in xml: </c>.
		/// <summary>
		/// For a collection or a dictionary.
		/// </summary>
		internal long? NumberOfElements;
	}

	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
}
