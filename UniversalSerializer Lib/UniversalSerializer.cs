
// Copyright Christophe Bertrand.

#if DEBUG
#if !SILVERLIGHT && !PORTABLE
	//#define DEBUG_WriteSerializedFile
#endif
#endif

using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace UniversalSerializerLib3
{

	/// <summary>
	/// Main serializing class.
	/// Manages one stream.
	/// </summary>
	public class UniversalSerializer : IDisposable
	{
		#region Static defs
		/// <summary>
		/// Default formatter.
		/// </summary>
		public const SerializerFormatters DefaultSerializationFormatter = (SerializerFormatters)0; // '0' to stay consistent with the rest of the program.

		/// <summary>
		/// These types have been tested with UniversalSerializer and should be serialized correctly.
		/// </summary>
		internal static readonly string[] ConfirmedTypes = new string[] {
#if ! SILVERLIGHT
			"System.Uri",
			"System.Windows.Input.Cursor",
			"System.Windows.Media.Imaging.BitmapFrameDecode",
			"System.Windows.Window",
#endif
		};

		internal static readonly ModifiersAssembly InternalModifiersAssembly = 
			ModifiersAssembly.GetModifiersAssembly(Assembly.GetExecutingAssembly());

		/// <summary>
		/// Modifiers for BCL types.
		/// This class will be found by reflexion and set in a ModifiersAssembly.
		/// </summary>
		public class BCLCustomModifiers : CustomModifiers
		{
			/// <summary>
			/// Inits BCL modifiers.
			/// </summary>
			public BCLCustomModifiers()
				: base(
					Containers: new ITypeContainer[] { 
#if !PORTABLE
#if ! SILVERLIGHT
				new CLRBinaryFormatterContainer(), 
#endif
			new CLRTypeConverterContainer(),
#endif
#if SILVERLIGHT
				new NullableContainer(), 
#endif
					new CLRTypeContainer(),
                 },

					FilterSets: new FilterSet[1] 
					{ new FilterSet() 
						{ 
							AdditionalPrivateFieldsAdder=_AdditionalPrivateFieldsAdder, 
							TypeSerializationValidator=_TypeSerializationValidator
						}
					},

					ForcedParametricConstructorTypes: new Type[] { typeof(KeyValuePair<,>) }
	)
			{
			}

		}

		#endregion Static defs

		#region Instance fields

		readonly SerializationFormatter CustomFormatter;
		readonly DeserializationFormatter CustomDeFormatter;

		/// <summary>
		/// Your parameters.
		/// </summary>
		public readonly Parameters parameters;

#if ! PORTABLE && !NETFX_CORE
		/// <summary>
		/// Only if the constructor has created a file stream.
		/// </summary>
		protected FileStream FileStreamCreatedByConstructorOnly;
#endif

        #endregion Instance fields

        // - - - -

		/// <summary>
		/// Prepare a serializer and deserializer that work on a stream.
		/// </summary>
		/// <param name="stream">Your stream.</param>
		public UniversalSerializer(
			Stream stream)
			: this(new Parameters() { Stream = stream })
		{

		}

        // - - - -

#if ! PORTABLE&& !NETFX_CORE
		/// <summary>
		/// Prepare a serializer and deserializer that work on a file.
		/// Do not forget to call Dispose() when you release UniversalSerializer, or to write using(). Otherwize an IO exception could occure when trying to access the file for the second time, saying the file can not be open (in fact the file would have not been closed by this first instance of UniversalSerializer).
		/// </summary>
		/// <param name="FileName">Creates or open this file.</param>
		public UniversalSerializer(
			string FileName)
			: this(new Parameters() { Stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite) })
		{
			this.FileStreamCreatedByConstructorOnly = (FileStream)this.parameters.Stream; // For Dispose().
		}
#endif

        // - - - -

		/// <summary>
		/// Prepare a serializer and deserializer following your parameters and modifiers.
		/// Parameters.Stream must be defined.
		/// </summary>
		/// <param name="parameters"></param>
		public UniversalSerializer(
			Parameters parameters)
		{
			if (parameters.Stream == null)
				throw new ArgumentNullException("parameters.Stream");
			parameters.Init();
			this.parameters = parameters;

			this.CustomFormatter = SerializationFormatter.ChooseDefaultFormatter(parameters);
			this.CustomDeFormatter = SerializationFormatter.ChooseDefaultDeFormatter(parameters);
		
#if !PORTABLE && !SILVERLIGHT
			// Pour initialiser Main:
//#error à faire: Voir comment éviter la ligne suivante.
			var a=Main.StaticMain;
#endif
		}

		// - - - -

		/// <summary>
		/// Serializes data to the stream(s).
		/// </summary>
		/// <param name="Data">Data to be serialized.</param>
		public void Serialize(object Data)
		{
			this.CustomDeFormatter.ForgetDataEndMark();
			this.CustomFormatter.SerializeAnotherData(Data);
		}

		// - - - -
		/// <summary>
		/// Deserializes data from the stream(s).
		/// </summary>
		/// <returns></returns>
		public object Deserialize()
		{
			return this.CustomDeFormatter.DeserializeAnotherData();
		}

		// - - - -

		/// <summary>
		/// Deserializes data from the stream(s). Generic version.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Deserialize<T>()
		{
			return this.CustomDeFormatter.DeserializeAnotherData<T>();
		}

		// - - - -

		/// <summary>
		/// Returns a list of field names to be serialized additionnaly.
		/// Or null if no field are to be added.
		/// </summary>
		internal static FieldInfo[] _AdditionalPrivateFieldsAdder(Type t)
		{
#if ! SILVERLIGHT
			{
				Type t2 = t.FindDerivedOrEqualToThisType(TGenericNullable);
				if (t2 != null)
					return new FieldInfo[2] { Tools.FieldInfoFromName(t2, "hasValue"), Tools.FieldInfoFromName(t2, "value") };
			}
#endif
            if (TPanel != null)
			{
				Type t2 = t.FindDerivedOrEqualToThisType(TPanel); // "System.Windows.Controls.Panel".
				if (t2 != null)
					return new FieldInfo[1] { Tools.FieldInfoFromName(t2, "_uiElementCollection") };
			}
			return null;
		}
		static readonly Type TPanel = Tools.GetTypeFromFullName("System.Windows.Controls.Panel");
		static readonly Type TGenericNullable = typeof(Nullable<>);

		static FieldInfo[] FieldInfosFromNames(Type t, string[] names)
		{
			FieldInfo[] fis = new FieldInfo[names.Length];
			for (int i = 0; i < names.Length; i++)
				fis[i] = Tools.FieldInfoFromName(t, names[i]);
			return fis;
		}

		internal static bool _TypeSerializationValidator(Type t)
		{
			if (t.Is(typeof(System.Delegate))) // We can not serialize a method anyway.
				return false;

			switch (t.FullName)
			{
				case "System.Windows.Controls.ControlTemplate": // TODO: create a container for System.Windows.TemplateContent .
					return false;
				case "System.IntPtr":
					return false;
				default:
					return true;
			}
		}

		/// <summary>
		/// The type is not Serializable and it has no no-param constructor.
		/// </summary>
		internal class NotSerializableException : Exception
		{
			internal NotSerializableException(Type type)
				: base(string.Format("The type '{0}' is not Serializable.", type.FullName))
			{ }
		}


		/// <summary>
		/// Releases open or created stream(s) by constructor(FileName).
		/// Do not release your own stream(s).
		/// </summary>
		public void Dispose()
		{
#if ! PORTABLE
			if (this.FileStreamCreatedByConstructorOnly != null)
				this.FileStreamCreatedByConstructorOnly.Dispose();
#endif
			this.CustomFormatter.Dispose();
			//this.CustomDeFormatter.Dispose();
		}
	}


	// #############################################################################
	// #############################################################################
}
