using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JSON_Tools.Tests
{
    public class RandomStringFromRegexTests
    {
        public const int MAX_REPS_SIMPLE_TESTCASES = 105;

        public static bool Test()
        {
            int ii = 0;
            int testsFailed = 0;

            RandomStringFromRegex.ClearCache();

            // ========== simple testcases, where the set of strings matched by the regex is relatively small ==========
            var simpleTestcases = new (string rex, HashSet<string> possibilities)[]
            {
                (@"(a|bb|^)\1(c?) \2", new HashSet<string>{ "aa ", "bbbb ", " ", "aac c", "bbbbc c", "c c" }),
                (@"\u0031[0-0a-c]{0,2}", new HashSet<string>{ "1", "10", "1a", "1b", "1c", "100", "10a", "10b", "10c", 
                                                                                       "1a0", "1aa", "1ab", "1ac",
                                                                                       "1b0", "1ba", "1bb", "1bc",
                                                                                       "1c0", "1ca", "1cb", "1cc",}),
                (@"[^\1-\77\x42-\u007F]\$(?i)[r-t]", new HashSet<string>{"@$r", "@$s", "@$t",
                                                                         "A$r", "A$s", "A$t",
                                                                         "@$R", "@$S", "@$T",
                                                                         "A$R", "A$S", "A$T"}),
                (@"(?i)[0-9-q\x40-\da\t\f\r\n\n]", new HashSet<string>{"q", "Q", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "@", "a", "A", "-", "\r", "\n", "\f", "\t"}),
                (@"(?i:\x63[\x6f](?-i:ff))ee", new HashSet<string>{"coffee", "Coffee", "cOffee", "COffee"}),
                (@"(foo)|(yj)baz\2$", new HashSet<string>{"foo", "yjbazyj"}),
                (@"(^sdwq)|(y\u004A)uip(?i)\2| 23\$\^&\*\+\{\}[\[][\]]\|\(\)\\7\t(?:\f\r)\n\&", new HashSet<string>{"sdwq", "yJuipyj", "yJuipyJ", "yJuipYj", "yJuipYJ", " 23$^&*+{}[]|()\\7\t\f\r\n&"}),
                ("(?:23)*-$", new HashSet<string>{"-", "23-", "2323-", "232323-", "23232323-", "2323232323-", "232323232323-", "23232323232323-", "2323232323232323-", "232323232323232323-", "23232323232323232323-", "2323232323232323232323-", "232323232323232323232323-", "23232323232323232323232323-", "2323232323232323232323232323-", "232323232323232323232323232323-", "23232323232323232323232323232323-", "2323232323232323232323232323232323-", "232323232323232323232323232323232323-", "23232323232323232323232323232323232323-", "2323232323232323232323232323232323232323-"}),
                (@"(#)+\1{1}", new HashSet<string>{"##", "###", "####", "#####", "######", "#######", "########", "#########", "##########", "###########", "############", "#############", "##############", "###############", "################", "#################", "##################", "###################", "####################", "#####################"}),
                (@"((?i)^a{,2} )\1w{,1}", new HashSet<string>{
                    "aa aa w", "Aa Aa w", "aA aA w", "AA AA w", "a a w", "A A w", "  w",
                    "aa aa ", "Aa Aa ", "aA aA ", "AA AA ", "a a ", "A A ", "  "
                }),
                ("(?:(?:%{11,}$))", new HashSet<string>{ "%%%%%%%%%%%", "%%%%%%%%%%%%", "%%%%%%%%%%%%%", "%%%%%%%%%%%%%%", "%%%%%%%%%%%%%%%", "%%%%%%%%%%%%%%%%", "%%%%%%%%%%%%%%%%%", "%%%%%%%%%%%%%%%%%%", "%%%%%%%%%%%%%%%%%%%", "%%%%%%%%%%%%%%%%%%%%" }),
                (@"<\^{14,17}>\?", new HashSet<string>{ "<^^^^^^^^^^^^^^>?", "<^^^^^^^^^^^^^^^>?", "<^^^^^^^^^^^^^^^^>?", "<^^^^^^^^^^^^^^^^^>?" }),
                (@"^(?:(fo|ba) ){2}\t\1{1,3}$", new HashSet<string>{ "fo fo \tfo", "fo fo \tfofo", "fo fo \tfofofo",
                                                                   "ba fo \tfo", "ba fo \tfofo", "ba fo \tfofofo",
                                                                   "fo ba \tba", "fo ba \tbaba", "fo ba \tbababa",
                                                                   "ba ba \tba", "ba ba \tbaba", "ba ba \tbababa" }),
                (@"(?-i)(?:xy{1,2}){,2}", new HashSet<string>{"", "xy", "xyy", "xyxy", "xyxyy", "xyyxy", "xyyxyy"}),
                (@"(b|g{3})?", new HashSet<string>{"", "b", "ggg"}),
                ("[a-][b-c-]|[hij-][-][+]", new HashSet<string>{"ab", "ac", "a-", "-b", "-c", "--", "h-+", "i-+", "j-+", "--+"}),

            };
            ii += simpleTestcases.Length * 2;
            foreach ((string rex, HashSet<string> possibilities) in simpleTestcases)
            {
                string possibilitiesAsJArrayStr = new JArray(0, possibilities.OrderBy(x => x).Select(x => new JNode(x)).ToList()).ToString();
                string rexAsJsonStr = JNode.StrToString(rex, true);
                string baseFailMsg = $"FAIL: expected  RandomStringFromRegex.GetGenerator({rexAsJsonStr}) to return strings from the set {possibilitiesAsJArrayStr}\r\nInstead ";
                Func<string> generator;
                try
                {
                    generator = RandomStringFromRegex.GetGenerator(rex);
                }
                catch (Exception ex)
                {
                    testsFailed += 2;
                    Npp.AddLine(baseFailMsg + $"got the following exception while compiling a generator:\r\n{ex}");
                    continue;
                }
                if (generator is null)
                {
                    testsFailed += 2;
                    Npp.AddLine(baseFailMsg + "got a null generator");
                    continue;
                }
                HashSet<string> valuesGenerated = new HashSet<string>();
                int minDistinctValuesGenerated;
                if (possibilities.Count > 11)
                    minDistinctValuesGenerated = (possibilities.Count * 2) / 3;
                else
                    minDistinctValuesGenerated = possibilities.Count < 7 ? possibilities.Count : 7;
                if (minDistinctValuesGenerated > 20)
                    minDistinctValuesGenerated = 20;
                int runtimeErrorCount = 0;
                int maxRuntimeErrorCount = 3;
                for (int jj = 0; jj < MAX_REPS_SIMPLE_TESTCASES; jj++)
                {
                    try
                    {
                        valuesGenerated.Add(generator());
                    }
                    catch (Exception ex)
                    {
                        Npp.AddLine(baseFailMsg + $"got the following exception while running a compiled generator:\r\n{ex}");
                        if (++runtimeErrorCount >= maxRuntimeErrorCount)
                            break;
                    }
                }
                if (runtimeErrorCount >= maxRuntimeErrorCount)
                {
                    testsFailed += 2;
                    continue;
                }
                string impossibleValuesGenerated = new JArray(0, valuesGenerated.Except(possibilities).OrderBy(x => x).Select(x => new JNode(x)).ToList()).ToString();
                if (valuesGenerated.Except(possibilities).ToArray().Length > 0)
                {
                    testsFailed++;
                    Npp.AddLine(baseFailMsg + $"got the following values that were not matched by the original regex:\r\n{impossibleValuesGenerated}");
                }
                if (valuesGenerated.Count < minDistinctValuesGenerated)
                {
                    string valuesGeneratedStr = new JArray(0, valuesGenerated.OrderBy(x => x).Select(x => new JNode(x)).ToList()).ToString();
                    testsFailed++;
                    Npp.AddLine(baseFailMsg + $"only generated the following values:\r\n{valuesGeneratedStr}");
                }
            }

            // ========== verify that the compiler throws an error of the correct type when faced with various invalid regexes  ==========
            var mustThrowTestcases = new (string rex, RandomStringFromRegexException.ExceptionType extype, bool runTime)[]
            {
                ("(foo", RandomStringFromRegexException.ExceptionType.UnclosedParen, false),
                ("(?:foo", RandomStringFromRegexException.ExceptionType.UnclosedParen, false),
                (@"unfinished\x0 hex", RandomStringFromRegexException.ExceptionType.ExpectedHexChar, false),
                (@"unfinished\xhex", RandomStringFromRegexException.ExceptionType.ExpectedHexChar, false),
                (@"unfinished\u003hex", RandomStringFromRegexException.ExceptionType.ExpectedHexChar, false),
                (@"unfinished\uhex", RandomStringFromRegexException.ExceptionType.ExpectedHexChar, false),
                (@"(?:(foo)|(bar)) \1\2", RandomStringFromRegexException.ExceptionType.BackreferenceToUncapturedGroup, true),
                ("[c-a]", RandomStringFromRegexException.ExceptionType.BadCharRange, false),
                ("\\g", RandomStringFromRegexException.ExceptionType.InvalidEscape, false),
                ("a^", RandomStringFromRegexException.ExceptionType.CaretAfterStartOfString, true),
                ("$b", RandomStringFromRegexException.ExceptionType.DollarBeforeEndOfString, false),
                (@"(a)$\1", RandomStringFromRegexException.ExceptionType.DollarBeforeEndOfString, false),
                (@"b$a", RandomStringFromRegexException.ExceptionType.DollarBeforeEndOfString, false),
                ("c{32}", RandomStringFromRegexException.ExceptionType.QuantifierExceedsMaxReps, false),
                ("(?:foo|+)", RandomStringFromRegexException.ExceptionType.NothingToQuantify, false),
                ("{7}", RandomStringFromRegexException.ExceptionType.NothingToQuantify, false),
                ("(+|yy|)", RandomStringFromRegexException.ExceptionType.NothingToQuantify, false),
                ("x+?", RandomStringFromRegexException.ExceptionType.TwoConsecutiveQuantifiers, false),
                ("(Foo|bar)?{8}", RandomStringFromRegexException.ExceptionType.TwoConsecutiveQuantifiers, false),
                ("(?:baz(Foo|bar))?{8}", RandomStringFromRegexException.ExceptionType.TwoConsecutiveQuantifiers, false),
                ("jjjx{,5}{6}(?:fooo)", RandomStringFromRegexException.ExceptionType.TwoConsecutiveQuantifiers, false),
                ("c{1,32}", RandomStringFromRegexException.ExceptionType.QuantifierExceedsMaxReps, false),
                (@"(foo)(bar)\3(baz)", RandomStringFromRegexException.ExceptionType.BadBackReference, false),
                (@"a{,\3", RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier, false),
                (@"a{f", RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier, false),
                (@"a{1,f", RandomStringFromRegexException.ExceptionType.BadCurlybraceQuantifier, false),
                ("(?8)bar)", RandomStringFromRegexException.ExceptionType.InvalidFlag, false),
                ("(?sz:bar)", RandomStringFromRegexException.ExceptionType.InvalidFlag, false),
                ("(?s-iq)foo)", RandomStringFromRegexException.ExceptionType.InvalidFlag, false),
            };

            ii += mustThrowTestcases.Length;

            foreach ((string rex, RandomStringFromRegexException.ExceptionType extype, bool runTime) in mustThrowTestcases)
            {
                string baseErrMsg = $"FAIL: Expected RandomStringFromRegex.GetGenerator({JNode.StrToString(rex, true)}) to throw a RandomStringFromRegexException of type {extype}.\r\nInstead "; 
                try
                {
                    Func<string> generator = RandomStringFromRegex.GetGenerator(rex);
                    // most errors occur while compiling, but some are only caught at runtime
                    if (runTime)
                        generator();
                    testsFailed++;
                    Npp.AddLine(baseErrMsg + "did not throw an exception.");
                }
                catch (Exception ex)
                {
                    if (!(ex is RandomStringFromRegexException rsfre && rsfre.ExType == extype))
                    {
                        testsFailed++;
                        Npp.AddLine($"{baseErrMsg}got the following exception:\r\n{ex}");
                    }
                }
            }

            RandomStringFromRegex.ClearCache();

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }
    }
}
