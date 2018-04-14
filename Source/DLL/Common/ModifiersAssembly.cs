
// Copyright Christophe Bertrand.

#if NETCOREAPP2_0
#define NETCORE
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UniversalSerializerLib3.TypeTools;

namespace UniversalSerializerLib3
{

	// #############################################################################
	// ######################################################################

	/*/// <summary>
	/// A table of the assemblies that define modifiers (filters and containers).
	/// </summary>
	public class ModifiersAssemblies : List<ModifiersAssembly>
	{}*/

	/// <summary>
	/// An assembly that defines modifiers (filters and containers).
	/// </summary>
	public class ModifiersAssembly
	{
		// - - -

		/// <summary>
		/// Assembly.
		/// </summary>
		public readonly Assembly assembly;
		internal readonly CustomModifiers aggregatedCustomModifiers;

		// - - -

		ModifiersAssembly(Assembly assembly)
		{
			this.assembly = assembly;
			var cmt = new List<CustomModifiers>();
			var types = assembly.GetTypes();

			CustomModifiers acm = CustomModifiers.Empty;

			foreach (Type t in types)
			{
				if (TypeEx.IsClass(t)) // optimization.
					if (t.Inherits(typeof(CustomModifiers)))
					{
						var cm = (CustomModifiers)Activator.CreateInstance(t);
						if (!(cm is CustomModifiers_Empty))
							acm = acm.GetCombinationWithOtherCustomModifiers(cm);
					}
			}
			this.aggregatedCustomModifiers = acm;
		}

		// - - -

		/// <summary>
		/// Find modifiers of this assembly.
		/// </summary>
		/// <param name="assembly"></param>
		/// <returns></returns>
		public static ModifiersAssembly GetModifiersAssembly(Assembly assembly)
		{
			ModifiersAssembly ret = null;
			if (!_ModifiersAssemblyCache.TryGetValue(assembly, out ret))
			{
				ret = new ModifiersAssembly(assembly);
				_ModifiersAssemblyCache.Add(assembly, ret);
			}
			return ret;
		}
		static Dictionary<Assembly, ModifiersAssembly> _ModifiersAssemblyCache = new Dictionary<Assembly, ModifiersAssembly>();

		// - - -

		/// <summary>
		/// Find modifiers of an assembly.
		/// </summary>
		/// <param name="assemblyShortName">The assembly (short) name [not the file name].</param>
		/// <param>On (pure, not portable) Android, the DLL file must be added as an Asset.</param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException">Please note on .NET Core the current directory can be wrong. Please set it correctly before calling this function.</exception>
		public static ModifiersAssembly GetModifiersAssembly(string assemblyShortName)
		{
			Assembly assembly;

#if WINDOWS_UWP
			var an = new AssemblyName(assemblyShortName);
			assembly = Assembly.Load(an);
#else
#if SILVERLIGHT && !WINDOWS_PHONE7_1
			var name = assemblyShortName + ".dll";
			var uri = new Uri(name, UriKind.Relative);
			var sm = System.Windows.Application.GetResourceStream(uri);
			if (sm == null)
				throw new FileNotFoundException(name);
			var ap = new System.Windows.AssemblyPart();
			assembly = ap.Load(sm.Stream);
#else
#if ANDROID
			// On Android, the DLL files must be added as embedded resources to the application project.
			var ea =Tools.GetEntryAssembly(); // finds the application assembly.
			var an = ea.GetName();
			var an2 = an.Name;
			var name = an2 + "." + assemblyShortName + ".dll";
			//var r =ea.GetManifestResourceNames();

			using (var sm = ea.GetManifestResourceStream(name))
			{
				if (sm == null)
					throw new FileNotFoundException(name);
				var bytes = new byte[sm.Length];
				sm.Read(bytes, 0, bytes.Length);
				assembly = Assembly.Load(bytes);
			}
#else // ordinary .NET
			try
			{
				assembly = Assembly.Load(assemblyShortName);
			}
			catch (FileNotFoundException ex)
			{
				// tries to read the dll file.
				var name = assemblyShortName + ".dll";
				if (File.Exists(name))
					assembly = Assembly.LoadFrom(name);
				else
				{
					// looks in the resources of all referenced assemblies for an embedded file.
					assembly = null;

					var ea = Tools.GetEntryAssembly(); // finds the application assembly.
					if (ea != null)
						assembly = _LoadResourceAssembly(ea, assemblyShortName);

					if (assembly == null)
					{
						// Searches the file in the resources of all loaded assemblies.
						var assemblies = AppDomain.CurrentDomain.GetAssemblies();
						for (int i = 0; i < assemblies.Length && assembly == null; i++)
						{
							Assembly rc = assemblies[i];
							assembly = _LoadResourceAssembly(rc, assemblyShortName);
						}
					}

					if (assembly == null)
						throw ex;
				}
			}
#endif
#endif
#endif
			return ModifiersAssembly.GetModifiersAssembly(assembly);
		}
#if !SILVERLIGHT && !WINDOWS_UWP
		static Assembly _LoadResourceAssembly(Assembly ea, string assemblyShortName)
		{
			if (ea != null)
#if !NET3_5
				if (!ea.IsDynamic)
#endif
				{
					var an = ea.GetName();
					if (an != null)
					{
						var an2 = an.Name;
						{
							if (!string.IsNullOrEmpty(an2))
							{
								var name = an2 + "." + assemblyShortName + ".dll";
								var r = ea.GetManifestResourceNames();

								using (var sm = ea.GetManifestResourceStream(name))
								{
									if (sm != null)
									{
										var bytes = new byte[sm.Length];
										sm.Read(bytes, 0, bytes.Length);
										return Assembly.Load(bytes);
										/*try
										{
											return Assembly.Load(bytes);
										}
										catch (NotSupportedException) // UWP
										{
											var c = Directory.GetCurrentDirectory();
											return Assembly.Load(new AssemblyName(assemblyShortName));
										}*/
									}
								}
							}
						}
					}
				}
			return null;
		}
#endif
	}
}