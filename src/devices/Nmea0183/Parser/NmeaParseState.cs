using System;
using System.Collections.Generic;
using System.Text;

namespace Nmea.Parser
{
    public enum NmeaParseState
    {
        WaitingForStart,
        ReadingIdentifier,
        BuildingSentence,
        ReadingChecksum,
        ChecksumError,
        SentenceReady
    }
}
