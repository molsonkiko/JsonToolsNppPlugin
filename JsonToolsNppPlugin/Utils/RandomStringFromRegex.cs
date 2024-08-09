using JSON_Tools.JSON_Tools;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
// the purpose of this file is to implement a method to generate random strings from .NET regular expressions.
// This can be used to generate random properties from the "patternProperties" JSON Schema keyword, and random strings from the "pattern" keyword
// IT IS NOT YET IMPLEMENTED! I HAVEN'T FINISHED ANYTHING IN HERE.
// The plan is to support the following:
// * the \w, \d, \s, \h, and \v character classes
// * capture groups (only up to 9, no named capture groups) and noncapturing groups
// * []-enclosed character sets, which can include
//     - \XX and \X octal escapes
//     - "x-y" character ranges
//     - anything that normal .NET []-enclosed charsets can include, EXCEPT unescaped "[" characters (those will cause an error)
// * \uXXXX and \xXX escapes
// * s (dot-matches-"\n") and i (ignorecase) flags, including the (?ON-OFF) and (?ON-OFF:PATTERN) constructs
// * {m,n} curlybrace quantifiers
// * alternations with "|"
// * all the usual metacharacters
// However, lookahead and lookbehind will ABSOLUTELY NEVER BE SUPPORTED.
// This means that "^" and "$" will always be treated as start-of-string and end-of-string assertions, and multiline mode will not be supported.

namespace JSON_Tools.Utils
{

    public class RandomStringFromRegexCompilerException : ArgumentException
    {
        public enum ExceptionType
        {
            QuantifierExceedsMaxReps,
            NonAsciiChar,
            InvalidEscape,
            BadCurlybraceQuantifier,
            ExpectedHexChar,
            UnclosedParen,
            MisplacedMetacharacter,
            /// <summary>
            /// as in other parts of JsonTools, NUL characters are disallowed because they can cause issues with Notepad++
            /// </summary>
            NulCharacter,
            /// <summary>
            /// happens for 'x-y' char ranges inside a charset when x > y
            /// </summary>
            BadCharRange,
            /// <summary>
            /// happens while specifying flags in a "(?ON-OFF)" or "(?ON-OFF:...)" sequence
            /// </summary>
            InvalidFlag,
        }

        public int Pos;
        public ExceptionType Type;

        public RandomStringFromRegexCompilerException(int pos, ExceptionType exceptionType)
        {
            Pos = pos;
            Type = exceptionType;
        }
    }

    public class RandomStringFromRegexTokenizer
    {
        /// <summary>
        /// All ASCII characters except NUL in codepoint order
        /// </summary>
        public static readonly string ASCII = "\u0001\u0002\u0003\u0004\u0005\u0006\u0007\b\t\n\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f\u0020!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f";

        /// <summary>
        /// All characters in ASCII except '\n'<br></br>
        /// This is based off of .NET regex, so '.' matches '\r' even when the "s" flag is turned off. 
        /// </summary>
        public static readonly string DOT = "\u0001\u0002\u0003\u0004\u0005\u0006\u0007\b\t\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f\u0020!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f";

        public static readonly string WORD_CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

        public static readonly string DIGITS = "0123456789";

        public static readonly string HORIZONTAL_SPACE = "\t\u0020";

        public static readonly string VERTICAL_SPACE = "\n\u000b\f\r";

        public static readonly string SPACE = HORIZONTAL_SPACE + VERTICAL_SPACE;

        public static readonly Dictionary<char, string> ESCAPE_SEQUENCES = new Dictionary<char, string>
        {
            ['d'] = DIGITS,
            ['h'] = HORIZONTAL_SPACE,
            ['s'] = SPACE,
            ['v'] = VERTICAL_SPACE,
            ['w'] = WORD_CHARS,
            ['('] = "(",
            [')'] = ")",
            ['['] = "[",
            [']'] = "]",
            ['{'] = "{",
            ['}'] = "}",
            ['?'] = "?",
            ['*'] = "*",
            ['+'] = "+",
            ['-'] = "-",
            ['|'] = "|",
            ['^'] = "^",
            ['$'] = "$",
            ['\\'] = "\\",
            ['.'] = ".",
            ['&'] = "&",
            ['~'] = "~",
            ['#'] = "#",
            [' '] = " ",
            // these escapes are pre-translated by my JsonParser, so I leave them be
            ['\t'] = "\t",
            ['\n'] = "\n",
            ['\r'] = "\r",
            ['\x0b'] = "\u000b",
            ['\x0c'] = "\f",
        };

        /// <summary>
        /// quantifiers (+, *, and {n,m}) are capped at this many repetitions.<br></br>
        /// A regex that contains a {n,m} quantifier where n or m is greater than MAX_REPS will raise an error.
        /// </summary>
        public const int MAX_REPS = 20;

        public enum TokenizerState
        {
            Start,
            /// <summary>
            /// the previous character was a '\\'
            /// </summary>
            Escape,
            /// <summary>
            /// inside a \xXX or \uXXXX escape
            /// </summary>
            ExpectHexChars,
            /// <summary>
            /// inside a [] charset
            /// </summary>
            Charset,
            /// <summary>
            /// inside a {m,n} quantifier
            /// </summary>
            CurlybraceQuantifier,
            /// <summary>
            /// between the "(?" and ")" of a flag sequence
            /// </summary>
            Flags,
        }

        /// <summary>
        /// every supported flag EXCEPT IgnoreCase is turned on
        /// </summary>
        public static RegexOptions DisableIgnorecase = RegexOptions.Singleline;
        /// <summary>
        /// every supported flag EXCEPT Singleline is turned on
        /// </summary>
        public static RegexOptions DisableSingleline = RegexOptions.IgnoreCase;


        public TokenizerState State { get; private set; } = TokenizerState.Start;
        private List<TokenizerState> StateStack;
        private List<object> tokens;
        private List<RegexOptions> FlagStack;
        private RegexOptions Flags = RegexOptions.None;
        /// <summary>
        /// iff true, the regex is treated as case-insensitive
        /// </summary>
        public bool Ignorecase { get { return Flags.HasFlag(RegexOptions.IgnoreCase); } }
        /// <summary>
        /// iff true, the '.' metacharacter matches '\n'
        /// </summary>
        public bool DotAll { get { return Flags.HasFlag(RegexOptions.Singleline); } }
        private int ii;
        public string Rex { get; private set; }
        private int len { get { return Rex.Length; } }

        /// <summary>
        /// converts a regex into a list of tokens, which can be compiled into a string generator
        /// </summary>
        /// <param name="regex">the regex to be tokenized</param>
        /// <param name="ignorecase">Equivalent to the 'i' flag. If true, every letter matches both cases of that letter.</param>
        /// <param name="dotAll">Equivalent to the 's' flag. if false, '.' matches everything except '\n'. If true, '.' matches everything.</param>
        /// <returns></returns>
        public List<object> Tokenize(string regex, bool ignorecase = false, bool dotAll = false)
        {
            State = TokenizerState.Start;
            Rex = regex;
            StateStack = new List<TokenizerState>();
            FlagStack = new List<RegexOptions>();
            tokens = new List<object>();
            Flags = ignorecase ? RegexOptions.IgnoreCase : RegexOptions.None;
            if (dotAll)
                Flags |= RegexOptions.Singleline;
            bool isNegatingFlags = false;
            ii = 0;
            bool tryCharRange = false;
            int minQuantity = 0, maxQuantity = MAX_REPS;
            bool hasSetMinQuantity = false;
            int endOfHexChars = 0;
            int startOfCurrentState = 0;
            int firstTokenInCurrentState = 0;
            var unclosedParenLocs = new List<int>();
            while (ii < len)
            {
                char c = Rex[ii];
                switch (State)
                {
                case TokenizerState.Start:
                    if (c == 0)
                        throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.NulCharacter);
                    else if (c == '\\')
                    {
                        StateStack.Add(State);
                        State = TokenizerState.Escape;
                        if (ii == len)
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.InvalidEscape);
                    }
                    else if ('a' <= c && c <= 'z')
                    {
                        if (Ignorecase)
                            tokens.Add(new UnquotedString($"{c}{c - 32}"));
                        else
                            tokens.Add(c);
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        if (Ignorecase)
                            tokens.Add(new UnquotedString($"{c + 32}{c}"));
                        else
                            tokens.Add(c);
                    }
                    else if (c == '.')
                        tokens.Add(new UnquotedString(DotAll ? ASCII : DOT));
                    else if (c == '+')
                        tokens.Add(new StartEnd { start = 1, end = MAX_REPS });
                    else if (c == '*')
                        tokens.Add(new StartEnd { start = 0, end = MAX_REPS });
                    else if (c == '?')
                        tokens.Add(new StartEnd { start = 0, end = 1 });
                    else if (c == '{')
                    {
                        State = TokenizerState.CurlybraceQuantifier;
                        minQuantity = 0;
                        maxQuantity = MAX_REPS;
                        startOfCurrentState = ii;
                        hasSetMinQuantity = false;
                    }
                    else if (c == '(')
                    {
                        if (ii >= len - 1)
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.UnclosedParen);
                        StateStack.Add(State);
                        firstTokenInCurrentState = tokens.Count;
                        unclosedParenLocs.Add(firstTokenInCurrentState);
                        FlagStack.Add(Flags);
                        char nextC = regex[ii + 1];
                        if (nextC != '?')
                        {
                            ii++;
                            // not a capturing group; it could be a flag-setter, or a noncapturing group
                            if (ii >= len - 2)
                                // there must be at least a ':)', 'i)', or 's)' after the '(?',
                                // otherwise it's an unterminated flag-setter or noncapturing group
                                throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.UnclosedParen);
                            nextC = regex[ii + 1];
                            if (nextC == '-' || nextC == 'i' || nextC == 's')
                                State = TokenizerState.Flags;
                            else if (nextC == ')')
                                throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.UnclosedParen);
                            else
                            {

                            }

                        }
                        else
                        {
                            // capturing group
                        }
                    }
                    else if (c == ')')
                    {
                        // handle end of group (capture or not)?
                        if (unclosedParenLocs.Count == 0)
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.MisplacedMetacharacter);
                        int parenClosed = unclosedParenLocs.Pop();
                        Flags = FlagStack.Pop();
                    }
                    else if (c == '[')
                    {
                        StateStack.Add(State);
                        State = TokenizerState.Charset;
                        firstTokenInCurrentState = tokens.Count;
                    }
                    else if (c == ']')
                        throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.MisplacedMetacharacter);
                    else
                        tokens.Add(c);
                    break;
                case TokenizerState.CurlybraceQuantifier:
                    if (c == ',')
                    {
                        if (hasSetMinQuantity)
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.BadCurlybraceQuantifier);
                        if (ii == startOfCurrentState)
                            hasSetMinQuantity = true;
                        else
                        {
                            string quantityStr = Rex.Substring(startOfCurrentState, ii - startOfCurrentState);
                            try
                            {
                                minQuantity = int.Parse(quantityStr);
                                hasSetMinQuantity = true;
                                startOfCurrentState = ii + 1;
                            }
                            catch
                            {
                                throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.BadCurlybraceQuantifier);
                            }
                        }
                    }
                    else if (c == '}')
                    {
                        if (ii >= 1 && Rex[ii - 1] == '{')
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.BadCurlybraceQuantifier);
                        try
                        {
                            string quantityStr = Rex.Substring(startOfCurrentState, ii - startOfCurrentState);
                            if (hasSetMinQuantity)
                                maxQuantity = quantityStr.Length == 0 ? MAX_REPS : int.Parse(quantityStr);
                            else
                                minQuantity = int.Parse(quantityStr);
                        }
                        catch
                        {
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.BadCurlybraceQuantifier);
                        }
                        State = StateStack.Pop();
                        tokens.Add(new StartEnd { start = minQuantity, end = maxQuantity });
                    }
                    else if (!('0' <= c && c <= '9'))
                        // curlybrace quantifiers can only contain digits and one comma between the { and }
                        throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.BadCurlybraceQuantifier);
                    break;
                case TokenizerState.Escape:
                    if (c == 'x')
                    {
                        // handle \xXX escape
                        endOfHexChars = ii + 2;
                        startOfCurrentState = ii + 1;
                        State = TokenizerState.ExpectHexChars;
                    }
                    else if (c == 'u')
                    {
                        // handle \uXXXX escape
                        endOfHexChars = ii + 4;
                        startOfCurrentState = ii + 1;
                        State = TokenizerState.ExpectHexChars;
                    }
                    else if (ESCAPE_SEQUENCES.TryGetValue(c, out string escaped))
                    {
                        if (escaped.Length == 1)
                            tokens.Add(escaped[0]);
                        else
                            tokens.Add(new UnquotedString(escaped));
                        State = StateStack.Pop();
                    }
                    else if ('0' <= c && c <= '9')
                    {
                        TokenizerState lastState = StateStack.Last();
                        int intValue = c - '0';
                        if (lastState == TokenizerState.Start)
                            tokens.Add(new BackReference { CaptureGroup = intValue });
                        else if (lastState == TokenizerState.Charset && c <= '7')
                        {
                            // inside of a charset, \XX and \X are both treated as octal.
                            // thus '[\3&]' is equivalent to '[\x03&]' (because '&' is not an octal digit)
                            // and '[\65]' is equivalent to '[\x35]' or just '[0]'
                            if (ii >= len - 1) // unclosed char range
                                throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.BadCharRange);
                            char nextC = regex[ii + 1];
                            int octalCharVal;
                            if ('0' <= nextC && nextC <= '7') // \XX escape (we skip nextC because we handled it here)
                            {
                                octalCharVal = nextC - '0' + 8 * intValue;
                                ii++;
                            }
                            else // \X escape (we will handle nextC later)
                                octalCharVal = intValue;
                            if (octalCharVal == 0)
                                throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.NulCharacter);
                            tokens.Add((char)octalCharVal);
                        }
                        else
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.InvalidEscape);
                        State = StateStack.Pop();
                    }
                    else
                        throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.InvalidEscape);
                    break;
                case TokenizerState.ExpectHexChars:
                    if (('0' <= c && c <= '9') || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'f'))
                    {
                        if (ii == endOfHexChars)
                        {
                            int hex = int.Parse(Rex.Substring(startOfCurrentState, ii + 1 - startOfCurrentState), System.Globalization.NumberStyles.HexNumber);
                            if (hex == 0)
                                throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.NulCharacter);
                            if (hex >= 128)
                                throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.NonAsciiChar);
                            State = StateStack.Pop();
                            tokens.Add((char)hex);
                        }
                    }
                    else
                        throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.ExpectedHexChar);
                    break;
                case TokenizerState.Charset:
                    if (c == 0)
                        throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.NulCharacter);
                    if (c == '[') // Some regex flavors allow unescaped '[' inside charsets, but .NET does not.
                                  // Rather than silently allowing the regex to descend into madness because of nested [] in charsets, I will just throw.
                        throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.MisplacedMetacharacter);
                    else if (c == '\\')
                    {
                        StateStack.Add(State);
                        State = TokenizerState.Escape;
                        if (ii == len)
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.InvalidEscape);
                    }
                    else if (c == ']')
                    {
                        // union together all the chars in the charset
                        var chars = new HashSet<char>();
                        for (int jj = firstTokenInCurrentState; jj < tokens.Count; jj++)
                        {
                            object tok = tokens[jj];
                            if (tok is char d)
                                chars.Add(d);
                            else if (tok is UnquotedString uqs)
                                chars.UnionWith(uqs.value);
                            else if (tok is string s)
                                chars.UnionWith(s);
                        }
                        string charStr = new string(chars.ToArray());
                        tokens.Add(new UnquotedString(charStr));
                        State = StateStack.Pop();
                    }
                    else if (tryCharRange)
                    {
                        // handle x-y character ranges
                        //     these only make sense if both x and y are single characters
                        //     so we treat this as a literal '-' character if:
                        //     * this is the first or last character of the charset (e.g., "--0", "\x20--")
                        //     * x or y is a charset (e.g., "b-\d", "a-z-q")
                        //     and we raise an error if x > y (e.g., "z-a", "\x20-\x01")
                        tryCharRange = false;
                        object lastTok = tokens.Last();
                        if (lastTok is char lastC)
                        {
                            int rangeLength = c - lastC + 1;
                            if (rangeLength < 1)
                                throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.BadCharRange);
                            tokens.RemoveAt(tokens.Count - 1);
                            char[] charRange = new char[rangeLength];
                            for (int jj = 0; jj < rangeLength; jj++)
                                charRange[jj] = (char)(lastC + jj);
                            tokens.Add(new string(charRange));
                        }
                        else
                        {
                            // '-' not flanked by two single characters, so we just add the '-' and the char
                            tokens.Add('-');
                            tokens.Add(c);
                        }
                    }
                    else if (c == '-' && firstTokenInCurrentState < tokens.Count)
                        tryCharRange = true; // the '-' of an "x-y" char range
                                             // (need to make sure it's not the first char of the charset)  
                    else // normal character, not part of a char range
                        tokens.Add(c);
                    startOfCurrentState = ii;
                    break;
                case TokenizerState.Flags:
                    switch (c)
                    {
                    case '-':
                        if (isNegatingFlags)
                            throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.InvalidFlag);
                        isNegatingFlags = true;
                        break;
                    case 's':
                        if (isNegatingFlags)
                            Flags &= DisableSingleline;
                        else
                            Flags |= RegexOptions.Singleline;
                        break;
                    case 'i':
                        if (isNegatingFlags)
                            Flags &= DisableIgnorecase;
                        else
                            Flags |= RegexOptions.IgnoreCase;
                        break;
                    case ')':
                        // a "(?ON-OFF)" construct. Whatever flag values we set here persist until reset, or until the close of the enclosing group
                        State = StateStack.Pop(); // this should be Start
                        break;
                    case ':':
                        // a "(?ON-OFF:...)" construct.
                        // We would set the flags as if we saw a ")" here,
                        //    but when the ")" shows up, we revert the flags to how they were before this.
                        State = StateStack.Pop();
                        break;
                    default:
                        throw new RandomStringFromRegexCompilerException(ii, RandomStringFromRegexCompilerException.ExceptionType.InvalidFlag);
                    }
                    break;
                }
                ii++;
            }
            return tokens;
        }
    }

    /// <summary>
    /// in RemesPathLexers, represents an unquoted string.<br></br>
    /// in RandomStringFromRegex, represents a set of characters (e.g., [\w\x20a-f])
    /// </summary>
    public struct UnquotedString
    {
        public string value;

        public UnquotedString(string value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return JNode.StrToString(value, true);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StartEnd
    {
        public int start;
        public int end;
    }

    public struct BackReference
    {
        public int CaptureGroup;
    }

    /// <summary>
    /// represents a capture group (in which case Options.Count = 1) or an set of '|'-delimited options
    /// </summary>
    public class Alternation
    {
        /// <summary>
        /// the range of tokens for each alternative
        /// </summary>
        public List<StartEnd> Options { get; private set; } = new List<StartEnd>();
        /// <summary>
        /// if this is -1, this is non-capturing
        /// </summary>
        public int CaptureNumber = -1;
    }
}