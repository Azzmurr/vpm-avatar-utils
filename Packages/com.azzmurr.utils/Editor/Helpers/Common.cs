using System;

namespace Azzmurr.Utils {
    public static class Common {
        public static string ToMebiByteString(long l) {
            if (l < Math.Pow(2, 10)) return l + " B";
            if (l < Math.Pow(2, 20)) return (l / Math.Pow(2, 10)).ToString("n2") + " KiB";
            if (l < Math.Pow(2, 30)) return (l / Math.Pow(2, 20)).ToString("n2") + " MiB";
            return (l / Math.Pow(2, 30)).ToString("n2") + " GiB";
        }

        public static string ToShortMebiByteString(long l) {
            if (l < Math.Pow(2, 10)) return l + " B";
            if (l < Math.Pow(2, 20)) return (l / Math.Pow(2, 10)).ToString("n0") + " KiB";
            if (l < Math.Pow(2, 30)) return (l / Math.Pow(2, 20)).ToString("n1") + " MiB";
            return (l / Math.Pow(2, 30)).ToString("n1") + " GiB";
        }
    }
}
