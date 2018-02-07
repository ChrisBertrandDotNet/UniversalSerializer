
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Collections;
using UniversalSerializerLib3.TypeManagement;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// Useful functions.
	/// </summary>
	public static partial class Tools
	{

		// -------------------------------------------

		/// <summary>
		/// This class encapsulates a generic IDictionary in an object IDictionary.
		/// That simplies some code in the project.
		/// This class is NOT thread-safe nor observable.
		/// </summary>
		internal class GenericIDictionaryBoxer<TKey, TValue> : IDictionary
		{
			internal readonly IDictionary<TKey, TValue> GenericDictionary;

			public GenericIDictionaryBoxer(IDictionary<TKey, TValue> GenericDictionary)
			{
				this.GenericDictionary = GenericDictionary;
			}

			internal static IDictionary CreateFromGenericIDictionary(
				object obj, L3TypeManager typeManager)
			{
				{
					IDictionary idict = obj as IDictionary;
					if (idict != null) // example: Dictionary<,> is a IDictionary.
						return idict;
				}
				var t2 = typeof(GenericIDictionaryBoxer<,>);
				Type t = t2.MakeGenericType(new Type[] { 
					typeManager.DictionaryKeysTypeManager.l2TypeManager.L1TypeManager.type,
					typeManager.DictionaryValuesTypeManager.l2TypeManager.L1TypeManager.type }); // TODO: put this type in cache.
				return (IDictionary)Activator.CreateInstance(t, new object[] { obj });
			}

			#region IDictionary
			public void Add(object key, object value)
			{
				this.GenericDictionary.Add((TKey)key, (TValue)value);
			}

			public void Clear()
			{
				this.GenericDictionary.Clear();
			}

			public bool Contains(object key)
			{
				return this.GenericDictionary.ContainsKey((TKey)key);
			}


			private class SimpleDictionaryEnumerator : IDictionaryEnumerator
			{
				readonly IEnumerator<KeyValuePair<TKey,TValue>> GenericDictEnumerator;
				readonly int sourceLength;
				Int32 index = -1;

				public SimpleDictionaryEnumerator(GenericIDictionaryBoxer<TKey, TValue> sd)
				{
					this.GenericDictEnumerator = sd.GenericDictionary.GetEnumerator();
					this.sourceLength = sd.Count;
				}

				// Return the current item.
				public Object Current { get { 
					return this.CurrentDictionaryEntry();			}				}

				// Return the current item.
				DictionaryEntry CurrentDictionaryEntry()
				{
					ValidateIndex();
					KeyValuePair<TKey, TValue> item = this.GenericDictEnumerator.Current;
					return new DictionaryEntry(item.Key, item.Value);
				}

				// Return the current dictionary entry.
				public DictionaryEntry Entry
				{
					get { return this.CurrentDictionaryEntry(); }
				}

				// Return the key of the current item.
				public Object Key { get { ValidateIndex();
				KeyValuePair<TKey, TValue> item = this.GenericDictEnumerator.Current;
				return item.Key;				}				}

				// Return the value of the current item.
				public Object Value { get { ValidateIndex();
				KeyValuePair<TKey, TValue> item = this.GenericDictEnumerator.Current;
				return item.Value;				}				}

				// Advance to the next item.
				public Boolean MoveNext()
				{
					bool ret = this.GenericDictEnumerator.MoveNext();
					if (ret)
						index++;
					return ret;
				}

				// Validate the enumeration index and throw an exception if the index is out of range.
				private void ValidateIndex()
				{
					if (index < 0 || index >= this.sourceLength)
						throw new InvalidOperationException("Enumerator is before or after the collection.");
				}

				// Reset the index to restart the enumeration.
				public void Reset()
				{
					index = -1;
					this.GenericDictEnumerator.Reset();
				}
			}
			public IDictionaryEnumerator GetEnumerator()
			{
				return new SimpleDictionaryEnumerator(this);
			}

			public bool IsFixedSize
			{
				get { return this.GenericDictionary.IsReadOnly; } // TODO use the new read-only dictionary interface in .Net 4.5 .
			}

			public bool IsReadOnly
			{
				get { return this.GenericDictionary.IsReadOnly; }
			}

			public ICollection Keys
			{
				get { throw new NotImplementedException(); } // useless here.
			}

			public void Remove(object key)
			{
				this.GenericDictionary.Remove((TKey)key);
			}

			public ICollection Values
			{
				get { throw new NotImplementedException(); } // useless here.
			}

			public object this[object key]
			{
				get
				{
					return this.GenericDictionary[(TKey)key];
				}
				set
				{
					this.GenericDictionary[(TKey)key] = (TValue)value;
				}
			}

			public void CopyTo(Array array, int index)
			{
				throw new NotImplementedException(); // useless here.
			}

			public int Count
			{
				get { 
					return this.GenericDictionary.Count; }
			}

			public bool IsSynchronized
			{
				get { throw new NotImplementedException(); } // useless here.
			}

			public object SyncRoot
			{
				get { return this.GenericDictionary; } // used to return the boxed dict.
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GenericDictionary.GetEnumerator();
			}
			#endregion IDictionary
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
