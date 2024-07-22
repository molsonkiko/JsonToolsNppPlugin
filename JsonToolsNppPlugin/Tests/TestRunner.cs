/*
A test runner for all of this package.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Tests
{
    public class TestRunner
    {
        private const string fuzzTestName = "RemesPath produces sane outputs on randomly generated queries";

        public static async Task RunAll()
        {
            Npp.notepad.FileNew();
            string header = $"Test results for JsonTools v{Npp.AssemblyVersionString()} on Notepad++ {Npp.nppVersionStr}\r\nNOTE: Ctrl-F (regular expressions *on*) for \"Failed [1-9]\\d*\" to find all failed tests";
            Npp.AddLine(header);

            string bigRandomFname = Path.Combine(Npp.pluginDllDirectory, "testfiles", "big_random.json");
            var tests = new (Func<bool> tester, string name, bool onlyIfNpp8Plus, bool onlyIfNpp8p5p5Plus)[]
            {
                (JsonParserTester.TestJNodeCopy, "JNode Copy method", false, false),
                (JsonParserTester.Test, "JSON parser", false, false),
                (JsonParserTester.TestThrowsWhenAppropriate, "if JSON parser throws errors on bad inputs", false, false),
                (JsonParserTester.TestSpecialParserSettings, "JSON parser advanced options", false, false),
                (JsonParserTester.TestLinter, "JSON parser's linter", false, false),
                (JsonParserTester.TestJsonLines, "JSON Lines parser", false, false),
                (JsonParserTester.TestCultureIssues, "parsing of numbers does not depend on current culture", false, false),
                (JsonParserTester.TestTryParseNumber, "JsonParser.TryParseNumber method", false, false),

                (YamlDumperTester.Test, "YAML dumper", false, false),
                
                (SliceTester.Test, "slice extension", false, false),
                
                (LruCacheTests.Test, "Least Recently Used (LRU) cache implementation", false, false),
                
                (RemesParserTester.Test, "RemesPath parser and compiler", false, false),
                (RemesPathThrowsWhenAppropriateTester.Test, "RemesPath throws errors on bad inputs", false, false),
                (RemesPathAssignmentTester.Test, "RemesPath assignment operations", false, false),
                (() => RemesPathFuzzTester.Test(3750, 16), fuzzTestName, false, false),
                (RemesPathComplexQueryTester.Test, "multi-statement queries in RemesPath", false, false),
                
                (JsonSchemaMakerTester.Test, "JsonSchema generator", false, false),
                (JsonSchemaValidatorTester.Test, "JsonSchema validator", false, false),
                
                (JsonTabularizerTester.Test, "JSON tabularizer", false, false),

                (CsvSnifferTests.Test, "CSV sniffer", false, false),

                (GlobTester.TestParseLinesSimple, "Glob syntax parser", false, false),
                
                // tests that require reading files (skip on Notepad++ earlier than v8)
                (JsonGrepperTester.TestFnames, "JSON grepper's file reading ability", true, false),
                (RandomJsonTests.TestRandomJson, "generation of random JSON from schema", true, false),
                (DsonTester.TestDump, "conversion of JSON to DSON (see https://dogeon.xyz/)", true, false),
                (FormatPathTester.Test, "JNode PathToPosition method", true, false),
                (IniFileParserTests.Test, "INI file parser", true, false),
                
                (UserInterfaceTester.Test, "UI tests", true, false),
                
                //because Visual Studio runs a whole bunch of other things in the background
                //     when I build my project, the benchmarking suite
                //     makes my code seem way slower than it actually is when it's running unhindered.
                //     * *To see how fast the code actually is, you need to run the executable outside of Visual Studio.**
                (() => Benchmarker.BenchmarkParserAndRemesPath(
                    new string[][] {
                        new string[] { "@[@[:].a * @[:].t < @[:].e]", "float arithmetic"
                        },
                        new string[] { "@[@[:].z =~ `(?i)[a-z]{5}`]", "string operations"
                        },
                        new string[] { "@..*", "basic recursive search"
                        },
                        new string[] { "group_by(@, s).*{\r\n" +
                                       "    Hmax: max((@[:].H)..*[is_num(@)][abs(@) < Infinity]),\r\n" +
                                       "    min_N: min((@[:].N)..*[is_num(@)][abs(@) < Infinity])\r\n" +
                                       "}",
                            "group_by, projections and aggregations"
                        },
                        new string[]{ "var qmask = @[:].q;\r\n" +
                                      "var nmax_q = max(@[qmask].n);\r\n" +
                                      "var nmax_notq = max(@[not qmask].n);\r\n" +
                                      "ifelse(nmax_q > nmax_notq, `when q=true, nmax = ` + str(nmax_q), `when q=false, nmax= ` + str(nmax_notq))",
                            "variable assignments and simple aggregations"
                        },
                        new string[]{ "var X = X;\r\n" +
                                      "var onetwo = j`[1, 2]`;\r\n" +
                                      "@[:]->at(@, X)->at(@, onetwo)",
                            "references to compile-time constant variables"
                        },
                        new string[]{ "var X = @->`X`;\r\n" +
                                      "var onetwo = @{1, 2};\r\n" +
                                      "@[:]->at(@, X)->at(@, onetwo)",
                            "references to variables that are not compile-time constants"
                        },
                        new string[]{"@[:].z = s_sub(@, g, B)", "simple string mutations"
                        },
                        new string[]{"@[:].x = ifelse(@ < 0.5, @ + 3, @ - 3)", "simple number mutations"
                        },
                        new string[]{"var xhalf = @[:].x < 0.5;\r\n" +
                                    "for lx = zip(@[:].l, xhalf);\r\n" +
                                    "    lx[0] = ifelse(lx[1], foo, bar);\r\n" +
                                    "end for;",
                            "mutations with a for loop"}
                    },
                    bigRandomFname, 32, 40
                    ), 
                    "JsonParser performance",
                    true, false
                ),
                //(() => Benchmarker.BenchmarkParsingAndLintingJsonWithErrors(30), "JsonParser performance and performance of JsonLint.message"),
                (() => Benchmarker.BenchmarkJNodeToString(64, bigRandomFname),
                    "performance of JSON compression and pretty-printing",
                    true, false
                ),
                (() => Benchmarker.BenchmarkRandomJsonAndSchemaValidation(64),
                    "performance of JsonSchemaValidator and random JSON creation",
                    true, false
                ),
            };

            var failures = new List<string>();
            var skipped = new List<string>();
            bool hasExplainedSkipLessThanNppV8 = false;

            foreach ((Func<bool> tester, string name, bool onlyIfNpp8Plus, bool onlyIfNpp8p5p5Plus) in tests)
            {
                if (onlyIfNpp8Plus && !Npp.nppVersionAtLeast8)
                {
                    // Notepad++ versions less than 8 (or something around 8)
                    // don't have separate plugin folders for each plugin, so the tests that involve reading files
                    // will cause the plugin to crash
                    if (!hasExplainedSkipLessThanNppV8)
                    {
                        hasExplainedSkipLessThanNppV8 = true;
                        Npp.AddLine("Skipping UI tests and all tests that would involve reading a file, because they would cause Notepad++ versions older than v8 to crash");
                    }
                    skipped.Add(name);
                }
                else if (Main.settings.skip_api_request_and_fuzz_tests && name == fuzzTestName)
                {
                    Npp.AddLine("\r\nskipped RemesPath fuzz tests because settings.skip_api_request_and_fuzz_tests was set to true");
                    skipped.Add(name);
                }
                else
                {
                    Npp.AddLine($@"=========================
Testing {name}
=========================
");
                    if (tester())
                        failures.Add(name);
                }
            }

            if (Npp.nppVersionAtLeast8)
            {
                // need to do this one separately because it's async
                Npp.AddLine(@"=========================
Testing JSON grepper's API request tool
=========================
");
                if (Main.settings.skip_api_request_and_fuzz_tests)
                {
                    Npp.AddLine("skipped tests because settings.skip_api_request_and_fuzz_tests was set to true");
                    skipped.Add("JSON grepper's API request tool");
                }
                else
                {
                    if (await JsonGrepperTester.TestApiRequester())
                        failures.Add("JSON grepper's API request tool");
                }
            }

            if (skipped.Count > 0)
                Npp.editor.InsertText(header.Length + 2, "Tests skipped: " + string.Join(", ", skipped) + "\r\n");
            Npp.editor.InsertText(header.Length + 2, "Tests failed: " + string.Join(", ", failures) + "\r\n");
        }
    }
}
