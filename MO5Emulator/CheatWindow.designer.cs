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
	[Register ("CheatWindow")]
	partial class CheatWindow
	{
		[Outlet]
		AppKit.NSTextField LabelStatus { get; set; }

		[Outlet]
		AppKit.NSButton RadioFormat { get; set; }

		[Outlet]
		AppKit.NSTableView TableValues { get; set; }

		[Outlet]
		AppKit.NSTextField TextAdressToWrite { get; set; }

		[Outlet]
		AppKit.NSTextField TextDescription { get; set; }

		[Outlet]
		AppKit.NSTextField TextValue { get; set; }

		[Outlet]
		AppKit.NSTextField TextValueToWrite { get; set; }

		[Action ("Add:")]
		partial void Add (Foundation.NSObject sender);

		[Action ("FindValue:")]
		partial void FindValue (Foundation.NSObject sender);

		[Action ("Load:")]
		partial void Load (Foundation.NSObject sender);

		[Action ("Save:")]
		partial void Save (Foundation.NSObject sender);

		[Action ("WriteValue:")]
		partial void WriteValue (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (TextValue != null) {
				TextValue.Dispose ();
				TextValue = null;
			}

			if (TextDescription != null) {
				TextDescription.Dispose ();
				TextDescription = null;
			}

			if (LabelStatus != null) {
				LabelStatus.Dispose ();
				LabelStatus = null;
			}

			if (TableValues != null) {
				TableValues.Dispose ();
				TableValues = null;
			}

			if (TextAdressToWrite != null) {
				TextAdressToWrite.Dispose ();
				TextAdressToWrite = null;
			}

			if (TextValueToWrite != null) {
				TextValueToWrite.Dispose ();
				TextValueToWrite = null;
			}

			if (RadioFormat != null) {
				RadioFormat.Dispose ();
				RadioFormat = null;
			}
		}
	}
}
