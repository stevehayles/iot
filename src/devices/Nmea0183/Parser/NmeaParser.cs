using Nmea.Extensions;
using Nmea.Sentences;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using State = Nmea.Parser.NmeaParseState;

namespace Nmea.Parser
{
    public class NmeaMessageEventArgs : EventArgs
    {
        public string Identifier { get; set; }
        public NmeaSentence Sentence { get; set; }
    }

    class NmeaParser
    {
        public event EventHandler<NmeaMessageEventArgs> MessageReady;

        private readonly StateMachine<State> _nmeaState;

        private readonly Lazy<Dictionary<string, Delegate>> _sentenceTypes = new Lazy<Dictionary<string, Delegate>>(() =>
        {
            var nmeaAttributes = typeof(NmeaSentence).Assembly.GetTypes().AsParallel()
                .Where(type => type.BaseType == typeof(NmeaSentence))
                .Select(type => (attribute: type.GetCustomAttributes<NmeaSentenceAttribute>(false).FirstOrDefault(), type))
                .Where(a => a.attribute != null);

            return nmeaAttributes.ToDictionary(a => a.attribute.Sentence, a =>  NmeaSentence.CreateNmeaSentenceFactoryDelegate(a.type));
        });

        public NmeaParser()
        {
            int currentIndex = 0;
            NmeaSentence currentMessage = null;

            const byte Delimiter = (byte)',';
            const byte Checksum = (byte)'*';
            const byte CR = (byte)'\r';
            const byte LF = (byte)'\n';

            byte[] StartOfSentence = new byte[] { (byte)'$', (byte)'!' };
            byte[] DelimiterOrChecksum = new byte[] { Delimiter, Checksum };
            byte[] CRLF = new byte[] { CR, LF };

            _nmeaState = new StateMachine<State>();

            _nmeaState[State.WaitingForStart] = (ref SequenceReader<byte> reader) =>
            {
                if (reader.TryReadToAny(out ReadOnlySpan<byte> tmp, StartOfSentence, advancePastDelimiter: true))
                {
                    currentMessage = default;
                    currentIndex = 0;
                   
                    return State.ReadingIdentifier;
                }

                // The TryReadToAny call has not found an SOM symbol but the current state expects it
                // Advance the reader to the end of the sequence to allow processing to continue
                reader.Advance(reader.Remaining);

                return State.WaitingForStart;
            };

            _nmeaState[State.ReadingIdentifier] = (ref SequenceReader<byte> reader) =>
            {
                if (reader.TryReadTo(out ReadOnlySpan<byte> bytes, Delimiter, advancePastDelimiter: true))
                {
                    if (bytes.Length != 5)
                        return State.WaitingForStart;

                    var talkerId = bytes.Slice(0, 2).AsStringUTF8();
                    var sentence = bytes.Slice(2, 3).AsStringUTF8();

                    if (_sentenceTypes.Value.TryGetValue(sentence, out var sentenceType))
                    {
                        currentMessage = (NmeaSentence)sentenceType.DynamicInvoke(talkerId, sentence);
                    }
                    else
                    {
                        return State.WaitingForStart;
                    }

                    return State.BuildingSentence;
                }

                return State.WaitingForStart;
            };

            _nmeaState[State.BuildingSentence] = (ref SequenceReader<byte> reader) =>
            {
                if (reader.TryReadToAny(out ReadOnlySpan<byte> span, DelimiterOrChecksum, advancePastDelimiter: false))
                {
                    currentMessage.AppendField(++currentIndex, span);

                    if (reader.TryPeek(out var nextChar))
                    {
                        switch (nextChar)
                        {
                            case Checksum:
                                return State.ReadingChecksum;
                            case CR:
                            case LF:
                                return State.ChecksumError;
                        }
                    }

                    reader.Advance(1);
                    return State.BuildingSentence;
                }

                return State.WaitingForStart;
            };

            _nmeaState[State.ReadingChecksum] = (ref SequenceReader<byte> reader) =>
            {
                if (reader.TryReadToAny(out ReadOnlySpan<byte> bytes, CRLF, advancePastDelimiter: false))
                {
                    var receivedChecksum = bytes.Slice(1, 2);

                    if (receivedChecksum.SequenceEqual(currentMessage.ChecksumString.AsSpanUTF8()))
                        return State.SentenceReady;
                }

                return State.ChecksumError;
            };

            _nmeaState[State.ChecksumError] = (ref SequenceReader<byte> reader) =>
            {
                reader.AdvancePastAny(CRLF);
                return State.WaitingForStart;
            };

            _nmeaState[State.SentenceReady] = (ref SequenceReader<byte> reader) =>
            {
                MessageReady?.Invoke(this, new NmeaMessageEventArgs() { Identifier = currentMessage.Sentence, Sentence = currentMessage });

                reader.AdvancePastAny(CRLF);
                return State.WaitingForStart;
            };
        }

        public void Update(ref SequenceReader<byte> reader) => _nmeaState.Update(ref reader);
    }
}
