/*
A test runner for all of this package.
*/
using System.Threading.Tasks;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class TestRunner
    {
        public static async Task RunAll()
        {
            Npp.notepad.FileNew();
            Npp.AddLine(@"=========================
Testing JNode Copy method
=========================
");
            JsonParserTester.TestJNodeCopy();

            Npp.AddLine(@"=========================
Testing JSON parser
=========================
");
            JsonParserTester.Test();

            Npp.AddLine(@"=========================
Testing if JSON parser throws errors on bad inputs
=========================
");
            JsonParserTester.TestThrowsWhenAppropriate();

            Npp.AddLine(@"=========================
Testing JSON parser advanced options (javascript comments, dates, datetimes, singlequoted strings)
=========================
");
            JsonParserTester.TestSpecialParserSettings();

            Npp.AddLine(@"=========================
Testing JSON parser's linter functionality
=========================
");
            JsonParserTester.TestLinter();

            Npp.AddLine(@"=========================
Testing JSON Lines parser
=========================
");
            JsonParserTester.TestJsonLines();

            Npp.AddLine(@"=========================
Testing YAML dumper
=========================
");
            YamlDumperTester.Test();

            Npp.AddLine(@"=========================
Testing Binops
=========================
");
            BinopTester.Test();

            Npp.AddLine(@"=========================
Testing ArgFunctions
=========================
");
            ArgFunctionTester.Test();

            Npp.AddLine(@"=========================
Testing slice extension
=========================
");
            SliceTester.Test();

            Npp.AddLine(@"=========================
Testing RemesPath lexer
=========================
");
            RemesPathLexerTester.Test();

            Npp.AddLine(@"=========================
Testing RemesPath parser and compiler
=========================
");
            RemesParserTester.Test();

            Npp.AddLine(@"=========================
Testing that RemesPath throws errors on bad inputs
=========================
");
            RemesPathThrowsWhenAppropriateTester.Test();

            Npp.AddLine(@"=========================
Testing RemesPath assignment operations
=========================
");
            RemesPathAssignmentTester.Test();

            Npp.AddLine(@"=========================
Testing JsonSchema generator
=========================
");
            JsonSchemaMakerTester.Test();

            Npp.AddLine(@"=========================
Testing JsonSchema validator
=========================
");
            JsonSchemaValidatorTester.Test();

            Npp.AddLine(@"=========================
Testing JSON tabularizer
=========================
");
            JsonTabularizerTester.Test();

            Npp.AddLine(@"=========================
Testing JSON grepper's file reading ability
=========================
");
            JsonGrepperTester.TestFnames();

            Npp.AddLine(@"=========================
Testing JSON grepper's API request tool
=========================
");
            await JsonGrepperTester.TestApiRequester();

            Npp.AddLine(@"=========================
Testing generation of random JSON from schema
=========================
");
            RandomJsonTests.TestRandomJson();

            Npp.AddLine(@"=========================
Testing conversion of JSON to DSON (see https://dogeon.xyz/)
=========================
");
            DsonTester.TestDump();

            Npp.AddLine(@"=========================
Performance tests for JsonParser
=========================
");
            // use an absolute path to the location of this file in your repo
            string big_random_fname = @"plugins\JsonTools\testfiles\big_random.json";
            Benchmarker.BenchmarkParserAndRemesPath(new string[][] {
                new string[] { "@[@[:].a * @[:].q < @[:].e]", "float arithmetic" },
                new string[] { "@[@[:].z =~ `(?i)[a-z]{5}`]", "string operations" },
            },
            big_random_fname, 32, 64);

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
    }
}
