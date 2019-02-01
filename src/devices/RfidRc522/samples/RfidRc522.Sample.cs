// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.I2c.Drivers;
using System.Device.Spi;
using System.Device.Spi.Drivers;
using System.Threading;
using Iot.Device.RfidRc522;

namespace Iot.Device.RfidRc522.Samples
{

    class Program
    {
        static SpiDevice CreateSpiDevice()
        {
            var connection = new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = 500000,
                Mode = SpiMode.Mode0
            };

            return new UnixSpiDevice(connection);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello RFID Sample!");

            int resetPin = 24;

            using (SpiDevice spi = CreateSpiDevice())
            using (MFRC522 mfrc522 = new MFRC522(spi, resetPin))
            {
                // defaults
                mfrc522.RegisterSet(MFRC522Register.TxModeReg, 0x00);
                mfrc522.RegisterSet(MFRC522Register.RxModeReg, 0x00);
                mfrc522.RegisterSet(MFRC522Register.ModWidthReg, 0x26);

                //
                mfrc522.RegisterSet(MFRC522Register.TModeReg, 0x80);
                mfrc522.RegisterSet(MFRC522Register.TPrescalerReg, 0xA9);
                mfrc522.RegisterSet(MFRC522Register.TReloadRegHigh, 0x03);
                mfrc522.RegisterSet(MFRC522Register.TReloadRegLow, 0xE8);
                mfrc522.RegisterSet(MFRC522Register.TxASKReg, 0x40);

                mfrc522.RegisterSet(MFRC522Register.ModeReg, 0x3D);

                mfrc522.TurnOnAntenna();

                //int numBytes = mfrc522.RegisterGet(MFRC522Register.FIFOLevelReg);
                //Console.WriteLine($"FIFO has {numBytes} bytes");
                //ReadOnlySpan<byte> fifo = mfrc522.RegisterRead(MFRC522Register.FIFODataReg, numBytes);
                //Console.WriteLine(string.Join(",", fifo.ToArray()));
                //Console.WriteLine("Setting FIFO");
                //mfrc522.RegisterSet(MFRC522Register.FIFODataReg, new byte[5] { 1, 2, 3, 4, 5 });

                //numBytes = mfrc522.RegisterGet(MFRC522Register.FIFOLevelReg);
                //Console.WriteLine($"FIFO has {numBytes} bytes");
                //fifo = mfrc522.RegisterRead(MFRC522Register.FIFODataReg, numBytes);
                //Console.WriteLine(string.Join(",", fifo.ToArray()));

                //numBytes = mfrc522.RegisterGet(MFRC522Register.FIFOLevelReg);
                //Console.WriteLine($"FIFO has {numBytes} bytes");
                //fifo = mfrc522.RegisterRead(MFRC522Register.FIFODataReg, numBytes);
                //Console.WriteLine(string.Join(",", fifo.ToArray()));

                //Thread.Sleep(50);

                foreach (MFRC522Register reg in typeof(MFRC522Register).GetEnumValues())
                {
                    byte val = mfrc522.RegisterGet(reg);
                    Console.WriteLine($"{reg} = {val} [{val.ToString("X2")}] [{Convert.ToString(val, 2).PadLeft(8, '0')}]");
                }

                while (true)
                {
                    if (!mfrc522.PICC_IsNewCardPresent())
                        continue;

                    Console.WriteLine("New card is present");
                    //if (!mfrc522.PICC_ReadCardSerial())
                    //    continue;

                    //Console.WriteLine(mfrc522.PICC_GetSerial());
                }
            }
        }
    }
}
