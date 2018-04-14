
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalSerializerLib3
{
	// #############################################################################
	// ######################################################################

	/// <summary>
	/// Parameters for UniversalSerializer.
	/// </summary>
	public class Parameters
	{
		/// <summary>
		/// Multiplex Stream.
		/// </summary>
		public Stream Stream;

		/// <summary>
		/// Assemblies that define your modifiers.
		/// Can be null to let serializer manage them automatically.
		/// </summary>
		public ModifiersAssembly[] ModifiersAssemblies;

		/// <summary>
		/// Your modifiers.
		/// Can be null.
		/// </summary>
		internal CustomModifiers customModifiers;

		/// <summary>
		/// Reserved for a future use.
		/// </summary>
		internal readonly SetOfStreams setOfStreams = null;

		/// <summary>
		/// If true, Type descriptors will be serialized in the stream.
		/// 'false' is for a future use, to share types along different streams.
		/// </summary>
		public bool SerializeTypeDescriptors = true;

		/// <summary>
		/// The stream can be written in different formats.
		/// </summary>
		public SerializerFormatters SerializerFormatter;

		/// <summary>
		/// Reserved for a future use.
		/// </summary>
		internal readonly StreamingModes? StreamingMode = null;

		/// <summary>
		/// If true, integers will be compressed as 7-bits variable-length. If false, they will be serialized normally.
		/// Currently, only the binary formatter of UniversalSerializer supports this option.
		/// </summary>
		public bool CompressIntsAs7Bits = true;

		#region defined by constructor
		internal StreamingModes TheStreamingMode;
		#endregion defined by constructor

		internal void Init()
		{
			// Adds the caller's modifiers to the list.
			{
				var callerAssembly = Tools.GetEntryAssembly();
				if (callerAssembly != null)
				{
					var ma = ModifiersAssembly.GetModifiersAssembly(callerAssembly);
					if (this.ModifiersAssemblies == null)
						this.ModifiersAssemblies = new ModifiersAssembly[1] { ma };
					else
						if (!this.ModifiersAssemblies.Contains(ma))
						{
							var mas = new List<ModifiersAssembly>(this.ModifiersAssemblies);
							mas.Add(ma);
							this.ModifiersAssemblies = mas.ToArray();
						}
				}
			}

			// Adds the internal modifiers to the list.
			if (this.ModifiersAssemblies == null)
				this.ModifiersAssemblies = new ModifiersAssembly[1] { UniversalSerializer.InternalModifiersAssembly };
			else
				if (!this.ModifiersAssemblies.Contains(UniversalSerializer.InternalModifiersAssembly))
				{
					var mas = new List<ModifiersAssembly>(this.ModifiersAssemblies);
					mas.Add(UniversalSerializer.InternalModifiersAssembly);
					this.ModifiersAssemblies = mas.ToArray();
				}

			{// this.customModifiers is the aggregation of all modifiers of all listed assemblies, more internal.
				CustomModifiers cm = CustomModifiers.Empty;
				foreach (var ma in this.ModifiersAssemblies)
				{
					cm = cm.GetCombinationWithOtherCustomModifiers(ma.aggregatedCustomModifiers);
				}
				this.customModifiers = cm;
			}
		}
	}

	// #############################################################################
	// #############################################################################
}