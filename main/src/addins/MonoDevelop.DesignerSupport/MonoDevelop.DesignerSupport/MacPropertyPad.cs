﻿
/* 
 * PropertyPad.cs: The pad that holds the MD property grid. Can also 
 * hold custom grid widgets.
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if MAC

using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using System;
using MonoDevelop.Components;
using Xamarin.PropertyEditing;
using Xamarin.PropertyEditing.Mac;
using MonoDevelop.Components.Mac;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Theming;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MonoDevelop.DesignerSupport
{
	class VerticalStackView : NSStackView
	{
		public event EventHandler SizeChanged;

		public VerticalStackView ()
		{
			Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			Alignment = NSLayoutAttribute.Leading;
			Spacing = 10;
			Distribution = NSStackViewDistribution.Fill;

		}

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			SizeChanged?.Invoke (this, EventArgs.Empty);
		}
	}

	class MacPropertyPad : PadContent, ICommandDelegator, IPropertyPad
	{
		Gtk.Widget widget;

		VerticalStackView verticalContainer;
		MacPropertyGrid propertyEditorPanel;

		PropertyPadEditorProvider editorProvider;

		NSScrollView scrollView;

		public bool IsPropertyGridEditing => false;

		public event EventHandler PropertyGridChanged;

		protected override void Initialize (IPadWindow window)
		{
			base.Initialize (window);

			verticalContainer = new VerticalStackView ();

			propertyEditorPanel = new MacPropertyGrid ();

			scrollView = new NSScrollView () {
				HasVerticalScroller = true,
				HasHorizontalScroller = false,
			};
			scrollView.WantsLayer = true;
			scrollView.DocumentView = propertyEditorPanel;

			verticalContainer.AddArrangedSubview (scrollView);
		
			widget = GtkMacInterop.NSViewToGtkWidget (verticalContainer);
			widget.CanFocus = true;
			widget.Sensitive = true;
			widget.KeyPressEvent += Widget_KeyPressEvent;
			widget.KeyReleaseEvent += Widget_KeyReleaseEvent;
			widget.Focused += Widget_Focused;

			propertyEditorPanel.Focused += PropertyEditorPanel_Focused;

			window.PadContentShown += Window_PadContentShown;

			verticalContainer.SizeChanged += VerticalContainer_Resized;

			//propertyEditorPanel.PropertiesChanged += PropertyEditorPanel_PropertiesChanged;

			DesignerSupport.Service.SetPad (this);

			widget.ShowAll ();
		}

		void Widget_Focused (object o, Gtk.FocusedArgs args)
		{
			propertyEditorPanel.BecomeFirstResponder ();
		}

		void PropertyEditorPanel_Focused (object sender, EventArgs e)
		{
			if (!widget.HasFocus) {
				widget.HasFocus = true;
				Widget_Focused (null, null);
			}
		}

		void Widget_KeyReleaseEvent (object o, Gtk.KeyReleaseEventArgs args)
		{

		}

		void Widget_KeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{

		}

		void VerticalContainer_Resized (object sender, EventArgs e)
		{
			scrollView.SetFrameSize (verticalContainer.Frame.Size);
		}

		void PropertyEditorPanel_PropertiesChanged (object sender, EventArgs e) => PropertyGridChanged?.Invoke (this, e);

		void Window_PadContentShown (object sender, EventArgs e)
		{
			if (editorProvider == null) {
				editorProvider = new PropertyPadEditorProvider ();
				propertyEditorPanel.TargetPlatform = new TargetPlatform (editorProvider) {
					SupportsCustomExpressions = true,
					SupportsMaterialDesign = true,
				};
			}
		}

		public void BlankPad ()
		{
			propertyEditorPanel.Select (new object [0]);
			CommandRouteOrigin = null;
		}

		#region ICommandDelegatorRouter implementation

		internal object CommandRouteOrigin { get; set; }

		object ICommandDelegator.GetDelegatedCommandTarget ()
		{
			// Route the save command to the object for which we are inspecting the properties,
			// so pressing the Save shortcut when doing changes in the property pad will save
			// the document we are changing
			if (IdeApp.CommandService.CurrentCommand == IdeApp.CommandService.GetCommand (FileCommands.Save))
				return CommandRouteOrigin;
			else
				return null;
		}

		#endregion

		public void PopulateGrid (bool saveEditSession)
		{

		}

		public class TestApp
		{
			public string MyProperty { get; set; }
		}

		public void SetCurrentObject (object lastComponent, object [] propertyProviders)
		{
			if (lastComponent != null) {
				editorProvider.SetPropertyProviders (propertyProviders);
				propertyEditorPanel.Select (new object [] { lastComponent });
			}
		}

		#region AbstractPadContent implementations

		public override Control Control {
			get { return widget; }
		}

		#endregion

		public override void Dispose ()
		{
			//propertyEditorPanel.PropertiesChanged -= PropertyEditorPanel_PropertiesChanged;

			widget.Focused -= Widget_Focused;
			widget.KeyPressEvent -= Widget_KeyPressEvent;
			widget.KeyReleaseEvent -= Widget_KeyReleaseEvent;

			propertyEditorPanel.Focused -= PropertyEditorPanel_Focused;

			verticalContainer.SizeChanged -= VerticalContainer_Resized;

			Window.PadContentShown -= Window_PadContentShown;

			DesignerSupport.Service.SetPad (null);

			base.Dispose ();
		}

	}

	class MacPropertyGrid : PropertyEditorPanel
	{
		public EventHandler Focused;

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}
	}
}

#endif