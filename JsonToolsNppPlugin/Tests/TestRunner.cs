/*
A test runner for all of this package.
*/
using System.Collections.Generic;
using System.Threading.Tasks;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Tests
{
    public class TestRunner
    {
        public static async Task RunAll()
        {
            Npp.notepad.FileNew();
            string header = $"Test results for JsonTools v{Npp.AssemblyVersionString()} on Notepad++ {Npp.nppVersionStr}\r\nNOTE: Ctrl-F (regular expressions *on*) for \"Failed [1-9]\\d*\" to find all failed tests";
            Npp.AddLine(header);

            var failures = new List<string>();

            Npp.AddLine(@"=========================
Testing JNode Copy method
=========================
");
            if (JsonParserTester.TestJNodeCopy())
                failures.Add("JNode copy");

            Npp.AddLine(@"=========================
Testing JSON parser
=========================
");
            if (JsonParserTester.Test())
                failures.Add("JSON parser");

            Npp.AddLine(@"=========================
Testing if JSON parser throws errors on bad inputs
=========================
");
            if (JsonParserTester.TestThrowsWhenAppropriate())
                failures.Add("JSON parser throws errors on bad inputs");

            Npp.AddLine(@"=========================
Testing JSON parser advanced options (javascript comments, dates, datetimes, singlequoted strings)
=========================
");
            if (JsonParserTester.TestSpecialParserSettings())
                failures.Add("JSON parser advanced options");

            Npp.AddLine(@"=========================
Testing JSON parser's linter functionality
=========================
");
            if (JsonParserTester.TestLinter())
                failures.Add("JSON parser's linter");

            Npp.AddLine(@"=========================
Testing JSON Lines parser
=========================
");
            if (JsonParserTester.TestJsonLines())
                failures.Add("JSON lines");

            Npp.AddLine(@"=========================
Testing that parsing of numbers does not depend on current culture
=========================
");
            if (JsonParserTester.TestCultureIssues())
                failures.Add("parsing of numbers does not depend on current culture");

            Npp.AddLine(@"=========================
Testing YAML dumper
=========================
");
            if (YamlDumperTester.Test())
                failures.Add("YAML dumper");

            Npp.AddLine(@"=========================
Testing Binops
=========================
");
            if (BinopTester.Test())
                failures.Add("Binops");

            Npp.AddLine(@"=========================
Testing ArgFunctions
=========================
");
            if (ArgFunctionTester.Test())
                failures.Add("argfunctions");

            Npp.AddLine(@"=========================
Testing slice extension
=========================
");
            if (SliceTester.Test())
                failures.Add("slice extension");

            Npp.AddLine(@"=========================
Testing Least Recently Used (LRU) cache implementation
=========================
");
            if (LruCacheTests.Test())
                failures.Add("(LRU) cache implementation");

            Npp.AddLine(@"=========================
Testing RemesPath parser and compiler
=========================
");
            if (RemesParserTester.Test())
                failures.Add("RemesPath parser");

            Npp.AddLine(@"=========================
Testing that RemesPath throws errors on bad inputs
=========================
");
            if (RemesPathThrowsWhenAppropriateTester.Test())
                failures.Add("RemesPath throws errors on bad inputs");

            Npp.AddLine(@"=========================
Testing RemesPath assignment operations
=========================
");
            if (RemesPathAssignmentTester.Test())
                failures.Add("RemesPath assignment operations");

            Npp.AddLine(@"=========================
Testing that RemesPath produces sane outputs on randomly generated queries
=========================
");
            if (RemesPathFuzzTester.Test(10000, 20))
                failures.Add("randomly generated queries");

            Npp.AddLine(@"=========================
Testing JsonSchema generator
=========================
");
            if (JsonSchemaMakerTester.Test())
                failures.Add("JsonSchema generator");

            Npp.AddLine(@"=========================
Testing JsonSchema validator
=========================
");
            if (JsonSchemaValidatorTester.Test())
                failures.Add("JsonSchema validator");

            Npp.AddLine(@"=========================
Testing JSON tabularizer
=========================
");
            if (JsonTabularizerTester.Test())
                failures.Add("JSON tabularizer");

            if (Npp.nppVersionAtLeast8)
            {
                // Notepad++ versions less than 8 (or something around 8)
                // don't have separate plugin folders for each plugin, so the tests that involve reading files
                // will cause the plugin to crash
                Npp.AddLine(@"=========================
Testing JSON grepper's file reading ability
=========================
");
                if (JsonGrepperTester.TestFnames())
                    failures.Add("JSON grepper's file reading");

                Npp.AddLine(@"=========================
Testing JSON grepper's API request tool
=========================
");
                if (Main.settings.skip_api_request_tests)
                {
                    Npp.AddLine("skipped tests because settings.skip_api_request_tests was set to true");
                }
                else
                {
                    if (await JsonGrepperTester.TestApiRequester())
                        failures.Add("JSON grepper's API request tool");
                }

                Npp.AddLine(@"=========================
Testing generation of random JSON from schema
=========================
");
                if (RandomJsonTests.TestRandomJson())
                    failures.Add("random JSON from schema");

                Npp.AddLine(@"=========================
Testing conversion of JSON to DSON (see https://dogeon.xyz/)
=========================
");
                if (DsonTester.TestDump())
                    failures.Add("DSON");

                Npp.AddLine(@"=========================
Testing JNode PathToPosition method
=========================
");
                if (FormatPathTester.Test())
                    failures.Add("PathToPosition");

                if (Npp.nppVersionAtLeast8p5p5)
                {
                    // the UI tests consistently cause NPP to hang on anything older than 8.5.5
                    // and I really don't feel like trying to learn why
                    Npp.AddLine(@"=========================
Performing UI tests by faking user actions
=========================
");
                    if (UserInterfaceTester.Test())
                        failures.Add("UI tests");
                }
                else
                    Npp.AddLine($"Skipping UI tests because they are very slow for Notepad++ versions older than v8.5.5");

                Npp.AddLine(@"=========================
Performance tests for JsonParser
=========================
");
                // use an absolute path to the location of this file in your repo
                string big_random_fname = @"plugins\JsonTools\testfiles\big_random.json";
                Benchmarker.BenchmarkParserAndRemesPath(new string[][] {
                    new string[] { "@[@[:].a * @[:].t < @[:].e]", "float arithmetic" },
                    new string[] { "@[@[:].z =~ `(?i)[a-z]{5}`]", "string operations" },
                    new string[] { "@..*", "basic recursive search" },
                },
                big_random_fname, 32, 64);

                Npp.AddLine(@"=========================
Performance tests for JSON compression and pretty-printing
=========================
");
                Benchmarker.BenchmarkJNodeToString(64, big_random_fname);

                Npp.AddLine($@"=========================
Performance tests for JsonSchemaValidator and random JSON creation
=========================
");
                Benchmarker.BenchmarkRandomJsonAndSchemaValidation(64);

                //because Visual Studio runs a whole bunch of other things in the background
                //     when I build my project, the benchmarking suite
                //     makes my code seem way slower than it actually is when it's running unhindered.
                //     * *To see how fast the code actually is, you need to run the executable outside of Visual Studio.**
            }
            else
                Npp.AddLine("Skipping UI tests and all tests that would involve reading a file, because they would cause Notepad++ versions older than v8 to crash");

            Npp.editor.InsertText(header.Length + 2, "Tests failed: " + string.Join(", ", failures) + "\r\n");
        }
    }
}
