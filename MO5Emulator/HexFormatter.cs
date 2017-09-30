// This file has been autogenerated from a class added in the UI designer.

using System;
using Foundation;

namespace MO5Emulator
{
    [Register("HexFormatter")]
    public partial class HexFormatter : NSFormatter
    {
        [Export("Minimum")]
        public nint Minimum { get; set; } = 0;

        [Export("Maximum")]
        public nint Maximum { get; set; } = int.MaxValue;

        public HexFormatter()
        {
        }

        public HexFormatter(NSCoder coder) : base(coder)
        {
        }

        public HexFormatter(IntPtr handle) : base(handle)
        {
        }

        public override string StringFor(NSObject value)
        {
            var number = value as NSNumber;
            if (number == null) return null;

            return number.Int32Value.ToString("X");
        }

		public override bool GetObjectValue(out NSObject obj, string str, out NSString error)
        {
            if (!int.TryParse(str, System.Globalization.NumberStyles.HexNumber,
                  System.Globalization.CultureInfo.CurrentCulture, out int hexValue))
            {
                obj = null;
                // TODO: translate
                error = new NSString(@"Hexadecimal format not recognized");
                return false;
            }

            if (hexValue < Minimum)
            {
                obj = new NSNumber(Minimum);
                // TODO: translate
                error = new NSString(@"Value out of bounds");
                return false;
            }

            if (hexValue > Maximum)
            {
                obj = new NSNumber(Maximum);
                // TODO: translate
                error = new NSString(@"Value out of bounds");
                return false;
            }

            error = null;
            obj = new NSNumber(hexValue);
            return true;
        }
    }
}