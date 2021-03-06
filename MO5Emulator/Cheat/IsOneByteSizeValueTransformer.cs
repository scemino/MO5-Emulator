﻿// This file has been autogenerated from a class added in the UI designer.

using System;
using Foundation;

namespace MO5Emulator
{
    [Register("IsOneByteSizeValueTransformer")]
    public class IsOneByteSizeValueTransformer : NSValueTransformer
    {
        public IsOneByteSizeValueTransformer(NSObjectFlag flag) : base(flag)
        {
        }

        public IsOneByteSizeValueTransformer(IntPtr handle) : base(handle)
        {
        }

        public override NSObject TransformedValue(NSObject value)
        {
            var number = value as NSNumber;
            if (number == null) return base.TransformedValue(value);

            return new NSNumber(number.Int32Value == 1);
        }

        public override NSObject ReverseTransformedValue(NSObject value)
        {
			var number = value as NSNumber;
			if (number == null) return base.TransformedValue(value);

            return new NSNumber(number.BoolValue ? 1 : 2);
        }
    }
}
