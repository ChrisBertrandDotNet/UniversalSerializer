
// Copyright Christophe Bertrand.

/* DependencyProperty are usually static fields of a DependencyObject.
 * As such, it should not be duplicated during deserialization, but retreived from its parent, this DependencyObject.
 * This container does this research.
 */

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;

namespace UniversalSerializerLib3
{
	/// <summary>
	/// For classes with [ValueSerializer].
	/// </summary>
	internal class DependencyPropertyContainer : ITypeContainer
	{
		public string Name;
		public Type OwnerType;
		public Type PropertyType;

		public DependencyPropertyContainer()
		{ }

		public object Deserialize()
		{
			var dp = DependencyPropertyContainer.GetStaticDependencyProperty(
				this.Name, this.OwnerType, this.PropertyType);
			if (dp == null)
				throw new Exception(string.Format(
					ErrorMessagesWPF.GetText(1),// "No public static field DependencyProperty {0} of type {1} found in type {2}.",
					this.Name, this.PropertyType, this.OwnerType));
			return dp;
		}

		public bool IsValidType(System.Type type)
		{
			return Tools.TypeIs(type, typeof(DependencyProperty));
		}

		public ITypeContainer CreateNewContainer(object ContainedObject)
		{
			if (ContainedObject == null)
				return null;
			var dp = ContainedObject as DependencyProperty;
			if (dp == null)
				return null;

			return new DependencyPropertyContainer()
			{
				Name = dp.Name,
				OwnerType = dp.OwnerType,
				PropertyType = dp.PropertyType
			};
		}

		public bool ApplyEvenIfThereIsAValidConstructor
		{
			get { return true; }
		}

		/// <summary>
		/// Get the public static DependencyProperty of a class.
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="OwnerType"></param>
		/// <param name="PropertyType"></param>
		/// <returns></returns>
		static DependencyProperty GetStaticDependencyProperty(
			string Name, Type OwnerType, Type PropertyType)
		{
			System.Reflection.FieldInfo fi = OwnerType.GetField(
				Name + "Property",
				System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
			if ((fi != null)
				&& fi.FieldType == typeof(DependencyProperty))
			{
				object c = fi.GetValue(null);
				DependencyProperty dp = c as DependencyProperty;
				if (dp != null && dp.PropertyType == PropertyType)
					return dp;
			}
			return null;
		}

		public bool ApplyToStructures
		{
			get { return false; }
		}
	}
}
