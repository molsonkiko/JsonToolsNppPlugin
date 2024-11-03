using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class YamlDumperTester
    {
        public static int MyUnitTest(string[][] testcases)
        {
            JsonParser jsonParser = new JsonParser();
            int testsFailed = 0;
            YamlDumper yamlDumper = new YamlDumper();
            for (int ii = 0; ii < testcases.Length; ii++)
            {
                var input = testcases[ii][0];
                JNode json = jsonParser.Parse(input);
                var correct = testcases[ii][1];
                var description = testcases[ii][2];
                var result = yamlDumper.Dump(json, 2);
                if (correct != result)
                {
                    Npp.AddLine(string.Format(@"Test {0} ({1}) failed:
Expected
{2}
Got
{3}",
                                      ii + 1, description, correct, result));
                    testsFailed++;
                }
            }
            Npp.AddLine("Failed " + testsFailed + " tests.");
            Npp.AddLine("Passed " + (testcases.Length - testsFailed) + " tests.");
            return testsFailed;
        }

        public static bool Test()
        {
            string[][] tests = {
				// space at end of key
				new string[]{ "{\"adogDOG! \": \"dog\"}", "\"adogDOG! \": dog\n",
                            "space at end of key" },
                new string[]{ "{\" adogDOG!\": \"dog\"}", "\" adogDOG!\": dog\n",
                             "space at start of key" },
				// space inside key
				new string[]{ "{\"a dog DOG!\": \"dog\"}", "\"a dog DOG!\": dog\n",
                            "space inside key" },
				// stringified nums as keys
				new string[]{ "{\"9\": 9}", "'9': 9\n", "stringified num as key" },
				//
				new string[] { "{\"9\": \"9\"}", "'9': '9'\n", "stringified num as val" },
                new string[] { "{\"9a\": \"9a\", \"a9.2\": \"a9.2\"}", "9a: 9a\na9.2: a9.2\n", "partially stringified nums as vals" },
                new string[] { "{\"a\\\"b'\": \"bub\\\"ar\"}", "a\"b': \"bub\\\"ar\"\n", "singlequotes and doublequotes inside key" },
                new string[] { "{\"a\": \"big\\nbad\\ndog\"}", "a: \"big\\nbad\\ndog\"\n", "values containing newlines" },
                new string[] { "{\"a\": \" big \"}", "a: \" big \"\n", "leading or ending space in dict value" },
                new string[] { "[\" big \"]", "- \" big \"\n", "leading or ending space in array value" },
                new string[] { "\"a \"", "\"a \"\n", "scalar string" },
                new string[] { "9", "9\n", "scalar int" },
                new string[] { "-940.3", "-940.3\n", "scalar float" },
                new string[] { "[true, false]", "- True\n- False\n", "scalar bools" },
                new string[] { "[null, Infinity, -Infinity, NaN]", "- null\n- .inf\n- -.inf\n- .nan\n", "null, +/-infinity, NaN" },
                // in the below case, there's actually a bit of an error;
                // it is better to dump the float 2.0 as '2.0', but this algorithm dumps it
                // as an integer.
                // So there's some room for improvement here
                new string[]{ "{\"a\": [[1, 2.0], { \"3\": [\"5\"]}], \"2\": 6}",
                             "a:\n  -\n    - 1\n    - 2.0\n  -\n    '3':\n      - '5'\n'2': 6\n",
                             "nested iterables" },
                new string[] { "{\"a\": \"a: b\"}", "a: \"a: b\"\n", "value contains colon" },
                new string[] { "{\"a: b\": \"a\"}", "\"a: b\": a\n", "key contains colon" },
                new string[] { "{\"a\": \"RT @blah: MondayMo\\\"r\'ing\"}", "a: \'RT @blah: MondayMo\"r\'\'ing\'\n", "Value contains quotes and colon" },
                new string[] { "{\"a\": \"a\\n\'big\'\\ndog\"}", "a: \"a\\n\'big\'\\ndog\"\n", "Value contains quotes and newline" },
                new string[] { "{\"a\": \"RT @blah: MondayMo\\nring\"}", "a: \"RT @blah: MondayMo\\nring\"\n", "value contains newline and colon" },
                new string[] { "{\"\\\"a: 'b'\": \"a\"}", "\'\"a: \'\'b\'\'\': a\n", "key contains quotes and colon" }
            };
            return MyUnitTest(tests) > 0; 
        }
    }
}
