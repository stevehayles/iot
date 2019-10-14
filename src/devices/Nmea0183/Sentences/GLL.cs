using System;

namespace Nmea.Sentences
{
    [NmeaSentence("GLL")]
    public class GLL : NmeaSentence
    {
        public GLL(string talkerId, string sentence) : base(talkerId, sentence) { }

        public double Latitude { get; private set; }

        public string LatitudeHemisphere { get; private set; }

        public double Longitude { get; private set; }

        public string LongitudeHemisphere { get; private set; }

        protected override void OnAppendField(int index, ReadOnlySpan<char> field)
        {
            switch (index)
            {
                case 1:
                    Latitude = ParseLatLng(field, LatLngType.Latitude);
                    break;
                case 2:
                    LatitudeHemisphere = ParseTextAndExecuteIfMatches(field, "S", () => Latitude *= -1);
                    break;
                case 3:
                    Longitude = ParseLatLng(field, LatLngType.Longitude);
                    break;
                case 4:
                    LongitudeHemisphere = ParseTextAndExecuteIfMatches(field, "W", () => Longitude *= -1);
                    break;           
            }
        }
    }

}
