﻿//
// DescriptorPropertyInfo.cs
//
// Author:
//       jmedrano <josmed@microsoft.com>
//
// Copyright (c) 2018 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#if MAC

using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.PropertyEditing;
using Xamarin.PropertyEditing.Reflection;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport
{
	class DescriptorPropertyInfo
		: IPropertyInfo, IEquatable<DescriptorPropertyInfo>
	{
		public PropertyDescriptor PropertyDescriptor { get; private set; }
		public object PropertyProvider { get; private set; }
		readonly ValueSources valueSources;
		static readonly IAvailabilityConstraint [] EmptyConstraints = new IAvailabilityConstraint [0];
		static readonly PropertyVariationOption [] EmptyVariationOptions = new PropertyVariationOption [0];

		public DescriptorPropertyInfo (PropertyDescriptor propertyInfo, object propertyProvider, ValueSources valueSources) 
		{
			this.PropertyDescriptor = propertyInfo;
			this.PropertyProvider = propertyProvider;
			this.valueSources = valueSources;
		}

		public string Name => PropertyDescriptor.DisplayName;

		public string Description => PropertyDescriptor.Description;

		public virtual Type Type => PropertyDescriptor.PropertyType;

		public ITypeInfo RealType => ToTypeInfo (PropertyDescriptor, PropertyProvider, Type);

		public string Category => PropertyDescriptor.Category;

		public bool CanWrite => !PropertyDescriptor.IsReadOnly;

		public ValueSources ValueSources => valueSources;

		public IReadOnlyList<PropertyVariationOption> Variations => EmptyVariationOptions;

		public IReadOnlyList<IAvailabilityConstraint> AvailabilityConstraints => EmptyConstraints;

		public bool IsUncommon => false;

		public bool Equals (DescriptorPropertyInfo other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals (this, other))
				return true;

			return PropertyDescriptor.Equals (other.PropertyDescriptor);
		}

		public override bool Equals (object obj)
		{
			return obj is DescriptorPropertyInfo info && Equals (info);
		}

		public override int GetHashCode ()
		{
			return PropertyDescriptor.GetHashCode ();
		}

		public static Type GetCollectionItemType (Type colType)
		{
			foreach (var member in colType.GetDefaultMembers ()) {
				var prop = member as PropertyInfo;
				if (prop != null && prop.Name == "Item" && prop.PropertyType != typeof (object))
					return prop.PropertyType;
			}
			return null;
		}

		public static ITypeInfo ToTypeInfo (PropertyDescriptor propertyDescriptor, object propertyProvider, Type type, bool isRelevant = true)
		{
			var asm = type.Assembly.GetName ().Name;
			return new PropertyProviderTypeInfo (propertyDescriptor, propertyProvider, new AssemblyInfo (asm, isRelevant), type.Namespace, type.Name);
		}

		const string ErrorOnGetValueMessage = "Error in GetValueAsync<T> using property descriptor converter";
		protected void LogGetValueAsyncError (Exception ex) => LoggingService.LogError (ErrorOnGetValueMessage, ex);

		internal virtual Task<T> GetValueAsync<T> (object target)
		{
			object value = null;

			try {
				value = PropertyDescriptor.GetValue (PropertyProvider);
				TypeConverter tc = PropertyDescriptor.Converter;
				if (tc.CanConvertTo (typeof (T))) {
					value = tc.ConvertTo (value, typeof (T));
				}
				return Task.FromResult ((T)value);
			} catch (Exception ex) {
				LogGetValueAsyncError (ex);
			}

			T converted = default;
			try {
				if (value != null && !(value is T)) {
					if (typeof (T) == typeof (string)) {
						value = value.ToString ();
					} else {
						value = Convert.ChangeType (value, typeof (T));
					}
				}
				return Task.FromResult ((T)value);
			} catch (Exception ex) {
				LogGetValueAsyncError (ex);
			}
			return Task.FromResult (converted);
		}

		internal virtual void SetValue<T> (object target, T value)
		{
			PropertyDescriptor.SetValue (PropertyProvider, value);
		}
	}
}

#endif