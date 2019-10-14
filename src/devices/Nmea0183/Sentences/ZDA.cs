using System;

namespace Nmea.Sentences
{
    [NmeaSentence("ZDA")]
    public class ZDA : NmeaSentence
    {
        public ZDA(string talkerId, string sentence) : base(talkerId, sentence) { }

        public TimeSpan? TimeOfDay { get; private set; }

        public int? Day { get; private set; }

        public int? Month { get; private set; }

        public int? Year { get; private set; }

        public int? TimeZone { get; private set; }

        public DateTimeOffset? DateTime
        {
            get
            {
                return new DateTimeOffset(new DateTime(Year ?? 01, Month ?? 01, Day ?? 01), TimeOfDay ?? TimeSpan.Zero);
            }
        }

        protected override void OnAppendField(int index, ReadOnlySpan<char> field)
        {
            switch (index)
            {
                case 1:
                    TimeOfDay = ParseTime(field);
                    break;
                case 2:
                    Day = ParseInt(field);
                    break;
                case 3:
                    Month = ParseInt(field);
                    break;
                case 4:
                    Year = ParseInt(field);
                    break;
                case 5:
                    TimeZone = ParseInt(field);
                    break;
            }
        }

    }
}
