
// Copyright Christophe Bertrand.

namespace UniversalSerializerLib3
{
	/// <summary>
	/// Public WindForm error messages.
	/// </summary>
	public static class ErrorMessagesWinForm
	{
		/// <summary>
		/// List of errors.
		/// </summary>
		public static ErrorMessages.ErrorDescriptor[] Errors = new ErrorMessages.ErrorDescriptor[]
		{

			new ErrorMessages.ErrorDescriptor() { ErrorNumber=0,
				Text= "Not an error (very strange !!)." },

		};

		/// <summary>
		/// Get a short error description from its number.
		/// </summary>
		/// <param name="ErrorNumber"></param>
		/// <returns></returns>
		public static string GetText(int ErrorNumber)
		{
			return string.Format("Error WinForm {0} : {1}", ErrorMessagesWinForm.Errors[ErrorNumber].ErrorNumber.ToString(), ErrorMessagesWinForm.Errors[ErrorNumber].Text);
		}
	}
}
