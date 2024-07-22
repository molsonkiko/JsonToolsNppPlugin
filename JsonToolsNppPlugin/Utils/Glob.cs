using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JSON_Tools.Utils
{
    public enum Ternary
    {
        FALSE,
        TRUE,
        UNDECIDED,
    }

    public class GlobFunction
    {
        public Func<string, bool> Matcher { get; }
        public bool Or { get; }
        public bool Negated { get; }
        /// <summary>
        /// does the GlobFunction's pattern contain any characters in the set <b>"*?[{"</b>?<br></br>
        /// If true, it is treated as using glob syntax.
        /// </summary>
        public bool UsesMetacharacters { get; }

        public GlobFunction(Func<string, bool> matcher, bool or, bool negated, bool usesMetacharacters)
        {
            this.Matcher = matcher;
            this.Or = or;
            this.Negated = negated;
            this.UsesMetacharacters = usesMetacharacters;
        }

        public Ternary IsMatch(string inp, Ternary previousResult)
        {
            if (Or && (previousResult == Ternary.TRUE))
                return Ternary.TRUE;
            if (!Or && (previousResult == Ternary.FALSE))
                return Ternary.FALSE;
            bool result = Matcher(inp);
            return (result ^ Negated) ? Ternary.TRUE : Ternary.FALSE;
        }
    }

    /// <summary>
    /// parser that parses glob syntax in space-separated globs<br></br>
    /// 1. Default behavior is to match ALL space-separated globs
    ///     (e.g. "foo bar txt" matches "foobar.txt" and "bartxt.foo")<br></br>
    /// 2. Also use glob syntax:<br></br>
    ///     * "*" matches any number (incl. zero) of characters except "\\"<br></br>
    ///     * "**" matches any number (incl. zero) of characters including "\\"<br></br>
    ///     * "[chars]" matches all characters inside the square brackets (same as in Perl-style regex, also includes character classes like [a-z], [0-9]). Note: can match ' ', which is normally a glob separator<br></br>
    ///     * "[!chars]" matches NONE of the characters inside the square brackets (and also doesn't match "\\") (same as Perl-style "[^chars]")<br></br>
    ///     * "foo.{abc,def,hij}" matches "foo.abc", "foo.def", or "foo.hij". Essentially "{x,y,z}" is equivalent to "(?:x|y|z)" in Perl-style regex<br></br>
    ///     * "?" matches any one character except "\\"<br></br>
    /// 3. "foo | bar" matches foo OR bar ("|" implements logical OR)<br></br>
    /// 4. "!foo" matches anything that DOES NOT CONTAIN "foo"<br></br>
    /// 5. "foo | &lt;baz bar&gt;" matches foo OR (bar AND baz) (that is, "&lt;" and "&gt;" act as grouping parentheses)
    /// </summary>
    public class Glob
    {
        public int ii;
        public string ErrorMsg;
        public int ErrorPos;
        public List<string> globs;
        public List<GlobFunction> globFunctions;

        public Glob()
        {
            globs = new List<string>();
            globFunctions = new List<GlobFunction>();
            Reset();
        }

        public void Reset()
        {
            globs.Clear();
            globFunctions.Clear();
            ErrorMsg = null;
            ErrorPos = -1;
            ii = 0;
        }

        public char Peek(string inp)
        {
            return (ii < inp.Length - 1)
                ? inp[ii + 1]
                : '\x00';
        }

        /// <summary>
        /// Converts a string following glob syntax to a C# regex that applies the same tests.<br></br>
        /// usesMetacharacters is true iff inp contains any characters in the set "*?[{"
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        public (Regex rex, bool usesMetacharacters) Glob2Regex(string inp)
        {
            var sb = new StringBuilder();
            int start = ii;
            bool isCharClass = false;
            bool usesMetacharacters = false;
            bool isAlternation = false;
            while (ii < inp.Length)
            {
                char c = inp[ii];
                char nextC;
                switch (c)
                {
                case '/': sb.Append("\\\\"); break;
                case '\\':
                    nextC = Peek(inp);
                    if (nextC == 'x' || nextC == 'u' // \xNN, \uNNNN unicode escapes
                        || (nextC == ']' && isCharClass))
                        sb.Append('\\');
                    else
                        sb.Append("\\\\");
                    break;
                case '*':
                    usesMetacharacters = true;
                    if (isCharClass)
                    {
                        sb.Append("\\*"); // "[*]" matches literal * character
                        break;
                    }
                    else if (sb.Length == 0)
                        break; // since globs are only anchored at the end,
                               // leading * in globs should not influence the matching behavior.
                               // For example, the globs "*foo.txt" and "foo.txt" should match the same things.
                    nextC = Peek(inp);
                    if (nextC == '*')
                    {
                        ii++;
                        // "**" means recursive search; ignores path delimiters
                        sb.Append(".*");
                    }
                    else
                    {
                        // "*" means anything but a path delimiter
                        sb.Append(@"[^\\]*");
                    }
                    break;
                case '[':
                    usesMetacharacters = true;
                    sb.Append('[');
                    nextC = Peek(inp);
                    if (!isCharClass && nextC == '!')
                    {
                        sb.Append("^\\\\"); // [! begins a negated char class, but also need to exclude path sep
                        ii++;
                    }
                    isCharClass = true;
                    break;
                case ']':
                    isCharClass = false;
                    sb.Append(']');
                    break; // TODO: consider allowing nested [] inside char class
                case '{':
                    if (isCharClass)
                        sb.Append("\\{");
                    else
                    {
                        usesMetacharacters = true;
                        isAlternation = true; // e.g. *.{cpp,h,c} matches *.cpp or *.h or *.c
                        sb.Append("(?:");
                    }
                    break;
                case '}':
                    if (isCharClass)
                        sb.Append("\\}");
                    else
                    {
                        sb.Append(")");
                        isAlternation = false;
                    }
                    break;
                case ',':
                    if (isAlternation)
                        sb.Append('|'); // e.g. *.{cpp,h,c} matches *.cpp or *.h or *.c
                    else
                        sb.Append(',');
                    break;
                case '.':
                case '$':
                case '(':
                case ')':
                case '^':
                    // these chars have no special meaning in glob syntax, but they're regex metacharacters
                    sb.Append('\\');
                    sb.Append(c);
                    break;
                case '?':
                    if (isCharClass)
                        sb.Append('?');
                    else
                    {
                        usesMetacharacters = true;
                        sb.Append(@"[^\\]"); // '?' is any single char
                    }
                    break;
                case '|':
                case '<':
                case '>':
                case ';':
                case '\t':
                    goto endOfLoop; // these characters are never allowed in Windows paths
                case ' ':
                case '!':
                    if (isCharClass)
                        sb.Append(c);
                    else
                        goto endOfLoop; // allow ' ' and '!' inside char classes, but otherwise these are special chars in NavigateTo
                    break;
                default:
                    sb.Append(c);
                    break;
                }
                ii++;
            }
        endOfLoop:
            if (usesMetacharacters) // anything without metacharacters will just be treated as a normal string
                sb.Append('$'); // globs are anchored at the end; that is "*foo" does not match "foo/bar.txt" but "*foo.tx?" does
            string pat = sb.ToString();
            try
            {
                var regex = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                globs.Add(pat);
                return (regex, usesMetacharacters);
            }
            catch (Exception ex)
            {
                ErrorMsg = ex.Message;
                ErrorPos = start;
                return (null, false);
            }
        }

        private static readonly char[] invalidPathChars = Path.GetInvalidPathChars();

        /// <summary>
        /// Parses any number of lines, each of which contains a simple glob that matches according to the following rules:<br></br>
        /// - "*" matches any number of characters other than "/" or "\"<br></br>
        /// - "**" matches any number of characters<br></br>
        /// - "?" matches any single character other than a path separator<br></br>
        /// - "/" and "\" both match a path separator<br></br>
        /// - any other valid character matches only itself<br></br>
        /// - any other invalid character throws an exception<br></br>
        /// and returns func(x) -> true if any of the globs in inp matches x.<br></br>
        /// All characters that are not legal in a path are ignored.
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        public Func<string, bool> ParseLinesSimple(string inp)
        {
            var sb = new StringBuilder("(?:");
            Reset();
            while (ii < inp.Length)
            {
                char c = inp[ii];
                switch (c)
                {
                case '\r':
                    char nextC = Peek(inp);
                    if (nextC == '\n')
                        ii++;
                    // match the end of the current pattern, followed by another pattern that must also match the entire string
                    sb.Append('|');
                    break;
                case '\n':
                    sb.Append('|');
                    break;
                case '*':
                    nextC = Peek(inp);
                    if (nextC == '*')
                    {
                        ii++;
                        // "**" means recursive search; ignores path delimiters
                        sb.Append(".*");
                    }
                    else
                    {
                        // "*" means anything but a path delimiter
                        sb.Append(@"[^\\]*");
                    }
                    break;
                case '\\':
                case '/':
                    sb.Append("\\\\");
                    break;
                case '?':
                    sb.Append(@"[^\\]"); // '?' is any single char other than a path delimiter
                    break;
                default:
                    if (invalidPathChars.Contains(c))
                        throw new FormatException($"Windows filesystem paths cannot contain the character '{c}'");
                    sb.Append(Regex.Escape(new string(c, 1)));
                    break;
                }
                ii++;
            }
            sb.Append(")$");
            var rex = new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rex.IsMatch;
        }

        /// <summary>
        /// Parse a string as a sequence of GlobFunctions<br></br>
        /// and return a function that takes a string as input and returns the result of applying
        /// all of those GlobFunctions<br></br>
        /// using the rules described in the docstring for this class.<br></br>
        /// the fromBeginning param is reserved for use in recursive self-calls of this function.<br></br>
        /// This function mutates this.ii, this.globFunctions (if fromBeginning)
        /// </summary>
        public Func<string, bool> Parse(string inp, bool fromBeginning = true)
        {
            if (fromBeginning)
                Reset();
            bool or = false;
            bool negated = false;
            var globFuncs = fromBeginning ? this.globFunctions : new List<GlobFunction>();
            while (ii < inp.Length)
            {
                char c = inp[ii];
                if (c == ' ' || c == '\t' || c == ';') { }
                else if (c == '|')
                    or = true;
                else if (c == '!')
                    negated = !negated;
                else if (c == '<')
                {
                    ii++;
                    var subFunc = Parse(inp, false);
                    globFuncs.Add(new GlobFunction(subFunc, or, negated, true));
                    negated = false;
                    or = false;
                }
                else if (c == '>')
                    break;
                else
                {
                    (var globRegex, bool usesMetacharacters) = Glob2Regex(inp);
                    if (globRegex == null)
                        continue; // ignore errors, try to parse everything else
                    globFuncs.Add(new GlobFunction(globRegex.IsMatch, or, negated, usesMetacharacters));
                    ii--;
                    negated = false;
                    or = false;
                }
                ii++;
            }
            ii++;
            return ApplyAllGlobFunctions(globFuncs);
        }

        private static Func<string, bool> ApplyAllGlobFunctions(List<GlobFunction> globFuncs)
        {
            if (globFuncs.Count == 0)
                return (string x) => true;
            if (globFuncs.Count == 1) // special-case this for efficiency
            {
                var glofu = globFuncs[0];
                if (glofu.Negated)
                    return (string x) => !glofu.Matcher(x);
                return glofu.Matcher;
            }
            if (globFuncs.All(gf => !gf.Or))
                // return a more efficient function that short-circuits
                // if none of the GlobFunctions use logical or
                return (string x) => globFuncs.All(gf => gf.Matcher(x) ^ gf.Negated);
            bool finalFunc(string x)
            {
                Ternary result = Ternary.UNDECIDED;
                foreach (GlobFunction globFunc in globFuncs)
                {
                    result = globFunc.IsMatch(x, result);
                }
                return result != Ternary.FALSE;
            }
            return finalFunc;
        }

        /// <summary>
        /// If a simple glob without any metacharacters matches any portion of a directory name,
        /// it will also match all files in that directory.<br></br>
        /// This function applies that logic to create a more efficient function for filtering
        /// the files with a shared top directory topDir.
        /// </summary>
        /// <param name="topDir"></param>
        /// <param name="globFuncs"></param>
        /// <returns></returns>
        public static Func<string, bool> CacheGlobFuncResultsForTopDirectory(string topDir, List<GlobFunction> globFuncs)
        {
            var newFuncs = new List<GlobFunction>(globFuncs.Count);
            foreach (GlobFunction glofu in globFuncs)
            {
                if (!glofu.UsesMetacharacters && glofu.Matcher(topDir))
                {
                    var newFunc = new GlobFunction(x => true, glofu.Or, glofu.Negated, glofu.UsesMetacharacters);
                    newFuncs.Add(newFunc);
                }
                else
                {
                    newFuncs.Add(glofu);
                }
            }
            return ApplyAllGlobFunctions(newFuncs);
        }
    }
}