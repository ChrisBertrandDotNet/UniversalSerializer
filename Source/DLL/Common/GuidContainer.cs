
// Copyright Christophe Bertrand.

using System;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// For System.Guid.
	/// <para>Reason: Guid has no public field or property that can save its state.</para>
	/// </summary>
	internal class GuidContainer : ITypeContainer
	{
		public byte[] As16Bytes;

		public GuidContainer() { }

		public object Deserialize()
		{
			return new Guid(this.As16Bytes);
		}

		public bool IsValidType(System.Type type)
		{
			return type == typeof(Guid);
		}

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
#if DEBUG
			if (!(ContainedObject is Guid))
				throw new ArgumentException();
#endif
			return new GuidContainer() { As16Bytes = ((Guid)ContainedObject).ToByteArray() };
		}

		public bool ApplyEvenIfThereIsAValidConstructor
		{
			get { return true; }
		}

		public bool ApplyToStructures
		{
			get { return true; }
		}
	}
}