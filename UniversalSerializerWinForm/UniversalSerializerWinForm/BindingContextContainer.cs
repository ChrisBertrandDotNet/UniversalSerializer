
// Copyright Christophe Bertrand.

// A ITypeContainer for BindingContext.


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

	internal class BindingContextContainer : ITypeContainer
	{
		#region Store
		public Dictionary<object, BindingManagerBase> Data;
		#endregion Store

		static BindingContextContainer()
		{
			var HashKeyClass = _tBindingContext.GetNestedType("HashKey",BindingFlags.Static | BindingFlags.NonPublic);
			wRefInHashKey = HashKeyClass.GetField("wRef", BindingFlags.Instance | BindingFlags.NonPublic);
			AddInBindingContext = _tBindingContext.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);

		}
		static readonly FieldInfo wRefInHashKey;
		static readonly MethodInfo AddInBindingContext;

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			var c = new BindingContextContainer();
			c.Data = new Dictionary<object, BindingManagerBase>();
			foreach (var o in ContainedObject as BindingContext)
			{
				DictionaryEntry item = (DictionaryEntry)o;
				c.Data.Add(
					((WeakReference) wRefInHashKey.GetValue(item.Key)).Target,
					((WeakReference)item.Value).Target as BindingManagerBase);
			}
			return c;
		}

		public object Deserialize()
		{
			var bc = new BindingContext();
			foreach (var item in this.Data)
			{				
				AddInBindingContext.Invoke(bc, new object[2] { item.Key, item.Value });
			}
			return bc;
		}

		public bool IsValidType(Type type)
		{
			return Tools.TypeIs(type, _tBindingContext);
		}
		static readonly Type _tBindingContext = typeof(BindingContext);

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
			get { return true; }
		}
	}

}

