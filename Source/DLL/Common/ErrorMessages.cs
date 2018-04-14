
// Copyright Christophe Bertrand.

namespace UniversalSerializerLib3
{
	/// <summary>
	/// Public error messages.
	/// </summary>
	public static class ErrorMessages
	{
		/// <summary>
		/// Error descriptor.
		/// </summary>
		public struct ErrorDescriptor
		{
			/// <summary>
			/// Unique error number.
			/// </summary>
			public int ErrorNumber;
			/// <summary>
			/// Short error description.
			/// </summary>
			public string Text;
		}

		/// <summary>
		/// List of errors.
		/// </summary>
		public static ErrorDescriptor[] Errors = new ErrorDescriptor[]
		{

			new ErrorDescriptor() { ErrorNumber=0,
				Text= "Not an error (very strange !!)." },

			new ErrorDescriptor() { ErrorNumber=1,
				Text= "This version of UniversalSerializer can not manage multi-dimensional arrays." },

			new ErrorDescriptor() { ErrorNumber=2,
				Text= "Unknown CustomFormatter." },

			new ErrorDescriptor() { ErrorNumber=3,
				Text= "Unknown CustomDeFormatter." },

			new ErrorDescriptor() { ErrorNumber=4,
				Text= "Can not set field '{0}'.'{1}' of type '{2}' with value '{3}' of type '{4}'" },

			new ErrorDescriptor() { ErrorNumber=5,
				Text= "Can not set property '{0}'.'{1}' of type '{2}' with value '{3}' of type '{4}'" },

			new ErrorDescriptor() { ErrorNumber=6,
				Text= "Type not found: \"{0}\"." },

			new ErrorDescriptor() { ErrorNumber=7,
				Text= "Unknown file version." },

			new ErrorDescriptor() { ErrorNumber=8,
				Text= "Type '{0}' (or one of its sub-data) is not serializable by BCL's BinaryFormatter. (suggestion: try to remove attribute [Serializable], or add an exploitable constructor)." },

			new ErrorDescriptor() { ErrorNumber=9,
				Text= "data is not a Primitive type" },

			new ErrorDescriptor() { ErrorNumber=10,
				Text= "Can not cast deserialized type \"{0}\" to wanted type \"{1}\"." },

			new ErrorDescriptor() { ErrorNumber=11,
				Text= "Construction (instanciation) of type \"{0}\" caused an error: {1}." },

			new ErrorDescriptor() { ErrorNumber=12,
				Text= "No exploitable constructor for type {0}" },

			new ErrorDescriptor() { ErrorNumber=13,
				Text= "Type {0} has no Add() method nor [Insert() method and Count get method], we can not set its items." },

			new ErrorDescriptor() { ErrorNumber=14,
				Text= "Type \"{0}\" can not be constructed because this parameter's type has been disallowed by a filter: Parameter's name=\"{1}\", Corresponding field's name=\"{2}\", Type=\"{3}\".\n\tSuggestion: use a filter to disallow the main type, or to allow the parameter's type." },

			new ErrorDescriptor() { ErrorNumber=15,
				Text= "The type '{0}' uses {1} as TypeConverter, but it does not convert to string correctly. Please investigate or contact the type's author." },

			new ErrorDescriptor() { ErrorNumber=16,
				Text= "The type '{0}' uses {1} as TypeConverter, but its transcoding type is unknown. Please investigate or contact the type's author." },

			new ErrorDescriptor() { ErrorNumber=17,
				Text= "Listed assembly in stream can not be loaded: \"{0}\". Error={1}" },

			new ErrorDescriptor() { ErrorNumber=18,
				Text= "Warning: DateTimeKind has more than 4 items now. That will cause problems in DateTimes. See ToTicksAndKind()." },

			new ErrorDescriptor() { ErrorNumber=19,
				Text= "Type {0} can not be deserialized because of 1) an error in its Container and 2) a refused private field." },

            new ErrorDescriptor() { ErrorNumber=20,
				Text= "(internal) Can not create a TypeManager for type {0}." },

            new ErrorDescriptor() { ErrorNumber=21,
				Text= "Silverlight does not allow creation of a TypeManager for private type {0}." },

            new ErrorDescriptor() { ErrorNumber=22,
				Text= "Type {0} has a circular type in its constructor parameters in a form that is not supported." },
		};

		/// <summary>
		/// Get a short error description from its number.
		/// </summary>
		/// <param name="ErrorNumber"></param>
		/// <returns></returns>
		public static string GetText(int ErrorNumber)
		{
			return string.Format("Error {0} : {1}", ErrorMessages.Errors[ErrorNumber].ErrorNumber.ToString(), ErrorMessages.Errors[ErrorNumber].Text);
		}
	}
}
