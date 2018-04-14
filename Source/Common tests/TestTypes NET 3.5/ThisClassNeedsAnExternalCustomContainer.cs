
// Copyright Christophe Bertrand.

using System;

namespace TestTypes
{
	/// <summary>
	/// . No default (no-param) constructor.
	/// . The only constructor has a parameter with no corresponding field.
	/// . 'IntegerText' property has a private 'set' and is different type from constructor's parameter.
	/// </summary>
	public class ThisClassNeedsAnExternalCustomContainer
	{
		/// <summary>
		/// It is built from the constructor's parameter.
		/// Since its 'set' method is not public, it will not be serialized directly.
		/// </summary>
		public string IntegerText { get; private set; }

		public ThisClassNeedsAnExternalCustomContainer(int Integer)
		{
			this.IntegerText = Integer.ToString();
		}
	}
}
