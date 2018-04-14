
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
		/// <summary>
		/// Ensures a global thread-safety.
		/// <para>UniversalSerializer manages the Types globally, then the serializer instances can not run simultaneously.</para>
		/// </summary>
		static readonly object SharedLock = new object();

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
			ModifiersAssembly.GetModifiersAssembly(typeof(UniversalSerializer).GetAssembly());
		//Assembly.GetExecutingAssembly());

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
#if !PORTABLE && !WINDOWS_UWP
#if ! SILVERLIGHT
				new CLRBinaryFormatterContainer(), 
#endif
			new CLRTypeConverterContainer(),
#endif
				new NullableContainer(), 
				new GuidContainer(),
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
		/// Prepares a serializer and deserializer that work on a stream.
		/// </summary>
		/// <param name="stream">Your stream.</param>
		public UniversalSerializer(
			Stream stream)
			: this(new Parameters() { Stream = stream })
		{

		}

		// - - - -

		/// <summary>
		/// Prepares a serializer and deserializer that work on a stream.
		/// </summary>
		/// <param name="stream">Your stream.</param>
		/// <param name="formatter">The stream format.</param>
		public UniversalSerializer(
			Stream stream, SerializerFormatters formatter)
			: this(new Parameters() { Stream = stream, SerializerFormatter = formatter })
		{ }

		// - - - -

#if !PORTABLE && !NETFX_CORE
		/// <summary>
		/// Prepares a serializer and deserializer that work on a file.
		/// <para>
		/// Do not forget to call Dispose() when you release UniversalSerializer, or to write using(). Otherwize an IO exception could occure when trying to access the file for the second time, saying the file can not be open (in fact the file would have not been closed by this first instance of UniversalSerializer).
		/// </para>
		/// </summary>
		/// <param name="FileName">Creates or open this file.</param>
		public UniversalSerializer(
			string FileName)
			: this(new Parameters() { Stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite) })
		{
			this.FileStreamCreatedByConstructorOnly = (FileStream)this.parameters.Stream; // For Dispose().
		}

		/// <summary>
		/// Prepares a serializer and deserializer that work on a file.
		/// <para>
		/// Do not forget to call Dispose() when you release UniversalSerializer, or to write using(). Otherwize an IO exception could occure when trying to access the file for the second time, saying the file can not be open (in fact the file would have not been closed by this first instance of UniversalSerializer).
		/// </para>
		/// </summary>
		/// <param name="FileName">Creates or open this file.</param>
		/// <param name="formatter">The stream format.</param>
		public UniversalSerializer(
			string FileName, SerializerFormatters formatter)
			: this(new Parameters() { Stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite), SerializerFormatter = formatter })
		{
			this.FileStreamCreatedByConstructorOnly = (FileStream)this.parameters.Stream; // For Dispose().
		}
#endif

		// - - - -

		/// <summary>
		/// Prepare a serializer and deserializer following your parameters and modifiers.
		/// <para>Parameters.Stream must be defined.</para>
		/// </summary>
		/// <param name="parameters">Detailed parameters.</param>
		public UniversalSerializer(
			Parameters parameters)
		{
			lock (SharedLock)
			{
				if (parameters.Stream == null)
					throw new ArgumentNullException("parameters.Stream");
				parameters.Init();
				this.parameters = parameters;

				this.CustomFormatter = SerializationFormatter.ChooseDefaultFormatter(parameters);
				this.CustomDeFormatter = SerializationFormatter.ChooseDefaultDeFormatter(parameters);
			}
		}

		// - - - -

		/// <summary>
		/// Serializes (saves) data to the stream(s).
		/// </summary>
		/// <param name="Data">Data to be serialized.</param>
		/// <exception cref="NotSupportedException">The stream does not support writing.</exception>
		public void Serialize(object Data)
		{
			lock (SharedLock)
			{
				if (!this.parameters.Stream.CanWrite)
					throw new NotSupportedException();
				this.CustomDeFormatter.ForgetDataEndMark();
				this.CustomFormatter.SerializeAnotherData(Data);
			}
		}

		// - - - -

		/// <summary>
		/// Deserializes (loads) data from the stream(s).
		/// </summary>
		/// <returns></returns>
		public object Deserialize()
		{
			lock (SharedLock)
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
			lock (SharedLock)
				return this.CustomDeFormatter.DeserializeAnotherData<T>();
		}

		// - - - -

		/// <summary>
		/// Returns a list of field names to be serialized additionnaly.
		/// Or null if no field are to be added.
		/// </summary>
		internal static FieldInfo[] _AdditionalPrivateFieldsAdder(Type t)
		{
#if ! SILVERLIGHT && !ANDROID
			{ // Nullable<T>
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
			lock (SharedLock)
			{
#if !PORTABLE && !WINDOWS_UWP
				if (this.FileStreamCreatedByConstructorOnly != null)
					this.FileStreamCreatedByConstructorOnly.Dispose();
#endif
				this.CustomFormatter.Dispose();
				//this.CustomDeFormatter.Dispose();
			}
		}
	}


	// #############################################################################
	// #############################################################################
}