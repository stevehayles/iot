using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Nmea.Extensions
{
    static class SpanExtensions
    {
        public static ReadOnlySpan<char> AsSpanUTF8(this ReadOnlySpan<byte> source)
        {
            ReadOnlySpan<char> result;

            var charArray = ArrayPool<char>.Shared.Rent(source.Length);
            try
            {
                for (int i = 0; i < source.Length; i++)
                {
                    charArray.SetValue(source[i], i);
                }

                result = charArray.AsSpan(..source.Length);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charArray);
            }

            return result;
        }

        public static string AsStringUTF8(this ReadOnlySpan<byte> source)
        {
            return source.AsSpanUTF8().ToString();
        }

        public static ReadOnlySpan<byte> AsSpanUTF8(this string source)
        {
            ReadOnlySpan<byte> result;
            ReadOnlySpan<char> span = source.AsSpan();

            var byteArray = ArrayPool<byte>.Shared.Rent(source.Length);
            try
            {
                for (int i = 0; i < span.Length; i++)
                {
                    byteArray.SetValue((byte)span[i], i);
                }

                result = byteArray.AsSpan(..span.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }

            return result;
        }
    }
}
