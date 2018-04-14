
// Copyright Christophe Bertrand.

// A ITypeContainer for Binding.
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

	internal class BindingContainer : ITypeContainer
	{
		#region Store
		public string propertyName;
		public object dataSource;
		public string dataMember;
		public bool formattingEnabled;
		public DataSourceUpdateMode dataSourceUpdateMode;
		public object nullValue;
		public string FormatString;
		public IFormatProvider formatInfo;
		#endregion Store

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			var c = new BindingContainer();
			Binding b = (Binding)ContainedObject;
			c.propertyName = b.PropertyName;
			c.dataSource = b.DataSource;
			c.dataMember = b.BindingMemberInfo.BindingMember; // Indirect.
			c.formattingEnabled = b.FormattingEnabled;
			c.dataSourceUpdateMode = b.DataSourceUpdateMode;
			c.nullValue = b.NullValue;
			c.FormatString = b.FormatString;
			c.formatInfo = b.FormatInfo;
			
			return c;
		}

		static readonly Type _tBinding = typeof(Binding);
		static readonly ConstructorInfo constructor = _tBinding.GetConstructor(
			BindingFlags.Public | BindingFlags.Instance, null,
			new Type[] { 
					typeof(String), // propertyName
					typeof(Object), // dataSource
					typeof(string), // dataMember
					typeof(bool), // formattingEnabled
					typeof(DataSourceUpdateMode), // dataSourceUpdateMode
					typeof(Object), // nullValue
					typeof(string), // formatString
					typeof(IFormatProvider) // formatInfo
			},
				null);

		public object Deserialize()
		{
			object bc = constructor.Invoke(new object[] { 
				this.propertyName,
				this.dataSource,
				this.dataMember,
				this.formattingEnabled,
				this.dataSourceUpdateMode,
				this.nullValue,
				this.FormatString,
				this.formatInfo
			});
			return bc;
		}

		public bool IsValidType(Type type)
		{
			return Tools.TypeIs(type, _tBinding);
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

