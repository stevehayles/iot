// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.I2c.Drivers;
using System.Device.Spi;
using System.Device.Spi.Drivers;
using System.Threading;
using System.Runtime.CompilerServices;

// Resources:
// - https://www.nxp.com/docs/en/data-sheet/MFRC522.pdf
// - https://github.com/miguelbalboa/rfid/tree/master/src
// - https://github.com/dalmirdasilva/ArduinoRadioFrequencyIdentification/tree/master/MifareClassic/datasheet

namespace Iot.Device.RfidRc522
{
    public class MFRC522 : IDisposable
    {
        const byte ReadMask = 0x80;
        const byte WriteMask = 0x00;
        const int BufferSize = 65;

        GpioController _controller;
        int _resetPin;
        SpiDevice _spi;

        byte[] _read = new byte[BufferSize];
        byte[] _write = new byte[BufferSize];
        int _pos = 0;
        int _inTransaction = 0;

        public MFRC522(SpiDevice spi, int resetPin)
        {
            _spi = spi;
            _resetPin = resetPin;

            _controller = new GpioController();
            _controller.OpenPin(resetPin, PinMode.Output);
            HardReset();
        }

        private void HardReset()
        {
            _controller.Write(_resetPin, PinValue.Low);
            // at least 100ns
            Thread.Sleep(TimeSpan.FromMilliseconds(0.1));
            _controller.Write(_resetPin, PinValue.High);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SpiBegin()
        {
            _inTransaction++;
            _pos = 0;

            if (_inTransaction > 1)
                throw new InvalidOperationException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SpiEnd()
        {
            _inTransaction--;

            if (_inTransaction < 0)
                throw new InvalidOperationException();

            Span<byte> r = new Span<byte>(_read, 0, _pos);
            Span<byte> w = new Span<byte>(_write, 0, _pos);
            _spi.TransferFullDuplex(w, r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte SpiReadByte()
        {
            if (_inTransaction != 0)
                throw new InvalidOperationException();

            return _read[_pos - 1];
        }

        private ReadOnlySpan<byte> SpiRead(int numBytes)
        {
            if (_inTransaction != 0)
                throw new InvalidOperationException();

            if (numBytes > _pos)
                throw new ArgumentOutOfRangeException(nameof(numBytes));

            return new ReadOnlySpan<byte>(_read, _pos - numBytes, numBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SpiWrite(byte data = 0x00)
        {
            if (_inTransaction != 1)
                throw new InvalidOperationException();

            _write[_pos++] = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SpiWrite(ReadOnlySpan<byte> data)
        {
            if (_inTransaction != 1)
                throw new InvalidOperationException();

            data.CopyTo(new Span<byte>(_write, _pos, data.Length));
            _pos += data.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendWriteToRegister(MFRC522Register reg)
        {
            byte addressMask = (byte)((byte)reg << 1);
            SpiWrite((byte)(WriteMask | addressMask));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendReadFromRegister(MFRC522Register reg)
        {
            byte addressMask = (byte)((byte)reg << 1);
            SpiWrite((byte)(ReadMask | addressMask));
        }

        public byte RegisterGet(MFRC522Register reg)
        {
            SpiBegin();
            SendReadFromRegister(reg);
            SpiWrite();
            SpiEnd();
            return SpiReadByte();
        }

        public ReadOnlySpan<byte> RegisterRead(MFRC522Register reg, int numBytes)
        {
            if (numBytes == 0)
                return ReadOnlySpan<byte>.Empty;

            SpiBegin();

            for (int i = 0; i < numBytes; i++)
            {
                SendReadFromRegister(reg);
            }

            SpiWrite();
            SpiEnd();

            return SpiRead(numBytes);
        }

        public void RegisterRead(MFRC522Register reg, Span<byte> data, byte rxAlign)
        {
            if (data.IsEmpty)
                return;

            ReadOnlySpan<byte> dataRead = RegisterRead(reg, data.Length);

            if (rxAlign != 0)
            {
                byte mask = (byte)((0xFF << rxAlign) & 0xFF);
                data[0] = (byte)((data[0] & ~mask) | (dataRead[0] & mask));
                dataRead.Slice(1).CopyTo(data.Slice(1));
            }
            else
            {
                dataRead.CopyTo(data);
            }
        }

        public void RegisterSet(MFRC522Register reg, ReadOnlySpan<byte> b)
        {
            SpiBegin();
            SendWriteToRegister(reg);
            SpiWrite(b);
            SpiEnd();
        }

        public void RegisterSet(MFRC522Register reg, byte b)
        {
            SpiBegin();
            SendWriteToRegister(reg);
            SpiWrite(b);
            SpiEnd();
        }

        public void RegisterSetMask(MFRC522Register reg, byte mask)
        {
            byte r = RegisterGet(reg);
            //if ((r & mask) != mask)
            {
                RegisterSet(reg, (byte)(r | mask));
            }
        }

        public void RegisterClearMask(MFRC522Register reg, byte mask)
        {
            byte r = RegisterGet(reg);
            //if ((r & mask) != 0)
            {
                RegisterSet(reg, (byte)(r & ~mask));
            }
        }

        private StatusCode PCD_CommunicateWithPICC(
            MFRC52Command command,
            byte waitIrq,
            ReadOnlySpan<byte> sendData,
            Span<byte> receiveData,
            out int dataWritten,
            ref byte validBits,
            byte rxAlign,
            bool checkCrc = false)
        {
            dataWritten = -1;

            byte txLastBits = validBits;
            byte bitFraming = (byte)((rxAlign << 4) + txLastBits);
            // Stop any active command
            RegisterSet(MFRC522Register.CommandReg, (byte)MFRC52Command.Idle);

            // Clear all interrupt request bits
            RegisterSet(MFRC522Register.ComIrqReg, 0x7F);

            // FlushBuffer = 1, FIFO initialization
            RegisterSet(MFRC522Register.FIFOLevelReg, 0x80);
            RegisterSet(MFRC522Register.FIFODataReg, sendData);
            RegisterSet(MFRC522Register.BitFramingReg, bitFraming);
            RegisterSet(MFRC522Register.CommandReg, (byte)command);

            if (command == MFRC52Command.Transceive)
            {
                RegisterSetMask(MFRC522Register.BitFramingReg, 0x80);
            }

            //Console.WriteLine("start loop");
            Stopwatch sw = Stopwatch.StartNew();
            bool timeout = true;
            while (sw.Elapsed.TotalMilliseconds < 26)
            {
                byte n = RegisterGet(MFRC522Register.ComIrqReg);
                if ((n & waitIrq) != 0)
                {
                    timeout = false;
                    break;
                }

                if ((n & 0x01) != 0)
                {
                    //Console.WriteLine("timeout (hardware)");
                    return StatusCode.Timeout;
                }
            }

            // At Least 35.75ms have elapsed, communication might be down
            if (timeout)
            {
                //Console.WriteLine("timeout (software)");
                return StatusCode.Timeout;
            }

            //Console.WriteLine("no timeout");

            byte errorRegValue = RegisterGet(MFRC522Register.ErrorReg);

            if ((errorRegValue & 0x13) != 0)
                return StatusCode.Error;

            byte validBitsTmp = 0;

            if (receiveData.Length > 0)
            {
                // number of bytes in the FIFO
                byte n = RegisterGet(MFRC522Register.FIFOLevelReg);
                if (n > receiveData.Length)
                    return StatusCode.NoRoom;

                RegisterRead(MFRC522Register.FIFODataReg, receiveData.Slice(0, n), rxAlign);

                validBits = validBitsTmp;
                dataWritten = n;
            }

            if ((errorRegValue & 0x08) != 0)
            {
                return StatusCode.Collision;
            }

            if (receiveData.Length > 0 && checkCrc)
            {
                throw new NotImplementedException("CRC not supported yet");
            }

            return StatusCode.Ok;
        }

        private StatusCode PCD_TransceiveData(
            ReadOnlySpan<byte> sendData,
            Span<byte> receiveData,
            out int bytesWritten,
            ref byte validBits,
            byte rxAlign = 0,
            bool checkCrc = false)
        {
            const byte waitIrq = 0x30;
            return PCD_CommunicateWithPICC(
                MFRC52Command.Transceive,
                waitIrq,
                sendData,
                receiveData,
                out bytesWritten,
                ref validBits,
                rxAlign,
                checkCrc);
        }

        public void TurnOnAntenna()
        {
            byte val = RegisterGet(MFRC522Register.TxControlReg);
            if ((val & 0x03) != 0x03)
            {
                RegisterSet(MFRC522Register.TxControlReg, (byte)(val | 0x03));
            }
            //RegisterSetMask(MFRC522Register.TxControlReg, 0x03);
        }

        private StatusCode PICC_REQA_or_WUPA(PiccCommand command, Span<byte> atqa, out int bytesWritten)
        {
            bytesWritten = -1;

            if (atqa.Length < 2)
                return StatusCode.NoRoom;

            RegisterClearMask(MFRC522Register.CollReg, 0x80);
            byte validBits = 7;

            Span<byte> sendData = stackalloc byte[1];
            sendData[0] = (byte)command;
            StatusCode status = PCD_TransceiveData(sendData, atqa, out bytesWritten, ref validBits);

            if (status != StatusCode.Ok)
                return status;

            if (bytesWritten != 2 || validBits != 0)
                return StatusCode.Error;

            return StatusCode.Ok;
        }

        private StatusCode PICC_RequestA(Span<byte> atqa, out int bytesWritten)
        {
            return PICC_REQA_or_WUPA(PiccCommand.PICC_CMD_REQA, atqa, out bytesWritten);
        }

        private StatusCode CalculateCRC(ReadOnlySpan<byte> data, Span<byte> result)
        {
            Debug.Assert(result.Length == 2, "result length must be exactly 2 bytes");
            
            RegisterSet(MFRC522Register.CommandReg, (byte)MFRC52Command.Idle);

            RegisterSet(MFRC522Register.DivIrqReg, 0x04);
            RegisterSet(MFRC522Register.FIFOLevelReg, 0x80);
            RegisterSet(MFRC522Register.FIFODataReg, data);
            RegisterSet(MFRC522Register.CommandReg, (byte)MFRC52Command.CalcCRC);

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 89)
            {
                byte n = RegisterGet(MFRC522Register.DivIrqReg);
                if ((n & 0x04) != 0)
                {
                    RegisterSet(MFRC522Register.CommandReg, (byte)MFRC52Command.Idle);
                    result[0] = RegisterGet(MFRC522Register.CRCResultRegLow);
                    result[1] = RegisterGet(MFRC522Register.CRCResultRegHigh);
                    return StatusCode.Ok;
                }
            }

            return StatusCode.Timeout;
        }

        private StatusCode PICC_Select(ref Uid uid, byte validBits = 0)
        {
            if (validBits > 80)
                return StatusCode.Invalid;

            RegisterClearMask(MFRC522Register.CollReg, 0x80);

            StatusCode result;
            Span<byte> buffer = stackalloc byte[9];
            Span<byte> responseBuffer = buffer.Slice(0, 0);
            byte uidIndex;
            byte index;
            byte cascadeLevel = 1;
            bool useCascadeTag;
            byte count;
            byte checkBit;
            byte txLastBits = 0; // !
            byte bufferUsed;
            byte rxAlign;

            bool uidComplete = false;
            while (!uidComplete)
            {
                switch (cascadeLevel)
                {
                    case 1:
                    {
                        buffer[0] = (byte)PiccCommand.PICC_CMD_SEL_CL1;
                        uidIndex = 0;
                        useCascadeTag = validBits != 0 && uid.Size > 4;
                        break;
                    }
                    case 2:
                    {
                        buffer[0] = (byte)PiccCommand.PICC_CMD_SEL_CL2;
                        uidIndex = 3;
                        useCascadeTag = validBits != 0 && uid.Size > 7;
                        break;
                    }
                    case 3:
                    {
                        buffer[0] = (byte)PiccCommand.PICC_CMD_SEL_CL3;
                        uidIndex = 6;
                        useCascadeTag = false;
                        break;
                    }
                    default: 
                        return StatusCode.InternalError;
                }

                sbyte currentLevelKnownBits = (sbyte)(validBits - (8 * uidIndex));
                if (currentLevelKnownBits < 0)
                {
                    currentLevelKnownBits = 0;
                }

                index = 2;

                if (useCascadeTag)
                {
                    buffer[index++] = (byte)PiccCommand.PICC_CMD_CT;
                }

                byte bytesToCopy = (byte)(currentLevelKnownBits / 8 + (currentLevelKnownBits % 8 != 0 ? 1 : 0));

                if (bytesToCopy > 0)
                {
                    byte maxBytes = useCascadeTag ? (byte)3 : (byte)4;

                    if (bytesToCopy > maxBytes)
                    {
                        bytesToCopy = maxBytes;
                    }

                    for (int i = 0; i < bytesToCopy; i++)
                    {
                        buffer[i++] = uid.UnsafeValue[uidIndex + i];
                    }
                }

                if (useCascadeTag)
                {
                    currentLevelKnownBits += 8;
                }

                bool selectDone = false;
                while (!selectDone)
                {
                    if (currentLevelKnownBits >= 32)
                    {
                        buffer[1] = 0x70;
                        buffer[6] = (byte)(buffer[2] ^ buffer[3] ^ buffer[4] ^ buffer[5]);
                        result = CalculateCRC(buffer.Slice(0, 7), buffer.Slice(7, 2));
                        
                        if (result != StatusCode.Ok)
                        {
                            return result;
                        }

                        txLastBits = 0;
                        bufferUsed = 9;
                        responseBuffer = buffer.Slice(6, 3);
                    }
                    else // ANTI COLLISION
                    {
                        txLastBits = (byte)(currentLevelKnownBits % 8);
                        count = (byte)(currentLevelKnownBits / 8);
                        index = (byte)(2 + count);
                        buffer[1] = (byte)((index << 4) + txLastBits);
                        bufferUsed = (byte)(index + (txLastBits != 0 ? 1 : 0));
                        responseBuffer = buffer.Slice(index);
                    }
                    
                    rxAlign = txLastBits;
                    RegisterSet(MFRC522Register.BitFramingReg, (byte)((rxAlign << 4) + txLastBits));

                    result = PCD_TransceiveData(buffer.Slice(0, bufferUsed), responseBuffer, out int responseLen, ref txLastBits, rxAlign);
                    if (result == StatusCode.Ok || result == StatusCode.Collision)
                    {
                        //Console.WriteLine($"respBufferLen: {responseBuffer.Length}, outlen={responseLen}");
                        responseBuffer = responseBuffer.Slice(0, responseLen);
                    }
                    else
                    {
                        //Console.WriteLine($"respBufferLen: error");
                    }

                    if (result == StatusCode.Collision)
                    {
                        //Console.WriteLine("transceive: collision");
                        byte valueOfCollReg = RegisterGet(MFRC522Register.CollReg);
                        if ((valueOfCollReg & 0x20) != 0)
                        {
                            return StatusCode.Collision;
                        }

                        byte collisionPos = (byte)(valueOfCollReg & 0x1F);
                        if (collisionPos == 0)
                        {
                            collisionPos = 32;
                        }

                        if (collisionPos <= currentLevelKnownBits)
                        {
                            return StatusCode.InternalError;
                        }

                        currentLevelKnownBits = (sbyte)collisionPos;
                        count = (byte)(currentLevelKnownBits % 8);
                        checkBit = (byte)((currentLevelKnownBits - 1) % 8);
                        index = (byte)(1 + (currentLevelKnownBits / 8) + (count != 0 ? 1 : 0));
                        buffer[index] |= (byte)(1 << checkBit);
                    }
                    else if (result != StatusCode.Ok)
                    {
                        //Console.WriteLine("transceive: error");
                        return result;
                    }
                    else
                    {
                        //Console.WriteLine("transceive: ok");
                        if (currentLevelKnownBits >= 32)
                        {
                            selectDone = true;
                        }
                        else
                        {
                            currentLevelKnownBits = 32;
                        }
                    }
                } // while (!selectDone)
                
                index = (buffer[2] == (byte)PiccCommand.PICC_CMD_CT) ? (byte)3 : (byte)2;
                bytesToCopy = (buffer[2] == (byte)PiccCommand.PICC_CMD_CT) ? (byte)3 : (byte)4;

                Span<byte> uidBytes = uid.UnsafeValue;
                for (count = 0; count < bytesToCopy; count++)
                {
                    uidBytes[uidIndex + count] = buffer[index++];
                }

                if (responseBuffer.Length != 3 || txLastBits != 0)
                {
                    return StatusCode.Error;
                }

                result = CalculateCRC(responseBuffer.Slice(0, 1), buffer.Slice(2, 2));

                if (result != StatusCode.Ok)
                {
                    return result;
                }

                if ((buffer[2] != responseBuffer[1]) || (buffer[3] != responseBuffer[2]))
                {
                    return StatusCode.CrcMismatch;
                }

                if ((responseBuffer[0] & 0x04) != 0)
                {
                    cascadeLevel++;
                }
                else
                {
                    uidComplete = true;
                    uid.Sak = responseBuffer[0];
                }
            } // while (!uidComplete)

            uid.Size = (byte)(3 * cascadeLevel + 1);
            return StatusCode.Ok;
        }

        public bool PICC_IsNewCardPresent()
        {
            // Reset baud rates
            RegisterSet(MFRC522Register.TxModeReg, 0x00);
            RegisterSet(MFRC522Register.RxModeReg, 0x00);

            // Reset ModWidthReg
            RegisterSet(MFRC522Register.ModWidthReg, 0x26);

            Span<byte> atqa = stackalloc byte[2];
            StatusCode result = PICC_RequestA(atqa, out int bytesWritten);
            return result == StatusCode.Ok || result == StatusCode.Collision;
        }

        public bool PICC_ReadCardSerial(ref Uid uid)
        {
            StatusCode result = PICC_Select(ref uid);
            return result == StatusCode.Ok;
        }

        //private void WriteToCommandRegister(MFRC52Command command)
        //{
        //    //
        //    WriteToRegister(MFRC522Register.CommandReg, (byte)((byte)(0x3 << 4) | (byte)command));
        //    throw new NotImplementedException();
        //}

        public void Dispose()
        {
            _controller?.Dispose();
            _controller = null;
            _spi?.Dispose();
            _spi = null;
        }
    }
}
