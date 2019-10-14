using System;
using System.Collections.Generic;
using System.Text;

namespace Nmea.Sentences
{
    [NmeaSentence("RMC")]
    public class RMC : NmeaSentence
    {
        public RMC(string talkerId, string sentence) : base(talkerId, sentence) { }

        public TimeSpan TimeOfPosition { get; private set; }

        public string Status { get; private set; }

        public double? Latitude { get; private set; }

        public string LatitudeHemisphere { get; private set; }

        public double? Longitude { get; private set; }

        public string LongitudeHemisphere { get; private set; }

        public double? SpeedOverGround { get; private set; }

        public double? CourseOverGround { get; private set; }

        public DateTimeOffset? Date { get; private set; }

        public double? MagVar { get; private set; }

        public string MagVarDirection { get; private set; }

        public string FaaMode { get; private set; }

        protected override void OnAppendField(int index, ReadOnlySpan<char> field)
        {
            switch (index)
            {
                case 1:
                    TimeOfPosition = ParseTime(field);
                    break;
                case 2:
                    Status = ParseText(field);
                    break;
                case 3:
                    Latitude = ParseLatLng(field, LatLngType.Latitude);
                    break;
                case 4:
                    LatitudeHemisphere = ParseTextAndExecuteIfMatches(field, "S", () => Latitude *= -1);
                    break;
                case 5:
                    Longitude = ParseLatLng(field, LatLngType.Longitude);
                    break;
                case 6:
                    LongitudeHemisphere = ParseTextAndExecuteIfMatches(field, "W", () => Longitude *= -1);
                    break;
                case 7:
                    SpeedOverGround = ParseDouble(field);
                    break;
                case 8:
                    CourseOverGround = ParseDouble(field);
                    break;
                case 9:
                    Date = ParseDate(field, "ddMMyy");
                    break;
                case 10:
                    MagVar = ParseDouble(field);
                    break;
                case 11:
                    MagVarDirection = ParseTextAndExecuteIfMatches(field, "W", () => MagVar *= -1);
                    break;
                case 12:
                    FaaMode = ParseText(field);
                    break;
            }
        }
    }
}
