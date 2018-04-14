
// Copyright Christophe Bertrand.

#if true//! SILVERLIGHT
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// For classes inheriting Type.
	/// </summary>
	internal class CLRTypeContainer : ITypeContainer
	{

		public CLRTypeContainer()
		{ }

		public object Deserialize()
		{
			throw new NotSupportedException();
		}

		bool _IsValidType(System.Type type)
		{
			return type.Is(tType);
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
		static Type tType = typeof(Type);

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			if (ContainedObject == null)
				return null;

			Type type = (Type)ContainedObject;
			ITypeContainer obj = CreateATypeContainerGeneric(type);

			return obj;
		}

		static ITypeContainer CreateATypeContainerGeneric(
			Type SourceObjectType)
		{
			Type g;
			if (!GenericContainersTypeCache.TryGetValue(SourceObjectType, out g))
			{
				Type[] typeArgs = { SourceObjectType };
				g = GenericType.MakeGenericType(typeArgs);
				GenericContainersTypeCache.Add(SourceObjectType, g);
			}
			object o = Activator.CreateInstance(g, SourceObjectType.AssemblyQualifiedName);
			return o as ITypeContainer;
		}
		static Type GenericType = typeof(CLRTypeContainerGeneric<>);
		static Dictionary<Type, Type> GenericContainersTypeCache
			= new Dictionary<Type, Type>();

		public bool ApplyEvenIfThereIsAValidConstructor
		{
			get { return false; }
		}

		// ----------------------------------------------------------------------------------
		// ###########################

		/// <summary>
		/// Internal generic adaptation.
		/// For classes inheriting Type.
		/// </summary>
		internal class CLRTypeContainerGeneric<TSourceObject> : ITypeContainer
		{
			public string AssemblyQualifiedName;

			public CLRTypeContainerGeneric()
			{ }

			public CLRTypeContainerGeneric(string AssemblyQualifiedName)
			{
				this.AssemblyQualifiedName = AssemblyQualifiedName;
			}

			public object Deserialize()
			{
				return Tools.GetTypeFromFullName(this.AssemblyQualifiedName);
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

		// - - - - - -- - - - - - - - - - - - - -- - - -

		public bool ApplyToStructures
		{
			get { return false; }
		}
	}
}
#endif