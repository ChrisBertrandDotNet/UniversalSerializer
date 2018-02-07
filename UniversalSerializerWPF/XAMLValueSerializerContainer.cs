
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Windows.Markup;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// For classes with [ValueSerializer].
	/// </summary>
	internal class XAMLValueSerializerContainer : ITypeContainer
	{
		static Dictionary<Type, ValueSerializer> VSCache = new Dictionary<Type, ValueSerializer>();

		public XAMLValueSerializerContainer()
		{ }

		public object Deserialize()
		{
			throw new NotSupportedException();
		}

		static ValueSerializer GetValueSerializerByCache(Type t)
		{
			ValueSerializer vs;
			if (XAMLValueSerializerContainer.VSCache.TryGetValue(t, out vs))
				return vs;
			var c = t.GetCustomAttributes(typeof(ValueSerializerAttribute), true);
			ValueSerializerAttribute att = c[0] as ValueSerializerAttribute;
			vs = System.Activator.CreateInstance(att.ValueSerializerType) as ValueSerializer;
			XAMLValueSerializerContainer.VSCache.Add(t, vs);
			return vs;
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
		bool _IsValidType(Type type)
		{
			if (
				(type.GetCustomAttributes(typeof(System.Windows.Markup.ValueSerializerAttribute), true).Length <= 0)
				// warning: some classes do not transcode the values correctly. e.g. LinearGradientBrush does not.
				|| (Tools.TypeIs(type, tLinearGradientBrush))
				|| type==typeof(System.Windows.DependencyProperty)
				|| type == typeof(System.Windows.Media.LineGeometry)
				)
				return false;

			return true;
		}
		static readonly Type tLinearGradientBrush = Tools.GetTypeFromFullName("System.Windows.Media.LinearGradientBrush");

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			if (ContainedObject == null)
				return null;

			Type type = ContainedObject.GetType();
			ITypeContainer obj = null;

			var vs = GetValueSerializerByCache(type);
			try
			{
				{
					obj = CreateAValueSerializerContainerGeneric(type, vs.ConvertToString(ContainedObject, null));
				}
			}
			catch(Exception e)
			{
				Log.WriteLine(e.Message);
			}
			if (obj == null)
				Log.WriteLine(string.Format(
					ErrorMessagesWPF.GetText(2)//"The type '{0}' uses {1} as ValueSerializer, but it was not transcoded correctly. Please investigate or contact the author."
					, type.FullName, vs.GetType().FullName));

			return obj;
		}

		static ITypeContainer CreateAValueSerializerContainerGeneric(
			Type SourceObjectType, string SerializedObject)
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
		static Type GenericType = typeof(XAMLValueSerializerContainerGeneric<>);
		static Dictionary<Type, Type> GenericContainersTypeCache
			= new Dictionary<Type, Type>();
		static string SerializeObject(object o)
		{
			ValueSerializer serialiseur = XAMLValueSerializerContainer.GetValueSerializerByCache(o.GetType());
			return serialiseur.ConvertToString(o, null);
		}

		public bool ApplyEvenIfThereIsAValidConstructor
		{
			get { return true; }
		}

		// ----------------------------------------------------------------------------------
		// ###########################

		/// <summary>
		/// Internal generic adaptation.
		/// For classes with [ValueSerializer].
		/// </summary>
		internal class XAMLValueSerializerContainerGeneric<TSourceObject> : ITypeContainer
		{
			public string StringSerialized;

			public XAMLValueSerializerContainerGeneric()
			{ }

			public XAMLValueSerializerContainerGeneric(string Serialized)
			{
				this.StringSerialized = Serialized;
			}

			public object Deserialize()
			{
				ValueSerializer serialiseur = XAMLValueSerializerContainer.GetValueSerializerByCache(typeof(TSourceObject));
				return serialiseur.ConvertFromString(this.StringSerialized, null);
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
