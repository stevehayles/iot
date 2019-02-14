using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Iot.Device.RfidRc522
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Uid
    {
        private const int ValueSize = 10;
        public byte Size;
        private fixed byte _value[ValueSize];
        public Span<byte> UnsafeValue
        {
            get
            {
                fixed (byte* v = _value)
                {
                    return new Span<byte>(v, ValueSize);
                }
            }
        }

        // Select acknowledgement
        public byte Sak;

        internal Span<byte> GetSpan()
        {
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
        }
    }
}
