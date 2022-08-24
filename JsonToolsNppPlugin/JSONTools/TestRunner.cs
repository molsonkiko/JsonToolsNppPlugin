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

namespace JSON_Tools.JSON_Tools
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
        public static void RunAll(string[] args)
        {
            YamlDumper yamlDumper = new YamlDumper();
            var sw = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8);
            sw.AutoFlush = true;
            Console.SetOut(sw);
            JsonParser parser = new JsonParser();
            if (args.Length == 0)
            {
                Console.WriteLine(@"=========================
Testing JSON parser
=========================
");
                JsonParserTester.Test();
                Console.WriteLine(@"=========================
Testing JSON parser advanced options (javascript comments, dates, datetimes, singlequoted strings)
=========================
");
                JsonParserTester.TestSpecialParserSettings();
                Console.WriteLine(@"=========================
Testing YAML dumper
=========================
");
                YamlDumperTester.Test();
                Console.WriteLine(@"=========================
Testing Binops
=========================
");
                BinopTester.Test();
                Console.WriteLine(@"=========================
Testing ArgFunctions
=========================
");
                ArgFunctionTester.Test();
                Console.WriteLine(@"=========================
Testing slice extension
=========================
");
                SliceTester.Test();
                Console.WriteLine(@"=========================
Testing RemesPath lexer
=========================
");
                RemesPathLexerTester.Test();
                Console.WriteLine(@"=========================
Testing RemesPath parser and compiler
=========================
");
                RemesParserTester.Test();
                Console.WriteLine(@"=========================
Performance tests for JsonParser and RemesPath
=========================
");
                string big_random_fname = "../../testfiles/big_random.json";
                RemesPathBenchmarker.Benchmark("@[@[:].z =~ `(?i)[a-z]{5}`]", big_random_fname, 14);
                // because Visual Studio runs a whole bunch of other things in the background
                // when I build my project, the benchmarking suite
                // makes my code seem way slower than it actually is when it's running unhindered.
                // **To see how fast the code actually is, you need to run the executable outside of Visual Studio.**

                Console.WriteLine(@"=========================
Testing JsonSchema generator
=========================
");
                JsonSchemaMakerTester.Test();
                Console.WriteLine(@"=========================
Testing JSON tabularizer
=========================
");
                JsonTabularizerTester.Test();
                Console.WriteLine(@"=========================
Testing JSON parser's linter functionality
=========================
");
                JsonParserTester.TestLinter();
//                Console.WriteLine(@"=========================
//Testing JSON grepper's file reading ability
//=========================
//");
//                JsonGrepperTester.TestFnames();
            }
            else
            {
                JsonParser jsonParser = new JsonParser();
                string out_type = args[0].ToLower();
                // Slice extension method from JsonPath module
                string fname = string.Join(" ", args.Slice("1:").ToArray());
                StreamReader streamReader = new StreamReader(fname);
                string jsonstr = streamReader.ReadToEnd();
                JNode json = jsonParser.Parse(jsonstr);
                streamReader.Close();
                // sw.WriteLine(EncodeNonAsciiCharacters(dumper.Dump(json, 2)));
                // the above line would convert UTF-16 characters to \uxxxx format.
                // That may be desirable, but in my experience it is unnecessary.
                if (out_type[0] == 'j')
                {
                    sw.WriteLine((out_type.Length == 2 && out_type[1] == 'p') ? json.PrettyPrint(4) : json.ToString());
                }
                else
                {
                    sw.WriteLine(yamlDumper.Dump(json, 2));
                }
            }
        }
    }
}
