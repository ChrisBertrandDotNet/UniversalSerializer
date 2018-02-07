
// Copyright Christophe Bertrand.

// A ITypeContainer for BindToObject.
// It is needed because of the constructor parameter dataMember (the equivalent field has a different type).


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
#if DEBUG
using System.Diagnostics;
#endif

namespace UniversalSerializerLib3
{

	internal class BindToObjectContainer : ITypeContainer
	{
		#region Store
		public Binding owner;
		public object dataSource;
		public string dataMember;
		#endregion Store

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			var c = new BindToObjectContainer();
			c.owner = (Binding) ownerFI.GetValue(ContainedObject);
			c.dataSource = dataSourceFI.GetValue(ContainedObject);
			c.dataMember = ((BindingMemberInfo)dataMemberFI.GetValue(ContainedObject)).BindingMember;
			
			return c;
		}

		static readonly Type _tBindToObject = Tools.GetTypeFromFullName("System.Windows.Forms.BindToObject");
		static readonly FieldInfo ownerFI = _tBindToObject.GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly FieldInfo dataSourceFI = _tBindToObject.GetField("dataSource", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly FieldInfo dataMemberFI = _tBindToObject.GetField("dataMember", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly ConstructorInfo constructor = _tBindToObject.GetConstructor(
			BindingFlags.NonPublic | BindingFlags.Instance, null,
			new Type[] { typeof(Binding), typeof(Object), typeof(String) }, null);

		public object Deserialize()
		{
			object bc = constructor.Invoke(new object[] { this.owner, this.dataSource, this.dataMember });
			return bc;
		}

		public bool IsValidType(Type type)
		{
			return Tools.TypeIs(type, _tBindToObject);
		}

		public bool ApplyEvenIfThereIsANoParamConstructor
		{
			get { return true; }
		}

		public bool ApplyToStructures
		{
			get { return false; }
		}


		public bool ApplyEvenIfThereIsAValidConstructor
		{
			get { return false; }
		}
	}

}

