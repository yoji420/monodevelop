﻿//
// PropertyPadObjectEditor.cs
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

using System;
using System.Collections.Generic;
using Xamarin.PropertyEditing;
using System.Linq;
using System.Reflection;

namespace MonoDevelop.DesignerSupport
{
	class PropertyDescriptorEventInfo
	: IEventInfo
	{
		public PropertyDescriptorEventInfo (EventInfo info)
		{
			this.info = info ?? throw new ArgumentNullException (nameof (info));
		}

		public string Name => this.info.Name;

		public IReadOnlyList<string> GetHandlers (object target)
		{
			//TODO:
			if (target == null)
				return Array.Empty<string> ();

			Type targetType = target.GetType ();
			FieldInfo field = targetType.GetField ($"Event{Name}", BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			Delegate d = field?.GetValue (target) as Delegate;
			if (d == null)
				return Array.Empty<string> ();

			return d.GetInvocationList ().Select (i => i.Method.Name).ToList ();
		}

		private readonly EventInfo info;
	}
}

#endif