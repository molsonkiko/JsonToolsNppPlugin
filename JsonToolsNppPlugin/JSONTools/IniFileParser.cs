using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace JSON_Tools.JSON_Tools
{
    public class IniFileParser
    {
        /// <summary>
        /// if true, ${SECTION:KEY} is replaced with the value associated with key KEY in section SECTION<br></br>
        /// ${KEY} is replaced with the value associated with key KEY <i>if KEY is in the same section</i>
        /// </summary>
        public bool UseInterpolation;
        public int utf8ExtraBytes { get; private set; }

        public IniFileParser(bool useInterpolation)
        {
            UseInterpolation = useInterpolation;
        }

        public static readonly Regex INI_TOKENIZER = new Regex(
            @"^\s*\[([^\r\n]+)\]\s*$|" + // section name
            @"^\s*([^\r\n:=]+)\s*[:=](.+)$|" + // key and value within section
            @"^\s*[;#](.+)$|" + // comment
            @"^\s*(?![;#])(\S+)$" // multi-line continuation of value
        );

        public JNode Parse(string text)
        {
            
        }
    }
}
