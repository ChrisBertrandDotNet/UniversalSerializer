
// Copyright Christophe Bertrand.

// This class is very special: it tries to build & contain a class that will be constructed with the values of its own (private or internal) fields.
// That is risky, but it can help a lot with classes with no no-param (default) constructors.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
#if DEBUG
using System.Diagnostics;
#endif
using UniversalSerializerLib3.TypeTools;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// For classes not serializable by the other ITypeContainers.
	/// It tries to call the class parameter constructors with the fields as parameters.
	/// </summary>
	internal static class ParametricConstructorsManager
	{

		/// <summary>
		/// A list of valid types.
		/// IntelligentConstructorContainer can contain these types safely.
		/// String format is 'Type'.AssemblyQualifiedName .
		/// </summary>
		internal static readonly string[] ValidatedTypes = new string[] { };

		/// <summary>
		/// A list of invalid types.
		/// IntelligentConstructorContainer can not contain these types safely.
		/// String format is 'Type'.AssemblyQualifiedName .
		/// </summary>
		internal static readonly string[] InvalidatedTypes = new string[] { };

		static readonly Type[] _InvalidatedTypes;

		static ParametricConstructorsManager()
		{
			_InvalidatedTypes = new Type[InvalidatedTypes.Length];
			for (int i = 0; i < InvalidatedTypes.Length; i++)
				_InvalidatedTypes[i] = Type.GetType(InvalidatedTypes[i]);
		}

		internal static object[] GetParameterFields(object o, ParametricConstructorDescriptor d)
		{
			var cons = d.constructorInfo;
			var fields = d.ParameterFields;
			var parameters = cons.GetParameters();
			object[] ret = new object[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
			{
				var field = fields[i];
				ret[i] = field.GetValue(o);
			}

			return ret;
		}

		/// <summary>
		/// Get a parametric constructor for this type.
		/// If none applies, returns null.
		/// </summary>
		static internal ParametricConstructorDescriptor GetTypeParamDescriptorFromCache(Type type)
		{
			Contains<ParametricConstructorDescriptor> d;
			if (paramDescriptorCache.TryGetValue(type, out d))
				return d.Value; // can return null.


			{
				// We need to analyse the class type:
				d = AnalyseClassType(type);
			}
			paramDescriptorCache.Add(type, d);

			return d.Value;
		}

		static Dictionary<Type, Contains<ParametricConstructorDescriptor>> paramDescriptorCache =
	new Dictionary<Type, Contains<ParametricConstructorDescriptor>>();

		// ---------------------------
		// ---------------------------
		// ---------------------------
		// ---------------------------

		#region analysis logic
		/// <summary>
		/// Returns Value=null if this type does not apply.
		/// </summary>
		static Contains<ParametricConstructorDescriptor> AnalyseClassType(Type t)
		{
			if (!t.IsEnum && !_InvalidatedTypes.Contains(t))
			{
				bool AvoidDefaultConstructor = true; // Never uses a default constructor.

				BindingFlags[] bflags = new BindingFlags[2] { BindingFlags.Public | BindingFlags.Instance, BindingFlags.NonPublic | BindingFlags.Instance };

				// 2 passes: one with public constructors, and one with private constructors.
				for (int iflags = 0; iflags < 2; iflags++)
				{
					var cs = t.GetConstructors(bflags[iflags]).Where((ci) => AvoidDefaultConstructor ? ci.GetParameters().Length != 0 : true);
					ConstructorInfo cons;
					var mf = AnalyseConstructors(cs, out cons);
					if (mf != null)
						return ParametricConstructorDescriptor.CreateDirectTypeParamDescriptor(cons, mf.ToArray());
				}
			}

			return new Contains<ParametricConstructorDescriptor>(null); // This type does not apply.
		}

		// ------------------------------


		static List<FieldInfo> AnalyseConstructors(
			IEnumerable<ConstructorInfo> constructors,
			out ConstructorInfo FoundConstructor)
		{
			// Sort the list from much parameters to none:
			var sorted = new List<ConstructorInfo>(constructors);
			if (sorted.Count > 1)
				sorted.Sort((a, b) => b.GetParameters().Length - a.GetParameters().Length);
			foreach (var cons in sorted)
			{
				var mf = AnalyseConstructor(cons);
				if (mf != null)
				{
					FoundConstructor = cons;
					return mf;
				}
			}
			FoundConstructor = null;
			return null;
		}

		/// <summary>
		/// Try to find a field for each parameter of the constructor.
		/// </summary>
		/// <param name="constructor"></param>
		static List<FieldInfo> AnalyseConstructor(ConstructorInfo constructor)
		{
#if false//DEBUG
			if (constructor.DeclaringType.FullName == "Test_UniversalSerializer.Tests+ClassInheritingAPrivateField")
				Debugger.Break();
#endif
			var pars = constructor.GetParameters();
			var availableFields = new List<FieldInfo>(
				//constructor.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
				constructor.DeclaringType.GetPrivateAndPublicFieldsIncludingInherited());
			List<FieldInfo> MatchingFields = new List<FieldInfo>(availableFields.Count);

			foreach (var par in pars)
			{
				FieldInfo field = FindMatchingField(par, availableFields);
				if (field == null)
					return null; // If any param is not found, we abandon.
				MatchingFields.Add(field);
				availableFields.Remove(field); // no more available.
			}
			return MatchingFields;
		}

		static FieldInfo FindMatchingField(ParameterInfo par, List<FieldInfo> availableFields)
		{
			string paramName = par.Name;
			Type paramType = par.ParameterType;

			foreach (var field in availableFields)
			{
				if (paramType.Is(field.FieldType))
				{
					if (paramName == field.Name) // parameter and field have the same name.
						return field;
					if (field.IsPrivate)
					{
						string privname = "_" + paramName; // Ex: "MyField" -> "_MyField".
						if (privname == field.Name)
							return field;
						privname = "_" + paramName.ToLower()[0] + paramName.Substring(1); // Ex: "MyField" -> "_myField".
						if (privname == field.Name)
							return field;
					}
					string privname3 = paramName.ToUpper()[0] + paramName.Substring(1); // Ex: "myField" -> "MyField".
					if (privname3 == field.Name)
						return field;

					string privname4 = paramName.ToLower()[0] + paramName.Substring(1); // Ex: "MyField" -> "myField".
					if (privname4 == field.Name)
						return field;
				}
			}
			return null;
		}
		#endregion analysis logic

		// ###################################################################################
		// ###################################################################################
		// ###################################################################################
		// ###################################################################################

		internal class Contains<T> where T : class
		{
			internal T Value;

			internal Contains(T value)
			{
				this.Value = value;
			}
		}

	}

	// ###################################################################################
	// ###################################################################################


	/// <summary>
	/// One instance per Type.
	/// That will NOT be serialized.
	/// </summary>
	internal class ParametricConstructorDescriptor
	{
		internal readonly FieldInfo[] ParameterFields;
		//internal readonly int[] FieldIndexes;
		internal readonly ConstructorInfo constructorInfo;

		// ------------------------------

		/// <summary>
		/// Warning: You have to check GetTypeParamDescriptor first.
		/// </summary>
		ParametricConstructorDescriptor(
			FieldInfo[] ParameterFields,
			//int[] FieldIndexes,
			ConstructorInfo constructorInfo
			)
		{
			this.constructorInfo = constructorInfo;
			this.ParameterFields = ParameterFields;
		}

		// ------------------------------

		/// <summary>
		/// Warning: You have to check GetTypeParamDescriptor first.
		/// </summary>
		static internal ParametricConstructorsManager.Contains<ParametricConstructorDescriptor> CreateDirectTypeParamDescriptor(
			ConstructorInfo constructorInfo, FieldInfo[] Fields)
		{
			return new ParametricConstructorsManager.Contains<ParametricConstructorDescriptor>(
				new ParametricConstructorDescriptor(Fields, constructorInfo));
		}

		// ------------------------------

		/// <summary>
		/// For each parameter, find its index in the SelectedFields, in the order of this.ParameterFields.
		/// </summary>
		/// <param name="SelectedFields">Concatenated selected public and private fields of a particular TypeManager.</param>
		/// <param name="CancelOnErrors">If true, an error will return null. If false, an error will throw an exception.</param>
		/// <returns>int[this.ParameterFields.Length]</returns>
		internal int[] GetParameterIndexes(FieldInfo[] SelectedFields, bool CancelOnErrors)
		{
			int[] indexes = new int[this.ParameterFields.Length];
			for (int i = 0; i < this.ParameterFields.Length; i++)
			{
				var fi = ParameterFields[i];

				indexes[i] = Array.IndexOf(SelectedFields, fi);
				if (indexes[i] < 0)
				{
					if (CancelOnErrors)
						return null;
					string msg = string.Format(
						ErrorMessages.GetText(14)// "Type \"{0}\" can not be constructed because this parameter's type has been disallowed by a filter: Parameter's name=\"{1}\", Corresponding field's name=\"{2}\", Type=\"{3}\".\n\tSuggestion: use a filter to disallow the main type, or to allow the parameter's type."
						, this.constructorInfo.DeclaringType.FullName, this.constructorInfo.GetParameters()[i].Name, fi.Name, fi.FieldType.FullName);
					Log.WriteLine(msg);
					throw new Exception(msg);
				}
			}
			return indexes;
		}

		// ------------------------------
		// ------------------------------
	}

	// ###################################################################################
	// ###################################################################################
	// ###################################################################################
	// ###################################################################################

}
