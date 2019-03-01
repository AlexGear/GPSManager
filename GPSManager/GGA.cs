using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSManager
{
    class Gga
    {
        private enum CoordinateKind { Latitude, Longitude }

        private const int GgaCommaPartsNum = 15;

        public bool IsDetermined { get; private set; }
        public readonly double Latitude;
        public readonly double Longitude;
        
        public Gga()
        {
            Latitude = 0;
            Longitude = 0;
        }

        public Gga(double Latitude, double Longitude)
        {
            this.Latitude = Latitude;
            this.Longitude = Longitude;
            IsDetermined = true;
        }

        public static Gga Parse(string s)
        {
            if(!TryParse(s, out var gga))
            {
                throw new ArgumentException($"Failed to parse GGA '{s}'");
            }
            return gga;
        }

        public static bool TryParse(string s, out Gga gga)
        {
            if (!s.StartsWith("$GPGGA") && !s.StartsWith("$GNGGA") ||
                !SplitChecksum(s, out var mainPart, out var checksumPart) ||
                !ValidateChecksum(mainPart, checksumPart) ||
                !SplitMainPart(s, out var subparts))
            {
                gga = default(Gga);
                return false;
            }

            string positionFixIndicator = subparts[6];
            if(positionFixIndicator == "0")
            {
                gga = new Gga { IsDetermined = false };
                return true;
            }

            double latitude, longitude;
            try
            {
                latitude = ParseCoordinate(s: subparts[2], letter: subparts[3], kind: CoordinateKind.Latitude);
                longitude = ParseCoordinate(s: subparts[4], letter: subparts[5], kind: CoordinateKind.Longitude);
            }
            catch
            {
                gga = default(Gga);
                return false;
            }

            gga = new Gga(latitude, longitude);
            return true;
        }

        private static bool SplitChecksum(string s, out string mainPart, out string checksumPart) {
            string[] splitted = s.Split('$', '*');
            if (splitted.Length != 3)
            {
                mainPart = default(string);
                checksumPart = default(string);
                return false;
            }
            mainPart = splitted[1];
            checksumPart = splitted[2];
            return true;
        }

        private static bool ValidateChecksum(string mainPart, string checksumPart)
        {
            int checksum;
            try
            {
                checksum = Convert.ToInt32(checksumPart, fromBase: 16);
            }
            catch
            {
                return false;
            }
            if(checksum != CalcChecksum(mainPart))
            {
                return false;
            }
            return true;

            int CalcChecksum(string str) => str.Aggregate(0, (acc, ch) => acc ^= ch);
        }

        private static bool SplitMainPart(string s, out string[] subparts)
        {
            subparts = s.Split(',');
            return subparts.Length == GgaCommaPartsNum;
        }

        private static double ParseCoordinate(string s, string letter, CoordinateKind kind)
        {
            if(kind == CoordinateKind.Latitude)
            {
                // longitude is longer by 1 char in GGA message
                s = '0' + s;
            }
            var degrees = double.Parse(s.Substring(0, 3));
            var minutes = double.Parse(s.Substring(3, s.Length - 3), CultureInfo.InvariantCulture);
            degrees += minutes / 60;

            if (letter == "S" && kind == CoordinateKind.Latitude ||
                letter == "W" && kind == CoordinateKind.Longitude)
            {
                degrees = -degrees;
            }

            return degrees;
        }
    }
}
