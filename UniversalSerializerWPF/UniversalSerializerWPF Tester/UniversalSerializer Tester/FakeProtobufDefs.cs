
// Copyright Christophe Bertrand.

// This allows use of Protobuf_net attributes in DataStructures.cs without linking to the protobuf_net DLL.

using System;

namespace ProtoBuf
{
	public class ProtoContractAttribute : Attribute { }

	public class ProtoMemberAttribute : Attribute 
	{
		public ProtoMemberAttribute(int i) { }
	}
}