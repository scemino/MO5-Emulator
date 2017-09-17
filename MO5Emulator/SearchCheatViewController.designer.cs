// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace MO5Emulator.Base.lproj
{
	[Register ("SearchCheatViewController")]
	partial class SearchCheatViewController
	{
		[Outlet]
		AppKit.NSTextField StatusTextField { get; set; }

		[Outlet]
		AppKit.NSTextField ValueTextField { get; set; }

		[Action ("ByteSize:")]
		partial void ByteSize (AppKit.NSButton sender);

		[Action ("Cancel:")]
		partial void Cancel (AppKit.NSButton sender);

		[Action ("Restart:")]
		partial void Restart (AppKit.NSButton sender);

		[Action ("Search:")]
		partial void Search (AppKit.NSButton sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (StatusTextField != null) {
				StatusTextField.Dispose ();
				StatusTextField = null;
			}

			if (ValueTextField != null) {
				ValueTextField.Dispose ();
				ValueTextField = null;
			}
		}
	}
}
