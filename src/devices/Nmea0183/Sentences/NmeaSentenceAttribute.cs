using System;
using System.Collections.Generic;
using System.Text;

namespace Nmea.Sentences
{
    class NmeaSentenceAttribute : Attribute
    {
        public NmeaSentenceAttribute(string sentence)
        {
            Sentence = sentence;
        }

        public string Sentence { get; private set; }
    }
}
