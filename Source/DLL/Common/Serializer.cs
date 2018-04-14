
// Copyright Christophe Bertrand.

		/* Reminder:
		 * KwownTypes: lets the programmer user have type descriptors in common.
		 * SerializationTypeDescriptors: type descriptors in a small form, for serialization.
		 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
#if DEBUG
using System.Diagnostics;
#endif
using UniversalSerializerLib3.TypeManagement;
using System.Runtime.CompilerServices;
using UniversalSerializerLib3.StreamFormat3;

namespace UniversalSerializerLib3
{
	internal enum StreamingModes { AssembledStream, SetOfStreams, MultiplexStream };

	/// <summary>
	/// Available formatters.
	/// There one formatter for each file format.
	/// </summary>
	public enum SerializerFormatters
	{
		/// <summary>
		/// The default formatter. Fast and efficient. This is a custom binary format.
		/// </summary>
		BinarySerializationFormatter,
		/// <summary>
		/// A XML formatter. The XML format is based on general descriptions. It is not human-readable.
		/// </summary>
		XmlSerializationFormatter
#if !JSON_DISABLED
			,
		/// <summary>
		/// A JSON formatter. The JSON format is based on general descriptions. It is not human-readable.
		/// </summary>
		JSONSerializationFormatter
#endif
	};

	// ######################################################################

	/// <summary>
	/// A set of streams needed to serialize fast.
	/// All of them must be defined.
	/// </summary>
	public class SetOfStreams
	{
		/// <summary>
		/// This stream contains the type descriptions.
		/// </summary>
		public readonly Stream TypesStream;
		/// <summary>
		/// This stream contains the values.
		/// </summary>
		public readonly Stream InstancesStream;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="TypesStream">Type descriptors stream.</param>
		/// <param name="InstancesStream">Values stream.</param>
		public SetOfStreams(
			Stream TypesStream,
			Stream InstancesStream)
		{
			if (TypesStream == null)
				throw new ArgumentNullException("TypesStream");
			if (InstancesStream == null)
				throw new ArgumentNullException("InstancesStream");
			this.TypesStream = TypesStream;
			this.InstancesStream = InstancesStream;
		}

	}

	// ######################################################################

	internal abstract partial class SerializationFormatter
	{

		// -------------------------------------------------------------------------------

		internal static SerializationFormatter ChooseDefaultFormatter(Parameters parameters)
		{
			switch (parameters.SerializerFormatter)
			{
				case SerializerFormatters.BinarySerializationFormatter:
					return new BinarySerializationFormatter(parameters);
#if !JSON_DISABLED
				case SerializerFormatters.JSONSerializationFormatter:
					return new JSONSerializationFormatter(parameters);
#endif
				case SerializerFormatters.XmlSerializationFormatter:
					return new XmlSerializationFormatter(parameters);
				default:
					throw new ArgumentException(ErrorMessages.GetText(2)); // "Unknown CustomFormatter");
			}
		}

		internal static DeserializationFormatter ChooseDefaultDeFormatter(Parameters parameters)
		{
			switch (parameters.SerializerFormatter)
			{
				case SerializerFormatters.BinarySerializationFormatter:
					return new BinaryDeserializationFormatter(parameters);
#if !JSON_DISABLED
				case SerializerFormatters.JSONSerializationFormatter:
					return new JSONDeserializationFormatter(parameters);
#endif
				case SerializerFormatters.XmlSerializationFormatter:
					return new XmlDeserializationFormatter(parameters);
				default:
					throw new ArgumentException(ErrorMessages.GetText(3)); // "Unknown CustomDeFormatter");
			}
		}

		// -------------------------------------------------------------------------------

		/// <summary>
		/// Serialize (another) data to the stream(s), using the same CustomParameters, keeping types in common.
		/// </summary>
		/// <param name="data"></param>
		/// <exception cref="NotSupportedException">The stream does not support writing.</exception>
		public void SerializeAnotherData(
			object data)
		{
			if (!this.parameters.Stream.CanWrite)
				throw new NotSupportedException();
			this.SubSerialize(data);
		}

		// -------------------------------------------------------------------------------

		/// <summary>
		/// Creates a container for this object, if any, and returns the container instance and type.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="typeManager"></param>
		/// <param name="containerTypeManager">TypeManager of the container.</param>
		/// <returns></returns>
		internal object CreateAContainerIfNecessary(
			object obj,
			L3TypeManager typeManager,
			out L3TypeManager containerTypeManager)
		{
			ITypeContainer container = typeManager.l2TypeManager.Container;
			if (container == null)
			{
				containerTypeManager = null;
				return null;
			}

			var obj2 = container.CreateNewContainer(obj);
			if (obj2 == null)
			{
				typeManager.l2TypeManager.Container = null; // The transcoder of this type is invalid. We do not use this container anymore.
				containerTypeManager = null;
				return obj;
			}

			containerTypeManager = l3typeManagerCollection.GetTypeManager(obj2.GetType(), this, true, false);
			return obj2;
		}

		// -------------------------------------------------------------------------------


		// ######################################################################

		internal class IndexClass
		{
			internal IndexClass(int value)
			{
				this.Value = value;
			}

			internal readonly int Value;
		}

		#region Sub-Serializer
#if !WINDOWS_PHONE && !PORTABLE && !NET3_5
		ConditionalWeakTable<object, IndexClass> ClassInstanceIndexes;
#else
        Dictionary<object, IndexClass> ClassInstanceIndexes; // TODO: replace by something quicker.
#endif
		int ClassInstancesCount;

		/// <summary>
		/// Serialize (another) data, using the same parameters, keeping types in common.
		/// </summary>
		/// <param name="data"></param>
		internal void SubSerialize(
			object data
 )
		{
#if !WINDOWS_PHONE && !PORTABLE && !NET3_5
			this.ClassInstanceIndexes = new ConditionalWeakTable<object, IndexClass>();
#else
            this.ClassInstanceIndexes = new Dictionary<object, IndexClass>(); // TODO: replace by something faster.
#endif
			this.ClassInstancesCount = 0;

			this.StartTree();

			// inserts Header: (new in version 3.0)
			{
				Header header = new Header();

				// inserts modifiers' assembly list:

				// Do not take in account this own assembly.
				ModifiersAssembly[] a1 = new ModifiersAssembly[parameters.ModifiersAssemblies.Length - 1];
				Array.Copy(parameters.ModifiersAssemblies, 0, a1, 0, a1.Length);
				header.AssemblyIdentifiers = a1.Select((a) => new AssemblyIdentifier(a.assembly)).ToArray();

				{ // Inserts in stream:
					var tm = this.l3typeManagerCollection.GetTypeManager(typeof(Header), this, true, false);
					this.AddAnObject(
						ref this.channelInfos[(int)ChannelNumber.InstancesChannel],
						header,
#if DEBUG
 "Header",
#else
					null,
#endif
 tm.TypeIndex,
						tm,
						false, // The header is not (officially) placed at root because it is not the main data.
						false);
				}
			}

			// Serialize main data:
			{
				Type t = data != null ? data.GetType() : typeof(object);

				var tm = this.l3typeManagerCollection.GetTypeManager(t, this, true, false);
				this.AddAnObject(
					ref this.channelInfos[(int)ChannelNumber.InstancesChannel], // The main object is added as an instance, even when it s structure.
					data,
					null,
					tm.TypeIndex, // Type is done for the root object.
					tm,
					tm.l2TypeManager.IsClass, // structures are not placed at root because that would create an instance element <s1>, then during deserialization that would imply it is an instance (a class) but it is not.
					true);
			}

			// serialize the type descriptors:
			if (parameters.SerializeTypeDescriptors && parameters.TheStreamingMode == StreamingModes.AssembledStream)
			{
				AddACollection(
					ref this.channelInfos[(int)ChannelNumber.TypeDescriptorsChannel],
					this.l3typeManagerCollection.GetSerializationTypeDescriptors(),
					this.l3typeManagerCollection.GetTypeManager(typeof(SerializationTypeDescriptorCollection), null, true, false)
);
			}

			this.FinalizeTreeToStream();
		}

		// ---------------------------------------------

		void AddACollection(
			ref ChannelInfos channelInfos,
			IEnumerable iEnumerable,
			L3TypeManager collectionTypeManager,
			string NameToWrite = null)
		{
			if (collectionTypeManager.l2TypeManager.L1TypeManager.type.IsArray)
			{
				Array array = iEnumerable as Array;
				if (array.Rank != 1)
					throw new Exception(ErrorMessages.GetText(1)); // "This version of UniversalSerializer can not manage multi-dimentionnal arrays."
																   // TODO: create a Container for complex arrays (multidimensional and not-0-based, see Array.GetLowerBound()).
			}


			Type collectionType = iEnumerable.GetType();
			L3TypeManager typeManager =
				collectionTypeManager.l2TypeManager.L1TypeManager.type == collectionType ?
				collectionTypeManager
				: this.l3typeManagerCollection.GetTypeManager(collectionType, this, true, false);
			int collectionTypeNumber =
				typeManager.TypeIndex;

			if (iEnumerable.IsEmpty())
			{
				// Optimisation: we only write an empty object as a collection.
				this.ChannelAddNull(
					ref channelInfos
);

				return;
			}

			Type CommonItemType = // can be null.
				typeManager.CollectionItemsTypeManager != null ?
				typeManager.CollectionItemsTypeManager.l2TypeManager.L1TypeManager.type
				: null;

			this.ChannelEnterCollection(
				ref channelInfos
#if DEBUG
, NameToWrite
#endif
#if DEBUG
, collectionType
#endif
);
			var CommonItemTM = typeManager.CollectionItemsTypeManager;
			int? CommonItemTypeNumber =
				CommonItemTM != null ?
				CommonItemTM.TypeIndex
				: (int?)null;


			if (CommonItemTM == null || CommonItemTM.l2TypeManager.IsClass || TypeTools.TypeEx.IsInterface(CommonItemTM.l2TypeManager.L1TypeManager.type))
			{
				// For item types that are not-sealed classes or interfaces:

				// item of foreach can not be referenced. Therefore we enumerate manually:
				var enumerator = iEnumerable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					var item = enumerator.Current;
					if (item != null)
					{
						Type itemType = item.GetType();
						L3TypeManager itemTm =
							itemType != CommonItemType ?
							this.l3typeManagerCollection.GetTypeManager(item.GetType(), this, true, false) // Items can have different types, even if all items inherit the same type.
							: CommonItemTM;
						int ItemTypeNumber = itemTm.TypeIndex;
						bool WriteTypeNumber = (ItemTypeNumber != CommonItemTypeNumber); // We write the type index only if it is different from the common type index.

						this.AddAnObject(ref channelInfos, item, null, ItemTypeNumber, itemTm, false, WriteTypeNumber);
					}
					else
						this.ChannelAddNull(
							ref channelInfos
);
				}
			}
			else // structures or sealed classes can not inherit therefore can be serialized faster (because we don't have to specify the item type):
			{
#if !DEBUG && !WINDOWS_UWP
				if (CommonItemTypeNumber != null)
				{
					// Using a generic IEnumerable is a bit quicker (11 % on .NET 4.5).
					var mi2 = gfAddAllStructs.MakeGenericMethod(CommonItemType);
					mi2.Invoke(this, new object[] {
							iEnumerable, channelInfos, CommonItemTypeNumber.Value, CommonItemTM
						});
				}
				else
#endif
				{
					// The normal algorithm makes debugging easier.
					// item of foreach can not be referenced. Therefore we enumerate manually:
					var enumerator = iEnumerable.GetEnumerator();
					while (enumerator.MoveNext())
					{
						var item = enumerator.Current;
						this.AddAnObject(ref channelInfos, item, null, CommonItemTypeNumber.Value, CommonItemTM, false, false);
					}
				}
			}
			this.ChannelExitCollection(ref channelInfos);
		}
#if !DEBUG && !WINDOWS_UWP
		#region Serialize items of a struct enumerable
		System.Reflection.MethodInfo gfAddAllStructs;
		delegate void delAddAllStructs<T>(IEnumerable<T> list, ref ChannelInfos channelInfos, int TypeNumber, L3TypeManager tm);
		void addAllStructs<T>(IEnumerable<T> list, ref ChannelInfos channelInfos, int TypeNumber, L3TypeManager tm)
		{
			T[] array = list as T[];
			if (array != null)
			{
				int size = array.Length;
				for (int i = 0; i < size; i++)
					this.AddAnObject(ref channelInfos, array[i], null, TypeNumber, tm, false, false);
			}
			else
			{
				var enumerator = list.GetEnumerator();
				while (enumerator.MoveNext())
				{
					var item = enumerator.Current;
					this.AddAnObject(ref channelInfos, item, null, TypeNumber, tm, false, false);
				}
			}
		}
		#endregion Serialize items of a struct enumerable
#endif

		// ---------------------------------------------

		void AddADictionary(
			ref ChannelInfos channelInfos,
			IDictionary AsIDictionary,
			L3TypeManager typeManager
)
		{
			int dictionaryTypeNumber =
				typeManager.TypeIndex;

			if (AsIDictionary.Count == 0)
			{
				// Optimisation: we only write an empty object as a collection.
				this.ChannelAddNull(
					ref channelInfos
);
				return;
			}

			L3TypeManager CommonKeyTM = typeManager.DictionaryKeysTypeManager; // can be null.
			L3TypeManager CommonValueTM = typeManager.DictionaryValuesTypeManager; // can be null.
			int? CommonKeyTypeNumber =
				CommonKeyTM != null ?
				CommonKeyTM.TypeIndex
				: (int?)null;
			int? CommonValueTypeNumber =
				CommonValueTM != null ?
				CommonValueTM.TypeIndex
				: (int?)null;


			this.ChannelEnterDictionary(ref channelInfos);

			// TODO: from here, separate generic dictionaries from object dict.

			Type CommonKeyTMType =
				CommonKeyTM != null ? CommonKeyTM.l2TypeManager.L1TypeManager.type : null;
			Type CommonValueTMType =
				CommonValueTM != null ? CommonValueTM.l2TypeManager.L1TypeManager.type : null;

			foreach (object _item in AsIDictionary)
			{
				object Key, Value;
				if (_item is DictionaryEntry)
				{
					DictionaryEntry item = (DictionaryEntry)_item;
					Key = item.Key;
					Value = item.Value;
				}
				else
					if (_item is KeyValuePair<object, object>)
				{
					KeyValuePair<object, object> item = (KeyValuePair<object, object>)_item;
					Key = item.Key;
					Value = item.Value;
				}
				else
					throw new Exception();


				// We simply write the key and the value, with no decoration.

				if (Key != null)
				{
					Type itemKeyType = Key.GetType();

					L3TypeManager itemKeyTm =
						itemKeyType != CommonKeyTMType ?
						this.l3typeManagerCollection.GetTypeManager(itemKeyType, this, true, false) // Items can have different types, even if all items inherit the same type.
						: CommonKeyTM;
					int ItemKeyTypeNumber = itemKeyTm.TypeIndex;
					bool WriteTypeNumber = (ItemKeyTypeNumber != CommonKeyTypeNumber); // We write the type index only if it is different from the common type index.

					this.AddAnObject(ref channelInfos, Key, null, ItemKeyTypeNumber, itemKeyTm, false, WriteTypeNumber);
				}
				else
					// We add an empty element. If not, the collection count would be wrong.
					this.ChannelAddNull(
						ref channelInfos
);

				if (Value != null)
				{
					Type itemValueType = Value.GetType();

					L3TypeManager itemValueTm =
						itemValueType != CommonValueTMType ?
						this.l3typeManagerCollection.GetTypeManager(itemValueType, this, true, false) // Items can have different types, even if all items inherit the same type.
						: CommonValueTM;
					int ItemValueTypeNumber = itemValueTm.TypeIndex;
					bool WriteTypeNumber = (ItemValueTypeNumber != CommonValueTypeNumber); // We write the type index only if it is different from the common type index.

					this.AddAnObject(ref channelInfos, Value, null, ItemValueTypeNumber, itemValueTm, false, WriteTypeNumber);
				}
				else
					// We add an empty element. If not, the collection count would be wrong.
					this.ChannelAddNull(
						ref channelInfos
);



			}
			this.ChannelExitDictionary(ref channelInfos);
		}
		/* TODO: later: void WriteGenericDictPairs<Tkey, TValue>(IDictionary<Tkey, TValue> dict)
        {
        }*/


		// ---------------------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void AddAnObject(
			ref ChannelInfos channelInfos, object obj, string NameToWrite, int TypeNumber, L3TypeManager tm, bool AtRoot, bool WriteType)
		{
#if DEBUG
			if (tm.TypeIndex == (int)TypeCode.Object && obj != null)
				Debugger.Break();
#endif

			bool AddSectionMark = AtRoot && this.parameters.TheStreamingMode == StreamingModes.MultiplexStream;
			if (AddSectionMark)
				this.ChannelEnterChannelSection(ref channelInfos);
			WriteType |= AtRoot && channelInfos.ChannelNumber == SerializationFormatter.ChannelNumber.InstancesChannel;

			if (obj == null)
				this.ChannelAddNull(
					ref channelInfos
);
			else
				if (tm.l2TypeManager.IsAPrimitiveType)
				this.AddPrimitiveElementOrReference(
					ref channelInfos, obj,
#if DEBUG
 NameToWrite,
tm.l2TypeManager.L1TypeManager.type,
#endif
 TypeNumber,
					null,
			WriteType);
			else
				this.AddAComplexObject(ref channelInfos, obj,
#if DEBUG
 NameToWrite,
#endif
 TypeNumber, tm, WriteType);

			if (AddSectionMark)
				this.ChannelExitChannelSection(ref channelInfos);
		}

		// -------------------------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		void AddPrimitiveElementOrReference(
			ref ChannelInfos channelInfos,
			object Value,
#if DEBUG
 string debug_Name,
				Type debug_type,
#endif
 int TypeNumber,
			int? ReferenceToInstanceNumber,
			bool ShouldWriteType)
		{
#if DEBUG
			if (Value == null)
				throw new ArgumentNullException();
#endif

			#region Manages instances
			if (Value.GetType() == typeof(string)
				&& channelInfos.ChannelNumber == ChannelNumber.InstancesChannel)
			{

				IndexClass index;
				if (!this.ClassInstanceIndexes.TryGetValue(Value, out index))
				{
					index = new IndexClass(this.AddObjToKnownInstances(Value));

					// serialize the object at the root of the Instances branch:
					this.ChannelAddPrimitiveElementToRoot(
						ref channelInfos,
						Value,
#if DEBUG
 debug_Name,
#endif
 TypeNumber,  // For an instance, the type is always wrote.
 unchecked((TypeCode)TypeNumber)
#if DEBUG
, this.l3typeManagerCollection.GetTypeManager(TypeNumber).l2TypeManager.L1TypeManager.type
#endif
);

				}

				// Replace instance by a reference:
				this._ChannelAddReference(
					ref channelInfos,
#if DEBUG
 debug_Name,
#endif
 index.Value
#if DEBUG
, debug_type
#endif
);
				return;

			}
			#endregion Manages instances

			this._ChannelAddPrimitiveElement(
				ref channelInfos,
				Value,
#if DEBUG
 debug_Name,
#endif
 ShouldWriteType ? TypeNumber : (int?)null,
 unchecked((TypeCode)TypeNumber)
#if DEBUG
, debug_type
#endif
);

		}

		// -------------------------------------------------

		int AddObjToKnownInstances(object obj)
		{
			int index = this.ClassInstancesCount++;
			this.ClassInstanceIndexes.Add(obj, new IndexClass(index));
			return index;
		}

		// -------------------------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		void AddAComplexObject(ref ChannelInfos channelInfos, object obj,
#if DEBUG
 string NameToWrite,
#endif
 int TypeNumber, L3TypeManager tm, bool WriteType)
		{

#if DEBUG
			if (tm == null)
				Debugger.Break();
#endif

			{
				L3TypeManager tm2 = null;
				var obj2 = this.CreateAContainerIfNecessary(obj, tm, out tm2);
				if (tm2 != null)
				{
					tm = tm2;
					TypeNumber = tm2.TypeIndex;
					WriteType = true;
					obj = obj2;
				}
			}

			#region Manages instances
			bool typeToBeSerializedIsAClass = tm.l2TypeManager.IsClass;
			if (typeToBeSerializedIsAClass && channelInfos.ChannelNumber != ChannelNumber.TypeDescriptorsChannel)
			{
				IndexClass index;
				if (!this.ClassInstanceIndexes.TryGetValue(obj, out index))
				{
					index = new IndexClass(this.AddObjToKnownInstances(obj));

					// serialize the object at the root of the Instances branch:
					this.AddAComplexObject_NoInstanceTest(
						ref this.channelInfos[(int)ChannelNumber.InstancesChannel],
						obj,
#if DEBUG
 NameToWrite,
#endif
 tm, true,
						true); // For an instance, the type is always wrote.
				}
				// Replace instance by a reference:
				this._ChannelAddReference(
					ref channelInfos,
#if DEBUG
 NameToWrite,
#endif
 index.Value
#if DEBUG
, this.l3typeManagerCollection.GetTypeManager(TypeNumber).l2TypeManager.L1TypeManager.type
#endif
);
				return;
			}
			#endregion Manages instances

			// We serialize the complex object:
			AddAComplexObject_NoInstanceTest(ref channelInfos, obj,
#if DEBUG
 NameToWrite,
#endif
 tm, false, WriteType);
		}

		// -------------------------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		void AddAComplexObject_NoInstanceTest(
			ref ChannelInfos channelInfos, object obj,
#if DEBUG
 string NameToWrite,
#endif
 L3TypeManager typeManager, bool AtRoot, bool WriteType)
		{
			IDictionary AsIDictionary = null;

			bool AddSectionMark = AtRoot && this.parameters.TheStreamingMode == StreamingModes.MultiplexStream;
			if (AddSectionMark)
				this.ChannelEnterChannelSection(ref channelInfos);

			long? NumberOfElements = null;
			if (typeManager.l2TypeManager.L1TypeManager.type.IsArray)
			{
				Array array = obj as Array;
				if (array.Rank != 1)
					throw new Exception(ErrorMessages.GetText(1)); // "This version of UniversalSerializer can not manage multi-dimentionnal arrays."
																   // TODO: create a Container for complex arrays (not only multidimensional, see Array.GetLowerBound() too).
				NumberOfElements =
#if SILVERLIGHT || PORTABLE || WINDOWS_UWP
 array.Length;
#else
 array.GetLongLength(0);
#endif
			}
			else
				if (typeManager.l2TypeManager.L1TypeManager.IsAnObjectIEnumerable)
				NumberOfElements = (obj as IEnumerable).GetCount();
			else
					if (typeManager.l2TypeManager.L1TypeManager.IsADictionary)
			{
				AsIDictionary = obj as IDictionary;
				if (AsIDictionary != null)
					NumberOfElements = AsIDictionary.Count;
				else
				{
					AsIDictionary = Tools.GenericIDictionaryBoxer<int, int>.CreateFromGenericIDictionary(obj, typeManager);
					if (AsIDictionary != null)
						NumberOfElements = AsIDictionary.Count;
					else
						throw new Exception(); // error.
				}
			}

			this.ChannelEnterSubBranch(
				ref channelInfos,
				NumberOfElements,
#if DEBUG
 NameToWrite,
#endif
 WriteType ? (int?)typeManager.TypeIndex : null,
null
#if DEBUG
, typeManager.l2TypeManager.L1TypeManager.type
#endif
, typeManager.l2TypeManager.L1TypeManager.IsStructure
);
			{
				#region serialize the fields.
				if (typeManager.SelectedFieldTypeManagers != null)
					for (int ifi = 0; ifi < typeManager.SelectedFieldTypeManagers.Length; ifi++)
					{
						object fieldValue =
								typeManager.l2TypeManager.SelectedFieldGetters[ifi](obj);

						Type FieldType = typeManager.l2TypeManager.SelectedFields[ifi].FieldType;

						Type t = fieldValue != null ? fieldValue.GetType() : FieldType;
						L3TypeManager _tm;
						bool serializable = FieldType == t ? true : l3typeManagerCollection.l2TypeManagerCollection.CanThisTypeBeSerialized(t);
						if (!serializable)
							fieldValue = null;

						if (FieldType != t && serializable)
						{
#if DEBUG
							if (!t.Is(FieldType)) // TODO: optimize this test and let it even in relase compilation.
								if (!FieldType.Is(typeof(Nullable<>)))
									throw new Exception();
#endif
							_tm = this.l3typeManagerCollection.GetTypeManager(t, this, true, false);
						}
						else
						{
							_tm = typeManager.SelectedFieldTypeManagers[ifi];
							if (_tm == null)
							{
								_tm =
									typeManager.SelectedFieldTypeManagers[ifi] =  // updates the TypeManager.
										this.l3typeManagerCollection.GetTypeManager(FieldType, this, true, false);
								typeManager.l2TypeManager.SelectedFieldTypeManagers[ifi] = _tm.l2TypeManager;
							}
						}
						bool WriteTypeNumber = serializable && FieldType != t;

						this.AddAnObject(ref channelInfos, fieldValue,
 null,
 _tm.TypeIndex,
_tm, false,
 WriteTypeNumber
);
					}
				#endregion serialize the fields.

				#region serialize the properties.
				if (typeManager.SelectedPropertyTypeManagers != null)
					// serialize the properties.
					for (int ipi = 0; ipi < typeManager.SelectedPropertyTypeManagers.Length; ipi++)
					{
						var pi = typeManager.l2TypeManager.SelectedProperties[ipi];
						Type propertyType = pi.PropertyType;

						object propertyValue =
							typeManager.l2TypeManager.SelectedPropertyGetters[ipi](obj);
						Type t = propertyValue != null ? propertyValue.GetType() : propertyType;
						L3TypeManager _tm;

						bool serializable = propertyType == t ? true : l3typeManagerCollection.l2TypeManagerCollection.CanThisTypeBeSerialized(t);
						if (!serializable)
							propertyValue = null;

						if (propertyType != t && serializable)
						{
#if DEBUG
							if (!t.Is(propertyType)) // TODO: optimize this test and let it even in relase compilation.
								if (!propertyType.Is(typeof(Nullable<>)))
									throw new Exception();
#endif
							_tm = this.l3typeManagerCollection.GetTypeManager(t, this, true, false);
						}
						else
						{
							_tm = typeManager.SelectedPropertyTypeManagers[ipi];
							if (_tm == null)
							{
								_tm =
									typeManager.SelectedPropertyTypeManagers[ipi] =  // updates the TypeManager.
										this.l3typeManagerCollection.GetTypeManager(propertyType, this, true, false);
								typeManager.l2TypeManager.SelectedPropertyTypeManagers[ipi] = _tm.l2TypeManager;
							}
						}
						bool WriteTypeNumber = serializable && propertyType != t;

						this.AddAnObject(ref channelInfos, propertyValue,
 null,
 _tm.TypeIndex,
_tm, false,
 WriteTypeNumber
);
					}
				#endregion serialize the properties.

				// serialize an inner dictionary:
				if (typeManager.l2TypeManager.L1TypeManager.IsADictionary)
				{
					this.AddADictionary(
						ref channelInfos, AsIDictionary, typeManager
);
				}

				// serialize an inner collection:
				if (typeManager.l2TypeManager.L1TypeManager.IsAnObjectIEnumerable)
				{
					this.AddACollection(ref channelInfos, obj as IEnumerable, typeManager
);
				}
			}
			this.ChannelExitSubBranch(ref channelInfos);
			if (AddSectionMark)
				this.ChannelExitChannelSection(ref channelInfos);
		}

		// -------------------------------------------------
		// -------------------------------------------------
		// -------------------------------------------------

		class IndexedTypeManager
		{
			internal readonly int Index;
			internal readonly L2TypeManager typeManager;

			internal IndexedTypeManager(int Index, L2TypeManager typeManager)
			{
				this.Index = Index;
				this.typeManager = typeManager;
			}
		}

		// -------------------------------------------------------



		// --------------
		#endregion Sub-Serializer

	}


	// ######################################################################
	// ######################################################################


}