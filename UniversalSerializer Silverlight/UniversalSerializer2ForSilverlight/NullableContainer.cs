
// Copyright Christophe Bertrand.

#if SILVERLIGHT
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// For Nullable classes in Silverlight.
	/// Reason: Silverlight can not access Nullable.hasValue and Nullable.value because they are private fields.
	/// </summary>
	internal class NullableContainer : ITypeContainer
	{

		public NullableContainer()
		{ }

		public object Deserialize()
		{
			throw new NotSupportedException();
		}

		bool _IsValidType(System.Type type)
		{
			Type t2 = type.FindDerivedOrEqualToThisType(TGenericNullable);
			return (t2 != null);
		}
		public bool IsValidType(System.Type type)
		{
			bool ret;
			if (!IsValidTypeCache.TryGetValue(type, out ret))
			{
				ret = this._IsValidType(type);
				IsValidTypeCache.Add(type, ret);
			}
			return ret;
		}
		static Dictionary<Type, bool> IsValidTypeCache = new Dictionary<Type, bool>();
		static readonly Type TGenericNullable = typeof(Nullable<>);

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			Type type = ContainedObject.GetType();

			ITypeContainer obj = null;

			obj = CreateABinaryContainerGeneric(
				type,
				ContainedObject);

			return obj;
		}

		static ITypeContainer CreateABinaryContainerGeneric(
			Type SourceObjectType, object ContainedObject)
		{
			Type g;
			if (!GenericContainersTypeCache.TryGetValue(SourceObjectType, out g))
			{
				Type[] typeArgs = { SourceObjectType };
				g = GenericType.MakeGenericType(typeArgs);
				GenericContainersTypeCache.Add(SourceObjectType, g);
			}
			object o;
			if (ContainedObject == null)
				o = Activator.CreateInstance(g);
			else
				o = Activator.CreateInstance(g, ContainedObject);
			return o as ITypeContainer;
		}
		static readonly Type GenericType = typeof(NullableContainerGeneric<>);
		static Dictionary<Type, Type> GenericContainersTypeCache
			= new Dictionary<Type, Type>();

		public bool ApplyEvenIfThereIsAValidConstructor
		{
			get { return true; }
		}

		// ----------------------------------------------------------------------------------
		// ###########################

		/// <summary>
		/// Internal generic adaptation.
		/// For Nullable classes in Silverlight.
		/// </summary>
		internal class NullableContainerGeneric<TSourceObject> : ITypeContainer
			where TSourceObject : struct
		{
			public bool HasValue;
			public TSourceObject Value;

			public NullableContainerGeneric(TSourceObject Value)
			{
				this.HasValue = true;
				this.Value = Value;
			}

			public NullableContainerGeneric()
			{
			}

			public object Deserialize()
			{
				if (this.HasValue)
					return new Nullable<TSourceObject>(this.Value);
				return new Nullable<TSourceObject>();
			}

			static string SerializeObject(object o)
			{
				throw new NotSupportedException();
			}

			public bool IsValidType(Type type)
			{
				throw new NotSupportedException();
			}

			public ITypeContainer CreateNewContainer(object ContainedObject)
			{
				throw new NotSupportedException();
			}

			public bool ApplyEvenIfThereIsAValidConstructor
			{
				get
				{
					throw new NotSupportedException();
				}
			}


			public bool ApplyToStructures
			{
				get { throw new NotSupportedException(); }
			}
		}



		public bool ApplyToStructures
		{
			get { return true; }
		}
	}
}
#endif