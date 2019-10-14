using Nmea.Sentences;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nmea.Sentences
{
    [NmeaSentence("VDO")]
    public class VDO : NmeaSentence
    {
        public VDO(string talkerId, string sentence) : base(talkerId, sentence) { }

        public string AisPayload { get; private set; }

        private int FragmentCount { get; set; }

        private int FragmentNumber { get; set; }

        private int SequenceId { get; set; }

        public string ChannelCode { get; private set; }

        public int FillBits { get; private set; }

        protected override void OnAppendField(int index, ReadOnlySpan<char> field)
        {
            switch (index)
            {
                case 1:
                    FragmentCount = ParseInt(field);
                    break;
                case 2:
                    FragmentNumber = ParseInt(field);
                    break;
                case 3:
                    FragmentNumber = ParseInt(field);
                    break;
                case 4:
                    ChannelCode = ParseText(field);
                    break;
                case 5:
                    AisPayload = ParseAisPayload(field, 0).ToString();
                    break;
                case 6:
                    FillBits = ParseInt(field);
                    break;
            }
        }
    }
}
