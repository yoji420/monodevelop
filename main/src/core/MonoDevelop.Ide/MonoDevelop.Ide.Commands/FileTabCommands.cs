//
// FileTabCommands.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Gtk;

using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components.DockNotebook;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Commands
{
	public enum FileTabCommands
	{
		CloseAll,
		CloseAllButThis,
		CopyPathName,
		ToggleMaximize,
		ReopenClosedTab,
		CloseAllExceptPinned,
		PinTab,
	}
	
	class CloseAllHandler : TabCommandHandler
	{
		protected virtual List<ViewContent> GetDocumentExceptions ()
		{
			return null;
		}

		protected override void Run ()
		{
			var active = IdeApp.Workbench.ActiveDocument;
			if (active == null)
				return;

			var activeNotebook = ((SdiWorkspaceWindow)active.Window).TabControl;
			var excluded = GetDocumentExceptions ();

			var docs = IdeApp.Workbench.Documents
				.Where (doc => ((SdiWorkspaceWindow)doc.Window).TabControl == activeNotebook && (excluded == null || !excluded.Any (except => doc.Window.ViewContent == except)))
				.ToArray ();

			var dirtyDialogShown = docs.Count (doc => doc.IsDirty) > 1;
			if (dirtyDialogShown)
				using (var dlg = new DirtyFilesDialog (docs, closeWorkspace: false, groupByProject: false)) {
					dlg.Modal = true;
					if (MessageService.ShowCustomDialog (dlg) != (int)Gtk.ResponseType.Ok)
						return;
				}
			
			foreach (Document doc in docs)
				if (dirtyDialogShown)
					doc.Window.CloseWindow (true);
				else
					doc.Close ().Ignore();
		}
	}

 	abstract class TabCommandHandler : CommandHandler
	{
		protected DockNotebookTab GetTabFromDocument (Document document)
		{
			var activeWindow = (SdiWorkspaceWindow)document.Window;
			var tabControl = activeWindow.TabControl;
			return tabControl.Tabs.FirstOrDefault (item => (item.Content as SdiWorkspaceWindow).Equals (activeWindow));
		}

		protected DockNotebookTab GetTabFromActiveDocument () 
		{
			var active = IdeApp.Workbench.ActiveDocument;
			if (active == null)
				return null;
			return GetTabFromDocument (active);
		}
	}

	class CloseAllExceptPinnedHandler : CloseAllHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Visible = info.Enabled = DefaultSourceEditorOptions.Instance.EnablePinTabs && IdeApp.Workbench.Documents.Count != 0;
		}

		protected override List<ViewContent> GetDocumentExceptions ()
		{
			var active = IdeApp.Workbench.ActiveDocument;
			if (active == null)
				return null;
			var activeNotebook = ((SdiWorkspaceWindow)active.Window).TabControl;

			var contents = IdeApp.Workbench.Documents.Where (doc => ((SdiWorkspaceWindow)doc.Window).TabControl == activeNotebook && GetTabFromDocument (doc).IsPinned)
				.Select (s => s.Window.ViewContent);

			return contents.ToList ();
		}
	}

	class PinTabHandler : TabCommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Visible = info.Enabled = DefaultSourceEditorOptions.Instance.EnablePinTabs && IdeApp.Workbench.ActiveDocument != null;
			if (!info.Visible)
				return;
			
			var selectedTab = GetTabFromActiveDocument ();
			if (selectedTab != null) {
				info.Text = (selectedTab.IsPinned) ? GettextCatalog.GetString ("Un_pin Tab") : GettextCatalog.GetString ("_Pin Tab");
			}
		}

		protected override void Run ()
		{
			var selectedTab = GetTabFromActiveDocument (); 
			if (selectedTab != null)
				selectedTab.IsPinned = !selectedTab.IsPinned;
		}
	}

	class CloseAllButThisHandler : CloseAllHandler
	{
		protected override List<ViewContent> GetDocumentExceptions ()
		{
			var active = IdeApp.Workbench.ActiveDocument;
			if (active == null) {
				return null;
			}
			return new List<ViewContent> () { active.Window.ViewContent };
		}
	}
	
	class ToggleMaximizeHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ToggleMaximize ();
		}
	}
	
	class CopyPathNameHandler : CommandHandler
	{
		protected override void Run ()
		{
			Document document = IdeApp.Workbench.ActiveDocument;
			if (document == null)
				return;
			var fileName = document.FileName;
			if (fileName == null)
				return;
			Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = fileName;
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = fileName;
		}
	}

	class ReopenClosedTabHandler : CommandHandler
	{
		protected override void Run ()
		{
			NavigationHistoryService.OpenLastClosedDocument ();
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationHistoryService.HasClosedDocuments;
		}
	}
}
