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
	[Register ("CheatViewController")]
	partial class CheatViewController
	{
		[Outlet]
		AppKit.NSTextField AdressTextField { get; set; }

		[Outlet]
		AppKit.NSTableView CheatTableView { get; set; }

		[Outlet]
		AppKit.NSTextField DescriptionTextField { get; set; }

		[Outlet]
		AppKit.NSButton RadioFormat { get; set; }

		[Outlet]
		AppKit.NSTextField StatusTextField { get; set; }

		[Outlet]
		AppKit.NSTextField ValueTextField { get; set; }

		[Action ("AddCheat:")]
		partial void AddCheat (AppKit.NSButton sender);

		[Action ("FindValue:")]
		partial void FindValue (AppKit.NSButton sender);

		[Action ("LoadCheats:")]
		partial void LoadCheats (AppKit.NSButton sender);

		[Action ("SaveCheats:")]
		partial void SaveCheats (AppKit.NSButton sender);

		[Action ("WriteValue:")]
		partial void WriteValue (AppKit.NSButton sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (ValueTextField != null) {
				ValueTextField.Dispose ();
				ValueTextField = null;
			}

			if (RadioFormat != null) {
				RadioFormat.Dispose ();
				RadioFormat = null;
			}

			if (AdressTextField != null) {
				AdressTextField.Dispose ();
				AdressTextField = null;
			}

			if (StatusTextField != null) {
				StatusTextField.Dispose ();
				StatusTextField = null;
			}

			if (DescriptionTextField != null) {
				DescriptionTextField.Dispose ();
				DescriptionTextField = null;
			}

			if (CheatTableView != null) {
				CheatTableView.Dispose ();
				CheatTableView = null;
			}
		}
	}
}
