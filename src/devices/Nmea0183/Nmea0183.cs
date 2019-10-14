using Nmea.Extensions;
using Nmea.Parser;
using Nmea.Sentences;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Iot.Device.Nmea
{  
    public class Nmea0183 : IDisposable
    {
        private readonly BlockingCollection<NmeaSentence> _messageQueue;
        private readonly Stream _stream;
        private readonly NmeaParser _parser;

        public Nmea0183(Stream stream)
        {
            _messageQueue = new BlockingCollection<NmeaSentence>();

            _stream = stream;
            _parser = new NmeaParser();

            _parser.MessageReady += (s, e) => _messageQueue.Add(e.Sentence);
        }

        public async IAsyncEnumerable<NmeaSentence> Messages()
        {
            foreach (var stentence in _messageQueue.GetConsumingEnumerable())
            {
                yield return stentence;
                await Task.Delay(0);
            }
        }

        public Task Start(CancellationToken cancellationToken = default)
        {
            var pipe = new Pipe();
            Task writing = FillPipeAsync(pipe.Writer, _stream, cancellationToken);
            Task reading = ReadPipeAsync(pipe.Reader, _parser, cancellationToken);

            return Task.WhenAll(reading, writing);
        }

        async Task FillPipeAsync(PipeWriter pipeWriter, Stream stream, CancellationToken cancellationToken = default)
        {
            const int minimumBufferSize = 1024;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Memory<byte> memory = pipeWriter.GetMemory(minimumBufferSize);
                    try
                    {
                        int bytesRead = await stream.ReadAsync(memory, cancellationToken);

                        if (bytesRead == 0)
                            break;

                        pipeWriter.Advance(bytesRead);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        break;
                    }

                    FlushResult result = await pipeWriter.FlushAsync();

                    if (result.IsCompleted)
                        break;
                }
            }
            catch (OperationCanceledException) { }
        }

        async Task ReadPipeAsync(PipeReader pipeReader, NmeaParser parser, CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await pipeReader.ReadAsync();
                    var buffer = result.Buffer;

                    var position = HandleResponse(buffer, parser);

                    if (result.IsCompleted)
                        break;

                    pipeReader.AdvanceTo(position, buffer.End);
                }
            }
            catch (OperationCanceledException) { }

            pipeReader.Complete();
        }

        private SequencePosition HandleResponse(in ReadOnlySequence<byte> sequence, NmeaParser nmeaParser)
        {
            var reader = new SequenceReader<byte>(sequence);

            while (!reader.End)
            {
                nmeaParser.Update(ref reader);
            }

            return reader.Position;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _messageQueue?.Dispose();
        }
    }
}
