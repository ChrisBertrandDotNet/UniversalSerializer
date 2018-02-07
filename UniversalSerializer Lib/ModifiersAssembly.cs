
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Reflection;
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
				if (t.IsClass) // optimization.
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
		/// Find modifiers of this assembly.
		/// </summary>
		/// <param name="assemblyString">Same as of Assembly.Load() .</param>
		/// <returns></returns>
		public static ModifiersAssembly GetModifiersAssembly(string assemblyString)
		{
			Assembly assembly = Assembly.Load(assemblyString);
			return ModifiersAssembly.GetModifiersAssembly(assembly);
		}

		// - - -
		// - - -
	}

}