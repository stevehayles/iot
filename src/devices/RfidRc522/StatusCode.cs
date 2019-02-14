using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.RfidRc522
{
    enum StatusCode
    {
        Ok,
        NoRoom,
        MifareNack,
        Collision,
        CrcMismatch,
        Timeout,
        Error,
        Invalid,
        InternalError,
    }
}
