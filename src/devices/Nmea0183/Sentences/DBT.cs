using System;
using System.Collections.Generic;
using System.Text;

namespace Nmea.Sentences
{
    [NmeaSentence("DBT")]
    public class DBT : NmeaSentence
    {
        public DBT(string talkerId, string sentence) : base(talkerId, sentence) { }

        public double DepthFeet { get; private set; }

        public double DepthMetres { get; private set; }

        public double DepthFathoms { get; private set; }

        protected override void OnAppendField(int index, ReadOnlySpan<char> field)
        {
            switch (index)
            {
                case 1:
                    DepthFeet = ParseDouble(field);
                    break;
                case 3:
                    DepthMetres = ParseDouble(field);
                    break;
                case 5:
                    DepthFathoms = ParseDouble(field);
                    break;
            }
        }
    }
}
