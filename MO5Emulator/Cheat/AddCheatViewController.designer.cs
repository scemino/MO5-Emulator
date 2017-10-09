// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace MO5Emulator
{
	[Register ("AddCheatViewController")]
	partial class AddCheatViewController
	{
		[Outlet]
		AppKit.NSButton AddButton { get; set; }

		[Outlet]
		AppKit.NSTextField AddressTextField { get; set; }

		[Outlet]
		AppKit.NSTextField DescriptionTextField { get; set; }

		[Outlet]
		AppKit.NSTextField ValueTextField { get; set; }

		[Action ("AddCheat:")]
		partial void AddCheat (AppKit.NSButton sender);

		[Action ("ByteSize:")]
		partial void ByteSize (AppKit.NSButton sender);

		[Action ("CancelCheat:")]
		partial void CancelCheat (AppKit.NSButton sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (AddButton != null) {
				AddButton.Dispose ();
				AddButton = null;
			}

			if (AddressTextField != null) {
				AddressTextField.Dispose ();
				AddressTextField = null;
			}

			if (DescriptionTextField != null) {
				DescriptionTextField.Dispose ();
				DescriptionTextField = null;
			}

			if (ValueTextField != null) {
				ValueTextField.Dispose ();
				ValueTextField = null;
			}
		}
	}
}
