
// Copyright Christophe Bertrand.

#if NETFX_CORE
	#define WINDOWS_STORE
#endif

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using UniversalSerializerLib3.NumberTools;
using UniversalSerializerLib3.DataTools;
using UniversalSerializerLib3.TypeTools;
#if NET3_5 || WINDOWS_PHONE7_1
using System.Reflection.Emit;
#endif

namespace UniversalSerializerLib3
{
	public static partial class Tools
	{
		// -------------------------------------------

		static Tools()
		{
			if (TypeTools.Types.GetEnumValuesCount(typeof(DateTimeKind)) > 4)
				Log.WriteLine(
					ErrorMessages.GetText(18));//"Warning: DateTimeKind has more than 4 items now. That will cause problems in DateTimes. See ToTicksAndKind().");
		}

		// -------------------------------------------

		/// <summary>
		/// The default culture "en-US" is useful for number and date transcoding to and from string.
		/// </summary>
		internal static readonly CultureInfo EnUSCulture = new CultureInfo("en-us");

		// -------------------------------------------

		/// <summary>
		/// Finds the Entry assembly. Usually the main application executable.
		/// </summary>
		/// <returns>The Entry assembly</returns>
		/// <exception cref="EntryPointNotFoundException">The Entry assembly has not be found.</exception>
		internal static Assembly GetEntryAssembly()
		{
			if (_EntryAssembly == null)
				_EntryAssembly = _GetEntryAssembly();
			return _EntryAssembly;
		}
		static Assembly _EntryAssembly;
		static Assembly _GetEntryAssembly()
		{
#if SILVERLIGHT
			return System.Windows.Application.Current.GetType().Assembly;
#else
#if PORTABLE || WINDOWS_STORE
			// Try by reflexion:
			var m = _GetEntryAssemblyMethod.Value;
			if (m != null)
			{
				var r=m();
				return r;
			}
			return null;
#else
#if ANDROID
			var ea = Assembly.GetEntryAssembly();
			if (ea != null)
				return ea;
			//var assss = AppDomain.CurrentDomain.GetAssemblies();
			var pile = new StackTrace(true);
			if (pile != null)
			{
				for (int i = pile.FrameCount - 1; i >= 0; i--)
				{
					var at = pile.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;
					if (at != null && !at.FullName.StartsWith("mscorlib,") && !at.FullName.StartsWith("Mono.Android,"))
						return at;
				}
			}
			throw new EntryPointNotFoundException();
#else
			Assembly ea;
			try
			{
				ea = Assembly.GetEntryAssembly();

				if (ea == null) // Android.
				{
					List<string> referedAssemblies; // the assemblies that are refered by other assemblies.
					{
						var assemblies = AppDomain.CurrentDomain.GetAssemblies();
						referedAssemblies = new List<string>(assemblies.Length);
						foreach (var a in assemblies)
							referedAssemblies.AddRange(a.GetReferencedAssemblies().Select(an => an.Name));
					}

					var pile = new StackTrace(true);
					if (pile != null)
					{
						for (int i = pile.FrameCount - 1; i >= 0; i--)
						{
							var frame = pile.GetFrame(i);
							if (frame != null)
							{
								var method = frame.GetMethod();
								if (method != null)
								{
									var decType = method.DeclaringType;
									if (decType != null)
									{
										var at = decType.Assembly;
										if (at != null && !referedAssemblies.Contains(at.GetName().Name))
											return at; // The first assembly that is not refered by other assemblies. It should be the entry assembly.
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new EntryPointNotFoundException("Entry assembly not found", ex);
			}
			if (ea == null)
				throw new EntryPointNotFoundException();
			return ea;
#endif
#endif
#endif
		}
#if PORTABLE || WINDOWS_STORE
		static SimpleLazy<Func<Assembly>> _GetEntryAssemblyMethod = new SimpleLazy<Func<Assembly>>(
			() =>
			{
				var ta = typeof(Assembly);
#if PORTABLE
				var m = ta.GetMethod("GetEntryAssembly", BindingFlags.Static | BindingFlags.Public);
				if (m == null)
					return null;
				var d = (Func<Assembly>)Delegate.CreateDelegate(typeof(Func<Assembly>), m);
#else // WINDOWS_STORE
				var m = ta.GetTypeInfo().GetDeclaredMethod("GetEntryAssembly");
				if (m == null)
					return null;
				var d = (Func<Assembly>)m.CreateDelegate(typeof(Func<Assembly>));
#endif
				return d;
			});
#endif

		// -------------------------------------------

		/// <summary>
		/// DateTimeKind is stored in bits 62 and 63.
		/// </summary>
		/// <param name="ticksAndTime"></param>
		/// <returns></returns>
		internal static DateTime DateTimeFromTicksAndKind(ulong ticksAndTime)
		{
			return new DateTime((long)(ticksAndTime & 0x3FFFFFFFFFFFFFFFUL), (DateTimeKind)(ticksAndTime >> 62));
		}

		/// <summary>
		/// DateTimeKind is stored in bits 62 and 63.
		/// </summary>
		/// <param name="dateTime"></param>
		/// <returns></returns>
		internal static ulong ToTicksAndKind(this DateTime dateTime)
		{
			return (ulong)dateTime.Ticks | ((ulong)dateTime.Kind << 62);
		}

		// -------------------------------------------

		internal static object GenericMethodCall(object classInstance, MethodInfo method, Type[] types, object[] parameters = null)
		{
			var m =
				method.IsGenericMethodDefinition ?
				method
				: method.GetGenericMethodDefinition();
			MethodInfo generic = m.MakeGenericMethod(types);
			return generic.Invoke(classInstance, parameters);
		}

		// -------------------------------------------

		internal static Func<object, object> GenerateFieldGetter(Type classType, Type memberType, FieldInfo fieldInfo)
		{
			var classThis = Expression.Parameter(typeof(object)
#if DEBUG || PORTABLE || NET3_5
, "this"
#endif
);
			var cast_classThis = Expression.Convert(classThis, classType);
			var member = Expression.Field(cast_classThis, fieldInfo);
			var lambda1 = Expression.Lambda(member, classThis);

			var resultBody = Expression.Convert(lambda1.Body, typeof(object));
			var lambda = Expression.Lambda<Func<object, object>>(resultBody, lambda1.Parameters);

			return lambda.Compile();
		}

		// -------------------------------------------

		internal static Func<object, object> GeneratePropertyGetter(Type classType, Type memberType, PropertyInfo propertyInfo)
		{
			var classThis = Expression.Parameter(typeof(object)
#if DEBUG || PORTABLE || NET3_5
, "this"
#endif
);
			var cast_classThis = Expression.Convert(classThis, classType);
			var member = Expression.Property(cast_classThis, propertyInfo);
			var lambda1 = Expression.Lambda(member, classThis);

			var resultBody = Expression.Convert(lambda1.Body, typeof(object));
			var lambda = Expression.Lambda<Func<object, object>>(resultBody, lambda1.Parameters);

			return lambda.Compile();
		}

		// -------------------------------------------

		// Keep that code for reference.
		static Func<object, object> GenerateMemberGetter(Type classType, Type memberType, string member_name)
		{
			var classThis = Expression.Parameter(typeof(object)
#if DEBUG || PORTABLE || NET3_5
, "this"
#endif
);
			var cast_classThis = Expression.Convert(classThis, classType);
			var member = Expression.PropertyOrField(cast_classThis, member_name);
			var lambda1 = Expression.Lambda(member, classThis);

			var resultBody = Expression.Convert(lambda1.Body, typeof(object));
			var lambda = Expression.Lambda<Func<object, object>>(resultBody, lambda1.Parameters);

			return lambda.Compile();
		}

		// -------------------------------------------

		// Keep that code for reference.
		/// <summary>
		/// Creates a compiled method that set a class member.
		/// Does not apply to structures (value references can not be cast in .NET).
		/// </summary>
		/// <param name="classType"></param>
		/// <param name="memberType"></param>
		/// <param name="member_name"></param>
		/// <returns></returns>
		static Action<object, object> GenerateReferenceTypeMemberSetterByTypesNoRef(
				Type classType, Type memberType, string member_name)
		{
#if DEBUG
			if (TypeEx.IsValueType( classType))
				throw new Exception();
#endif

			var objectType = typeof(object);
			var param_this = Expression.Parameter(objectType, "this");
			var param_value = Expression.Parameter(typeof(object), "value");
			var cast_this = Expression.Convert(param_this, classType);
			var cast_param = Expression.Convert(param_value, memberType);
			var member = Expression.PropertyOrField(cast_this, member_name);
			var assign =
#if !WINDOWS_PHONE && !PORTABLE && !NET3_5
				Expression.Assign
#else
 ExpressionEx.Assign
#endif
(member, cast_param);
			var lambda = Expression.Lambda<Action<object, object>>(assign, param_this, param_value);

			return lambda.Compile();
		}

		// -------------------------------------------

		/// <summary>
		/// Creates a compiled method that set a class field.
		/// Does not apply to structures (value references can not be cast in .NET).
		/// </summary>
		/// <param name="fi">FieldInfo</param>
		/// <returns></returns>
		internal static Action<object, object> GenerateReferenceTypeFieldSetter(
				FieldInfo fi)
		{
#if DEBUG
			if (TypeEx.IsValueType( fi.DeclaringType))
				throw new Exception();
#endif

			if (fi.IsInitOnly)
				return null; // Expression.Assign() can not set a readonly field.

			var objectType = typeof(object);
			var param_this = Expression.Parameter(objectType, "this");
			var param_value = Expression.Parameter(typeof(object), "value");
			var cast_this = Expression.Convert(param_this, fi.DeclaringType);
			var cast_param = Expression.Convert(param_value, fi.FieldType);
			var member = Expression.Field(cast_this, fi);
			var assign =
#if !WINDOWS_PHONE && !PORTABLE && !NET3_5
				Expression.Assign
#else
 ExpressionEx.Assign
#endif
(member, cast_param);
			var lambda = Expression.Lambda<Action<object, object>>(assign, param_this, param_value);

			return lambda.Compile();
		}

		// -------------------------------------------

		/// <summary>
		/// Creates a compiled method that set a class property.
		/// Does not apply to structures (value references can not be cast in .NET).
		/// </summary>
		/// <param name="pi">PropertyInfo</param>
		/// <returns></returns>
		internal static Action<object, object> GenerateReferenceTypePropertySetter(
				 PropertyInfo pi)
		{
#if DEBUG
			if (TypeEx.IsValueType(pi.DeclaringType))
				throw new Exception();
#endif

			if (!pi.CanWrite)
				return null; // Expression.Assign() can not set a property with no set_().

			var objectType = typeof(object);
			var param_this = Expression.Parameter(objectType, "this");
			var param_value = Expression.Parameter(typeof(object), "value");
			var cast_this = Expression.Convert(param_this, pi.DeclaringType);
			var cast_param = Expression.Convert(param_value, pi.PropertyType);
			var member = Expression.Property(cast_this, pi);
			var assign =
#if !WINDOWS_PHONE && !PORTABLE && !NET3_5
				Expression.Assign
#else
 ExpressionEx.Assign
#endif
(member, cast_param);
			var lambda = Expression.Lambda<Action<object, object>>(assign, param_this, param_value);

			return lambda.Compile();
		}

		// -------------------------------------------

		// Keep that code for reference.
		static ValueTypeMemberSetterDelegate<T, V> GenerateValueTypeMemberSetter<T, V>(string member_name)
				where T : struct
		{
			var this_ref = typeof(T).MakeByRefType();

			var param_this = Expression.Parameter(this_ref, "this");
			var param_value = Expression.Parameter(typeof(V), "value");
			var member = Expression.PropertyOrField(param_this, member_name);
			var assign =
#if !WINDOWS_PHONE && !PORTABLE && !NET3_5
				Expression.Assign
#else
 ExpressionEx.Assign
#endif
(member, param_value);
			var lambda = Expression.Lambda<ValueTypeMemberSetterDelegate<T, V>>(assign, param_this, param_value);

			return lambda.Compile();
		}
		delegate void ValueTypeMemberSetterDelegate<T, V>(ref T @this, V value);

		// -------------------------------------------

		internal delegate void ValueTypeObjectMemberSetterDelegate<T>(ref T @this, object value);

		// -------------------------------------------

		internal static ValueTypeObjectMemberSetterDelegate<T> GenerateValueTypeFieldSetter<T>(FieldInfo fi)
		{
#if NET3_5// also a test for WINDOWS_PHONE7_1
			return GenerateValueTypeFieldSetter_ByIL<T>(fi);
#else
			var this_ref = typeof(T).MakeByRefType();
#if false
			if (this_ref == null)
				throw new Exception(string.Format("Type{{{0}}}.MakeByRefType() returned null.", typeof(T).FullName));
#endif

			var param_struct = Expression.Parameter(this_ref
#if DEBUG || PORTABLE
, "this"
#endif
);
#if false
			if (param_struct == null)
				throw new Exception();
#endif

			var param_value = Expression.Parameter(typeof(object)
#if DEBUG || PORTABLE
, "value"
#endif
);
#if false
			if (param_value == null)
				throw new Exception();
#endif

#if WINDOWS_PHONE
			//Debugger.Break(); // PB with Windows Phone 7.1: IsByRef does not exist in ParameterExpression ! Therefore Expression.Field() can not take a ref type parameter.
#endif

			var field = Expression.Field(param_struct, fi); // PB with .NET 3.5 and Windows Phone 7.1: IsByRef does not exist in ParameterExpression ! Therefore Expression.Field() can not take a ref type parameter.
#if false
			if (field == null)
				throw new Exception();
#endif

			var cast_param = Expression.Convert(param_value, fi.FieldType);
#if false
			if (cast_param == null)
				throw new Exception();
#endif

			var assign =
#if !WINDOWS_PHONE && !PORTABLE && !NET3_5
				Expression.Assign
#else
 ExpressionEx.Assign
#endif
(field, cast_param);
#if false
			if (assign == null)
				throw new Exception();
#endif

			var lambda = Expression.Lambda<ValueTypeObjectMemberSetterDelegate<T>>(assign, param_struct, param_value);
#if false
			if (lambda == null)
				throw new Exception();
#endif

			var ret = lambda.Compile();
#if false
			if (ret == null)
				throw new Exception(string.Format("Expression.Compile() {0} returned null.", lambda.GetType().FullName));
#endif

			return ret;
#endif
		}

#if NET3_5 // also a test for WINDOWS_PHONE7_1

		/// <summary>
		/// Windows Phone 7.1 does not provide compile a value set property by ref, so this function make it by IL code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fi"></param>
		/// <returns></returns>
		internal static ValueTypeObjectMemberSetterDelegate<T> GenerateValueTypeFieldSetter_ByIL<T>(FieldInfo fi)
		{
			Type type = typeof(T);
			var this_ref = type.MakeByRefType();
			if (this_ref == null)
			{
				this_ref = Type.GetType(type.FullName + "&", true); // Causes an exception on VS 2010 Phone 7.1 !
			}
			DynamicMethod dynamicSetByRef = new DynamicMethod(
				fi.Name, // important: keep this name. Because the TypeManager will use it later (notably on .NET 3.5).
				typeof(void), new Type[] { this_ref, typeof(object) }/*, type*/);
			ILGenerator il = dynamicSetByRef.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Unbox_Any, fi.FieldType);
			il.Emit(OpCodes.Stfld, fi);
			il.Emit(OpCodes.Ret);
			return (ValueTypeObjectMemberSetterDelegate<T>)dynamicSetByRef.CreateDelegate(typeof(ValueTypeObjectMemberSetterDelegate<T>));
		}

#endif

		// -------------------------------------------

		internal static ValueTypeObjectMemberSetterDelegate<T> GenerateValueTypePropertySetter<T>(PropertyInfo pi)
		{
#if WINDOWS_PHONE7_1 || NET3_5
			return GenerateValueTypePropertySetter_ByIL<T>(pi);
#else

			var this_ref = typeof(T).MakeByRefType();

			var param_struct = Expression.Parameter(this_ref
#if DEBUG || PORTABLE
, "this"
#endif
);
			var param_value = Expression.Parameter(typeof(object)
#if DEBUG || PORTABLE
, "value"
#endif
);
			var Property = Expression.Property(param_struct, pi);
			var cast_param = Expression.Convert(param_value, pi.PropertyType);
			var assign =
#if !WINDOWS_PHONE && !PORTABLE && !NET3_5
				Expression.Assign
#else
 ExpressionEx.Assign
#endif
(Property, cast_param);
			var lambda = Expression.Lambda<ValueTypeObjectMemberSetterDelegate<T>>(assign, param_struct, param_value);

			return lambda.Compile();
#endif
		}


#if WINDOWS_PHONE7_1 || NET3_5
		/// <summary>
		/// Windows Phone 7.1 and .NET 3.5 do not provide compile a value set property by ref, so this function makes it by IL code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="pi"></param>
		/// <returns></returns>
		internal static ValueTypeObjectMemberSetterDelegate<T> GenerateValueTypePropertySetter_ByIL<T>(PropertyInfo pi)
		{
			var this_ref = typeof(T).MakeByRefType();
			Type type = typeof(T);
			DynamicMethod dynamicSetByRef = new DynamicMethod("_", typeof(void), new Type[] { this_ref, typeof(object) }, type);
			ILGenerator il = dynamicSetByRef.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Unbox_Any, pi.PropertyType);
			var propSet = pi.GetSetMethod(true);
			il.EmitCall(OpCodes.Call, propSet, null);
			il.Emit(OpCodes.Ret);
			return (ValueTypeObjectMemberSetterDelegate<T>)dynamicSetByRef.CreateDelegate(typeof(ValueTypeObjectMemberSetterDelegate<T>));
		}

#endif


		// -------------------------------------------

		/// <summary>
		/// Gets the MethodInfo of a not-static method.
		/// No class instance is needed.
		/// Usage: Tools.GetMethodInfo&lt;TClass&gt;(u => u.TheMethod(..))
		/// We need parameters, but they will not be used by this function, so they can be null or default(T).
		/// 
		/// Note 1: you do not need this function to obtain the MethodInfo of a STATIC method:
		/// <code>
		///		Func&lt;int&gt; f = TClass.StaticMethod&lt;int&gt;;
		///		MethodInfo mi = f.Method;
		///	</code>
		///	Or in shorter:
		///	<code>
		///		MethodInfo mf = (()Func&lt;int&gt; TClass.StaticMethod&lt;int&gt;).Method;
		///	</code>
		///		
		/// Note 2: alternative method for NOT-static methods, using a class instance:
		/// <code>
		///		Func&lt;int&gt; f = this.NotStaticMethod&lt;int&gt;; // or Action
		///		MethodInfo mf = f.Method;
		///	</code>
		///	Or in shorter:
		///	<code>
		///		MethodInfo mf = (()Func&lt;int&gt; this.NotStaticMethod&lt;int&gt;).Method;
		///	</code>
		/// </summary>
		internal static MethodInfo GetMethodInfo<TClass>(
				Expression<Func<TClass, object>> expression)
		{
			{
				UnaryExpression ubody = expression.Body as UnaryExpression;
				if (ubody != null)
				{
					var operand = ubody.Operand as System.Linq.Expressions.MethodCallExpression;
					if (operand.Method != null)
						return operand.Method;
				}
			}
			return null;
		}

		// -------------------------------------------

		/// <summary>
		/// Returns true for (List{int},List{}), but false for (MyList{int},List{}) because it is inherited twice.
		/// </summary>
		internal static bool IsADirectGenericOf(this Type objType, Type genericType)
		{
			if (!TypeEx.IsGenericType(objType))
				return false;
			Type t = objType.GetGenericTypeDefinition();
			return t == genericType;
		}

		// -------------------------------------------

		internal static bool Contains<T>(this IEnumerable<T> list, T value)
		{
			foreach (T item in list)
				if (item.Equals(value))
					return true;
			return false;
		}

		// -------------------------------------------

		internal static Func<object> GetNoParamConstructorWithoutCache2(
			Type type, bool ShouldTestConstructor, DefaultConstructorTestCleanerDelegate[] defaultConstructorTestCleaners)
		{
			ConstructorInfo ci =
#if !PORTABLE
			TypeEx.IsPublic(type) ?
			type.GetConstructor(
									BindingFlags.Public | BindingFlags.Instance,
									null,
									Type.EmptyTypes,
									null
									)
			: type.GetConstructor(
								BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
								null,
									Type.EmptyTypes,
								null
								);
#else
 type.GetConstructor(EmptyTypes);
			if (ci == null && !type.IsPublic)
			{
				var constrs = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
				if (constrs.Length > 0)
					ci = constrs.First((ci2) => ci2.GetParameters().Length == 0);
			}
#endif
			if (ci == null)
				return null;
			// Some default constructors only contain a 'throw new Exception', so we test them.
			try
			{
				var f = DefaultConstructorToDelegate(ci);
				if (ShouldTestConstructor)
				{
					object o = f();
					if (defaultConstructorTestCleaners != null)
						foreach (var defaultConstructorTestCleaner in defaultConstructorTestCleaners)
							if (defaultConstructorTestCleaner(o))
								break;
					return o != null ? f : null;
				}
				return f;
			}
			catch
			{
#if DEBUG
				// Pauses on unknown types:
#if SILVERLIGHT
				if (type != typeof(System.Windows.Input.InputScope))
#else
				if (type.Name != "RuntimeType")
#endif
					Debugger.Break();
#endif
				return null;
			}
		}
		private delegate object _CreateObject();
#if PORTABLE
		static Type[] EmptyTypes = new Type[0];
#endif

		// -------------------------------------------

		/// <summary>
		/// Create a Delegate from a ConstructorInfo.
		/// Equivalent to a IL emission, but more portable.
		/// Compatible with Silverlight 4.
		/// </summary>
		/// <param name="ci"></param>
		/// <returns></returns>
		internal static Func<object> DefaultConstructorToDelegate(System.Reflection.ConstructorInfo ci)
		{
			var lambda = System.Linq.Expressions.Expression.Lambda<Func<object>>(
							System.Linq.Expressions.Expression.New(ci),
#if DEBUG && !WINDOWS_PHONE && !PORTABLE && !NET3_5
 ci.DeclaringType.Name + "_defaultConstructor",
#endif
 new System.Linq.Expressions.ParameterExpression[0]);
			var func = lambda.Compile();
			return func;
		}

		// -------------------------------------------

		/// <summary>
		/// Create a Delegate from a ConstructorInfo.
		/// Equivalent to a IL emission, but more portable.
		/// Compatible with Silverlight 4.
		/// </summary>
		/// <param name="ci"></param>
		/// <returns></returns>
		internal static Func<P1, R> Param1ConstructorToDelegate<P1, R>(System.Reflection.ConstructorInfo ci)
		{
			var param1 = System.Linq.Expressions.Expression.Parameter(typeof(P1)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var pars = new System.Linq.Expressions.ParameterExpression[] { param1 };
			var lambda = System.Linq.Expressions.Expression.Lambda<Func<P1, R>>(
							System.Linq.Expressions.Expression.New(ci, pars),
#if DEBUG && !WINDOWS_PHONE && !PORTABLE && !NET3_5
 ci.DeclaringType.Name + "_defaultConstructor",
#endif
 pars);
			var func = lambda.Compile();
			return func;
		}

		// -------------------------------------------

		/// <summary>
		/// Create a Delegate from a ConstructorInfo.
		/// Equivalent to a IL emission, but more portable.
		/// Compatible with Silverlight 4.
		/// </summary>
		/// <param name="ci"></param>
		/// <returns></returns>
		internal static Func<P1, P2, R> Param2ConstructorToDelegate<P1, P2, R>(System.Reflection.ConstructorInfo ci)
		{
			var param1 = System.Linq.Expressions.Expression.Parameter(typeof(P1)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param2 = System.Linq.Expressions.Expression.Parameter(typeof(P2)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var pars = new System.Linq.Expressions.ParameterExpression[] { param1, param2 };
			var lambda = System.Linq.Expressions.Expression.Lambda<Func<P1, P2, R>>(
							System.Linq.Expressions.Expression.New(ci, pars),
#if DEBUG && !WINDOWS_PHONE && !PORTABLE && !NET3_5
 ci.DeclaringType.Name + "_Constructor",
#endif
 pars);
			var func = lambda.Compile();
			return func;
		}

		// -------------------------------------------

		/// <summary>
		/// Create a Delegate from a ConstructorInfo.
		/// Equivalent to a IL emission, but more portable.
		/// Compatible with Silverlight 4.
		/// </summary>
		/// <param name="ci"></param>
		/// <returns></returns>
		internal static Func<P1, P2, P3, P4, P5, P6, R> Param6ConstructorToDelegate<P1, P2, P3, P4, P5, P6, R>(System.Reflection.ConstructorInfo ci)
		{
			var param1 = System.Linq.Expressions.Expression.Parameter(typeof(P1)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param2 = System.Linq.Expressions.Expression.Parameter(typeof(P2)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param3 = System.Linq.Expressions.Expression.Parameter(typeof(P3)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param4 = System.Linq.Expressions.Expression.Parameter(typeof(P4)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param5 = System.Linq.Expressions.Expression.Parameter(typeof(P5)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param6 = System.Linq.Expressions.Expression.Parameter(typeof(P6)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var pars = new System.Linq.Expressions.ParameterExpression[] { param1, param2, param3, param4, param5, param6 };
			var constr = System.Linq.Expressions.Expression.New(ci, pars);
			var lambda = System.Linq.Expressions.Expression.Lambda<Func<P1, P2, P3, P4, P5, P6, R>>(
							constr,
#if DEBUG && !WINDOWS_PHONE && !PORTABLE && !NET3_5
 ci.DeclaringType.Name + "_Constructor",
#endif
 pars);
			var func = lambda.Compile();
			return func;
		}

#if WINDOWS_PHONE || PORTABLE || NET3_5
		/// <summary>
		/// Func delegate with 6 parameters.
		/// </summary>
		/// <typeparam name="P1"></typeparam>
		/// <typeparam name="P2"></typeparam>
		/// <typeparam name="P3"></typeparam>
		/// <typeparam name="P4"></typeparam>
		/// <typeparam name="P5"></typeparam>
		/// <typeparam name="P6"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <param name="p4"></param>
		/// <param name="p5"></param>
		/// <param name="p6"></param>
		/// <returns></returns>
		public delegate TResult Func<P1, P2, P3, P4, P5, P6, TResult>(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6);
#endif

		// -------------------------------------------

		/// <summary>
		/// Create a Delegate from a ConstructorInfo.
		/// Equivalent to a IL emission, but more portable.
		/// Compatible with Silverlight 4.
		/// </summary>
		/// <param name="ci"></param>
		/// <returns></returns>
		internal static Func<P1, P2, P3, P4, P5, R> Param5ConstructorToDelegate<P1, P2, P3, P4, P5, R>(System.Reflection.ConstructorInfo ci)
		{
			var param1 = System.Linq.Expressions.Expression.Parameter(typeof(P1)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param2 = System.Linq.Expressions.Expression.Parameter(typeof(P2)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param3 = System.Linq.Expressions.Expression.Parameter(typeof(P3)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param4 = System.Linq.Expressions.Expression.Parameter(typeof(P4)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param5 = System.Linq.Expressions.Expression.Parameter(typeof(P5)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var pars = new System.Linq.Expressions.ParameterExpression[] { param1, param2, param3, param4, param5 };
			var constr = System.Linq.Expressions.Expression.New(ci, pars);
			var lambda = System.Linq.Expressions.Expression.Lambda<Func<P1, P2, P3, P4, P5, R>>(
							constr,
#if DEBUG && !WINDOWS_PHONE && !PORTABLE && !NET3_5
 ci.DeclaringType.Name + "_Constructor",
#endif
 pars);
			var func = lambda.Compile();
			return func;
		}

#if WINDOWS_PHONE || PORTABLE || NET3_5
		/// <summary>
		/// Func delegate with 5 parameters.
		/// </summary>
		/// <typeparam name="P1"></typeparam>
		/// <typeparam name="P2"></typeparam>
		/// <typeparam name="P3"></typeparam>
		/// <typeparam name="P4"></typeparam>
		/// <typeparam name="P5"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <param name="p4"></param>
		/// <param name="p5"></param>
		/// <returns></returns>
		public delegate TResult Func<P1, P2, P3, P4, P5, TResult>(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);
#endif

		// -------------------------------------------

		/// <summary>
		/// Create a Delegate from a ConstructorInfo.
		/// Equivalent to a IL emission, but more portable.
		/// Compatible with Silverlight 4.
		/// </summary>
		/// <param name="ci"></param>
		/// <returns></returns>
		internal static Func<P1, P2, P3, P4, R> Param4ConstructorToDelegate<P1, P2, P3, P4, R>(System.Reflection.ConstructorInfo ci)
		{
			var param1 = System.Linq.Expressions.Expression.Parameter(typeof(P1)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param2 = System.Linq.Expressions.Expression.Parameter(typeof(P2)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param3 = System.Linq.Expressions.Expression.Parameter(typeof(P3)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var param4 = System.Linq.Expressions.Expression.Parameter(typeof(P4)
#if WINDOWS_PHONE || PORTABLE || NET3_5
, string.Empty
#endif
);
			var pars = new System.Linq.Expressions.ParameterExpression[] { param1, param2, param3, param4 };
			var constr = System.Linq.Expressions.Expression.New(ci, pars);
			var lambda = System.Linq.Expressions.Expression.Lambda<Func<P1, P2, P3, P4, R>>(
							constr,
#if DEBUG && !WINDOWS_PHONE && !PORTABLE && !NET3_5
 ci.DeclaringType.Name + "_Constructor",
#endif
 pars);
			var func = lambda.Compile();
			return func;
		}

		// -------------------------------------------

		internal static bool IsEmpty(this IEnumerable collection)
		{
			{
				var coll = collection as ICollection;
				if (coll != null)
					return coll.Count == 0;
			}

			var enumerator = collection.GetEnumerator();
			return !enumerator.MoveNext();
		}

		// -------------------------------------------

		internal static int GetCount(this IEnumerable collection)
		{
			{
				var coll = collection as ICollection;
				if (coll != null)
					return coll.Count;
			}

			var enumerator = collection.GetEnumerator();
			int i = 0;
			while (enumerator.MoveNext()) // Very slow, but what else ?
				i++;
			return i;
		}

		// -------------------------------------------

		/// <summary>
		/// Get item type of an IEnumerable, if it is a generic IEnumerable as well.
		/// </summary>
		internal static Type GetCollectionItemType(Type collectionType)
		{
			if (collectionType.IsArray)
				return collectionType.GetElementType();
			Type genericIEnumerable = collectionType.FindDerivedOrEqualToThisType(typeof(IEnumerable<>));
			return
				(genericIEnumerable != null) ?
				genericIEnumerable.GetGenericArguments()[0]
				: null;
		}

		// -------------------------------------------

		/// <summary>
		/// Get key and value types of an IDictionary&lt;,&gt;.
		/// </summary>
		internal static void GetKeyAndValueTypes(Type collectionType, out Type KeyType, out Type ValueType)
		{
			Type genericIDictionary = collectionType.FindDerivedOrEqualToThisType(typeof(IDictionary<,>));

			if (genericIDictionary != null)
			{
				KeyType = genericIDictionary.GetGenericArguments()[0];
				ValueType = genericIDictionary.GetGenericArguments()[1];
			}
			KeyType = ValueType = null;
		}

		// -------------------------------------------

		/// <summary>
		/// Get generic type short name.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		internal static string GetGenericTypeName(this Type t)
		{
			if (!TypeEx.IsGenericType(t))
				return t.Name;
			var pars = t.GetGenericArguments();
			StringBuilder ret = new StringBuilder(t.Name.Substring(0, t.Name.IndexOf('`')));
			ret.Append('<');
			bool append = false;
			foreach (var par in pars)
			{
				if (append)
					ret.Append(',');
				ret.Append(par.GetGenericTypeName());
				append = true;
			}
			ret.Append('>');
			return ret.ToString();
		}

		// -------------------------------------------

		/// <summary>
		/// Returns true if the object type inherits the searched type
		/// Returns false if the type are the same.
		/// </summary>
		internal static bool Inherits(this Type ObjectType, Type SearchedType)
		{
			return FindDerivedToThisType(ObjectType, SearchedType) != null;
		}

		// -------------------------------------------

		internal static Type FindDerivedToThisType(this Type TheObjectType, Type SearchedType)
		{
			if (SearchedType == TypeTools.Types.ObjectType)
				return SearchedType; // optimisation: All types are objects.

			Type ret;
			KeyValuePair<Type, Type> searched = new KeyValuePair<Type, Type>(TheObjectType, SearchedType);
			if (!_FindDerivedToThisTypeCache.TryGetValue(searched, out ret))
			{
				ret = _FindDerivedToThisType(TheObjectType, SearchedType);
				_FindDerivedToThisTypeCache.Add(searched, ret);
			}
			return ret;
		}
		internal static Type _FindDerivedToThisType(Type ObjectType, Type SearchedType)
		{
			if (TypeEx.IsInterface(SearchedType))
			{
				if (TypeEx.IsGenericType(ObjectType) && !TypeEx.IsGenericTypeDefinition(ObjectType) && (ObjectType.GetGenericTypeDefinition() == SearchedType))
					return SearchedType;
				var ints = ObjectType.GetInterfaces();
				return ContainsThisType(ints, SearchedType);
			}
			else // NOT an interface: --------------------------
			{
				bool lookForAPureGenericWithoutDefinedParameter =
					TypeEx.IsGenericType( SearchedType) && TypeEx.ContainsGenericParameters(SearchedType);

				if (!lookForAPureGenericWithoutDefinedParameter)
				{
					Type tparent = TypeEx.BaseType(ObjectType);
					while (tparent != null)
					{
						if (tparent == SearchedType)
							return tparent;
						tparent = TypeEx.BaseType(tparent);
					}
				}
				else
				{
#if true // new algo 2016-10-08
					Type t = ObjectType;
					while (t != null)
					{
						if (TypeEx.IsGenericType(t) && t.GetGenericTypeDefinition() == SearchedType)
							return t;
						t = TypeEx.BaseType(t);
					}
#else // old algo
					Type previous = ObjectType;
					Type tparent = ObjectType.IsGenericType ? ObjectType.GetGenericTypeDefinition() : ObjectType;
					while (tparent != null)
					{
						if (tparent == SearchedType)
							return previous;// tparent;
						// next:
						previous = tparent;
						tparent = tparent.BaseType;
						if (tparent != null)
						{
							if (tparent.IsGenericType)
								tparent = tparent.GetGenericTypeDefinition();
						}
					}
#endif
				}
			}
			return null;
		}
		static Dictionary<KeyValuePair<Type, Type>, Type> _FindDerivedToThisTypeCache
			= new Dictionary<KeyValuePair<Type, Type>, Type>();

		// -------------------------------------------

		/// <summary>
		/// Set a value in the list, even if it is not defined yet.
		/// Missing indexes are filled with default(T).
		/// </summary>
		/// <param name="index">Index of the value to be modified.</param>
		/// <param name="list"></param>
		/// <param name="Value">The value.</param>
		/// <returns></returns>
		internal static void SecurelySetIndex<T>(this IList<T> list, int index, T Value)
		{
			if (index < 0)
				throw new ArgumentException();
			if (index < list.Count)
				list[index] = Value;

			// Il manque des cases dans cette liste, on doit les créer pour pouvoir modifier celle-ci.
			list.FillUntilIndex(index - 1);

			list.Add(Value); // Maintenant, on peut modifier cette case, car elle existe.
		}

		// -------------------------------------------

		internal static void FillUntilIndex<T>(
			this IList<T> list, int index)
		{
			Type t = typeof(T);
			for (int i = list.Count; i <= index; i++)
				list.Add(default(T));
		}

		// -------------------------------------------

		internal static void SetMinimalSize<T>(
			this IList<T> list, int index)
		{
			list.FillUntilIndex(index - 1);
		}

		// -------------------------------------------

		/// <summary>
		/// Returns true if the types are equal or if the object type inherits the searched type.
		/// This is equivalent to C# 'is', but for Type.
		/// </summary>
		public static bool TypeIs(Type ObjectType, Type SearchedType)
		{
			return ObjectType.FindDerivedOrEqualToThisType(SearchedType) != null;
		}

		// -------------------------------------------

		/// <summary>
		/// Returns true if the types are the equal or if the objetc type inherits the searched type
		/// </summary>
		internal static bool Is(this Type ObjectType, Type SearchedType)
		{
			return ObjectType.FindDerivedOrEqualToThisType(SearchedType) != null;
		}

		// -------------------------------------------

		/// <summary>
		/// Returns true if the types are the equal or if the object type inherits the searched type
		/// From the AssemblyQualifiedName or the FullName.
		/// </summary>
		internal static bool Is(this Type ObjectType, string SearchedTypeFullName)
		{
			return ObjectType.FindDerivedOrEqualToThisType(GetTypeFromFullName(SearchedTypeFullName)) != null;
		}

#if !NETFX_CORE
		internal static bool IsClass(this Type t)
		{
			return t.IsClass;
		}

		internal static bool IsValueType(this Type t)
		{
			return t.IsValueType;
		}

		internal static TypeCode GetTypeCode(this Type t)
		{
			return Type.GetTypeCode(t);
		}
#endif

		internal static Assembly GetAssembly(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().Assembly;
#else
			return t.Assembly;
#endif
		}


		/// <summary>
		/// From the AssemblyQualifiedName or the FullName.
		/// </summary>
		public static Type GetTypeFromFullName(string TypeFullName)
		{
			Type ret;
			if (!_GetTypeFromFullNameCache.TryGetValue(TypeFullName, out ret))
			{
				ret = _GetTypeFromFullName(TypeFullName);
				_GetTypeFromFullNameCache.Add(TypeFullName, ret);
			}
			return ret;
		}

		/// <summary>
		/// From the AssemblyQualifiedName or the FullName.
		/// </summary>
		static Type _GetTypeFromFullName(string TypeFullName)
		{
			var searchedType = Type.GetType(TypeFullName);
			if (searchedType == null)
			{
				foreach (var assem in FrameworkTools.Framework.Assemblies.Value)
				{
					searchedType =
						assem.GetType(TypeFullName);
					if (searchedType != null)
						break;
				}
			}
			return searchedType;
		}
		static Dictionary<string, Type> _GetTypeFromFullNameCache = new Dictionary<string, Type>(100);

		// -------------------------------------------

		/// <summary>
		/// Returns the FieldInfo from the field name.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		static public FieldInfo FieldInfoFromName(Type t, string name)
		{
			return t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// -------------------------------------------

		/// <summary>
		/// Get type name including generic parameters.
		/// </summary>
		static public string GetName(this Type t)
		{
			return Tools.GetNameAsCSharpStyle(t);
		}

		/// <summary>
		/// Get type name including generic parameters.
		/// Uses C# style.
		/// </summary>
		static public string GetNameAsCSharpStyle(this Type t)
		{
			if (t == null)
				return null;
			return LanguageStyle("<", ">", t);
		}

		/// <summary>
		/// Get type name including generic parameters.
		/// Uses VB style.
		/// </summary>
		static public string GetNameAsVBStyle(this Type t)
		{
			if (t == null)
				return null;
			return LanguageStyle("(Of ", ")", t);
		}

		internal static string LanguageStyle(string prefix, string suffix, Type type)
		{
			var gts = type.GetGenericArguments();
			if (gts.Length > 0)
			{
				int cut = type.Name.IndexOf('`');
				string mainName =
				 (cut < 0) ?
				 type.Name :
				 type.Name.Substring(0, cut);
				StringBuilder sb = new StringBuilder(mainName);
				sb.Append(prefix);
				bool pendingElement = false;
				foreach (var param in gts)
				{
					if (pendingElement)
						sb.Append(", ");
					sb.Append(LanguageStyle(prefix, suffix, param));
					pendingElement = true;
				}
				sb.Append(suffix);
				return sb.ToString();
			}
			else
				return type.Name;
		}

		// -------------------------------------------

		/// <summary>
		/// Returns the derived type of ObjectType that correspond to SearchType.
		/// Also works with interfaces and generic types.
		/// </summary>
		/// <returns></returns>
		public static Type DerivedType(Type ObjectType, Type SearchedType)
		{
			return ObjectType.FindDerivedOrEqualToThisType(SearchedType);
		}

		// -------------------------------------------

		/// <summary>
		/// Returns the meeting SearchedType in the inheritance path of ObjectType.
		/// Returns null of none.
		/// Example: FindDerivedOrEqualToThisType(typeof(List&lt;int&gt;),typeof(List&lt;&gt;)) returns typeof(List&lt;int&gt;).
		/// </summary>
		/// <param name="ObjectType">The type to be analysed.</param>
		/// <param name="SearchedType">Searched type. Can be generic with a type (as List&lt;int&gt;) of nothing (as List&lt;&gt;).</param>
		/// <returns></returns>
		public static Type FindDerivedOrEqualToThisType(this Type ObjectType, Type SearchedType)
		{
			if (ObjectType == SearchedType)
				return ObjectType;
			return ObjectType.FindDerivedToThisType(SearchedType);
		}

		// -------------------------------------------

		/// <summary>
		/// From the AssemblyQualifiedName or the FullName.
		/// </summary>
		public static Type FindDerivedOrEqualToThisType(this Type ObjectType, String SearchedTypeFullName)
		{
			if (ObjectType.FullName == SearchedTypeFullName)
				return ObjectType;
			return ObjectType.FindDerivedToThisType(GetTypeFromFullName(SearchedTypeFullName));
		}

		// -------------------------------------------

		static Type ContainsThisType(Type[] array, Type value)
		{
			int length = array.Length;
			for (int i = 0; i < length; i++)
			{
				Type item = array[i];
				Type comparableItem =
					(TypeEx.IsGenericTypeDefinition(value) && TypeEx.IsGenericType(item) && !TypeEx.IsGenericTypeDefinition(item)) ?
					item.GetGenericTypeDefinition()
					: item;

				if (
					(comparableItem == value)
					||
					((comparableItem.FullName == value.FullName)
#if !PORTABLE
 && (TypeEx.GUID( comparableItem) == TypeEx.GUID(value))
#endif
))
					return item;
			}
			return null;
		}
		// -------------------------------------------

		internal static string TranscodeToXmlCompatibleString(string s)
		{
			if (s == null)
				return s;

			if (s.Length <= 10)
			{
				s = s.Replace("&", "&amp;");
				s = s.Replace("\b", "&#8;"/*string.Empty*/);// "&#8;" is a illegal character in xml. We remove it.
				s = s.Replace("\f", string.Empty);// "&#12;" is a illegal character in xml. We remove it.
				s = s.Replace("\r", "&#13;");
				s = s.Replace("<", "&lt;");
				s = s.Replace(">", "&gt;");
				s = s.Replace("\"", "&quot;");
				s = s.Replace("'", "&apos;");// "&#39;");
				return s;
			}

			StringBuilder sb = new StringBuilder(s);
			sb.Replace("&", "&amp;");
			sb.Replace("\b", string.Empty);// "&#8;" is a illegal character in xml. We remove it.
			sb.Replace("\f", string.Empty);// "&#12;" is a illegal character in xml. We remove it.
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			sb.Replace("\"", "&quot;");
			sb.Replace("'", "&apos;");//"&#39;");
			return sb.ToString();
		}

		// -------------------------------------------

		internal static string TranscodeToJSONCompatibleString(string s)
		{
			if (s == null)
				return s;

			// Escape list generated from System.Runtime.Serialization.Json.JsonReaderWriterFactory .

			if (s.Length <= 10)
			{
				s = s.Replace(@"\", @"\\"); // '\' -> '\\'
				s = s.Replace("\b", @"\b"); // 8 (backspace) -> '\b'
				s = s.Replace("\t", @"\t"); // 9 (tab) -> '\t'
				s = s.Replace("\n", @"\n"); // (new line, string depends on O.S.) -> '\n'
				s = s.Replace("\f", @"\f"); // 12 (formfeed) -> '\f'
				s = s.Replace("\r", @"\r"); // 13 (Carriage return, code depends on O.S.) -> '\r'
				s = s.Replace("\"", "\\\""); // '' -> '\"'
				return s;
			}

			StringBuilder sb = new StringBuilder(s);
			sb.Replace(@"\", @"\\"); // '\' -> '\\'
			sb.Replace("\b", @"\b"); // 8 (backspace) -> '\b'
			sb.Replace("\t", @"\t"); // 9 (tab) -> '\t'
			sb.Replace("\n", @"\n"); // (new line, string depends on O.S.) -> '\n'
			sb.Replace("\f", @"\f"); // 12 (formfeed) -> '\f'
			sb.Replace("\r", @"\r"); // 13 (Carriage return, code depends on O.S.) -> '\r'
			sb.Replace("\"", "\\\""); // '' -> '\"'
			return sb.ToString();
		}

		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------

	}

	// ###############################################################
	// ###############################################################

#if WINDOWS_PHONE || PORTABLE || NET3_5

	/// <summary>
	/// A simplified, mono-thread, version of System.Lazy, for Windows Phone and PCL.
	/// </summary>
	public class Lazy<T> : SimpleLazy<T>
	{
		/// <summary>
		/// Lazy constructor taking a definition function.
		/// </summary>
		/// <param name="valueFactory">The definition function.</param>
		public Lazy(Func<T> valueFactory)
			: base(valueFactory)
		{
		}
	}

#endif

	/// <summary>
	/// A simplified version of System.Lazy, for Windows Phone and PCL.
	/// Based on Mono, but thread management has been removed.
	/// </summary>
	public class SimpleLazy<T>
	{
		T value;
		Func<T> factory;
		Exception exception;
		bool inited;

		/// <summary>
		/// Builds from a function.
		/// </summary>
		/// <param name="valueFactory"></param>
		public SimpleLazy(Func<T> valueFactory)
		{
			/*this.inited = false;
			this.exception = null;
			this.value = default(T);*/
			if (valueFactory == null)
				throw new ArgumentNullException("valueFactory");
			this.factory = valueFactory;
		}

		/// <summary>
		/// Gets the evaluated value.
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Value
		{
			get
			{
				if (inited)
					return value;
				if (exception != null)
					throw exception;

				return InitValue();
			}
		}

		T InitValue()
		{
			Func<T> init_factory;
			T v;

			{
					init_factory = factory;
					if (init_factory == null)
						throw exception = new InvalidOperationException("The initialization function tries to access Value on this instance");
					{
						factory = null;
						v = init_factory();
						value = v;
						inited = true;
					}
			}

			return value;
		}

		/// <summary>
		/// Returns true if the function has already been evaluated.
		/// </summary>
		public bool IsValueCreated
		{
			get
			{
				return inited;
			}
		}

		/// <summary>
		/// Gets the value as a string, or "&lt;Value is not evaluated&gt;".
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (inited)
				return value.ToString();
			else
				return "<Value is not evaluated>";
		}
	}		


	// ###############################################################
	// ###############################################################

#if WINDOWS_PHONE || PORTABLE || NET3_5
	// May be useful for .NET 3.5 also..
	/// <summary>
	/// Tree Expression extensions.
	/// </summary>
	public static class ExpressionEx
	{
		/// <summary>
		/// Assign copies a value of an Expression to another Expression.
		/// Internally, we use Expression.Add().
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static BinaryExpression Assign(Expression left, Expression right)
		{
			var assign = typeof(Assigner<>).MakeGenericType(left.Type).GetMethod("Assign");

			var assignExpr = Expression.Add(left, right, assign);

			return assignExpr;
		}

		private static class Assigner<T>
		{
			public static T Assign(ref T left, T right)
			{
				return (left = right);
			}
		}
	}
#endif

#if false // possible future use
	//http://allwpf.blogspot.fr/2009/11/in-project-i-currently-working-on-which.html
public static class ExpressionExtenstions
    {
        private class AssignmentHelper<TPropType>
        {
            private static void SetValue(ref TPropType target, TPropType value)
            {
                target = value;
            }

            internal static MethodInfo MethodInfoSetValue =
                typeof(AssignmentHelper<TPropType>).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Static);
        }
	
				public static Expression<Action<TOwner, TPropType>> ToFieldAssignExpression<TOwner, TPropType>
            (this Expression<Func<TOwner, TPropType>> fieldGetter)
        {
            if (fieldGetter == null)
                throw new ArgumentNullException("fieldGetter");

            if(fieldGetter.Parameters.Count != 1 || !(fieldGetter.Body is MemberExpression))
                throw new ArgumentException ("Input expression must be a single parameter field getter, e.g. g => g._fieldToSet  or function(g) g._fieldToSet");

            ParameterExpression[] parms = new ParameterExpression[] {
                                        fieldGetter.Parameters[0],
                                        Expression.Parameter(typeof(TPropType), "value")};

            Expression body = Expression.Call(AssignmentHelper<TPropType>.MethodInfoSetValue, new Expression[] { fieldGetter.Body, parms[1] });

            return Expression.Lambda<Action<TOwner, TPropType>>(body, parms);
        }


				public static Action<TOwner, TPropType> ToFieldAssignment<TOwner, TPropType>
            (
                this Expression<Func<TOwner, TPropType>> fieldGetter
            )
        {
            return fieldGetter.ToFieldAssignExpression().Compile();
        }
    }
#endif

	// ###############################################################
	// ###############################################################

}

// ###############################################################
// ###############################################################

namespace UniversalSerializerLib3.TypeTools
{
	// ------------------------------


	internal static class Types
	{
		// ------------------------------

		internal static Type[] PrimitiveTypes = new Type[] {
			null, //Empty = 0,
			typeof(Object), // = 1,
#if !PORTABLE
			typeof(DBNull), // = 2,
#else
			null, // = 2, // No DBNull in that framework.
#endif
			typeof(Boolean), // = 3,
			typeof(Char), // = 4,
			typeof(SByte), // = 5,
			typeof(Byte), // = 6,
			typeof(Int16), // = 7,
			typeof(UInt16), // = 8,
			typeof(Int32), // = 9,
			typeof(UInt32), // = 10,
			typeof(Int64), // = 11,
			typeof(UInt64), // = 12,
			typeof(Single), // = 13,
			typeof(Double), // = 14,
			typeof(Decimal), // = 15,
			typeof(DateTime), // = 16,
			typeof(String), // = 18,		
		};
		internal static Type ObjectType = typeof(object);

		// ------------------------------

		/// <summary>
		/// Returns all private and public fields including the inherited fields.
		/// The list is sorted by name.
		/// The reason for this function is .NET's GetFields() returns inherited public types but not inherited private types.
		/// </summary>
		/// <param name="type"></param>
		/// <returns>The fields, sorted by name.</returns>
		internal static IEnumerable<FieldInfo> GetPrivateAndPublicFieldsIncludingInherited(this Type type)
		{
			return _GetFieldsIncludingInherited(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		}

		// ------------------------------

		/// <summary>
		/// Returns all private fields including the inherited fields.
		/// The list is sorted by name.
		/// The reason for this function is .NET's GetFields() returns inherited public types but not inherited private types.
		/// </summary>
		/// <param name="type"></param>
		/// <returns>The fields, sorted by name.</returns>
		internal static IEnumerable<FieldInfo> GetPrivateFieldsIncludingInherited(this Type type)
		{
			return _GetFieldsIncludingInherited(type, BindingFlags.Instance | BindingFlags.NonPublic);
		}

		// ------------------------------

		/// <summary>
		/// Returns all fields including the inherited fields.
		/// The list is sorted by name.
		/// The reason for this function is .NET's GetFields() returns inherited public types but not inherited private types.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns>The fields, sorted by name.</returns>
		static IEnumerable<FieldInfo> _GetFieldsIncludingInherited(Type type, BindingFlags flags)
		{
			var list = new Dictionary<string, FieldInfo>();

			Type t = type;
			do
			{
				foreach (var fi in t.GetFields(flags))
				{
					if (!list.ContainsKey(fi.Name))
						list.Add(fi.Name, fi);
				}
				t = TypeEx.BaseType(t);
			} while (t != null);

			return list.Select((kvp) => kvp.Value).OrderBy(fi => fi.Name);
		}

		// ------------------------------

		internal static bool MemberIsDeclaredAsForceSerializeAttribute(MemberInfo mi)
		{
			var att = mi.GetCustomAttributes(typeof(ForceSerializeAttribute), false).ToArray(); // ToArray for UWP.
			return (att != null && att.Length > 0);
		}

		// ------------------------------

		internal static bool MemberIsDeclaredAsForceNotSerializeAttribute(MemberInfo mi)
		{
			var att = mi.GetCustomAttributes(typeof(ForceNotSerializeAttribute), false).ToArray(); // ToArray for UWP.
			return (att != null && att.Length > 0);
		}

		// ------------------------------

		internal static bool FieldIsDeclaredAsNotSerializableByNonSerializedAttribute(FieldInfo fi)
		{
#if !PORTABLE && !SILVERLIGHT && !WINDOWS_UWP
			object[] att = fi.GetCustomAttributes(typeof(System.NonSerializedAttribute), false);
			return (att != null && att.Length > 0);
#else
			return false;
#endif
		}

		// ------------------------------

		internal static bool MemberIsDeclaredAsNotSerializableByXmlIgnoreAttribute(MemberInfo mi)
		{
#if !WINDOWS_UWP
			object[] att = mi.GetCustomAttributes(typeof(System.Xml.Serialization.XmlIgnoreAttribute), false);
			return (att != null && att.Length > 0);
#else
			return false;
#endif
		}

		// ------------------------------

		internal static bool MemberIsDeclaredAsNotEditorBrowsable(MemberInfo mi)
		{
#if !WINDOWS_UWP
			// Look for "[EditorBrowsable(EditorBrowsableState.Never)]".
			object[] att = mi.GetCustomAttributes(typeof(System.ComponentModel.EditorBrowsableAttribute), false);
			if (att != null && att.Length > 0)
			{
				foreach (var at in att)
				{
					var ba = at as System.ComponentModel.EditorBrowsableAttribute;
					if (ba != null && ba.State == System.ComponentModel.EditorBrowsableState.Never)
						return true;
				}
			}
#endif
			return false;
		}

		// ------------------------------

		internal static int GetEnumValuesCount(this Type EnumType)
		{
			if (!TypeEx.IsEnum( EnumType))
				throw new ArgumentException("Type is not an enumeration", "enumType");

			var fields = EnumType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			return fields.Length;
		}

#if !WINDOWS_PHONE && !PORTABLE
		internal static string[] GetEnumNames(this Type EnumType)
		{
			return Enum.GetNames(EnumType);
		}

		internal static Array GetEnumValues(this Type EnumType)
		{
			return Enum.GetValues(EnumType);
		}
		internal static TEnum[] GetEnumValues<TEnum>()
		{
			return (TEnum[])Enum.GetValues(typeof(TEnum));
		}
#else
		/// <summary>
		/// From Mono.
		/// </summary>
		/// <param name="EnumType"></param>
		/// <returns></returns>
		internal static string[] GetEnumNames(this Type EnumType)
		{
			if (!EnumType.IsEnum)
				throw new ArgumentException("Type is not an enumeration", "enumType");

			var fields = EnumType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

			string[] names = new string[fields.Length];
			if (0 != names.Length)
			{
				for (int i = 0; i < fields.Length; ++i)
					names[i] = fields[i].Name;

				return names.OrderBy(s => s).ToArray();
			}

			return names;
		}

		/// <summary>
		/// From Mono.
		/// </summary>
		/// <returns></returns>
		internal static Array GetEnumValues(this Type EnumType)
		{
			if (!EnumType.IsEnum)
				throw new ArgumentException("Type is not an enumeration", "enumType");

			var fields = EnumType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

			var values = Array.CreateInstance(EnumType, fields.Length);

			for (int i = 0; i < fields.Length; ++i)
			{
				values.SetValue(Types.GetRawConstantValueOfFieldInfo(fields[i]), i);
			}
			return values;
		}

		internal static TEnum[] GetEnumValues<TEnum>()
		{
			var EnumType = typeof(TEnum);
			if (!EnumType.IsEnum)
				throw new ArgumentException("Type is not an enumeration", "enumType");

			var fields = EnumType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

			var values = (TEnum[])Array.CreateInstance(EnumType, fields.Length);

			for (int i = 0; i < fields.Length; ++i)
			{
				values.SetValue((TEnum)Types.GetRawConstantValueOfFieldInfo(fields[i]), i);
			}
			return values;
		}
#endif

#if PORTABLE || WINDOWS_PHONE7_1 || NETFX_CORE
    // WP7.1 has Enum.GetRawConstantValue(), but it is wrong and causes an exception.
    internal static object GetRawConstantValueOfFieldInfo(FieldInfo fieldInfo)
	{
		// from http://code.google.com/p/protobuf-net/source/browse/trunk/protobuf-net/Meta/ValueMember.cs. License: Apache 2.
		object value = fieldInfo.GetValue(null);
		switch(Enum.GetUnderlyingType(fieldInfo.FieldType).GetTypeCode())
		{
			case TypeCode.SByte: return (sbyte)value;
			case TypeCode.Byte: return (byte)value;
			case TypeCode.Int16: return (short)value;
			case TypeCode.UInt16: return (ushort)value;
			case TypeCode.Int32: return (int)value;
			case TypeCode.UInt32: return (uint)value;
			case TypeCode.Int64: return (long)value;
			case TypeCode.UInt64: return (ulong)value;
			default:
#if DEBUG
			throw new InvalidOperationException();
#else
				return null; // only for compilation.
#endif
		}
	}
#else
		internal static object GetRawConstantValueOfFieldInfo(FieldInfo fieldInfo)
		{
			return fieldInfo.GetRawConstantValue();
		}
#endif

	}
}

// ###############################################################
		// ###############################################################


namespace UniversalSerializerLib3.FileTools
{
	internal static class Files
	{ }

#if false
	/// <summary>
	/// Allows use of protected Read7BitEncodedInt().
	/// </summary>
	internal class BinaryReader2 : BinaryReader
	{
		public BinaryReader2(Stream input)
			: base(input)
		{ }

		public BinaryReader2(Stream input, Encoding encoding)
			: base(input,encoding)
		{	}


		/// <summary>
		/// Reads compressed short from stream.
		/// </summary>
		/// <returns></returns>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal short Read7BitEncodedShort()
		{
			return unchecked((short)this.Read7BitEncodedUShort());
		}

		/// <summary>
		/// Reads compressed short from stream using special encoding.
		/// </summary>
		/// <returns></returns>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal short ReadSpecial7BitEncodedShort()
		{
			return unchecked(Numbers.Special16ToInt16((short)this.Read7BitEncodedUShort()));
		}

		/// <summary>
		/// Reads compressed ushort from stream.
		/// </summary>
		/// <returns></returns>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal ushort Read7BitEncodedUShort()
		{
			ushort retValue = 0;
			int shifter = 0;

			while (shifter != 21) // 21 because 7*3 > 16 (bits).
			{
				byte b = this.ReadByte();
				retValue |= (ushort)((ushort)(b & 0x7f) << shifter);
				shifter += 7;
				if ((b & 0x80) == 0)
				{
					return retValue;
				}
			}
#if DEBUG
			throw new FormatException();
#else
			return 0;
#endif
		}

		/// <summary>
		/// Reads a compressed integer from the stream.
		/// </summary>
		/// <returns></returns>
		public new int Read7BitEncodedInt()
		{
			return base.Read7BitEncodedInt(); // go to the protected method.
		}

		/// <summary>
		/// Reads a compressed integer from the stream using special encoding.
		/// </summary>
		/// <returns></returns>
		public int ReadSpecial7BitEncodedInt()
		{
			return Numbers.Special32ToInt32(base.Read7BitEncodedInt()); // go to the protected method.
		}

		/// <summary>
		/// Reads a compressed unsigned integer from the stream.
		/// </summary>
		/// <returns></returns>
		public uint Read7BitEncodedUInt()
		{
			return unchecked((uint)base.Read7BitEncodedInt()); // go to the protected method.
		}

		/// <summary>
		/// Reads compressed long from stream.
		/// </summary>
		/// <returns></returns>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal long Read7BitEncodedLong()
		{
			return unchecked((long)this.Read7BitEncodedULong());
		}

		/// <summary>
		/// Reads compressed long from stream special encoding.
		/// </summary>
		/// <returns></returns>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal long ReadSpecial7BitEncodedLong()
		{
			return Numbers.Special64ToInt64( unchecked((long)this.Read7BitEncodedULong()));
		}

		/// <summary>
		/// Reads compressed long from stream.
		/// </summary>
		/// <returns></returns>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal ulong Read7BitEncodedULong()
		{
			ulong retValue = 0;
			int shifter = 0;

			while (shifter != 70) // 70 because 7*10 > 64 (bits).
			{
				byte b = this.ReadByte();
				retValue |= (ulong)(b & 0x7f) << shifter;
				shifter += 7;
				if ((b & 0x80) == 0)
				{
					return retValue;
				}
			}
#if DEBUG
			throw new FormatException();
#else
			return 0;
#endif
		}



#if (SILVERLIGHT || PORTABLE) && !WINDOWS_PHONE8
		public decimal ReadDecimal()
        {
            int[] buffer = new int[4];// d.lo, d.mid, d.hi, d.flags            
            buffer[0] = this.ReadInt32();
            buffer[1] = this.ReadInt32();
            buffer[2] = this.ReadInt32();
            buffer[3] = this.ReadInt32();
            return new decimal(buffer);
        }
#endif

	}

	/// <summary>
	/// Allows use of protected Write7BitEncodedInt().
	/// </summary>
	internal class BinaryWriter2 : BinaryWriter
	{
		public BinaryWriter2(Stream output)
			: base(output)
		{ }
		public BinaryWriter2(Stream output, Encoding encoding)
			: base(output, encoding)
		{ }

		/// <summary>
		/// Writes compressed short to stream.
		/// </summary>
		/// <param name="value"></param>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal void Write7BitEncodedShort(short value)
		{
			this.Write7BitEncodedUShort(unchecked((ushort)value));
		}

		/// <summary>
		/// Writes compressed short to stream using special encoding.
		/// </summary>
		/// <param name="value"></param>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal void WriteSpecial7BitEncodedShort(short value)
		{
			this.Write7BitEncodedUShort(unchecked((ushort) Numbers.Int16ToSpecial16(value)));
		}

		/// <summary>
		/// Writes compressed ushort to stream.
		/// </summary>
		/// <param name="value"></param>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal void Write7BitEncodedUShort(ushort value)
		{
			ushort number;

			for (number = value; number >= (ushort)128u; number >>= 7)
				this.Write(unchecked((byte)(number | (ushort)128u)));

			this.Write((byte)number); // remaining bits.
		}

		/// <summary>
		/// Writes a compressed integer to the stream.
		/// </summary>
		/// <param name="value"></param>
		public new void Write7BitEncodedInt(int value)
		{
			base.Write7BitEncodedInt(value); // go to the protected method.
		}

		/// <summary>
		/// Writes a compressed integer to the stream using special encoding.
		/// </summary>
		/// <param name="value"></param>
		public void WriteSpecial7BitEncodedInt(int value)
		{
			base.Write7BitEncodedInt(Numbers.Int32ToSpecial32(value)); // go to the protected method.
		}

		/// <summary>
		/// Writes a compressed unsigned integer to the stream.
		/// </summary>
		/// <param name="value"></param>
		public void Write7BitEncodedUInt(uint value)
		{
			base.Write7BitEncodedInt(unchecked((int)value)); // go to the protected method.
		}

		/// <summary>
		/// Writes compressed long to stream.
		/// </summary>
		/// <param name="value"></param>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal void Write7BitEncodedLong(long value)
		{
			this.Write7BitEncodedULong(unchecked((ulong)value));
		}

		/// <summary>
		/// Writes compressed long to stream using special encoding.
		/// </summary>
		/// <param name="value"></param>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal void WriteSpecial7BitEncodedLong(long value)
		{
			this.Write7BitEncodedULong(unchecked((ulong)Numbers.Int64ToSpecial64(value)));
		}

		/// <summary>
		/// Writes compressed ulong to stream.
		/// </summary>
		/// <param name="value"></param>
		[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)256)] // AggressiveInlining
		internal void Write7BitEncodedULong(ulong value)
		{
			ulong number;

			for (number = value; number >= 128ul; number >>= 7)
				this.Write(unchecked((byte)(number | 128ul)));

			this.Write((byte)number); // remaining bits.
		}

#if (SILVERLIGHT || PORTABLE) && !WINDOWS_PHONE8
		public void WriteDecimal(decimal value)
        {
            var ints = decimal.GetBits(value); // returns d.lo, d.mid, d.hi, d.flags.
            foreach (int i in ints)
                this.Write(i);
        }
#endif

	}
#endif
}

	// ###############################################################
	// ###############################################################

namespace UniversalSerializerLib3.DataTools
{

	// ###############################################################
	// ###############################################################

	internal static class Data
	{

		// -------------------------------------------

		public class ComparableKeyValuePair<TKey, TValue> : IEquatable<ComparableKeyValuePair<TKey, TValue>>, IEqualityComparer
			where TKey : class//IEquatable<TKey>
			where TValue : class//IEquatable<TValue>
		{
			public readonly TKey Key;
			public readonly TValue Value;

			public ComparableKeyValuePair(TKey Key, TValue Value)
			{
				this.Key = Key;
				this.Value = Value;
			}

			public static bool operator ==(ComparableKeyValuePair<TKey, TValue> A, ComparableKeyValuePair<TKey, TValue> B)
			{
				if (object.ReferenceEquals(A, null) || object.ReferenceEquals(B, null))
					return false;
				return A.Key == B.Key && A.Value == B.Value;
			}

			public static bool operator !=(ComparableKeyValuePair<TKey, TValue> A, ComparableKeyValuePair<TKey, TValue> B)
			{
				if (object.ReferenceEquals(A, null) || object.ReferenceEquals(B, null))
					return true;
				return A.Key != B.Key || A.Value != B.Value;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is ComparableKeyValuePair<TKey, TValue>))
					return false;
				return this == (ComparableKeyValuePair<TKey, TValue>)obj;
			}

			public override int GetHashCode()
			{
				return this.Key.GetHashCode() ^ this.Value.GetHashCode();
			}

			public bool Equals(ComparableKeyValuePair<TKey, TValue> other)
			{
				return this == other;
			}

			public new bool Equals(object x, object y)
			{
				ComparableKeyValuePair<TKey, TValue> X = (ComparableKeyValuePair<TKey, TValue>)x;
				ComparableKeyValuePair<TKey, TValue> Y = (ComparableKeyValuePair<TKey, TValue>)y;
				return x == y;
			}

			public int GetHashCode(object obj)
			{
				ComparableKeyValuePair<TKey, TValue> o = (ComparableKeyValuePair<TKey, TValue>)obj;
				return o.Key.GetHashCode() ^ o.Value.GetHashCode();
			}
		}
	// -------------------------------------------

		internal static void AddRangeNoDuplicate<T>(this List<T> list, IEnumerable<T> collection)
		{
			foreach (T item in collection)
				if (!list.Contains(item))
					list.Add(item);
		}

		// -------------------------------------------

		/// <summary>
		/// Find the index of an item in the enumerable.
		/// Please note that arrays already have a IndexOf method, but static.
		/// </summary>
		/// <param name="enumerable"></param>
		/// <param name="o"></param>
		/// <returns>The item index, or -1 if it has not been found.</returns>
		internal static int IndexOf(this IEnumerable enumerable, object o)
		{
			Array array = enumerable as Array;
			if (array != null)
				return array.IndexOf(o);

			int i = 0;
			foreach (var item in enumerable)
			{
				if (item == o)
					return i;
				i++;
			}
			return -1;
		}

		// -------------------------------------------

		/// <summary>
		/// Find the index of an item in the enumerable.
		/// Compares using Equals().
		/// Please note that arrays already have a IndexOf method, but static.
		/// </summary>
		/// <param name="enumerable"></param>
		/// <param name="o"></param>
		/// <returns>The item index, or -1 if it has not been found.</returns>
		internal static int IndexOf2<T>(this IEnumerable<T> enumerable, T o)
			where T : class
		{
			if (!object.ReferenceEquals(o, null))
			{
				int i = 0;
				foreach (var item in enumerable)
				{
					if (!object.ReferenceEquals(item, null) &&  item.Equals(o))
						return i;
					i++;
				}
			}
			return -1;
		}

		// -------------------------------------------

	}

	// ###############################################################

	/// <summary>
	/// A generic limited-size dictionary that will eliminate least used items on priority.
	/// Maximum size is given by the constructor.
	/// It is optimized for small size (10 items or less). No hash code is used.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	internal class FrequencyOrderedLimitedSizeDict<TKey, TValue>:IDictionary<TKey,TValue>
		where TKey : class
	{
		readonly TKey[] Keys;
		readonly TValue[] Values;
		readonly int[] Counters;
		readonly int MaximumSize;
		int CurrentCount;

		public FrequencyOrderedLimitedSizeDict(int MaximumSize)
		{
			this.MaximumSize = MaximumSize;
			this.Counters=new int[MaximumSize];
			this.Keys = new TKey[MaximumSize];
			this.Values = new TValue[MaximumSize];
		}

		public void Add(TKey key, TValue value)
		{
			// 1. get a free index:
			int index = this.CurrentCount;
			if (index >= this.MaximumSize)
			{	// replace the least-used item
				int count = int.MaxValue;
				for (int i = 0; i < this.CurrentCount; i++)
				{
					int counter = Counters[i];
					if (counter < count)
					{
						index = i;
						count = counter;
					}
				}
				Counters[index] = 0;
			}
			else
				this.CurrentCount++;

			// 2. set the item in the free index:
			this.Keys[index] = key;
			this.Values[index] = value;
			Counters[index]++;
		}

		public bool Remove(TKey key)
		{
			if (key.Equals(default(TKey)))
				throw new ArgumentNullException(); // We need a set value.
			int index = this.Keys.IndexOf2(key);
			if (index < 0)
				return false;
			this.Keys[index] = default(TKey);
			this.Counters[index] = 0;
			return true;
		}

		/// <summary>
		/// Compares keys using Equals().
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool ContainsKey(TKey key)
		{
			for (int i = 0; i < this.Keys.Length; i++)
				if (!object.ReferenceEquals(this.Keys[i], null))
					//if (this.Keys[i] == key)
						if (this.Keys[i].Equals(key))
							return true;
			return false;
		}

		ICollection<TKey> IDictionary<TKey, TValue>.Keys
		{
			get { return this.Keys; }
		}

		/// <summary>
		/// Compares keys using Equals().
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			int index = this.Keys.IndexOf2(key);
			if (index < 0)
			{
				value = default(TValue);
				return false;
			}
			value = this.Values[index];
			this.Counters[index]++;
			return true;
		}

		ICollection<TValue> IDictionary<TKey, TValue>.Values
		{
			get { return this.Values; }
		}

		public TValue this[TKey key]
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get { return this.Keys.Length; }
		}

		public bool IsReadOnly
		{
			get { throw new NotImplementedException(); }
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return new DictEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new DictEnumerator(this);
		}

		internal class DictEnumerator : IEnumerator<KeyValuePair<TKey,TValue>>
		{
			private FrequencyOrderedLimitedSizeDict<TKey, TValue> _collection;
			private int curIndex;
			private KeyValuePair<TKey, TValue> curElem;


			public DictEnumerator(FrequencyOrderedLimitedSizeDict<TKey, TValue> collection)
			{
				_collection = collection;
				curIndex = -1;
				curElem = default(KeyValuePair<TKey, TValue>);
			}

			public bool MoveNext()
			{
				//Avoids going beyond the end of the collection.
				if (++curIndex >= _collection.Count)
				{
					return false;
				}
				else
				{
					// Set current box to next item in collection.
					curElem = new KeyValuePair<TKey,TValue>( _collection.Keys[curIndex], _collection.Values[curIndex]) ;
				}
				return true;
			}

			public void Reset() { curIndex = -1; }

			void IDisposable.Dispose() { }

			public KeyValuePair<TKey, TValue> Current
			{
				get { return curElem; }
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}

		}
	}

	// ###############################################################
	// ###############################################################
	// ###############################################################
	// ###############################################################

	// -------------------------------------------
	// -------------------------------------------
	// -------------------------------------------
	// -------------------------------------------
}

    // ###############################################################
	// ###############################################################

namespace UniversalSerializerLib3.FrameworkTools
{

	internal static class Framework
	{

		internal static readonly Lazy<Assembly[]> Assemblies =
			new Lazy<Assembly[]>(() =>
#if !PORTABLE
 AppDomain.CurrentDomain.GetAssemblies());
#else
 {
	 return AppDomain.Assemblies;
 });
#endif

	}

#if PORTABLE

	/// <summary>
	/// We try to obtain current AppDomain using reflection.
	/// </summary>
	internal static class AppDomain
	{
		/// <summary>
		/// Can be null.
		/// </summary>
		internal static object CurrentDomain { get; private set; }

		/// <summary>
		/// Always defined, but can be empty if not found by reflexion in the framework.
		/// </summary>
		internal static Assembly[] Assemblies { get; private set; }

		static AppDomain()
		{
			var tAppDomain = Type.GetType("System.AppDomain");
			if (tAppDomain != null)
			{
				var pi = tAppDomain.GetProperty("CurrentDomain", BindingFlags.Static | BindingFlags.Public);
				if (pi != null)
				{
					CurrentDomain = pi.GetValue(null, null);
					if (CurrentDomain != null)
					{
						var mi = CurrentDomain.GetType().GetMethod("GetAssemblies", BindingFlags.Instance | BindingFlags.Public);
						if (mi != null)
							Assemblies = (Assembly[])mi.Invoke(CurrentDomain, null);
					}
				}
			}
			if (Assemblies == null)
				Assemblies = new Assembly[0];
		}
	} // class AppDomain
#endif // PORTABLE

}
// ###############################################################
// ###############################################################
namespace UniversalSerializerLib3.NumberTools
{

	internal static class Numbers
	{

#region Special integers

		/* 
		 * 'Special' integers are signed integers where negative values have their bits reversed (except the higher bit),
		 * then value is rotated one bit left, the higher bit goes to the lower site.
		 * The objective is to reduce the number of set (1) bits for small negative numbers.
		 * The compresser considers small numbers (positive or negative) as more frequent and optimizes their storage size.
		 * 
		 * The transcoding method is the same in the two directions, but I wrote one function for each direction for clarity reasons.
		 */

		internal static short Int16ToSpecial16(short i)
		{
			if (i >= 0)
				return (short)(i << 1);
			return unchecked((short)((i << 1) ^ -1));
		}

		internal static short Special16ToInt16(short i)
		{
			if ((i & 1) == 0)
				return (short)((uint)(ushort)i >> 1);
			return unchecked((short)((((uint)(ushort)i >> 1)) ^ -1));
		}

		internal static int Int32ToSpecial32(int i)
		{
			if (i >= 0)
				return i << 1;
			return (i << 1) ^ -1;
		}

		internal static int Special32ToInt32(int i)
		{
			if ((i & 1) == 0)
				return (int)((uint)i >> 1);
			return ((int)((uint)i >> 1)) ^ -1;
		}

		internal static long Int64ToSpecial64(long i)
		{
			if (i >= 0)
				return i << 1;
			return (i << 1) ^ -1L;
		}

		internal static long Special64ToInt64(long i)
		{
			if ((i & 1) == 0)
				return (long)((ulong)i >> 1);
			return ((long)((ulong)i >> 1)) ^ -1L;
		}

#endregion Special integers

	}
}

namespace UniversalSerializerLib3.TypeTools
{
	internal static class TypeEx
	{

		internal static Type BaseType(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().BaseType;
#else
			return t.BaseType;
#endif
		}

		internal static bool ContainsGenericParameters(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().ContainsGenericParameters;
#else
			return t.ContainsGenericParameters;
#endif
		}

		internal static TypeCode GetTypeCode(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeCode();
#else
			return Type.GetTypeCode(t);
#endif
		}

		internal static Guid GUID(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().GUID;
#else
			return t.GUID;
#endif
		}

		internal static bool IsArray(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsArray;
#else
			return t.IsArray;
#endif
		}

		internal static bool IsClass(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsClass;
#else
			return t.IsClass;
#endif
		}

		internal static bool IsEnum(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsEnum;
#else
			return t.IsEnum;
#endif
		}

		internal static bool IsGenericType(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsGenericType;
#else
			return t.IsGenericType;
#endif
		}

		internal static bool IsGenericTypeDefinition(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsGenericTypeDefinition;
#else
			return t.IsGenericTypeDefinition;
#endif
		}



		internal static bool IsPublic(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsPublic;
#else
			return t.IsPublic;
#endif
		}

		internal static bool IsValueType(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsValueType;
#else
			return t.IsValueType;
#endif
		}

		internal static bool IsInterface(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsInterface;
#else
			return t.IsInterface;
#endif
		}

		internal static bool IsAbstract(Type t)
		{
#if WINDOWS_UWP
			return t.GetTypeInfo().IsAbstract;
#else
			return t.IsAbstract;
#endif
		}

	}

}