using Nmea.Extensions;
using System;
using System.Buffers;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Nmea.Sentences
{
    public abstract class NmeaSentence
    {
        private static ReadOnlySpan<byte> Delimiter => new byte[] { (byte)',' };
        private int _checksum;

        public static Delegate CreateNmeaSentenceFactoryDelegate(Type type)
        {
            var methodInfo = typeof(NmeaSentence)
                .GetMethod("CreateActivator", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(type);

            var activator = typeof(Func<,,>).MakeGenericType(typeof(string), typeof(string), type);
            var factory = typeof(Func<>).MakeGenericType(activator);

            return (Delegate)Delegate.CreateDelegate(factory, methodInfo).DynamicInvoke();
        }

#pragma warning disable IDE0051 // This method is referenced from CreateNmeaSentenceFactoryDelegate
        private static Func<string, string, T> CreateActivator<T>()
#pragma warning restore IDE0051
        {
            ConstructorInfo ctor = typeof(T).GetConstructor(new[] { typeof(string), typeof(string) });

            ParameterExpression param1 = Expression.Parameter(typeof(string), "talker");
            ParameterExpression param2 = Expression.Parameter(typeof(string), "sentence");

            NewExpression newExp = Expression.New(ctor, new Expression[] { param1, param2 });
            LambdaExpression lambda = Expression.Lambda(typeof(Func<string, string, T>), newExp, new[] { param1, param2 });

            Func<string, string, T> compiled = (Func<string, string, T>)lambda.Compile();
            return compiled;
        }

        public NmeaSentence(string talkerId, string sentence)
        {
            TalkerId = talkerId;
            Sentence = sentence;

            UpdateChecksum($"{TalkerId}{Sentence}".AsSpanUTF8());
        }

        public string Sentence { get; private set; }

        public string TalkerId { get; private set; }

        public void AppendField(int index, ReadOnlySpan<byte> field)
        {
            UpdateChecksum(Delimiter);
            UpdateChecksum(field);

            OnAppendField(index, field.AsSpanUTF8());
        }

        protected abstract void OnAppendField(int index, ReadOnlySpan<char> field);

        private void UpdateChecksum(ReadOnlySpan<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                _checksum ^= span[i];
            }
        }

        protected double ParseLatLng(ReadOnlySpan<char> field, LatLngType latLngType)
        {
            int sizeOfDegrees = (latLngType == LatLngType.Latitude) ? 2 : 3;

            if (field.IsEmpty || field.Length < sizeOfDegrees)
                return 0d;

            if (int.TryParse(field.Slice(0, sizeOfDegrees), out var degrees))
            {
                if (double.TryParse(field.Slice(sizeOfDegrees), out var decimalDegrees))
                {
                    return degrees + (decimalDegrees / 60f);
                }
            }

            return 0d;
        }

        protected string ParseText(ReadOnlySpan<char> field)
        {
            return field.ToString();
        }

        protected int ParseInt(ReadOnlySpan<char> field)
        {
            if (!field.IsEmpty && int.TryParse(field, out var value))
                return value;

            return 0;
        }

        protected double ParseDouble(ReadOnlySpan<char> field)
        {
            if (!field.IsEmpty && double.TryParse(field, out var value))
                return value;

            return 0d;
        }

        protected string ParseTextAndExecuteIfMatches(ReadOnlySpan<char> field, string match, Action action)
        {
            var text = field.ToString();

            if (string.Compare(text, match, StringComparison.OrdinalIgnoreCase) == 0)
            {
                action();
            }

            return text;
        }

        protected TimeSpan ParseTime(ReadOnlySpan<char> field)
        {
            var point = new ReadOnlySpan<char>(new[] { '.' });

            if (!int.TryParse(field.Slice(0, 2), out var hours))
                hours = 0;

            if (!int.TryParse(field.Slice(2, 2), out var minutes))
                minutes = 0;

            if (!int.TryParse(field.Slice(4, 2), out var seconds))
                seconds = 0;
            
            int milliseconds = 0;

            // if the span field contains fractional seconds (which are optional) then 
            // extract the milliseconds value
            if (field.Contains(point, StringComparison.OrdinalIgnoreCase) && field.Length > 7)
            {
                int.TryParse(field.Slice(7), out milliseconds);
            }

            return new TimeSpan(0, hours, minutes, seconds, milliseconds);
        }

        protected DateTimeOffset ParseDate(ReadOnlySpan<char> field, params string[] formats)
        {
            if (!DateTime.TryParseExact(field, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                dt = DateTime.MinValue;

            return new DateTimeOffset(dt);
        }

        protected ReadOnlySpan<char> ParseAisPayload(ReadOnlySpan<char> field, int numBits)
        {
            var buffer  = ArrayPool<char>.Shared.Rent((field.Length * 6) + 2); // ensure we are over size
            Array.Fill(buffer, '0');
            var result = buffer.AsSpan();

            int position = 0;
            try
            {
                foreach (var c in field)
                {
                    var b = (byte)c - 48;

                    if (b > 40)
                        b -= 8;

                    var value = ConvertToBase(b, 2);
                    value.CopyTo(result.Slice(position + 6 - value.Length, value.Length));
                    position += 6;
                }

                var remainder = (position + numBits) % 6;

                if (remainder != 0)
                    numBits += 6 - remainder;

                return result.Slice(0, (field.Length * 6) + numBits);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        public static ReadOnlySpan<char> ConvertToBase(long value, int radix)
        {
            if (value == 0)
                return "0".AsSpan();

            const int bitLength = 64;
            var Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan();

            int index = bitLength - 1;

            var buffer = ArrayPool<char>.Shared.Rent(bitLength);
            try
            {
                while (value != 0)
                {
                    int remainder = (int)(value % radix);
                    buffer.SetValue(Digits[remainder], index--);
                    value /= radix;
                }

                return buffer.AsSpan().Slice(index + 1, bitLength - index - 1);
            }
            finally 
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        protected enum LatLngType
        {
            Latitude,
            Longitude
        }

        public string ChecksumString => _checksum.ToString("X2");
    }
}
