
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestTypes;
using UniversalSerializerLib3;

namespace ExternalModifiers
{
	public class ExternalCustomContainerTestModifiers : CustomModifiers
	{
		public ExternalCustomContainerTestModifiers()
			: base(Containers: new ITypeContainer[] {
					new ExternalContainerForMyStrangeClass()
					})
		{
		}
	}

	/// <summary>
	/// Container for class 'ThisClassNeedsAnExternalCustomContainer'.
	/// </summary>
	class ExternalContainerForMyStrangeClass : ITypeContainer
	{
		#region Here you add data to be serialized in place of the class instance

		public int AnInteger; // We store the smallest, sufficient and necessary data.

		#endregion Here you add data to be serialized in place of the class instance


		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			ThisClassNeedsAnExternalCustomContainer sourceInstance = ContainedObject as ThisClassNeedsAnExternalCustomContainer;
			return new ExternalContainerForMyStrangeClass() { AnInteger = int.Parse(sourceInstance.IntegerText) };
		}

		public object Deserialize()
		{
			return new ThisClassNeedsAnExternalCustomContainer(this.AnInteger);
		}

		public bool IsValidType(Type type)
		{
			return Tools.TypeIs(type, typeof(ThisClassNeedsAnExternalCustomContainer));
		}

		public bool ApplyEvenIfThereIsAValidConstructor
		{
			get { return true; }
		}

		public bool ApplyToStructures
		{
			get { return false; }
		}
	}


}
