
// Copyright Christophe Bertrand.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace UniversalSerializerLib3
{

	/// <summary>
	/// Serializations methods for WinForm.
	/// </summary>
	public class UniversalSerializerWinForm : UniversalSerializer
	{
		/// <summary>
		/// Prepare a serializer and deserializer following your parameters and modifiers.
		/// </summary>
		/// <param name="stream">The stream where the serialized data will be read or wrote.</param>
		public UniversalSerializerWinForm(
			Stream stream)
			: base(CheckParameters(new Parameters() { Stream = stream }))
		{
		}

		// - - - -

		/// <summary>
		/// Prepare a serializer and deserializer following your parameters and modifiers.
		/// Parameters.Stream must be defined.
		/// </summary>
		/// <param name="parameters"></param>
		public UniversalSerializerWinForm(
			Parameters parameters)
			: base(CheckParameters(parameters))
		{
		}

		// - - - -

		/// <summary>
		/// Prepare a serializer and deserializer that work on a file.
		/// Do not forget to call Dispose() when you release UniversalSerializer, or to write using(). Otherwize an IO exception could occure when trying to access the file for the second time, saying the file can not be open (in fact the file would have not be closed by this first instance of UniversalSerializer).
		/// </summary>
		/// <param name="FileName">The name of the file to be open or created.</param>
		public UniversalSerializerWinForm(
			string FileName)
			: this(new Parameters() { Stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite) })
		{
			this.FileStreamCreatedByConstructorOnly = (FileStream)this.parameters.Stream; // For Dispose().
		}

		// - - - -

		static Parameters CheckParameters(Parameters parameters)
		{
			// Adds the internal modifiers to the list.
			if (parameters.ModifiersAssemblies == null)
				parameters.ModifiersAssemblies = new ModifiersAssembly[1] { UniversalSerializerWinForm.InternalWinFormModifiersAssembly };
			else
				if (!parameters.ModifiersAssemblies.Contains(UniversalSerializerWinForm.InternalWinFormModifiersAssembly))
				{
					var mas = new List<ModifiersAssembly>(parameters.ModifiersAssemblies);
					mas.Add(UniversalSerializerWinForm.InternalWinFormModifiersAssembly);
					parameters.ModifiersAssemblies = mas.ToArray();
				}

			return parameters;
		}
		internal static readonly ModifiersAssembly InternalWinFormModifiersAssembly =
			ModifiersAssembly.GetModifiersAssembly(Assembly.GetExecutingAssembly());
	}


	/// <summary>
	/// CustomModifiers for Windows Forms.
	/// </summary>
	public class CustomModifiersForWinForm : CustomModifiers
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public CustomModifiersForWinForm()
			: base(
			Containers: new ITypeContainer[] { 
				new BindingContextContainer(),
				new BindToObjectContainer(),
				new BindingContainer()
                 },

			FilterSets: new FilterSet[1] 
			 { new FilterSet() 
				 { 
					 AdditionalPrivateFieldsAdder = _AdditionalPrivateFieldsAdder,
					  TypeSerializationValidator =  _TypeSerializationValidator
					}
			 },

			ForcedParametricConstructorTypes: new Type[1] { typeof(PropertyManager) }
			)
		{
		}

		// -------------

		/// <summary>
		/// Returns a list of field names to be serialized additionnaly.
		/// Or null if no field are to be added.
		/// </summary>
		static FieldInfo[] _AdditionalPrivateFieldsAdder(Type t)
		{
			if (TBinding != null)
			{
				Type t2 = Tools.FindDerivedOrEqualToThisType(t, TBinding); // "System.Windows.Forms.Binding".
				if (t2 != null)
					return new FieldInfo[] { 
						Tools.FieldInfoFromName(t2, "bindToObject"),
						Tools.FieldInfoFromName(t2, "propertyName")
					};
			}
			return null;
		}
		static readonly Type TBinding = typeof(System.Windows.Forms.Binding);

		// -------------

		static bool _TypeSerializationValidator(Type t)
		{
			return !(
				Tools.TypeIs(t, typeof(System.ComponentModel.Design.IDesignerHost))
				|| t.FullName=="System.ComponentModel.Design.DesignerHost+Site"
				|| t.FullName.StartsWith("System.Windows.Forms.Design.")
				);
		}

		// -------------
		// -------------
		// -------------
		// -------------
		// -------------
	}

}