
// Copyright Christophe Bertrand.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace UniversalSerializerLib3
{
	public static partial class Tools
	{

		// -------------------------------------------

		internal static IList GetIListBoxer(IEnumerable enumerable, out Type ItemType)
		{
			Type et = enumerable.GetType();

			// TODO: put that analysis in cache (parameter: type).

			// First: we determine the item type, if a generic IEnumerable.
			var genericIEnumerable = et.FindDerivedOrEqualToThisType(typeof(IEnumerable<>));
			ItemType =
				(genericIEnumerable != null) ?
				genericIEnumerable.GetGenericArguments()[0]
				: null;

			if (enumerable is IList)
				return enumerable as IList; // best option.

			// Now we try to enbox the enumerable.

			{
				Type genericICollection = et.FindDerivedOrEqualToThisType(typeof(ICollection<>));
				if (genericICollection != null)
				{
					ItemType = genericICollection.GetGenericArguments()[0];
					return IListBoxerFromGenericICollection<int>.CreateFromGenericICollection(enumerable, ItemType);
				}
			}

			{

				if (genericIEnumerable != null)
				{
					ItemType = genericIEnumerable.GetGenericArguments()[0];
					return IListBoxerFromGenericIEnumerable<int>.CreateFromGenericIEnumerable(enumerable, ItemType);
				}
			}

			{
				var ICollectionObj = enumerable as ICollection;
				if (ICollectionObj != null)
					return IListBoxerFromICollection.CreateFromICollection(ICollectionObj);
			}

			return IListBoxerFromIEnumerable.CreateFromIEnumerable(enumerable);
		}

		// -------------------------------------------

		/// <summary>
		/// This class encapsulates a generic ICollection in an object IList.
		/// That simplifies some code in the project.
		/// </summary>
		internal class IListBoxerFromGenericICollection<T> : IList
		{
			internal readonly ICollection<T> GenericICollection;

			public IListBoxerFromGenericICollection(ICollection<T> GenericICollection)
			{
				this.GenericICollection = GenericICollection;
			}

			internal static IList CreateFromGenericICollection(IEnumerable obj, Type ItemType)
			{
				var t2 = tGenericIListBoxer;
				Type t = t2.MakeGenericType(new Type[] { ItemType });
				return (IList)Activator.CreateInstance(t, new object[] { obj });
			}
			static readonly Type tGenericIListBoxer = typeof(IListBoxerFromGenericICollection<>);


			public int Add(object value)
			{
				this.GenericICollection.Add((T)value);
				return this.GenericICollection.Count - 1;
			}

			public void Clear()
			{
				this.GenericICollection.Clear();
			}

			public bool Contains(object value)
			{
				return this.GenericICollection.Contains((T)value);
			}

			public int IndexOf(object value)
			{
				throw new NotSupportedException();
			}

			public void Insert(int index, object value)
			{
				throw new NotSupportedException();
			}

			public bool IsFixedSize
			{
				get { throw new NotSupportedException(); }
			}

			public bool IsReadOnly
			{
				get { return this.GenericICollection.IsReadOnly; }
			}

			public void Remove(object value)
			{
				this.GenericICollection.Remove((T)value);
			}

			public void RemoveAt(int index)
			{
				throw new NotSupportedException();
			}

			public object this[int index]
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
			}

			public void CopyTo(Array array, int index)
			{
				throw new NotSupportedException();
			}

			public int Count
			{
				get { return this.GenericICollection.Count; }
			}

			public bool IsSynchronized
			{
				get { throw new NotSupportedException(); }
			}

			public object SyncRoot
			{
				get { throw new NotSupportedException(); }
			}

			public IEnumerator GetEnumerator()
			{
				return this.GenericICollection.GetEnumerator();
			}
		}

		// -------------------------------------------

		/// <summary>
		/// This class encapsulates a generic IEnumerable in an object IList.
		/// That simplifies some code in the project.
		/// The difficulty is to find a way to add items in the list.
		/// </summary>
		internal class IListBoxerFromGenericIEnumerable<T> : IList
		{
			internal readonly IEnumerable<T> GenericIEnumerable;
			readonly MethodsByReflexion methodsByReflexion;

			public IListBoxerFromGenericIEnumerable(IEnumerable<T> GenericICollection)
			{
				this.GenericIEnumerable = GenericICollection;
				this.methodsByReflexion = MethodsByReflexion.CreateWithCache(GenericICollection.GetType());
			}

			internal static IList CreateFromGenericIEnumerable(IEnumerable obj, Type ItemType)
			{
				var t2 = tGenericIListBoxer;
				Type t = t2.MakeGenericType(new Type[] { ItemType });
				return (IList)Activator.CreateInstance(t, new object[] { obj });
			}
			static readonly Type tGenericIListBoxer = typeof(IListBoxerFromGenericIEnumerable<>);


			public int Add(object value)
			{
				return this.methodsByReflexion.AddReturnsIndex(this.GenericIEnumerable, value);
			}

			public void Clear()
			{
				throw new NotImplementedException();
			}

			public bool Contains(object value)
			{
				return this.GenericIEnumerable.Contains((T)value);
			}

			public int IndexOf(object value)
			{
				throw new NotImplementedException();
			}

			public void Insert(int index, object value)
			{
				throw new NotImplementedException();
			}

			public bool IsFixedSize
			{
				get { throw new NotImplementedException(); }
			}

			public bool IsReadOnly
			{
				get { throw new NotImplementedException(); }
			}

			public void Remove(object value)
			{
				throw new NotImplementedException();
			}

			public void RemoveAt(int index)
			{
				throw new NotImplementedException();
			}

			public object this[int index]
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			public void CopyTo(Array array, int index)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { return this.methodsByReflexion.GetCount(this.GenericIEnumerable); }
			}

			public bool IsSynchronized
			{
				get { throw new NotImplementedException(); }
			}

			public object SyncRoot
			{
				get { throw new NotImplementedException(); }
			}

			public IEnumerator GetEnumerator()
			{
				return this.GenericIEnumerable.GetEnumerator();
			}
		}

		// -------------------------------------------

		/// <summary>
		/// This class encapsulates a ICollection in an object IList.
		/// That simplifies some code in the project.
		/// The difficulty is to find a way to add items in the list.
		/// </summary>
		internal class IListBoxerFromICollection : IList
		{
			internal readonly ICollection internalCollection;
			readonly MethodsByReflexion methodsByReflexion;

			public IListBoxerFromICollection(ICollection ICollection)
			{
				this.internalCollection = ICollection;
				this.methodsByReflexion = MethodsByReflexion.CreateWithCache(ICollection.GetType());
			}

			internal static IList CreateFromICollection(ICollection obj)
			{
				return new IListBoxerFromICollection(obj);
			}
			static readonly Dictionary<Type, MethodsByReflexion> methodsCache = new Dictionary<Type, MethodsByReflexion>();

			public int Add(object value)
			{
				return this.methodsByReflexion.AddReturnsIndex(this.internalCollection, value);
			}

			public void Clear()
			{
				throw new NotImplementedException();
			}

			public bool Contains(object value)
			{
				throw new NotImplementedException();
			}

			public int IndexOf(object value)
			{
				throw new NotImplementedException();
			}

			public void Insert(int index, object value)
			{
				throw new NotImplementedException();
			}

			public bool IsFixedSize
			{
				get { throw new NotImplementedException(); }
			}

			public bool IsReadOnly
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public void Remove(object value)
			{
				throw new NotImplementedException();
			}

			public void RemoveAt(int index)
			{
				throw new NotImplementedException();
			}

			public object this[int index]
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			public void CopyTo(Array array, int index)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { return this.internalCollection.Count; }
			}

			public bool IsSynchronized
			{
				get { throw new NotImplementedException(); }
			}

			public object SyncRoot
			{
				get { throw new NotImplementedException(); }
			}

			public IEnumerator GetEnumerator()
			{
				return this.internalCollection.GetEnumerator();
			}
		}

		// -------------------------------------------

		/// <summary>
		/// This class encapsulates a IEnumerable in an object IList.
		/// That simplifies some code in the project.
		/// The difficulty is to find a way to add items in the list.
		/// </summary>
		internal class IListBoxerFromIEnumerable : IList
		{
			internal readonly IEnumerable internalCollection;
			readonly MethodsByReflexion methodsByReflexion;

			public IListBoxerFromIEnumerable(IEnumerable ICollection)
			{
				this.internalCollection = ICollection;
				this.methodsByReflexion = MethodsByReflexion.CreateWithCache(ICollection.GetType());
			}

			internal static IList CreateFromIEnumerable(IEnumerable obj)
			{
				return new IListBoxerFromIEnumerable(obj); ;
			}
			static readonly Dictionary<Type, MethodsByReflexion> methodsCache = new Dictionary<Type, MethodsByReflexion>();

			public int Add(object value)
			{
				return this.methodsByReflexion.AddReturnsIndex(this.internalCollection, value);
			}

			public void Clear()
			{
				throw new NotImplementedException();
			}

			public bool Contains(object value)
			{
				throw new NotImplementedException();
			}

			public int IndexOf(object value)
			{
				throw new NotImplementedException();
			}

			public void Insert(int index, object value)
			{
				throw new NotImplementedException();
			}

			public bool IsFixedSize
			{
				get { throw new NotImplementedException(); }
			}

			public bool IsReadOnly
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public void Remove(object value)
			{
				throw new NotImplementedException();
			}

			public void RemoveAt(int index)
			{
				throw new NotImplementedException();
			}

			public object this[int index]
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			public void CopyTo(Array array, int index)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { return this.methodsByReflexion.GetCount(this.internalCollection); }
			}

			public bool IsSynchronized
			{
				get { throw new NotImplementedException(); }
			}

			public object SyncRoot
			{
				get { throw new NotImplementedException(); }
			}

			public IEnumerator GetEnumerator()
			{
				return this.internalCollection.GetEnumerator();
			}
		}

		// -------------------------------------------

		class MethodsByReflexion
		{
			readonly MethodInfo Adder;
			readonly MethodInfo AdderReturnsIndex;
			readonly MethodInfo CountGetter;
			readonly MethodInfo Inserter;

			private MethodsByReflexion(MethodInfo Adder, MethodInfo AdderReturnsIndex, MethodInfo CountGetter, MethodInfo Inserter)
			{
				this.Adder = Adder;
				this.AdderReturnsIndex = AdderReturnsIndex;
				this.CountGetter = CountGetter;
				this.Inserter = Inserter;
			}

			internal static MethodsByReflexion CreateWithCache(Type t)
			{
				MethodsByReflexion ms;
				if (!methodsCache.TryGetValue(t, out ms))
				{
					ms = _findMethods(t);
					methodsCache.Add(t, ms);
				}
				return ms;
			}
			static readonly Dictionary<Type, MethodsByReflexion> methodsCache = new Dictionary<Type, MethodsByReflexion>();

			internal void Add(object collection, object value)
			{
				if (this.Adder != null)
				{
					this.Adder.Invoke(collection, new Object[] { value });
					return;
				}

				if (this.AdderReturnsIndex != null)
				{
					this.AdderReturnsIndex.Invoke(collection, new Object[] { value });
					return;
				}

				if (this.CountGetter != null && this.Inserter != null)
				{
					this.Inserter.Invoke(collection, new Object[] { 
								this.CountGetter.Invoke(collection, null)
								,value });
					return;
				}

				throw new NoMethodsException(collection.GetType());
			}

			internal int AddReturnsIndex(object collection, object value)
			{
				if (this.AdderReturnsIndex != null)
					return (int)this.AdderReturnsIndex.Invoke(collection, new Object[] { value });

				if (this.Adder != null && this.CountGetter != null)
				{
					int index = (int)this.CountGetter.Invoke(collection, null);
					this.Adder.Invoke(collection, new Object[] { value });
					return index;
				}

				if (this.CountGetter != null && this.Inserter != null)
				{
					int index = (int)this.CountGetter.Invoke(collection, null);
#if !PORTABLE && !NETFX_CORE
					this.Inserter.Invoke(collection, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Object[] { index,value }, null);
#else
                    this.Inserter.Invoke(collection, new Object[] { index, value });
#endif
					return index;
				}

				throw new NoMethodsException(collection.GetType());
			}

			internal int GetCount(object collection)
			{
				if (this.CountGetter != null)
					return (int)this.CountGetter.Invoke(collection, null);

				throw new NoMethodsException(collection.GetType());
			}

			static MethodsByReflexion _findMethods(Type t)
			{
				var Adder = _findMethod(t, "Add", 1, false);
				var AdderReturnsIndex = _findMethod(t, "Add", 1, true);
				MethodInfo CountGetter = _findGetMethod(t, "Count", true);
				MethodInfo Inserter = null;
				if (Adder == null)
				{
					Inserter = _findMethod(t, "Insert", 2, false);
					if (Inserter != null)
					{
						var pars = Inserter.GetParameters();
						if (pars.Length > 0 && pars[0].ParameterType != typeof(int))
							Inserter = null; // useless.
					}

					if ((Adder == null) && (Inserter == null))
						throw new NoMethodsException(t);
				}

				return new MethodsByReflexion(Adder, AdderReturnsIndex, CountGetter, Inserter);
			}
			static MethodInfo _findMethod(Type t, string name, int parametersNumber, bool MustReturnInt)
			{
				var ms = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var mi in ms)
				{
					if (mi.Name == name
						&& (MustReturnInt ? mi.ReturnType == typeof(int) : true)
							&&
							mi.GetParameters().Length == parametersNumber)
						return mi;
				}
				return null;
			}
			static MethodInfo _findGetMethod(Type t, string propertyName, bool MustReturnInt)
			{
				var ms = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var pi in ms)
				{
					if (pi.Name == propertyName)
					{
						var mi = pi.GetGetMethod();
						if (mi != null
							&& MustReturnInt ? mi.ReturnType == typeof(int) : true
							&& mi.GetParameters().Length == 0)
							return mi;
					}
				}
				return null;
			}

			class NoMethodsException : Exception
			{
				internal NoMethodsException(Type t)
					: base(string.Format(
					ErrorMessages.GetText(13)// "Type {0} has no Add() method nor [Insert() method and Count get method], we can not set its items."
					, t.FullName))
				{
				}
			}

		}

		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------
		// -------------------------------------------

	}
}
