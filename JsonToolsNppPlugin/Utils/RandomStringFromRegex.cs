using JSON_Tools.JSON_Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

// The purpose of this file is to implement a method to generate random strings from .NET regular expressions.
// This can be used to generate random properties from the "patternProperties" JSON Schema keyword, and random strings from the "pattern" keyword.
// Currently the following should be supported:
// * all ASCII characters except \x00 (no non-ASCII characters)
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
// However, all of the following will ABSOLUTELY NEVER BE SUPPORTED (because they require backtracking, and random strings must be generated in a single pass):
// * "^" and "$" as start-of-line and end-of-line anchors
// * multiline mode (which normally is activated by the m flag)
// * (?=PATTERN) for lookahead and (?<=PATTERN) for lookbehind
// * any pattern that could result in an uncaptured capture group being referenced.
//     For example, "(?:(foo)|(bar))\1" is a valid regular expression with no explicit backtracking,
//     but it could result in the "\1" backreference being visited when the 2nd capture group ("bar") was captured,
//     and thus backtracking is sometimes required to match a string with that regex.
// * greedy and lazy quantifiers
// * anything else I haven't already thought of that requires backtracking

namespace JSON_Tools.Utils
{
    /// <summary>
    /// thrown while compiling a regular expression into a function that generates random strings,
    /// or while generating strings.
    /// </summary>
    public class RandomStringFromRegexException : ArgumentException
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
            /// <summary>
            /// happens when there's a backreference to a capture group that hasn't been captured yet
            /// </summary>
            BadBackReference,
            EmptyCharSet,
            TwoConsecutiveQuantifiers,
            /// <summary>
            /// happens when there is a quantifier that does not reference a pattern.<br></br>
            /// For example would be thrown by the patterns <c>+</c> or <c>bar|?</c> or <c>(foo|{2,3})</c>
            /// </summary>
            NothingToQuantify,
            /// <summary>
            /// occurs when, while generating a string from a regex,
            /// we encounter a backreference to a capture group that wasn't captured.<br></br>
            /// For example, this will happen with regex <c>(?:(foo)|(bar))\1\2</c> because it is impossible for capture groups 1 and 2 to both be captured.
            /// </summary>
            BackreferenceToUncapturedGroup,
            /// <summary>
            /// In RandomStringFromRegex, the <c>^</c> metacharacter <i>always</i> represents the start of the string.<br></br>
            /// Thus, any regex that would result in a character before <c>^</c> raises an error of this type
            /// </summary>
            CaretAfterStartOfString,
            /// <summary>
            /// In RandomStringFromRegex, the <c>$</c> metacharacter <i>always</i> represents the end of the string.<br></br>
            /// Thus, any regex that would result in a character after <c>$</c> raises an error of this type.
            /// </summary>
            DollarBeforeEndOfString,
            /// <summary>
            /// any error with an unknown cause
            /// </summary>
            UnknownError,
        }

        public string Regex;
        public int Pos;
        public ExceptionType ExType;

        public RandomStringFromRegexException(string regex, int pos, ExceptionType exceptionType)
        {
            Regex = regex;
            Pos = pos;
            ExType = exceptionType;
        }

        public override string ToString()
        {
            string exDescr;
            if (!ExceptionTypeDescriptions.TryGetValue(ExType, out exDescr))
                exDescr = ExType.ToString();
            return RemesLexerException.MarkLocationOfSyntaxError(Regex, Pos, exDescr);
        }

        private const string RSFR_NAME = "JsonTools RandomStringFromRegex";

        private static readonly Dictionary<ExceptionType, string> ExceptionTypeDescriptions = new Dictionary<ExceptionType, string>
        {
            [ExceptionType.BackreferenceToUncapturedGroup] = "Backreference to uncaptured capture group",
            [ExceptionType.BadBackReference] = "Backreference to capture group with too high a group number",
            [ExceptionType.BadCharRange] = "[x-y] character range where y had a greater ASCII code than x",
            [ExceptionType.BadCurlybraceQuantifier] = "Error parsing a {x,y} quantifier",
            [ExceptionType.EmptyCharSet] = "[] character set with no characters inside",
            [ExceptionType.ExpectedHexChar] = "Expected hexadecimal character (0-9, a-f, or A-F)",
            [ExceptionType.InvalidEscape] = "Invalid character following \\",
            [ExceptionType.InvalidFlag] = $"In {RSFR_NAME}, only the s (DotAll) and i (IgnoreCase) flags are used",
            [ExceptionType.MisplacedMetacharacter] = "Regex special character in an inappropriate location",
            [ExceptionType.NonAsciiChar] = $"Only ASCII characters are allowed in {RSFR_NAME}",
            [ExceptionType.NothingToQuantify] = "{x,y}, *, or + quantifier not preceded by a pattern",
            [ExceptionType.NulCharacter] = $"The NUL character \\x00 is not allowed in {RSFR_NAME}",
            [ExceptionType.QuantifierExceedsMaxReps] = $"{RandomStringFromRegex.MAX_REPS} is the maximum number of repetitions for any quantifier in {RSFR_NAME}",
            [ExceptionType.TwoConsecutiveQuantifiers] = "Two consecutive quantifiers (for example, \"x+*\" or \"y{1,2}+\")",
            [ExceptionType.UnclosedParen] = "Unclosed ) character",
            [ExceptionType.UnknownError] = "Unknown error (likely due to a programming mistake)",
        };
    }

    public class RandomStringFromRegex
    {
        #region public interface
        private static LruCache<string, Func<string>> regexGeneratorCache = new LruCache<string, Func<string>>(32);

        /// <summary>
        /// Parses a regular expression and returns a function that generates a random string matching that regex.
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="ignorecase"></param>
        /// <param name="dotAll"></param>
        /// <returns></returns>
        public static Func<string> GetGenerator(string regex, bool ignorecase = false, bool dotAll = false)
        {
            if (ignorecase || dotAll)
            {
                var flags = "(?" + (ignorecase ? (dotAll ? "si)" : "i)") : "s)");
                regex = flags + regex;
            }
            if (regexGeneratorCache.TryGetValue(regex, out Func<string> generator))
                return generator;
            var rsfr = new RandomStringFromRegex(regex);
            rsfr.CompileGenerator();
            if (rsfr.fillStringBuilder is null)
                throw new Exception("Function generated by RandomStringFromRegex.CompileGenerator was null");
            Func<string> newGenerator = rsfr.Generate;
            regexGeneratorCache[regex] = newGenerator;
            return newGenerator;
        }

        public static void ClearCache() => regexGeneratorCache.Clear();

        /// <summary>
        /// returns the upper-case and lower-case forms of an ASCII letter c (With c first)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string GetUpperAndLower(char c)
        {
            char other = (char)(c < 'a' ? c + 32 : c - 32);
            return new string(new char[] { c, other });
        }

        /// <summary>
        /// Returns a copy of s, except that every lowercase ASCII letter has a 50% chance of being replaced with uppercase, and vice versa.<br></br>
        /// Thus <c>ScrambleCase("foo2")</c> could return <c>"foo2", "fOo2", "FoO2"</c>, etc.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ScrambleCase(string s)
        {
            if (!s.Any(c => c.IsAsciiLetter()))
                return s;
            char[] sarr = s.ToCharArray();
            bool changed = false;
            for (int ii = 0; ii < sarr.Length; ii++)
            {
                char c = sarr[ii];
                if (c.IsAsciiLetter())
                {
                    bool randBool = random.Next(2) == 1;
                    if (randBool)
                    {
                        sarr[ii] = (char)(c < 'a' ? c + 32 : c - 32);
                        changed = true;
                    }
                }
            }
            return changed ? new string(sarr) : s;
        }

        private const int NUM_TRIALS_IsRSFR_Compatible = 4;

        /// <summary>
        /// Attempt to compile pattern into a random string generator, then try generating <see cref="NUM_TRIALS_IsRSFR_Compatible"/> random strings.<br></br>
        /// If this test is successful, return true and set errMsg to null.<br></br>
        /// If this test raises an exception ex, return false and set errMsg to ex.ToString(). 
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool IsRSFR_Compatible(string pattern, out string errMsg, bool ignorecase = false, bool dotAll = false)
        {
            try
            {
                Func<string> generator = GetGenerator(pattern, ignorecase, dotAll);
                for (int ii = 0; ii < NUM_TRIALS_IsRSFR_Compatible; ii++)
                    generator();
                errMsg = null;
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// Assuming <c>schema</c> is a JSON schema, recursively search <c>schema</c> for <c>object</c> subschemas with <c>patternProperties</c> subschemas<br></br>
        /// and <c>string</c> subschemas with the <c>pattern</c> attribute<br></br>
        /// and verify that each <c>pattern</c> or <c>patternProperties</c> key is a regular expression that can be parsed by <see cref="RandomStringFromRegex"/>.<br></br>
        /// If <c>schema</c> is not an object, return <c>true</c> and <c>regexErrors</c> will be null.<br></br>
        /// If every such pattern passes the <see cref="IsRSFR_Compatible(string, out string, bool, bool)"/> test, this function will return <c>true</c> and <c>regexErrors</c> will be an empty list.<br></br>
        /// If not, this function will return <c>false</c> and <c>regexErrors</c> will be a list of all the regexes that failed this test, and the error that occurred while testing.<br></br>
        /// <b>NOTE:</b> This function does not actually test if <c>schema</c> is a valid JSON schema, so it could in principle give false positives unless <c>schema</c> is determined to be a valid schema by some other means.
        /// </summary>
        public static bool AllPatternsAreRSFR_Compatible(JNode schema, out List<(string regex, string errMsg)> regexErrors)
        {
            regexErrors = null;
            if (!(schema is JObject))
                return true;
            regexErrors = new List<(string regex, string errMsg)>();
            AllPatternsAreRSFR_CompatibleHelper(schema, regexErrors);
            return regexErrors.Count == 0;
        }

        private static void AllPatternsAreRSFR_CompatibleHelper(JNode schema, List<(string regex, string errMsg)> regexErrors)
        {
            if (schema is JObject obj)
            {
                if (obj.TryGetValue("type", out JNode typeNode) && typeNode.value is string typeStr)
                {
                    if (typeStr == "string")
                    {
                        if (obj.TryGetValue("pattern", out JNode patternNode)
                            && patternNode.value is string pattern
                            && !IsRSFR_Compatible(pattern, out string errMsg))
                            regexErrors.Add((pattern, errMsg));
                    }
                    else if (typeStr == "object" || typeStr == "array")
                    {
                        if (typeStr == "object"
                            && obj.TryGetValue("patternProperties", out JNode patternPropsNode)
                            && patternPropsNode is JObject patternPropsObj)
                        {
                            foreach (string patternProp in patternPropsObj.children.Keys)
                            {
                                if (!IsRSFR_Compatible(patternProp, out string errMsg))
                                    regexErrors.Add((patternProp, errMsg));
                            }
                        }
                    }
                }
                foreach (JNode child in obj.children.Values)
                    AllPatternsAreRSFR_CompatibleHelper(child, regexErrors);
            }
            else if (schema is JArray arr)
            {
                foreach (JNode child in arr.children)
                    AllPatternsAreRSFR_CompatibleHelper(child, regexErrors);
            }
        }
        #endregion // public interface
        #region tokenizer stuff
        private static readonly Random random = RandomJsonFromSchema.random;

        /// <summary>
        /// All ASCII characters except NUL in codepoint order
        /// </summary>
        public static readonly string ASCII = "\u0001\u0002\u0003\u0004\u0005\u0006\u0007\b\t\n\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f\u0020!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f";

        private static readonly HashSet<char> ASCII_SET = new HashSet<char>(ASCII);

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
            ['t'] = "\t",
            ['n'] = "\n",
            ['r'] = "\r",
            ['f'] = "\f",
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
        private static RegexOptions DisableIgnorecase = RegexOptions.Singleline;
        /// <summary>
        /// every supported flag EXCEPT Singleline is turned on
        /// </summary>
        private static RegexOptions DisableSingleline = RegexOptions.IgnoreCase;

        public TokenizerState State { get; private set; } = TokenizerState.Start;
        private List<TokenizerState> StateStack;
        private List<object> tokens;
        private List<RegexOptions> FlagStack;
        private List<Alternation> AlternationStack;
        private int maxCaptureNumber = 0;
        private RegexOptions Flags = RegexOptions.None;
        private Alternation LastClosedAlternation;
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

        private delegate void FillStringBuilder(StringBuilder sb, string[] capturedStrings);

        private FillStringBuilder fillStringBuilder;

        /// <summary>
        /// converts a regex into a list of tokens, which can then be used to generate random strings using the <see cref="Generate"/> method.
        /// </summary>
        private RandomStringFromRegex(string regex)
        {
            State = TokenizerState.Start;
            Rex = regex;
            StateStack = new List<TokenizerState>();
            FlagStack = new List<RegexOptions>();
            var baseAlternation = new Alternation(-1, 1);
            bool charsetIsNegated = false;
            int maxClosedCaptureNumber = 0;
            AlternationStack = new List<Alternation> { baseAlternation };
            LastClosedAlternation = null;
            tokens = new List<object> { baseAlternation };
            bool isNegatingFlags = false;
            ii = 0;
            int minQuantity = 0, maxQuantity = MAX_REPS;
            bool hasSetMinQuantity = false;
            int endOfHexChars = 0;
            int startOfCurrentState = 0;
            int firstTokenInCurrentState = 0;
            int indexOfDollarMetachar = -1;
            var unclosedParenStack = new List<int>();
            while (ii < len)
            {
                char c = Rex[ii];
                switch (State)
                {
                case TokenizerState.Start:
                    if (c == 0)
                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.NulCharacter);
                    else if (c == '\\')
                    {
                        StateStack.Add(State);
                        State = TokenizerState.Escape;
                        if (ii == len)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.InvalidEscape);
                    }
                    else if (c.IsAsciiLetter())
                    {
                        if (Ignorecase)
                            tokens.Add(GetUpperAndLower(c));
                        else
                            tokens.Add(c);
                    }
                    else if (c == '.')
                        tokens.Add(DotAll ? ASCII : DOT);
                    else if (c == '+')
                        AddQuantifier(new StartEnd { start = 1, end = MAX_REPS });
                    else if (c == '*')
                        AddQuantifier(new StartEnd { start = 0, end = MAX_REPS });
                    else if (c == '?')
                        AddQuantifier(new StartEnd { start = 0, end = 1 });
                    else if (c == '{')
                    {
                        StateStack.Add(State);
                        State = TokenizerState.CurlybraceQuantifier;
                        minQuantity = 0;
                        maxQuantity = MAX_REPS;
                        startOfCurrentState = ii;
                        hasSetMinQuantity = false;
                    }
                    else if (c == '(')
                    {
                        if (ii >= len - 1)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.UnclosedParen);
                        StateStack.Add(State);
                        firstTokenInCurrentState = tokens.Count;
                        unclosedParenStack.Add(firstTokenInCurrentState);
                        FlagStack.Add(Flags);
                        char nextC = regex[ii + 1];
                        if (nextC == '?')
                        {
                            ii++;
                            // not a capturing group; it could be a flag-setter, or a noncapturing group
                            if (ii >= len - 2)
                                // there must be at least a ':)', 'i)', or 's)' after the '(?',
                                // otherwise it's an unterminated flag-setter or noncapturing group
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.UnclosedParen);
                            nextC = regex[ii + 1];
                            if (nextC == '-' || nextC == 'i' || nextC == 's')
                                State = TokenizerState.Flags;
                            else if (nextC == ')')
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.UnclosedParen);
                            else if (nextC == ':')
                            {
                                // noncapturing group
                                ii++;
                                AddAlternationToStack(false);
                            }
                            else
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.InvalidFlag);

                        }
                        else
                        {
                            // capturing group
                            AddAlternationToStack(true);
                        }
                    }
                    else if (c == ')')
                    {
                        if (unclosedParenStack.Count == 0)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.MisplacedMetacharacter);
                        unclosedParenStack.Pop();
                        Alternation closedAlt = CloseTopAlternation();
                        if (closedAlt.CaptureNumber >= 1)
                            maxClosedCaptureNumber = closedAlt.CaptureNumber;
                        Flags = FlagStack.Pop();
                        State = StateStack.Pop();
                    }
                    else if (c == '[')
                    {
                        StateStack.Add(State);
                        State = TokenizerState.Charset;
                        if (ii < len - 1 && Rex[ii + 1] == '^')
                        {
                            charsetIsNegated = true;
                            ii++;
                        }
                        firstTokenInCurrentState = tokens.Count;
                    }
                    else if (c == ']')
                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.MisplacedMetacharacter);
                    else if (c == '|')
                        AddOptionToTopAlternation();
                    else if (c == '^')
                        tokens.Add(new Anchor { StartOfString = true, PositionInRegex = ii });
                    else if (c == '$')
                    {
                        indexOfDollarMetachar = tokens.Count;
                        tokens.Add(new Anchor { StartOfString = false });
                    }
                    else
                        tokens.Add(c);
                    break; // TokenizerState.Start
                case TokenizerState.CurlybraceQuantifier:
                    if (c == ',')
                    {
                        if (hasSetMinQuantity)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier);
                        if (ii == startOfCurrentState + 1)
                        {
                            // x{,y} is just between 0 and y repetitions of x
                            startOfCurrentState = ii;
                            hasSetMinQuantity = true;
                        }
                        else
                        {
                            string quantityStr = Rex.Substring(startOfCurrentState + 1, ii - startOfCurrentState - 1);
                            try
                            {
                                minQuantity = int.Parse(quantityStr);
                                hasSetMinQuantity = true;
                                startOfCurrentState = ii;
                            }
                            catch
                            {
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier);
                            }
                        }
                    }
                    else if (c == '}')
                    {
                        if (ii >= 1 && Rex[ii - 1] == '{')
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier);
                        try
                        {
                            string quantityStr = Rex.Substring(startOfCurrentState + 1, ii - startOfCurrentState - 1);
                            if (hasSetMinQuantity)
                                maxQuantity = quantityStr.Length == 0 ? MAX_REPS : int.Parse(quantityStr);
                            else
                                minQuantity = maxQuantity = int.Parse(quantityStr);
                        }
                        catch
                        {
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier);
                        }
                        State = StateStack.Pop();
                        if (maxQuantity < minQuantity)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier);
                        if (maxQuantity > MAX_REPS || minQuantity > MAX_REPS)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.QuantifierExceedsMaxReps);
                        if (minQuantity != 1 || maxQuantity != 1)
                            AddQuantifier(new StartEnd { start = minQuantity, end = maxQuantity });
                    }
                    else if (!('0' <= c && c <= '9'))
                        // curlybrace quantifiers can only contain digits and one comma between the { and }
                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier);
                    break; // TokenizerState.CurlybraceQuantifier
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
                            tokens.Add(escaped);
                        State = StateStack.Pop();
                    }
                    else if ('0' <= c && c <= '9')
                    {
                        State = StateStack.Pop();
                        int intValue = c - '0';
                        if (State == TokenizerState.Start)
                        {
                            if (intValue > maxClosedCaptureNumber)
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadBackReference);
                            tokens.Add(new BackReference { CaptureGroup = intValue, IgnoreCase = Ignorecase, PositionInRegex = ii });
                        }
                        else if (State == TokenizerState.Charset && c <= '7')
                        {
                            // inside of a charset, \XX and \X are both treated as octal.
                            // thus '[\3&\18]' is equivalent to '[\x03&\x018]' (because '&' and '8' are not octal digits)
                            // and '[\65]' is equivalent to '[\x35]' or just '[0]'
                            if (ii >= len - 1) // unclosed char range
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadCharRange);
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
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.NulCharacter);
                            tokens.Add((char)octalCharVal);
                        }
                        else
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.InvalidEscape);
                    }
                    else
                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.InvalidEscape);
                    break; // TokenizerState.Escape
                case TokenizerState.ExpectHexChars:
                    if (c.IsHexadecimal())
                    {
                        if (ii == endOfHexChars)
                        {
                            int hex = int.Parse(Rex.Substring(startOfCurrentState, ii + 1 - startOfCurrentState), System.Globalization.NumberStyles.HexNumber);
                            if (hex == 0)
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.NulCharacter);
                            if (hex >= 128)
                                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.NonAsciiChar);
                            State = StateStack.Pop();
                            char chex = (char)hex;
                            if (State == TokenizerState.Start && Ignorecase && chex.IsAsciiLetter())
                                tokens.Add(GetUpperAndLower(chex));
                            else
                                tokens.Add(chex);
                        }
                    }
                    else
                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.ExpectedHexChar);
                    break; // TokenizerState.ExpectHexChars
                case TokenizerState.Charset:
                    if (c == 0)
                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.NulCharacter);
                    if (c == '[') // Some regex flavors allow unescaped '[' inside charsets, but .NET does not.
                                  // Rather than silently allowing the regex to descend into madness because of nested [] in charsets, I will just throw.
                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.MisplacedMetacharacter);
                    else if (c == '\\')
                    {
                        StateStack.Add(State);
                        State = TokenizerState.Escape;
                        if (ii == len)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.InvalidEscape);
                    }
                    else if (c == ']')
                    {
                        // union together all the chars in the charset
                        var chars = new HashSet<char>();
                        bool tryCharRange = false;
                        bool precededByCharRange = false;
                        for (int jj = firstTokenInCurrentState; jj < tokens.Count; jj++)
                        {
                            object tok = tokens[jj];
                            if (tok is char d)
                            {
                                if (tryCharRange)
                                {
                                    char firstCharInRange = (char)tokens[jj - 2];
                                    if (firstCharInRange > d)
                                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.BadCharRange);
                                    for (char kk = (char)(firstCharInRange + 1); kk <= d; kk = (char)(kk + 1))
                                        chars.Add(kk);
                                    tryCharRange = false;
                                    precededByCharRange = true;
                                }
                                else if (d == '-' && jj > firstTokenInCurrentState && jj < tokens.Count - 1 && !precededByCharRange)
                                    tryCharRange = true;
                                else
                                {
                                    precededByCharRange = false;
                                    chars.Add(d);
                                }

                            }
                            else if (tok is string s)
                            {
                                if (tryCharRange)
                                    chars.Add('-');
                                tryCharRange = false;
                                precededByCharRange = true;
                                chars.UnionWith(s);
                            }
                        }
                        char[] charArr = chars.ToArray();
                        if (Ignorecase)
                        {
                            foreach (char d in charArr)
                            {
                                if (d.IsLowerCase())
                                    chars.Add((char)(d - 32));
                                else if (d.IsUpperCase())
                                    chars.Add((char)(d + 32));
                            }
                            charArr = chars.ToArray();
                        }
                        string charStr = new string(charsetIsNegated ? ASCII_SET.Except(charArr).ToArray() : charArr);
                        if (charStr.Length == 0)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.EmptyCharSet);
                        tokens.RemoveRange(firstTokenInCurrentState, tokens.Count - firstTokenInCurrentState);
                        tokens.Add(charStr);
                        charsetIsNegated = false;
                        State = StateStack.Pop();
                    }
                    else
                        tokens.Add(c);
                    startOfCurrentState = ii;
                    break; // TokenizerState.Charset
                case TokenizerState.Flags:
                    switch (c)
                    {
                    case '-':
                        if (isNegatingFlags)
                            throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.InvalidFlag);
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
                        FlagStack.Pop();
                        unclosedParenStack.Pop();
                        break;
                    case ':':
                        // a "(?ON-OFF:...)" construct to start a noncapturing group.
                        // We would set the flags as if we saw a ")" here,
                        //    but when the ")" shows up, we revert the flags to how they were before this.
                        State = TokenizerState.Start;
                        AddAlternationToStack(false);
                        break;
                    default:
                        throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.InvalidFlag);
                    }
                    break; // TokenizerState.Flags
                }
                ii++;
            }
            CloseTopAlternation();
            if (indexOfDollarMetachar >= 0 && indexOfDollarMetachar < tokens.Count - 1)
                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.DollarBeforeEndOfString);
            if (unclosedParenStack.Count > 0)
                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.UnclosedParen);
            if (State != TokenizerState.Start || StateStack.Count > 0 || FlagStack.Count > 0)
                throw new RandomStringFromRegexException(regex, ii, RandomStringFromRegexException.ExceptionType.UnknownError);
        }

        private void AddQuantifier(StartEnd se)
        {
            // can't allow a quantifier that doesn't reference a pattern
            if (AlternationStack.Last().LastOptionIsEmpty(tokens.Count))
                throw new RandomStringFromRegexException(Rex, ii, RandomStringFromRegexException.ExceptionType.NothingToQuantify);
            // can't allow two consecutive quantifiers
            if (tokens.Last() is StartEnd && (LastClosedAlternation == null || LastClosedAlternation.End < tokens.Count))
                throw new RandomStringFromRegexException(Rex, ii, RandomStringFromRegexException.ExceptionType.TwoConsecutiveQuantifiers);
            tokens.Add(se);
        }

        private Alternation CloseTopAlternation()
        {
            var topAlternation = AlternationStack.Pop();
            AddEmptyStringToEmptyOption(topAlternation);
            topAlternation.Close(tokens.Count);
            LastClosedAlternation = topAlternation;
            return topAlternation;
        }

        private void AddOptionToTopAlternation()
        {
            var topAlternation = AlternationStack.Last();
            AddEmptyStringToEmptyOption(topAlternation);
            topAlternation.AddOption(tokens.Count);
        }

        private void AddEmptyStringToEmptyOption(Alternation topAlternation)
        {
            if (topAlternation.LastOptionIsEmpty(tokens.Count))
                tokens.Add("");
        }

        private void AddAlternationToStack(bool isCapturing)
        {
            var newAlternation = new Alternation(isCapturing ? ++maxCaptureNumber : -1, tokens.Count + 1);
            tokens.Add(newAlternation);
            AlternationStack.Add(newAlternation);
        }
        #endregion // tokenizer stuff

        #region compiling stuff
        /// <summary>
        /// bind this.fillStringBuilder to a function that generates random strings from the tokenized regex.
        /// </summary>
        /// <returns></returns>
        private void CompileGenerator()
        {
            if (tokens.Count < 2)
                throw new Exception("RandomStringFromRegexTokenizer should have at least two tokens");
            var rootAlt = (Alternation)tokens[0];
            fillStringBuilder = GenerateForAlternation(rootAlt);
        }

        private FillStringBuilder GenerateForAlternation(Alternation alt)
        {
            var fillers = new FillStringBuilder[alt.Options.Count];
            for (int ii = 0; ii < fillers.Length; ii++)
            {
                StartEnd option = alt.Options[ii];
                fillers[ii] = GenerateForOption(option.start, option.end);
            }
            return new FillStringBuilder((StringBuilder sb, string[] capturedStrings) =>
            {
                FillStringBuilder chosenFiller = fillers[random.Next(fillers.Length)];
                int lengthBeforeFill = sb.Length;
                chosenFiller(sb, capturedStrings);
                if (alt.CaptureNumber > 0)
                {
                    string res = sb.ToString(lengthBeforeFill, sb.Length - lengthBeforeFill);
                    capturedStrings[alt.CaptureNumber - 1] = res;
                }
            });
        }

        private FillStringBuilder GenerateForOption(int start, int end)
        {
            int ii = start;
            var fillers = new List<FillStringBuilder>();
            end = end > tokens.Count ? tokens.Count : end;
            while (ii < end)
            {
                FillStringBuilder filler;
                object tok = tokens[ii++];
                if (tok is char c)
                    filler = new FillStringBuilder((sb, _) => sb.Append(c));
                else if (tok is Alternation alt)
                {
                    filler = GenerateForAlternation(alt);
                    ii = alt.End;
                }
                else if (tok is BackReference b)
                    filler = new FillStringBuilder((sb, captures) =>
                    {
                        string backref = captures[b.CaptureGroup - 1];
                        if (backref is null)
                            throw new RandomStringFromRegexException(Rex, b.PositionInRegex, RandomStringFromRegexException.ExceptionType.BackreferenceToUncapturedGroup);
                        sb.Append(b.IgnoreCase ? ScrambleCase(backref) : backref);
                    });
                else if (tok is string s)
                    filler = new FillStringBuilder((sb, _) =>
                    {
                        if (s.Length > 0)
                            sb.Append(s[random.Next(s.Length)]);
                    });
                else if (tok is Anchor a)
                {
                    if (a.StartOfString) // ^ anchor, need to make sure it's at the start of the string
                    {
                        filler = new FillStringBuilder((sb, __) =>
                        {
                            if (sb.Length > 0)
                                throw new RandomStringFromRegexException(Rex, a.PositionInRegex, RandomStringFromRegexException.ExceptionType.CaretAfterStartOfString);
                        });
                    }
                    else
                        // Any $ anchor that has any token(s) after it already raised an error at compile time.
                        // Technically this excludes some regexes that can match text
                        //     (e.g., "a$(b|)" and "a$())" both match "a")
                        //     but we can't determine these things at compile time (too many corner cases)
                        //     and we also can't do it at runtime without lookahead (impossible)
                        //     or adding a new piece of persistent state to be tracked by all fillers (too computationally expensive)
                        continue;
                }
                else
                    throw new RandomStringFromRegexException(Rex, ii - 1, RandomStringFromRegexException.ExceptionType.UnknownError);
                if (ii < end && tokens[ii] is StartEnd repsRange)
                {
                    fillers.Add(new FillStringBuilder((sb, captures) =>
                    {
                        int numReps = random.Next(repsRange.start, repsRange.end + 1);
                        for (int jj = 0; jj < numReps; jj++)
                            filler(sb, captures);
                    }));
                    ii++;
                }
                else
                    fillers.Add(filler);
            }
            return new FillStringBuilder((sb, captures) =>
            {
                for (ii = 0; ii < fillers.Count; ii++)
                    fillers[ii](sb, captures);
            });
        }

        private string Generate()
        {
            var sb = new StringBuilder();
            var capturedStrings = new string[maxCaptureNumber];
            fillStringBuilder?.Invoke(sb, capturedStrings);
            return sb.ToString();
        }
        #endregion // compiling stuff
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
        /// <summary>
        /// if this is true, this matches <see cref="RandomStringFromRegex.ScrambleCase(string)"/> on the referenced capture group
        /// </summary>
        public bool IgnoreCase;
        public int PositionInRegex;
    }

    /// <summary>
    /// If <c>StartOfString == true</c>, represents the <c>^</c> metacharacter in regex<br></br>
    /// If <c>StartOfString == false</c>, represents the <c>$</c> metacharacter in regex<br></br>
    /// </summary>
    public struct Anchor
    {
        public bool StartOfString;
        public int PositionInRegex;
    }

    /// <summary>
    /// represents a capture group (in which case Options.Count = 1) or an set of '|'-delimited options
    /// </summary>
    public class Alternation
    {
        /// <summary>
        /// the range of tokens for each alternative. The end of a range is excluded, but the start is included.
        /// </summary>
        public List<StartEnd> Options { get; private set; }
        /// <summary>
        /// if this is -1, this is non-capturing
        /// </summary>
        public int CaptureNumber;

        public StartEnd LastOption => Options.Last();

        public int Start => Options[0].start;

        public int End => LastOption.end;

        public bool IsClosed => End > 0;

        public Alternation(int captureNumber, int startOfFirstOption)
        {
            CaptureNumber = captureNumber;
            Options = new List<StartEnd> { new StartEnd { start = startOfFirstOption, end = -1 } };
        }

        ///// THIS LINE NOT IMPLEMENTED YET: <i>First, mutate tokens by compressing all runs of multiple consecutive char tokens into a single UnquotedString token.</i><br></br>
        ///// EXAMPLE<br></br>
        ///// Assume that this.Options.Last().start = 1.<br></br>
        ///// Let toks = {'a', 'b', 'c', "012", 'd', 'e', 'f'}<br></br>
        ///// Close(toks) would mutate toks into {'a', UnquotedString("abc"), "012", UnquotedString("def")} and set this.Options.Last().end to 4. 
        /// <summary>
        /// Set the end of the last option in this alternation to the number of tokens.<br></br>
        /// </summary>
        /// <param name="tokens"></param>
        public void Close(int endOfCurrentOption /* List<object> tokens */)
        {
            var lastOption = Options.Last();
            Options[Options.Count - 1] = new StartEnd { start = lastOption.start, end = endOfCurrentOption };
            //int start = lastOption.start;
            //object[] tokensToCheck = tokens.LazySlice(start, tokens.Count, 1).ToArray();
            //if (tokensToCheck.Count(x => x is char) < 2)
            //{
            //}
            //int tokensToCompress = ogCount - start;
            //tokens.RemoveRange(lastOption.start, tokensToCompress);
            //var sb = new StringBuilder();

            //for (int ii = 0; ii < )
        }

        public void AddOption(int endOfCurrentOption)
        {
            Close(endOfCurrentOption);
            Options.Add(new StartEnd { start = endOfCurrentOption, end = -1 });
        }

        public bool LastOptionIsEmpty(int end)
        {
            return LastOption.start == end;
        }
    }
}