
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Reflection;
using UniversalSerializerLib3.DataTools;
using System.Linq;

namespace UniversalSerializerLib3
{
	// #############################################################################
	// #############################################################################

	/// <summary>
	/// Returns 'false' if this type should not be serialized at all.
	/// That will let the default value created by the constructor of its container class/structure.
	/// </summary>
	public delegate bool TypeSerializationValidatorDelegate(Type t);

	/// <summary>
	/// Returns 'false' if the default constructor of this type should not be tested by instanciation.
	/// Default value is 'true'.
	/// Serializer instanciates the default constructor (when available) once to ensure it does not throw an exception. Unfortunately, that instanciation can lead to problems. Example: each WPF Window have to be closed properly before closing the application.
	/// </summary>
	public delegate bool CanTestDefaultConstructorDelegate(Type t);

	/// <summary>
	/// Cleans a type instance made by the default constructor.
	/// Serializer instanciates the default constructor (when available) once to ensure it does not throw an exception. Unfortunately, that instanciation can be registred in static lists. Example: each WPF Window have to be closed properly before closing the application.
	/// Returns true if the instance has been cleaned.
	/// </summary>
	public delegate bool DefaultConstructorTestCleanerDelegate(object Instance);

	/// <summary>
	/// Tells the serializer to add some certain private fields to store the type.
	/// </summary>
	public delegate FieldInfo[] AdditionalPrivateFieldsAdderDelegate(Type t);

	// #############################################################################
	// #############################################################################

	/// <summary>
	/// Defines ITypeContainers and FilterSets.
	/// </summary>
	public class CustomModifiers
	{
		/// <summary>
		/// Type containers array.
		/// </summary>
		public readonly ITypeContainer[] Containers;
		/// <summary>
		/// Type filter sets array;
		/// </summary>
		public readonly FilterSet[] FilterSets;

		/// <summary>
		/// When a type is in this list, the serializer ignores its default constructor and calls the best parametric constructor available.
		/// </summary>
		public readonly Type[] ForcedParametricConstructorTypes;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="Containers"></param>
		/// <param name="FilterSets"></param>
		/// <param name="ForcedParametricConstructorTypes"></param>
		public CustomModifiers(
			ITypeContainer[] Containers = null,
			FilterSet[] FilterSets = null,
			Type[] ForcedParametricConstructorTypes = null)
		{
			this.Containers = Containers;
			this.FilterSets = FilterSets;
			this.ForcedParametricConstructorTypes = ForcedParametricConstructorTypes;
		}

		// --------------

		/// <summary>
		/// One instance facilitates combinations caching.
		/// </summary>
		internal static CustomModifiers Empty = new CustomModifiers_Empty();

		// --------------

		/// <summary>
		/// Combines two sets of modifers.
		/// The current set has priority over the second one.
		/// This function is cached in order to give only one instance for each possible combination. That allows optimizations in the rest of the library.
		/// </summary>
		internal CustomModifiers GetCombinationWithOtherCustomModifiers(CustomModifiers customParameters2)
		{
			return CustomModifiers.GetCombinationOfTwoCustomModifiers(this, customParameters2);
		}

		/// <summary>
		/// Combines two sets of modifers.
		/// The first set has priority over the second one.
		/// This function is cached in order to give only one instance for each possible combination. That allows optimizations in the rest of the library.
		/// </summary>
		internal static CustomModifiers GetCombinationOfTwoCustomModifiers(
				CustomModifiers customParameters1, CustomModifiers customParameters2)
		{
			CustomModifiers ret;
			var k = new Data.ComparableKeyValuePair<CustomModifiers, CustomModifiers>(customParameters1, customParameters2);

			if (!_CombineTwoCustomParametersCache.TryGetValue(k, out ret))
			{
				ret = _CombineTwoCustomModifiers(customParameters1, customParameters2);
				_CombineTwoCustomParametersCache.Add(k, ret);
			}
			return ret;
		}
		static DataTools.FrequencyOrderedLimitedSizeDict<Data.ComparableKeyValuePair<CustomModifiers, CustomModifiers>, CustomModifiers> _CombineTwoCustomParametersCache =
			new DataTools.FrequencyOrderedLimitedSizeDict<Data.ComparableKeyValuePair<CustomModifiers, CustomModifiers>, CustomModifiers>(8);

		/// <summary>
		/// Combines two sets of parameters.
		/// The first set has priority over the second one.
		/// </summary>
		static CustomModifiers _CombineTwoCustomModifiers(
				CustomModifiers customParameters1, CustomModifiers customParameters2)
		{
			if (customParameters1 == null || customParameters1 is CustomModifiers_Empty)
				return customParameters2;

			//CustomModifiers ret = new CustomModifiers();

			ITypeContainer[] Containers;

			{
				if ((customParameters1.Containers == null) && (customParameters2.Containers == null))
					Containers = null;
				else
				{
					if (customParameters1.Containers == null)
						Containers = customParameters2.Containers;
					else
					{
						if (customParameters2.Containers == null)
							Containers = customParameters1.Containers;
						else
						{
							var l = new List<ITypeContainer>(customParameters1.Containers);
							l.AddRange(customParameters2.Containers);
							Containers = l.ToArray();
						}
					}
				}
			}

			FilterSet[] FilterSets;
			{
				if ((customParameters1.FilterSets == null) && (customParameters2.FilterSets == null))
					FilterSets = null;
				else
				{
					if (customParameters1.FilterSets == null)
						FilterSets = customParameters2.FilterSets;
					else
					{
						if (customParameters2.FilterSets == null)
							FilterSets = customParameters1.FilterSets;
						else
						{
							var l = new List<FilterSet>(customParameters1.FilterSets);
							l.AddRange(customParameters2.FilterSets);
							FilterSets = l.ToArray();
						}
					}
				}
			}

			Type[] ForcedParametricConstructorTypes;
			{
				if ((customParameters1.ForcedParametricConstructorTypes == null) && (customParameters2.ForcedParametricConstructorTypes == null))
					ForcedParametricConstructorTypes = null;
				else
				{
					if (customParameters1.ForcedParametricConstructorTypes == null)
						ForcedParametricConstructorTypes = customParameters2.ForcedParametricConstructorTypes;
					else
					{
						if (customParameters2.ForcedParametricConstructorTypes == null)
							ForcedParametricConstructorTypes = customParameters1.ForcedParametricConstructorTypes;
						else
						{
							var l = new List<Type>(customParameters1.ForcedParametricConstructorTypes);
							l.AddRange(customParameters2.ForcedParametricConstructorTypes);
							ForcedParametricConstructorTypes = l.ToArray();
						}
					}
				}
			}

			return new CustomModifiers(Containers, FilterSets, ForcedParametricConstructorTypes);
		}

	}

	internal class CustomModifiers_Empty : CustomModifiers
	{
		public CustomModifiers_Empty()
		{ }
	}

	// #############################################################################
	// #############################################################################

	/// <summary>
	/// Type filters.
	/// </summary>
	public class FilterSet
	{

		/// <summary>
		/// Returns 'false' if this type should not be serialized at all.
		/// That will let the default value created by the constructor of its container class/structure.
		/// </summary>
		public TypeSerializationValidatorDelegate TypeSerializationValidator;

		/// <summary>
		/// Returns 'false' if the default constructor of this type should not be tested by instanciation.
		/// Default value is 'true'.
		/// Serializer instanciates the default constructor (when available) once to ensure it does not throw an exception. Unfortunately, that instanciation can lead to problems. Example: each WPF Window have to be closed properly before closing the application.
		/// </summary>
		public CanTestDefaultConstructorDelegate CanTestDefaultConstructor;

		/// <summary>
		/// Cleans a type instance made by the default constructor.
		/// Serializer instanciates the default constructor (when available) once to ensure it does not throw an exception. Unfortunately, that instanciation can be registred in static lists. Example: each WPF Window have to be closed properly before closing the application.
		/// </summary>
		public DefaultConstructorTestCleanerDelegate DefaultConstructorTestCleaner;

		/// <summary>
		/// Tells the serializer to add some certain private fields to store the type.
		/// </summary>
		public AdditionalPrivateFieldsAdderDelegate AdditionalPrivateFieldsAdder;

	}

	// #############################################################################
	// #############################################################################

	/// <summary>
	/// A IContainerGenerator generates a container that encapsulates a not-serializable object and manages its own serialization method.
	/// This container generator should be a class, not a structure.
	/// </summary>
	public interface IContainerGenerator
	{
		/// <summary>
		/// Create a new instance of this container, containing ths object.
		/// Note: This function should be static but an interface can not declare static methods.
		/// </summary>
		ITypeContainer CreateNewContainer(object ContainedObject);

		/// <summary>
		/// Test if the type can be contained by this container.
		/// Note: This function should be static but an interface can not declare static methods.
		/// </summary>
		bool IsValidType(Type type);

		/// <summary>
		/// Some types need a container even when they have a no-param constructor.
		/// Note: This function should be static but an interface can not declare static methods.
		/// </summary>
		bool ApplyEvenIfThereIsAValidConstructor { get; }

		/// <summary>
		/// Some types, as FontStretch, are structure with only private fields.
		/// Note: This function should be static but an interface can not declare static methods.
		/// </summary>
		bool ApplyToStructures { get; }
	}

	/// <summary>
	/// A IContainer encapsulates a not-serializable object, and manages its own serialization method.
	/// </summary>
	public interface IContainer
	{
		/// <summary>
		/// Deserialize this instance.
		/// </summary>
		object Deserialize();
	}

	/// <summary>
	/// A ITypeContainer has two roles: it acts both as a container generator and as a container itself.
	/// </summary>
	public interface ITypeContainer : IContainerGenerator, IContainer
	{ }

	// #############################################################################
	// #############################################################################

}