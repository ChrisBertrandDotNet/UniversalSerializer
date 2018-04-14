
// Copyright Christophe Bertrand.

// TypeManager gives informations about the type, and dynamically-compiled methods to access or construct an instance.

#define CheckAllGenericParameters // not compulsory.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using UniversalSerializerLib3.TypeTools;
using UniversalSerializerLib3.DataTools;
#if DEBUG
using System.Diagnostics;
using UniversalSerializerLib3.StreamFormat3;
#endif

namespace UniversalSerializerLib3.TypeManagement
{
	// ######################################################################
	// ######################################################################

	/// <summary>
	/// L2TypeManager depends on SerializationFormatter and on L2TypeManager (which depends on CustomParameter and on L1TypeManager (which depends on Type only)).
	/// </summary>
	internal class L3TypeManager
	{
		internal readonly L2TypeManager l2TypeManager;
		internal int TypeIndex;
		internal readonly L3TypeManager[] SelectedFieldTypeManagers;
		internal readonly L3TypeManager[] SelectedPropertyTypeManagers;
		internal readonly L3TypeManager CollectionItemsTypeManager; // type of the items of a collection or array.
		internal readonly L3TypeManager DictionaryKeysTypeManager;
		internal readonly L3TypeManager DictionaryValuesTypeManager;
		internal readonly int TypeIndexationStart;
#if NET3_5
        /// <summary>
        /// A method to add items to the list.
        /// .NET 3.5 can not do IList.Add(null) on a List&lt;T&gt;. This is a workaround.
        /// </summary>
        internal readonly Lazy<Action<object, object>> ItemAdder;
#endif

		// ------------------------------

		/// <summary>
		/// For serialization.
		/// Simple constructor, for Primitive types only (see System.Type.TypeCode).
		/// </summary>
		internal L3TypeManager(
			L2TypeManager l2TypeManager, int TypeIndex, int TypeIndexationStart)
		{
			this.l2TypeManager = l2TypeManager;
			this.TypeIndex = TypeIndex;
			this.TypeIndexationStart = TypeIndexationStart;
#if NET3_5
            if (l2TypeManager.CollectionItemsTypeManager != null)
                this.ItemAdder = new Lazy<Action<object, object>>(() =>
                {
                    var tItems = this.CollectionItemsTypeManager.l2TypeManager.L1TypeManager.type;
                    if (tItems != null)
                    {
                        var type = this.l2TypeManager.L1TypeManager.type;
                        if (type != null)
                        {
                            var tlist = type.FindDerivedOrEqualToThisType(typeof(List<>));
                            if (tlist != null)
                            {
                                var m = tlist.GetMethod("Add", new Type[1] { tItems });
                                if (m != null)
                                {
                                    return (list, item) => m.Invoke(list, new object[1] { item });
                                }
                            }
                        }
                    }
                    return null;
                });
#endif
		}

		// ------------------------------

		/// <summary>
		/// For serialization.
		/// </summary>
		internal L3TypeManager(
			L3TypeManagerCollection l3TypeManagerCollection, L2TypeManager l2TypeManager, int TypeIndex, int TypeIndexationStart, SerializationFormatter serializationFormatter)
			: this(l2TypeManager, TypeIndex, TypeIndexationStart)
		{
			if (l2TypeManager.SelectedFieldTypeManagers != null)
				this.SelectedFieldTypeManagers =
					l2TypeManager.SelectedFieldTypeManagers.Select(
					(l2tm) => l2tm != null ? l3TypeManagerCollection.GetTypeManager(l2tm, serializationFormatter, false, false) : null).ToArray();

			if (l2TypeManager.SelectedPropertyTypeManagers != null)
				this.SelectedPropertyTypeManagers =
					l2TypeManager.SelectedPropertyTypeManagers.Select(
						(l2tm) =>
							l2tm != null ? l3TypeManagerCollection.GetTypeManager(l2tm, serializationFormatter, false, false) : null).ToArray();

			if (l2TypeManager.CollectionItemsTypeManager != null)
				this.CollectionItemsTypeManager =
					l3TypeManagerCollection.GetTypeManager(l2TypeManager.CollectionItemsTypeManager, serializationFormatter, false, false);

			if (l2TypeManager.DictionaryKeysTypeManager != null)
				this.DictionaryKeysTypeManager =
					l3TypeManagerCollection.GetTypeManager(l2TypeManager.DictionaryKeysTypeManager, serializationFormatter, false, false);

			if (l2TypeManager.DictionaryValuesTypeManager != null)
				this.DictionaryValuesTypeManager =
					l3TypeManagerCollection.GetTypeManager(l2TypeManager.DictionaryValuesTypeManager, serializationFormatter, false, false);
		}

		// ------------------------------

#if DEBUG
		public override string ToString()
		{
			return string.Format("TypeManager #{0} {{{1}}}",
				this.TypeIndex, this.l2TypeManager.L1TypeManager.Name);
		}
#endif

		// ------------------------------
		// ------------------------------

		internal bool IsAPredefinedType
		{
			get
			{
				return this.TypeIndex >= SerializationFormatter.PredefinedTypeIndexationStart
					&& this.TypeIndex < this.TypeIndexationStart;
			}
		}

		// ------------------------------

		internal bool IsANewType
		{
			get { return this.TypeIndex >= this.TypeIndexationStart; }
		}

		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------

	}


	// ######################################################################
	// ######################################################################

	/// <summary>
	/// There is one L3TypeManagerCollection for each instance of Serializer or Deserializer.
	/// The objective is to have indexed TypeManagers. The ordre depends on de (De)Serializer.
	/// This collection depends on the CustomParameters.
	/// Because these parameters modify the way types are managed.
	/// This class is cached in order to share the TypeManager defined for a each CustomParameters.
	/// </summary>
	internal class L3TypeManagerCollection
	{
		// ------------------------------

		internal readonly L2TypeManagerCollection l2TypeManagerCollection;
		readonly Dictionary<Type, L3TypeManager> TypeManagerCache = new Dictionary<Type, L3TypeManager>(200);
		readonly Dictionary<L2TypeManager, L3TypeManager> l2TypeManagerCache = new Dictionary<L2TypeManager, L3TypeManager>(50);
		readonly List<L3TypeManager> TypeManagersOrderedList = new List<L3TypeManager>(50); // ordered by index. TODO: check if a Dictionary enumerator returns items in the addition order (if true I can remove this list and enumerate TypeManagerCache).

		//readonly L3TypeManager[] PrimitiveTypeManagers = new L3TypeManager[16];
		internal readonly L3TypeManager SerializationTypeDescriptorTM;
		internal readonly int TypeIndexationStartForThisVersion;

		// ------------------------------

		internal L3TypeManagerCollection(/*Serializer serialiazer, L2TypeManagerCollection l2TypeManagerCollection*/
			CustomModifiers customModifiers, StreamFormatVersion Version)
		{
			// Gets the source TypeManagers not-indexed collection:
			this.l2TypeManagerCollection = L2TypeManagerCollection.GetTypeManagerCollection(customModifiers);

			// Adds Primitive types:
			{
				Type[] PrimitiveTypes = new Type[16]
				{
		typeof(Object),// = 1,
		typeof(Boolean),// = 3,
		typeof(Char),// = 4,
		typeof(SByte),// = 5,
		typeof(Byte),// = 6,
		typeof(Int16),// = 7,
		typeof(UInt16),// = 8,
		typeof(Int32),// = 9,
		typeof(UInt32),// = 10,
		typeof(Int64),// = 11,
		typeof(UInt64),// = 12,
		typeof(Single),// = 13,
		typeof(Double),// = 14,
		typeof(Decimal),// = 15,
		typeof(DateTime),// = 16,
		typeof(String)// = 18,
				};

				foreach (Type t in PrimitiveTypes)
				{
					int i = (int)TypeEx.GetTypeCode(t);
					this.AddPrimitiveTypeManager(t, i);
				}
			}

			#region CompulsoryTypes
			{
				var typeIndexes = SerializationFormatter.CompulsoryTypeIndexes;
#if DEBUG
				if (typeIndexes.Length != SerializationFormatter.CompulsoryTypes.Length)
					throw new Exception();
#endif
				for (int i = 0; i < SerializationFormatter.CompulsoryTypes.Length; i++)
				{
					this.AddKnownTypeManager(
						SerializationFormatter.CompulsoryTypes[i],
						(int)typeIndexes.GetValue(i),
						null);
				}
			}
			#endregion CompulsoryTypes

			this.SerializationTypeDescriptorTM = this.TypeManagerCache[typeof(SerializationTypeDescriptor)];

			// Prepare for new types:
			{
				int index;
				switch (Version)
				{
					case StreamFormatVersion.Version2_0:
						index = SerializationFormatter.TypeIndexationStartVersion2_0;
						break;
					case StreamFormatVersion.Version3_0:
						index = SerializationFormatter.TypeIndexationStartVersion3_0;
						break;
					default:
						throw new Exception();
				}
				this.TypeIndexationStartForThisVersion = index;
				this.TypeManagersOrderedList.SetMinimalSize(index);
			}
		}

		// ------------------------------
		// ------------------------------

		internal int GetAvailableIndex()
		{
			return this.TypeManagersOrderedList.Count;
		}

		// ------------------------------

		/// <summary>
		/// Add the TypeManager if it does exist yet.
		/// For deserialization.
		/// </summary>
		internal void AddNewType(SerializationTypeDescriptor Descriptor)
		{
			this.AddTypeManager(Descriptor, false); // I don't want to duplicate my code.   ;)
		}

		// ------------------------------

		internal SerializationTypeDescriptorCollection GetSerializationTypeDescriptors()
		{
			// We create a SerializationTypeDescriptors à partir de typeManagerCollection.
			var std = new SerializationTypeDescriptorCollection();
			foreach (var td in this.TypeManagersOrderedList)
			{
				if (td != null && td.IsANewType) // We do not serialize descriptors for simple or predefined types.
					std.Add(
						td.l2TypeManager.GetDescriptor()
						);
			}
			return std;
		}

		// ------------------------------

		/// <summary>
		/// Cached.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal bool CanThisTypeBeSerialized(Type type)
		{
			return this.l2TypeManagerCollection.CanThisTypeBeSerialized(type);
		}

		// ------------------------------

		void AddPrimitiveTypeManager(Type type, int i)
		{
			var tm2 = this.l2TypeManagerCollection.GetExistingTypeManager(type);
			var tm3 = new L3TypeManager(tm2, i, this.TypeIndexationStartForThisVersion);
			this._AddTypeManager(tm3, type);
		}

		// ------------------------------

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="i"></param>
		/// <param name="serializationFormatter">Is null for deserialization.</param>
		void AddKnownTypeManager(Type type, int i, SerializationFormatter serializationFormatter)
		{
			var tm2 = this.l2TypeManagerCollection.GetExistingTypeManager(type);
			var tm3 = new L3TypeManager(this, tm2, i, this.TypeIndexationStartForThisVersion, serializationFormatter);
			this._AddTypeManager(tm3, type);
		}

		// ------------------------------

		// recursive function, but not parallel.
		/// <summary>
		/// Get TypeManager from the L2TypeManager. Creates it if necessary.
		/// For serialization.
		/// <param name="NeedsADefinedReturnValue">If false, a null can be returned if the value can not be defined yet.</param>
		/// <param name="ReserveSiteFirst">False for serialization, True for deserialization.</param>
		/// <param name="l2TypeManager"></param>
		/// <param name="serializationFormatter"></param>
		/// </summary>
		internal L3TypeManager AddTypeManager(
			L2TypeManager l2TypeManager,
			SerializationFormatter serializationFormatter,
			bool NeedsADefinedReturnValue,
			bool ReserveSiteFirst)
		{
#if DEBUG
			if (l2TypeManager == null)
				throw new Exception();
#endif

			L3TypeManager tm;
			{
				{
					if (this.BeingAnalysed.Contains(l2TypeManager))
						if (NeedsADefinedReturnValue)
#if DEBUG
							Debugger.Break(); // PB !!!!
#else
						throw new Exception();
#endif
						else
							return null; // for CheckTypeManager().
					this.BeingAnalysed.Add(l2TypeManager);
				}

				int index = -1;
				if (ReserveSiteFirst)
				{
					index = this.TypeManagersOrderedList.Count;
					this.TypeManagersOrderedList.Add(null);
				}

				tm = new L3TypeManager(this, l2TypeManager, index, this.TypeIndexationStartForThisVersion, serializationFormatter);

				{ // serializes the new type descriptor:
					if (serializationFormatter != null
						&& serializationFormatter.parameters.SerializeTypeDescriptors
							&& serializationFormatter.parameters.TheStreamingMode != StreamingModes.AssembledStream
							&& !l2TypeManager.IsAPrimitiveType)
					{
						object descr = l2TypeManager.GetDescriptor();
						serializationFormatter.AddAnObject(
							ref serializationFormatter.channelInfos[(int)SerializationFormatter.ChannelNumber.TypeDescriptorsChannel],
							descr, null,
							(int)SerializationFormatter.CompulsoryType.SerializationTypeDescriptorIndex,
							this.SerializationTypeDescriptorTM
							, true, false);
					}
				}
				this.l2TypeManagerCache.Add(l2TypeManager, tm);
				this.TypeManagerCache.Add(l2TypeManager.L1TypeManager.type, tm);

				if (ReserveSiteFirst)
					this.TypeManagersOrderedList[index] = tm;
				else
				{
					index = this.TypeManagersOrderedList.Count;
					tm.TypeIndex = index;
					this.TypeManagersOrderedList.Add(tm);
				}

				this.BeingAnalysed.Remove(l2TypeManager);
			}
			return tm;
		}
		List<L2TypeManager> BeingAnalysed = new List<L2TypeManager>();

		// ------------------------------

		void _AddTypeManager(L3TypeManager tm, Type type)
		{
			this.l2TypeManagerCache.Add(tm.l2TypeManager, tm);
			this.TypeManagerCache.Add(type, tm);
			this.TypeManagersOrderedList.SecurelySetIndex(
				tm.TypeIndex,
				tm);
		}

		// ------------------------------

		// recursive function, but not parallel.
		/// <summary>
		/// Get TypeManager from the L2TypeManager. Creates it if necessary.
		/// For serialization.
		/// <param name="NeedsADefinedReturnValue">If false, a null can be returned if the value can not be defined yet.</param>
		/// <param name="ReserveSiteFirst">False for serialization, True for deserialization.</param>
		/// <param name="l2TypeManager"></param>
		/// <param name="serializationFormatter"></param>
		/// </summary>
		internal L3TypeManager GetTypeManager(
			L2TypeManager l2TypeManager,
			SerializationFormatter serializationFormatter,
			bool NeedsADefinedReturnValue,
			bool ReserveSiteFirst)
		{
#if DEBUG
			if (l2TypeManager == null)
				throw new Exception();
#endif

			L3TypeManager tm;
			if (!this.l2TypeManagerCache.TryGetValue(l2TypeManager, out tm))
				tm = this.AddTypeManager(l2TypeManager, serializationFormatter, NeedsADefinedReturnValue, ReserveSiteFirst);
			return tm;
		}

		// ------------------------------

		/// <summary>
		/// Get TypeManager from the type. Creates it if necessary.
		/// For serialization.
		/// <param name="NeedsADefinedReturnValue">If false, a null can be returned if the value can not be defined yet.</param>
		/// <param name="ReserveSiteFirst">False for serialization, True for deserialization.</param>
		/// <param name="serializationFormatter"></param>
		/// <param name="type"></param>
		/// </summary>
		internal L3TypeManager GetTypeManager(
			Type type, SerializationFormatter serializationFormatter,
			bool NeedsADefinedReturnValue,
			bool ReserveSiteFirst)
		{
			if (type == null)
				return null;

			L3TypeManager tm;
			if (!this.TypeManagerCache.TryGetValue(type, out tm))
				tm = this.AddTypeManager(
					this.l2TypeManagerCollection.GetTypeManager(type, NeedsADefinedReturnValue, ReserveSiteFirst),
					serializationFormatter, NeedsADefinedReturnValue, ReserveSiteFirst);
			return tm;
		}

		// ------------------------------

		/// <summary>
		/// Creates a L3TypeManager from the Descriptor.
		/// For deserialization ONLY.
		/// </summary>
		internal L3TypeManager AddTypeManager(SerializationTypeDescriptor Descriptor, bool NeedsTheValue = true)
		{
			var type = Tools.GetTypeFromFullName(Descriptor.AssemblyQualifiedName);
			if (type == null)
			{
				// TODO: take versions into account (check fields, props & constructor order and existence).
				string msg = string.Format(
					ErrorMessages.GetText(6),//"Type not found: \"{0}\".",
					Descriptor.AssemblyQualifiedName);
				Log.WriteLine(msg);
				throw new Exception(msg);
			}

			{
				L3TypeManager already;
				if (this.TypeManagerCache.TryGetValue(type, out already))
					return already;
			}

			return this.AddTypeManager(
					this.l2TypeManagerCollection.GetTypeManager(type, NeedsTheValue, true),
					null, NeedsTheValue, true);

		}

		// ------------------------------

		/// <summary>
		/// For deserialization.
		/// </summary>
		internal L3TypeManager GetTypeManager(int TypeNumber)
		{
			return this.TypeManagersOrderedList[TypeNumber];
		}

		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------

	}

	// ######################################################################
	// ######################################################################

	/// <summary>
	/// L2TypeManager depends on CustomParameter and on Type. It uses L1GlobalTypeDefinition.
	/// </summary>
	internal abstract class L2TypeManager
	{
		#region For serialization AND deserialization
		internal readonly L1TypeManager L1TypeManager;
		internal readonly bool IsAPrimitiveType;
		internal bool IsClass { get { return this.L1TypeManager.IsClass; } }


		/// <summary>
		/// Only the fields we will serialize.
		/// </summary>
		internal readonly FieldInfo[] SelectedFields;

		/// <summary>
		/// Only the properties we will serialize.
		/// </summary>
		internal readonly PropertyInfo[] SelectedProperties;

		internal readonly L2TypeManager[] SelectedFieldTypeManagers;
		internal readonly L2TypeManager[] SelectedPropertyTypeManagers;

		internal readonly Func<object, object>[] SelectedFieldGetters;
		internal readonly Action<object, object>[] SelectedClassFieldSetters;
		internal readonly Func<object, object>[] SelectedPropertyGetters;
		internal readonly Action<object, object>[] SelectedClassPropertySetters;

		internal readonly Func<object> DefaultConstructor; // Depends on the CustomParameters.
		internal readonly ParametricConstructorDescriptor parametricConstructorDescriptor;
		internal readonly L2TypeManager CollectionItemsTypeManager; // type of the items of a collection or array.
		internal readonly L2TypeManager DictionaryKeysTypeManager;
		internal readonly L2TypeManager DictionaryValuesTypeManager;
		/// <summary>
		/// It is defined if the present type needs a ITypeContainer.
		/// </summary>
		internal ITypeContainer Container; // If needs a container.
		internal readonly int[] ParametricConstructorFieldParameterIndexes;
		#endregion For serialization AND deserialization

		#region For deserialization only
		/// <summary>
		/// 'null' for classes, or empty structure.
		/// Created by L2GenericTypeManager.
		/// </summary>
		internal object DefaultValue;
		#endregion For deserialization only


		// ------------------------------

		/// <summary>
		/// For serialization.
		/// Simple constructor, for Primitive types only (see System.Type.TypeCode).
		/// </summary>
		internal L2TypeManager(Type type)
		{
			this.L1TypeManager = L1TypeManagerCache.GetTypeDescription(type);
			this.IsAPrimitiveType = true;
		}

		// ------------------------------

		/// <summary>
		/// For serialization.
		/// </summary>
		internal L2TypeManager(
			L2TypeManagerCollection l2typeManagerCollection,
			Type type, CustomModifiers customModifiers,
			bool ReserveSiteFirst)
		{
			l2typeManagerCollection.TypeManagerCache.Add(type, this);

			this.L1TypeManager = L1TypeManagerCache.GetTypeDescription(type);

			//this.DefaultConstructor = new SimpleLazy<Func<object>>(() => Tools.GetNoParamConstructorWithoutCache2(type, false));

#if false//DEBUG
            if (type.FullName == "System.Windows.Input.CursorType")
                Debugger.Break();
#endif

			{
				this.IsAPrimitiveType = false;
			}

			L1TypeManager l1tm = this.L1TypeManager;

			List<FieldInfo> fields = null;
			List<L2TypeManager> fieldsTM = null;
			List<Func<object, object>> selectedFieldGetters = null;
			List<Action<object, object>> selectedClassFieldSetters = null;

			bool OnlyRegisterThisType = TypeEx.IsInterface(type) || TypeEx.IsAbstract(type) || TypeEx.IsArray(type);

			#region Finds Constructor
			if (!OnlyRegisterThisType)
			{

				// Get constructor:
				{
					bool UseTheDefaultConstructor =
										customModifiers.ForcedParametricConstructorTypes != null ?
										!customModifiers.ForcedParametricConstructorTypes.Any(t => type.Is(t))
										: true;
					if (UseTheDefaultConstructor)
					{
						if (this.IsClass)
						{

							if (customModifiers.ForcedParametricConstructorTypes == null
								|| !customModifiers.ForcedParametricConstructorTypes.Contains(type))
							{
								this.DefaultConstructor = Tools.GetNoParamConstructorWithoutCache2
									(type, _DefaultConstructorCanBeTested(type, customModifiers), _getDefaultConstructorTestCleaners
									(customModifiers));
							}
						}
						else
						{   // structures:

#if SILVERLIGHT
							if (type.IsEnum)
							{
								this.DefaultConstructor = FictionalNullableConstructor;
							}
							else
#endif
							this.DefaultConstructor =
									(l1tm.IsNullable) ?
									FictionalNullableConstructor
									: l1tm.DefaultConstructor.Value;
						}
					}

					if (this.DefaultConstructor == null
						&&
						l2typeManagerCollection.CanThisTypeBeSerialized(type))
					{
						this.parametricConstructorDescriptor =
							l1tm.parametricConstructorDescriptor.Value;
					}
				}
			}
			#endregion Finds Constructor

			if (!OnlyRegisterThisType)
			{
				this.Container = _FindITypeContainer(customModifiers);
				// Important: This container can be invalidated on serialization, therfore we continue collecting informations about this type.
			}

			if (!OnlyRegisterThisType)
			{
				if (l1tm.AsGenericIDictionary != null)
				{
					var ge = l1tm.AsGenericIDictionaryGenericArguments;
					this.DictionaryKeysTypeManager =
							(l1tm.AsGenericIDictionary == null) ?
								null
								:
								(ge.Length > 0 ?
								l2typeManagerCollection.GetTypeManager(ge[0], true, ReserveSiteFirst)
								: null);
					this.DictionaryValuesTypeManager =
							(l1tm.AsGenericIDictionary == null) ?
								null
								:
								(ge.Length > 1 ?
								l2typeManagerCollection.GetTypeManager(ge[1], true, ReserveSiteFirst)
								: null);
				}
			}

			if (!OnlyRegisterThisType || type.IsArray)
			{
				this.CollectionItemsTypeManager =
					l1tm.CollectionItemType != null ?
					l2typeManagerCollection.GetTypeManager(l1tm.CollectionItemType, false, ReserveSiteFirst)
						: null;
			}

			if (!OnlyRegisterThisType)
			{
#if CheckAllGenericParameters
				// Checks all generic parameters:
				{
					var ts = type.GetGenericArguments();
					foreach (var t in ts)
					{
						if (t != type)
						{
							var tm = l2typeManagerCollection.GetTypeManager(t, false, ReserveSiteFirst);
						}
					}
				}
#endif

				#region  Fields

				#region Public fields
				// Looks for public fields:
				if (l1tm.AllPublicFields != null && l1tm.AllPublicFields.Length != 0)
				{
					fields = new List<FieldInfo>(l1tm.AllPublicFields.Length);
					fieldsTM = new List<L2TypeManager>(l1tm.AllPublicFields.Length);
					selectedFieldGetters = new List<Func<object, object>>(l1tm.AllPublicFields.Length);
					selectedClassFieldSetters = new List<Action<object, object>>(l1tm.AllPublicFields.Length);

					for (int ifield = 0; ifield < l1tm.AllPublicFields.Length; ifield++)
					{
						var fi = l1tm.AllPublicFields[ifield];
						var ft = fi.FieldType;
						if (l2typeManagerCollection.CanThisTypeBeSerialized(ft))
						{
							fields.Add(fi);
							fieldsTM.Add(l2typeManagerCollection.GetTypeManager(ft, false, ReserveSiteFirst));
							selectedFieldGetters.Add(l1tm.AllPublicFieldGetters[ifield].Value);
							if (!l1tm.IsValueType)
								selectedClassFieldSetters.Add(l1tm.AllPublicClassFieldSetters[ifield].Value);
						}
					}
				}
				#endregion Public fields

				#region Private fields
				List<FieldInfo> AdditionalPrivateFields =
				(this.parametricConstructorDescriptor != null) ?
					// Add the parameter fields of the parametric constructor:
					new List<FieldInfo>(this.parametricConstructorDescriptor.ParameterFields
						.Where((fi) => !fi.IsPublic))
					: new List<FieldInfo>();

				{

					{   // Adds some private fields forced by CustomParameters:
						var adders = l2typeManagerCollection.customModifiers.FilterSets.Select(
							(fs) => fs.AdditionalPrivateFieldsAdder).ToArray(); // TODO: optimize by computing array once only.
						if (adders.Length > 0)
							foreach (var adder in adders)
							{
								if (adder != null)
								{
									var fieldsToAdd2 = adder(type);
									if (fieldsToAdd2 != null)
										AdditionalPrivateFields.AddRangeNoDuplicate(fieldsToAdd2);
								}
							}
					}

					{   // Adds some private fields forced by attributes (like "ForceSerializeAttribute"):
						if (this.L1TypeManager.AllPrivateFields != null)
							foreach (var fi in this.L1TypeManager.AllPrivateFields)
							{
								if (!AdditionalPrivateFields.Contains(fi))
									if (Types.MemberIsDeclaredAsForceSerializeAttribute(fi))
										AdditionalPrivateFields.Add(fi);
							}
					}

					if (AdditionalPrivateFields.Count != 0)
					{
						bool isValueType = l1tm.IsValueType;

						if (fields == null)
						{
							fields = new List<FieldInfo>(AdditionalPrivateFields.Count);
							fieldsTM = new List<L2TypeManager>(AdditionalPrivateFields.Count);
							selectedFieldGetters = new List<Func<object, object>>(AdditionalPrivateFields.Count);
							if (!isValueType)
								selectedClassFieldSetters = new List<Action<object, object>>(AdditionalPrivateFields.Count);
						}

						// Reuse the accessors created by GlobalTypeDefinition:
						for (int ifield = 0; ifield < l1tm.AllPrivateFields.Length; ifield++)
						{
							var fi = l1tm.AllPrivateFields[ifield];
							if (AdditionalPrivateFields.Contains(fi))
								if (l2typeManagerCollection.CanThisTypeBeSerialized(fi.FieldType))
								{
									fields.Add(fi);
									fieldsTM.Add(l2typeManagerCollection.GetTypeManager(fi.FieldType, false, ReserveSiteFirst));
									selectedFieldGetters.Add(l1tm.AllPrivateFieldGetters[ifield].Value);
									if (!isValueType)
										selectedClassFieldSetters.Add(l1tm.AllPrivateClassFieldSetters[ifield].Value);
								}
						}
#if false//DEBUG
						if (fields.Count != AdditionalPrivateFields.Count)
							throw new Exception();
#endif
					}
				}
				#endregion Private fields


				// Now forces the computation now, in order to get the right indexes:
				if (fields != null)
				{
					this.SelectedFields = fields.ToArray();
					this.SelectedFieldGetters = selectedFieldGetters.ToArray();
					this.SelectedClassFieldSetters =
						selectedClassFieldSetters != null ?
						selectedClassFieldSetters.ToArray() : null;
					this.SelectedFieldTypeManagers = fieldsTM.ToArray();
				}
				#endregion  Fields

				#region Properties
				// Look for the writable properties.
				if (l1tm.AllProperties != null && l1tm.AllProperties.Length != 0)
				{
					List<PropertyInfo> selectedProperties = new List<PropertyInfo>(l1tm.AllProperties.Length);
					List<L2TypeManager> selectedPropertyTypeManagers = new List<L2TypeManager>(l1tm.AllProperties.Length);
					List<Func<object, object>> selectedPropertyGetters = new List<Func<object, object>>(l1tm.AllProperties.Length);
					List<Action<object, object>> selectedClassPropertySetters = new List<Action<object, object>>(l1tm.AllProperties.Length);

					for (int iprop = 0; iprop < l1tm.AllProperties.Length; iprop++)
					{
						var pi = l1tm.AllProperties[iprop];
						if (l2typeManagerCollection.CanThisTypeBeSerialized(pi.PropertyType))
						{
							selectedProperties.Add(pi);

							{
								// Check if all property types are analysed yet:
								// important: new types are serialized before the current one.
								selectedPropertyTypeManagers.Add(
									l2typeManagerCollection.GetTypeManager(pi.PropertyType, false, ReserveSiteFirst));

								selectedPropertyGetters.Add(
									Tools.GeneratePropertyGetter(type, pi.PropertyType, pi));

								if (!l1tm.IsValueType)
									selectedClassPropertySetters.Add(
										Tools.GenerateReferenceTypePropertySetter(pi));
							}
						}
					}
					if (selectedPropertyTypeManagers.Count > 0)
					{
						this.SelectedProperties = selectedProperties.ToArray();
						this.SelectedPropertyTypeManagers = selectedPropertyTypeManagers.ToArray();
						this.SelectedPropertyGetters = selectedPropertyGetters.ToArray();
						this.SelectedClassPropertySetters = selectedClassPropertySetters.ToArray();
					}
				}
				#endregion Properties


				{   // Now that selected fields and properties are known, we can use them as parameters for the parametric constructor:
					if (this.parametricConstructorDescriptor != null)
						this.ParametricConstructorFieldParameterIndexes =
							this.parametricConstructorDescriptor.GetParameterIndexes(this.SelectedFields, this.Container != null);
				}


#if false // Until now, I do not add private properties.
			if (!OnlyRegisterThisType)
			{
				var pcd = this.parametricConstructorDescriptor;
				if (pcd != null)
				// Add private properties for parametric constructor:
				{
					foreach (var field in pcd.Fields)
					{
						if (!fields.Contains(field) && typeManagerCollection.CanThisTypeBeSerialized(field.FieldType))
						{
							fields.Add(field);
							fieldsTM.Add(typeManagerCollection.GetTypeManager(field.FieldType, serializationFormatter, false, ReserveSiteFirst));
						}
					}
				}
#endif
			}
		}

		// ------------------------------

		bool _DefaultConstructorCanBeTested(Type type, CustomModifiers customModifiers)
		{
			foreach (var fs in customModifiers.FilterSets)
			{
				if (fs.CanTestDefaultConstructor != null && !fs.CanTestDefaultConstructor(type))
					return false;
			}
			return true;
		}

		// ------------------------------

		// TODO: put in cache.
		DefaultConstructorTestCleanerDelegate[] _getDefaultConstructorTestCleaners(CustomModifiers customModifiers)
		{
			List<DefaultConstructorTestCleanerDelegate> ret = new List<DefaultConstructorTestCleanerDelegate>();
			foreach (var fs in customModifiers.FilterSets)
			{
				if (fs.DefaultConstructorTestCleaner != null)
					ret.Add(fs.DefaultConstructorTestCleaner);
			}
			if (ret.Count == 0)
				return null;
			return ret.ToArray();
		}

		// ------------------------------

		internal abstract object ConstructNewObjectAndSetMembers(
				L3TypeManager l3TypeManager,
			DeserializationFormatter deserializationFormatter, bool AddToTheInstances, long? NumberOfElements);

		// ------------------------------


		static internal object FictionalNullableConstructor()
		{
			return null;
		}

		// ------------------------------

		ITypeContainer _FindITypeContainer(CustomModifiers customModifiers)
		{
			var gtd = this.L1TypeManager;

			if (!gtd.type.IsArray)
			{
				bool HasAValidConstructor =
					this.DefaultConstructor != null
					|| this.parametricConstructorDescriptor != null; // Additional condition over version 1.
				{
					foreach (ITypeContainer container in customModifiers.Containers)
					{
						if ((!HasAValidConstructor) || (container.ApplyEvenIfThereIsAValidConstructor))
							if (gtd.IsClass || (gtd.IsValueType && container.ApplyToStructures))
							{
								bool usable = container.IsValidType(gtd.type);
								if (usable)
								{
									return container;
								}
							}
					}
				}
			}
			return null;
		}

		// ------------------------------

#if DEBUG
		public override string ToString()
		{
			return "TypeManager {" + this.L1TypeManager.Name + "}";
		}
#endif

		// ------------------------------

		internal SerializationTypeDescriptor GetDescriptor()
		{
			return new SerializationTypeDescriptor()
			{
				AssemblyQualifiedName = this.L1TypeManager.type.AssemblyQualifiedName,
				FieldNames = this.SelectedFields != null ? this.SelectedFields.Select((fi) => fi.Name).ToArray() : null,
				PropertyNames = this.SelectedProperties != null ? this.SelectedProperties.Select((pi) => pi.Name).ToArray() : null,
				FieldAndPropertyNamesForConstructorParameters =
						(this.parametricConstructorDescriptor != null ?
						this.parametricConstructorDescriptor.ParameterFields.Select((fi) => fi.Name).ToArray()
						: null)
			};
		}

		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------

	}

	// ######################################################################
	// ######################################################################

	/// <summary>
	/// This generic TypeManager has more possibilities.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class L2GenericTypeManager<T> : L2TypeManager
	{

		internal readonly Tools.ValueTypeObjectMemberSetterDelegate<T>[] SelectedStructureFieldSetters;
		internal readonly Tools.ValueTypeObjectMemberSetterDelegate<T>[] SelectedStructurePropertySetters;

		// ------------------------------

		/// <summary>
		/// For primitive types.
		/// </summary>
		/// <param name="type"></param>
		public L2GenericTypeManager(
			Type type)
			: base(type)
		{
			base.DefaultValue = default(T);
		}

		// ------------------------------

		public L2GenericTypeManager(
			L2TypeManagerCollection l2typeManagerCollection,
			Type type, CustomModifiers customModifiers,
			bool ReserveSiteFirst)
			: base(l2typeManagerCollection, type, customModifiers, ReserveSiteFirst)
		{
			base.DefaultValue = default(T);

			if (this.L1TypeManager.IsValueType)
			{
				if (this.SelectedFields != null)
					this.SelectedStructureFieldSetters = this.SelectedFields.Select(
						(fi) => Tools.GenerateValueTypeFieldSetter<T>(fi)).ToArray();
				if (this.SelectedProperties != null)
					this.SelectedStructurePropertySetters = this.SelectedProperties.Select(
						(fi) => Tools.GenerateValueTypePropertySetter<T>(fi)).ToArray();
			}
		}

		// ------------------------------

		/// <summary>
		/// For primitive types
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static L2TypeManager CreateGenericTypeManager(Type type)
		{
			Type typed = L2GenericTypeManagerTypeDefinition.MakeGenericType(new Type[] { type });
#if DEBUG
			// using Linq Expression (slower at first call but debugging is much easier):
			var c =
				Tools.Param1ConstructorToDelegate<Type, L2TypeManager>(
					typed.GetConstructor(
						new Type[1] { typeof(Type) }));
			return c(type);
#else
			// using reflection:
			return (L2TypeManager)Activator.CreateInstance(typed, 
				new object[] { type });
#endif
		}

		// ------------------------------


		internal static L2TypeManager CreateGenericTypeManager(
			L2TypeManagerCollection l2typeManagerCollection,
			Type type,
			CustomModifiers customModifiers,
			bool ReserveSiteFirst)
		{
			try
			{
				Type typed = L2GenericTypeManagerTypeDefinition.MakeGenericType(new Type[] { type });

#if DEBUG || SILVERLIGHT
				// using Linq Expression (slower at first call but debugging is much easier):
				var ci = typed.GetConstructor(new Type[4] {
				typeof(L2TypeManagerCollection), typeof(Type), typeof(CustomModifiers), typeof(bool)});
				var d = Tools.Param4ConstructorToDelegate<
					L2TypeManagerCollection, Type, CustomModifiers, bool, L2TypeManager>(ci);
				var ret = d(l2typeManagerCollection, type, customModifiers, ReserveSiteFirst);
				return ret;
#else
			// using reflection:
			return (L2TypeManager)Activator.CreateInstance(typed,
				new object[] { l2typeManagerCollection, type, customModifiers, ReserveSiteFirst });
#endif
			}
			catch (Exception e)
			{
				string msg;
#if SILVERLIGHT
                if (e is System.MethodAccessException && type.IsNotPublic)
                    msg = string.Format(
                        ErrorMessages.GetText(21), // "Silverlight does not allow creation of a TypeManager for private type {0}."
                        type.FullName);
                else
#endif
				msg = string.Format(
					ErrorMessages.GetText(20), // "(internal) Can not create a TypeManager for type {0}. Please contact me."
					type.FullName);
				throw new Exception(msg, e);
			}
		}

		// ------------------------------

		static Type L2GenericTypeManagerTypeDefinition = typeof(L2GenericTypeManager<int>).GetGenericTypeDefinition();

		// ------------------------------

		internal void SetStructureField(int FieldIndex, ref T obj, object Value)
		{
			var setter = this.SelectedStructureFieldSetters[FieldIndex];
			setter(ref obj, Value);
		}

		internal void SetStructureProperty(int PropertyIndex, ref T obj, object Value)
		{
			var setter = this.SelectedStructurePropertySetters[PropertyIndex];
			setter(ref obj, Value);
		}

		// ------------------------------

		internal void SetFieldValues(ref T obj, object[] fieldValues)
		{
			bool IsValueType = this.L1TypeManager.IsValueType;

			if (this.SelectedFields != null)
			{
				for (int ifi = 0; ifi < this.SelectedFields.Length; ifi++)
				{
					var fi = this.SelectedFields[ifi];

					object fieldValue = fieldValues[ifi];

					// TODO: get default values from the 'this'.
					object defaultValue =  // Value created by the type constructor.
						this.SelectedFieldGetters[ifi](obj);

					if (defaultValue != fieldValue)
					{
						try
						{
							if (IsValueType) // structure
							{
								var setter = this.SelectedStructureFieldSetters[ifi];
								if (setter != null)
								{
#if NET3_5

									bool err = false;
                                    try
                                    {
                                        setter(ref obj, fieldValue);
                                    }
                                    catch (MethodAccessException)
                                    {
                                        err = true;
                                    }
                                    if (err)
                                        fi.SetValueDirect(__makeref(obj), fieldValue); // reflexion method: slow.
#else
									//fi.SetValue(obj, fieldValue); // reflexion method: does not return the object.
									/*if (typeof(T) != obj.GetType())
										throw new Exception();
									if (fieldValue.GetType()!=this.SelectedFields[ifi].FieldType)
										throw new Exception();*/
									setter(ref obj, fieldValue);
#endif
									continue;
								}
							}
							else // class
							{
								var setter = this.SelectedClassFieldSetters[ifi];
								if (setter != null)
								{
									setter(obj, fieldValue);
									continue;
								}
							}
							fi.SetValue(obj, fieldValue); // reflexion method: slow.
						}
						catch (Exception ex)
						{
							throw new Exception(
								string.Format(
									ErrorMessages.GetText(4), // "Can not set field '{0}'.'{1}' of type '{2}' with value '{3}' of type '{4}'",
									this.L1TypeManager.type.GetName(),
									fi.Name,
									fi.FieldType.GetName(),
									fieldValue != null ? fieldValue.ToString() : "<null>",
									fieldValue != null ? fieldValue.GetType().GetName() : "<null>"
								), ex);
						}
					}
				}
			}
		}

		// ------------------------------

		internal void SetPropertyValues(ref T obj, object[] PropertyValues)
		{
			bool IsValueType = this.L1TypeManager.IsValueType;

			if (this.SelectedProperties != null)
				for (int ipi = 0; ipi < this.SelectedProperties.Length; ipi++)
				{
					var pi = this.SelectedProperties[ipi];
					object propValue = PropertyValues[ipi];

					object currentValue =  // Value created by the type constructor.
						this.SelectedPropertyGetters[ipi](obj);

					if (currentValue != propValue)
					{
						try
						{
							if (IsValueType) // structure
							{
								var setter = this.SelectedStructurePropertySetters[ipi];
								if (setter != null)
								{
#if !PORTABLE
									setter(ref obj, propValue);
#else
									try
									{
										setter(ref obj, propValue);
									}
									catch (Exception ex)
									{
										if (ex is System.Security.VerificationException)
										{
											// For some (unjustified) security reason, the portable framework layer can cause this exception.
											// Example: System.Windows.Size, property Width.

											// TODO: find a solution !
											// Because the following SetValue do not set the property on OUR obj.

											pi.SetValue(obj, propValue, null); // does not work, because only changes a copy.
#if true
											if (!pi.GetValue(obj, null).Equals(propValue))
											{
#if DEBUG
												Debugger.Break(); // Big problem.
#endif
												throw;
											}
#endif

											this.SelectedStructurePropertySetters[ipi] = null; // optimisation: we eliminate this possibility now.
										}
										else
											throw ex;
									}
#endif
									continue;
								}
							}
							else // class
							{
								var setter = this.SelectedClassPropertySetters[ipi];
								if (setter != null)
								{
									setter(obj, propValue);
									continue;
								}
							}
							pi.SetValue(obj, propValue, null); // reflexion method: slow.
						}
						catch (Exception ex)
						{
							throw new Exception(
								string.Format(
									ErrorMessages.GetText(5),//"Can not set property '{0}'.'{1}' of type '{2}' with value '{3}' of type '{4}'",
									this.L1TypeManager.type.GetName(),
									pi.Name,
									pi.PropertyType.GetName(),
									propValue != null ? propValue.ToString() : "<null>",
									propValue != null ? propValue.GetType().GetName() : "<null>"
								), ex);
						}
					}
				}
		}

		// ------------------------------

		/// <summary>
		/// Only calls deserializationFormatter.ConstructAndSetMembers&lt;T&gt;().
		/// It is only an optimization.
		/// </summary>
		/// <param name="deserializationFormatter"></param>
		/// <param name="AddToTheInstances"></param>
		/// <param name="NumberOfElements"></param>
		/// <param name="l3TypeManager"></param>
		/// <returns></returns>
		internal override object ConstructNewObjectAndSetMembers(
				L3TypeManager l3TypeManager,
			DeserializationFormatter deserializationFormatter, bool AddToTheInstances, long? NumberOfElements)
		{
			return deserializationFormatter.ConstructAndSetMembers<T>(l3TypeManager, AddToTheInstances, NumberOfElements);
		}


		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
	}

	// ######################################################################
	// ######################################################################

	/// <summary>
	/// There is one instance of L2TypeManagerCollection for each instance of CustomParameters.
	/// Because these parameters modify the way (contained, filtered) types are managed.
	/// This class is cached in order to share the TypeManager defined for a each CustomParameters.
	/// </summary>
	internal class L2TypeManagerCollection
	{
		// ------------------------------

		internal readonly CustomModifiers customModifiers;
		internal readonly Dictionary<Type, L2TypeManager> TypeManagerCache = new Dictionary<Type, L2TypeManager>(200);

		// ------------------------------

		L2TypeManagerCollection(CustomModifiers customModifiers)
		{
			this.customModifiers = customModifiers;

			// Add Primitive types:
			{
				Type[] PrimitiveTypes = new Type[16]
				{
		typeof(Object),// = 1,
		typeof(Boolean),// = 3,
		typeof(Char),// = 4,
		typeof(SByte),// = 5,
		typeof(Byte),// = 6,
		typeof(Int16),// = 7,
		typeof(UInt16),// = 8,
		typeof(Int32),// = 9,
		typeof(UInt32),// = 10,
		typeof(Int64),// = 11,
		typeof(UInt64),// = 12,
		typeof(Single),// = 13,
		typeof(Double),// = 14,
		typeof(Decimal),// = 15,
		typeof(DateTime),// = 16,
		typeof(String)// = 18,
				};

				foreach (Type t in PrimitiveTypes)
				{
					this.AddPrimitiveTypeManager(t);
				}
			}

			#region CompulsoryTypes
			{
				foreach (var type in SerializationFormatter.CompulsoryTypes)
				{
					this.AddTypeManager(type, null);
				}
			}
			#endregion CompulsoryTypes
		}

		// ------------------------------

		/// <summary>
		/// Cached along customModifiers.
		/// </summary>
		/// <param name="customModifiers"></param>
		/// <returns></returns>
		internal static L2TypeManagerCollection GetTypeManagerCollection(CustomModifiers customModifiers)
		{
			L2TypeManagerCollection ret;
			if (!_TypeManagerCollectionCache.TryGetValue(customModifiers, out ret))
			{
				ret = new L2TypeManagerCollection(customModifiers);

				_TypeManagerCollectionCache.Add(customModifiers, ret);
			}
			return ret;
		}
		static FrequencyOrderedLimitedSizeDict<CustomModifiers, L2TypeManagerCollection> _TypeManagerCollectionCache =
			new FrequencyOrderedLimitedSizeDict<CustomModifiers, L2TypeManagerCollection>(8);

		// ------------------------------

		/// <summary>
		/// Cached.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal bool CanThisTypeBeSerialized(Type type)
		{
			bool ret;
			if (!this._ThisTypeCanBeSerializedCache.TryGetValue(type, out ret))
			{
				ret = this._AnalyseIfCanBeSerialized(type, this.customModifiers);
				this._ThisTypeCanBeSerializedCache.Add(type, ret);
			}
			return ret;
		}
		Dictionary<Type, bool> _ThisTypeCanBeSerializedCache = new Dictionary<Type, bool>();

		bool _AnalyseIfCanBeSerialized(Type type, CustomModifiers customModifiers)
		{
			foreach (var fs in customModifiers.FilterSets)
			{
				if (fs.TypeSerializationValidator != null && !fs.TypeSerializationValidator(type))
					return false;
			}
			return true;
		}

		// ------------------------------

		void AddPrimitiveTypeManager(Type type)
		{
			var tm = L2GenericTypeManager<int>.CreateGenericTypeManager(
				type);
			//this._AddTypeManager(tm, type);
			this.TypeManagerCache.Add(type, tm);
		}

		// ------------------------------

		void AddTypeManager(Type type, SerializationFormatter serializationFormatter)
		{
			var tm = L2GenericTypeManager<int>.CreateGenericTypeManager(
				this,
				type, this.customModifiers,
				false);
			//this._AddTypeManager(tm, type);
		}

		// ------------------------------

		/*void _AddTypeManager(L2TypeManager tm, Type type)
        {
            if (!this.TypeManagerCache.ContainsKey(type))
                this.TypeManagerCache.Add(type, tm);
        }*/

		// ------------------------------

		/// <summary>
		/// For L3TypeManagerCollection.
		/// Just a simple access to the TypeManager list.
		/// </summary>
		internal L2TypeManager GetExistingTypeManager(
			Type type)
		{
			return this.TypeManagerCache[type];
		}

		// ------------------------------


		// recursive function, but not parallel.
		/// <summary>
		/// Get TypeManager from the type. Creates it if necessary.
		/// For serialization.
		/// <param name="NeedsADefinedReturnValue">If false, a null can be returned if the value can not be defined yet.</param>
		/// <param name="ReserveSiteFirst"></param>
		/// <param name="type"></param>
		/// </summary>
		internal L2TypeManager GetTypeManager(
			Type type,
			bool NeedsADefinedReturnValue,
			bool ReserveSiteFirst)
		{
			if (type == null)
				return null;

			L2TypeManager tm;
			if (!this.TypeManagerCache.TryGetValue(type, out tm))
			{
				if (this.BeingAnalysed.Contains(type))
					if (NeedsADefinedReturnValue)
#if DEBUG
						Debugger.Break(); // PB !!!!
#else
						throw new Exception();
#endif
					else
						return null; // for CheckTypeManager().
				this.BeingAnalysed.Add(type);

				tm = L2GenericTypeManager<int>.CreateGenericTypeManager(
					this,
					type, this.customModifiers,
					ReserveSiteFirst);

				/*if (!this.TypeManagerCache.ContainsKey(type))
                    this.TypeManagerCache.Add(type, tm);*/

				this.BeingAnalysed.Remove(type);
			}
			return tm;

		}
		List<Type> BeingAnalysed = new List<Type>();

		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------
		// ------------------------------

	}

	// ######################################################################
	// ######################################################################

	/// <summary>
	/// Level 1: depends on Type.
	/// For optimisation reasons, a type description does not depend on anything but the type itself.
	/// Some informations are SimpleLazy because they are not always needed and Linq Expression compiler is slow to give their content.
	/// </summary>
	internal class L1TypeManager
	{
		#region infos
		internal readonly Type type;
		internal readonly bool IsNullable;
		internal readonly bool IsClass;
		internal readonly bool IsStructure;

		internal readonly FieldInfo[] AllPublicFields;
		internal readonly FieldInfo[] AllPrivateFields;
		internal readonly PropertyInfo[] AllProperties;
		internal readonly SimpleLazy<Func<object, object>>[] AllPublicFieldGetters;
		internal readonly SimpleLazy<Func<object, object>>[] AllPrivateFieldGetters;
		/// <summary>
		/// Limited to classes for now.
		/// Readonly (InitOnly) fields have null.
		/// </summary>
		internal readonly SimpleLazy<Action<object, object>>[] AllPublicClassFieldSetters;
		internal readonly SimpleLazy<Action<object, object>>[] AllPrivateClassFieldSetters;
		internal readonly SimpleLazy<Func<object, object>>[] AllPropertyGetters;
		/// <summary>
		/// Limited to classes for now.
		/// </summary>
		internal readonly SimpleLazy<Action<object, object>>[] AllClassPropertySetters;
		internal readonly bool IsAnObjectIEnumerable; // not-generic IEnumerable, but not a dictionary.
		internal readonly bool IsADictionary; // a generic or an object dictionary.
#if DEBUG
		internal readonly string Name;
#endif
		internal readonly bool IsValueType; // cache.
		internal readonly Type AsGenericIDictionary;
		internal readonly Type[] AsGenericIDictionaryGenericArguments;
		internal readonly Type CollectionItemType;
		internal readonly Type[] GenericArguments;
		internal readonly SimpleLazy<Func<object>> DefaultConstructor;
		internal readonly SimpleLazy<ParametricConstructorDescriptor> parametricConstructorDescriptor;
		#endregion infos

		// ------------------------------

		internal L1TypeManager(Type type)
		{
			this.type = type;
#if DEBUG
			this.Name = type.GetName();
#endif
			this.IsValueType = TypeEx.IsValueType(type);
			this.IsNullable = type.Is(typeof(Nullable<>));
			this.IsClass = TypeEx.IsClass(type) && !type.IsPointer;
			this.IsStructure = !this.IsClass && !this.IsNullable && !TypeEx.IsEnum(type) && !TypeEx.IsInterface(type);

			bool OnlyRegisterThisType = TypeEx.IsInterface(type) || TypeEx.IsAbstract(type) || type.IsArray;

			#region Finds Constructor
			if (!this.IsClass)
			/*this.DefaultConstructor = new SimpleLazy<Func<object>>(() => Tools.GetNoParamConstructorWithoutCache2(type, false));
        else*/
			{   // structures:

#if SILVERLIGHT
							if (type.IsEnum)
							{
								this.DefaultConstructor = new SimpleLazy<Func<object>>(()=>{return L2TypeManager.FictionalNullableConstructor;});
							}
							else
#endif
				this.DefaultConstructor = new SimpleLazy<Func<object>>(() =>
#if WINDOWS_UWP
					(Func<object>)StructureConstructorMethod.MakeGenericMethod(type).CreateDelegate(FuncObjectType));
#else
					(Func<object>)Delegate.CreateDelegate(FuncObjectType, StructureConstructorMethod.MakeGenericMethod(type)));
#endif
			}

			this.parametricConstructorDescriptor = new SimpleLazy<ParametricConstructorDescriptor>(() =>
				ParametricConstructorsManager.GetTypeParamDescriptorFromCache(type));

#endregion Finds Constructor

			if (!OnlyRegisterThisType)
			{
				Type AsIDictionary = type.FindDerivedOrEqualToThisType(typeof(IDictionary));
				this.AsGenericIDictionary = type.FindDerivedOrEqualToThisType(typeof(IDictionary<,>));
				this.IsADictionary = AsIDictionary != null || AsGenericIDictionary != null;

				if (this.AsGenericIDictionary != null)
				{
					this.AsGenericIDictionaryGenericArguments = AsGenericIDictionary.GetGenericArguments();
				}
			}

			if (!OnlyRegisterThisType || type.IsArray)
			{
				Type AsIEnumerable = type.FindDerivedOrEqualToThisType(typeof(IEnumerable));
				this.IsAnObjectIEnumerable = !this.IsADictionary
					&& AsIEnumerable != null;

				this.CollectionItemType = Tools.GetCollectionItemType(type);
			}

			if (!OnlyRegisterThisType)
			{
				// Checks all generic parameters:
				{
					this.GenericArguments = type.GetGenericArguments();
				}

				// Looks for the writable fields:
				this.AllPublicFields =
					type.GetFields(BindingFlags.Instance | BindingFlags.Public)
					.Where((fi) => /*!fi.IsInitOnly &&*/
						Types.MemberIsDeclaredAsForceSerializeAttribute(fi)
						|| (
							!Types.MemberIsDeclaredAsForceNotSerializeAttribute(fi)
							&& !Types.MemberIsDeclaredAsNotSerializableByXmlIgnoreAttribute(fi)
							&& !Types.MemberIsDeclaredAsNotEditorBrowsable(fi)
							&& !Types.FieldIsDeclaredAsNotSerializableByNonSerializedAttribute(fi)
						))
					.ToArray();
				if (this.AllPublicFields.Length == 0)
					this.AllPublicFields = null;

				this.AllPrivateFields =
					Types.GetPrivateFieldsIncludingInherited(type)
					.Where((fi) =>
						Types.MemberIsDeclaredAsForceSerializeAttribute(fi)
						|| (
							!Types.MemberIsDeclaredAsForceNotSerializeAttribute(fi)
							&& !Types.MemberIsDeclaredAsNotSerializableByXmlIgnoreAttribute(fi)
							&& !Types.MemberIsDeclaredAsNotEditorBrowsable(fi)
							&& !Types.FieldIsDeclaredAsNotSerializableByNonSerializedAttribute(fi)
						))
						.ToArray();
				if (this.AllPrivateFields.Length == 0)
					this.AllPrivateFields = null;

				// Look for the writable properties.
				{
					{
						var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
						var publicProps = props.Where(
							(pi) =>
								pi.CanWrite
								&& pi.CanRead
								&& pi.GetIndexParameters().Length == 0
								&& (
									Types.MemberIsDeclaredAsForceSerializeAttribute(pi)
										|| (
											!Types.MemberIsDeclaredAsForceNotSerializeAttribute(pi)
											&& !Types.MemberIsDeclaredAsNotSerializableByXmlIgnoreAttribute(pi)
											&& !Types.MemberIsDeclaredAsNotEditorBrowsable(pi))
							));
						props = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);
						var privateProps = props.Where(
							(pi) =>
								pi.CanWrite
								&& pi.CanRead
								&& pi.GetIndexParameters().Length == 0
								&& (
									Types.MemberIsDeclaredAsForceSerializeAttribute(pi)
										&& (
											!Types.MemberIsDeclaredAsForceNotSerializeAttribute(pi)
											//&& !Types.MemberIsDeclaredAsNotSerializableByXmlIgnoreAttribute(pi)
											//&& !Types.MemberIsDeclaredAsNotEditorBrowsable(pi))
											)
							));
						this.AllProperties = publicProps.Concat(privateProps).ToArray();
					}
					if (this.AllProperties.Length > 0)
					{
						// Check if all property types are analysed yet:
						// important: new types are serialized before the current one.
						this.AllPropertyGetters = AllProperties.Select(
							(pi) => new SimpleLazy<Func<object, object>>(() =>
								Tools.GeneratePropertyGetter(type, pi.PropertyType, pi))).ToArray();
						if (!this.IsValueType)
							this.AllClassPropertySetters = AllProperties.Select(
								(pi) => new SimpleLazy<Action<object, object>>(() =>
									Tools.GenerateReferenceTypePropertySetter(pi))).ToArray();
					}
					else
					{
						this.AllProperties = null; // optimisation.
					}
				}
			}

			if (this.AllPrivateFields != null && this.AllPrivateFields.Length != 0)
			{
				this.AllPrivateFieldGetters = this.AllPrivateFields.Select(
					(fi) => new SimpleLazy<Func<object, object>>(() =>
						Tools.GenerateFieldGetter(type, fi.FieldType, fi))).ToArray();
				if (!this.IsValueType)
					this.AllPrivateClassFieldSetters = this.AllPrivateFields.Select(
					(fi) => new SimpleLazy<Action<object, object>>(() =>
						Tools.GenerateReferenceTypeFieldSetter(fi))).ToArray();
			}

			if (this.AllPublicFields != null && this.AllPublicFields.Length != 0)
			{
				this.AllPublicFieldGetters = this.AllPublicFields.Select(
					(fi) => new SimpleLazy<Func<object, object>>(() =>
						Tools.GenerateFieldGetter(type, fi.FieldType, fi))).ToArray();
				if (!this.IsValueType)
					this.AllPublicClassFieldSetters = this.AllPublicFields.Select(
					(fi) => new SimpleLazy<Action<object, object>>(() =>
						Tools.GenerateReferenceTypeFieldSetter(fi))).ToArray();
			}
		}

		// ------------------------------

		static readonly Type FuncObjectType = typeof(Func<object>);

		// ------------------------------

		static object StructureConstructor<T>() where T : struct
		{
			return new T();
		}
		static readonly MethodInfo StructureConstructorMethod =
#if WINDOWS_UWP
			((Func<object>)(StructureConstructor<int>)).GetMethodInfo().GetGenericMethodDefinition();
#else
			((Func<object>)(StructureConstructor<int>)).Method.GetGenericMethodDefinition();
#endif

		// ------------------------------

#if DEBUG
		public override string ToString()
		{
			return string.Format("{0} for {1}", typeof(L1TypeManager).Name, this.type.Name);
		}
#endif
	}


	// ######################################################################
	// ######################################################################

	/// <summary>
	/// This cache is valid between serialisations.
	/// It is necessary because Linq Expression compilation is extremely slow.
	/// </summary>
	internal static class L1TypeManagerCache
	{
		static Dictionary<Type, L1TypeManager> TypeDescriptions = new Dictionary<Type, L1TypeManager>();

		internal static L1TypeManager GetTypeDescription(Type type)
		{
			L1TypeManager td;
			if (!TypeDescriptions.TryGetValue(type, out td))
			{
				td = new L1TypeManager(type);
				TypeDescriptions.Add(type, td);
			}
			return td;
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