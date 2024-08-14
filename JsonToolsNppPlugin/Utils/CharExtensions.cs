using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace JSON_Tools.Utils
{
    public static class CharExtensions
    {
        public static bool IsDigit(this char c) => c >= '0' && c <= '9';

        public static bool IsLowerCase(this char c) => c >= 'a' && c <= 'z';

        public static bool IsUpperCase(this char c) => c >= 'A' && c <= 'Z';

        public static bool IsAsciiLetter(this char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        public static bool IsHexadecimal(this char c) => ('0' <= c && c <= '9') || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'f');
    }
}
