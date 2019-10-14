using Nmea.Sentences;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Iot.Device.Nmea.Samples
{
    class Program
    {
        const string SERIAL_PORT_NAME = "com98";

        static async Task Main(string[] args)
        {
            SerialPort sp = new SerialPort(SERIAL_PORT_NAME);
            sp.Open();

            Nmea0183 nmea = new Nmea0183(sp.BaseStream);

            _ = Task.Run(async () =>
            {
                await foreach (var sentence in nmea.Messages())
                {
                    switch (sentence)
                    {
                        case GLL gll:
                            Console.WriteLine($"{gll.TalkerId} {gll.Sentence} Lat: {gll.Latitude} Lng: {gll.Longitude} {gll.ChecksumString}");
                            break;

                        case ZDA zda:
                            Console.WriteLine($"{zda.TalkerId} {zda.Sentence} Time: {zda.TimeOfDay} Day: {zda.Day} Month: {zda.Month} Year: {zda.Year} ");
                            break;

                        case RMC rmc:
                            Console.WriteLine($"{rmc.TalkerId} {rmc.Sentence} Time: {rmc.TimeOfPosition} Lat: {rmc.Latitude} Lng: {rmc.Longitude} MagVar: {rmc.MagVar} ");
                            break;

                        case VDO vdo:
                            Console.WriteLine($"{vdo.TalkerId} {vdo.Sentence} Payload: {vdo.AisPayload.Substring(0,40)}... Channel: {vdo.ChannelCode}");
                            break;

                        case DBT dbt:
                            Console.WriteLine($"{dbt.TalkerId} {dbt.Sentence} Metres: {dbt.DepthMetres} Feet: {dbt.DepthFeet} Fathoms: {dbt.DepthFathoms}");
                            break;
                    }
                }
            });

            await nmea.Start();
        }
    }
}
