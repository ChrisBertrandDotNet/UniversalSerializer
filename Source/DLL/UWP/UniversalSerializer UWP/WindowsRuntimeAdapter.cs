
// Copyright Christophe Bertrand.

// Additional classes for Windows Runtime.
// NETFX_CORE;WINDOWS_UWP

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

#if false
namespace System
{
	/// <summary>
	/// (chris) For completion only.
	/// </summary>
	public class DBNull
	{
	}
}
#endif

namespace UniversalSerializerLib3
{

	internal static class WindowsRuntimeAdapter
	{

		static WindowsRuntimeAdapter()
		{
			var dico = _GetTypeCodeCache = new Dictionary<Type, TypeCode>(Enum.GetValues(typeof(TypeCode)).Length);
#if !WINDOWS_UWP
			dico.Add(null, TypeCode.Empty);
#endif
			dico.Add(typeof(object), TypeCode.Object);
			dico.Add(typeof(DBNull), TypeCode.DBNull);
			dico.Add(typeof(Boolean), TypeCode.Boolean);
			dico.Add(typeof(char), TypeCode.Char);
			dico.Add(typeof(SByte), TypeCode.SByte);
			dico.Add(typeof(Byte), TypeCode.Byte);
			dico.Add(typeof(Int16), TypeCode.Int16);
			dico.Add(typeof(UInt16), TypeCode.UInt16);
			dico.Add(typeof(Int32), TypeCode.Int32);
			dico.Add(typeof(UInt32), TypeCode.UInt32);
			dico.Add(typeof(Int64), TypeCode.Int64);
			dico.Add(typeof(UInt64), TypeCode.UInt64);
			dico.Add(typeof(Single), TypeCode.Single);
			dico.Add(typeof(Double), TypeCode.Double);
			dico.Add(typeof(Decimal), TypeCode.Decimal);
			dico.Add(typeof(DateTime), TypeCode.DateTime);
			dico.Add(typeof(string), TypeCode.String);
		}

		internal static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingAttr, object/*Binder*/ binder, Type[] types, object/*ParameterModifier*/[] modifiers)
		{
			if (bindingAttr == (BindingFlags.Instance | BindingFlags.Public))
				return type.GetConstructor(types);

			var cs = type.GetConstructors(bindingAttr);
			if (cs != null && cs.Length != 0)
				foreach (var c in cs)
				{
					var pars = c.GetParameters();
					if (pars.Length == 0 && (types == null || types.Length == 0))
						return c;
					if (pars.Length == types.Length)
					{
						var same = true;
						for (int i = 0; i < pars.Length; i++)
							same &= pars[i].ParameterType == types[i];
						if (same)
							return c;
					}
				}
			return null;
		}

		internal static FieldInfo[] GetFields(this Type t, BindingFlags flags=BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
		{
			var fs = t.GetTypeInfo().DeclaredFields.ToList();
			{
				var ti = t.GetTypeInfo();
				while (true)
				{
					var type = ti.BaseType;
					if (type == null)
						break;
					ti = type.GetTypeInfo();
					fs.AddRange(ti.DeclaredFields);
				}
			}
			var fs2 = fs.Where(fi =>
					flags.HasFlag(BindingFlags.Instance) != fi.IsStatic
					&& (flags.HasFlag(BindingFlags.Public) == fi.IsPublic || flags.HasFlag(BindingFlags.NonPublic) == fi.IsPrivate)
				);
			return fs2.ToArray();
		}

		internal static Type[] GetGenericArguments(this Type t)
		{
			return t.GetTypeInfo().GenericTypeArguments;
		}

		/// <summary>
		/// Equivalent to Type.GetTypeCode(type).
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		internal static TypeCode GetTypeCode(this Type t)
		{
			TypeCode ret;
			if (_GetTypeCodeCache.TryGetValue(t, out ret))
				return ret;
			return TypeCode.Object;
		}
		static Dictionary<Type, TypeCode> _GetTypeCodeCache;

		internal static Type[] GetTypes(this Assembly a)
		{
			return a.DefinedTypes.Select(t => t.AsType()).ToArray();
		}
	}

#if false
	internal enum BindingFlags
	{
		Default = 0,
		IgnoreCase = 1,
		DeclaredOnly = 2,
		Instance = 4,
		Static = 8,
		Public = 16,
		NonPublic = 32,
		FlattenHierarchy = 64,
		InvokeMethod = 256,
		CreateInstance = 512,
		GetField = 1024,
		SetField = 2048,
		GetProperty = 4096,
		SetProperty = 8192,
		PutDispProperty = 16384,
		PutRefDispProperty = 32768,
		ExactBinding = 65536,
		SuppressChangeType = 131072,
		OptionalParamBinding = 262144,
		IgnoreReturn = 16777216,
	}
#endif

	internal enum TypeCode
	{
		Empty = 0,
		Object = 1,
		DBNull = 2, // it does not exist in UWP.
		Boolean = 3,
		Char = 4,
		SByte = 5,
		Byte = 6,
		Int16 = 7,
		UInt16 = 8,
		Int32 = 9,
		UInt32 = 10,
		Int64 = 11,
		UInt64 = 12,
		Single = 13,
		Double = 14,
		Decimal = 15,
		DateTime = 16,
		String = 18,
	};

	internal class AppDomain
	{
		internal readonly static AppDomain CurrentDomain = new AppDomain();

		internal Assembly[] GetAssemblies()
		{
			List<Assembly> assemblies = new List<Assembly>();

			var filesTask = Windows.ApplicationModel.Package.Current.InstalledLocation.GetFilesAsync().AsTask();
			filesTask.Wait();
			var files = filesTask.Result;
			if (files != null)
			{
				foreach (var file in files.Where(file => file.FileType == ".dll" || file.FileType == ".exe"))
				{
					try
					{
						assemblies.Add(Assembly.Load(new AssemblyName(file.DisplayName)));
					}
					catch (FileNotFoundException)
					{
					}
				}
			}

			return assemblies.ToArray();
		}
	}

}