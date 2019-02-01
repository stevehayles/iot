using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.RfidRc522
{
    // https://www.nxp.com/docs/en/data-sheet/MFRC522.pdf
    // 10.3.1
    public enum MFRC52Command : byte
    {
        Idle = 0b000,
        Mem = 0b0001,
        GenerateRandomId = 0b0010,
        CalcCRC = 0b0011,
        Transmit = 0b0100,
        NoCmdChange = 0b0111,
        Receive = 0b1000,
        Transceive = 0b1100,
        MFAuthent = 0b1110,
        SoftReset = 0b1111,
    }
}
