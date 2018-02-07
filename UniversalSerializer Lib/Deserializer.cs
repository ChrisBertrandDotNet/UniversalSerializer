
// Copyright Christophe Bertrand.

/* Reminder:
 * KwownTypes: lets the programmer user have type descriptors in common.
 * SerializationTypeDescriptors: type descriptors in a small form, for serialization.
 */

using System;
using System.Collections;
using System.IO;
#if DEBUG
using System.Diagnostics;
#endif
using UniversalSerializerLib3.TypeManagement;
using System.Collections.Generic;
using UniversalSerializerLib3.StreamFormat3;

namespace UniversalSerializerLib3
{

	// ######################################################################


	// ######################################################################

	public abstract partial class DeserializationFormatter
	{
		// -------------------------------------------------------------------------------

		/// <summary>
		/// Serialize (another) data to the stream(s), using the same CustomParameters, keeping types in common.
		/// If the stream position is the end of the stream, the position is set to 0 first.
		/// </summary>
		public T DeserializeAnotherData<T>()
		{
			var deserialized = this.DeserializeAnotherData();
			if (deserialized != null && !(deserialized is T))
				throw new TypeMismatchException(deserialized.GetType(), typeof(T));
			return (T)deserialized;
		}

		// -------------------------------------------------------------------------------

		/// <summary>
		/// Serialize (another) data to the stream(s), using the same CustomParameters, keeping types in common.
		/// If the stream position is the end of the stream, the position is set to 0 first.
		/// </summary>
		public Object DeserializeAnotherData()
		{
			return this.Deserialize();
		}

		// ######################################################################

		/// <summary>
		/// Can not cast deserialized type.
		/// </summary>
		public class TypeMismatchException : Exception
		{
			/// <summary>
			/// Can not cast deserialized type.
			/// </summary>
			/// <param name="DeserializedType"></param>
			/// <param name="WantedType"></param>
			public TypeMismatchException(Type DeserializedType, Type WantedType)
				: base(string.Format(
				ErrorMessages.GetText(10),// "Can not cast deserialized type \"{0}\" to wanted type \"{1}\".",
				DeserializedType.FullName, WantedType.FullName))
			{
			}
		}

		// ######################################################################

		#region Sub-Deserializer
		InstancesChannel instancesChannel;
		L3TypeManager TypeDescriptorTypeManager;
		bool DataEndMarkHasBeenPassed;
		internal L3TypeManagerCollection typeManagerCollection;

		internal void ForgetDataEndMark()
		{
			this.DataEndMarkHasBeenPassed = false;
		}

		internal Object Deserialize()
		{
			if (this.DataEndMarkHasBeenPassed)
				throw new EndOfStreamException();
			this.DataEndMarkHasBeenPassed = false;
			this.instancesChannel = new InstancesChannel();
			if (this.stream.Position == this.stream.Length)
				this.stream.Position = 0;
			this.SetStreamPosition(this.stream.Position); // Useful if any cache memory.


			this.PassPreamble();

			if (this.typeManagerCollection == null)
				this.typeManagerCollection =
					new L3TypeManagerCollection(this.parameters.customModifiers, this.Version);

			this.TypeDescriptorTypeManager = this.typeManagerCollection.GetTypeManager((int)SerializationFormatter.CompulsoryType.SerializationTypeDescriptorIndex);

			if (this.Version == StreamFormatVersion.Version3_0)
			{
				// We load the Header (new in version 3.0):

				Header header = (Header)this.LookForObjectAndManagesChannels(
					 this.typeManagerCollection.GetTypeManager((int)SerializationFormatter.CompulsoryType.HeaderIndex),
					 null, null, false);

				// now we try to load these assemblies:
				foreach (var ai in header.AssemblyIdentifiers)
				{
					ai.Load();
				}
			}

			object o = this.LookForObjectAndManagesChannels(null, null, null, false);

			if (!this.DataEndMarkHasBeenPassed)
				this.PassDataEndMark();
			this.PassTreeEndTag();

			return o;
		}

		// --------------------------------------------------

		internal class Signal
		{
		}
		static readonly Signal DeserializationTerminated = new Signal();
		internal static readonly Signal NoValue = new Signal();

		// --------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Deserialized object, or ElementTypes.TypesChannelSection if some type has been described, or null if no more elements have been found, or InstanceNotReadyAndListOfNeeds if instance is not available yet.</returns>
		Object LookForObjectAndManagesChannels(
			L3TypeManager WantedType,
 object AlreadyInstanciatedCollectionOrDictionary, // For inner collection or dictionary.
long? NumberOfElements, // For inner collection or dictionary.
							bool AddToTheInstances)
		{
			object obj = this._LookForObjectAndManagesChannels(
				WantedType,
 AlreadyInstanciatedCollectionOrDictionary, NumberOfElements, AddToTheInstances);

			var obj2 = obj as ITypeContainer;
			if (obj2 != null)
			{
				var obj3 = obj2.Deserialize();
				return obj3;
			}
			return obj;
		}

		// --------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Deserialized object, or ElementTypes.TypesChannelSection if some type has been described, or DeserializationTerminated if no more elements have been found, or InstanceNotReadyAndListOfNeeds if instance is not available yet.</returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		Object _LookForObjectAndManagesChannels(
			L3TypeManager WantedType,
 object AlreadyInstanciatedCollectionOrDictionary, // For inner collection or dictionary.
long? NumberOfElements, // For inner collection or dictionary.
							bool AddToTheInstances)
		{
			Element e = new Element();

			{
				bool found =
					this.GetNextElementAs(ref e, true, null, WantedType, false, false);

				if (!found)
				{
					this.DataEndMarkHasBeenPassed = true;
					return DeserializationTerminated;
				}
			}

			switch (e.ElementType)
			{
				case ElementTypes.TypesChannelSection:
					// Deserialize a type descriptor and add it to the type manager collection:
					{
						SerializationTypeDescriptor td =
							(SerializationTypeDescriptor)this.LookForObjectAndManagesChannels(
								this.TypeDescriptorTypeManager, null, null, false);
						this.typeManagerCollection.AddNewType(td);
						this.PassClosingTag(ElementTypes.TypesChannelSection);
						return this.LookForObjectAndManagesChannels(WantedType
, AlreadyInstanciatedCollectionOrDictionary
, NumberOfElements
, false
); // look for next element.
					}

				case ElementTypes.InstancesChannelSection:
					// Deserialize an object and add it to the root of the instances channel:
					{
						object instance = this.LookForObjectAndManagesChannels(null
, null
, null
, true
);
						this.PassClosingTag(ElementTypes.InstancesChannelSection);
						object obj = this.LookForObjectAndManagesChannels(WantedType
, AlreadyInstanciatedCollectionOrDictionary
, NumberOfElements
, false
); // look for next element.

						if (obj == DeserializationTerminated)
							return instance;
						return obj;
					}

				case ElementTypes.SubBranch: // Deserializes a structured object:
					return this.DeserializeSubBranch(e, WantedType, AlreadyInstanciatedCollectionOrDictionary, AddToTheInstances);

				case ElementTypes.PrimitiveValue:
					{
						Object obj;
						bool closeTagAlreadyPassed = false;
						{
							L3TypeManager t =
							e.typeIndexIsKnown ?
							this.typeManagerCollection.GetTypeManager(e.typeIndex)
							: WantedType;

							// TODO: set GetSimpleValueInElement as generic and write a 'switch' here. Beware to avoid double switch.

							obj = this.GetSimpleValueInElement(
								t);
							if (obj == DeserializationFormatter.NoValue)
							{
								// specific to xml. In xml, we have <p></p> for a string.Empty .
#if DEBUG
								if (this.GetType() != typeof(XmlDeserializationFormatter))
									throw new Exception();
#endif
								closeTagAlreadyPassed = true;
								obj = string.Empty;
							}
						}


						if (AddToTheInstances)
						{
#if DEBUG
							if (obj.GetType() != typeof(string))
								Debugger.Break();
#endif
							this.instancesChannel.AddInstance(obj);
						}

						if (!closeTagAlreadyPassed)
							this.PassClosingTag(ElementTypes.PrimitiveValue);

						return obj;
					}

				case ElementTypes.Reference:
					{
						Object obj;

#if DEBUG
						if (AddToTheInstances)
							Debugger.Break();
#endif
						obj = this.instancesChannel.GetInstance(e.InstanceIndex.Value);

						if (e.NeedsAnEndElement)
						{
#if DEBUG
							// In xml, <e/> elements can be closed in the same tag.
							if (this.GetType() == typeof(XmlDeserializationFormatter))
								throw new Exception();
#endif
							this.PassClosingTag(ElementTypes.Reference);
						}

						return obj;
					}

				case ElementTypes.Null:
					{
						Object obj;

						obj = null;

						if (e.NeedsAnEndElement)
						{
#if DEBUG
							// In xml, <e/> elements can be closed in the same tag.
							if (this.GetType() == typeof(XmlDeserializationFormatter))
								throw new Exception();
#endif
							this.PassClosingTag(ElementTypes.Null);
						}

						return obj;
					}

				case ElementTypes.Collection:
					return DeserializeCollection(AlreadyInstanciatedCollectionOrDictionary, NumberOfElements.Value, WantedType, e);

				case ElementTypes.Dictionary:
					return DeserializeDictionary(AlreadyInstanciatedCollectionOrDictionary, NumberOfElements.Value, WantedType, e);

				default:
					byte code = (byte)e.ElementType;
					throw new NotImplementedException(code.ToString());
			}
			throw new NotImplementedException();

		}
		internal static readonly int StringTypeCode = (int)Type.GetTypeCode(typeof(string));

		// --------------------------------------------------

		Object DeserializeCollection(
			object collection,
			long NumberOfElements,
			L3TypeManager WantedType,
			Element e
			)
		{
#if false//DEBUG
			//if (collection.GetType().Name == "RelationsDHéritage")
			if (WantedType.l2TypeManager.L1TypeManager.Name=="List<DéclarationStructurée>")
				Debugger.Break();
#endif

#if DEBUG
			if (collection == null || WantedType == null)
				Debugger.Break();
#endif
			IList list = collection as IList;
			bool listIsNotIndexed = list == null;
			if (listIsNotIndexed)
			{
				Type ItemType;
				list = Tools.GetIListBoxer(collection as IEnumerable, out ItemType);
			}

			Array array = collection as Array;
			bool is1DArray = array != null && array.Rank == 1;
#if DEBUG
			if (array != null && array.Rank != 1)
				throw new Exception(
					ErrorMessages.GetText(1));//"This version of UniversalSerializer can not manage multi-dimentionnal arrays."
#endif
			L3TypeManager itemsType = WantedType.CollectionItemsTypeManager;// Manager.Value;
#if false
				if (itemsType == null)
					Debugger.Break();
#endif

			for (int i = 0; i < NumberOfElements; i++)
			{
				var item = this.LookForObjectAndManagesChannels(
					itemsType
, null
, null
, false
);

				var notReady = item as InstancesChannel.InstanceNotReadyAndListOfNeeds;

				if (is1DArray)
					if (notReady == null)
						list[i] = item;
					else
					{
						var iCopy = i;  // tells C# to not use a closure for the following lambda.
						var listCopy = list; // tells C# to not use a closure for the following lambda.
						notReady.Setters.Add((o) => listCopy[iCopy] = o);
					}
				else
					if (notReady == null)
						list.Add(item);
					else
					{
						if (listIsNotIndexed)
							throw new NotSupportedException(
								string.Format(ErrorMessages.GetText(22),  // "Type {0} has a circular type in its constructor parameters in a form that is not supported."
								Tools.GetName(itemsType.l2TypeManager.L1TypeManager.type)));
						list.Add(itemsType.l2TypeManager.DefaultValue); // reserve space, to preserve right indexes.
						var listCopy = list; // tells C# to not use a closure for the following lambda.
						var iCopy = i;  // tells C# to not use a closure for the following lambda.
						notReady.Setters.Add((o) => listCopy[iCopy] = o);
					}
			}
			if (e.NeedsAnEndElement)
				this.PassClosingTag(ElementTypes.Collection);

			return collection;
		}

		// --------------------------------------------------

		Object DeserializeDictionary(
			object dictionary,
			long NumberOfElements,
			L3TypeManager WantedType,
			Element e
			)
		{
#if DEBUG
			if (dictionary == null || WantedType == null)
				Debugger.Break();
#endif
			IDictionary dict = Tools.GenericIDictionaryBoxer<int, int>.CreateFromGenericIDictionary(dictionary, WantedType);

			L3TypeManager keysType = WantedType.DictionaryKeysTypeManager; // can be null, for not-generic dictionaries.
			L3TypeManager ValuesType = WantedType.DictionaryValuesTypeManager; // can be null, for not-generic dictionaries.

			for (int i = 0; i < NumberOfElements; i++)
			{
				var key = this.LookForObjectAndManagesChannels(keysType, null, null, false);
				var Value = this.LookForObjectAndManagesChannels(ValuesType, null, null, false);

				var keyAsNotReady = key as InstancesChannel.InstanceNotReadyAndListOfNeeds;
				var valueAsNotReady = Value as InstancesChannel.InstanceNotReadyAndListOfNeeds;

				if (keyAsNotReady == null && valueAsNotReady == null)
					dict.Add(key, Value);
				else
				{
					var dictCopy = dict; // tells C# to not use a closure for the following lambdas.

					if (keyAsNotReady != null && valueAsNotReady != null)
					{
						// TODO: implement (how?)

						string typeMsg = "[ unknown ]";
						if (ValuesType != null && keysType != null)
							typeMsg =
								"[ " + Tools.GetName(keysType.l2TypeManager.L1TypeManager.type) + " or " +
								Tools.GetName(ValuesType.l2TypeManager.L1TypeManager.type) + " ]";
						else
							if (WantedType != null)
							{
								if (WantedType.l2TypeManager.L1TypeManager.AsGenericIDictionaryGenericArguments != null)
									if (WantedType.l2TypeManager.L1TypeManager.AsGenericIDictionaryGenericArguments[0] == WantedType.l2TypeManager.L1TypeManager.AsGenericIDictionaryGenericArguments[1])
										typeMsg = Tools.GetName(WantedType.l2TypeManager.L1TypeManager.AsGenericIDictionaryGenericArguments[0]);
									else
										typeMsg =
											"[ " +
											Tools.GetName(WantedType.l2TypeManager.L1TypeManager.AsGenericIDictionaryGenericArguments[0])
											+ " or " +
											Tools.GetName(WantedType.l2TypeManager.L1TypeManager.AsGenericIDictionaryGenericArguments[1])
											+ " ]";
							}
						throw new NotSupportedException(
							string.Format(ErrorMessages.GetText(22),  // "Type {0} has a circular type in its constructor parameters in a form that is not supported."
							typeMsg));
					}
					else
						if (keyAsNotReady != null)
						{
							var ValueCopy = Value; // tells C# to not use a closure for the following lambda.
							keyAsNotReady.Setters.Add((o) => dictCopy.Add(o, ValueCopy));
						}
						else
						{
							var keyCopy = key; // tells C# to not use a closure for the following lambda.
							valueAsNotReady.Setters.Add((o) => dictCopy.Add(keyCopy, o));
						}
				}
			}
			if (e.NeedsAnEndElement)
				this.PassClosingTag(ElementTypes.Dictionary);

			return dictionary;
		}

		// --------------------------------------------------

		Object DeserializeSubBranch(
			Element e, L3TypeManager WantedType,
			object AlreadyInstanciatedCollectionOrDictionary, // For inner collection or dictionary.
			bool AddToTheInstances)
		{
			L3TypeManager typeManager =
				e.typeIndexIsKnown && (WantedType == null || (WantedType != null && e.typeIndex != WantedType.TypeIndex)) ?
				this.typeManagerCollection.GetTypeManager(e.typeIndex)
				: WantedType;

			// TODO: try to dynamically build a method that deserializes this particular type by this formatter.

			// Two different ways: with a default constructor or with a parametric constructor.
			// A parametric constructor needs to deserialize fields and properties first.

			Object obj = AlreadyInstanciatedCollectionOrDictionary;
#if DEBUG
			if (AlreadyInstanciatedCollectionOrDictionary != null && AddToTheInstances)
				Debugger.Break();
#endif

			if (obj == null)
				obj = typeManager.l2TypeManager.ConstructNewObjectAndSetMembers(typeManager, this, AddToTheInstances, e.NumberOfElements);

			#region Now deserializes the inner collection
			if (typeManager.l2TypeManager.L1TypeManager.IsAnObjectIEnumerable)
			{
				if (AlreadyInstanciatedCollectionOrDictionary == null)
				{
#if DEBUG
					if (e.NumberOfElements == null)
						Debugger.Break();
#endif

#if DEBUG
					IEnumerable collection = (IEnumerable)
#endif
this.LookForObjectAndManagesChannels(
							typeManager
, obj
, e.NumberOfElements
, false
);
				}

			}
			#endregion Now deserializes the inner collection


			#region Now deserializes the inner dictionary
			if (typeManager.l2TypeManager.L1TypeManager.IsADictionary)
				if (AlreadyInstanciatedCollectionOrDictionary == null)
				{
#if DEBUG
					if (e.NumberOfElements == null)
						Debugger.Break();
#endif

#if DEBUG
					IEnumerable dictionary = (IEnumerable)
#endif
this.LookForObjectAndManagesChannels(
							typeManager
, obj
, e.NumberOfElements
, false
);
				}
			#endregion Now deserializes the inner dictionary

			this.PassClosingTag(ElementTypes.SubBranch);
			return obj;
		}

		// ---------------------------------

#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal object ConstructAndSetMembers<T>(
			L3TypeManager l3TypeManager,
			bool AddToTheInstances, long? NumberOfElements)
		{
#if false//DEBUG
			if (l3TypeManager.l2TypeManager.L1TypeManager.Name == "CircularInstancesInAListAndParametricConstructor")
				Debugger.Break();
#endif

			L2GenericTypeManager<T> typeManager = l3TypeManager.l2TypeManager as L2GenericTypeManager<T>;

			T obj;
			var gtd = typeManager.L1TypeManager;

			if (gtd.type.IsArray)
			{
				object array = Array.CreateInstance(typeManager.CollectionItemsTypeManager.L1TypeManager.type,
#if SILVERLIGHT || PORTABLE
                        checked((int)NumberOfElements.Value)
#else
 NumberOfElements.Value
#endif
);
				if (AddToTheInstances)
					this.instancesChannel.AddInstance(array); // Adds the instance before deserializing inner data. Because of possible circular types.
				return array;
			}
			else
				if (typeManager.DefaultConstructor != null)
				{
					#region Build from a default (no-param) constructor

					// creates an instance (or a value if it is a structure):
#if SILVERLIGHT
						if (typeManager.L1TypeManager.type.IsEnum)
							obj = default(T);
						else
#endif
					obj = (T)typeManager.DefaultConstructor();

					if (AddToTheInstances)
						this.instancesChannel.AddInstance(obj); // Add the instance before deserializing inner data.

					if (typeManager.SelectedFields != null)
						typeManager.SetFieldValues(ref obj,
								GetFieldValues(l3TypeManager));

					if (typeManager.SelectedProperties != null)
						typeManager.SetPropertyValues(ref obj,
							GetPropertyValues(l3TypeManager));

					#endregion Build from a default (no-param) constructor
				}
				else
					if (typeManager.parametricConstructorDescriptor != null)
					#region Build from a parametric constructor
					{

						int instanceIndex = 0;
						if (AddToTheInstances)
						{
							instanceIndex = this.instancesChannel.ReserveSpaceForInstance(); // reserves site.
						}


						object[] fieldValues =
						 (typeManager.SelectedFields != null) ?
								GetFieldValues(l3TypeManager)
								: new object[0];
						for (int i = 0; i < fieldValues.Length; i++)
						{
							var nr = fieldValues[i] as InstancesChannel.InstanceNotReadyAndListOfNeeds;
							if (nr != null)
							{
								var iCopy = i; // tells C# to not use a closure for the following lambda.
								nr.Setters.Add((o) => fieldValues[iCopy] = o);
							}
						}

						object[] propValues =
						 (typeManager.SelectedProperties != null) ?
								GetPropertyValues(l3TypeManager)
								: new object[0];
						for (int i = 0; i < propValues.Length; i++)
						{
							var nr = propValues[i] as InstancesChannel.InstanceNotReadyAndListOfNeeds;
							if (nr != null)
							{
								var iCopy = i; // tells C# to not use a closure for the following lambda.
								nr.Setters.Add((o) => propValues[iCopy] = o);
							}
						}

#if false // Future use, when the parameter indexes will be on both fields and properties.
							object[] fieldsAndProps = new object[fieldValues.Length + propValues.Length];
							fieldValues.CopyTo(fieldsAndProps, 0);
							propValues.CopyTo(fieldsAndProps, fieldValues.Length);
#endif
						// TODO: check if the deserialized ParametricConstructorDescriptor is the same as the TypeManager's one.

						int nbPars = typeManager.parametricConstructorDescriptor.ParameterFields.Length;
						object[] parameters = new object[nbPars];
						if (nbPars > 0 && typeManager.ParametricConstructorFieldParameterIndexes == null)
							throw new Exception(ErrorMessages.GetText(19)); // "Type {0} can not be deserialized because of 1) an error in its Container and 2) a refused private field."
						for (int ipars = 0; ipars < nbPars; ipars++)
							parameters[ipars] = fieldValues[typeManager.ParametricConstructorFieldParameterIndexes[ipars]];

						// I do not use a Linq Expression compiled constructor because it takes a lot of time to be produced.
						// A reflection constructor call is slower but it has no creation time.
                        try
                        {
                            obj = (T)typeManager.parametricConstructorDescriptor.constructorInfo.Invoke(parameters);
                        }
                        catch (Exception e)
                        {
                            string msg = string.Format(
								ErrorMessages.GetText(11),//"Construction (instanciation) of type \"{0}\" caused an error: {1}."
                                typeof(T).FullName, e.Message);
                            for (int ip = 0; ip < parameters.Length; ip++)
                            {
                                var p = parameters[ip];
                                if (p != null)
                                    msg += string.Format("\n\tParameter #{0} is type \"{1}\".", ip.ToString(), p.GetType().FullName);
                            }
                            Log.WriteLine(msg);
                            throw new Exception(msg, e);
                        }

						if (AddToTheInstances)
							this.instancesChannel.SetInstance(instanceIndex, obj);

						if (fieldValues.Length > 0)
							typeManager.SetFieldValues(ref obj, fieldValues);

						if (propValues.Length > 0)
							typeManager.SetPropertyValues(ref obj, propValues);
					}
					#endregion Build from a parametric constructor
					else
					{
						string msg = string.Format(
							ErrorMessages.GetText(12)//"No exploitable constructor for type {0}"
							, gtd.type.FullName);
						Log.WriteLine(msg);

						throw new Exception(msg); // No valid constructor.
					}

			return obj;
		}

		// --------------------------------------------------

		object[] GetFieldValues(L3TypeManager typeManager)
		{
			if (typeManager.SelectedFieldTypeManagers == null)
				return new object[0];

			int l = typeManager.SelectedFieldTypeManagers.Length;

			object[] fieldValues = new object[l];

			for (int ifi = 0; ifi < l; ifi++)
			{
				var _tm = typeManager.SelectedFieldTypeManagers[ifi];
				if (_tm == null)
				{
					_tm =
						typeManager.SelectedFieldTypeManagers[ifi] =
							this.typeManagerCollection.GetTypeManager(
							typeManager.l2TypeManager.SelectedFields[ifi].FieldType,
							null, true, true);
					typeManager.l2TypeManager.SelectedFieldTypeManagers[ifi] =
						_tm.l2TypeManager;
				}
				var fieldValue = this.LookForObjectAndManagesChannels(
					_tm
, null
, null
, false
);
				fieldValues[ifi] = fieldValue;
			}
			return fieldValues;
		}

		// --------------------------------------------------

		// --------------------------------------------------

		object[] GetPropertyValues(L3TypeManager typeManager)
		{
			if (typeManager.SelectedPropertyTypeManagers == null)
				return new object[0];

			int l = typeManager.SelectedPropertyTypeManagers.Length;
			object[] propertyValues = new object[l];

			for (int ipi = 0; ipi < l; ipi++)
			{
				var _tm = typeManager.SelectedPropertyTypeManagers[ipi];
				if (_tm == null)
				{
					_tm =
						typeManager.SelectedPropertyTypeManagers[ipi] =
							this.typeManagerCollection.GetTypeManager(
							typeManager.l2TypeManager.L1TypeManager.type,
							null, true, true);
					typeManager.l2TypeManager.SelectedPropertyTypeManagers[ipi] = _tm.l2TypeManager;
				}
				var propValue = this.LookForObjectAndManagesChannels(_tm, null, null, false);

				propertyValues[ipi] = propValue;
			}
			return propertyValues;
		}

		// --------------------------------------------------
		// --------------------------------------------------
		// --------------------------------------------------
		// --------------------------------------------------


		// ######################################################################

		#endregion Sub-Deserializer

		// ######################################################################
		// ######################################################################

		internal class InstancesChannel
		{
			/// <summary>
			/// objects can be:
			/// . a definitive class instance.
			/// . null, as a definitive value.
			/// . a temporary instance of InstanceNotReadyAndListOfNeeds. To be replaced by a definitive class instance when it is ready.
			/// </summary>
			readonly List<object> Instances = new List<object>();

			internal object GetInstance(int index)
			{
#if DEBUG
				if (this.Instances.Count <= index)
					throw new Exception();
#if false
				if (this.Instances[index] is InstanceNotReadyAndListOfNeeds)
					Debugger.Break();
#endif
#endif
				return this.Instances[index];
			}

			internal void SetInstance(int index, object o)
			{
#if DEBUG
				if (this.Instances.Count <= index || !(this.Instances[index] is InstanceNotReadyAndListOfNeeds))
					throw new Exception();
#endif
				InstanceNotReadyAndListOfNeeds setters = (InstanceNotReadyAndListOfNeeds)this.Instances[index];
				this.Instances[index] = o;
				for (int i = 0; i < setters.Setters.Count; i++)
					setters.Setters[i](o);
			}

			internal int AddInstance(object o)
			{
				int index = this.Instances.Count;
				this.Instances.Add(o);
				return index;
			}

			internal int ReserveSpaceForInstance()
			{
				int index = this.Instances.Count;
				this.Instances.Add(new InstanceNotReadyAndListOfNeeds());
				return index;
			}

			/// <summary>
			/// Marks the instance as not ready.
			/// And stores a list of field and property setters to be called when the instance is ready.
			/// </summary>
			internal sealed class InstanceNotReadyAndListOfNeeds
			{
				/// <summary>
				/// Field and property setters to be called when the instance is ready.
				/// </summary>
				internal List<Action<object>> Setters = new List<Action<object>>();
			}

		}
	}


	// ######################################################################
	// ######################################################################


}

