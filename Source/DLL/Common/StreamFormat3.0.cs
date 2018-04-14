
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace UniversalSerializerLib3.StreamFormat3
{

	// ######################################################################
	// ######################################################################
	// ######################################################################

	/// <summary>
	/// Header of stream format, just after format version entry.
	/// </summary>
	public struct Header
	{
		/// <summary>
		/// Identifies assemblies that may contain modifiers (filters and containers).
		/// </summary>
		public AssemblyIdentifier[] AssemblyIdentifiers;
	}

	// ######################################################################
	// ######################################################################

	/// <summary>
	/// Identifies an assembly.
	/// </summary>
	public struct AssemblyIdentifier
	{
		/// <summary>
		/// Got from {Assembly}.GetName().Name .
		/// To be used by Assembly.LoadWithPartialName(PartialName).
		/// </summary>
		public string PartialName;

		/// <summary>
		/// Got from {Assembly}.Location .
		/// To be used by Assembly.LoadFrom(Location).
		/// </summary>
		public string Location;

		/// <summary>
		/// Got from {Assembly}.GetName().FullName .
		/// To be used by Assembly.Load(new AssemblyName(AssemblyName_FullName)).
		/// </summary>
		public string AssemblyName_FullName;

		internal AssemblyIdentifier(Assembly a)
		{
#if !PORTABLE && !SILVERLIGHT
			var an = a.GetName();
			this.PartialName = an.Name;
			this.AssemblyName_FullName = an.FullName;
#if WINDOWS_UWP
			this.Location = null;
#else
			this.Location = a.Location;
#endif
#else
			this.PartialName = a.FullName.Split(',')[0];// an.Name;
			this.Location = null;// a.Location;
			this.AssemblyName_FullName = a.FullName; // an.FullName;
#endif
		}

		internal Assembly Load()
		{
			if (thisAssemblyName.Value == this.AssemblyName_FullName)
#if WINDOWS_UWP
				return typeof(UniversalSerializer).GetAssembly();
#else
			return Assembly.GetExecutingAssembly();
#endif

#if !WINDOWS_UWP//!PORTABLE
			Exception e;
			try
			{
#if !PORTABLE && !SILVERLIGHT
				return Assembly.Load(new AssemblyName(this.AssemblyName_FullName));
#else
				return Assembly.Load(this.AssemblyName_FullName);
#endif
			}
			catch (Exception e1)
			{
				e = e1;
				try
				{
					return Assembly.Load/*WithPartialName*/(this.PartialName);
				}
				catch (Exception e2)
				{
					e = e2;
#if !PORTABLE && !SILVERLIGHT
					try
					{
						return Assembly.LoadFrom(this.Location);
					}
					catch (Exception e3)
					{
						e = e3;
					}
#endif
					Log.WriteLine(string.Format(
						ErrorMessages.GetText(17)//"Listed assembly in stream can not be loaded: \"{0}\". Error={1}"
						, this.AssemblyName_FullName, e.Message));
					return null;
				}
			}
#else
			try
			{
				return Assembly.Load(new AssemblyName(this.PartialName));
			}
			catch (Exception e)
			{
				Log.WriteLine(string.Format("Listed assembly in stream can not be loaded: \"{0}\". Error={1}",
					this.AssemblyName_FullName, e.Message));
				return null;
			}
#endif
		}
		static readonly SimpleLazy<string> thisAssemblyName = new SimpleLazy<string>(() =>
#if WINDOWS_UWP
		typeof(UniversalSerializer).GetAssembly().GetName().FullName
#else
#if !PORTABLE && !SILVERLIGHT
			Assembly.GetExecutingAssembly().GetName().FullName
#else
 Assembly.GetExecutingAssembly().FullName
#endif
#endif
			);

	}

	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################
	// ######################################################################

}