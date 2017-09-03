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
        AppKit.NSTableView CheatTableView { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (CheatTableView != null) {
                CheatTableView.Dispose ();
                CheatTableView = null;
            }
        }
    }
}
