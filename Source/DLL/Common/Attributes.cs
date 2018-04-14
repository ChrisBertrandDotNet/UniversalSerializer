
// Copyright Christophe Bertrand.

using System;

namespace UniversalSerializerLib3
{

	/// <summary>
	/// Order UniversalSerializerLib to serialize this field or property, not taking into account other attributes.
	/// It is optional.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class ForceSerializeAttribute : Attribute
	{
	}

	/// <summary>
	/// Order UniversalSerializerLib to never serialize this field or property, not taking into account other attributes.
	/// It is optional.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class ForceNotSerializeAttribute : Attribute
	{
	}
}