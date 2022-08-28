/*
A test runner for all of this package.
There is also a CLI utility that accepts a letter ('j' or 'y') and a filename as an argument and prints the
resultant pretty-printed JSON (if first arg j) or YAML (if first arg y) document (UTF-8 encoded). 
Redirecting this output to a text file allows the creation of a new YAML file.
*/
using System;
using System.IO;
using System.Linq;
using System.Text;
using JSON_Tools.Utils;
using JSON_Tools.JSON_Tools;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Tests
{
    public class TestRunner
    {
        /// <summary>
        /// If no command line args are given, runs all tests for everything in this package and displays the results.
        /// Optionally, this can take two args:
        /// 1. the letter "j" (for JSON) or "y" (for YAML)
        /// 2. The filename of a JSON file, not enclosed in quotes. Spaces in the filename are fine.
        /// If those args are supplied, this will dump the JSON file as pretty-printed JSON if the j arg was given, 
        /// or as YAML if the y arg was given.
        /// </summary>
        /// <param name="args"></param>
        public static void RunAll()
        {
            Npp.notepad.FileNew();
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
Performance tests for JsonParser and RemesPath
=========================
");
            // use an absolute path to the location of this file in your repo
            string big_random_fname = @"C:\Users\mjols\Documents\csharp\JsonToolsPlugin\testfiles\big_random.json";
            Benchmarker.Benchmark("@[@[:].z =~ `(?i)[a-z]{5}`]", big_random_fname, 14);
            //because Visual Studio runs a whole bunch of other things in the background
            //     when I build my project, the benchmarking suite
            //     makes my code seem way slower than it actually is when it's running unhindered.
            //     * *To see how fast the code actually is, you need to run the executable outside of Visual Studio.**
        }
    }
}
