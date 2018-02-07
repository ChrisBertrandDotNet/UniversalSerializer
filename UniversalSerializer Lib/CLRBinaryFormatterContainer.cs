
// Copyright Christophe Bertrand.

#if ! SILVERLIGHT
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// For classes with [Serializable] but no no-param constructor.
	/// </summary>
	internal class CLRBinaryFormatterContainer : ITypeContainer
	{

		public CLRBinaryFormatterContainer()
		{ }

		public object Deserialize()
		{
			throw new NotSupportedException();
		}

		static byte[] SerializeObject(object o)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				try
				{
					formatter.Serialize(stream, o);
				}
				catch (Exception e)
				{
					throw new Exception(string.Format(
						ErrorMessages.GetText(8),//"Type '{0}' (or one of its sub-data) is not serializable by BCL's BinaryFormatter. (suggestion: try remove attribute [Serializable], or add an exploitable constructor).",
						o.GetType().GetName()), e);
				}
				return stream.ToArray();
			}
		}

		bool _IsValidType(System.Type type)
		{
			if (type == typeof(string))
				return false; // because it is not efficient.
			if ((type == typeof(System.WeakReference))
				|| (type == typeof(DictionaryEntry)))
				return false; // Because its value is not always serializable.
			if ((type.FullName == "System.Drawing.Size")
				|| (type.FullName == "System.Drawing.Point")
				|| (type.FullName == "System.Drawing.SizeF"))
					return false; // because it is not efficient.
			if (type.IsADirectGenericOf(typeof(Dictionary<,>)))
				return false; // because it is not efficient.
			return ((type.GetCustomAttributes(typeof(System.SerializableAttribute), true).Length > 0)
				|| type.GetInterface(typeof(System.Runtime.Serialization.ISerializable).Name) != null);
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

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			if (ContainedObject == null)
				return null;

			Type type = ContainedObject.GetType();

			ITypeContainer obj = null;

#if false
			var so = SerializeObject(ContainedObject);
			
			obj = CreateABinaryContainerGeneric(
				type, 
				so);
#else
			obj = CreateABinaryContainerGeneric(
				type,
				SerializeObject(ContainedObject));
#endif

			return obj;
		}

		static ITypeContainer CreateABinaryContainerGeneric(
			Type SourceObjectType, byte[] SerializedObject)
		{
			Type g;
			if (!GenericContainersTypeCache.TryGetValue(SourceObjectType, out g))
			{
				Type[] typeArgs = { SourceObjectType };
				g = GenericType.MakeGenericType(typeArgs);
				GenericContainersTypeCache.Add(SourceObjectType, g);
			}
			object o = Activator.CreateInstance(g, SerializedObject);
			return o as ITypeContainer;
		}
		static Type GenericType = typeof(CLRBinaryFormatterContainerGeneric<>);
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
		/// For classes with [ValueSerializer].
		/// </summary>
		internal class CLRBinaryFormatterContainerGeneric<TSourceObject> : ITypeContainer
		{
			public byte[] BinarySerialized;

			public CLRBinaryFormatterContainerGeneric()
			{ }

			public CLRBinaryFormatterContainerGeneric(byte[] Serialized)
			{
				this.BinarySerialized = Serialized;
			}

			public object Deserialize()
			{
				using (MemoryStream stream = new MemoryStream(this.BinarySerialized))
				{
					var formatter = new BinaryFormatter();
					return formatter.Deserialize(stream);
				}
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