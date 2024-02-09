using Kbg.NppPluginNET.PluginInfrastructure;
using System.Text.RegularExpressions;

namespace JSON_Tools.JSON_Tools
{
    public class CsvSniffer
    {
        public const int DEFAULT_MAX_CHARS_TO_SNIFF = 1600;

        /// <summary>
        /// Attempt to parse text as an RFC 4180-compliant CSV file with delimiter delimiter, newline eol, and quote character '"'.<br></br>
        /// Each line, count how many columns there are. If there are two lines with different numbers of lines, or if all lines have one column, return -1.<br></br>
        /// If maxLinesToSniff lines are consumed before hitting maxCharsToSniff characters,
        /// and all the lines have the same number of columns, return that number of columns<br></br>
        /// If maxCharsToSniff characters are consumed, return -1 unless at least minLinesToDecide lines were consumed.
        /// </summary>
        /// <param name="eol">CR, CRLF, or LF</param>
        /// <param name="delimiter">the delimiter (e.g., ',' for a CSV file, '\t' for a TSV file)</param>
        /// <param name="maxLinesToSniff">consume no more than this many complete lines while sniffing</param>
        /// <param name="maxCharsToSniff">consume no more than this many characters while sniffing</param>
        /// <param name="minLinesToDecide">return -1 if fewer than this many complete lines were consumed</param>
        /// <returns>the number of columns in the file, or -1 if text does not appear to have that delimiter-eol combo</returns>
        public static int Sniff(string text, EndOfLine eol, char delimiter, char quote, int maxLinesToSniff = 16, int maxCharsToSniff = DEFAULT_MAX_CHARS_TO_SNIFF, int minLinesToDecide = 6)
        {
            maxCharsToSniff = maxCharsToSniff > text.Length ? text.Length : maxCharsToSniff;
            int nColumns = -1;
            int nColumnsThisLine = 0;
            int matchStart = 0;
            int linesConsumed = 0;
            string delimStr = ArgFunction.CsvCleanChar(delimiter);
            string newline = eol == EndOfLine.CRLF ? "\r\n" : eol == EndOfLine.CR ? "\r" : "\n";
            string escapedNewline = JNode.StrToString(newline, false);
            string newlineOrDelimiter = $"(?:{delimStr}|{escapedNewline}|\\z)";
            string regexStr = ArgFunction.CsvColumnRegex(ArgFunction.CsvCleanChar(delimiter), new string(quote, 1)) + newlineOrDelimiter;
            Regex regex = new Regex(regexStr, RegexOptions.Compiled);
            while (matchStart < maxCharsToSniff)
            {
                Match match = regex.Match(text, matchStart);
                if (!match.Success)
                    return -1;
                nColumnsThisLine++;
                int matchEnd = matchStart + match.Length;
                if (matchEnd == matchStart)
                    matchEnd++;
                bool atEndOfLine = matchEnd >= text.Length || match.Value.EndsWith(newline);
                if (atEndOfLine)
                {
                    if (nColumns == -1) // first line
                        nColumns = nColumnsThisLine;
                    else if (nColumns == 1 // a row has only one column
                        || (nColumns >= 0 && nColumnsThisLine != nColumns)) // two rows with different numbers of columns
                        return -1;
                    nColumnsThisLine = 0;
                    linesConsumed++;
                    if (linesConsumed == maxLinesToSniff)
                        return nColumns;
                }
                matchStart = matchEnd;
            }
            if (linesConsumed < 2 // only one line is never enough to decide anything
                || (matchStart < text.Length && linesConsumed < minLinesToDecide))
                    // we haven't consumed enough lines to be confident in our delimiter-eol combo
                    // (unless we consumed the whole file, in which case we're fine)
                return -1;
            return nColumns;
        }
    }
}
