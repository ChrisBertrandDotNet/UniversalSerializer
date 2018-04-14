
// Copyright Christophe Bertrand.

// Note: SerializationTypeDescriptor is used in version 3 too.

using System.Collections.Generic;

namespace UniversalSerializerLib3
{

	// ######################################################################
	// ######################################################################

	/// <summary>
	/// Description of a type for its (de)serialization.
	/// It has to be a structure, and not a class.
	/// </summary>
	public struct SerializationTypeDescriptor
	{
		#region fields

		/// <summary>
		/// From type.AssemblyQualifiedName .
		/// </summary>		
		public string AssemblyQualifiedName;

		/// <summary>
		/// Only these fields we will serialize, public or private.
		/// </summary>
		public string[] FieldNames;

		/// <summary>
		/// Only these properties we will serialize, public or private.
		/// </summary>
		public string[] PropertyNames;

		/// <summary>
		/// If we need to use a parametric constructor, each integer here is an index in the concatenated [fields+properties] list.
		/// That is enough to find the right constructor.
		/// </summary>
		public string[] FieldAndPropertyNamesForConstructorParameters;

		#endregion fields

		#region Functions
		/// <summary>
		/// Returns the name without assembly details.
		/// </summary>
		/// <returns>The short name.</returns>
		public string GetShortName()
		{

			return this.AssemblyQualifiedName.Substring(0, this.AssemblyQualifiedName.IndexOf(','));
		}

		/// <summary>
		/// Returns the name as it would be in C#.
		/// </summary>
		/// <returns>The parse name.</returns>
		public string ParseName()
		{
			if (this.ParseNameCache == null)
			{
				string aqtn = this.AssemblyQualifiedName;
				this.ParseNameCache = new ParsedAssemblyQualifiedName(aqtn).CSharpStyleName.Value;
			}
			return this.ParseNameCache;
		}
		private string ParseNameCache;

#if DEBUG
		/// <summary>
		/// A useful name while debugging.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "SerializationTypeDescriptor {" + this.ParseName();
		}
#endif
		#endregion Functions
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
	// ######################################################################

}