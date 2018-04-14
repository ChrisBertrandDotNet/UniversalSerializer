
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// Public WPF error messages.
	/// </summary>
	public static class ErrorMessagesWPF
	{
		/// <summary>
		/// List of errors.
		/// </summary>
		public static ErrorMessages.ErrorDescriptor[] Errors = new ErrorMessages.ErrorDescriptor[]
		{

			new ErrorMessages.ErrorDescriptor() { ErrorNumber=0,
				Text= "Not an error (very strange !!)." },

			new ErrorMessages.ErrorDescriptor() { ErrorNumber=1,
				Text= "No public static field DependencyProperty {0} of type {1} found in type {2}." },

			new ErrorMessages.ErrorDescriptor() { ErrorNumber=2,
				Text= "The type '{0}' uses {1} as ValueSerializer, but it was not transcoded correctly. Please investigate or contact the author." },

		};

		/// <summary>
		/// Get a short WPF error description from its number.
		/// </summary>
		/// <param name="ErrorNumber"></param>
		/// <returns></returns>
		public static string GetText(int ErrorNumber)
		{
			return string.Format("Error WPF {0} : {1}", ErrorMessagesWPF.Errors[ErrorNumber].ErrorNumber.ToString(), ErrorMessagesWPF.Errors[ErrorNumber].Text);
		}
	}
}
