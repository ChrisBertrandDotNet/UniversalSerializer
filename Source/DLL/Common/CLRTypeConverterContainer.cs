
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UniversalSerializerLib3
{

	/// <summary>
	/// For classes with [TypeConverter] (as System.Windows.Media.FontFamily).
	/// </summary>
	internal class CLRTypeConverterContainer : ITypeContainer
	{
		static Dictionary<Type, TypeConverter> VSCache = new Dictionary<Type, TypeConverter>();

		public CLRTypeConverterContainer()
		{ }

		public object Deserialize()
		{
			throw new NotSupportedException(); // will be done by TypeConverterContainerGeneric<T>.
		}

		static TypeConverter GetTypeConverterByCache(Type t)
		{
			TypeConverter vs;
			if (CLRTypeConverterContainer.VSCache.TryGetValue(t, out vs))
				return vs;
			var c = t.GetCustomAttributes(typeof(TypeConverterAttribute), true);
			TypeConverterAttribute att = c[0] as TypeConverterAttribute;
			vs = System.Activator.CreateInstance(Type.GetType(att.ConverterTypeName)) as TypeConverter;
			CLRTypeConverterContainer.VSCache.Add(t, vs);
			return vs;
		}

		static Type[] PossibleConvertionTypes = new Type[] { typeof(string), typeof(byte[]) };

		bool _IsValidType(Type type)
		{
			if (type.GetCustomAttributes(typeof(TypeConverterAttribute), true).Length <= 0)
				return false;

#if SILVERLIGHT
			if (type.Is(typeof(System.Windows.Media.ImageSource)))
				return false;	
#else
			// .NET 4:
			if (tbrush != null && type.Is(tbrush)) // From my point of view, BrushConverter is buggy.
				return false;

			if (type.FullName == "System.Windows.Forms.ControlBindingsCollection"
					|| (type.FullName == "System.Windows.Forms.Binding")
					|| (type.FullName == "System.Windows.TextDecorationCollection")
					|| (type.FullName == "System.Windows.Media.LineGeometry")
				)
				return false;
#endif

			{ // Test if a conversion type is managed:
				TypeConverter tc = GetTypeConverterByCache(type);

				bool anyConvertionTypeWorks = false;
				foreach (Type convertionType in PossibleConvertionTypes)
					if (tc.CanConvertTo(convertionType) && tc.CanConvertFrom(convertionType))
					{
						anyConvertionTypeWorks = true;
						break;
					}
				if (!anyConvertionTypeWorks)
					return false; // e.g. DependencyProperty
			}

			return true;
		}
		static Type tbrush = Tools.GetTypeFromFullName("System.Windows.Media.Brush"); // can be null if the application is not linked to System.Windows.Media.

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

		/// <summary>
		/// Returns a TypeConverterContainer&lt;T&gt;.
		/// </summary>
		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			if (ContainedObject == null)
				return null;

			Type type = ContainedObject.GetType();
			ITypeContainer obj=null;

			TypeConverter tc = GetTypeConverterByCache(type);
			try
			{
				if (tc.CanConvertTo(TypeString) && tc.CanConvertFrom(TypeString))
				{
					obj = CreateATypeConverterContainerGeneric(type, TypeString, tc.ConvertTo(null, Tools.EnUSCulture, ContainedObject, TypeString));
					if (obj != null)
					{
						var o2 = obj as _CLRTypeConverterContainerGeneric<string>;
						if (o2.Serialized == null || o2.Serialized == type.FullName)
						{
							Log.WriteLine(string.Format(
								ErrorMessages.GetText(15)//"The type '{0}' uses {1} as TypeConverter, but it does not convert to string correctly. Please investigate or contact the type's author."
								, type.FullName, tc.GetType().FullName));
							return null;
						}
					}
				}
				else
					if (tc.CanConvertTo(TypeByteArray) && tc.CanConvertFrom(TypeByteArray))
					{
						obj = CreateATypeConverterContainerGeneric(type, TypeByteArray, tc.ConvertTo(null, Tools.EnUSCulture, ContainedObject, TypeByteArray));
					}
			}
			catch (Exception e)
			{
				Log.WriteLine(e.Message);
			}
			if (obj == null)
				Log.WriteLine(string.Format(
					ErrorMessages.GetText(16)//"The type '{0}' uses {1} as TypeConverter, but its transcoding type is unknown. Please investigate or contact the type's author."
					, type.FullName, tc.GetType().FullName));

			return obj;
		}
		static readonly Type TypeString=typeof(string);
		static readonly Type TypeByteArray = typeof(Byte[]);

		static ITypeContainer CreateATypeConverterContainerGeneric(
			Type SourceObjectType, Type SerializationType, object SerializedObject)
		{
			KeyValuePair<Type,Type> k=new KeyValuePair<Type,Type>(SerializationType, SourceObjectType);
			Type g;
			if (!GenericContainersTypeCache.TryGetValue(k, out g))
			{
				Type[] typeArgs = { SerializationType, SourceObjectType };
				g = GenericType.MakeGenericType(typeArgs);
				GenericContainersTypeCache.Add(k, g);
			}
			object o = Activator.CreateInstance(g, SerializedObject);
			return o as ITypeContainer;
		}
		static Type GenericType = typeof(CLRTypeConverterContainerGeneric<,>);
		static Dictionary<KeyValuePair<Type, Type>, Type> GenericContainersTypeCache
			= new Dictionary<KeyValuePair<Type, Type>, Type>();

		public bool ApplyEvenIfThereIsAValidConstructor
		{
			get { return true; }
		}

		// ###########################

		internal class _CLRTypeConverterContainerGeneric<TSerialized>
		{
			public TSerialized Serialized;
		}

		/// <summary>
		/// Internal generic adaptation.
		/// For classes with [TypeConverter].
		/// </summary>
		/// <typeparam name="TSerialized">Type for internal serialization.</typeparam>
		/// <typeparam name="TSourceObject">Data type.</typeparam>
		internal class CLRTypeConverterContainerGeneric<TSerialized,TSourceObject>
			: _CLRTypeConverterContainerGeneric<TSerialized>,ITypeContainer
		{
			//public TSerialized Serialized;

			public CLRTypeConverterContainerGeneric()
			{ }

			public CLRTypeConverterContainerGeneric(TSerialized Serialized)
			{
				this.Serialized = Serialized;
			}

			public object Deserialize()
			{
				TypeConverter deserializer = CLRTypeConverterContainer.GetTypeConverterByCache(typeof(TSourceObject));
				return deserializer.ConvertFrom(null, Tools.EnUSCulture, this.Serialized);
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

		// #######################################


		public bool ApplyToStructures
		{
			get { return true; }
		}
	}
}
